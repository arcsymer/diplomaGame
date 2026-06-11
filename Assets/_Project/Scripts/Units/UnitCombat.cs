using System;
using System.Collections.Generic;
using DiplomaGame.Runtime.Buildings;
using DiplomaGame.Runtime.Combat;
using DiplomaGame.Runtime.Data;
using DiplomaGame.Runtime.Hero;
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

        [SerializeField] private UnitData _data;

        // Дополнительный буфер к attackRange при атаке здания (здания крупные).
        private const float BuildingAttackBuffer = 2f;

        // ----------------------------------------------------------------
        // Публичный API для тестов и HUD
        // ----------------------------------------------------------------

        /// <summary>Текущее состояние боевого ИИ.</summary>
        public CombatState CurrentCombatState { get; private set; } = CombatState.None;

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
            // Используем GetEntityId() как seed — уникален, нет аллокаций.
            // Сдвиг на -AttackCooldown: первый выстрел возможен ровно через offset секунд,
            // иначе CanFire отложил бы его на полный кулдаун + offset.
            if (_data != null)
            {
                float offset = CombatLogic.RandomiseInitialCooldownOffset(
                    _data.AttackCooldown, (int)gameObject.GetEntityId());
                _lastAttackTime = Time.time - _data.AttackCooldown + offset;

                // Фаза сканирования тоже рандомизируется: иначе все юниты сканируют
                // в один кадр, и порядок Update решает, кто захватит цель первым.
                _scanTimer = CombatLogic.RandomiseInitialCooldownOffset(
                    ScanInterval, (int)gameObject.GetEntityId() * 31);
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
                UpdateRetreating();
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
            float offset = CombatLogic.RandomiseInitialCooldownOffset(
                data.AttackCooldown, (int)gameObject.GetEntityId());
            _lastAttackTime = Time.time - data.AttackCooldown + offset;
            _scanTimer = CombatLogic.RandomiseInitialCooldownOffset(
                ScanInterval, (int)gameObject.GetEntityId() * 31);
        }

        // ----------------------------------------------------------------
        // Применение данных
        // ----------------------------------------------------------------

        private void ApplyData(UnitData data)
        {
            _health.Init(data.MaxHp);
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

            // После ретрита юнит вернулся на базу в Idle.
            // Чтобы бой не замирал — сканируем на AggroRadius * 10 (120 ед. при базовом
            // aggro 12 — больше диагонали между базами ~85). При ×5 (60) ловился стейлмейт:
            // обе стороны целиком отступали на свои базы и не видели друг друга.
            if (_retreatTriggered && _unit.CurrentState == UnitState.Idle)
                return _data.AggroRadius * 10f;

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

            if (!FireRateLogic.CanFire(_lastAttackTime, _data.AttackCooldown, Time.time))
                return;

            if (_data.AoeRadius > 0f)
            {
                TryAttackAoe();
            }
            else
            {
                // Одиночная атака — старый путь без изменений
                _currentTargetHealth.TakeDamage(_data.Damage);
                _lastAttackTime = Time.time;
                AnyAttacked?.Invoke(transform.position);

                // Если цель умерла — сбрасываем
                if (_currentTargetHealth.IsDead)
                {
                    SetCurrentTarget(null, null);
                    CurrentCombatState = CombatState.None;
                }
            }
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

            for (int i = 0; i < _aoeIndexBuffer.Count; i++)
            {
                var target = _candidateHealths[_aoeIndexBuffer[i]];
                if (target != null && !target.IsDead)
                    target.TakeDamage(_data.Damage);
            }

            AnyAttacked?.Invoke(transform.position);

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
            return CombatLogic.ShouldRetreat(_health.Fraction, _data.RetreatHpFraction, _data.RetreatDisabled);
        }

        private void StartRetreat()
        {
            _retreatTriggered  = true;
            _lastRetreatTime   = Time.time;
            CurrentCombatState = CombatState.Retreating;
            SetCurrentTarget(null, null);

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

        private void UpdateRetreating()
        {
            // Когда добрались до базы — переходим в None
            if (_agent.pathPending) return;

            if (_agent.remainingDistance <= _agent.stoppingDistance + 0.1f)
            {
                CurrentCombatState = CombatState.None;
                _unit.StopInternal();
            }
        }

        // ----------------------------------------------------------------
        // Обработчики событий
        // ----------------------------------------------------------------

        private void OnCommandIssued(UnitCommand cmd)
        {
            // Прямой приказ игрока сбрасывает отступление
            _retreatTriggered = false;
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
