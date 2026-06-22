using UnityEngine;

namespace DiplomaGame.Runtime.AI
{
    /// <summary>
    /// ScriptableObject-профиль сложности ИИ.
    /// Хранит все числовые параметры, которые ApplyProfile() переносит в EnemyCommander.
    /// </summary>
    [CreateAssetMenu(menuName = "DiplomaGame/AI/DifficultyProfile", fileName = "DifficultyProfile")]
    public sealed class DifficultyProfileSO : ScriptableObject
    {
        // ----------------------------------------------------------------
        // Сериализованные поля
        // ----------------------------------------------------------------

        [SerializeField] private string _displayName        = "Normal";

        /// <summary>Интервал тиков решений ИИ, сек.</summary>
        [SerializeField] private float  _decisionInterval   = 2f;

        /// <summary>Множитель размера волны (GetWaveSizeForTime × scale, Mathf.RoundToInt, min 1).</summary>
        [SerializeField] private float  _waveSizeScale      = 1f;

        /// <summary>Максимальное время ожидания между волнами, сек.</summary>
        [SerializeField] private float  _maxWaitTime        = 30f;

        /// <summary>Лимит юнитов противника.</summary>
        [SerializeField] private int    _maxUnits           = 12;

        /// <summary>
        /// Резерв для ShouldResearchWithReserve.
        /// -1 означает «никогда не исследовать».
        /// </summary>
        [SerializeField] private int    _researchReserve    = 50;

        /// <summary>Соотношение пехота / (танк+1) для PickProductionEntryIndex.</summary>
        [SerializeField] private int    _infantryRatio      = 3;

        /// <summary>Бонусное золото, которое добавляется врагу один раз на старте.</summary>
        [SerializeField] private int    _enemyStartingBonusGold = 0;

        // ----------------------------------------------------------------
        // Circle-14: тактика ИИ — фланговые волны + экстренная волна
        // ----------------------------------------------------------------

        /// <summary>
        /// Задержка (сек) перед отправкой экстренной волны после потери вражеского здания.
        /// </summary>
        [SerializeField] private float _emergencyWaveDelay = 15f;

        /// <summary>
        /// Длительность паузы производства (сек) после потери вражеского здания.
        /// 0 = паузы нет.
        /// </summary>
        [SerializeField] private float _productionPauseDuration = 10f;

        /// <summary>
        /// Вероятность (0..1) того, что волна пойдёт во фланг, а не прямо на HQ.
        /// 0 = всегда в HQ, 1 = всегда во фланг.
        /// </summary>
        [SerializeField] [Range(0f, 1f)] private float _flankProbability = 0f;

        // ----------------------------------------------------------------
        // Геттеры
        // ----------------------------------------------------------------

        public string DisplayName            => _displayName;
        public float  DecisionInterval       => _decisionInterval;
        public float  WaveSizeScale          => _waveSizeScale;
        public float  MaxWaitTime            => _maxWaitTime;
        public int    MaxUnits               => _maxUnits;
        public int    ResearchReserve        => _researchReserve;
        public int    InfantryRatio          => _infantryRatio;
        public int    EnemyStartingBonusGold => _enemyStartingBonusGold;

        // Circle-14 геттеры
        public float EmergencyWaveDelay      => _emergencyWaveDelay;
        public float ProductionPauseDuration => _productionPauseDuration;
        public float FlankProbability        => _flankProbability;

        // ----------------------------------------------------------------
        // Internal factory — только для EditMode-тестов
        // ----------------------------------------------------------------

        /// <summary>
        /// Создаёт экземпляр без AssetDatabase.
        /// Используется исключительно в EditMode-тестах.
        /// </summary>
        internal static DifficultyProfileSO CreateForTest(
            string displayName,
            float  decisionInterval,
            float  waveSizeScale,
            float  maxWaitTime,
            int    maxUnits,
            int    researchReserve,
            int    infantryRatio,
            int    enemyStartingBonusGold,
            float  emergencyWaveDelay      = 15f,
            float  productionPauseDuration = 10f,
            float  flankProbability        = 0f)
        {
            var so = CreateInstance<DifficultyProfileSO>();
            so._displayName              = displayName;
            so._decisionInterval         = decisionInterval;
            so._waveSizeScale            = waveSizeScale;
            so._maxWaitTime              = maxWaitTime;
            so._maxUnits                 = maxUnits;
            so._researchReserve          = researchReserve;
            so._infantryRatio            = infantryRatio;
            so._enemyStartingBonusGold   = enemyStartingBonusGold;
            so._emergencyWaveDelay       = emergencyWaveDelay;
            so._productionPauseDuration  = productionPauseDuration;
            so._flankProbability         = flankProbability;
            return so;
        }
    }
}
