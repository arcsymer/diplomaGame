using UnityEngine;

namespace DiplomaGame.Runtime.Data
{
    /// <summary>
    /// Запись таблицы производства здания: тип юнита, стоимость, время, иконка и хоткей.
    /// techData != null означает «производить технологию», unitData игнорируется.
    /// </summary>
    [System.Serializable]
    public sealed class ProductionEntry
    {
        [SerializeField] public UnitData unitData;
        [SerializeField] public int      cost;
        [SerializeField] public float    productionTime;
        [SerializeField] public Sprite   icon;
        [SerializeField] public string   hotkeyLabel;

        /// <summary>
        /// Если не null — запись описывает исследование технологии, а не производство юнита.
        /// </summary>
        [SerializeField] public TechData techData;
    }

    /// <summary>
    /// Запись таблицы технологий здания (только технология).
    /// Используется BuildingData._techEntries для перечисления доступных в здании исследований.
    /// </summary>
    [System.Serializable]
    public sealed class TechEntry
    {
        [SerializeField] public TechData techData;
    }

    /// <summary>
    /// ScriptableObject с характеристиками здания.
    /// Один ассет — один тип здания.
    /// </summary>
    [CreateAssetMenu(menuName = "DiplomaGame/Building Data", fileName = "NewBuildingData")]
    public sealed class BuildingData : ScriptableObject
    {
        [SerializeField] private string       _displayName      = "Building";
        [TextArea(2, 4)]
        [SerializeField] private string       _description      = "";
        [SerializeField] private int          _cost             = 0;
        [SerializeField] private float        _maxHp            = 500f;
        [SerializeField] private BuildingType _buildingType     = BuildingType.Headquarters;
        [SerializeField] private int          _incomePerTick    = 0;
        [SerializeField] private float        _incomeTickInterval = 2f;

        [Tooltip("Юнит, производимый в этом здании (только для Barracks).")]
        [SerializeField] private UnitData _produces = null;

        [SerializeField] private float _productionTime = 5f;
        [SerializeField] private int   _productionCost = 50;

        [Tooltip("Список производимых юнитов (multi-production). Пустой массив — legacy-режим.")]
        [SerializeField] private ProductionEntry[] _productionEntries = new ProductionEntry[0];

        [Tooltip("Технологии, доступные для исследования в этом здании.")]
        [SerializeField] private TechEntry[] _techEntries = new TechEntry[0];

        // ----------------------------------------------------------------
        // Публичный API (read-only)
        // ----------------------------------------------------------------

        public string       DisplayName       => _displayName;
        public string       Description       => _description;
        public int          Cost              => _cost;
        public float        MaxHp             => _maxHp;
        public BuildingType BuildingType      => _buildingType;
        public int          IncomePerTick     => _incomePerTick;
        public float        IncomeTickInterval => _incomeTickInterval;
        public UnitData     Produces          => _produces;
        public float        ProductionTime    => _productionTime;
        public int          ProductionCost    => _productionCost;

        /// <summary>Таблица производства (multi-production). Может быть пустой.</summary>
        public ProductionEntry[] ProductionEntries => _productionEntries;

        /// <summary>True, если задана таблица производства с хотя бы одной записью.</summary>
        public bool HasMultiProduction => _productionEntries != null && _productionEntries.Length > 0;

        /// <summary>Таблица технологий, доступных для исследования в этом здании. Может быть пустой.</summary>
        public TechEntry[] TechEntries => _techEntries;

        /// <summary>True, если в здании есть хотя бы одна запись технологий.</summary>
        public bool HasTechEntries => _techEntries != null && _techEntries.Length > 0;

        // ----------------------------------------------------------------
        // Internal — для тестов
        // ----------------------------------------------------------------

        /// <summary>
        /// Создаёт BuildingData с произвольными значениями без SerializedObject (для PlayMode-тестов).
        /// </summary>
        internal static BuildingData CreateForTest(
            string            displayName        = "TestBuilding",
            int               cost               = 0,
            float             maxHp              = 500f,
            BuildingType      buildingType        = BuildingType.Headquarters,
            int               incomePerTick       = 0,
            float             incomeTickInterval  = 2f,
            UnitData          produces            = null,
            float             productionTime      = 5f,
            int               productionCost      = 50,
            string            description         = "",
            ProductionEntry[] productionEntries   = null,
            TechEntry[]       techEntries         = null)
        {
            var data                  = CreateInstance<BuildingData>();
            data._displayName         = displayName;
            data._description         = description;
            data._cost                = cost;
            data._maxHp               = maxHp;
            data._buildingType        = buildingType;
            data._incomePerTick       = incomePerTick;
            data._incomeTickInterval  = incomeTickInterval;
            data._produces            = produces;
            data._productionTime      = productionTime;
            data._productionCost      = productionCost;
            data._productionEntries   = productionEntries ?? new ProductionEntry[0];
            data._techEntries         = techEntries ?? new TechEntry[0];
            return data;
        }
    }
}
