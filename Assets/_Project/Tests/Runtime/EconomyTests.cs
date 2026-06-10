using System.Collections;
using DiplomaGame.Runtime.Buildings;
using DiplomaGame.Runtime.Combat;
using DiplomaGame.Runtime.Data;
using DiplomaGame.Runtime.Economy;
using DiplomaGame.Runtime.Units;
using NUnit.Framework;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace DiplomaGame.Tests.Runtime
{
    /// <summary>
    /// PlayMode-тесты для системы экономики и зданий M5.
    /// Создают NavMesh-плоскость и проверяют ResourceBank, ResourceNode,
    /// Building-доход и ProductionBuilding.
    /// </summary>
    [TestFixture]
    public class EconomyTests
    {
        // ----------------------------------------------------------------
        // Общая инфраструктура
        // ----------------------------------------------------------------

        private GameObject     _groundGo;
        private NavMeshSurface _surface;

        [SetUp]
        public void SetUp()
        {
            _groundGo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            _groundGo.name = "EconomyTestGround";
            _groundGo.transform.position   = Vector3.zero;
            _groundGo.transform.localScale = new Vector3(5f, 1f, 5f); // 50x50

            _surface = _groundGo.AddComponent<NavMeshSurface>();
            _surface.BuildNavMesh();

            // Маркеры баз — как в продакшен-сцене
            var playerBase = new GameObject("PlayerBaseSpawn");
            playerBase.transform.position = new Vector3(-15f, 0f, -15f);
            var enemyBase = new GameObject("EnemyBaseSpawn");
            enemyBase.transform.position = new Vector3(15f, 0f, 15f);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (go.name.StartsWith("EconomyTest") ||
                    go.name.StartsWith("ResourceBank") ||
                    go.name.StartsWith("TestBuilding") ||
                    go.name.StartsWith("TestBarracks") ||
                    go.name.StartsWith("TestUnit")     ||
                    go.name.StartsWith("ResourceNode") ||
                    go.name.EndsWith("BaseSpawn"))
                {
                    Object.Destroy(go);
                }
            }

            Object.Destroy(_groundGo);
            NavMesh.RemoveAllNavMeshData();
        }

        // ----------------------------------------------------------------
        // Вспомогательные фабрики
        // ----------------------------------------------------------------

        private static ResourceBank CreateBank(int playerBalance = 150, int enemyBalance = 150)
        {
            var go   = new GameObject("ResourceBankGO");
            var bank = go.AddComponent<ResourceBank>();
            bank.InitForTest(playerBalance, enemyBalance);
            return bank;
        }

        private static Building CreateBuilding(
            BuildingData data,
            Faction      faction,
            ResourceBank bank,
            Vector3      position)
        {
            var go       = new GameObject("TestBuilding");
            go.transform.position = position;
            go.AddComponent<Health>();
            var building = go.AddComponent<Building>();
            building.InitForTest(data, faction, bank);
            return building;
        }

        // ================================================================
        // ResourceBank: базовые тесты
        // ================================================================

        [TestFixture]
        public class ResourceBankTests
        {
            private GameObject   _go;
            private ResourceBank _bank;

            [SetUp]
            public void SetUp()
            {
                _go   = new GameObject("ResourceBankTest");
                _bank = _go.AddComponent<ResourceBank>();
                _bank.InitForTest(100, 100);
            }

            [TearDown]
            public void TearDown()
            {
                Object.Destroy(_go);
            }

            [Test]
            public void GetBalance_ReturnsInitialBalance()
            {
                Assert.AreEqual(100, _bank.GetBalance(Faction.Player));
                Assert.AreEqual(100, _bank.GetBalance(Faction.Enemy));
            }

            [Test]
            public void Add_IncreasesBalance()
            {
                _bank.Add(Faction.Player, 50);
                Assert.AreEqual(150, _bank.GetBalance(Faction.Player));
            }

            [Test]
            public void Add_FiresBalanceChangedEvent()
            {
                Faction capturedFaction = Faction.Enemy;
                int     capturedBalance = -1;
                _bank.BalanceChanged += (f, b) =>
                {
                    capturedFaction = f;
                    capturedBalance = b;
                };

                _bank.Add(Faction.Player, 30);

                Assert.AreEqual(Faction.Player, capturedFaction, "Событие должно передать фракцию Player.");
                Assert.AreEqual(130, capturedBalance, "Событие должно передать новый баланс 130.");
            }

            [Test]
            public void TrySpend_SufficientBalance_ReturnsTrueAndDeductsBalance()
            {
                bool result = _bank.TrySpend(Faction.Player, 60);

                Assert.IsTrue(result, "TrySpend должен вернуть true при достаточном балансе.");
                Assert.AreEqual(40, _bank.GetBalance(Faction.Player), "Баланс должен уменьшиться на 60.");
            }

            [Test]
            public void TrySpend_InsufficientBalance_ReturnsFalseAndBalanceUnchanged()
            {
                bool result = _bank.TrySpend(Faction.Player, 200);

                Assert.IsFalse(result, "TrySpend должен вернуть false при нехватке средств.");
                Assert.AreEqual(100, _bank.GetBalance(Faction.Player),
                    "Баланс не должен измениться при неудачном списании.");
            }

            [Test]
            public void TrySpend_ExactBalance_ReturnsTrueAndZeroBalance()
            {
                bool result = _bank.TrySpend(Faction.Player, 100);

                Assert.IsTrue(result, "Списание всего баланса должно быть успешным.");
                Assert.AreEqual(0, _bank.GetBalance(Faction.Player), "Баланс после списания должен быть 0.");
            }

            [Test]
            public void TrySpend_FiresBalanceChangedOnSuccess()
            {
                int capturedBalance = -1;
                _bank.BalanceChanged += (f, b) => capturedBalance = b;

                _bank.TrySpend(Faction.Player, 40);

                Assert.AreEqual(60, capturedBalance, "Событие должно передать новый баланс 60.");
            }

            [Test]
            public void TrySpend_DoesNotFireEventOnFailure()
            {
                bool eventFired = false;
                _bank.BalanceChanged += (f, b) => eventFired = true;

                _bank.TrySpend(Faction.Player, 200); // нехватка

                Assert.IsFalse(eventFired, "Событие не должно сработать при неудачном TrySpend.");
            }

            [Test]
            public void Add_NegativeAmount_Ignored()
            {
                _bank.Add(Faction.Player, -50);
                Assert.AreEqual(100, _bank.GetBalance(Faction.Player),
                    "Отрицательное значение не должно изменять баланс.");
            }
        }

        // ================================================================
        // ResourceNode
        // ================================================================

        [TestFixture]
        public class ResourceNodeTests
        {
            private GameObject   _go;
            private ResourceNode _node;

            [SetUp]
            public void SetUp()
            {
                _go   = new GameObject("ResourceNodeTest");
                _node = _go.AddComponent<ResourceNode>();
                _node.InitForTest(100);
            }

            [TearDown]
            public void TearDown()
            {
                Object.Destroy(_go);
            }

            [Test]
            public void Remaining_ReturnsInitialReserve()
            {
                Assert.AreEqual(100, _node.Remaining);
            }

            [Test]
            public void ExtractUpTo_PartialAmount_DecreasesRemaining()
            {
                int extracted = _node.ExtractUpTo(30);

                Assert.AreEqual(30, extracted, "Должно вернуть запрошенное количество.");
                Assert.AreEqual(70, _node.Remaining, "Остаток должен уменьшиться на 30.");
            }

            [Test]
            public void ExtractUpTo_MoreThanRemaining_ReturnsActualAmount()
            {
                int extracted = _node.ExtractUpTo(150);

                Assert.AreEqual(100, extracted, "Нельзя добыть больше, чем есть в ноде.");
                Assert.AreEqual(0, _node.Remaining, "Остаток должен стать 0.");
            }

            [Test]
            public void ExtractUpTo_ExhaustedNode_ReturnsZero()
            {
                _node.ExtractUpTo(100); // исчерпываем
                int extracted = _node.ExtractUpTo(10);

                Assert.AreEqual(0, extracted, "Из исчерпанной ноды нельзя добыть ресурсы.");
                Assert.AreEqual(0, _node.Remaining, "Remaining не должен уйти ниже 0.");
            }

            [Test]
            public void ExtractUpTo_ZeroAmount_ReturnsZero()
            {
                int extracted = _node.ExtractUpTo(0);

                Assert.AreEqual(0, extracted, "Добыча 0 единиц возвращает 0.");
                Assert.AreEqual(100, _node.Remaining, "Remaining не должен изменяться.");
            }
        }

        // ================================================================
        // Building-доход: HQ пассивно добавляет ресурсы
        // ================================================================

        [UnityTest]
        public IEnumerator Building_HQ_PassiveIncome_IncreasesBalance()
        {
            var bank = CreateBank(0, 0);

            var data = BuildingData.CreateForTest(
                displayName:      "TestHQ",
                buildingType:     BuildingType.Headquarters,
                maxHp:            1000f,
                incomePerTick:    10,
                incomeTickInterval: 0.2f);  // тик каждые 0.2с

            CreateBuilding(data, Faction.Player, bank, Vector3.zero);

            int initialBalance = bank.GetBalance(Faction.Player);

            // Ждём ~0.5с — должно пройти минимум 2 тика
            yield return new WaitForSeconds(0.5f);

            int newBalance = bank.GetBalance(Faction.Player);
            Assert.Greater(newBalance, initialBalance,
                "Через 0.5с баланс игрока должен вырасти благодаря пассивному доходу HQ.");
        }

        // ================================================================
        // ProductionBuilding: TryEnqueue → юнит появляется, баланс списан
        // ================================================================

        [UnityTest]
        public IEnumerator ProductionBuilding_TryEnqueue_ProducesUnit()
        {
            var bank = CreateBank(150, 150);

            // Создаём простейший шаблон юнита (неактивный, инстанцируется при спавне)
            var unitTemplate = new GameObject("TestUnit_Template");
            unitTemplate.AddComponent<NavMeshAgent>();
            unitTemplate.AddComponent<Unit>();
            unitTemplate.SetActive(false);

            var barracksData = BuildingData.CreateForTest(
                displayName:      "TestBarracks",
                buildingType:     BuildingType.Barracks,
                maxHp:            500f,
                productionCost:   50,
                productionTime:   0.3f);  // быстрое производство для теста

            var buildingGo  = new GameObject("TestBarracks");
            buildingGo.transform.position = new Vector3(0f, 0f, 0f);
            buildingGo.AddComponent<Health>();

            var building = buildingGo.AddComponent<Building>();
            building.InitForTest(barracksData, Faction.Player, bank);

            var prodBuilding = buildingGo.AddComponent<ProductionBuilding>();
            prodBuilding.InitForTest(unitTemplate, bank);

            int balanceBefore = bank.GetBalance(Faction.Player);

            // Добавляем в очередь
            bool enqueued = prodBuilding.TryEnqueue();

            Assert.IsTrue(enqueued, "TryEnqueue должен вернуть true при достаточном балансе.");
            Assert.AreEqual(balanceBefore - 50, bank.GetBalance(Faction.Player),
                "После TryEnqueue баланс должен уменьшиться на productionCost=50.");
            Assert.AreEqual(1, prodBuilding.QueueCount, "В очереди должен быть 1 юнит.");

            // Ждём дольше productionTime
            yield return new WaitForSeconds(0.6f);

            Assert.AreEqual(0, prodBuilding.QueueCount,
                "После завершения производства очередь должна быть пустой.");

            // Проверяем, что юнит заспавнился (ищем активный TestUnit_Template)
            var spawnedUnits = Object.FindObjectsByType<Unit>(FindObjectsSortMode.None);
            bool found = false;
            foreach (var u in spawnedUnits)
            {
                if (u.gameObject.activeSelf && u.gameObject != buildingGo)
                {
                    found = true;
                    break;
                }
            }
            Assert.IsTrue(found, "Произведённый юнит должен быть активен в сцене.");

            // Убираем шаблон
            Object.Destroy(unitTemplate);
        }

        [Test]
        public void ProductionBuilding_TryEnqueue_InsufficientBalance_ReturnsFalse()
        {
            var bank = CreateBank(10, 10); // слишком мало

            var barracksData = BuildingData.CreateForTest(
                displayName:    "TestBarracks",
                buildingType:   BuildingType.Barracks,
                maxHp:          500f,
                productionCost: 50,
                productionTime: 5f);

            var buildingGo = new GameObject("TestBarracks_NoBudget");
            buildingGo.AddComponent<Health>();
            var building = buildingGo.AddComponent<Building>();
            building.InitForTest(barracksData, Faction.Player, bank);

            var prodBuilding = buildingGo.AddComponent<ProductionBuilding>();
            prodBuilding.InitForTest(null, bank);

            bool enqueued = prodBuilding.TryEnqueue();

            Assert.IsFalse(enqueued,
                "TryEnqueue при нехватке ресурсов должен вернуть false.");
            Assert.AreEqual(10, bank.GetBalance(Faction.Player),
                "Баланс не должен измениться при неудачном TryEnqueue.");
            Assert.AreEqual(0, prodBuilding.QueueCount,
                "Очередь не должна заполниться при неудачном TryEnqueue.");

            Object.Destroy(buildingGo);
        }
    }
}
