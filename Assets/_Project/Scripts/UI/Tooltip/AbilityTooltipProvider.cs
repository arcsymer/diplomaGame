using DiplomaGame.Runtime.Core.Localization;
using DiplomaGame.Runtime.Data;
using UnityEngine;

namespace DiplomaGame.Runtime.UI
{
    /// <summary>
    /// Провайдер тултипа для слота способности.
    /// Требует AbilitySlotUI на том же объекте; читает AbilityData через GetBoundAbility().
    /// </summary>
    [RequireComponent(typeof(AbilitySlotUI))]
    public sealed class AbilityTooltipProvider : MonoBehaviour, ITooltipProvider
    {
        private AbilitySlotUI _slotUi;

        // Кэш: последний использованный AbilityData и соответствующий ему TooltipData.
        // Пересчёт происходит только при смене ссылки на AbilityData.
        private AbilityData _cachedAbility;
        private TooltipData _cachedTooltip;

        private void Awake()
        {
            _slotUi = GetComponent<AbilitySlotUI>();
        }

        private void OnEnable()
        {
            LocService.LanguageChanged += InvalidateCache;
        }

        private void OnDisable()
        {
            LocService.LanguageChanged -= InvalidateCache;
        }

        private void InvalidateCache()
        {
            // Сброс кэша — при следующем GetTooltipData строки пересчитаются в новом языке.
            _cachedAbility = null;
        }

        public TooltipData GetTooltipData()
        {
            var ability = _slotUi != null ? _slotUi.GetBoundAbility() : null;

            if (ability == null)
                return new TooltipData(
                    LocService.Get("tooltip.ability_empty_title"),
                    LocService.Get("tooltip.ability_empty_desc"));

            // Пересчитываем только при смене AbilityData (сравнение по ссылке)
            if (!ReferenceEquals(ability, _cachedAbility))
            {
                _cachedAbility = ability;
                string stats   = TooltipLogic.FormatAbilityStats(ability);
                _cachedTooltip = new TooltipData(ability.DisplayName, ability.Description, stats);
            }

            return _cachedTooltip;
        }
    }
}
