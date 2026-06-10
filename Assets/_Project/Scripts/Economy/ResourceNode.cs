using UnityEngine;

namespace DiplomaGame.Runtime.Economy
{
    /// <summary>
    /// Маркер месторождения кристаллов на карте.
    /// Хранит запас и предоставляет метод добычи.
    /// Исчерпанная нода не уничтожается — визуальное затемнение добавляется в M8.
    /// </summary>
    public sealed class ResourceNode : MonoBehaviour
    {
        [SerializeField] private int _reserve = 1000;

        // ----------------------------------------------------------------
        // Публичный API
        // ----------------------------------------------------------------

        /// <summary>Текущий остаток ресурсов в ноде.</summary>
        public int Remaining => _reserve;

        /// <summary>
        /// Извлекает до amount кристаллов из ноды.
        /// Возвращает фактически добытое (может быть меньше amount, если запас иссяк).
        /// При исчерпанном запасе возвращает 0.
        /// </summary>
        public int ExtractUpTo(int amount)
        {
            if (_reserve <= 0 || amount <= 0)
                return 0;

            int extracted = amount <= _reserve ? amount : _reserve;
            _reserve -= extracted;
            return extracted;
        }

        // ----------------------------------------------------------------
        // Internal — для тестов
        // ----------------------------------------------------------------

        /// <summary>Устанавливает начальный запас без SerializedObject (для PlayMode-тестов).</summary>
        internal void InitForTest(int reserve)
        {
            _reserve = reserve;
        }
    }
}
