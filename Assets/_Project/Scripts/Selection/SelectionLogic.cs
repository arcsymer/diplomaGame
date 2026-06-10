using UnityEngine;

namespace DiplomaGame.Runtime.Selection
{
    /// <summary>
    /// Чистая статика без зависимости от MonoBehaviour.
    /// Полностью тестируется в EditMode.
    /// </summary>
    public static class SelectionLogic
    {
        /// <summary>
        /// Строит нормализованный экранный Rect из двух произвольных углов.
        /// Гарантирует, что xMin &lt; xMax и yMin &lt; yMax.
        /// </summary>
        public static Rect GetScreenRect(Vector2 a, Vector2 b)
        {
            float xMin = Mathf.Min(a.x, b.x);
            float xMax = Mathf.Max(a.x, b.x);
            float yMin = Mathf.Min(a.y, b.y);
            float yMax = Mathf.Max(a.y, b.y);
            return new Rect(xMin, yMin, xMax - xMin, yMax - yMin);
        }

        /// <summary>
        /// Проверяет, находится ли экранная точка внутри прямоугольника.
        /// Граничные точки считаются внутренними.
        /// </summary>
        public static bool IsInside(Rect rect, Vector2 screenPoint)
        {
            return screenPoint.x >= rect.xMin
                && screenPoint.x <= rect.xMax
                && screenPoint.y >= rect.yMin
                && screenPoint.y <= rect.yMax;
        }

        /// <summary>
        /// Возвращает true, если расстояние между двумя точками меньше порога —
        /// то есть это клик, а не рамка выделения.
        /// </summary>
        /// <param name="down">Позиция нажатия.</param>
        /// <param name="up">Позиция отпускания.</param>
        /// <param name="threshold">Порог в пикселях (по умолчанию 8).</param>
        public static bool IsClick(Vector2 down, Vector2 up, float threshold = 8f)
        {
            return (up - down).magnitude < threshold;
        }
    }
}
