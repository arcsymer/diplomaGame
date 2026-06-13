using DiplomaGame.Runtime.AI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DiplomaGame.Tests.Editor
{
    /// <summary>
    /// EditMode-тесты для DifficultyProfileSO и ShouldResearchWithReserve.
    /// </summary>
    [TestFixture]
    public class DifficultyProfileTests
    {
        // ================================================================
        // WaveSizeScale — проверяем, что масштаб применяется корректно
        // ================================================================

        [Test]
        public void WaveScale_Easy_ReducesBaseSize()
        {
            // Easy: waveSizeScale = 0.6
            var profile = DifficultyProfileSO.CreateForTest(
                displayName: "Легко",
                decisionInterval: 4.0f,
                waveSizeScale: 0.6f,
                maxWaitTime: 50f,
                maxUnits: 8,
                researchReserve: -1,
                infantryRatio: 3,
                enemyStartingBonusGold: 0);

            int baseSize = EnemyWaveLogic.GetWaveSizeForTime(0f); // = 3
            int scaled   = Mathf.Max(1, Mathf.RoundToInt(baseSize * profile.WaveSizeScale));

            Assert.AreEqual(2, scaled,
                "Easy: 3 * 0.6 = 1.8 → округляет до 2.");
        }

        [Test]
        public void WaveScale_Hard_IncreasesBaseSize()
        {
            // Hard: waveSizeScale = 1.4
            var profile = DifficultyProfileSO.CreateForTest(
                displayName: "Сложно",
                decisionInterval: 1.0f,
                waveSizeScale: 1.4f,
                maxWaitTime: 20f,
                maxUnits: 16,
                researchReserve: 25,
                infantryRatio: 2,
                enemyStartingBonusGold: 100);

            int baseSize = EnemyWaveLogic.GetWaveSizeForTime(0f); // = 3
            int scaled   = Mathf.Max(1, Mathf.RoundToInt(baseSize * profile.WaveSizeScale));

            Assert.AreEqual(4, scaled,
                "Hard: 3 * 1.4 = 4.2 → округляет до 4.");
        }

        // ================================================================
        // ShouldResearchWithReserve — базовые контракты
        // ================================================================

        [Test]
        public void ShouldResearchWithReserve_ReserveMinusOne_ReturnsFalse()
        {
            // reserve = -1 → «никогда не исследовать»
            Assert.IsFalse(
                EnemyWaveLogic.ShouldResearchWithReserve(balance: 9999, techCost: 100, reserve: -1),
                "reserve = -1 означает «никогда не исследовать».");
        }

        [Test]
        public void ShouldResearchWithReserve_ReserveZero_TrueWhenBalanceEqualsTechCost()
        {
            // reserve = 0: достаточно просто покрыть techCost
            Assert.IsTrue(
                EnemyWaveLogic.ShouldResearchWithReserve(balance: 100, techCost: 100, reserve: 0),
                "reserve = 0: balance == techCost → разрешено.");
        }

        [Test]
        public void ShouldResearchWithReserve_ReserveZero_FalseWhenBalanceBelowTechCost()
        {
            Assert.IsFalse(
                EnemyWaveLogic.ShouldResearchWithReserve(balance: 99, techCost: 100, reserve: 0),
                "reserve = 0: balance < techCost → запрещено.");
        }

        [Test]
        public void ShouldResearchWithReserve_ParityWithShouldResearch()
        {
            // reserve = 50 должен давать те же результаты, что ShouldResearch (который жёстко использует +50)
            int techCost = 150;

            bool withReserve = EnemyWaveLogic.ShouldResearchWithReserve(
                balance: 200, techCost: techCost, reserve: 50);
            bool original = EnemyWaveLogic.ShouldResearch(
                balance: 200, techCost: techCost);

            Assert.AreEqual(original, withReserve,
                "ShouldResearchWithReserve(reserve=50) обязан совпадать с ShouldResearch при тех же параметрах.");

            // Также проверяем граничный случай — недостаточный баланс
            bool withReserveNeg = EnemyWaveLogic.ShouldResearchWithReserve(
                balance: 180, techCost: techCost, reserve: 50);
            bool originalNeg = EnemyWaveLogic.ShouldResearch(
                balance: 180, techCost: techCost);

            Assert.AreEqual(originalNeg, withReserveNeg,
                "Граница: ShouldResearchWithReserve(reserve=50) должна совпадать с ShouldResearch.");
        }

        // ================================================================
        // Normal asset — проверяем значения через AssetDatabase
        // (Assert.Ignore если ассет ещё не создан Forge)
        // ================================================================

        private const string NormalAssetPath = "Assets/_Project/Data/Difficulty/DifficultyNormal.asset";

        [Test]
        public void NormalAsset_DecisionInterval_IsTwo()
        {
            var asset = AssetDatabase.LoadAssetAtPath<DifficultyProfileSO>(NormalAssetPath);
            if (asset == null)
            {
                Assert.Ignore("DifficultyNormal.asset не создан — запустите Setup Difficulty (v13) в Project Forge.");
                return;
            }

            Assert.AreEqual(2.0f, asset.DecisionInterval, 0.001f,
                "Normal: DecisionInterval должен быть 2.0.");
        }

        [Test]
        public void NormalAsset_MaxUnits_IsTwelve()
        {
            var asset = AssetDatabase.LoadAssetAtPath<DifficultyProfileSO>(NormalAssetPath);
            if (asset == null)
            {
                Assert.Ignore("DifficultyNormal.asset не создан — запустите Setup Difficulty (v13) в Project Forge.");
                return;
            }

            Assert.AreEqual(12, asset.MaxUnits,
                "Normal: MaxUnits должен быть 12 (совпадает с историческим хардкодом).");
        }

        [Test]
        public void NormalAsset_ResearchReserve_IsFifty()
        {
            var asset = AssetDatabase.LoadAssetAtPath<DifficultyProfileSO>(NormalAssetPath);
            if (asset == null)
            {
                Assert.Ignore("DifficultyNormal.asset не создан — запустите Setup Difficulty (v13) в Project Forge.");
                return;
            }

            Assert.AreEqual(50, asset.ResearchReserve,
                "Normal: ResearchReserve должен быть 50 (совпадает с историческим хардкодом ShouldResearch +50).");
        }
    }
}
