using UnityEngine;

namespace DiplomaGame.Runtime.UI
{
    /// <summary>
    /// Чистая статика для HUD-вычислений.
    /// Тестируется через EditMode без MonoBehaviour.
    /// </summary>
    public static class HudLogic
    {
        /// <summary>Форматирует количество кристаллов в строку «Crystals: N».</summary>
        public static string FormatCrystals(int amount)
        {
            return "Crystals: " + amount.ToString();
        }

        /// <summary>
        /// Возвращает fillAmount оверлея кулдауна (0 = готова, 1 = только что использована).
        /// remaining — остаток кулдауна в секундах; total — полное время кулдауна.
        /// </summary>
        public static float CooldownFill(float remaining, float total)
        {
            if (total <= 0f) return 0f;
            return Mathf.Clamp01(remaining / total);
        }
    }
}
