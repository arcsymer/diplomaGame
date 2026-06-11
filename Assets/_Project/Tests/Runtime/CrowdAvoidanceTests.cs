using System.Collections;
using System.Collections.Generic;
using DiplomaGame.Runtime.Combat;   // Health, IDamageable
using DiplomaGame.Runtime.Data;     // UnitData, TargetPriority
using DiplomaGame.Runtime.Units;    // Unit, UnitRegistry, UnitCommandLogic, UnitCommand, Faction
using NUnit.Framework;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace DiplomaGame.Tests.Runtime
{
    /// <summary>
    /// PlayMode-тесты для оценки crowd-avoidance после v3-тюнинга:
    /// увеличенный формационный spacing + NavMeshAgent HighQualityObstacleAvoidance.
    /// Категория Balance — результаты учитываются метриками Improve.
    /// </summary>
    [TestFixture]
    [Category("Balance")]
    public class CrowdAvoidanceTests
    {
        private GameObject     _groundGo;
        private NavMeshSurface _surface;

        [SetUp]
        public void SetUp()
        {
            _groundGo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            _groundGo.name = "CrowdGround";
            _groundGo.transform.position   = Vector3.zero;
            _groundGo.transform.localScale = new Vector3(8f, 1f, 8f); // 80×80

            _surface = _groundGo.AddComponent<NavMeshSurface>();
            _surface.BuildNavMesh();

            // Rally-маркеры нужны для логики отступления в UnitCombat
            var playerBase = new GameObject("PlayerBaseSpawn");
            playerBase.transform.position = new Vector3(-30f, 0f, -30f);
            var enemyBase = new GameObject("EnemyBaseSpawn");
            enemyBase.transform.position = new Vector3(30f, 0f, 30f);

            Physics.SyncTransforms();
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;

            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (go == null) continue;
                if (go.name.StartsWith("Crowd") || go.name.EndsWith("BaseSpawn"))
                    Object.DestroyImmediate(go);
            }

            NavMesh.RemoveAllNavMeshData();
        }

        // ----------------------------------------------------------------
        // Фабрика юнитов — аналог BalanceSimulationTests.CreateUnit
        // ----------------------------------------------------------------

        private static GameObject CreateUnit(string name, Faction faction, Vector3 position, UnitData data)
        {
            var go = new GameObject(name);
            go.transform.position = position;

            var agent = go.AddComponent<NavMeshAgent>();
            // v3 crowd-avoidance параметры (Marine/EnemyGrunt)
            agent.radius               = 0.45f;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            agent.avoidancePriority    = 50;
            agent.stoppingDistance     = 0.5f;
            agent.speed                = data.MoveSpeed;

            var unit = go.AddComponent<Unit>();
            var factionField = typeof(Unit).GetField(
                "_faction",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            factionField?.SetValue(unit, faction);

            UnitRegistry.Unregister(unit);
            UnitRegistry.Register(unit);

            go.AddComponent<Health>();

            var combat = go.AddComponent<UnitCombat>();
            combat.InitForTest(data);

            return go;
        }

        private static UnitData MarineData() => UnitData.CreateForTest(
            displayName: "Marine", maxHp: 100f, damage: 10f, attackRange: 8f,
            attackCooldown: 1f, aggroRadius: 12f, moveSpeed: 5f,
            retreatHpFraction: 0.25f, retreatDisabled: false);

        private static UnitData TankData() => UnitData.CreateForTest(
            displayName: "Tank", maxHp: 280f, damage: 25f, attackRange: 5f,
            attackCooldown: 2.0f, aggroRadius: 12f, moveSpeed: 3.0f,
            retreatDisabled: true, supplyCost: 3,
            aoeRadius: 3.0f, targetPriority: TargetPriority.Buildings);

        // ----------------------------------------------------------------
        // Тест 1: 8 Marine движутся к одной точке — нет перекрытий при прибытии
        // ----------------------------------------------------------------

        /// <summary>
        /// 8 юнитов получают приказ Move к одной точке (формация с spacing 2.0).
        /// После прибытия ни одна пара не должна находиться ближе 0.8 м.
        /// </summary>
        [UnityTest]
        [Timeout(60000)]
        public IEnumerator GroupMove_8Units_NoOverlapAtDestination()
        {
            var data = MarineData();
            var units = new List<GameObject>(8);
            var destination = new Vector3(0f, 0f, 0f);

            // Спавним 8 юнитов в линию напротив целевой точки
            for (int i = 0; i < 8; i++)
            {
                var go = CreateUnit($"CrowdMarine_{i}", Faction.Player,
                    new Vector3(-20f + i * 2f, 0f, -15f), data);
                units.Add(go);
            }

            yield return null;

            // Выдаём формационный приказ (имитируем логику CommandInput)
            float spacing = units.Count > 6 ? 2.5f : 2.0f;
            for (int i = 0; i < units.Count; i++)
            {
                var unit = units[i].GetComponent<Unit>();
                if (unit == null) continue;
                var offset = DiplomaGame.Runtime.Units.UnitCommandLogic.GetFormationOffset(i, spacing);
                unit.IssueCommand(DiplomaGame.Runtime.Units.UnitCommand.Move(destination + offset));
            }

            // Ускоряем симуляцию
            Time.timeScale = 8f;

            // Ждём прибытия (max 10 реальных секунд = 80 сим-секунд)
            float waited = 0f;
            while (waited < 10f)
            {
                yield return new WaitForSecondsRealtime(0.25f);
                waited += 0.25f;

                bool allArrived = true;
                foreach (var go in units)
                {
                    if (go == null) continue;
                    var agent = go.GetComponent<NavMeshAgent>();
                    if (agent == null) continue;
                    if (agent.pathPending || agent.remainingDistance > agent.stoppingDistance + 0.5f)
                    {
                        allArrived = false;
                        break;
                    }
                }
                if (allArrived) break;
            }

            Time.timeScale = 1f;

            // Проверяем отсутствие перекрытий
            float minAllowedDist = 0.8f;
            for (int i = 0; i < units.Count; i++)
            {
                for (int j = i + 1; j < units.Count; j++)
                {
                    if (units[i] == null || units[j] == null) continue;
                    float dist = Vector3.Distance(
                        units[i].transform.position,
                        units[j].transform.position);
                    Assert.GreaterOrEqual(dist, minAllowedDist,
                        $"Юниты {i} и {j} перекрываются: расстояние {dist:F2} < {minAllowedDist}.");
                }
            }
        }

        // ----------------------------------------------------------------
        // Тест 2: 4 Marine атакуют 1 Tank — разброс > 1.5 через 2 сим-с
        // ----------------------------------------------------------------

        /// <summary>
        /// 4 Marine получают AttackMove на позицию одинокого танка.
        /// Через 2 сим-секунды расстояние между крайними атакующими должно быть > 1.5 м
        /// (avoidance работает и атакующие не стоят в одной точке).
        /// </summary>
        [UnityTest]
        [Timeout(30000)]
        public IEnumerator AttackingGroup_4MarinesVs1Tank_SpreadGreaterThan1_5()
        {
            var marineData = MarineData();
            var tankData   = TankData();

            // Создаём танк-мишень (Enemy) в центре
            var tankGo = CreateUnit("CrowdTank_0", Faction.Enemy, new Vector3(0f, 0f, 0f), tankData);

            // 4 Marine (Player) стартуют сзади
            var marines = new List<GameObject>(4);
            for (int i = 0; i < 4; i++)
            {
                var go = CreateUnit($"CrowdMarine_{i}", Faction.Player,
                    new Vector3(-6f + i * 2f, 0f, -10f), marineData);
                marines.Add(go);
            }

            yield return null;

            // AttackMove к позиции танка (с формационным смещением)
            float spacing = 2.0f;
            for (int i = 0; i < marines.Count; i++)
            {
                var unit = marines[i].GetComponent<Unit>();
                if (unit == null) continue;
                var offset = DiplomaGame.Runtime.Units.UnitCommandLogic.GetFormationOffset(i, spacing);
                unit.IssueCommand(DiplomaGame.Runtime.Units.UnitCommand.AttackMove(
                    tankGo.transform.position + offset));
            }

            // Ускоряем и ждём 2 сим-секунды
            Time.timeScale = 8f;
            yield return new WaitForSecondsRealtime(0.25f); // 0.25 реальных * 8 = 2 сим-с
            Time.timeScale = 1f;

            // Измеряем разброс живых marines
            float maxDist = 0f;
            for (int i = 0; i < marines.Count; i++)
            {
                if (marines[i] == null) continue;
                var hiA = marines[i].GetComponent<Health>();
                if (hiA != null && hiA.IsDead) continue;

                for (int j = i + 1; j < marines.Count; j++)
                {
                    if (marines[j] == null) continue;
                    var hiB = marines[j].GetComponent<Health>();
                    if (hiB != null && hiB.IsDead) continue;

                    float d = Vector3.Distance(marines[i].transform.position, marines[j].transform.position);
                    if (d > maxDist) maxDist = d;
                }
            }

            BalanceReport.Write("balance-crowd-spread.json", new BalanceReport.ClashResult
            {
                scenario       = "CrowdAvoidance: 4 Marines vs 1 Tank, spread after 2 sim-s",
                winner         = "N/A",
                playerAlive    = 4,
                enemyAlive     = 1,
                playerHpLeft   = maxDist,
                enemyHpLeft    = 0,
                simDurationSec = 2f,
            });

            Assert.Greater(maxDist, 1.5f,
                $"Разброс атакующих marines через 2 сим-с должен быть > 1.5 м. Фактически: {maxDist:F2}.");
        }
    }
}
