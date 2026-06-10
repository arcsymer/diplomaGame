using System.Collections;
using System.Collections.Generic;
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
    /// PlayMode-тесты M10: интеграционный «плейтест» полного матча.
    /// Вершина тестовой пирамиды: вся боевая петля в сборе.
    ///
    /// M10-BUG-001 ИСПРАВЛЕН: UnitCombat.ScanForTarget() теперь учитывает
    /// как юниты (UnitRegistry), так и здания (BuildingRegistry).
    /// Тест #1 проверяет реальную атаку HQ — HQ должен умереть без имитации.
    /// </summary>
    [TestFixture]
    public class MatchSimulationTests
    {
        // ----------------------------------------------------------------
        // Инфраструктура
        // ----------------------------------------------------------------

        private GameObject     _groundGo;
        private NavMeshSurface _surface;

        [SetUp]
        public void SetUp()
        {
            _groundGo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            _groundGo.name = "MatchSimGround";
            _groundGo.transform.position   = Vector3.zero;
            _groundGo.transform.localScale  = new Vector3(5f, 1f, 5f); // 50×50

            _surface = _groundGo.AddComponent<NavMeshSurface>();
            _surface.BuildNavMesh();

            var playerBase = new GameObject("PlayerBaseSpawn");
            playerBase.transform.position = new Vector3(-15f, 0f, -15f);

            var enemyBase = new GameObject("EnemyBaseSpawn");
            enemyBase.transform.position = new Vector3(15f, 0f, 15f);

            Physics.SyncTransforms();
        }

        [TearDown]
        public void TearDown()
        {
            // ОБЯЗАТЕЛЬНО: возвращаем timeScale до очистки объектов
            // (GameOverController.ShowVictory/ShowDefeat ставит timeScale = 0)
            Time.timeScale = 1f;

            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (go == null) continue;

                if (go.name.StartsWith("MatchSim")       ||
                    go.name.StartsWith("PlayerBaseSpawn") ||
                    go.name.StartsWith("EnemyBaseSpawn")  ||
                    go.name.StartsWith("MatchPlayerHQ")   ||
                    go.name.StartsWith("MatchEnemyHQ")    ||
                    go.name.StartsWith("MatchBarracks")   ||
                    go.name.StartsWith("MatchWatcher")    ||
                    go.name.StartsWith("MatchCommander")  ||
                    go.name.StartsWith("MatchBank")       ||
                    go.name.StartsWith("MatchCanvas")     ||
                    go.name.StartsWith("MatchUnitPrefab"))
                {
                    Object.DestroyImmediate(go);
                }
            }

            NavMesh.RemoveAllNavMeshData();
        }

        // ----------------------------------------------------------------
        // Фабрики
        // ----------------------------------------------------------------

        /// <summary>
        /// Создаёт HQ здание с заданным HP. Регистрируется в BuildingRegistry через OnEnable.
        /// </summary>
        private static GameObject CreateHQ(string goName, Faction faction, Vector3 position, float maxHp = 300f)
        {
            var go = new GameObject(goName);
            go.transform.position = position;

            go.AddComponent<Health>();
            var building = go.AddComponent<Building>();

            var data = BuildingData.CreateForTest(
                displayName:  "HQ",
                maxHp:        maxHp,
                buildingType: BuildingType.Headquarters);

            building.InitForTest(data, faction, bank: null);
            Physics.SyncTransforms();
            return go;
        }

        /// <summary>
        /// Создаёт GameOverController с панелями Victory и Defeat.
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

            var flags = BindingFlags.NonPublic | BindingFlags.Instance;
            typeof(GameOverController)
                .GetField("victoryPanel", flags)
                ?.SetValue(ctrl, victoryGo);
            typeof(GameOverController)
                .GetField("defeatPanel", flags)
                ?.SetValue(ctrl, defeatGo);

            return ctrl;
        }

        /// <summary>
        /// Создаёт шаблон юнита-врага (неактивный GO) с NavMeshAgent, Unit, Health, UnitCombat.
        /// Фракция Enemy задаётся через рефлексию.
        /// </summary>
        private static GameObject CreateEnemyUnitPrefab(string name, UnitData unitData)
        {
            var go = new GameObject(name);
            go.AddComponent<NavMeshAgent>();

            var unit = go.AddComponent<Unit>();

            // Задаём фракцию Enemy через рефлексию (как в CombatTests)
            var factionField = typeof(Unit).GetField(
                "_faction",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (factionField != null)
                factionField.SetValue(unit, Faction.Enemy);

            go.AddComponent<Health>();

            var combat = go.AddComponent<UnitCombat>();
            combat.InitForTest(unitData);

            // Деактивируем — префаб до Instantiate неактивен
            go.SetActive(false);
            return go;
        }

        // ----------------------------------------------------------------
        // Тест 1: FullMatch_EnemyWavesDestroyUndefendedPlayerHQ_DefeatShown
        //
        // M10-BUG-001 ИСПРАВЛЕН: UnitCombat теперь атакует здания.
        // Тест проверяет:
        //   - Волны юнитов производятся EnemyCommander и отправляются к HQ игрока.
        //   - Вражеские юниты реально атакуют HQ (UnitCombat видит Building через BuildingRegistry).
        //   - HQ умирает без ручного TakeDamage.
        //   - GameWatcher замечает смерть HQ → GameOverController.ShowDefeat активируется.
        // ----------------------------------------------------------------

        [UnityTest]
        [Timeout(120000)]
        public IEnumerator FullMatch_EnemyWavesDestroyUndefendedPlayerHQ_DefeatShown()
        {
            // --- Arrange ---

            // ResourceBank Enemy 1000
            var bankGo = new GameObject("MatchBankGo");
            var bank   = bankGo.AddComponent<ResourceBank>();
            bank.InitForTest(playerBalance: 0, enemyBalance: 1000);

            // HQ игрока (без защитников, HP 300) рядом с PlayerBaseSpawn
            var playerHQGo     = CreateHQ("MatchPlayerHQ_Main", Faction.Player, new Vector3(-10f, 0f, -10f), maxHp: 300f);
            var playerHQHealth = playerHQGo.GetComponent<Health>();

            // HQ врага (просто чтобы BuildingRegistry был заполнен)
            var enemyHQGo = CreateHQ("MatchEnemyHQ_Main", Faction.Enemy, new Vector3(10f, 0f, 10f), maxHp: 500f);

            // UnitData для вражеских бойцов (damage 50, attackRange 6, aggroRadius 15)
            // Достаточно урона чтобы убить HQ (HP 300) за 6 ударов при 5 юнитах
            var enemyUnitData = UnitData.CreateForTest(
                displayName:      "EnemyWarrior",
                maxHp:            80f,
                damage:           50f,
                attackRange:      6f,
                attackCooldown:   0.3f,
                aggroRadius:      20f,
                moveSpeed:        6f,
                retreatHpFraction: 0f,
                retreatDisabled:  true);

            // Шаблон юнита-врага
            var unitPrefab = CreateEnemyUnitPrefab("MatchUnitPrefab_Enemy", enemyUnitData);

            // Барак врага (cost 50, productionTime 0.3)
            var barracksGo = new GameObject("MatchBarracksGo");
            barracksGo.transform.position = new Vector3(12f, 0f, 12f);

            barracksGo.AddComponent<Health>();
            var barracksBuilding = barracksGo.AddComponent<Building>();

            var barracksData = BuildingData.CreateForTest(
                displayName:    "EnemyBarracks",
                maxHp:          300f,
                buildingType:   BuildingType.Barracks,
                productionTime: 0.3f,
                productionCost: 50,
                produces:       enemyUnitData);

            barracksBuilding.InitForTest(barracksData, Faction.Enemy, bank);

            var production = barracksGo.AddComponent<ProductionBuilding>();
            production.InitForTest(unitPrefab, bank);

            // GameOverController
            var gameOver = CreateGameOverController("MatchCanvas_Defeat");

            // GameWatcher
            var watcherGo = new GameObject("MatchWatcherGo");
            var watcher   = watcherGo.AddComponent<GameWatcher>();
            watcher.InitForTest(gameOver);
            watcher.WatchHQs(playerHQHealth, enemyHQGo.GetComponent<Health>());

            // EnemyCommander (decisionInterval 0.5, волны по 2-3 юнита)
            var commanderGo = new GameObject("MatchCommanderGo");
            var commander   = commanderGo.AddComponent<EnemyCommander>();
            commander.InitForTest(bank, production, decisionInterval: 0.5f);

            // Ускоряем симуляцию
            Time.timeScale = 10f;

            yield return null; // один кадр инициализации

            // --- Act: ждём пока HQ игрока не умрёт (до 90 сим-секунд = 9 реальных) ---

            const float SimLimit = 90f;
            const float PollStep = 0.5f; // реальных секунд

            float elapsed = 0f;

            while (elapsed < SimLimit)
            {
                yield return new WaitForSecondsRealtime(PollStep);
                elapsed += PollStep * Time.timeScale; // сим-секунды


                // Прекращаем ждать, если timeScale был сброшен в 0 (GameOver сработал)
                if (Time.timeScale <= 0f)
                    break;

                // Прекращаем ждать, если HQ уже мёртв
                if (playerHQHealth.IsDead)
                    break;
            }

            // Восстанавливаем timeScale ДО assert-ов (ShowDefeat поставил 0)
            Time.timeScale = 1f;

            // --- Проверка производства волн ---
            var enemyBuffer   = new List<Unit>(32);
            UnitRegistry.GetUnits(Faction.Enemy, enemyBuffer);
            int spentResources = 1000 - bank.GetBalance(Faction.Enemy);
            Assert.Greater(
                spentResources,
                0,
                $"EnemyCommander должен был потратить ресурсы за {SimLimit} сим-секунд. " +
                $"Баланс Enemy: {bank.GetBalance(Faction.Enemy)} из 1000.");

            // --- Главная проверка: HQ игрока мёртв от атак вражеских юнитов ---
            Assert.IsTrue(
                playerHQHealth.IsDead,
                "HQ игрока должен погибнуть от атак вражеских юнитов за 90 сим-секунд. " +
                "UnitCombat.ScanForTarget теперь включает здания из BuildingRegistry.");

            // GameWatcher должен показать экран поражения
            Assert.IsTrue(
                gameOver.IsShown,
                "После гибели HQ игрока GameOverController должен показать экран GameOver.");

            var defeatPanel = (GameObject)typeof(GameOverController)
                .GetField("defeatPanel", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(gameOver);

            Assert.IsTrue(
                defeatPanel != null && defeatPanel.activeSelf,
                "Панель Defeat должна быть активна после гибели HQ игрока.");

            // Убираем префаб вручную
            Object.DestroyImmediate(unitPrefab);
        }

        // ----------------------------------------------------------------
        // Тест 2: FullMatch_PlayerDestroysEnemyHQ_VictoryShown
        //
        // Симметричный тест: TakeDamage на HQ врага → Victory.
        // Проверяет полный путь GameWatcher → GameOverController.ShowVictory.
        // ----------------------------------------------------------------

        [UnityTest]
        [Timeout(120000)]
        public IEnumerator FullMatch_PlayerDestroysEnemyHQ_VictoryShown()
        {
            // --- Arrange ---

            var playerHQGo     = CreateHQ("MatchPlayerHQ_Victory", Faction.Player, new Vector3(-10f, 0f, -10f), maxHp: 500f);
            var enemyHQGo      = CreateHQ("MatchEnemyHQ_Victory",  Faction.Enemy,  new Vector3( 10f, 0f,  10f), maxHp: 300f);

            var playerHQHealth = playerHQGo.GetComponent<Health>();
            var enemyHQHealth  = enemyHQGo.GetComponent<Health>();

            var gameOver = CreateGameOverController("MatchCanvas_Victory");

            var watcherGo = new GameObject("MatchWatcherGo_Victory");
            var watcher   = watcherGo.AddComponent<GameWatcher>();
            watcher.InitForTest(gameOver);
            watcher.WatchHQs(playerHQHealth, enemyHQHealth);

            // Ускоряем симуляцию
            Time.timeScale = 10f;

            // Ждём один кадр для инициализации всех компонентов
            yield return null;

            // Восстанавливаем timeScale ДО TakeDamage (ShowVictory ставит 0)
            Time.timeScale = 1f;

            // --- Act: игрок уничтожает HQ врага ---
            enemyHQHealth.TakeDamage(9999f);

            // Ждём кадр для propagation события Died → GameWatcher → ShowVictory
            yield return null;

            // --- Assert ---
            Assert.IsTrue(
                gameOver.IsShown,
                "После уничтожения HQ врага GameOverController должен показать экран GameOver.");

            var victoryPanel = (GameObject)typeof(GameOverController)
                .GetField("victoryPanel", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(gameOver);

            Assert.IsTrue(
                victoryPanel != null && victoryPanel.activeSelf,
                "Панель Victory должна быть активна при уничтожении вражеского HQ.");

            // GameOverController.ShowVictory поставил timeScale = 0 — сбрасываем
            Time.timeScale = 1f;
        }

        // ----------------------------------------------------------------
        // Тест 3: Economy_FullLoop_IncomeProductionWave
        //
        // Банк Enemy 200 + барак (cost 50, time 0.3) + EnemyCommander →
        // за 20 сим-секунд должно быть произведено ≥2 юнитов и баланс изменился.
        // ----------------------------------------------------------------

        [UnityTest]
        [Timeout(120000)]
        public IEnumerator Economy_FullLoop_IncomeProductionWave()
        {
            // --- Arrange ---

            var bankGo = new GameObject("MatchBankGo_Economy");
            var bank   = bankGo.AddComponent<ResourceBank>();
            bank.InitForTest(playerBalance: 0, enemyBalance: 200);

            var unitData = UnitData.CreateForTest(
                displayName:      "EcoUnit",
                maxHp:            60f,
                damage:           5f,
                attackRange:      4f,
                attackCooldown:   1f,
                aggroRadius:      10f,
                moveSpeed:        5f,
                retreatDisabled:  true);

            var unitPrefab = CreateEnemyUnitPrefab("MatchUnitPrefab_Eco", unitData);

            var barracksGo = new GameObject("MatchBarracksGo_Eco");
            barracksGo.transform.position = new Vector3(12f, 0f, 12f);

            barracksGo.AddComponent<Health>();
            var barracksBuilding = barracksGo.AddComponent<Building>();

            var barracksData = BuildingData.CreateForTest(
                displayName:    "EcoBarracks",
                maxHp:          300f,
                buildingType:   BuildingType.Barracks,
                productionTime: 0.3f,
                productionCost: 50,
                produces:       unitData);

            barracksBuilding.InitForTest(barracksData, Faction.Enemy, bank);

            var production = barracksGo.AddComponent<ProductionBuilding>();
            production.InitForTest(unitPrefab, bank);

            // EnemyCommander с коротким интервалом решений
            var commanderGo = new GameObject("MatchCommanderGo_Eco");
            var commander   = commanderGo.AddComponent<EnemyCommander>();
            commander.InitForTest(bank, production, decisionInterval: 0.5f);

            // HQ игрока — нужен EnemyCommander.CachePlayerHQ (иначе волна не отправляется,
            // но производство работает независимо)
            var playerHQGo = CreateHQ("MatchPlayerHQ_Eco", Faction.Player, new Vector3(-10f, 0f, -10f));

            int initialBalance = bank.GetBalance(Faction.Enemy); // 200

            // --- Act: ускорение ×10, ждём 20 сим-секунд = 2 реальных секунды ---
            Time.timeScale = 10f;

            yield return new WaitForSecondsRealtime(2f); // 20 сим-секунд

            Time.timeScale = 1f;

            // --- Assert ---
            int finalBalance = bank.GetBalance(Faction.Enemy);
            int spent        = initialBalance - finalBalance;

            Assert.Greater(
                spent, 0,
                $"За 20 сим-секунд EnemyCommander должен потратить ресурсы. " +
                $"Начальный баланс: {initialBalance}, текущий: {finalBalance}.");

            // При balance=200, cost=50 → максимум 4 заказа; production 0.3 сим-сек → ≥2 готово за 20 сек
            var enemyUnits = new List<Unit>(32);
            UnitRegistry.GetUnits(Faction.Enemy, enemyUnits);

            // Учитываем: юниты с UnitCombat и AttackMove могут уйти с позиции,
            // но в реестре остаются живые. Минимальное требование — ≥2.
            Assert.GreaterOrEqual(
                enemyUnits.Count, 2,
                $"За 20 сим-секунд должно быть произведено минимум 2 юнита Enemy. " +
                $"Сейчас в UnitRegistry: {enemyUnits.Count}, потрачено ресурсов: {spent}.");

            // Убираем префаб вручную
            Object.DestroyImmediate(unitPrefab);
        }
    }
}
