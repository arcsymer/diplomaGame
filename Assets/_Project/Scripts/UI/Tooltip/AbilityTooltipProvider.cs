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

        public TooltipData GetTooltipData()
        {
            var ability = _slotUi != null ? _slotUi.GetBoundAbility() : null;

            if (ability == null)
                return new TooltipData("Способность", "Слот не назначен");

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
