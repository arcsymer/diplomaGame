namespace DiplomaGame.Runtime.Hero
{
    /// <summary>
    /// Чистая статика: тик и проверка кулдауна способностей.
    /// Полностью тестируется в EditMode.
    /// </summary>
    public static class AbilityCooldownLogic
    {
        /// <summary>
        /// Возвращает true, если способность готова к использованию (кулдаун истёк).
        /// </summary>
        public static bool IsReady(float remainingCooldown)
        {
            return remainingCooldown <= 0f;
        }

        /// <summary>
        /// Уменьшает оставшийся кулдаун на deltaTime. Не уходит ниже нуля.
        /// </summary>
        public static float Tick(float remainingCooldown, float deltaTime)
        {
            float next = remainingCooldown - deltaTime;
            return next > 0f ? next : 0f;
        }

        /// <summary>
        /// Начинает отсчёт кулдауна, возвращая его длительность.
        /// </summary>
        public static float StartCooldown(float duration)
        {
            return duration;
        }
    }
}
