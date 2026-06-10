using DiplomaGame.Runtime.Buildings;
using DiplomaGame.Runtime.Economy;
using NUnit.Framework;

namespace DiplomaGame.Tests.Editor
{
    /// <summary>
    /// EditMode-тесты для чистой логики экономики и размещения зданий M5.
    /// </summary>
    [TestFixture]
    public class EconomyLogicTests
    {
        // ================================================================
        // EconomyLogic.CanAfford
        // ================================================================

        [Test]
        public void CanAfford_SufficientBalance_ReturnsTrue()
        {
            Assert.IsTrue(EconomyLogic.CanAfford(100, 50),
                "balance=100 >= cost=50 → true");
        }

        [Test]
        public void CanAfford_ExactBalance_ReturnsTrue()
        {
            Assert.IsTrue(EconomyLogic.CanAfford(50, 50),
                "balance == cost → true (граничный случай)");
        }

        [Test]
        public void CanAfford_InsufficientBalance_ReturnsFalse()
        {
            Assert.IsFalse(EconomyLogic.CanAfford(30, 50),
                "balance=30 < cost=50 → false");
        }

        [Test]
        public void CanAfford_ZeroCost_ReturnsTrue()
        {
            Assert.IsTrue(EconomyLogic.CanAfford(0, 0),
                "balance=0, cost=0 → true");
        }

        [Test]
        public void CanAfford_ZeroBalance_ZeroCost_ReturnsTrue()
        {
            Assert.IsTrue(EconomyLogic.CanAfford(0, 0));
        }

        // ================================================================
        // EconomyLogic.Spend
        // ================================================================

        [Test]
        public void Spend_NormalCase_SubtractsCost()
        {
            int result = EconomyLogic.Spend(100, 50);
            Assert.AreEqual(50, result, "100 - 50 = 50");
        }

        [Test]
        public void Spend_ExactBalance_ReturnsZero()
        {
            int result = EconomyLogic.Spend(50, 50);
            Assert.AreEqual(0, result, "50 - 50 = 0");
        }

        [Test]
        public void Spend_OverspendProtection_ReturnsZeroNotNegative()
        {
            // Защита от случайного перерасхода (CanAfford должен был быть вызван раньше)
            int result = EconomyLogic.Spend(10, 100);
            Assert.AreEqual(0, result,
                "Spend не должен давать отрицательный результат — защита от ошибок вызывающей стороны");
        }

        // ================================================================
        // EconomyLogic.CalculateIncomeTicks
        // ================================================================

        [Test]
        public void CalculateIncomeTicks_LessThanOneInterval_ReturnsZero()
        {
            int ticks = EconomyLogic.CalculateIncomeTicks(0.9f, 1.0f, out float remainder);

            Assert.AreEqual(0, ticks,     "0.9s < 1.0s → 0 тиков");
            Assert.AreEqual(0.9f, remainder, 0.0001f, "Весь elapsed — это остаток");
        }

        [Test]
        public void CalculateIncomeTicks_ExactlyOneInterval_ReturnsOne()
        {
            int ticks = EconomyLogic.CalculateIncomeTicks(1.0f, 1.0f, out float remainder);

            Assert.AreEqual(1, ticks,    "1.0s / 1.0s = 1 тик");
            Assert.AreEqual(0f, remainder, 0.0001f, "Остаток = 0");
        }

        [Test]
        public void CalculateIncomeTicks_TwoAndHalfIntervals_ReturnsTwoWithRemainder()
        {
            int ticks = EconomyLogic.CalculateIncomeTicks(2.5f, 1.0f, out float remainder);

            Assert.AreEqual(2, ticks,    "2.5s / 1.0s = 2 тика (целая часть)");
            Assert.AreEqual(0.5f, remainder, 0.0001f, "Остаток = 0.5");
        }

        [Test]
        public void CalculateIncomeTicks_ZeroInterval_ReturnsZero()
        {
            int ticks = EconomyLogic.CalculateIncomeTicks(10f, 0f, out float remainder);

            Assert.AreEqual(0, ticks,
                "Нулевой интервал → 0 тиков (защита от деления на ноль)");
            Assert.AreEqual(0f, remainder, 0.0001f);
        }

        [Test]
        public void CalculateIncomeTicks_LagSpike_ReturnsMultipleTicks()
        {
            // Имитируем лаг: за 5 секунд при интервале 2с = 2 тика, остаток 1с
            int ticks = EconomyLogic.CalculateIncomeTicks(5f, 2f, out float remainder);

            Assert.AreEqual(2, ticks,    "5s / 2s = 2 тика");
            Assert.AreEqual(1f, remainder, 0.0001f, "Остаток = 1с");
        }

        // ================================================================
        // PlacementLogic.IsPlacementValid
        // ================================================================

        [Test]
        [TestCase(false, false, false, true,  TestName = "Free spot, no node required → valid")]
        [TestCase(true,  false, false, false, TestName = "Overlaps → invalid")]
        [TestCase(false, true,  true,  true,  TestName = "Extractor, node nearby → valid")]
        [TestCase(false, true,  false, false, TestName = "Extractor, no node → invalid")]
        [TestCase(true,  true,  true,  false, TestName = "Overlaps + node nearby → still invalid (overlap wins)")]
        public void IsPlacementValid_TableCases(bool overlaps, bool needsNode, bool hasNodeNearby, bool expected)
        {
            bool result = PlacementLogic.IsPlacementValid(overlaps, needsNode, hasNodeNearby);
            Assert.AreEqual(expected, result);
        }

        // ================================================================
        // ProductionQueueLogic
        // ================================================================

        [Test]
        public void TickProgress_AddsTime()
        {
            float result = ProductionQueueLogic.TickProgress(2.5f, 0.3f);
            Assert.AreEqual(2.8f, result, 0.0001f, "TickProgress должен прибавлять deltaTime");
        }

        [Test]
        public void IsComplete_BelowProductionTime_ReturnsFalse()
        {
            Assert.IsFalse(ProductionQueueLogic.IsComplete(2.9f, 3f),
                "progress 2.9 < productionTime 3 → не готово");
        }

        [Test]
        public void IsComplete_ExactProductionTime_ReturnsTrue()
        {
            Assert.IsTrue(ProductionQueueLogic.IsComplete(3f, 3f),
                "progress == productionTime → готово");
        }

        [Test]
        public void IsComplete_ExceedsProductionTime_ReturnsTrue()
        {
            Assert.IsTrue(ProductionQueueLogic.IsComplete(3.5f, 3f),
                "progress > productionTime → тоже готово");
        }

        [Test]
        public void IsComplete_ZeroProductionTime_ReturnsFalse()
        {
            Assert.IsFalse(ProductionQueueLogic.IsComplete(0f, 0f),
                "productionTime=0 → защита от случайного срабатывания");
        }
    }
}
