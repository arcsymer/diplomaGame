using UnityEngine;

namespace DiplomaGame.Runtime.Data
{
    /// <summary>
    /// ScriptableObject с данными технологии.
    /// Один ассет — одна технология. Нет хардкода.
    /// </summary>
    [CreateAssetMenu(menuName = "DiplomaGame/Tech Data", fileName = "NewTechData")]
    public sealed class TechData : ScriptableObject
    {
        [SerializeField] private string    _displayName     = "Tech";
        [TextArea(2, 4)]
        [SerializeField] private string    _description     = "";
        [SerializeField] private int       _cost            = 100;
        [SerializeField] private float     _researchTime    = 30f;
        [SerializeField] private Sprite    _icon            = null;
        [SerializeField] private string    _hotkeyLabel     = "";
        [SerializeField] private TechEffect _effectType     = TechEffect.None;

        [Tooltip("+0.20 = +20%; для кулдауна −0.15 = −15%.")]
        [SerializeField] private float     _effectMagnitude = 0f;

        [Tooltip("Технологии, которые должны быть исследованы до этой.")]
        [SerializeField] private TechData[] _prerequisites  = new TechData[0];

        [Tooltip("Если true — эффект применяется только к пехоте (AoeRadius == 0).")]
        [SerializeField] private bool      _infantryOnly    = false;

        // ----------------------------------------------------------------
        // Публичный API (read-only)
        // ----------------------------------------------------------------

        public string     DisplayName     => _displayName;
        public string     Description     => _description;
        public int        Cost            => _cost;
        public float      ResearchTime    => _researchTime;
        public Sprite     Icon            => _icon;
        public string     HotkeyLabel     => _hotkeyLabel;
        public TechEffect EffectType      => _effectType;
        public float      EffectMagnitude => _effectMagnitude;
        public TechData[] Prerequisites   => _prerequisites;
        public bool       InfantryOnly    => _infantryOnly;

        // ----------------------------------------------------------------
        // Internal — для тестов
        // ----------------------------------------------------------------

        /// <summary>
        /// Создаёт TechData с произвольными значениями без SerializedObject (для тестов).
        /// </summary>
        internal static TechData CreateForTest(
            string     displayName     = "TestTech",
            string     description     = "",
            int        cost            = 100,
            float      researchTime    = 30f,
            Sprite     icon            = null,
            string     hotkeyLabel     = "",
            TechEffect effectType      = TechEffect.None,
            float      effectMagnitude = 0f,
            TechData[] prerequisites   = null,
            bool       infantryOnly    = false)
        {
            var data               = CreateInstance<TechData>();
            data._displayName      = displayName;
            data._description      = description;
            data._cost             = cost;
            data._researchTime     = researchTime;
            data._icon             = icon;
            data._hotkeyLabel      = hotkeyLabel;
            data._effectType       = effectType;
            data._effectMagnitude  = effectMagnitude;
            data._prerequisites    = prerequisites ?? new TechData[0];
            data._infantryOnly     = infantryOnly;
            return data;
        }
    }
}
