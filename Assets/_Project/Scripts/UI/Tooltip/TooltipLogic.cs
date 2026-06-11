using System;
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

            // Invariant — чтобы вывод не зависел от локали ОС («1.5», не «1,5»)
            switch (data.AbilityType)
            {
                case AbilityType.Dash:
                    return FormattableString.Invariant(
                        $"Кулдаун: {data.Cooldown:F0}с   Дистанция: {data.DashDistance:F0}");

                case AbilityType.Shockwave:
                    return FormattableString.Invariant(
                        $"Кулдаун: {data.Cooldown:F0}с   Урон: {data.EffectAmount:F0}   Радиус: {data.EffectRadius:F0}");

                case AbilityType.RepairField:
                    return FormattableString.Invariant(
                        $"Кулдаун: {data.Cooldown:F0}с   Лечение: {data.EffectAmount:F0}   Радиус: {data.EffectRadius:F0}");

                case AbilityType.Overcharge:
                    return FormattableString.Invariant(
                        $"Кулдаун: {data.Cooldown:F0}с   Длительность: {data.BuffDuration:F0}с   Урон ×{data.DamageMultiplier:F1}");

                default:
                    return FormattableString.Invariant($"Кулдаун: {data.Cooldown:F0}с");
            }
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
