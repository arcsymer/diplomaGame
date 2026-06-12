using DiplomaGame.Runtime.AI;
using NUnit.Framework;

namespace DiplomaGame.Tests.Editor
{
    /// <summary>
    /// EditMode-тесты для EnemyWaveLogic — чистая статика, не требует сцены.
    /// </summary>
    [TestFixture]
    public class EnemyWaveLogicTests
    {
        // ================================================================
        // ShouldProduce
        // ================================================================

        [Test]
        public void ShouldProduce_SufficientBalance_BelowLimit_ReturnsTrue()
        {
            Assert.IsTrue(
                EnemyWaveLogic.ShouldProduce(balance: 100, unitCost: 50, currentUnits: 5, maxUnits: 12),
                "При достаточном балансе и незаполненном лимите должны производить.");
        }

        [Test]
        public void ShouldProduce_InsufficientBalance_ReturnsFalse()
        {
            Assert.IsFalse(
                EnemyWaveLogic.ShouldProduce(balance: 30, unitCost: 50, currentUnits: 5, maxUnits: 12),
                "При недостаточном балансе производство невозможно.");
        }

        [Test]
        public void ShouldProduce_AtLimit_ReturnsFalse()
        {
            Assert.IsFalse(
                EnemyWaveLogic.ShouldProduce(balance: 1000, unitCost: 50, currentUnits: 12, maxUnits: 12),
                "При достижении лимита юнитов производство должно остановиться.");
        }

        [Test]
        public void ShouldProduce_OverLimit_ReturnsFalse()
        {
            Assert.IsFalse(
                EnemyWaveLogic.ShouldProduce(balance: 1000, unitCost: 50, currentUnits: 15, maxUnits: 12),
                "При превышении лимита производство невозможно.");
        }

        [Test]
        public void ShouldProduce_ExactBalance_ReturnsTrue()
        {
            // Баланс ровно равен стоимости — граничный случай
            Assert.IsTrue(
                EnemyWaveLogic.ShouldProduce(balance: 50, unitCost: 50, currentUnits: 0, maxUnits: 12),
                "Баланс ровно равен стоимости — производство разрешено.");
        }

        [Test]
        public void ShouldProduce_ZeroUnitCost_ReturnsFalse()
        {
            // unitCost <= 0 — некорректные данные, не производим
            Assert.IsFalse(
                EnemyWaveLogic.ShouldProduce(balance: 100, unitCost: 0, currentUnits: 0, maxUnits: 12),
                "При нулевой стоимости производство некорректно — должно вернуть false.");
        }

        // ================================================================
        // ShouldLaunchWave
        // ================================================================

        [Test]
        public void ShouldLaunchWave_EnoughUnits_ReturnsTrue()
        {
            // Накопилось waveSize юнитов — атакуем немедленно
            Assert.IsTrue(
                EnemyWaveLogic.ShouldLaunchWave(idleCombatUnits: 5, waveSize: 5,
                    timeSinceLastWave: 5f, maxWaitTime: 30f),
                "При накоплении waveSize юнитов должна запуститься волна.");
        }

        [Test]
        public void ShouldLaunchWave_MoreThanWaveSize_ReturnsTrue()
        {
            Assert.IsTrue(
                EnemyWaveLogic.ShouldLaunchWave(idleCombatUnits: 8, waveSize: 5,
                    timeSinceLastWave: 5f, maxWaitTime: 30f),
                "При превышении waveSize волна также должна запускаться.");
        }

        [Test]
        public void ShouldLaunchWave_TimeoutWithAtLeastTwo_ReturnsTrue()
        {
            // Вышло время, есть 2+ юнита — атакуем даже без полной волны
            Assert.IsTrue(
                EnemyWaveLogic.ShouldLaunchWave(idleCombatUnits: 2, waveSize: 5,
                    timeSinceLastWave: 30f, maxWaitTime: 30f),
                "При истечении maxWaitTime и наличии 2+ юнитов волна запускается.");
        }

        [Test]
        public void ShouldLaunchWave_TimeoutButOnlyOne_ReturnsFalse()
        {
            // Только 1 юнит — минимум не достигнут
            Assert.IsFalse(
                EnemyWaveLogic.ShouldLaunchWave(idleCombatUnits: 1, waveSize: 5,
                    timeSinceLastWave: 60f, maxWaitTime: 30f),
                "При истечении maxWaitTime, но только 1 юните, волна не запускается.");
        }

        [Test]
        public void ShouldLaunchWave_ZeroUnits_ReturnsFalse()
        {
            Assert.IsFalse(
                EnemyWaveLogic.ShouldLaunchWave(idleCombatUnits: 0, waveSize: 3,
                    timeSinceLastWave: 999f, maxWaitTime: 30f),
                "При отсутствии юнитов волна не запускается.");
        }

        [Test]
        public void ShouldLaunchWave_NotEnoughUnitsAndTimeNotExpired_ReturnsFalse()
        {
            Assert.IsFalse(
                EnemyWaveLogic.ShouldLaunchWave(idleCombatUnits: 3, waveSize: 5,
                    timeSinceLastWave: 15f, maxWaitTime: 30f),
                "Недостаточно юнитов, время не вышло — волна не запускается.");
        }

        // ================================================================
        // GetWaveSizeForTime
        // ================================================================

        [Test]
        public void GetWaveSizeForTime_Below3Min_Returns3()
        {
            Assert.AreEqual(3, EnemyWaveLogic.GetWaveSizeForTime(0f));
            Assert.AreEqual(3, EnemyWaveLogic.GetWaveSizeForTime(90f));
            Assert.AreEqual(3, EnemyWaveLogic.GetWaveSizeForTime(179.9f),
                "Прямо перед 3 мин — всё ещё 3.");
        }

        [Test]
        public void GetWaveSizeForTime_AtExact3Min_Returns5()
        {
            // 180 секунд — граница: ровно 3 минуты → переходим в диапазон 3–7 мин
            Assert.AreEqual(5, EnemyWaveLogic.GetWaveSizeForTime(180f),
                "Ровно 3 минуты — размер волны должен стать 5.");
        }

        [Test]
        public void GetWaveSizeForTime_Between3And7Min_Returns5()
        {
            Assert.AreEqual(5, EnemyWaveLogic.GetWaveSizeForTime(300f));
            Assert.AreEqual(5, EnemyWaveLogic.GetWaveSizeForTime(419.9f),
                "Прямо перед 7 мин — всё ещё 5.");
        }

        [Test]
        public void GetWaveSizeForTime_AtExact7Min_Returns7()
        {
            // 420 секунд — граница: ровно 7 минут → максимальный размер
            Assert.AreEqual(7, EnemyWaveLogic.GetWaveSizeForTime(420f),
                "Ровно 7 минут — размер волны должен стать 7.");
        }

        [Test]
        public void GetWaveSizeForTime_After7Min_Returns7()
        {
            Assert.AreEqual(7, EnemyWaveLogic.GetWaveSizeForTime(600f));
            Assert.AreEqual(7, EnemyWaveLogic.GetWaveSizeForTime(3600f));
        }

        // ================================================================
        // ShouldResearch (v7)
        // ================================================================

        [Test]
        public void ShouldResearch_SufficientBalance_ReturnsTrue()
        {
            // 300 >= 150 + 50 = 200
            Assert.IsTrue(
                EnemyWaveLogic.ShouldResearch(balance: 300, techCost: 150),
                "При балансе >= techCost + 50 исследование разрешено.");
        }

        [Test]
        public void ShouldResearch_InsufficientBuffer_ReturnsFalse()
        {
            // 180 < 150 + 50 = 200
            Assert.IsFalse(
                EnemyWaveLogic.ShouldResearch(balance: 180, techCost: 150),
                "При балансе < techCost + 50 исследование запрещено (резерв 50 не покрывается).");
        }
    }
}
