namespace DiplomaGame.Runtime.Hero
{
    /// <summary>
    /// Чистая статика: проверка кулдауна стрельбы.
    /// Полностью тестируется в EditMode.
    /// </summary>
    public static class FireRateLogic
    {
        /// <summary>
        /// Возвращает true, если с момента последнего выстрела прошло достаточно времени.
        /// </summary>
        /// <param name="lastFireTime">Time.time в момент последнего выстрела (0 → никогда не стреляли).</param>
        /// <param name="cooldown">Минимальный интервал между выстрелами в секундах.</param>
        /// <param name="now">Текущее Time.time.</param>
        public static bool CanFire(float lastFireTime, float cooldown, float now)
        {
            // Эпсилон компенсирует погрешность float-вычитания на точной границе кулдауна
            const float epsilon = 1e-4f;
            return now - lastFireTime >= cooldown - epsilon;
        }
    }
}
