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
                "Миникарта",
                "ПКМ — задать точку сбора. Показывает союзников (синие) и врагов (красные).");
        }
    }
}
