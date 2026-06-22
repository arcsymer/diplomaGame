using System.Collections;
using DiplomaGame.Runtime.AI;
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
    /// PlayMode-тесты circle-14: тактика ИИ-противника.
    /// Проверяет, что экстренная волна запускается после уничтожения вражеского
    /// производственного здания и приходит раньше стандартного интервала.
    /// </summary>
    [TestFixture]
    public class AiTacticsTests
    {
        private GameObject     _groundGo;
        private NavMeshSurface _surface;

        [SetUp]
        public void SetUp()
        {
            // NavMesh-плоскость (паттерн ScenarioTests/BalanceSimulationTests)
            _groundGo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            _groundGo.name = "AiTacticsGround";
            _groundGo.transform.position   = Vector3.zero;
            _groundGo.transform.localScale = new Vector3(5f, 1f, 5f);

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
            Time.timeScale = 1f;

            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (go == null) continue;
                if (go.name.StartsWith("AiTactics") ||
                    go.name.StartsWith("PlayerBase") ||
                    go.name.StartsWith("EnemyBase")  ||
                    go.name.StartsWith("TacticsHQ")  ||
                    go.name.StartsWith("TacticsBarracks") ||
                    go.name.StartsWith("TacticsCommander") ||
                    go.name.StartsWith("TacticsBank"))
                {
                    Object.DestroyImmediate(go);
                }
            }

            NavMesh.RemoveAllNavMeshData();
        }

        // ----------------------------------------------------------------
        // Вспомогательные фабрики
        // ----------------------------------------------------------------

        private static GameObject CreateHQ(string name, Faction faction, Vector3 pos)
        {
            var go       = new GameObject(name);
            go.transform.position = pos;
            go.AddComponent<Health>();
            var building = go.AddComponent<Building>();
            var data     = BuildingData.CreateForTest(
                displayName:  "HQ",
                maxHp:        500f,
                buildingType: BuildingType.Headquarters);
            building.InitForTest(data, faction, bank: null);
            Physics.SyncTransforms();
            return go;
        }

        private static (GameObject go, Building building, Health health) CreateEnemyBarracks(
            string name, Vector3 pos, ResourceBank bank)
        {
            var go       = new GameObject(name);
            go.transform.position = pos;
            var health   = go.AddComponent<Health>();
            var building = go.AddComponent<Building>();
            var data     = BuildingData.CreateForTest(
                displayName:    "EnemyBarracks",
                maxHp:          300f,
                buildingType:   BuildingType.Barracks,
                productionTime: 5f,
                productionCost: 50);
            building.InitForTest(data, Faction.Enemy, bank);
            Physics.SyncTransforms();
            return (go, building, health);
        }

        // ================================================================
        // Тест: после гибели вражеского барака экстренная волна приходит
        // раньше стандартного интервала maxWaitTime
        // ================================================================

        [UnityTest]
        public IEnumerator AfterEnemyProductionBuildingDies_EmergencyWaveLaunchesBeforeNormalInterval()
        {
            // Arrange
            // ResourceBank — врагу не нужны деньги для волны, только для производства
            var bankGo = new GameObject("TacticsBank");
            var bank   = bankGo.AddComponent<ResourceBank>();
            bank.InitForTest(playerBalance: 0, enemyBalance: 0);

            // HQ игрока — цель атаки
            var playerHQGo = CreateHQ("TacticsHQ_Player", Faction.Player,
                new Vector3(-10f, 0f, -10f));

            // Вражеский барак (производственное здание)
            var (barracksGo, _, barracksHealth) = CreateEnemyBarracks(
                "TacticsBarracks_Enemy", new Vector3(12f, 0f, 12f), bank);

            // EnemyCommander:
            // - emergencyWaveDelay = 1.5f (короткий, чтобы тест уложился)
            // - maxWaitTime (стандартный интервал) = 30f
            // - decisionInterval = 0.5f (быстрые тики)
            // - нет барака в _enemyBarracks → производство не мешает тесту
            var commanderGo = new GameObject("TacticsCommander");
            var commander   = commanderGo.AddComponent<EnemyCommander>();
            commander.InitForTest(
                bank:                   bank,
                barracks:               null,
                decisionInterval:       0.5f,
                warFactory:             null,
                emergencyWaveDelay:     1.5f,
                productionPauseDuration: 5f,
                flankProbability:       0f,
                flankWaypoints:         null);

            // Ждём инициализации
            yield return null;

            // Засекаем момент начала
            float startTime = Time.time;

            // Подписываемся на событие волны через именованный обработчик
            bool waveLaunched = false;
            void OnWave() { waveLaunched = true; }
            EnemyCommander.WaveLaunched += OnWave;

            // Act: уничтожаем вражеский барак → должен сработать OnAnyDied
            barracksHealth.TakeDamage(9999f);

            // Ждём до 5 секунд (emergencyWaveDelay=1.5 + запас)
            const float timeout = 5f;
            while (!waveLaunched && Time.time - startTime < timeout)
                yield return null;

            float elapsed = Time.time - startTime;

            // Отписываемся
            EnemyCommander.WaveLaunched -= OnWave;

            // Assert
            Assert.IsTrue(waveLaunched,
                "После уничтожения вражеского производственного здания " +
                "EnemyCommander должен запустить WaveLaunched в течение timeout.");

            // Волна должна прийти ДО стандартного интервала (30 сек)
            Assert.Less(elapsed, 30f,
                $"Экстренная волна должна запуститься до истечения maxWaitTime=30s. " +
                $"Прошло: {elapsed:F2}s.");

            // И после emergencyWaveDelay (1.5s с небольшим запасом на тики)
            Assert.GreaterOrEqual(elapsed, 1.0f,
                $"Экстренная волна не должна запуститься мгновенно (нужна задержка ≥1s). " +
                $"Прошло: {elapsed:F2}s.");

            // Cleanup: удаляем вручную barracks GO (не попадает в стандартный teardown по имени)
            if (barracksGo != null)
                Object.DestroyImmediate(barracksGo);
        }
    }
}
