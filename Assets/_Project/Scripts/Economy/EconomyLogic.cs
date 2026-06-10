namespace DiplomaGame.Runtime.Economy
{
    /// <summary>
    /// Чистая статическая логика экономики.
    /// Без MonoBehaviour — тестируется в EditMode без запуска сцены.
    /// </summary>
    public static class EconomyLogic
    {
        /// <summary>Возвращает true, если на балансе достаточно средств для оплаты cost.</summary>
        public static bool CanAfford(int balance, int cost)
        {
            return balance >= cost;
        }

        /// <summary>
        /// Списывает cost с баланса и возвращает новый баланс.
        /// Вызывать только после CanAfford — не проверяет доступность.
        /// Результат не опускается ниже нуля (защита от ошибок вызывающей стороны).
        /// </summary>
        public static int Spend(int balance, int cost)
        {
            int result = balance - cost;
            return result < 0 ? 0 : result;
        }

        /// <summary>
        /// Вычисляет, сколько полных тиков прошло за elapsed секунд при интервале tickInterval.
        /// Возвращает количество тиков; в remainder — остаток времени после последнего тика.
        /// Стабилен при лагах: крупный elapsed даёт несколько тиков сразу.
        /// </summary>
        public static int CalculateIncomeTicks(float elapsed, float tickInterval, out float remainder)
        {
            if (tickInterval <= 0f)
            {
                remainder = 0f;
                return 0;
            }

            int ticks = (int)(elapsed / tickInterval);
            remainder = elapsed - ticks * tickInterval;
            return ticks;
        }
    }
}
