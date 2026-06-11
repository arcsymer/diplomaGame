using UnityEngine;

namespace DiplomaGame.Runtime.Data
{
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

        // ----------------------------------------------------------------
        // Internal — для тестов
        // ----------------------------------------------------------------

        /// <summary>
        /// Создаёт BuildingData с произвольными значениями без SerializedObject (для PlayMode-тестов).
        /// </summary>
        internal static BuildingData CreateForTest(
            string       displayName       = "TestBuilding",
            int          cost              = 0,
            float        maxHp             = 500f,
            BuildingType buildingType      = BuildingType.Headquarters,
            int          incomePerTick     = 0,
            float        incomeTickInterval = 2f,
            UnitData     produces          = null,
            float        productionTime    = 5f,
            int          productionCost    = 50,
            string       description       = "")
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
            return data;
        }
    }
}
