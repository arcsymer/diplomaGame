using UnityEngine;

namespace DiplomaGame.Runtime.GameFeel
{
    /// <summary>
    /// Чистая статическая логика динамического FOV TPS-камеры.
    /// Без зависимостей от MonoBehaviour — полностью тестируется в EditMode.
    ///
    /// Модель:
    ///   Состояние хранится снаружи (в DynamicFovController) и передаётся аргументами.
    ///   Kick: мгновенно поднимает _kickRemaining и targetFov = baseFov + kickAmount.
    ///          По мере истечения kickRemaining targetFov возвращается к baseFov.
    ///   Sprint-widen: не реализован — HeroController не экспонирует состояние спринта
    ///                 (единственная скорость moveSpeed без флага is_sprinting).
    /// </summary>
    public static class DynamicFovLogic
    {
        // ----------------------------------------------------------------
        // Kick trigger
        // ----------------------------------------------------------------

        /// <summary>
        /// Возвращает новое значение kickRemaining после применения kick-триггера.
        /// Всегда перезаписывает таймер — повторный kick во время предыдущего не накапливается,
        /// а сбрасывает таймер заново (так kick ощущается предсказуемо).
        /// </summary>
        /// <param name="kickDuration">Полная длительность kick-возврата (с).</param>
        public static float TriggerKick(float kickDuration)
        {
            return Mathf.Max(kickDuration, 0f);
        }

        // ----------------------------------------------------------------
        // Target FOV computation
        // ----------------------------------------------------------------

        /// <summary>
        /// Вычисляет целевой FOV для текущего кадра.
        /// Когда kickRemaining > 0 — целевой FOV widened на kickAmount.
        /// Когда kickRemaining == 0 — возврат к baseFov.
        /// </summary>
        /// <param name="baseFov">Базовый FOV (оригинальное значение ассета камеры).</param>
        /// <param name="kickAmount">Величина раскрытия FOV при kick (градусы, > 0).</param>
        /// <param name="kickRemaining">Оставшееся время kick-таймера (с). 0 = нет активного kick.</param>
        public static float GetTargetFov(float baseFov, float kickAmount, float kickRemaining)
        {
            return kickRemaining > 0f ? baseFov + kickAmount : baseFov;
        }

        // ----------------------------------------------------------------
        // Smooth step
        // ----------------------------------------------------------------

        /// <summary>
        /// Плавно приближает currentFov к targetFov за один кадр (линейный lerp).
        /// Возвращает следующее значение FOV.
        /// </summary>
        /// <param name="currentFov">Текущий FOV.</param>
        /// <param name="targetFov">Целевой FOV.</param>
        /// <param name="returnSpeed">Скорость возврата (1/с). Чем выше — тем быстрее.</param>
        /// <param name="dt">Time.deltaTime.</param>
        public static float StepFov(float currentFov, float targetFov, float returnSpeed, float dt)
        {
            if (Mathf.Approximately(currentFov, targetFov))
                return targetFov;

            float t = Mathf.Clamp01(returnSpeed * dt);
            return Mathf.Lerp(currentFov, targetFov, t);
        }

        // ----------------------------------------------------------------
        // Kick timer tick
        // ----------------------------------------------------------------

        /// <summary>
        /// Убывает kick-таймер. Возвращает обновлённое значение kickRemaining (не ниже 0).
        /// </summary>
        /// <param name="kickRemaining">Текущее значение таймера.</param>
        /// <param name="dt">Time.deltaTime.</param>
        public static float TickKick(float kickRemaining, float dt)
        {
            return Mathf.Max(kickRemaining - dt, 0f);
        }

        // ----------------------------------------------------------------
        // Convenience: all-in-one tick (for MonoBehaviour LateUpdate)
        // ----------------------------------------------------------------

        /// <summary>
        /// Объединённый тик: убывает kick-таймер, вычисляет targetFov, плавно движется к нему.
        /// Возвращает (nextFov, nextKickRemaining).
        /// </summary>
        public static (float nextFov, float nextKickRemaining) Tick(
            float currentFov,
            float baseFov,
            float kickAmount,
            float kickRemaining,
            float returnSpeed,
            float dt)
        {
            float nextKick   = TickKick(kickRemaining, dt);
            float targetFov  = GetTargetFov(baseFov, kickAmount, nextKick);
            float nextFov    = StepFov(currentFov, targetFov, returnSpeed, dt);
            return (nextFov, nextKick);
        }
    }
}
