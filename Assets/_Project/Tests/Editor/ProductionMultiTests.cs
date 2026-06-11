using DiplomaGame.Runtime.AI;
using DiplomaGame.Runtime.Buildings;
using DiplomaGame.Runtime.Combat;
using DiplomaGame.Runtime.Data;
using DiplomaGame.Runtime.Economy;
using DiplomaGame.Runtime.Units;
using NUnit.Framework;
using UnityEngine;

namespace DiplomaGame.Tests.Editor
{
    /// <summary>
    /// EditMode-тесты для v6:
    /// • ProductionEntry — TryEnqueue с entry списывает cost; legacy fallback; очередь полная
    /// • EnemyWaveLogic.PickProductionEntryIndex
    /// • UnitData.FireWhileRetreating (дефолт true)
    /// </summary>
    [TestFixture]
    public class ProductionMultiTests
    {
        // ================================================================
        // TryEnqueue(ProductionEntry) — списание cost
        // ================================================================

        [Test]
        public void TryEnqueue_WithEntry_SpendsCost()
        {
            // Создаём компоненты вручную (Awake не вызывается в EditMode без сцены)
            var go       = new GameObject("TestBarracks");
            var bank     = go.AddComponent<ResourceBank>();
            bank.InitForTest(playerBalance: 200, enemyBalance: 0);

            // Building нужен ProductionBuilding (RequireComponent)
            var health   = go.AddComponent<Health>();
            var building = go.AddComponent<Building>();
            // InitForTest задаёт фракцию — нам нужен Faction.Player
            var bData    = BuildingData.CreateForTest(buildingType: BuildingType.Barracks);
            building.InitForTest(bData, Faction.Player, bank);

            var prod = go.AddComponent<ProductionBuilding>();
            prod.InitForTest(unitPrefab: null, bank: bank);

            var unitData = UnitData.CreateForTest(displayName: "Marine");
            var entry    = new ProductionEntry { unitData = unitData, cost = 50, productionTime = 5f, hotkeyLabel = "T" };

            bool result = prod.TryEnqueue(entry);

            Assert.IsTrue(result, "TryEnqueue с достаточным балансом должен вернуть true.");
            Assert.AreEqual(150, bank.GetBalance(Faction.Player),
                "После TryEnqueue баланс должен уменьшиться на 50 (200 - 50 = 150).");
            Assert.AreEqual(1, prod.QueueCount, "В очереди должен быть 1 элемент.");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void TryEnqueue_InsufficientFunds_ReturnsFalse()
        {
            var go       = new GameObject("TestBarracks2");
            var bank     = go.AddComponent<ResourceBank>();
            bank.InitForTest(playerBalance: 30, enemyBalance: 0);

            go.AddComponent<Health>();
            var building = go.AddComponent<Building>();
            var bData    = BuildingData.CreateForTest(buildingType: BuildingType.Barracks);
            building.InitForTest(bData, Faction.Player, bank);

            var prod = go.AddComponent<ProductionBuilding>();
            prod.InitForTest(unitPrefab: null, bank: bank);

            var entry = new ProductionEntry { unitData = null, cost = 50, productionTime = 5f };

            bool result = prod.TryEnqueue(entry);

            Assert.IsFalse(result, "При недостатке средств TryEnqueue должен вернуть false.");
            Assert.AreEqual(30, bank.GetBalance(Faction.Player), "Баланс не должен измениться.");
            Assert.AreEqual(0, prod.QueueCount, "Очередь должна оставаться пустой.");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void TryEnqueue_QueueFull_ReturnsFalse()
        {
            var go   = new GameObject("TestBarracks3");
            var bank = go.AddComponent<ResourceBank>();
            bank.InitForTest(playerBalance: 10000, enemyBalance: 0);

            go.AddComponent<Health>();
            var building = go.AddComponent<Building>();
            var bData    = BuildingData.CreateForTest(buildingType: BuildingType.Barracks);
            building.InitForTest(bData, Faction.Player, bank);

            var prod = go.AddComponent<ProductionBuilding>();
            prod.InitForTest(unitPrefab: null, bank: bank);

            var entry = new ProductionEntry { unitData = null, cost = 1, productionTime = 5f };

            // Заполняем очередь до максимума (MaxQueueSize = 5)
            for (int i = 0; i < 5; i++)
                prod.TryEnqueue(entry);

            Assert.AreEqual(5, prod.QueueCount, "Очередь должна содержать ровно 5 элементов.");

            bool result = prod.TryEnqueue(entry);

            Assert.IsFalse(result, "При полной очереди TryEnqueue должен вернуть false.");
            Assert.AreEqual(5, prod.QueueCount, "Очередь не должна вырасти сверх 5.");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void TryEnqueue_Legacy_UsesProductionCostFromBuildingData()
        {
            var go   = new GameObject("TestBarracks4");
            var bank = go.AddComponent<ResourceBank>();
            bank.InitForTest(playerBalance: 200, enemyBalance: 0);

            go.AddComponent<Health>();
            var building = go.AddComponent<Building>();
            // legacy: no productionEntries
            var bData    = BuildingData.CreateForTest(
                buildingType:   BuildingType.Barracks,
                productionCost: 75,
                productionTime: 5f);
            building.InitForTest(bData, Faction.Player, bank);

            var prod = go.AddComponent<ProductionBuilding>();
            prod.InitForTest(unitPrefab: null, bank: bank);

            bool result = prod.TryEnqueue();

            Assert.IsTrue(result, "Legacy TryEnqueue() должен вернуть true при достаточном балансе.");
            Assert.AreEqual(125, bank.GetBalance(Faction.Player),
                "Legacy: баланс 200 - 75 = 125.");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void TryEnqueue_Legacy_WithMultiProduction_UsesFallbackEntry0()
        {
            // Если HasMultiProduction, legacy TryEnqueue() должен использовать entries[0]
            var go   = new GameObject("TestBarracks5");
            var bank = go.AddComponent<ResourceBank>();
            bank.InitForTest(playerBalance: 200, enemyBalance: 0);

            go.AddComponent<Health>();
            var building = go.AddComponent<Building>();
            var entry0   = new ProductionEntry { unitData = null, cost = 60, productionTime = 5f };
            var bData    = BuildingData.CreateForTest(
                buildingType:      BuildingType.Barracks,
                productionCost:    99, // legacy cost — не должен использоваться
                productionEntries: new[] { entry0 });
            building.InitForTest(bData, Faction.Player, bank);

            var prod = go.AddComponent<ProductionBuilding>();
            prod.InitForTest(unitPrefab: null, bank: bank);

            bool result = prod.TryEnqueue();

            Assert.IsTrue(result, "TryEnqueue() при HasMultiProduction должен использовать entries[0].");
            Assert.AreEqual(140, bank.GetBalance(Faction.Player),
                "Списан cost entries[0]=60: 200-60=140, НЕ legacy 99.");

            Object.DestroyImmediate(go);
        }

        // ================================================================
        // EnemyWaveLogic.PickProductionEntryIndex
        // ================================================================

        [Test]
        public void PickProductionEntryIndex_NoUnits_ReturnsZero()
        {
            int idx = EnemyWaveLogic.PickProductionEntryIndex(infantryCount: 0, tankCount: 0);
            Assert.AreEqual(0, idx,
                "Нет юнитов — нужна пехота (index 0).");
        }

        [Test]
        public void PickProductionEntryIndex_OnlyTanks_NoInfantry_ReturnsZero()
        {
            // 0 пехоты, 5 танков → infantry(0) < ratio(3) * (5+1)=18 → index 0
            int idx = EnemyWaveLogic.PickProductionEntryIndex(infantryCount: 0, tankCount: 5);
            Assert.AreEqual(0, idx,
                "Нет пехоты при 5 танках — нужна пехота (index 0).");
        }

        [Test]
        public void PickProductionEntryIndex_InfantryBelowRatio_ReturnsZero()
        {
            // 2 пехоты, 1 танк, ratio=3: нужно infantry >= 3*(1+1)=6
            // 2 < 6 → index 0
            int idx = EnemyWaveLogic.PickProductionEntryIndex(infantryCount: 2, tankCount: 1);
            Assert.AreEqual(0, idx,
                "2 пехоты при 1 танке (ratio=3) — нужна пехота (index 0).");
        }

        [Test]
        public void PickProductionEntryIndex_InfantryOverflow_ReturnsOne()
        {
            // 9 пехоты, 2 танка, ratio=3: 9 >= 3*(2+1)=9 → index 1
            int idx = EnemyWaveLogic.PickProductionEntryIndex(infantryCount: 9, tankCount: 2);
            Assert.AreEqual(1, idx,
                "9 пехоты при 2 танках (ratio=3) — нужен танк (index 1).");
        }

        [Test]
        public void PickProductionEntryIndex_RatioBoundaryExact_ReturnsOne()
        {
            // 3 пехоты, 0 танков, ratio=3: 3 >= 3*(0+1)=3 → index 1
            int idx = EnemyWaveLogic.PickProductionEntryIndex(infantryCount: 3, tankCount: 0);
            Assert.AreEqual(1, idx,
                "Точно на границе ratio (3 пехоты, ratio=3, 0 танков) — должен вернуть index 1.");
        }

        // ================================================================
        // UnitData.FireWhileRetreating — дефолт true
        // ================================================================

        [Test]
        public void UnitData_FireWhileRetreating_DefaultIsTrue()
        {
            var data = UnitData.CreateForTest();
            Assert.IsTrue(data.FireWhileRetreating,
                "FireWhileRetreating по умолчанию должен быть true.");
        }

        [Test]
        public void UnitData_FireWhileRetreating_CanBeSetFalse()
        {
            var data = UnitData.CreateForTest(fireWhileRetreating: false);
            Assert.IsFalse(data.FireWhileRetreating,
                "При явном false FireWhileRetreating должен быть false.");
        }
    }
}
