using System;
using DiplomaGame.Runtime.Combat;
using UnityEngine;
using UnityEngine.AI;

namespace DiplomaGame.Runtime.Units
{
    /// <summary>
    /// Базовый компонент юнита. Принимает приказы, управляет NavMeshAgent и анимирует
    /// кольцо выделения. Кэширует все компоненты в Awake — нет аллокаций в Update.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public sealed class Unit : MonoBehaviour
    {
        [SerializeField] private Faction _faction;

        // ----------------------------------------------------------------
        // Публичный API
        // ----------------------------------------------------------------

        public Faction   Faction      => _faction;
        public UnitState CurrentState => _state;

        /// <summary>
        /// Кэшированный Health-компонент (GetComponent в Awake). Используется UnitCombat
        /// для работы с целями без аллокаций.
        /// </summary>
        public Health CachedHealth
        {
            // Ленивый кэш: в тестах Health может добавляться на GO после Unit.Awake
            get
            {
                if (_cachedHealth == null) _cachedHealth = GetComponent<Health>();
                return _cachedHealth;
            }
        }

        /// <summary>
        /// Тип последнего приказа игрока. null — если приказов ещё не было.
        /// </summary>
        public UnitCommandType? CurrentCommandType { get; private set; }

        /// <summary>
        /// Вызывается при каждом IssueCommand от игрока.
        /// UnitCombat использует это для сброса Retreating.
        /// </summary>
        public event Action<UnitCommand> CommandIssued;

        // ----------------------------------------------------------------
        // Кэшированные ссылки (Awake)
        // ----------------------------------------------------------------

        private NavMeshAgent _agent;
        private GameObject   _selectionRing;
        private Health       _cachedHealth;

        // ----------------------------------------------------------------
        // Состояние патруля
        // ----------------------------------------------------------------

        private Vector3 _patrolPointA;
        private Vector3 _patrolPointB;

        // ----------------------------------------------------------------
        // Внутреннее состояние
        // ----------------------------------------------------------------

        private UnitState _state = UnitState.Idle;

        // ----------------------------------------------------------------
        // Unity lifecycle
        // ----------------------------------------------------------------

        private void Awake()
        {
            _agent        = GetComponent<NavMeshAgent>();
            _cachedHealth = GetComponent<Health>();

            // Ищем кольцо выделения — null-безопасно
            var ring = transform.Find("SelectionRing");
            if (ring != null)
                _selectionRing = ring.gameObject;
        }

        private void OnEnable()
        {
            UnitRegistry.Register(this);
        }

        private void OnDisable()
        {
            UnitRegistry.Unregister(this);
        }

        private void Update()
        {
            if (_state == UnitState.Patrolling)
            {
                if (UnitCommandLogic.HasArrived(_agent.remainingDistance, _agent.stoppingDistance, _agent.pathPending))
                {
                    Vector3 next = UnitCommandLogic.GetNextPatrolPoint(transform.position, _patrolPointA, _patrolPointB);
                    _agent.SetDestination(next);
                }
            }
            else if (_state == UnitState.Moving)
            {
                if (UnitCommandLogic.HasArrived(_agent.remainingDistance, _agent.stoppingDistance, _agent.pathPending))
                {
                    _state     = UnitState.Idle;
                    _stuckTime = 0f;
                }
                else if (!_agent.pathPending && _agent.velocity.sqrMagnitude < StuckVelocitySqr)
                {
                    // Анти-застревание: толпа агентов у общей точки не даёт достичь
                    // stoppingDistance — юнит стоит на месте, но формально «едет».
                    // Считаем его прибывшим, чтобы боевой ИИ видел юнита свободным.
                    _stuckTime += Time.deltaTime;
                    if (_stuckTime >= StuckTimeout)
                    {
                        _agent.ResetPath();
                        _state     = UnitState.Idle;
                        _stuckTime = 0f;
                    }
                }
                else
                {
                    _stuckTime = 0f;
                }
            }
        }

        private const float StuckVelocitySqr = 0.01f;
        private const float StuckTimeout     = 1.5f;
        private float _stuckTime;

        // ----------------------------------------------------------------
        // Приказы
        // ----------------------------------------------------------------

        /// <summary>
        /// Выдаёт юниту приказ. Меняет состояние и перенаправляет NavMeshAgent.
        /// </summary>
        public void IssueCommand(UnitCommand cmd)
        {
            CurrentCommandType = cmd.Type;

            switch (cmd.Type)
            {
                case UnitCommandType.Move:
                case UnitCommandType.AttackMove:
                    _agent.SetDestination(cmd.TargetPoint);
                    _state = UnitState.Moving;
                    break;

                case UnitCommandType.Hold:
                    _agent.ResetPath();
                    _state = UnitState.Holding;
                    break;

                case UnitCommandType.Patrol:
                    _patrolPointA = transform.position;
                    _patrolPointB = cmd.TargetPoint;
                    _agent.SetDestination(_patrolPointB);
                    _state = UnitState.Patrolling;
                    break;
            }

            CommandIssued?.Invoke(cmd);
        }

        // ----------------------------------------------------------------
        // Internal — для UnitCombat (не сбрасывают CurrentCommandType игрока)
        // ----------------------------------------------------------------

        /// <summary>
        /// Перемещает юнита к точке без изменения CurrentCommandType.
        /// Используется UnitCombat для преследования/отступления.
        /// </summary>
        internal void MoveToInternal(Vector3 destination)
        {
            _agent.SetDestination(destination);
            _state = UnitState.Moving;
        }

        /// <summary>
        /// Останавливает юнита без изменения CurrentCommandType.
        /// Используется UnitCombat при атаке на месте.
        /// </summary>
        internal void StopInternal()
        {
            _agent.ResetPath();
            _state = UnitState.Idle;
        }

        // ----------------------------------------------------------------
        // Выделение
        // ----------------------------------------------------------------

        /// <summary>Показывает или скрывает кольцо выделения под юнитом.</summary>
        public void SetSelected(bool selected)
        {
            if (_selectionRing != null)
                _selectionRing.SetActive(selected);
        }
    }
}
