using DiplomaGame.Runtime.Core.Localization;
using UnityEngine;

namespace DiplomaGame.Runtime.UI
{
    /// <summary>
    /// Провайдер тултипа для ресурсного индикатора.
    /// </summary>
    public sealed class ResourceTooltipProvider : MonoBehaviour, ITooltipProvider
    {
        public enum ResourceKind { Crystals }

        [SerializeField] private ResourceKind resourceKind = ResourceKind.Crystals;

        public TooltipData GetTooltipData()
        {
            switch (resourceKind)
            {
                case ResourceKind.Crystals:
                    return new TooltipData(
                        LocService.Get("tooltip.crystals_title"),
                        LocService.Get("tooltip.crystals_desc"));

                default:
                    return new TooltipData(LocService.Get("tooltip.crystals_title"), string.Empty);
            }
        }
    }
}
