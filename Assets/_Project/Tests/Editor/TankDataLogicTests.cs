using DiplomaGame.Runtime.Data;
using NUnit.Framework;

namespace DiplomaGame.Tests.Editor
{
    /// <summary>
    /// EditMode-тесты для новых полей UnitData: AoeRadius и TargetPriority (v3).
    /// Проверяют значения по умолчанию и корректность CreateForTest.
    /// </summary>
    [TestFixture]
    public class TankDataLogicTests
    {
        [Test]
        public void UnitData_AoeRadius_DefaultIsZero()
        {
            // CreateForTest без явного aoeRadius — дефолт должен быть 0 (одиночная атака).
            var data = UnitData.CreateForTest();

            Assert.AreEqual(0f, data.AoeRadius, 0.0001f,
                "AoeRadius по умолчанию должен быть 0 (одиночная атака).");
        }

        [Test]
        public void UnitData_TargetPriority_DefaultIsUnits()
        {
            // CreateForTest без явного targetPriority — дефолт должен быть Units.
            var data = UnitData.CreateForTest();

            Assert.AreEqual(TargetPriority.Units, data.TargetPriority,
                "TargetPriority по умолчанию должен быть Units.");
        }

        [Test]
        public void UnitData_CreateForTest_AoeRadiusIsSet()
        {
            const float expectedAoe = 3.0f;
            var data = UnitData.CreateForTest(aoeRadius: expectedAoe);

            Assert.AreEqual(expectedAoe, data.AoeRadius, 0.0001f,
                "AoeRadius должен принять переданное значение.");
        }

        [Test]
        public void UnitData_CreateForTest_TargetPriorityBuildings()
        {
            var data = UnitData.CreateForTest(targetPriority: TargetPriority.Buildings);

            Assert.AreEqual(TargetPriority.Buildings, data.TargetPriority,
                "TargetPriority должен принять переданное значение Buildings.");
        }

        [Test]
        public void UnitData_TankStats_MatchSpecification()
        {
            // Проверяем, что CreateForTest с танковыми параметрами создаёт верный объект
            // (зеркало балансовых данных из спеки: HP 280, Damage 25, AoeRadius 2.3 — баланс-фикс circle-19).
            var tank = UnitData.CreateForTest(
                displayName:    "Tank",
                maxHp:          280f,
                damage:         25f,
                attackRange:    5f,
                attackCooldown: 2.0f,
                aggroRadius:    12f,
                moveSpeed:      3.0f,
                retreatDisabled: true,
                supplyCost:     3,
                aoeRadius:      2.3f,
                targetPriority: TargetPriority.Buildings);

            Assert.AreEqual(280f, tank.MaxHp,          0.001f, "MaxHp танка должен быть 280.");
            Assert.AreEqual(25f,  tank.Damage,         0.001f, "Damage танка должен быть 25.");
            Assert.AreEqual(2.3f, tank.AoeRadius,      0.001f, "AoeRadius танка должен быть 2.3.");
            Assert.IsTrue(tank.RetreatDisabled,                 "RetreatDisabled танка должен быть true.");
            Assert.AreEqual(TargetPriority.Buildings,  tank.TargetPriority,
                "TargetPriority танка должен быть Buildings.");
        }
    }
}
