using DiplomaGame.Runtime.Core.Localization;
using UnityEngine;

namespace DiplomaGame.Runtime.Data
{
    /// <summary>
    /// ScriptableObject с данными способности героя.
    /// Один ассет — одна способность (Data-driven, нет хардкода).
    /// </summary>
    [CreateAssetMenu(menuName = "DiplomaGame/Ability", fileName = "NewAbility")]
    public sealed class AbilityData : ScriptableObject
    {
        [SerializeField] private string      _displayName  = "Ability";
        [TextArea(2, 4)]
        [SerializeField] private string      _description  = "";

        [Header("Локализация EN")]
        [SerializeField] private string      _displayNameEn = "";
        [TextArea(2, 4)]
        [SerializeField] private string      _descriptionEn = "";
        [SerializeField] private float       _cooldown     = 8f;
        [SerializeField] private AbilityType _abilityType  = AbilityType.Shockwave;

        [Tooltip("Дистанция рывка. Используется только когда abilityType == Dash.")]
        [SerializeField] private float _dashDistance = 6f;

        [Tooltip("Радиус действия (Shockwave — урон, RepairField — лечение).")]
        [SerializeField] private float _effectRadius = 6f;

        [Tooltip("Величина эффекта: урон (Shockwave) или лечение (RepairField).")]
        [SerializeField] private float _effectAmount = 40f;

        [Tooltip("Длительность баффа в секундах. Используется только когда abilityType == Overcharge.")]
        [SerializeField] private float _buffDuration = 5f;

        [Tooltip("Множитель скорострельности при Overcharge (2 = вдвое чаще).")]
        [SerializeField] private float _fireRateMultiplier = 2f;

        [Tooltip("Множитель урона при Overcharge.")]
        [SerializeField] private float _damageMultiplier = 1.5f;

        // ----------------------------------------------------------------
        // Публичный API (read-only)
        // ----------------------------------------------------------------

        public string      DisplayName        =>
            LocService.CurrentLanguage == LocLanguage.En && !string.IsNullOrEmpty(_displayNameEn)
                ? _displayNameEn : _displayName;

        public string      Description        =>
            LocService.CurrentLanguage == LocLanguage.En && !string.IsNullOrEmpty(_descriptionEn)
                ? _descriptionEn : _description;
        public float       Cooldown           => _cooldown;
        public AbilityType AbilityType        => _abilityType;
        public float       DashDistance       => _dashDistance;
        public float       EffectRadius       => _effectRadius;
        public float       EffectAmount       => _effectAmount;
        public float       BuffDuration       => _buffDuration;
        public float       FireRateMultiplier => _fireRateMultiplier;
        public float       DamageMultiplier   => _damageMultiplier;

        // ----------------------------------------------------------------
        // Internal — для PlayMode-тестов
        // ----------------------------------------------------------------

        /// <summary>
        /// Создаёт AbilityData и заполняет поля — без SerializedObject (доступно в PlayMode).
        /// </summary>
        internal static AbilityData CreateForTest(
            string name,
            float cooldown,
            AbilityType type,
            float dashDistance = 0f,
            float effectRadius = 6f,
            float effectAmount = 40f,
            float buffDuration = 5f,
            float fireRateMultiplier = 2f,
            float damageMultiplier = 1.5f,
            string description = "")
        {
            var data                 = CreateInstance<AbilityData>();
            data._displayName        = name;
            data._description        = description;
            data._cooldown           = cooldown;
            data._abilityType        = type;
            data._dashDistance       = dashDistance;
            data._effectRadius       = effectRadius;
            data._effectAmount       = effectAmount;
            data._buffDuration       = buffDuration;
            data._fireRateMultiplier = fireRateMultiplier;
            data._damageMultiplier   = damageMultiplier;
            return data;
        }
    }
}
