using System;
using System.Collections.Generic;
using DiplomaGame.Runtime.Data;
using DiplomaGame.Runtime.Economy;
using DiplomaGame.Runtime.Tech;
using DiplomaGame.Runtime.Units;
using UnityEngine;

namespace DiplomaGame.Runtime.Buildings
{
    /// <summary>
    /// Компонент производства юнитов (для Barracks / WarFactory).
    /// Поддерживает multi-production (список ProductionEntry) и legacy-режим (_produces/_productionCost/_productionTime).
    /// Rally-point задаётся вручную или вычисляется по умолчанию.
    /// </summary>
    [RequireComponent(typeof(Building))]
    public sealed class ProductionBuilding : MonoBehaviour
    {
        // ----------------------------------------------------------------
        // Маппинг UnitData → префаб (multi-production)
        // ----------------------------------------------------------------

        [System.Serializable]
        public struct UnitPrefabEntry
        {
            public UnitData   unitData;
            public GameObject prefab;
        }

        [SerializeField] private GameObject      _unitPrefab;   // legacy fallback
        [SerializeField] private UnitPrefabEntry[] _unitPrefabs = new UnitPrefabEntry[0];

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
                float total = _currentEntryProductionTime;
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
        public event Action<Unit, ProductionEntry> UnitProduced;

        // ----------------------------------------------------------------
        // Кэшированные ссылки и состояние
        // ----------------------------------------------------------------

        private Building     _building;
        private ResourceBank _bank;

        private readonly Queue<ProductionEntry> _queue = new Queue<ProductionEntry>(MaxQueueSize);

        // Время производства текущего первого элемента очереди (чтобы не читать Data каждый кадр)
        private float _currentEntryProductionTime;

        private float   _progress;
        private Vector3 _rallyPoint;
        private bool    _rallyPointSet;

        // Снимок очереди для UI (обновляется при каждом изменении очереди)
        private ProductionEntry[] _queueSnapshot = new ProductionEntry[MaxQueueSize];
        private int               _queueSnapshotCount;

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

            _progress = ProductionQueueLogic.TickProgress(_progress, Time.deltaTime);

            if (ProductionQueueLogic.IsComplete(_progress, _currentEntryProductionTime))
            {
                var entry = _queue.Dequeue();
                _progress = 0f;
                UpdateSnapshot();
                UpdateCurrentEntryTime();
                SpawnUnit(entry);
            }
        }

        // ----------------------------------------------------------------
        // Публичный API
        // ----------------------------------------------------------------

        /// <summary>
        /// Legacy TryEnqueue: добавляет юнита по данным из BuildingData.
        /// Если HasMultiProduction — использует первую запись из таблицы (entries[0]).
        /// Иначе — синтетическая запись из legacy-полей _produces/_productionCost/_productionTime.
        /// </summary>
        public bool TryEnqueue()
        {
            var building = GetBuilding();
            if (building == null || building.Data == null) return false;

            if (building.Data.HasMultiProduction)
            {
                // Fallback на первую запись таблицы
                return TryEnqueue(building.Data.ProductionEntries[0]);
            }

            // Legacy: синтетическая запись
            var entry = new ProductionEntry
            {
                unitData       = building.Data.Produces,
                cost           = building.Data.ProductionCost,
                productionTime = building.Data.ProductionTime,
                icon           = null,
                hotkeyLabel    = "T",
            };
            return TryEnqueueInternal(entry);
        }

        /// <summary>
        /// Multi-production TryEnqueue: добавляет запись из таблицы производства.
        /// Списывает entry.cost с баланса фракции здания.
        /// Возвращает false, если очередь заполнена или не хватает ресурсов.
        /// </summary>
        public bool TryEnqueue(ProductionEntry entry)
        {
            if (entry == null) return false;
            return TryEnqueueInternal(entry);
        }

        /// <summary>Задаёт rally-point для выходящих юнитов.</summary>
        public void SetRallyPoint(Vector3 point)
        {
            RallyPoint = point;
        }

        /// <summary>
        /// Возвращает элемент снимка очереди по индексу без аллокаций.
        /// Индекс 0 — текущий (производится), 1..N-1 — ожидающие.
        /// Возвращает null, если индекс вне диапазона.
        /// </summary>
        public ProductionEntry PeekEntryAt(int index)
        {
            if (index < 0 || index >= _queueSnapshotCount) return null;
            return _queueSnapshot[index];
        }

        // ----------------------------------------------------------------
        // Internal — для тестов
        // ----------------------------------------------------------------

        /// <summary>
        /// Инициализирует компонент для PlayMode-тестов без SerializedObject.
        /// entries — опциональный список записей производства (для multi-production).
        /// </summary>
        internal void InitForTest(GameObject unitPrefab, ResourceBank bank, ProductionEntry[] entries = null)
        {
            _unitPrefab = unitPrefab;
            _bank       = bank;

            // Если переданы entries — проставляем их через _building.Data (обновлять через BuildingData
            // нельзя без SerializedObject в тестах, поэтому игнорируем; тесты, которым нужны entries,
            // должны проставить BuildingData.CreateForTest с productionEntries)
            _ = entries; // reserved for direct test use via BuildingData.CreateForTest
        }

        // ----------------------------------------------------------------
        // Приватные методы
        // ----------------------------------------------------------------

        private bool TryEnqueueInternal(ProductionEntry entry)
        {
            if (_queue.Count >= MaxQueueSize) return false;

            var building = GetBuilding();
            if (building == null) return false;

            var bank = GetBank();
            if (bank == null) return false;

            if (!bank.TrySpend(building.Faction, entry.cost))
                return false;

            _queue.Enqueue(entry);
            UpdateSnapshot();

            // Если это первый элемент — обновляем время текущего производства
            if (_queue.Count == 1)
                UpdateCurrentEntryTime();

            return true;
        }

        private void UpdateCurrentEntryTime()
        {
            if (_queue.Count == 0)
            {
                _currentEntryProductionTime = 0f;
                return;
            }

            // Queue не предоставляет Peek без аллокаций напрямую — используем снимок
            var first = _queueSnapshotCount > 0 ? _queueSnapshot[0] : null;
            if (first != null)
            {
                // productionTime == 0 → fallback на legacy BuildingData.ProductionTime
                float t = first.productionTime;
                if (t <= 0f)
                {
                    var building = GetBuilding();
                    t = building != null && building.Data != null ? building.Data.ProductionTime : 0f;
                }
                _currentEntryProductionTime = t;
            }
            else
            {
                var building = GetBuilding();
                _currentEntryProductionTime = building != null && building.Data != null
                    ? building.Data.ProductionTime
                    : 0f;
            }
        }

        private void UpdateSnapshot()
        {
            _queueSnapshotCount = 0;
            foreach (var e in _queue)
            {
                if (_queueSnapshotCount >= _queueSnapshot.Length) break;
                _queueSnapshot[_queueSnapshotCount++] = e;
            }
        }

        private void SpawnUnit(ProductionEntry entry)
        {
            // Guard: запись не может быть одновременно без юнита и без технологии
            if (entry == null) return;

            // Ветка исследования технологии
            if (entry.techData != null)
            {
                var buildingForTech = GetBuilding();
                if (buildingForTech != null && TechRegistry.Instance != null)
                    TechRegistry.Instance.MarkResearched(buildingForTech.Faction, entry.techData);
                // UnitProduced НЕ вызывается — юнит не создан
                return;
            }

            // Обычный спавн юнита
            var prefab = ResolvePrefab(entry);
            if (prefab == null) return;

            Vector3 spawnPos = transform.position + transform.forward * 1.5f;
            var     go       = Instantiate(prefab, spawnPos, Quaternion.identity);
            go.SetActive(true);

            var unit = go.GetComponent<Unit>();
            if (unit != null)
            {
                Vector3 offset = UnitCommandLogic.GetFormationOffset(_spawnCounter++);
                unit.IssueCommand(UnitCommand.Move(RallyPoint + offset));
                UnitProduced?.Invoke(unit, entry);
            }
        }

        /// <summary>
        /// Resolves the prefab for a ProductionEntry.
        /// Priority: _unitPrefabs mapping by UnitData, then _unitPrefab legacy fallback.
        /// </summary>
        private GameObject ResolvePrefab(ProductionEntry entry)
        {
            if (entry != null && entry.unitData != null && _unitPrefabs != null)
            {
                for (int i = 0; i < _unitPrefabs.Length; i++)
                {
                    if (_unitPrefabs[i].unitData == entry.unitData)
                        return _unitPrefabs[i].prefab;
                }
            }

            return _unitPrefab;
        }

        private int _spawnCounter;

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
