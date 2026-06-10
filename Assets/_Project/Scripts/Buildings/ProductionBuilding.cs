using System;
using System.Collections.Generic;
using DiplomaGame.Runtime.Data;
using DiplomaGame.Runtime.Economy;
using DiplomaGame.Runtime.Units;
using UnityEngine;

namespace DiplomaGame.Runtime.Buildings
{
    /// <summary>
    /// Компонент производства юнитов (для Barracks).
    /// Управляет очередью и спавном. Rally-point задаётся вручную или вычисляется по умолчанию.
    /// </summary>
    [RequireComponent(typeof(Building))]
    public sealed class ProductionBuilding : MonoBehaviour
    {
        [SerializeField] private GameObject _unitPrefab;

        private const int MaxQueueSize = 5;

        // ----------------------------------------------------------------
        // Публичный API
        // ----------------------------------------------------------------

        /// <summary>Количество юнитов в очереди (включая производимого).</summary>
        public int QueueCount => _queue.Count;

        /// <summary>Прогресс производства текущего юнита (0..1). 0 если очередь пуста.</summary>
        public float CurrentProgress01
        {
            get
            {
                if (_queue.Count == 0) return 0f;
                var building = GetBuilding();
                if (building == null || building.Data == null) return 0f;
                float total = building.Data.ProductionTime;
                return total > 0f ? Mathf.Clamp01(_progress / total) : 0f;
            }
        }

        /// <summary>
        /// Точка назначения для только что произведённых юнитов.
        /// По умолчанию — позиция здания + forward*4.
        /// </summary>
        public Vector3 RallyPoint
        {
            get => _rallyPointSet ? _rallyPoint : transform.position + transform.forward * 4f;
            private set
            {
                _rallyPoint    = value;
                _rallyPointSet = true;
            }
        }

        /// <summary>Вызывается при выходе готового юнита.</summary>
        public event Action<Unit> UnitProduced;

        // ----------------------------------------------------------------
        // Кэшированные ссылки и состояние
        // ----------------------------------------------------------------

        private Building     _building;
        private ResourceBank _bank;

        // Очередь хранит UnitData (тип производимого юнита) или маркер-null для зданий без data
        private readonly Queue<UnitData> _queue = new Queue<UnitData>(MaxQueueSize);

        private float   _progress;
        private Vector3 _rallyPoint;
        private bool    _rallyPointSet;

        // ----------------------------------------------------------------
        // Unity lifecycle
        // ----------------------------------------------------------------

        private void Awake()
        {
            _building = GetComponent<Building>();
        }

        private void Start()
        {
            if (_bank == null)
                _bank = UnityEngine.Object.FindFirstObjectByType<ResourceBank>();
        }

        private void Update()
        {
            if (_queue.Count == 0) return;

            var building = GetBuilding();
            if (building == null || building.Data == null) return;

            _progress = ProductionQueueLogic.TickProgress(_progress, Time.deltaTime);

            if (ProductionQueueLogic.IsComplete(_progress, building.Data.ProductionTime))
            {
                _queue.Dequeue();
                _progress = 0f;
                SpawnUnit(building);
            }
        }

        // ----------------------------------------------------------------
        // Публичный API
        // ----------------------------------------------------------------

        /// <summary>
        /// Добавляет юнита в очередь, если есть место и средства.
        /// Возвращает true при успехе.
        /// </summary>
        public bool TryEnqueue()
        {
            if (_queue.Count >= MaxQueueSize) return false;

            var building = GetBuilding();
            if (building == null || building.Data == null) return false;

            var bank = GetBank();
            if (bank == null) return false;

            if (!bank.TrySpend(building.Faction, building.Data.ProductionCost))
                return false;

            // Помещаем UnitData в очередь (может быть null — тогда спавн по _unitPrefab без data)
            _queue.Enqueue(building.Data.Produces);
            return true;
        }

        /// <summary>Задаёт rally-point для выходящих юнитов.</summary>
        public void SetRallyPoint(Vector3 point)
        {
            RallyPoint = point;
        }

        // ----------------------------------------------------------------
        // Internal — для тестов
        // ----------------------------------------------------------------

        /// <summary>
        /// Инициализирует компонент для PlayMode-тестов без SerializedObject.
        /// templateUnit — неактивный GO, используемый как шаблон для Instantiate.
        /// </summary>
        internal void InitForTest(GameObject unitPrefab, ResourceBank bank)
        {
            _unitPrefab = unitPrefab;
            _bank       = bank;
        }

        // ----------------------------------------------------------------
        // Приватные методы
        // ----------------------------------------------------------------

        private void SpawnUnit(Building building)
        {
            if (_unitPrefab == null) return;

            Vector3 spawnPos = transform.position + transform.forward * 1.5f;
            var     go       = Instantiate(_unitPrefab, spawnPos, Quaternion.identity);
            go.SetActive(true);

            var unit = go.GetComponent<Unit>();
            if (unit != null)
            {
                unit.IssueCommand(UnitCommand.Move(RallyPoint));
                UnitProduced?.Invoke(unit);
            }
        }

        private Building GetBuilding()
        {
            if (_building == null)
                _building = GetComponent<Building>();
            return _building;
        }

        private ResourceBank GetBank()
        {
            if (_bank == null)
                _bank = UnityEngine.Object.FindFirstObjectByType<ResourceBank>();
            return _bank;
        }
    }
}
