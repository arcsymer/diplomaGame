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
        [SerializeField] private float       _cooldown     = 8f;
        [SerializeField] private AbilityType _abilityType  = AbilityType.Placeholder2;

        [Tooltip("Дистанция рывка. Используется только когда abilityType == Dash.")]
        [SerializeField] private float _dashDistance = 6f;

        // ----------------------------------------------------------------
        // Публичный API (read-only)
        // ----------------------------------------------------------------

        public string      DisplayName  => _displayName;
        public float       Cooldown     => _cooldown;
        public AbilityType AbilityType  => _abilityType;
        public float       DashDistance => _dashDistance;

        // ----------------------------------------------------------------
        // Internal — для PlayMode-тестов
        // ----------------------------------------------------------------

        /// <summary>
        /// Создаёт AbilityData и заполняет поля — без SerializedObject (доступно в PlayMode).
        /// </summary>
        internal static AbilityData CreateForTest(string name, float cooldown, AbilityType type, float dashDistance = 0f)
        {
            var data            = CreateInstance<AbilityData>();
            data._displayName   = name;
            data._cooldown      = cooldown;
            data._abilityType   = type;
            data._dashDistance  = dashDistance;
            return data;
        }
    }
}
