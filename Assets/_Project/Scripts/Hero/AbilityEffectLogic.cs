using System.Collections.Generic;
using UnityEngine;

namespace DiplomaGame.Runtime.Hero
{
    /// <summary>
    /// Чистая статика: вычисления эффектов способностей героя.
    /// Полностью тестируется в EditMode.
    /// </summary>
    public static class AbilityEffectLogic
    {
        /// <summary>
        /// Заполняет <paramref name="resultIndices"/> индексами позиций, попавших в радиус
        /// от <paramref name="center"/>. Без аллокаций — буфер очищается и заполняется заново.
        /// </summary>
        public static void CollectIndicesInRadius(
            Vector3 center,
            IReadOnlyList<Vector3> positions,
            float radius,
            List<int> resultIndices)
        {
            resultIndices.Clear();
            if (positions == null || radius <= 0f)
                return;

            float radiusSqr = radius * radius;
            for (int i = 0; i < positions.Count; i++)
            {
                if ((positions[i] - center).sqrMagnitude <= radiusSqr)
                    resultIndices.Add(i);
            }
        }

        /// <summary>
        /// Эффективный кулдаун выстрела с учётом множителя скорострельности.
        /// Множитель ≤ 0 или ≤ 1 трактуется как «без ускорения» при ≤1 (защита от деления на 0).
        /// </summary>
        public static float EffectiveFireCooldown(float baseCooldown, float fireRateMultiplier)
        {
            if (fireRateMultiplier <= 1f)
                return baseCooldown;

            return baseCooldown / fireRateMultiplier;
        }

        /// <summary>Активен ли бафф в момент <paramref name="now"/> (границу считаем активной).</summary>
        public static bool IsBuffActive(float now, float buffUntil)
        {
            return now <= buffUntil;
        }
    }
}
