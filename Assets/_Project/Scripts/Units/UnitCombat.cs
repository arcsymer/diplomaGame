using System;
using System.Collections.Generic;
using DiplomaGame.Runtime.Buildings;
using DiplomaGame.Runtime.Combat;
using DiplomaGame.Runtime.Data;
using DiplomaGame.Runtime.Hero;
using DiplomaGame.Runtime.Tech;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace DiplomaGame.Runtime.Units
{
    /// <summary>
    /// Боевой ИИ юнита. Автоматически выбирает цели (юниты И здания вражеской фракции),
    /// атакует, преследует и отступает.
    /// Не дублирует движение — делегирует Unit.MoveToInternal / StopInternal.
    /// </summary>
    [RequireComponent(typeof(Unit))]
    [RequireComponent(typeof(Health))]
    public sealed class UnitCombat : MonoBehaviour
    {
        // ----------------------------------------------------------------
        // Статическое событие (M7 Audio шина)
        // ----------------------------------------------------------------

        /// <summary>Вызывается при атаке любого UnitCombat. Параметр — мировая позиция атакующего.</summary>
        public static event Action<Vector3> AnyAttacked;

        // ----------------------------------------------------------------
        // Инстанс-событие (v5 — UnitAnimator)
        // ----------------------------------------------------------------

        /// <summary>
        /// Вызывается при каждой успешной атаке данного юнита.
        /// UnitAnimator подписывается на это событие для запуска trigger Attack.
        /// Намеренно отдельное от AnyAttacked: избегает ненужных вызовов у всех юнитов.
        /// </summary>
        public event Action Attacked;

        // ----------------------------------------------------------------
        // Диагностические события (BalanceDiag)
        // ----------------------------------------------------------------

        /// <summary>
        /// Вызывается при первом выстреле любого UnitCombat за сессию.
        /// Параметры: фракция атакующего, мировая позиция, EntityId.
        /// </summary>
        public static event Action<Faction, Vector3, int> AnyAttackedWithFaction;

        /// <summary>
        /// Вызывается при начале отступления любого UnitCombat.
        /// Параметры: фракция отступающего, мировая позиция.
        /// </summary>
        public static event Action<Faction, Vector3> AnyRetreatStarted;

        [SerializeField] private UnitData _data;

        // Дополнительный буфер к attackRange при атаке здания (здания крупные).
        private const float BuildingAttackBuffer = 2f;

        // ----------------------------------------------------------------
        // Публичный API для тестов и HUD
        // ----------------------------------------------------------------

        /// <summary>Текущее состояние боевого ИИ.</summary>
        public CombatState CurrentCombatState { get; private set; } = CombatState.None;

        /// <summary>UnitData этого юнита (read-only). Null если не задан.</summary>
        public UnitData Data => _data;

        // ----------------------------------------------------------------
        // Кэшированные ссылки
        // ----------------------------------------------------------------

        private Unit         _unit;
        private Health       _health;
        private NavMeshAgent _agent;

        // Rally-точка своей базы (кэш из Awake)
        private Vector3 _rallyPoint;

        // Кэш: является ли текущая цель зданием (нет NavMeshAgent).
        // Обновляется при каждой смене _currentTargetTransform.
        private bool _currentTargetIsBuilding;

        // Кэш: является ли этот юнит героем (есть HeroController). Вычисляется в Awake.
        private bool _isHero;

        // ----------------------------------------------------------------
        // Буферы без аллокаций — кандидаты (Health + позиции)
        // ----------------------------------------------------------------

        private readonly List<Health>  _candidateHealths   = new List<Health>(32);
        private readonly List<Vector3> _candidatePositions = new List<Vector3>(32);

        // Временные буферы для заполнения из реестров
        private readonly List<Unit>     _unitBuffer     = new List<Unit>(32);
        private readonly List<Building> _buildingBuffer = new List<Building>(32);

        // Переиспользуемый буфер для AoE-индексов (без аллокаций в Update-пути)
        private readonly List<int> _aoeIndexBuffer = new List<int>(16);

        [Tooltip("Опциональный эффект AoE-атаки. Воспроизводится при каждом AoE-ударе.")]
        [SerializeField] private ParticleSystem _aoeVfx;

        // ----------------------------------------------------------------
        // Внутреннее состояние
        // ----------------------------------------------------------------

        /// <summary>Текущая цель: Health юнита или здания.</summary>
        private Health    _currentTargetHealth;
        private Transform _currentTargetTransform;

        private float _scanTimer;
        private float _lastAttackTime;

        private const float ScanInterval = 0.25f;

        // Флаг: отступление уже было активировано (для одноразового срабатывания)
        private bool  _retreatTriggered;

        // Time.time в момент, когда началось последнее отступление (для кулдауна)
        private float _lastRetreatTime;

        // Флаг: ретрит временно подавлен stall-breaker'ом (только для тест-харнессов).
        // Сбрасывается при следующем явном приказе через OnCommandIssued.
        // Не влияет на production-логику — устанавливается только через internal-метод.
        private bool _retreatSuppressedForBreaker;

        // ----------------------------------------------------------------
        // Unity lifecycle
        // ----------------------------------------------------------------

        private void Awake()
        {
            _unit   = GetComponent<Unit>();
            _health = GetComponent<Health>();
            _agent  = GetComponent<NavMeshAgent>();

            // Кэш: определяем один раз, является ли этот юнит героем
            _isHero = GetComponent<HeroController>() != null;

            // Подписка на события
            _unit.CommandIssued += OnCommandIssued;
            _health.Died        += OnDied;

            // Кэш rally-точки по фракции
            _rallyPoint = FindRallyPoint(_unit.Faction);

            // Применяем UnitData, если задан
            if (_data != null)
                ApplyData(_data);
        }

        private void Start()
        {
            // Рандомизируем начальный кулдаун атаки, чтобы устранить first-mover advantage
            // от порядка Update: юниты, созданные первыми, иначе стреляют раньше всех.
            // Используем GetEntityId() XOR фракционную соль как seed — это разрывает
            // корреляцию между двумя командами, которые получают последовательные блоки ID:
            // без соли соседние ID двух команд давали похожие hash-значения → системный перекос.
            if (_data != null)
            {
                int factionSalt = _unit.Faction == Faction.Player ? 0x27D4EB2F : unchecked((int)0x9B2257E5u);
                int seed = (int)gameObject.GetEntityId() ^ factionSalt;
                float offset = CombatLogic.RandomiseInitialCooldownOffset(_data.AttackCooldown, seed);
                _lastAttackTime = Time.time - _data.AttackCooldown + offset;

                // Фаза сканирования тоже рандомизируется: иначе все юниты сканируют
                // в один кадр, и порядок Update решает, кто захватит цель первым.
                _scanTimer = CombatLogic.RandomiseInitialCooldownOffset(ScanInterval, seed * 31);
            }
        }

        private void OnDestroy()
        {
            if (_unit != null)
                _unit.CommandIssued -= OnCommandIssued;
            if (_health != null)
                _health.Died -= OnDied;
        }

        private void Update()
        {
            if (_health.IsDead) return;

            // --- Периодическое сканирование ---
            _scanTimer += Time.deltaTime;
            bool doScan = _scanTimer >= ScanInterval;
            if (doScan)
                _scanTimer = 0f;

            // --- Логика состояний ---
            if (CurrentCombatState == CombatState.Retreating)
            {
                UpdateRetreating(doScan);
                return;
            }

            // Проверяем отступление первым делом.
            // _retreatTriggered блокирует повторный ретрит до истечения кулдауна.
            // Если кулдаун > 0 и прошло достаточно времени — разрешаем снова.
            if (_retreatTriggered && _data != null &&
                CombatLogic.CanRetreatAgain(_data.RetreatCooldown, _lastRetreatTime, Time.time))
            {
                _retreatTriggered = false;
            }

            if (!_retreatTriggered && ShouldStartRetreat())
            {
                StartRetreat();
                return;
            }

            if (doScan)
                ScanForTarget();

            UpdateCombat();
        }

        // ----------------------------------------------------------------
        // Internal — для тестов
        // ----------------------------------------------------------------

        /// <summary>
        /// Инициализирует UnitData без SerializedObject (доступно в PlayMode-тестах).
        /// Рандомизирует начальный кулдаун атаки так же, как Start().
        /// </summary>
        internal void InitForTest(UnitData data)
        {
            _data = data;
            if (_health != null)
                ApplyData(data);

            // Рандомизация кулдауна — применяем сразу, т.к. Start() может вызваться
            // раньше или позже InitForTest в зависимости от порядка AddComponent/Start.
            // Сдвиг на -AttackCooldown — как в Start(): первый выстрел через offset секунд.
            // Используем ту же логику seed (EntityId ^ factionSalt), что и Start().
            int factionSalt = _unit != null && _unit.Faction == Faction.Player
                ? 0x27D4EB2F
                : unchecked((int)0x9B2257E5u);
            int seed = (int)gameObject.GetEntityId() ^ factionSalt;
            float offset = CombatLogic.RandomiseInitialCooldownOffset(data.AttackCooldown, seed);
            _lastAttackTime = Time.time - data.AttackCooldown + offset;
            _scanTimer = CombatLogic.RandomiseInitialCooldownOffset(ScanInterval, seed * 31);
        }

        /// <summary>
        /// Снимает активное отступление и подавляет повторный ретрит до следующей
        /// явной команды от игрока / EnemyCommander.
        /// Предназначено ТОЛЬКО для stall-breaker'ов тест-харнессов.
        ///
        /// Правильный порядок вызова:
        ///   1. IssueCommand(AttackMove(...))   ← OnCommandIssued сбросит _retreatSuppressedForBreaker
        ///   2. SuppressRetreatForStallBreaker() ← выставляет _retreatSuppressedForBreaker = true
        ///
        /// Пока флаг активен, ShouldStartRetreat() всегда возвращает false,
        /// поэтому юнит с HP ≤ retreatHpFraction идёт к врагу не отвлекаясь.
        /// Следующий AttackMove/Move от reальной игровой логики сбросит флаг обратно.
        /// </summary>
        internal void SuppressRetreatForStallBreaker()
        {
            // Подавляем новый ретрит (до следующей явной команды)
            _retreatSuppressedForBreaker = true;

            // Снимаем ещё идущий ретрит на случай, если OnCommandIssued уже не отработал.
            // Не вызываем StopInternal: если перед этим была IssueCommand(AttackMove),
            // Unit уже получил направление к цели — останавливать нельзя.
            if (CurrentCombatState == CombatState.Retreating)
            {
                CurrentCombatState = CombatState.None;
                SetCurrentTarget(null, null);
                // Движение к базе уже задано NavMesh-агентом; AttackMove-команда
                // перекроет маршрут при первом UpdateCombat → MoveToInternal вызове.
            }
        }

        // ----------------------------------------------------------------
        // Применение данных
        // ----------------------------------------------------------------

        private void ApplyData(UnitData data)
        {
            // MaxHp с модификатором технологий (только для новых юнитов при инициализации)
            float hpMult = 1f;
            var registry = TechRegistry.Instance;
            if (registry != null && _unit != null)
                hpMult = 1f + registry.GetMaxHpMultiplier(_unit.Faction, data);

            _health.Init(data.MaxHp * hpMult);
            _agent.speed = data.MoveSpeed;
        }

        // ----------------------------------------------------------------
        // Поиск цели
        // ----------------------------------------------------------------

        private void ScanForTarget()
        {
            if (_data == null) return;

            float scanRange = GetScanRange();
            if (scanRange <= 0f)
            {
                SetCurrentTarget(null, null);
                return;
            }

            Faction enemyFaction = _unit.Faction == Faction.Player ? Faction.Enemy : Faction.Player;

            // Собираем всех кандидатов в порядке, определяемом TargetPriority.
            // Buildings → сначала здания, потом юниты (танк ломает здания).
            // Units     → сначала юниты, потом здания (стандартный порядок).
            _candidateHealths.Clear();
            _candidatePositions.Clear();

            bool buildingsFirst = _data.TargetPriority == DiplomaGame.Runtime.Data.TargetPriority.Buildings;

            if (buildingsFirst)
            {
                AddBuildingCandidates(enemyFaction);
                AddUnitCandidates(enemyFaction);
            }
            else
            {
                AddUnitCandidates(enemyFaction);
                AddBuildingCandidates(enemyFaction);
            }

            int idx = CombatLogic.FindNearestTargetIndex(transform.position, _candidatePositions, scanRange);
            if (idx >= 0)
            {
                var targetHealth = _candidateHealths[idx];
                SetCurrentTarget(targetHealth, targetHealth.transform);
            }
            else
            {
                SetCurrentTarget(null, null);
            }
        }

        private void AddUnitCandidates(Faction enemyFaction)
        {
            UnitRegistry.GetUnits(enemyFaction, _unitBuffer);
            for (int i = 0; i < _unitBuffer.Count; i++)
            {
                var u = _unitBuffer[i];
                if (u == null) continue;
                var h = u.CachedHealth;
                if (h == null || h.IsDead) continue;
                _candidateHealths.Add(h);
                _candidatePositions.Add(u.transform.position);
            }
        }

        private void AddBuildingCandidates(Faction enemyFaction)
        {
            BuildingRegistry.GetBuildings(enemyFaction, _buildingBuffer);
            for (int i = 0; i < _buildingBuffer.Count; i++)
            {
                var b = _buildingBuffer[i];
                if (b == null) continue;
                var h = b.CachedHealth;
                if (h == null || h.IsDead) continue;
                _candidateHealths.Add(h);
                _candidatePositions.Add(b.transform.position);
            }
        }

        private float GetScanRange()
        {
            if (_data == null) return 0f;

            var cmdType = _unit.CurrentCommandType;

            // При AttackMove — сканируем на большое расстояние (aggro * 3 как «вдоль пути»)
            if (cmdType == UnitCommandType.AttackMove)
                return _data.AggroRadius * 3f;

            // При Hold — атакуем в радиусе агрессии, но не преследуем
            if (_unit.CurrentState == UnitState.Holding)
                return _data.AggroRadius;

            // При обычном Move — юнит игнорирует врагов
            if (cmdType == UnitCommandType.Move)
                return 0f;

            // После ретрита юнит обороняет зону базы: скан ×2 (24 ед. при aggro 12) —
            // видит подходящую волну раньше обычного, но НЕ контратакует через карту.
            // История: ×10 (map-wide) ставился ради сходимости синтетических Balance-боёв,
            // но на реальной карте отступившие юниты в одиночку маршировали к вражескому
            // HQ и сносили его (нарушение приказов и анти-микро-дизайна; вскрыто
            // SceneIntegration-тестом). Сходимость синтетики теперь обеспечивает
            // stall-breaker в тест-харнессе (AttackMove при затишье — модель командира).
            // Широкий скан действует не только в Idle — иначе дальняя цель сбрасывалась
            // обычным радиусом на следующем тике (осцилляция «шаг-стоп»).
            if (_retreatTriggered && CurrentCombatState != CombatState.Retreating)
                return _data.AggroRadius * 2f;

            // Idle, Patrol — стандартный aggro
            return _data.AggroRadius;
        }

        // ----------------------------------------------------------------
        // Обновление состояний боя
        // ----------------------------------------------------------------

        private void UpdateCombat()
        {
            if (_data == null) return;

            if (_currentTargetHealth == null || _currentTargetTransform == null)
            {
                // Нет цели — выходим в None
                if (CurrentCombatState != CombatState.None)
                    CurrentCombatState = CombatState.None;
                return;
            }

            // Цель умерла
            if (_currentTargetHealth.IsDead)
            {
                SetCurrentTarget(null, null);
                CurrentCombatState = CombatState.None;
                return;
            }

            // Эффективный радиус атаки: для зданий добавляем буфер
            float effectiveRange = _data.AttackRange + GetTargetRangeBuffer();

            bool inAttackRange = CombatLogic.IsInRange(
                transform.position, _currentTargetTransform.position, effectiveRange);

            if (inAttackRange)
            {
                if (CurrentCombatState != CombatState.Attacking)
                {
                    CurrentCombatState = CombatState.Attacking;
                    _unit.StopInternal();
                }
                TryAttack();
            }
            else
            {
                // Holding-юнит не преследует
                if (_unit.CurrentState == UnitState.Holding)
                {
                    CurrentCombatState = CombatState.None;
                    return;
                }

                // Преследуем цель
                if (CurrentCombatState != CombatState.Engaging)
                    CurrentCombatState = CombatState.Engaging;

                _unit.MoveToInternal(_currentTargetTransform.position);
            }
        }

        /// <summary>
        /// Дополнительный буфер к attackRange.
        /// Здания крупные — добавляем BuildingAttackBuffer.
        /// Использует кэшированный флаг _currentTargetIsBuilding — без GetComponent каждый кадр.
        /// </summary>
        private float GetTargetRangeBuffer()
        {
            if (_currentTargetTransform == null) return 0f;
            return _currentTargetIsBuilding ? BuildingAttackBuffer : 0f;
        }

        /// <summary>
        /// Устанавливает текущую цель и синхронно кэширует флаг "цель — здание".
        /// Все присвоения _currentTargetTransform должны идти через этот метод.
        /// </summary>
        private void SetCurrentTarget(Health health, Transform target)
        {
            _currentTargetHealth    = health;
            _currentTargetTransform = target;
            // Здания не имеют NavMeshAgent; юниты — обязательно имеют
            _currentTargetIsBuilding = target != null &&
                                       target.GetComponent<NavMeshAgent>() == null;
        }

        private void TryAttack()
        {
            if (_data == null || _currentTargetHealth == null) return;

            float effectiveCooldown = GetEffectiveCooldown();

            if (!FireRateLogic.CanFire(_lastAttackTime, effectiveCooldown, Time.time))
                return;

            if (_data.AoeRadius > 0f)
            {
                TryAttackAoe();
            }
            else
            {
                // Одиночная атака с модификатором урона
                float effectiveDamage = GetEffectiveDamage();
                _currentTargetHealth.TakeDamage(effectiveDamage);
                _lastAttackTime = Time.time;
                AnyAttacked?.Invoke(transform.position);
                AnyAttackedWithFaction?.Invoke(_unit.Faction, transform.position, (int)gameObject.GetEntityId());
                Attacked?.Invoke();

                // Если цель умерла — сбрасываем
                if (_currentTargetHealth.IsDead)
                {
                    SetCurrentTarget(null, null);
                    CurrentCombatState = CombatState.None;
                }
            }
        }

        /// <summary>Эффективный урон с учётом модификаторов технологий (null-safe).</summary>
        private float GetEffectiveDamage()
        {
            var registry = TechRegistry.Instance;
            if (registry == null || _unit == null) return _data.Damage;
            float mult = 1f + registry.GetDamageMultiplier(_unit.Faction, _data);
            return _data.Damage * mult;
        }

        /// <summary>Эффективный кулдаун с учётом модификаторов технологий (null-safe).</summary>
        private float GetEffectiveCooldown()
        {
            var registry = TechRegistry.Instance;
            if (registry == null || _unit == null) return _data.AttackCooldown;
            float mult = 1f + registry.GetCooldownMultiplier(_unit.Faction, _data);
            // Кулдаун не может быть меньше 0.05 (floor для читабельности)
            return Mathf.Max(0.05f, _data.AttackCooldown * mult);
        }

        /// <summary>
        /// AoE-ветка атаки: сплеш вокруг ТОЧКИ ПОПАДАНИЯ (позиции текущей цели, как в SC2),
        /// а не вокруг танка — иначе при AttackRange (5) > AoeRadius (3) выстрел с дистанции
        /// никогда не достал бы неподвижное здание. Бьёт юнитов И здания в радиусе.
        /// BuildingAttackBuffer не применяется — AoE площадная атака, не прицельная.
        /// </summary>
        private void TryAttackAoe()
        {
            // Центр сплеша — цель; без цели TryAttack сюда не доходит, но страхуемся
            Vector3 splashCenter = _currentTargetTransform != null
                ? _currentTargetTransform.position
                : transform.position;

            Faction enemyFaction = _unit.Faction == Faction.Player ? Faction.Enemy : Faction.Player;

            // Собираем позиции всех вражеских целей (юниты + здания) во временный список
            _candidateHealths.Clear();
            _candidatePositions.Clear();
            AddUnitCandidates(enemyFaction);
            AddBuildingCandidates(enemyFaction);

            CombatLogic.FindTargetsInRadius(
                splashCenter,
                _candidatePositions,
                _data.AoeRadius,
                _aoeIndexBuffer);

            // Основная цель гарантированно в сплеше (центр — она сама), но при
            // пустом списке кулдаун не сжигаем
            if (_aoeIndexBuffer.Count == 0) return;

            _lastAttackTime = Time.time;

            float effectiveDamage = GetEffectiveDamage();
            for (int i = 0; i < _aoeIndexBuffer.Count; i++)
            {
                var target = _candidateHealths[_aoeIndexBuffer[i]];
                if (target != null && !target.IsDead)
                    target.TakeDamage(effectiveDamage);
            }

            AnyAttacked?.Invoke(transform.position);
            AnyAttackedWithFaction?.Invoke(_unit.Faction, transform.position, (int)gameObject.GetEntityId());
            Attacked?.Invoke();

            if (_aoeVfx != null)
            {
                // Взрыв рисуем в точке сплеша (у цели), а не на танке
                _aoeVfx.transform.position = splashCenter;
                _aoeVfx.Play();
            }

            // Если основная цель погибла — сбрасываем
            if (_currentTargetHealth != null && _currentTargetHealth.IsDead)
            {
                SetCurrentTarget(null, null);
                CurrentCombatState = CombatState.None;
            }
        }

        // ----------------------------------------------------------------
        // Отступление
        // ----------------------------------------------------------------

        private bool ShouldStartRetreat()
        {
            if (_data == null) return false;
            // stall-breaker подавил ретрит на один раунд — пропускаем
            if (_retreatSuppressedForBreaker) return false;
            return CombatLogic.ShouldRetreat(_health.Fraction, _data.RetreatHpFraction, _data.RetreatDisabled);
        }

        private void StartRetreat()
        {
            _retreatTriggered  = true;
            _lastRetreatTime   = Time.time;
            CurrentCombatState = CombatState.Retreating;
            SetCurrentTarget(null, null);
            AnyRetreatStarted?.Invoke(_unit.Faction, transform.position);

            // Находим ближайшего врага (только юниты) для расчёта точки отступления
            Vector3 threatPos    = Vector3.zero;
            Faction enemyFaction = _unit.Faction == Faction.Player ? Faction.Enemy : Faction.Player;

            UnitRegistry.GetUnits(enemyFaction, _unitBuffer);
            _candidatePositions.Clear();
            for (int i = 0; i < _unitBuffer.Count; i++)
                _candidatePositions.Add(_unitBuffer[i].transform.position);

            int idx = CombatLogic.FindNearestTargetIndex(transform.position, _candidatePositions, float.MaxValue);
            if (idx >= 0)
                threatPos = _candidatePositions[idx];

            Vector3 retreatPoint = CombatLogic.GetRetreatPoint(transform.position, threatPos, _rallyPoint);
            _unit.MoveToInternal(retreatPoint);
        }

        private void UpdateRetreating(bool doScan)
        {
            // Стрельба на ходу во время отступления (без преследования, движение не прерываем)
            if (_data != null && _data.FireWhileRetreating)
            {
                if (doScan)
                    ScanForRetreatTarget();

                if (_currentTargetHealth != null && !_currentTargetHealth.IsDead)
                    TryAttack();
            }

            // Когда добрались до базы — переходим в None и сбрасываем цель
            if (_agent.pathPending) return;

            if (_agent.remainingDistance <= _agent.stoppingDistance + 0.1f)
            {
                SetCurrentTarget(null, null);
                CurrentCombatState = CombatState.None;
                _unit.StopInternal();
            }
        }

        /// <summary>
        /// Сканирует только врагов в AttackRange во время отступления.
        /// Не меняет направление движения (движение задаётся MoveToInternal в StartRetreat).
        /// </summary>
        private void ScanForRetreatTarget()
        {
            if (_data == null) return;

            float range = _data.AttackRange + GetTargetRangeBuffer();
            if (range <= 0f)
            {
                SetCurrentTarget(null, null);
                return;
            }

            Faction enemyFaction = _unit.Faction == Faction.Player ? Faction.Enemy : Faction.Player;

            _candidateHealths.Clear();
            _candidatePositions.Clear();

            // При отступлении пехота приоритет: Units (стандарт)
            AddUnitCandidates(enemyFaction);
            AddBuildingCandidates(enemyFaction);

            int idx = CombatLogic.FindNearestTargetIndex(transform.position, _candidatePositions, range);
            if (idx >= 0)
            {
                var targetHealth = _candidateHealths[idx];
                SetCurrentTarget(targetHealth, targetHealth.transform);
            }
            else
            {
                SetCurrentTarget(null, null);
            }
        }

        // ----------------------------------------------------------------
        // Обработчики событий
        // ----------------------------------------------------------------

        private void OnCommandIssued(UnitCommand cmd)
        {
            // Прямой приказ игрока сбрасывает отступление
            _retreatTriggered = false;
            // Сбрасываем stall-breaker подавление — следующий ретрит будет
            // разрешён если снова придёт обычная команда (не stall-breaker).
            // SuppressRetreatForStallBreaker вызывается ПОСЛЕ IssueCommand,
            // поэтому сброс здесь не мешает подавлению.
            _retreatSuppressedForBreaker = false;
            if (CurrentCombatState == CombatState.Retreating)
                CurrentCombatState = CombatState.None;
        }

        private void OnDied()
        {
            CurrentCombatState = CombatState.None;
            SetCurrentTarget(null, null);

            // Герой управляет своим жизненным циклом через GameWatcher (респаун).
            // _isHero кэшируется в Awake — без GetComponent при каждой смерти.
            if (_isHero) return;

            // Небольшая задержка перед уничтожением GO (для проигрывания эффектов)
            Destroy(gameObject, 0.1f);
        }

        // ----------------------------------------------------------------
        // Rally-точка (статический кэш — GameObject.Find только один раз за загрузку сцены)
        // ----------------------------------------------------------------

        // Кэшированные позиции маркеров спавна. Сцена с юнитами одна — инвалидация
        // не нужна: если маркер уничтожен или отсутствует, кэш хранит Vector3.zero (допустимо).
        private static bool    _rallyCacheValid;
        private static Vector3 _playerRallyCache;
        private static Vector3 _enemyRallyCache;

        private static Vector3 FindRallyPoint(Faction faction)
        {
            if (!_rallyCacheValid)
            {
                var playerGo = GameObject.Find("PlayerBaseSpawn");
                var enemyGo  = GameObject.Find("EnemyBaseSpawn");
                _playerRallyCache = playerGo != null ? playerGo.transform.position : Vector3.zero;
                _enemyRallyCache  = enemyGo  != null ? enemyGo.transform.position  : Vector3.zero;
                _rallyCacheValid  = true;
            }

            return faction == Faction.Player ? _playerRallyCache : _enemyRallyCache;
        }

        /// <summary>
        /// Сбрасывает статический кэш rally-точек. Вызывается при перезагрузке сцены или из тестов.
        /// </summary>
        internal static void InvalidateRallyCache()
        {
            _rallyCacheValid = false;
        }

        // Подписка на смену сцены — выполняется один раз при загрузке домена
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void RegisterSceneReloadCallback()
        {
            _rallyCacheValid = false;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            _rallyCacheValid = false;
        }
    }
}
