using DiplomaGame.Runtime.Hero;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiplomaGame.Runtime.UI
{
    /// <summary>
    /// UI-слот одной способности TPS-HUD.
    /// Иконка — цветной квадрат-заглушка, overlay fillAmount = кулдаун/полное.
    /// </summary>
    public sealed class AbilitySlotUI : MonoBehaviour
    {
        [SerializeField] private Image   icon;
        [SerializeField] private Image   cooldownOverlay;
        [SerializeField] private TMP_Text keyLabel;

        private AbilitySystem _abilitySystem;
        private int           _slotIndex;

        // ----------------------------------------------------------------
        // Unity lifecycle
        // ----------------------------------------------------------------

        private void Update()
        {
            if (_abilitySystem == null || cooldownOverlay == null) return;

            float remaining = _abilitySystem.GetRemainingCooldown(_slotIndex);
            float total     = _abilitySystem.GetCooldownDuration(_slotIndex);
            cooldownOverlay.fillAmount = HudLogic.CooldownFill(remaining, total);
        }

        // ----------------------------------------------------------------
        // Публичный API
        // ----------------------------------------------------------------

        /// <summary>
        /// Связывает слот с AbilitySystem и индексом способности (0..3).
        /// </summary>
        public void Bind(AbilitySystem system, int index)
        {
            _abilitySystem = system;
            _slotIndex     = index;

            if (keyLabel != null)
                keyLabel.SetText((index + 1).ToString());

            // Цвет иконки-заглушки по индексу для различимости
            if (icon != null)
            {
                icon.color = index switch
                {
                    0 => new Color(0.3f, 0.7f, 1f),
                    1 => new Color(1f,   0.7f, 0.2f),
                    2 => new Color(0.5f, 1f,   0.3f),
                    3 => new Color(1f,   0.3f, 0.5f),
                    _ => Color.white,
                };
            }
        }
    }
}
