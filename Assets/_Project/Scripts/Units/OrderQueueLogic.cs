using System.Collections.Generic;

namespace DiplomaGame.Runtime.Units
{
    /// <summary>
    /// Чистая статика для логики очереди приказов.
    /// Не зависит от MonoBehaviour и сцены — полностью тестируется в EditMode.
    /// Работает поверх переданной коллекции; Unit хранит состояние, эта логика — правила.
    /// </summary>
    public static class OrderQueueLogic
    {
        /// <summary>
        /// Определяет, допустима ли постановка данного типа приказа в очередь (Shift+ПКМ).
        /// Move и AttackMove — ставятся в очередь.
        /// Hold — немедленный (сбрасывает всё): зажать «стоп» в конце маршрута нельзя
        ///   предугадать разумно, и Hold обычно означает «остановись прямо сейчас».
        /// Patrol — не ставится в очередь: Patrol — бесконечный цикл между двумя точками;
        ///   добавлять его в цепочку не имеет смысла — он никогда не завершится.
        ///   Shift+P применяется как немедленный Patrol (очередь очищается).
        /// </summary>
        public static bool CanEnqueue(UnitCommandType type)
        {
            return type == UnitCommandType.Move || type == UnitCommandType.AttackMove;
        }

        /// <summary>
        /// Добавляет приказ в хвост очереди.
        /// </summary>
        public static void Enqueue(Queue<UnitCommand> queue, UnitCommand cmd)
        {
            queue.Enqueue(cmd);
        }

        /// <summary>
        /// Очищает очередь (вызывается при немедленном приказе IssueCommand).
        /// </summary>
        public static void Clear(Queue<UnitCommand> queue)
        {
            queue.Clear();
        }

        /// <summary>
        /// Пытается извлечь следующий приказ из очереди.
        /// Возвращает true, если приказ получен; false — очередь пуста.
        /// </summary>
        public static bool TryDequeueNext(Queue<UnitCommand> queue, out UnitCommand next)
        {
            if (queue.Count > 0)
            {
                next = queue.Dequeue();
                return true;
            }
            next = default;
            return false;
        }

        /// <summary>
        /// Возвращает количество приказов в очереди.
        /// </summary>
        public static int Count(Queue<UnitCommand> queue) => queue.Count;
    }
}
