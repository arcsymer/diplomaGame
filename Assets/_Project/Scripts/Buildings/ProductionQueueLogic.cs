namespace DiplomaGame.Runtime.Buildings
{
    /// <summary>
    /// Чистая статическая логика производственной очереди.
    /// Без MonoBehaviour — тестируется в EditMode без запуска сцены.
    /// </summary>
    public static class ProductionQueueLogic
    {
        /// <summary>
        /// Увеличивает прогресс производства на deltaTime.
        /// Возвращает новое значение прогресса (не ограничено сверху — проверяйте IsComplete).
        /// </summary>
        public static float TickProgress(float progress, float deltaTime)
        {
            return progress + deltaTime;
        }

        /// <summary>
        /// Возвращает true, если прогресс достиг или превысил productionTime.
        /// </summary>
        public static bool IsComplete(float progress, float productionTime)
        {
            return productionTime > 0f && progress >= productionTime;
        }
    }
}
