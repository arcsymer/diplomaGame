namespace DiplomaGame.Runtime.VFX
{
    /// <summary>
    /// Чистая статика: вспомогательная логика VFX-пула.
    /// Отдельно от MonoBehaviour для тестируемости.
    /// </summary>
    public static class VfxLogic
    {
        /// <summary>
        /// Возвращает следующий индекс пула по кругу.
        /// </summary>
        /// <param name="current">Текущий индекс (последний использованный).</param>
        /// <param name="size">Размер пула (количество слотов).</param>
        /// <returns>Следующий индекс в диапазоне [0, size).</returns>
        public static int NextPoolIndex(int current, int size)
        {
            if (size <= 0)
                return 0;
            return (current + 1) % size;
        }
    }
}
