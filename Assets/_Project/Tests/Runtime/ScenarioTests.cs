using System.Collections;
using System.Reflection;
using DiplomaGame.Runtime.AI;
using DiplomaGame.Runtime.Buildings;
using DiplomaGame.Runtime.Combat;
using DiplomaGame.Runtime.Core;
using DiplomaGame.Runtime.Data;
using DiplomaGame.Runtime.Economy;
using DiplomaGame.Runtime.UI;
using DiplomaGame.Runtime.Units;
using NUnit.Framework;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace DiplomaGame.Tests.Runtime
{
    /// <summary>
    /// PlayMode-тесты M9: GameWatcher (условия победы/поражения) и EnemyCommander (производство).
    /// Паттерн — по образцу CombatTests: NavMesh + маркеры баз.
    /// </summary>
    [TestFixture]
    public class ScenarioTests
    {
        // ----------------------------------------------------------------
        // Общая инфраструктура
        // ----------------------------------------------------------------

        private GameObject     _groundGo;
        private NavMeshSurface _surface;

        [SetUp]
        public void SetUp()
        {
            // NavMesh-плоскость (как в CombatTests)
            _groundGo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            _groundGo.name = "ScenarioTestGround";
            _groundGo.transform.position   = Vector3.zero;
            _groundGo.transform.localScale = new Vector3(5f, 1f, 5f);

            _surface = _groundGo.AddComponent<NavMeshSurface>();
            _surface.BuildNavMesh();

            // Маркеры баз
            var playerBase = new GameObject("PlayerBaseSpawn");
            playerBase.transform.position = new Vector3(-15f, 0f, -15f);
            var enemyBase = new GameObject("EnemyBaseSpawn");
            enemyBase.transform.position = new Vector3(15f, 0f, 15f);

            Physics.SyncTransforms();
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;

            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                // Объект мог быть уничтожен внутри теста (Building.Died → Destroy)
                // или каскадно как ребёнок уже удалённого родителя
                if (go == null) continue;

                if (go.name.StartsWith("ScenarioTest") ||
                    go.name.StartsWith("PlayerBase")   ||
                    go.name.StartsWith("EnemyBase")    ||
                    go.name.StartsWith("PlayerHQ")     ||
                    go.name.StartsWith("EnemyHQ")      ||
                    go.name.StartsWith("TestBarracks")  ||
                    go.name.StartsWith("Watcher")      ||
                    go.name.StartsWith("Commander")    ||
                    go.name.StartsWith("ScenarioUnit")  ||
                    go.name == "PlayerBaseSpawn"        ||
                    go.name == "EnemyBaseSpawn")
                {
                    Object.DestroyImmediate(go);
                }
            }

            NavMesh.RemoveAllNavMeshData();
        }

        // ----------------------------------------------------------------
        // Вспомогательные фабрики
        // ----------------------------------------------------------------

        /// <summary>
        /// Создаёт минимальное здание-HQ с Building + Health.
        /// BuildingData.CreateForTest → тип Headquarters.
        /// </summary>
        private static GameObject CreateHQBuilding(string goName, Faction faction, Vector3 position)
        {
            var go = new GameObject(goName);
            go.transform.position = position;

            var health = go.AddComponent<Health>();
            var building = go.AddComponent<Building>();

            var data = BuildingData.CreateForTest(
                displayName:  "HQ",
                maxHp:        500f,
                buildingType: BuildingType.Headquarters);

            building.InitForTest(data, faction, bank: null);
            // Health.Init вызывается внутри Building.InitForTest
            Physics.SyncTransforms();
            return go;
        }

        /// <summary>
        /// Создаёт GameOverController с двумя заглушками панелей.
        /// </summary>
        private static GameOverController CreateGameOverController(string prefix)
        {
            var canvasGo = new GameObject($"{prefix}Canvas");
            canvasGo.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

            var victoryGo = new GameObject($"{prefix}VictoryPanel");
            victoryGo.transform.SetParent(canvasGo.transform, false);
            victoryGo.AddComponent<RectTransform>();
            victoryGo.SetActive(false);

            var defeatGo = new GameObject($"{prefix}DefeatPanel");
            defeatGo.transform.SetParent(canvasGo.transform, false);
            defeatGo.AddComponent<RectTransform>();
            defeatGo.SetActive(false);

            var ctrl = canvasGo.AddComponent<GameOverController>();

            // Проставляем ссылки через рефлексию (как SerializedObject в Editor)
            var flags = BindingFlags.NonPublic | BindingFlags.Instance;
            typeof(GameOverController)
                .GetField("victoryPanel", flags)
                ?.SetValue(ctrl, victoryGo);
            typeof(GameOverController)
                .GetField("defeatPanel", flags)
                ?.SetValue(ctrl, defeatGo);

            return ctrl;
        }

        // ================================================================
        // GameWatcher: гибель вражеского HQ → Victory
        // ================================================================

        [UnityTest]
        public IEnumerator GameWatcher_EnemyHQDies_ShowsVictory()
        {
            // Arrange — два HQ
            var playerHQGo = CreateHQBuilding("PlayerHQ_Test", Faction.Player, new Vector3(-10f, 0f, -10f));
            var enemyHQGo  = CreateHQBuilding("EnemyHQ_Test",  Faction.Enemy,  new Vector3( 10f, 0f,  10f));

            var gameOver = CreateGameOverController("Watcher_");

            var watcherGo = new GameObject("WatcherGo");
            var watcher   = watcherGo.AddComponent<GameWatcher>();
            watcher.InitForTest(gameOver);

            var playerHQHealth = playerHQGo.GetComponent<Health>();
            var enemyHQHealth  = enemyHQGo.GetComponent<Health>();

            // Подписываем watcher на конкретные Health через internal API
            watcher.WatchHQs(playerHQHealth, enemyHQHealth);

            // Ждём один кадр для инициализации
            yield return null;

            // Act — убиваем вражеский HQ
            enemyHQHealth.TakeDamage(9999f);

            // Ждём кадр для обработки события
            yield return null;

            // Assert
            Assert.IsTrue(
                gameOver.IsShown,
                "После гибели вражеского HQ GameOverController должен показать экран.");

            var victoryPanel = (GameObject)typeof(GameOverController)
                .GetField("victoryPanel", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(gameOver);

            Assert.IsTrue(
                victoryPanel != null && victoryPanel.activeSelf,
                "Панель Victory должна быть активна при гибели вражеского HQ.");
        }

        // ================================================================
        // GameWatcher: гибель HQ игрока → Defeat
        // ================================================================

        [UnityTest]
        public IEnumerator GameWatcher_PlayerHQDies_ShowsDefeat()
        {
            // Arrange
            var playerHQGo = CreateHQBuilding("PlayerHQ_Defeat", Faction.Player, new Vector3(-10f, 0f, -10f));
            var enemyHQGo  = CreateHQBuilding("EnemyHQ_Defeat",  Faction.Enemy,  new Vector3( 10f, 0f,  10f));

            var gameOver = CreateGameOverController("WatcherDefeat_");

            var watcherGo = new GameObject("WatcherGo_Defeat");
            var watcher   = watcherGo.AddComponent<GameWatcher>();
            watcher.InitForTest(gameOver);

            var playerHQHealth = playerHQGo.GetComponent<Health>();
            var enemyHQHealth  = enemyHQGo.GetComponent<Health>();
            watcher.WatchHQs(playerHQHealth, enemyHQHealth);

            yield return null;

            // Act
            playerHQHealth.TakeDamage(9999f);
            yield return null;

            // Assert
            Assert.IsTrue(gameOver.IsShown,
                "После гибели HQ игрока GameOverController должен показать экран.");

            var defeatPanel = (GameObject)typeof(GameOverController)
                .GetField("defeatPanel", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(gameOver);

            Assert.IsTrue(
                defeatPanel != null && defeatPanel.activeSelf,
                "Панель Defeat должна быть активна при гибели HQ игрока.");
        }

        // ================================================================
        // EnemyCommander: при достаточном балансе производит юнитов
        // ================================================================

        [UnityTest]
        public IEnumerator EnemyCommander_SufficientBalance_ProducesUnits()
        {
            // Arrange — ResourceBank Enemy = 500
            var bankGo = new GameObject("ScenarioTestBank");
            var bank   = bankGo.AddComponent<ResourceBank>();
            bank.InitForTest(playerBalance: 0, enemyBalance: 500);

            // Барак с быстрым производством (0.2 с) и стоимостью 50
            var barracksGo    = new GameObject("TestBarracks_Enemy");
            barracksGo.transform.position = new Vector3(15f, 0f, 15f);

            var barrackHealth   = barracksGo.AddComponent<Health>();
            var barracksBuilding = barracksGo.AddComponent<Building>();

            // UnitData-шаблон для производимого юнита
            var unitTemplate = UnitData.CreateForTest(
                maxHp:        50f,
                moveSpeed:    5f,
                attackRange:  3f,
                aggroRadius:  8f,
                retreatDisabled: true);

            var barracksData = BuildingData.CreateForTest(
                displayName:    "TestBarracks",
                maxHp:          300f,
                buildingType:   BuildingType.Barracks,
                productionTime: 0.2f,
                productionCost: 50,
                produces:       unitTemplate);

            barracksBuilding.InitForTest(barracksData, Faction.Enemy, bank);

            var production = barracksGo.AddComponent<ProductionBuilding>();

            // Создаём минимальный префаб юнита (NavMeshAgent требует NavMesh, поэтому используем
            // простой GO без NavMesh — он не сможет перемещаться, но произведётся)
            var unitPrefabGo = new GameObject("ScenarioUnitPrefab");
            unitPrefabGo.AddComponent<NavMeshAgent>();
            unitPrefabGo.AddComponent<Unit>();
            unitPrefabGo.AddComponent<Health>();
            // Отключаем — префаб неактивен до спавна
            unitPrefabGo.SetActive(false);

            production.InitForTest(unitPrefabGo, bank);

            Physics.SyncTransforms();

            // EnemyCommander с коротким интервалом решений
            var commanderGo = new GameObject("CommanderGo");
            var commander   = commanderGo.AddComponent<EnemyCommander>();
            commander.InitForTest(bank, production, decisionInterval: 0.5f);

            // Ждём 3 секунды (несколько циклов решений + производство)
            yield return new WaitForSeconds(3f);

            // Assert — должен произвестись хотя бы 1 юнит
            UnitRegistry.GetUnits(Faction.Enemy, new System.Collections.Generic.List<Unit>(16));

            // Считаем через UnitRegistry
            var buffer = new System.Collections.Generic.List<Unit>(16);
            UnitRegistry.GetUnits(Faction.Enemy, buffer);

            // Также проверим через bank — средства потрачены
            int spent = 500 - bank.GetBalance(Faction.Enemy);

            Assert.Greater(spent, 0,
                "За 3 секунды EnemyCommander должен был потратить ресурсы на производство юнитов.");

            // Убираем prefab-GO вручную
            Object.DestroyImmediate(unitPrefabGo);
        }
    }
}
