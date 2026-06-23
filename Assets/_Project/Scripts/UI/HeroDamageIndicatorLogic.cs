using UnityEngine;

namespace DiplomaGame.Runtime.UI
{
    /// <summary>
    /// Чистая логика индикатора урона героя (Circle-21).
    /// Нет MonoBehaviour — тестируется в EditMode без сцены.
    ///
    /// Limitation: <see cref="DiplomaGame.Runtime.Combat.Health.AnyDamaged"/> не несёт позицию
    /// источника урона (сигнатура — Health, float), поэтому направленный указатель невозможен.
    /// Реализован full-edge красный флэш с затуханием.
    /// </summary>
    public static class HeroDamageIndicatorLogic
    {
        // ----------------------------------------------------------------
        // Дросселирование
        // ----------------------------------------------------------------

        /// <summary>
        /// Возвращает true если индикатор должен сработать (или быть обновлён/продлён).
        /// При быстрых повторных попаданиях в окне throttle — обновляет таймер (refresh/extend),
        /// поэтому всегда возвращает true, пока герой получает урон.
        /// Дросселирование применяется только к числу ОТДЕЛЬНЫХ «вспышек»:
        /// если флэш уже идёт — мы обновляем lastTriggerTime, а не спауним новый.
        /// </summary>
        /// <param name="lastTriggerTime">Время последнего успешного триггера (Time.time).</param>
        /// <param name="now">Текущее время (Time.time).</param>
        /// <param name="throttleWindow">Окно дросселирования в секундах.</param>
        public static bool ShouldTrigger(float lastTriggerTime, float now, float throttleWindow)
        {
            // Первый вызов (lastTriggerTime = float.NegativeInfinity) — всегда пропускаем.
            // Повторный в окне: обновляем таймер (refresh/extend), поэтому тоже true.
            return true;  // throttling is handled by coroutine restart (interrupt-restart pattern)
        }

        /// <summary>
        /// Вычисляет текущую альфу флэша по нормализованному времени t ∈ [0, 1].
        /// t=0: пик (peakAlpha), t=1: 0.
        /// Кривая: мгновенный пик → линейное затухание.
        /// </summary>
        /// <param name="t">Нормализованное время (0=только что сработал, 1=угасло).</param>
        /// <param name="peakAlpha">Максимальная альфа в момент удара.</param>
        public static float ComputeFadeAlpha(float t, float peakAlpha)
        {
            // Линейный спад: peakAlpha → 0 за [0..1]
            float clamped = Mathf.Clamp01(t);
            return Mathf.Lerp(peakAlpha, 0f, clamped);
        }

        /// <summary>
        /// Возвращает true, если флэш-анимация завершена (alpha достигла нуля).
        /// </summary>
        public static bool IsFadeDone(float elapsed, float duration)
        {
            return elapsed >= duration;
        }
    }
}
