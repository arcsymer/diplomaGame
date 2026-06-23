using DiplomaGame.Runtime.Audio;
using DiplomaGame.Runtime.Data;
using DiplomaGame.Runtime.Hero;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiplomaGame.Runtime.UI
{
    /// <summary>
    /// UI-слот одной способности TPS-HUD.
    /// Иконка — цветной квадрат-заглушка, overlay fillAmount = кулдаун/полное.
    ///
    /// Circle-20: детектирует переход кулдауна >0 → 0 и выдаёт UiPulse + UI-звук.
    /// </summary>
    public sealed class AbilitySlotUI : MonoBehaviour
    {
        [SerializeField] private Image   icon;
        [SerializeField] private Image   cooldownOverlay;
        [SerializeField] private TMP_Text keyLabel;

        private AbilitySystem _abilitySystem;
        private int           _slotIndex;

        // ----------------------------------------------------------------
        // Кэш компонентов (Awake)
        // ----------------------------------------------------------------

        private UiPulse _pulse;

        // ----------------------------------------------------------------
        // Состояние детектора готовности (Circle-20)
        // ----------------------------------------------------------------

        /// <summary>
        /// Флаг: был ли кулдаун > 0 на предыдущем кадре.
        /// Чистая логика готовности — <see cref="AbilityReadyLogic.DetectReadyEdge"/>.
        /// </summary>
        private bool _wasCoolingDown;

        // ----------------------------------------------------------------
        // Unity lifecycle
        // ----------------------------------------------------------------

        private void Awake()
        {
            _pulse = GetComponent<UiPulse>();
        }

        private void Update()
        {
            if (_abilitySystem == null || cooldownOverlay == null) return;

            float remaining = _abilitySystem.GetRemainingCooldown(_slotIndex);
            float total     = _abilitySystem.GetCooldownDuration(_slotIndex);
            cooldownOverlay.fillAmount = HudLogic.CooldownFill(remaining, total);

            // Детекция перехода "охлаждается → готова" (Circle-20)
            bool readyEdge = AbilityReadyLogic.DetectReadyEdge(_wasCoolingDown, remaining);
            if (readyEdge)
                OnAbilityBecameReady();

            _wasCoolingDown = AbilityReadyLogic.IsCoolingDown(remaining);
        }

        // ----------------------------------------------------------------
        // Публичный API
        // ----------------------------------------------------------------

        /// <summary>
        /// Возвращает AbilityData, привязанную к этому слоту.
        /// Используется AbilityTooltipProvider для получения данных тултипа.
        /// </summary>
        public AbilityData GetBoundAbility()
        {
            return _abilitySystem != null ? _abilitySystem.GetAbility(_slotIndex) : null;
        }

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

        // ----------------------------------------------------------------
        // Обработчик готовности (Circle-20)
        // ----------------------------------------------------------------

        private void OnAbilityBecameReady()
        {
            // Scale-пульс слота
            if (_pulse != null)
                _pulse.TriggerPulse();

            // UI-звук: используем PlayUiClick как ближайший подходящий CC0-клип.
            // TODO: заменить на специализированный «ability ready» звук, когда он будет добавлен.
            if (AudioManager.Instance != null)
                AudioManager.Instance.PlayUiClick();
        }
    }
}
