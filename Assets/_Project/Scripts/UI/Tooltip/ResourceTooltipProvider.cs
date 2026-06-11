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
                        "Кристаллы",
                        "Основная валюта. Добываются экстракторами и поступают от штаба.");

                default:
                    return new TooltipData("Ресурс", string.Empty);
            }
        }
    }
}
