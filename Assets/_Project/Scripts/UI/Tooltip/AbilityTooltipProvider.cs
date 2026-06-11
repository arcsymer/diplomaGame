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

        private void Awake()
        {
            _slotUi = GetComponent<AbilitySlotUI>();
        }

        public TooltipData GetTooltipData()
        {
            var ability = _slotUi != null ? _slotUi.GetBoundAbility() : null;

            if (ability == null)
                return new TooltipData("Способность", "Слот не назначен");

            string stats = TooltipLogic.FormatAbilityStats(ability);
            return new TooltipData(ability.DisplayName, ability.Description, stats);
        }
    }
}
