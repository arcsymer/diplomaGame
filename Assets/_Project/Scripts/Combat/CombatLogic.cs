using System.Collections.Generic;
using UnityEngine;

namespace DiplomaGame.Runtime.Combat
{
    /// <summary>
    /// Чистая статика: боевые вычисления без зависимостей от MonoBehaviour и сцены.
    /// Полностью тестируется в EditMode.
    /// </summary>
    public static class CombatLogic
    {
        /// <summary>
        /// Возвращает индекс ближайшей цели в радиусе <paramref name="maxRange"/>
        /// или -1, если ни одна цель не попала в радиус или список пуст.
        /// </summary>
        /// <param name="from">Позиция ищущего.</param>
        /// <param name="candidates">Список позиций кандидатов.</param>
        /// <param name="maxRange">Максимальная дальность обнаружения.</param>
        public static int FindNearestTargetIndex(Vector3 from, IReadOnlyList<Vector3> candidates, float maxRange)
        {
            if (candidates == null || candidates.Count == 0)
                return -1;

            float maxRangeSqr = maxRange * maxRange;
            int   bestIndex   = -1;
            float bestSqr     = float.MaxValue;

            for (int i = 0; i < candidates.Count; i++)
            {
                float sqr = (from - candidates[i]).sqrMagnitude;
                if (sqr <= maxRangeSqr && sqr < bestSqr)
                {
                    bestSqr   = sqr;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        /// <summary>
        /// Возвращает true, если юнит должен отступить:
        /// HP-доля ниже порога и отступление не запрещено.
        /// </summary>
        public static bool ShouldRetreat(float hpFraction, float retreatFraction, bool retreatDisabled)
        {
            if (retreatDisabled) return false;
            return hpFraction < retreatFraction;
        }

        /// <summary>
        /// Проверяет, находится ли точка <paramref name="b"/> в атаке-дальности от <paramref name="a"/>.
        /// Использует sqrMagnitude — без корня.
        /// </summary>
        public static bool IsInRange(Vector3 a, Vector3 b, float range)
        {
            return (a - b).sqrMagnitude <= range * range;
        }

        /// <summary>
        /// Сколько HP реально восстановится при лечении: не выше недостающего,
        /// 0 — для мёртвых и при неположительном лечении.
        /// </summary>
        public static float ClampHeal(float healAmount, float currentHp, float maxHp, bool isDead)
        {
            if (isDead || healAmount <= 0f || currentHp >= maxHp)
                return 0f;

            float missing = maxHp - currentHp;
            return healAmount < missing ? healAmount : missing;
        }

        /// <summary>
        /// Возвращает точку отступления.
        /// Движется к <paramref name="rallyPoint"/> базы; если угрозы нет (threatPos == Vector3.zero
        /// или совпадает с позицией юнита) — просто к rallyPoint.
        /// </summary>
        /// <param name="unitPos">Текущая позиция юнита.</param>
        /// <param name="threatPos">Позиция ближайшего врага (Vector3.zero — нет угрозы).</param>
        /// <param name="rallyPoint">Точка сбора у своей базы.</param>
        public static Vector3 GetRetreatPoint(Vector3 unitPos, Vector3 threatPos, Vector3 rallyPoint)
        {
            return rallyPoint;
        }

        /// <summary>
        /// Вычисляет смещение первого выстрела для рандомизации кулдауна.
        /// Возвращает случайное значение в диапазоне [0, cooldown), используя
        /// детерминированный случай на основе <paramref name="seed"/>.
        /// Нет аллокаций, нет зависимости от UnityEngine.Random — чистая математика.
        /// </summary>
        /// <param name="cooldown">Кулдаун атаки юнита.</param>
        /// <param name="seed">Уникальное число для рандомизации (например, GetInstanceID()).</param>
        public static float RandomiseInitialCooldownOffset(float cooldown, int seed)
        {
            if (cooldown <= 0f) return 0f;
            // Псевдослучайное значение через хэш-mix без аллокаций
            uint h = (uint)seed;
            h ^= h >> 16;
            h *= 0x45d9f3b;
            h ^= h >> 16;
            // Нормализуем в [0, 1)
            float t = (h & 0x7FFFFFFFu) / (float)0x7FFFFFFF;
            return t * cooldown;
        }

        /// <summary>
        /// Проверяет, можно ли снова отступить.
        /// Если <paramref name="retreatCooldown"/> == 0 — отступление одноразово (всегда false после первого).
        /// Если &gt; 0 — разрешает повторное отступление по истечении кулдауна.
        /// </summary>
        /// <param name="retreatCooldown">Параметр из UnitData.RetreatCooldown.</param>
        /// <param name="lastRetreatTime">Time.time в момент последнего ухода в Retreating.</param>
        /// <param name="now">Текущее Time.time.</param>
        public static bool CanRetreatAgain(float retreatCooldown, float lastRetreatTime, float now)
        {
            if (retreatCooldown <= 0f) return false;
            return now - lastRetreatTime >= retreatCooldown;
        }

        /// <summary>
        /// Находит все позиции в радиусе <paramref name="radius"/> от <paramref name="from"/>
        /// и записывает их индексы в <paramref name="resultIndices"/>.
        /// Метод чистой статики, без аллокаций: использует переданный буфер.
        /// AoE проверяет фактическое расстояние без дополнительного BuildingAttackBuffer
        /// (здания крупные, но AoE — площадная атака, не точечная).
        /// </summary>
        /// <param name="from">Центр области поиска (позиция атакующего).</param>
        /// <param name="positions">Список позиций кандидатов.</param>
        /// <param name="radius">Радиус AoE.</param>
        /// <param name="resultIndices">Буфер-приёмник индексов; очищается перед заполнением.</param>
        public static void FindTargetsInRadius(
            Vector3               from,
            IReadOnlyList<Vector3> positions,
            float                 radius,
            List<int>             resultIndices)
        {
            resultIndices.Clear();

            if (positions == null || positions.Count == 0)
                return;

            float radiusSqr = radius * radius;
            for (int i = 0; i < positions.Count; i++)
            {
                if ((from - positions[i]).sqrMagnitude <= radiusSqr)
                    resultIndices.Add(i);
            }
        }
    }
}
