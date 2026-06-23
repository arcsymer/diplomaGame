namespace DiplomaGame.Runtime.UI
{
    /// <summary>
    /// Чистая статика детекции перехода кулдауна способности из «охлаждается» в «готова».
    /// Нет MonoBehaviour — тестируется в EditMode без сцены.
    /// </summary>
    public static class AbilityReadyLogic
    {
        /// <summary>
        /// Возвращает true только в момент перехода кулдауна >0 → 0.
        /// Параметр <paramref name="wasCoolingDown"/> — состояние предыдущего кадра.
        /// </summary>
        public static bool DetectReadyEdge(bool wasCoolingDown, float currentRemaining)
        {
            return wasCoolingDown && !IsCoolingDown(currentRemaining);
        }

        /// <summary>
        /// Возвращает true, если способность ещё на кулдауне (remaining > 0).
        /// </summary>
        public static bool IsCoolingDown(float remaining)
        {
            return remaining > 0f;
        }
    }
}
