using System;
using System.Collections.Generic;
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
        /// Кэшированный UnitCombat-компонент (GetComponent в Awake). Используется EnemyCommander
        /// и AudioManager для проверки боевого состояния без аллокаций в горячих путях.
        /// </summary>
        public UnitCombat CachedCombat
        {
            // Ленивый кэш: в тестах UnitCombat может добавляться после Unit.Awake
            get
            {
                if (_cachedCombat == null) _cachedCombat = GetComponent<UnitCombat>();
                return _cachedCombat;
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
        private UnitCombat   _cachedCombat;

        // ----------------------------------------------------------------
        // Очередь приказов (Shift+ПКМ waypoint queue, М15)
        // ----------------------------------------------------------------

        // Предаллоцирована в Awake: нет аллокаций в Update-пути.
        // Queue<T>.Dequeue/Enqueue не аллоцирует пока Count < Capacity.
        private Queue<UnitCommand> _orderQueue;

        // Начальная ёмкость достаточна для разумного числа точек (SC2-уровень).
        private const int OrderQueueInitialCapacity = 8;

        /// <summary>
        /// Количество приказов, ожидающих в очереди.
        /// Читается тестами через InternalsVisibleTo.
        /// </summary>
        internal int OrderQueueCount => _orderQueue != null ? _orderQueue.Count : 0;

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

            // Предаллоцируем очередь один раз — нет аллокаций при Enqueue/Dequeue в Update-пути.
            _orderQueue = new Queue<UnitCommand>(OrderQueueInitialCapacity);

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
                    _stuckTime = 0f;
                    AdvanceOrderQueue();
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
                        _stuckTime = 0f;
                        AdvanceOrderQueue();
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
        /// Выдаёт юниту немедленный приказ. Очищает очередь и выполняет команду прямо сейчас.
        /// Это исходное поведение; используется без Shift или для Hold/Patrol.
        /// </summary>
        public void IssueCommand(UnitCommand cmd)
        {
            // Немедленный приказ обнуляет всю накопленную очередь.
            OrderQueueLogic.Clear(_orderQueue);

            ExecuteCommand(cmd);
        }

        /// <summary>
        /// Добавляет приказ в хвост очереди (Shift+ПКМ waypoint queue, М15).
        /// Если тип не поддерживает очередь (Hold, Patrol) — выполняется немедленно
        /// через IssueCommand (очередь очищается).
        /// Если очередь пуста и у юнита нет активного движения — выполняется немедленно.
        /// </summary>
        public void EnqueueCommand(UnitCommand cmd)
        {
            if (!OrderQueueLogic.CanEnqueue(cmd.Type))
            {
                // Hold и Patrol не ставятся в очередь — выполнить немедленно,
                // сбросив накопленные точки.
                IssueCommand(cmd);
                return;
            }

            // Если юнит прямо сейчас не движется по приказу — начинаем немедленно
            // (первый waypoint запускает движение; последующие Shift-клики добавляются в очередь).
            if (_state != UnitState.Moving)
            {
                OrderQueueLogic.Clear(_orderQueue);
                ExecuteCommand(cmd);
                return;
            }

            // Юнит в движении — добавляем в очередь на потом.
            OrderQueueLogic.Enqueue(_orderQueue, cmd);
        }

        // ----------------------------------------------------------------
        // Внутренняя логика приказов (без очистки очереди)
        // ----------------------------------------------------------------

        /// <summary>
        /// Применяет приказ к NavMeshAgent и обновляет состояние.
        /// Не трогает очередь — это делают IssueCommand и EnqueueCommand.
        /// </summary>
        private void ExecuteCommand(UnitCommand cmd)
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

        /// <summary>
        /// Вызывается при завершении текущего приказа движения (прибытие или анти-застревание).
        /// Берёт следующий из очереди или переходит в Idle.
        /// </summary>
        private void AdvanceOrderQueue()
        {
            if (OrderQueueLogic.TryDequeueNext(_orderQueue, out UnitCommand next))
            {
                // Выполняем следующий приказ напрямую через ExecuteCommand —
                // очередь уже уменьшилась на один элемент (Dequeue внутри TryDequeueNext).
                ExecuteCommand(next);
            }
            else
            {
                _state = UnitState.Idle;
            }
        }

        // ----------------------------------------------------------------
        // Internal — InitForTest (паттерн проекта)
        // ----------------------------------------------------------------

        /// <summary>
        /// Инициализирует очередь приказов в тестовой среде без NavMeshAgent/NavMesh.
        /// Позволяет тестировать логику очереди в EditMode.
        /// ТОЛЬКО для тестов; не вызывать из production-кода.
        /// </summary>
        internal void InitForTest()
        {
            if (_orderQueue == null)
                _orderQueue = new Queue<UnitCommand>(OrderQueueInitialCapacity);
        }

        /// <summary>
        /// Форсированно переводит юнита в состояние Moving (для тестов очереди).
        /// Позволяет протестировать поведение EnqueueCommand при _state == Moving
        /// без реального NavMeshAgent.
        /// ТОЛЬКО для тестов.
        /// </summary>
        internal void SetMovingForTest()
        {
            _state = UnitState.Moving;
        }

        /// <summary>
        /// Эмулирует прибытие юнита (вызывает AdvanceOrderQueue напрямую).
        /// Позволяет тестировать продвижение очереди в EditMode без NavMesh/Time.deltaTime.
        /// ТОЛЬКО для тестов.
        /// </summary>
        internal void SimulateArrivalForTest()
        {
            _stuckTime = 0f;
            AdvanceOrderQueue();
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
