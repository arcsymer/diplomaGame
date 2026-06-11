using UnityEngine;

namespace DiplomaGame.Runtime.Units
{
    /// <summary>
    /// Чистая статика без зависимостей от MonoBehaviour и сцены.
    /// Полностью тестируется в EditMode.
    /// </summary>
    public static class UnitCommandLogic
    {
        /// <summary>
        /// Возвращает состояние, в которое юнит должен перейти при получении приказа.
        /// </summary>
        public static UnitState GetStateForCommand(UnitCommandType type)
        {
            switch (type)
            {
                case UnitCommandType.Move:       return UnitState.Moving;
                case UnitCommandType.AttackMove: return UnitState.Moving;
                case UnitCommandType.Hold:       return UnitState.Holding;
                case UnitCommandType.Patrol:     return UnitState.Patrolling;
                default:                         return UnitState.Idle;
            }
        }

        /// <summary>
        /// Возвращает следующую точку патрулирования (переключает A ↔ B).
        /// Если юнит рядом с A — идёт к B; иначе — к A.
        /// </summary>
        public static Vector3 GetNextPatrolPoint(Vector3 current, Vector3 pointA, Vector3 pointB)
        {
            // Выбираем точку, которая дальше от текущей позиции.
            // Тем самым гарантируем переключение: если пришли к A — идём к B и наоборот.
            float distA = (current - pointA).sqrMagnitude;
            float distB = (current - pointB).sqrMagnitude;
            return distA <= distB ? pointB : pointA;
        }

        /// <summary>
        /// Определяет, достиг ли юнит цели.
        /// </summary>
        /// <param name="remainingDistance">NavMeshAgent.remainingDistance</param>
        /// <param name="stoppingDistance">NavMeshAgent.stoppingDistance</param>
        /// <param name="pathPending">NavMeshAgent.pathPending</param>
        public static bool HasArrived(float remainingDistance, float stoppingDistance, bool pathPending)
        {
            if (pathPending) return false;
            return remainingDistance <= stoppingDistance + 0.01f;
        }

        /// <summary>
        /// Возвращает смещение для i-го юнита в строевом порядке (простая сетка).
        /// Индекс 0 — центр (Vector3.zero). Без аллокаций, чистая математика.
        /// Spacing увеличен до 2.0 в рамках v3-улучшения crowd avoidance (ADR-018).
        /// </summary>
        /// <param name="index">Порядковый номер юнита в группе (0-based).</param>
        /// <param name="spacing">Расстояние между ячейками сетки.</param>
        public static Vector3 GetFormationOffset(int index, float spacing = 2.0f)
        {
            if (index == 0) return Vector3.zero;

            // Раскладываем по спирали из колец:
            // кольцо 1: 8 ячеек (индексы 1..8), кольцо 2: 16 ячеек (9..24), ...
            int ring = 0;
            int count = 0;
            while (count + 8 * (ring + 1) < index)
            {
                count += 8 * (ring + 1);
                ring++;
            }
            ring++;

            int posInRing = index - count - 1; // 0-based в текущем кольце
            int sideCount = 8 * ring;          // всего ячеек в кольце

            float angle = (posInRing / (float)sideCount) * 360f * Mathf.Deg2Rad;
            float r     = ring * spacing;

            return new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r);
        }
    }
}
