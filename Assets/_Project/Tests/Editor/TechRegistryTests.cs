using DiplomaGame.Runtime.Data;
using DiplomaGame.Runtime.Tech;
using DiplomaGame.Runtime.Units;
using NUnit.Framework;

namespace DiplomaGame.Tests.Editor
{
    /// <summary>
    /// EditMode-тесты для TechRegistry (9 кейсов из спеки).
    /// Чистая логика, не требует сцены.
    /// </summary>
    [TestFixture]
    public class TechRegistryTests
    {
        // TechRegistry — синглтон; сбрасываем через рефлексию между тестами,
        // чтобы каждый тест стартовал с чистым состоянием.
        [SetUp]
        public void SetUp()
        {
            ResetRegistry();
        }

        [TearDown]
        public void TearDown()
        {
            ResetRegistry();
        }

        private static void ResetRegistry()
        {
            // RuntimeInitializeOnLoadMethod недоступен в EditMode напрямую;
            // сбрасываем _instance через рефлексию.
            var field = typeof(TechRegistry).GetField(
                "_instance",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            field?.SetValue(null, null);
        }

        // ================================================================
        // 1. IsResearched — технология не исследована по умолчанию
        // ================================================================

        [Test]
        public void IsResearched_InitiallyFalse()
        {
            var tech = TechData.CreateForTest(displayName: "TestTech");
            Assert.IsFalse(TechRegistry.Instance.IsResearched(Faction.Player, tech),
                "Новая технология не должна быть исследована.");
        }

        // ================================================================
        // 2. MarkResearched → IsResearched возвращает true
        // ================================================================

        [Test]
        public void MarkResearched_ThenIsResearched_ReturnsTrue()
        {
            var tech = TechData.CreateForTest(displayName: "TestTech");
            TechRegistry.Instance.MarkResearched(Faction.Player, tech);
            Assert.IsTrue(TechRegistry.Instance.IsResearched(Faction.Player, tech),
                "После MarkResearched IsResearched должен вернуть true.");
        }

        // ================================================================
        // 3. CanResearch — без Prerequisites возвращает true для неисследованной
        // ================================================================

        [Test]
        public void CanResearch_NoPrerequisites_ReturnsTrue()
        {
            var tech = TechData.CreateForTest(displayName: "TestTech");
            Assert.IsTrue(TechRegistry.Instance.CanResearch(Faction.Player, tech),
                "Технология без prerequisitов должна быть доступна.");
        }

        // ================================================================
        // 4. CanResearch — с неисследованным Prerequisites возвращает false
        // ================================================================

        [Test]
        public void CanResearch_WithUnresearchedPrerequisite_ReturnsFalse()
        {
            var prereq = TechData.CreateForTest(displayName: "Prereq");
            var tech   = TechData.CreateForTest(displayName: "Tech", prerequisites: new[] { prereq });

            Assert.IsFalse(TechRegistry.Instance.CanResearch(Faction.Player, tech),
                "Технология с неисследованным prerequisite недоступна.");
        }

        // ================================================================
        // 5. CanResearch — после исследования Prerequisite возвращает true
        // ================================================================

        [Test]
        public void CanResearch_AfterPrerequisiteResearched_ReturnsTrue()
        {
            var prereq = TechData.CreateForTest(displayName: "Prereq");
            var tech   = TechData.CreateForTest(displayName: "Tech", prerequisites: new[] { prereq });

            TechRegistry.Instance.MarkResearched(Faction.Player, prereq);

            Assert.IsTrue(TechRegistry.Instance.CanResearch(Faction.Player, tech),
                "После исследования prerequisite технология должна стать доступной.");
        }

        // ================================================================
        // 6. MarkResearched вызывает событие TechResearched
        // ================================================================

        [Test]
        public void MarkResearched_FiresEvent()
        {
            var tech = TechData.CreateForTest(displayName: "EventTech");

            Faction  firedFaction = (Faction)(-1);
            TechData firedTech    = null;

            TechRegistry.TechResearched += (f, t) =>
            {
                firedFaction = f;
                firedTech    = t;
            };

            TechRegistry.Instance.MarkResearched(Faction.Enemy, tech);

            TechRegistry.TechResearched -= (f, t) => { };

            Assert.AreEqual(Faction.Enemy, firedFaction, "Событие должно передать корректную фракцию.");
            Assert.AreEqual(tech,          firedTech,    "Событие должно передать корректную технологию.");
        }

        // ================================================================
        // 7. GetDamageMultiplier применяет модификатор для пехоты
        // ================================================================

        [Test]
        public void GetDamageMultiplier_InfantryTech_AffectsInfantry()
        {
            var tech = TechData.CreateForTest(
                effectType:      TechEffect.DamageMultiplier,
                effectMagnitude: 0.15f,
                infantryOnly:    false);

            TechRegistry.Instance.MarkResearched(Faction.Player, tech);

            var unitData = UnitData.CreateForTest(aoeRadius: 0f); // пехота
            float mult = TechRegistry.Instance.GetDamageMultiplier(Faction.Player, unitData);

            Assert.AreEqual(0.15f, mult, 1e-5f,
                "DamageMultiplier 0.15 должен вернуть 0.15 для пехоты.");
        }

        // ================================================================
        // 8. InfantryOnly игнорирует танк (AoeRadius > 0)
        // ================================================================

        [Test]
        public void InfantryOnly_DoesNotAffectTank()
        {
            var tech = TechData.CreateForTest(
                effectType:      TechEffect.MaxHpMultiplier,
                effectMagnitude: 0.20f,
                infantryOnly:    true);

            TechRegistry.Instance.MarkResearched(Faction.Player, tech);

            var tankData = UnitData.CreateForTest(aoeRadius: 3f); // AoeRadius > 0 → танк
            float mult = TechRegistry.Instance.GetMaxHpMultiplier(Faction.Player, tankData);

            Assert.AreEqual(0f, mult, 1e-5f,
                "InfantryOnly-технология не должна влиять на танк (AoeRadius > 0).");
        }

        // ================================================================
        // 9. Кэш инвалидируется после MarkResearched
        // ================================================================

        [Test]
        public void Cache_InvalidatesAfterMarkResearched()
        {
            var unitData = UnitData.CreateForTest(aoeRadius: 0f);

            // Первое чтение — кэш пустой
            float before = TechRegistry.Instance.GetDamageMultiplier(Faction.Player, unitData);
            Assert.AreEqual(0f, before, 1e-5f, "До исследования множитель должен быть 0.");

            var tech = TechData.CreateForTest(
                effectType:      TechEffect.DamageMultiplier,
                effectMagnitude: 0.15f,
                infantryOnly:    false);

            TechRegistry.Instance.MarkResearched(Faction.Player, tech);

            // После исследования кэш должен инвалидироваться
            float after = TechRegistry.Instance.GetDamageMultiplier(Faction.Player, unitData);
            Assert.AreEqual(0.15f, after, 1e-5f,
                "После MarkResearched кэш должен обновиться и вернуть правильный множитель.");
        }
    }
}
