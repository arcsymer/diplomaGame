using System.Collections.Generic;
using DiplomaGame.Runtime.Combat;
using NUnit.Framework;
using UnityEngine;

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
    }
}
