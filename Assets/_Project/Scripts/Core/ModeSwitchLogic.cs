namespace DiplomaGame.Runtime.Core
{
    /// <summary>
    /// Чистая статическая логика переключения режимов.
    /// Не зависит от MonoBehaviour и сцены — полностью тестируется в EditMode.
    /// </summary>
    public static class ModeSwitchLogic
    {
        private const int PriorityActive   = 20;
        private const int PriorityInactive = 10;

        /// <summary>
        /// Возвращает приоритеты камер для заданного режима.
        /// </summary>
        /// <param name="mode">Текущий режим игры.</param>
        /// <returns>Кортеж (rtsPriority, tpsPriority).</returns>
        public static (int rtsPriority, int tpsPriority) GetPriorities(GameMode mode)
        {
            return mode == GameMode.Rts
                ? (PriorityActive, PriorityInactive)
                : (PriorityInactive, PriorityActive);
        }

        /// <summary>
        /// Инвертирует режим: Rts → Tps, Tps → Rts.
        /// </summary>
        public static GameMode Toggle(GameMode mode)
        {
            return mode == GameMode.Rts ? GameMode.Tps : GameMode.Rts;
        }
    }
}
