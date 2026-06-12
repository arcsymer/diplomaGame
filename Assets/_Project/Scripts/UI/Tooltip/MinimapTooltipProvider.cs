using DiplomaGame.Runtime.Core.Localization;
using UnityEngine;

namespace DiplomaGame.Runtime.UI
{
    /// <summary>
    /// Провайдер тултипа для миникарты.
    /// </summary>
    public sealed class MinimapTooltipProvider : MonoBehaviour, ITooltipProvider
    {
        public TooltipData GetTooltipData()
        {
            return new TooltipData(
                LocService.Get("tooltip.minimap_title"),
                LocService.Get("tooltip.minimap_desc"));
        }
    }
}
