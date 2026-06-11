using System.Collections.Generic;
using DiplomaGame.Runtime.Combat;
using DiplomaGame.Runtime.Hero;
using NUnit.Framework;
using UnityEngine;

namespace DiplomaGame.Tests.Editor
{
    /// <summary>
    /// EditMode-тесты чистой логики эффектов способностей героя (слоты 2–4, ADR-013).
    /// </summary>
    [TestFixture]
    public class AbilityEffectLogicTests
    {
        // ----------------------------------------------------------------
        // CollectIndicesInRadius
        // ----------------------------------------------------------------

        [Test]
        public void CollectIndicesInRadius_ReturnsOnlyTargetsInside()
        {
            var positions = new List<Vector3>
            {
                new Vector3(1f, 0f, 0f),   // внутри (дист 1)
                new Vector3(5f, 0f, 0f),   // на границе (дист 5)
                new Vector3(5.1f, 0f, 0f), // снаружи
                new Vector3(0f, 0f, -3f),  // внутри
            };
            var result = new List<int>();

            AbilityEffectLogic.CollectIndicesInRadius(Vector3.zero, positions, 5f, result);

            CollectionAssert.AreEquivalent(new[] { 0, 1, 3 }, result);
        }

        [Test]
        public void CollectIndicesInRadius_EmptyOrNullPositions_ReturnsEmpty()
        {
            var result = new List<int> { 99 }; // мусор должен быть очищен

            AbilityEffectLogic.CollectIndicesInRadius(Vector3.zero, null, 5f, result);
            Assert.IsEmpty(result);

            AbilityEffectLogic.CollectIndicesInRadius(Vector3.zero, new List<Vector3>(), 5f, result);
            Assert.IsEmpty(result);
        }

        [Test]
        public void CollectIndicesInRadius_NonPositiveRadius_ReturnsEmpty()
        {
            var positions = new List<Vector3> { Vector3.zero };
            var result    = new List<int>();

            AbilityEffectLogic.CollectIndicesInRadius(Vector3.zero, positions, 0f, result);

            Assert.IsEmpty(result);
        }

        // ----------------------------------------------------------------
        // EffectiveFireCooldown
        // ----------------------------------------------------------------

        [Test]
        public void EffectiveFireCooldown_DoubleRate_HalvesCooldown()
        {
            Assert.AreEqual(0.075f, AbilityEffectLogic.EffectiveFireCooldown(0.15f, 2f), 1e-5f);
        }

        [Test]
        public void EffectiveFireCooldown_MultiplierAtMostOne_ReturnsBase()
        {
            Assert.AreEqual(0.15f, AbilityEffectLogic.EffectiveFireCooldown(0.15f, 1f));
            Assert.AreEqual(0.15f, AbilityEffectLogic.EffectiveFireCooldown(0.15f, 0f));
            Assert.AreEqual(0.15f, AbilityEffectLogic.EffectiveFireCooldown(0.15f, -2f));
        }

        // ----------------------------------------------------------------
        // IsBuffActive
        // ----------------------------------------------------------------

        [Test]
        public void IsBuffActive_BeforeAndAtDeadline_True()
        {
            Assert.IsTrue(AbilityEffectLogic.IsBuffActive(now: 4f, buffUntil: 5f));
            Assert.IsTrue(AbilityEffectLogic.IsBuffActive(now: 5f, buffUntil: 5f));
        }

        [Test]
        public void IsBuffActive_AfterDeadline_False()
        {
            Assert.IsFalse(AbilityEffectLogic.IsBuffActive(now: 5.01f, buffUntil: 5f));
        }

        // ----------------------------------------------------------------
        // CombatLogic.ClampHeal (логика лечения Health.Heal)
        // ----------------------------------------------------------------

        [Test]
        public void ClampHeal_PartialDamage_HealsRequestedAmount()
        {
            Assert.AreEqual(30f, CombatLogic.ClampHeal(30f, currentHp: 50f, maxHp: 100f, isDead: false));
        }

        [Test]
        public void ClampHeal_NearMax_ClampsToMissing()
        {
            Assert.AreEqual(10f, CombatLogic.ClampHeal(30f, currentHp: 90f, maxHp: 100f, isDead: false));
        }

        [Test]
        public void ClampHeal_FullHp_ReturnsZero()
        {
            Assert.AreEqual(0f, CombatLogic.ClampHeal(30f, currentHp: 100f, maxHp: 100f, isDead: false));
        }

        [Test]
        public void ClampHeal_Dead_ReturnsZero()
        {
            Assert.AreEqual(0f, CombatLogic.ClampHeal(30f, currentHp: 0f, maxHp: 100f, isDead: true));
        }

        [Test]
        public void ClampHeal_NonPositiveAmount_ReturnsZero()
        {
            Assert.AreEqual(0f, CombatLogic.ClampHeal(0f,  currentHp: 50f, maxHp: 100f, isDead: false));
            Assert.AreEqual(0f, CombatLogic.ClampHeal(-5f, currentHp: 50f, maxHp: 100f, isDead: false));
        }
    }
}
