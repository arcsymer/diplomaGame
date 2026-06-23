using UnityEngine;

namespace DiplomaGame.Runtime.UI
{
    /// <summary>
    /// Чистая логика индикатора урона героя (Circle-21 / Circle-23).
    /// Нет MonoBehaviour — тестируется в EditMode без сцены.
    ///
    /// Circle-21: full-edge красный флэш с затуханием (Health.AnyDamaged).
    /// Circle-23: направленный индикатор — угол к источнику урона (Health.AnyDamagedFrom).
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

        // ----------------------------------------------------------------
        // Circle-23: направленный угол к источнику урона
        // ----------------------------------------------------------------

        /// <summary>
        /// Вычисляет угол (в градусах) от опорного направления героя до источника урона
        /// в горизонтальной плоскости XZ. Чисто-математический метод без MonoBehaviour —
        /// детерминирован и тестируется в EditMode без сцены.
        ///
        /// Соглашение:
        ///   0°   — атакующий ПЕРЕД героем (верх кольца HUD).
        ///  +90°  — атакующий СПРАВА.
        ///  180°  — атакующий ПОЗАДИ.
        ///  −90°  — атакующий СЛЕВА.
        ///
        /// Формула: signed angle от refForwardXZ до вектора (sourcePos − heroPos)
        /// в плоскости XZ (ось вращения = Vector3.up).
        /// </summary>
        /// <param name="refForwardXZ">
        ///   Нормализованный вектор «вперёд» героя в плоскости XZ
        ///   (получается из camera.transform.forward или hero.transform.forward,
        ///   с обнулённым Y и нормализацией). Не обязан быть единичным — метод нормализует сам.
        /// </param>
        /// <param name="heroPos">Мировая позиция героя.</param>
        /// <param name="sourcePos">Мировая позиция источника урона (атакующего).</param>
        /// <returns>
        ///   Угол в диапазоне (−180, +180], в градусах.
        ///   Если heroPosXZ == sourcePosXZ (атака прямо в точке героя) — возвращает 0f.
        ///   Если refForwardXZ ≈ нулевой вектор — возвращает 0f.
        /// </returns>
        public static float ComputeIndicatorAngleDegrees(
            Vector3 refForwardXZ,
            Vector3 heroPos,
            Vector3 sourcePos)
        {
            // Проецируем опорное направление в плоскость XZ
            Vector3 fwd = new Vector3(refForwardXZ.x, 0f, refForwardXZ.z);
            if (fwd.sqrMagnitude < 1e-10f)
                return 0f;

            fwd.Normalize();

            // Вектор от героя к источнику в плоскости XZ
            Vector3 toSource = new Vector3(
                sourcePos.x - heroPos.x,
                0f,
                sourcePos.z - heroPos.z);

            if (toSource.sqrMagnitude < 1e-10f)
                return 0f;

            // SignedAngle(from, to, axis) — положительный угол по часовой стрелке
            // вокруг Vector3.up: т.е. +90° = правее опорного направления.
            return Vector3.SignedAngle(fwd, toSource, Vector3.up);
        }
    }
}
