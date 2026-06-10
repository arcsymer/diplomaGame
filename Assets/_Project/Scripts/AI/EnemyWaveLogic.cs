namespace DiplomaGame.Runtime.AI
{
    /// <summary>
    /// Чистая статическая логика принятия решений ИИ-противника.
    /// Без MonoBehaviour — полностью тестируема в EditMode.
    /// </summary>
    public static class EnemyWaveLogic
    {
        /// <summary>
        /// Возвращает true, если ИИ должен произвести очередного юнита.
        /// Условие: баланс покрывает стоимость И текущее количество юнитов меньше лимита.
        /// </summary>
        public static bool ShouldProduce(int balance, int unitCost, int currentUnits, int maxUnits)
        {
            if (unitCost <= 0)         return false;
            if (currentUnits >= maxUnits) return false;
            return balance >= unitCost;
        }

        /// <summary>
        /// Возвращает true, если ИИ должен отправить волну.
        /// Условие A: накопилось не менее waveSize юнитов.
        /// Условие B: прошло maxWaitTime секунд с последней волны И есть хотя бы 2 юнита.
        /// </summary>
        public static bool ShouldLaunchWave(
            int   idleCombatUnits,
            int   waveSize,
            float timeSinceLastWave,
            float maxWaitTime)
        {
            if (idleCombatUnits <= 0) return false;

            // Условие A — накопилась полная волна
            if (idleCombatUnits >= waveSize) return true;

            // Условие B — вышло время ожидания, хотя бы 2 юнита
            if (timeSinceLastWave >= maxWaitTime && idleCombatUnits >= 2) return true;

            return false;
        }

        /// <summary>
        /// Возвращает желаемый размер волны в зависимости от времени матча (в секундах).
        /// До 3 мин — 3, до 7 мин — 5, после — 7.
        /// </summary>
        public static int GetWaveSizeForTime(float matchTime)
        {
            if (matchTime < 180f) return 3;   // 0–3 мин
            if (matchTime < 420f) return 5;   // 3–7 мин
            return 7;                          // 7+ мин
        }
    }
}
