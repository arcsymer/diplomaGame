using UnityEngine;

namespace DiplomaGame.Runtime.UI
{
    /// <summary>
    /// Чистая статика хитмаркерной логики прицела.
    /// Нет MonoBehaviour — тестируется в EditMode без сцены.
    /// </summary>
    public static class CrosshairHitmarkerLogic
    {
        /// <summary>
        /// Вычисляет целевой цвет и пиковый масштаб хитмаркера.
        /// </summary>
        /// <param name="hit">true = попадание, false = промах.</param>
        /// <param name="baseColor">Исходный цвет полосок прицела (обычно белый).</param>
        /// <param name="hitColor">Цвет вспышки при попадании (warm orange).</param>
        /// <param name="expandScale">Пиковый масштаб при попадании (напр. 1.15).</param>
        /// <param name="missScale">Пиковый масштаб при промахе (напр. 1.05).</param>
        /// <param name="outColor">Целевой цвет для анимации.</param>
        /// <param name="outPeakScale">Пиковый масштаб для анимации.</param>
        public static void Resolve(
            bool hit,
            Color baseColor,
            Color hitColor,
            float expandScale,
            float missScale,
            out Color outColor,
            out float outPeakScale)
        {
            if (hit)
            {
                outColor     = hitColor;
                outPeakScale = expandScale;
            }
            else
            {
                outColor     = baseColor;
                outPeakScale = missScale;
            }
        }

        /// <summary>
        /// Линейная интерполяция масштаба по нормализованному времени t ∈ [0, 1].
        /// Первая половина — нарастание до пика, вторая — спад обратно к 1.
        /// </summary>
        public static float PingPongScale(float t, float peakScale)
        {
            if (t <= 0.5f)
                return Mathf.LerpUnclamped(1f, peakScale, t / 0.5f);
            else
                return Mathf.LerpUnclamped(peakScale, 1f, (t - 0.5f) / 0.5f);
        }

        /// <summary>
        /// Интерполяция цвета: нарастание к hitColor за первую половину,
        /// затем затухание обратно к baseColor.
        /// </summary>
        public static Color PingPongColor(float t, Color baseColor, Color targetColor)
        {
            if (t <= 0.5f)
                return Color.LerpUnclamped(baseColor, targetColor, t / 0.5f);
            else
                return Color.LerpUnclamped(targetColor, baseColor, (t - 0.5f) / 0.5f);
        }
    }
}
