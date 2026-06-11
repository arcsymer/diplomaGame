using System.Collections.Generic;
using DiplomaGame.Runtime.Combat;
using NUnit.Framework;
using UnityEngine;
// FindTargetsInRadius — добавлено v3 (AoE-атака Танка)

namespace DiplomaGame.Tests.Editor
{
    /// <summary>
    /// EditMode-тесты для CombatLogic (чистая статика, без MonoBehaviour).
    /// </summary>
    [TestFixture]
    public class CombatLogicTests
    {
        // ----------------------------------------------------------------
        // FindNearestTargetIndex
        // ----------------------------------------------------------------

        [Test]
        public void FindNearestTargetIndex_ReturnsNearestInRange()
        {
            var from = Vector3.zero;
            var candidates = new List<Vector3>
            {
                new Vector3(5f,  0f, 0f),   // index 0, dist 5
                new Vector3(2f,  0f, 0f),   // index 1, dist 2  — ближайший
                new Vector3(10f, 0f, 0f),   // index 2, dist 10
            };

            int result = CombatLogic.FindNearestTargetIndex(from, candidates, maxRange: 15f);

            Assert.AreEqual(1, result, "Должен вернуть индекс ближайшего кандидата.");
        }

        [Test]
        public void FindNearestTargetIndex_OutsideRange_ReturnsMinusOne()
        {
            var from = Vector3.zero;
            var candidates = new List<Vector3>
            {
                new Vector3(10f, 0f, 0f),   // dist 10
                new Vector3(20f, 0f, 0f),   // dist 20
                new Vector3(15f, 0f, 0f),   // dist 15
            };

            int result = CombatLogic.FindNearestTargetIndex(from, candidates, maxRange: 5f);

            Assert.AreEqual(-1, result, "Все кандидаты вне радиуса — должен вернуть -1.");
        }

        [Test]
        public void FindNearestTargetIndex_EmptyList_ReturnsMinusOne()
        {
            var from       = Vector3.zero;
            var candidates = new List<Vector3>();

            int result = CombatLogic.FindNearestTargetIndex(from, candidates, maxRange: 100f);

            Assert.AreEqual(-1, result, "Пустой список — должен вернуть -1.");
        }

        [Test]
        public void FindNearestTargetIndex_NullList_ReturnsMinusOne()
        {
            int result = CombatLogic.FindNearestTargetIndex(Vector3.zero, null, maxRange: 100f);

            Assert.AreEqual(-1, result, "null список — должен вернуть -1.");
        }

        [Test]
        public void FindNearestTargetIndex_ExactlyOnBoundary_ReturnsIndex()
        {
            // Кандидат точно на границе радиуса — должен попасть
            var from       = Vector3.zero;
            var candidates = new List<Vector3> { new Vector3(5f, 0f, 0f) };

            int result = CombatLogic.FindNearestTargetIndex(from, candidates, maxRange: 5f);

            Assert.AreEqual(0, result, "Кандидат точно на границе radиуса — должен быть найден.");
        }

        // ----------------------------------------------------------------
        // ShouldRetreat
        // ----------------------------------------------------------------

        [Test]
        public void ShouldRetreat_BelowThreshold_ReturnsTrue()
        {
            bool result = CombatLogic.ShouldRetreat(hpFraction: 0.2f, retreatFraction: 0.25f, retreatDisabled: false);

            Assert.IsTrue(result, "HP ниже порога и отступление разрешено — должен вернуть true.");
        }

        [Test]
        public void ShouldRetreat_AboveThreshold_ReturnsFalse()
        {
            bool result = CombatLogic.ShouldRetreat(hpFraction: 0.5f, retreatFraction: 0.25f, retreatDisabled: false);

            Assert.IsFalse(result, "HP выше порога — должен вернуть false.");
        }

        [Test]
        public void ShouldRetreat_ExactlyAtThreshold_ReturnsFalse()
        {
            // Условие строгое: < fraction, не <=
            bool result = CombatLogic.ShouldRetreat(hpFraction: 0.25f, retreatFraction: 0.25f, retreatDisabled: false);

            Assert.IsFalse(result, "HP точно на пороге — не отступаем (строгое <).");
        }

        [Test]
        public void ShouldRetreat_RetreatDisabled_AlwaysFalse()
        {
            bool result = CombatLogic.ShouldRetreat(hpFraction: 0.0f, retreatFraction: 0.25f, retreatDisabled: true);

            Assert.IsFalse(result, "retreatDisabled=true — никогда не должен возвращать true.");
        }

        // ----------------------------------------------------------------
        // IsInRange
        // ----------------------------------------------------------------

        [Test]
        public void IsInRange_WithinRange_ReturnsTrue()
        {
            bool result = CombatLogic.IsInRange(Vector3.zero, new Vector3(3f, 0f, 0f), range: 5f);

            Assert.IsTrue(result, "Точка в радиусе — должен вернуть true.");
        }

        [Test]
        public void IsInRange_OutsideRange_ReturnsFalse()
        {
            bool result = CombatLogic.IsInRange(Vector3.zero, new Vector3(6f, 0f, 0f), range: 5f);

            Assert.IsFalse(result, "Точка вне радиуса — должен вернуть false.");
        }

        [Test]
        public void IsInRange_ExactlyOnBoundary_ReturnsTrue()
        {
            // Точка точно на границе (sqrMagnitude == range * range)
            bool result = CombatLogic.IsInRange(Vector3.zero, new Vector3(5f, 0f, 0f), range: 5f);

            Assert.IsTrue(result, "Точка точно на границе дальности — должен вернуть true (<=).");
        }

        // ----------------------------------------------------------------
        // GetRetreatPoint
        // ----------------------------------------------------------------

        [Test]
        public void GetRetreatPoint_ReturnsRallyPoint()
        {
            var unitPos    = new Vector3(5f,  0f, 5f);
            var threatPos  = new Vector3(6f,  0f, 6f);
            var rallyPoint = new Vector3(0f,  0f, 0f);

            Vector3 result = CombatLogic.GetRetreatPoint(unitPos, threatPos, rallyPoint);

            Assert.AreEqual(rallyPoint, result, "Точка отступления должна быть rallyPoint.");
        }

        [Test]
        public void GetRetreatPoint_NoThreat_ReturnsRallyPoint()
        {
            var unitPos    = new Vector3(5f, 0f, 5f);
            var rallyPoint = new Vector3(0f, 0f, 0f);

            Vector3 result = CombatLogic.GetRetreatPoint(unitPos, Vector3.zero, rallyPoint);

            Assert.AreEqual(rallyPoint, result, "Без угрозы — всё равно возвращает rallyPoint.");
        }

        // ----------------------------------------------------------------
        // RandomiseInitialCooldownOffset
        // ----------------------------------------------------------------

        [Test]
        public void RandomiseInitialCooldownOffset_ZeroCooldown_ReturnsZero()
        {
            float result = CombatLogic.RandomiseInitialCooldownOffset(cooldown: 0f, seed: 12345);

            Assert.AreEqual(0f, result, 0.0001f, "Нулевой кулдаун — смещение всегда 0.");
        }

        [Test]
        public void RandomiseInitialCooldownOffset_InRange()
        {
            float cooldown = 1.5f;
            float result   = CombatLogic.RandomiseInitialCooldownOffset(cooldown, seed: 42);

            Assert.GreaterOrEqual(result, 0f,       "Смещение не должно быть отрицательным.");
            Assert.Less(result,           cooldown,  "Смещение должно быть строго меньше cooldown.");
        }

        [Test]
        public void RandomiseInitialCooldownOffset_DifferentSeeds_ProduceDifferentOffsets()
        {
            // Вероятность коллизии для двух разных seed пренебрежимо мала — это детерминированная функция.
            float a = CombatLogic.RandomiseInitialCooldownOffset(1f, seed: 1);
            float b = CombatLogic.RandomiseInitialCooldownOffset(1f, seed: 2);

            Assert.AreNotEqual(a, b, "Разные seed должны давать разные смещения.");
        }

        [Test]
        public void RandomiseInitialCooldownOffset_SameSeed_Deterministic()
        {
            float first  = CombatLogic.RandomiseInitialCooldownOffset(2f, seed: 999);
            float second = CombatLogic.RandomiseInitialCooldownOffset(2f, seed: 999);

            Assert.AreEqual(first, second, 0f, "Одинаковый seed — результат детерминирован.");
        }

        // ----------------------------------------------------------------
        // CanRetreatAgain
        // ----------------------------------------------------------------

        [Test]
        public void CanRetreatAgain_ZeroCooldown_AlwaysFalse()
        {
            // retreatCooldown == 0 означает одноразовое отступление
            bool result = CombatLogic.CanRetreatAgain(retreatCooldown: 0f, lastRetreatTime: 0f, now: 100f);

            Assert.IsFalse(result, "RetreatCooldown=0 — повторное отступление запрещено навсегда.");
        }

        [Test]
        public void CanRetreatAgain_CooldownNotExpired_ReturnsFalse()
        {
            bool result = CombatLogic.CanRetreatAgain(retreatCooldown: 30f, lastRetreatTime: 10f, now: 35f);

            Assert.IsFalse(result, "Кулдаун ещё не истёк — повторное отступление запрещено.");
        }

        [Test]
        public void CanRetreatAgain_CooldownExpired_ReturnsTrue()
        {
            bool result = CombatLogic.CanRetreatAgain(retreatCooldown: 30f, lastRetreatTime: 10f, now: 40f);

            Assert.IsTrue(result, "Кулдаун истёк — повторное отступление разрешено.");
        }

        [Test]
        public void CanRetreatAgain_ExactlyAtBoundary_ReturnsTrue()
        {
            // now - lastRetreatTime == retreatCooldown → разрешаем (>=)
            bool result = CombatLogic.CanRetreatAgain(retreatCooldown: 20f, lastRetreatTime: 5f, now: 25f);

            Assert.IsTrue(result, "Точно на границе кулдауна — повторное отступление разрешено (>=).");
        }

        // ----------------------------------------------------------------
        // FindTargetsInRadius (v3 AoE)
        // ----------------------------------------------------------------

        [Test]
        public void AoeFindTargets_ReturnsAllInRadius()
        {
            var from = Vector3.zero;
            var positions = new List<Vector3>
            {
                new Vector3(1f,  0f, 0f),  // index 0 — в радиусе
                new Vector3(2f,  0f, 0f),  // index 1 — в радиусе
                new Vector3(10f, 0f, 0f),  // index 2 — вне радиуса
            };
            var result = new List<int>();

            CombatLogic.FindTargetsInRadius(from, positions, radius: 3f, result);

            Assert.AreEqual(2, result.Count, "Два кандидата в радиусе 3 — должны оба попасть.");
            Assert.Contains(0, result);
            Assert.Contains(1, result);
            Assert.IsFalse(result.Contains(2), "Индекс 2 (dist=10) не должен попасть в результат.");
        }

        [Test]
        public void AoeFindTargets_EmptyList_ResultIsEmpty()
        {
            var from      = Vector3.zero;
            var positions = new List<Vector3>();
            var result    = new List<int> { 99 }; // предварительно замусоренный буфер

            CombatLogic.FindTargetsInRadius(from, positions, radius: 100f, result);

            Assert.AreEqual(0, result.Count, "Пустой список позиций — результат должен быть пустым.");
        }

        [Test]
        public void AoeFindTargets_NullList_ResultIsEmpty()
        {
            var result = new List<int> { 5 };

            CombatLogic.FindTargetsInRadius(Vector3.zero, null, radius: 100f, result);

            Assert.AreEqual(0, result.Count, "Null список — результат должен быть пустым.");
        }

        [Test]
        public void AoeFindTargets_ExactlyOnBoundary_IsIncluded()
        {
            var from      = Vector3.zero;
            var positions = new List<Vector3> { new Vector3(5f, 0f, 0f) };
            var result    = new List<int>();

            CombatLogic.FindTargetsInRadius(from, positions, radius: 5f, result);

            Assert.AreEqual(1, result.Count, "Цель точно на границе радиуса должна быть включена.");
        }
    }
}
