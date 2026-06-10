using System.Collections.Generic;
using DiplomaGame.Runtime.Combat;
using DiplomaGame.Runtime.Data;
using DiplomaGame.Runtime.Hero;
using UnityEngine;
using UnityEngine.AI;

namespace DiplomaGame.Runtime.Units
{
    /// <summary>
    /// Боевой ИИ юнита. Автоматически выбирает цели, атакует, преследует и отступает.
    /// Не дублирует движение — делегирует Unit.MoveToInternal / StopInternal.
    /// </summary>
    [RequireComponent(typeof(Unit))]
    [RequireComponent(typeof(Health))]
    public sealed class UnitCombat : MonoBehaviour
    {
        [SerializeField] private UnitData _data;

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

        // ----------------------------------------------------------------
        // Буферы без аллокаций
        // ----------------------------------------------------------------

        private readonly List<Unit>    _candidateUnits     = new List<Unit>(32);
        private readonly List<Vector3> _candidatePositions = new List<Vector3>(32);

        // ----------------------------------------------------------------
        // Внутреннее состояние
        // ----------------------------------------------------------------

        private Unit  _currentTarget;
        private float _scanTimer;
        private float _lastAttackTime;

        private const float ScanInterval = 0.25f;

        // Флаг: отступление уже было активировано (для одноразового срабатывания)
        private bool _retreatTriggered;

        // ----------------------------------------------------------------
        // Unity lifecycle
        // ----------------------------------------------------------------

        private void Awake()
        {
            _unit   = GetComponent<Unit>();
            _health = GetComponent<Health>();
            _agent  = GetComponent<NavMeshAgent>();

            // Подписка на события
            _unit.CommandIssued += OnCommandIssued;
            _health.Died        += OnDied;

            // Кэш rally-точки по фракции
            _rallyPoint = FindRallyPoint(_unit.Faction);

            // Применяем UnitData, если задан
            if (_data != null)
                ApplyData(_data);
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

            // Проверяем отступление первым делом
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
        /// </summary>
        internal void InitForTest(UnitData data)
        {
            _data = data;
            if (_health != null)
                ApplyData(data);
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

            // Определяем фракцию врагов
            Faction enemyFaction = _unit.Faction == Faction.Player ? Faction.Enemy : Faction.Player;

            // Заполняем буферы без аллокаций
            UnitRegistry.GetUnits(enemyFaction, _candidateUnits);
            _candidatePositions.Clear();
            for (int i = 0; i < _candidateUnits.Count; i++)
                _candidatePositions.Add(_candidateUnits[i].transform.position);

            // Радиус сканирования зависит от режима
            float scanRange = GetScanRange();

            int idx = CombatLogic.FindNearestTargetIndex(transform.position, _candidatePositions, scanRange);
            _currentTarget = idx >= 0 ? _candidateUnits[idx] : null;
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

            // Idle, Patrol — стандартный aggro
            return _data.AggroRadius;
        }

        // ----------------------------------------------------------------
        // Обновление состояний боя
        // ----------------------------------------------------------------

        private void UpdateCombat()
        {
            if (_data == null) return;

            if (_currentTarget == null || _currentTarget.gameObject == null)
            {
                // Нет цели — выходим в None
                if (CurrentCombatState != CombatState.None)
                    CurrentCombatState = CombatState.None;
                return;
            }

            // Цель умерла
            var targetHealth = _currentTarget.GetComponent<Health>();
            if (targetHealth != null && targetHealth.IsDead)
            {
                _currentTarget     = null;
                CurrentCombatState = CombatState.None;
                return;
            }

            bool inAttackRange = CombatLogic.IsInRange(
                transform.position, _currentTarget.transform.position, _data.AttackRange);

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

                _unit.MoveToInternal(_currentTarget.transform.position);
            }
        }

        private void TryAttack()
        {
            if (_data == null || _currentTarget == null) return;

            if (!FireRateLogic.CanFire(_lastAttackTime, _data.AttackCooldown, Time.time))
                return;

            var damageable = _currentTarget.GetComponent<IDamageable>();
            if (damageable == null) return;

            damageable.TakeDamage(_data.Damage);
            _lastAttackTime = Time.time;

            // Если цель умерла — сбрасываем
            var targetHealth = _currentTarget.GetComponent<Health>();
            if (targetHealth != null && targetHealth.IsDead)
            {
                _currentTarget     = null;
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
            CurrentCombatState = CombatState.Retreating;
            _currentTarget     = null;

            // Находим ближайшего врага для расчёта точки отступления
            Vector3 threatPos    = Vector3.zero;
            Faction enemyFaction = _unit.Faction == Faction.Player ? Faction.Enemy : Faction.Player;

            UnitRegistry.GetUnits(enemyFaction, _candidateUnits);
            _candidatePositions.Clear();
            for (int i = 0; i < _candidateUnits.Count; i++)
                _candidatePositions.Add(_candidateUnits[i].transform.position);

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
            // Небольшая задержка перед уничтожением GO (для проигрывания эффектов)
            Destroy(gameObject, 0.1f);
        }

        // ----------------------------------------------------------------
        // Rally-точка
        // ----------------------------------------------------------------

        private static Vector3 FindRallyPoint(Faction faction)
        {
            string markerName = faction == Faction.Player ? "PlayerBaseSpawn" : "EnemyBaseSpawn";
            var    go         = GameObject.Find(markerName);
            return go != null ? go.transform.position : Vector3.zero;
        }
    }
}
