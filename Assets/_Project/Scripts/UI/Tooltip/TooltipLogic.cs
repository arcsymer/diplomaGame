using System;
using DiplomaGame.Runtime.Core.Localization;
using DiplomaGame.Runtime.Data;
using UnityEngine;

namespace DiplomaGame.Runtime.UI
{
    /// <summary>
    /// Чистая статика: форматирование статов тултипа и математика позиционирования.
    /// Тестируется через EditMode без MonoBehaviour.
    /// </summary>
    public static class TooltipLogic
    {
        public const float TooltipMaxWidth = 280f;

        // ----------------------------------------------------------------
        // Форматирование статов по типу способности
        // ----------------------------------------------------------------

        /// <summary>
        /// Возвращает строку Stats для тултипа способности.
        /// Возвращает null, если data == null.
        /// </summary>
        public static string FormatAbilityStats(AbilityData data)
        {
            if (data == null) return null;

            // Invariant — чтобы вывод не зависел от локали ОС («1.5», не «1,5»).
            // Локализованные метки берём из LocService; числа форматируем Invariant.
            string cooldownFmt  = LocService.Get("tooltip.ability_cooldown");  // "Кулдаун: {0}с"
            string distanceFmt  = LocService.Get("tooltip.ability_distance");  // "Дистанция: {0}"
            string damageFmt    = LocService.Get("tooltip.ability_damage");    // "Урон: {0}"
            string radiusFmt    = LocService.Get("tooltip.ability_radius");    // "Радиус: {0}"
            string healFmt      = LocService.Get("tooltip.ability_heal");      // "Лечение: {0}"
            string durationFmt  = LocService.Get("tooltip.ability_duration");  // "Длительность: {0}с"
            string dmgMultFmt   = LocService.Get("tooltip.ability_dmg_mult");  // "Урон ×{0}"

            // Заменяем {0} числовым значением Invariant-форматом.
            string cooldownStr = FormatStat(cooldownFmt, data.Cooldown, "F0");

            switch (data.AbilityType)
            {
                case AbilityType.Dash:
                    return cooldownStr + "   "
                        + FormatStat(distanceFmt, data.DashDistance, "F0");

                case AbilityType.Shockwave:
                    return cooldownStr + "   "
                        + FormatStat(damageFmt, data.EffectAmount, "F0") + "   "
                        + FormatStat(radiusFmt, data.EffectRadius, "F0");

                case AbilityType.RepairField:
                    return cooldownStr + "   "
                        + FormatStat(healFmt, data.EffectAmount, "F0") + "   "
                        + FormatStat(radiusFmt, data.EffectRadius, "F0");

                case AbilityType.Overcharge:
                    return cooldownStr + "   "
                        + FormatStat(durationFmt, data.BuffDuration, "F0") + "   "
                        + FormatStat(dmgMultFmt, data.DamageMultiplier, "F1");

                default:
                    return cooldownStr;
            }
        }

        // ----------------------------------------------------------------
        // Вспомогательные методы форматирования
        // ----------------------------------------------------------------

        /// <summary>
        /// Заменяет {0} в шаблоне числом с указанным форматом (Invariant culture).
        /// Если в шаблоне нет {0} — возвращает шаблон как есть.
        /// </summary>
        private static string FormatStat(string fmt, float value, string numFmt)
        {
            if (string.IsNullOrEmpty(fmt)) return "";
            int idx = fmt.IndexOf("{0}", StringComparison.Ordinal);
            if (idx < 0) return fmt;

            string numStr = value.ToString(numFmt, System.Globalization.CultureInfo.InvariantCulture);
            return fmt.Substring(0, idx) + numStr + fmt.Substring(idx + 3);
        }

        // ----------------------------------------------------------------
        // Позиционирование
        // ----------------------------------------------------------------

        /// <summary>
        /// Вычисляет anchoredPosition тултипа относительно Canvas (ScreenSpaceOverlay).
        /// Входная screenPos — координата курсора в Screen-пространстве (y=0 снизу).
        /// Результат кламплируется в границы экрана с учётом размера тултипа.
        /// Если тултип выходит за правый край → якорь смещается влево.
        /// Если тултип выходит за нижний край → якорь смещается вверх.
        /// </summary>
        public static Vector2 ClampToScreen(Vector2 screenPos, Vector2 tooltipSize)
        {
            // Базовое смещение курсор + (16, -16)
            float x = screenPos.x + 16f;
            float y = screenPos.y - 16f;

            float w = Screen.width;
            float h = Screen.height;

            // Выход за правый край
            if (x + tooltipSize.x > w)
                x = screenPos.x - tooltipSize.x - 16f;

            // Выход за нижний край (tultip открывается вниз от y, y=0 снизу)
            // Тултип рисуется вниз от якоря (anchor top-left), поэтому низ = y - tooltipSize.y
            if (y - tooltipSize.y < 0f)
                y = screenPos.y + tooltipSize.y + 16f;

            // Дополнительный кламп чтобы не выйти за экран сверху/слева
            x = Mathf.Clamp(x, 0f, w - tooltipSize.x);
            y = Mathf.Clamp(y, tooltipSize.y, h);

            return new Vector2(x, y);
        }
    }
}
