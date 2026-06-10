using DiplomaGame.Runtime.Combat;
using DiplomaGame.Runtime.Data;
using DiplomaGame.Runtime.Economy;
using DiplomaGame.Runtime.Units;
using UnityEngine;

namespace DiplomaGame.Runtime.Buildings
{
    /// <summary>
    /// Базовый компонент здания. Управляет здоровьем, регистрацией в реестре
    /// и пассивным доходом (HQ — прямой, Extractor — через ResourceNode).
    /// </summary>
    [RequireComponent(typeof(Health))]
    public sealed class Building : MonoBehaviour
    {
        [SerializeField] private BuildingData  _data;
        [SerializeField] private ResourceBank  _bank;

        // Фракция задаётся через InitForTest или SerializedObject на сцен-инстансе
        [SerializeField] private Faction _faction;

        // ----------------------------------------------------------------
        // Публичный API
        // ----------------------------------------------------------------

        public BuildingData Data    => _data;
        public Faction      Faction => _faction;

        // ----------------------------------------------------------------
        // Кэшированные ссылки
        // ----------------------------------------------------------------

        private Health       _health;
        private ResourceNode _nearestNode; // только для Extractor

        // ----------------------------------------------------------------
        // Таймер дохода
        // ----------------------------------------------------------------

        private float _incomeAccumulator;

        // ----------------------------------------------------------------
        // Unity lifecycle
        // ----------------------------------------------------------------

        private void Awake()
        {
            _health = GetComponent<Health>();

            if (_data != null)
                _health.Init(_data.MaxHp);

            _health.Died += OnDied;
        }

        private void Start()
        {
            // Кэшируем ResourceBank (ищем только если не проставлен в Inspector)
            if (_bank == null)
                _bank = Object.FindFirstObjectByType<ResourceBank>();

            // Кэшируем ближайший ResourceNode для Extractor
            if (_data != null && _data.BuildingType == BuildingType.Extractor)
                _nearestNode = FindNearestNode(transform.position, 5f);
        }

        private void OnEnable()
        {
            BuildingRegistry.Register(this);
        }

        private void OnDisable()
        {
            BuildingRegistry.Unregister(this);
        }

        private void Update()
        {
            if (_data == null || _bank == null)             return;
            if (_data.IncomePerTick <= 0)                   return;
            if (_data.IncomeTickInterval <= 0f)             return;

            _incomeAccumulator += Time.deltaTime;

            int ticks = EconomyLogic.CalculateIncomeTicks(
                _incomeAccumulator,
                _data.IncomeTickInterval,
                out float remainder);

            if (ticks <= 0) return;

            _incomeAccumulator = remainder;

            for (int i = 0; i < ticks; i++)
                ApplyIncomeTick();
        }

        private void OnDestroy()
        {
            if (_health != null)
                _health.Died -= OnDied;
        }

        // ----------------------------------------------------------------
        // Internal — для тестов
        // ----------------------------------------------------------------

        /// <summary>
        /// Инициализирует здание без SerializedObject (доступно в PlayMode-тестах).
        /// </summary>
        internal void InitForTest(BuildingData data, Faction faction, ResourceBank bank)
        {
            _data    = data;
            _faction = faction;
            _bank    = bank;

            if (_health == null)
                _health = GetComponent<Health>();

            if (_data != null)
                _health.Init(_data.MaxHp);

            // Для Extractor — кэш ноды
            if (_data != null && _data.BuildingType == BuildingType.Extractor)
                _nearestNode = FindNearestNode(transform.position, 5f);
        }

        // ----------------------------------------------------------------
        // Приватные методы
        // ----------------------------------------------------------------

        private void ApplyIncomeTick()
        {
            if (_data.BuildingType == BuildingType.Extractor)
            {
                if (_nearestNode != null)
                {
                    int extracted = _nearestNode.ExtractUpTo(_data.IncomePerTick);
                    if (extracted > 0)
                        _bank.Add(_faction, extracted);
                }
            }
            else
            {
                // HQ и любой другой тип с доходом — прямой бонус
                _bank.Add(_faction, _data.IncomePerTick);
            }
        }

        private void OnDied()
        {
            Destroy(gameObject, 0.1f);
        }

        /// <summary>Ищет ближайший ResourceNode в заданном радиусе.</summary>
        private static ResourceNode FindNearestNode(Vector3 origin, float radius)
        {
            var colliders = Physics.OverlapSphere(origin, radius);
            ResourceNode best     = null;
            float        bestDist = float.MaxValue;

            foreach (var col in colliders)
            {
                var node = col.GetComponent<ResourceNode>();
                if (node == null) continue;

                float dist = Vector3.SqrMagnitude(col.transform.position - origin);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best     = node;
                }
            }

            return best;
        }
    }
}
