namespace DiplomaGame.Runtime.Buildings
{
    /// <summary>
    /// Чистая статическая логика валидации размещения зданий.
    /// Без MonoBehaviour — тестируется в EditMode без запуска сцены.
    /// </summary>
    public static class PlacementLogic
    {
        /// <summary>
        /// Возвращает true, если позиция для строительства допустима.
        /// </summary>
        /// <param name="overlaps">true, если место уже занято (Physics.OverlapBox нашёл объекты).</param>
        /// <param name="needsNode">true, если тип здания требует ResourceNode поблизости (Extractor).</param>
        /// <param name="hasNodeNearby">true, если ResourceNode найден в нужном радиусе.</param>
        public static bool IsPlacementValid(bool overlaps, bool needsNode, bool hasNodeNearby)
        {
            if (overlaps)    return false;
            if (needsNode && !hasNodeNearby) return false;
            return true;
        }
    }
}
