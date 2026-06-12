using System.Collections;
using System.Collections.Generic;
using DiplomaGame.Runtime.Combat;
using DiplomaGame.Runtime.Data;
using DiplomaGame.Runtime.Tech;
using DiplomaGame.Runtime.Units;
using NUnit.Framework;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace DiplomaGame.Tests.Runtime
{
    /// <summary>
    /// PlayMode-тесты баланса дерева технологий (категория Balance, v7).
    /// Проверяют, что исследованные технологии фактически влияют на исход боя.
    /// </summary>
    [TestFixture]
    [Category("Balance")]
    public class TechBalanceTests
    {
        private GameObject     _groundGo;
        private NavMeshSurface _surface;

        [SetUp]
        public void SetUp()
        {
            _groundGo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            _groundGo.name = "TechBalanceGround";
            _groundGo.transform.position   = Vector3.zero;
            _groundGo.transform.localScale = new Vector3(8f, 1f, 8f);

            _surface = _groundGo.AddComponent<NavMeshSurface>();
            _surface.BuildNavMesh();

            var playerBase = new GameObject("PlayerBaseSpawn");
            playerBase.transform.position = new Vector3(-30f, 0f, -30f);
            var enemyBase = new GameObject("EnemyBaseSpawn");
            enemyBase.transform.position = new Vector3(30f, 0f, 30f);

            Physics.SyncTransforms();
            UnitCombat.InvalidateRallyCache();

            // Сбрасываем TechRegistry для чистоты теста
            ResetTechRegistry();
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;

            ResetTechRegistry();

            foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (go == null) continue;
                if (go.name.StartsWith("TechBalance") || go.name.EndsWith("BaseSpawn"))
                    Object.DestroyImmediate(go);
            }

            NavMesh.RemoveAllNavMeshData();
        }

        private static void ResetTechRegistry()
        {
            var field = typeof(TechRegistry).GetField(
                "_instance",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            field?.SetValue(null, null);
        }

        // ----------------------------------------------------------------
        // Фабрика юнитов
        // ----------------------------------------------------------------

        private static GameObject CreateUnit(string name, Faction faction, Vector3 position, UnitData data)
        {
            var go = new GameObject(name);
            go.transform.position = position;

            go.AddComponent<NavMeshAgent>();
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

        private static void SpawnLine(Faction faction, UnitData data, float x, int count, string namePrefix)
        {
            float zStart = -(count - 1);
            for (int i = 0; i < count; i++)
                CreateUnit($"{namePrefix}_{i}", faction, new Vector3(x, 0f, zStart + i * 2f), data);
        }

        // ----------------------------------------------------------------
        // Вспомогательные методы
        // ----------------------------------------------------------------

        private static IEnumerator WaitForBattleEnd(float simLimitSeconds, List<Unit> buffer)
        {
            const float PollStep       = 0.25f;
            // Stall-breaker зеркален BalanceSimulationTests: порог 15 сим-с,
            // цель — позиция БАЗЫ (не центр масс), см. детали в BalanceSimulationTests.
            const float StallSimSeconds = 15f;

            float simElapsed    = 0f;
            float lastChangeSim = 0f;
            float lastTotalHp   = -1f;

            Vector3 playerBasePos = GetBasePosition("PlayerBaseSpawn");
            Vector3 enemyBasePos  = GetBasePosition("EnemyBaseSpawn");

            while (simElapsed < simLimitSeconds)
            {
                yield return new WaitForSecondsRealtime(PollStep);
                simElapsed += PollStep * Time.timeScale;

                if (CountAlive(Faction.Player, buffer) == 0 || CountAlive(Faction.Enemy, buffer) == 0)
                    yield break;

                float totalHp = SumHp(Faction.Player, buffer) + SumHp(Faction.Enemy, buffer);
                if (!Mathf.Approximately(totalHp, lastTotalHp))
                {
                    lastTotalHp   = totalHp;
                    lastChangeSim = simElapsed;
                }
                else if (simElapsed - lastChangeSim >= StallSimSeconds)
                {
                    BreakStall(buffer, playerBasePos, enemyBasePos);
                    lastChangeSim = simElapsed;
                }
            }
        }

        private static Vector3 GetBasePosition(string markerName)
        {
            var go = GameObject.Find(markerName);
            return go != null ? go.transform.position : Vector3.zero;
        }

        private static void BreakStall(List<Unit> buffer, Vector3 playerBasePos, Vector3 enemyBasePos)
        {
            IssueAttackMoveToAll(Faction.Player, enemyBasePos,  buffer);
            IssueAttackMoveToAll(Faction.Enemy,  playerBasePos, buffer);
            Debug.Log($"[TechBalance] Stall-breaker: Player→EnemyBase({enemyBasePos}), " +
                      $"Enemy→PlayerBase({playerBasePos}).");
        }

        private static void IssueAttackMoveToAll(Faction faction, Vector3 target, List<Unit> buffer)
        {
            UnitRegistry.GetUnits(faction, buffer);
            for (int i = 0; i < buffer.Count; i++)
            {
                var u = buffer[i];
                if (u == null) continue;
                var h = u.GetComponent<Health>();
                if (h == null || h.IsDead) continue;
                // 1. Выдаём команду (OnCommandIssued сбросит _retreatSuppressedForBreaker)
                u.IssueCommand(UnitCommand.AttackMove(target));
                // 2. Подавляем ретрит ПОСЛЕ команды — юниты с HP≤retreatThreshold
                //    не уйдут обратно к базе немедленно, а пойдут на врага до конца раунда
                var combat = u.GetComponent<UnitCombat>();
                combat?.SuppressRetreatForStallBreaker();
            }
        }

        private static int CountAlive(Faction faction, List<Unit> buffer)
        {
            UnitRegistry.GetUnits(faction, buffer);
            int alive = 0;
            for (int i = 0; i < buffer.Count; i++)
            {
                var health = buffer[i].GetComponent<Health>();
                if (health != null && !health.IsDead)
                    alive++;
            }
            return alive;
        }

        private static float SumHp(Faction faction, List<Unit> buffer)
        {
            UnitRegistry.GetUnits(faction, buffer);
            float sum = 0f;
            for (int i = 0; i < buffer.Count; i++)
            {
                var health = buffer[i].GetComponent<Health>();
                if (health != null && !health.IsDead)
                    sum += health.CurrentHp;
            }
            return sum;
        }

        // ----------------------------------------------------------------
        // Тест 1: WeaponUpgrade_5v5_FavorsUpgradedArmy
        // 5 Marine (Player, Tech_Weapons исследован) vs 5 Marine (Enemy, без апгрейда)
        // Player должен победить ИЛИ иметь преимущество по HP
        // ----------------------------------------------------------------

        /// <summary>
        /// Серия из 5 раундов: один бой 5v5 при снежной дисперсии (первый выстрел/ретрит)
        /// не гарантирует победу даже с +15% урона — одиночный assert флакал.
        /// Метрика — большинство раундов за апгрейженной армией (≥3 из 5).
        /// </summary>
        [UnityTest]
        [Timeout(300000)]
        public IEnumerator WeaponUpgrade_5v5_FavorsUpgradedArmy()
        {
            const int Rounds = 5;
            var buffer = new List<Unit>(32);

            // Базовые статы Marine (зеркало ConfigTab)
            var marineData = UnitData.CreateForTest(
                displayName: "Marine", maxHp: 100f, damage: 10f, attackRange: 8f,
                attackCooldown: 1f, aggroRadius: 12f, moveSpeed: 5f,
                retreatHpFraction: 0.25f, retreatDisabled: false);

            // Исследуем Tech_Weapons для Player (до спавна — InitForTest вызовет ApplyData)
            var weaponsTech = TechData.CreateForTest(
                displayName:     "Усиленные стволы",
                effectType:      TechEffect.DamageMultiplier,
                effectMagnitude: 0.15f,
                infantryOnly:    false);

            TechRegistry.Instance.MarkResearched(Faction.Player, weaponsTech);

            int playerScore = 0, enemyScore = 0;

            for (int round = 0; round < Rounds; round++)
            {
                // Чередуем порядок создания команд (гасит остаточный порядок Update)
                if (round % 2 == 0)
                {
                    SpawnLine(Faction.Player, marineData, x: -5f, count: 5, namePrefix: "TechBalanceP");
                    SpawnLine(Faction.Enemy,  marineData, x: +5f, count: 5, namePrefix: "TechBalanceE");
                }
                else
                {
                    SpawnLine(Faction.Enemy,  marineData, x: +5f, count: 5, namePrefix: "TechBalanceE");
                    SpawnLine(Faction.Player, marineData, x: -5f, count: 5, namePrefix: "TechBalanceP");
                }

                Time.timeScale = 10f;
                yield return null;

                yield return WaitForBattleEnd(simLimitSeconds: 180f, buffer);

                int   playerAlive = CountAlive(Faction.Player, buffer);
                int   enemyAlive  = CountAlive(Faction.Enemy, buffer);
                float playerHp    = SumHp(Faction.Player, buffer);
                float enemyHp     = SumHp(Faction.Enemy, buffer);

                // Очко за раунд: уничтожение либо преимущество по HP на таймауте
                if (enemyAlive == 0 && playerAlive > 0)      playerScore++;
                else if (playerAlive == 0 && enemyAlive > 0) enemyScore++;
                else if (playerHp > enemyHp)                 playerScore++;
                else                                         enemyScore++;

                Debug.Log($"[TechBalance] WeaponUpgrade round {round + 1}/{Rounds}: " +
                          $"P {playerAlive}/{playerHp:F0}hp — E {enemyAlive}/{enemyHp:F0}hp " +
                          $"(счёт {playerScore}:{enemyScore})");

                DestroyRoundUnits(buffer);
                Time.timeScale = 1f;
                yield return null;
            }

            Assert.GreaterOrEqual(playerScore, 3,
                $"С +15% уроном Player обязан взять большинство из {Rounds} раундов. " +
                $"Счёт: Player {playerScore}, Enemy {enemyScore}.");
        }

        /// <summary>Уничтожает юнитов раунда между раундами серии.</summary>
        private static void DestroyRoundUnits(List<Unit> buffer)
        {
            UnitRegistry.GetUnits(Faction.Player, buffer);
            for (int i = buffer.Count - 1; i >= 0; i--)
                if (buffer[i] != null) Object.DestroyImmediate(buffer[i].gameObject);

            UnitRegistry.GetUnits(Faction.Enemy, buffer);
            for (int i = buffer.Count - 1; i >= 0; i--)
                if (buffer[i] != null) Object.DestroyImmediate(buffer[i].gameObject);
        }

        // ----------------------------------------------------------------
        // Тест 2: Armoring_TankUnaffected
        // InfantryOnly-технология не должна влиять на танка (AoeRadius > 0)
        // Проверяем через GetMaxHpMultiplier
        // ----------------------------------------------------------------

        [Test]
        public void Armoring_TankUnaffected()
        {
            var armoringTech = TechData.CreateForTest(
                displayName:     "Бронирование",
                effectType:      TechEffect.MaxHpMultiplier,
                effectMagnitude: 0.20f,
                infantryOnly:    true);

            TechRegistry.Instance.MarkResearched(Faction.Player, armoringTech);

            var tankData = UnitData.CreateForTest(aoeRadius: 3f); // танк
            float tankMult = TechRegistry.Instance.GetMaxHpMultiplier(Faction.Player, tankData);

            var infantryData = UnitData.CreateForTest(aoeRadius: 0f); // пехота
            float infantryMult = TechRegistry.Instance.GetMaxHpMultiplier(Faction.Player, infantryData);

            Assert.AreEqual(0f,    tankMult,     1e-5f,
                "InfantryOnly: танк не должен получать бонус MaxHp (mult=0.0).");
            Assert.AreEqual(0.20f, infantryMult, 1e-5f,
                "InfantryOnly: пехота должна получать бонус MaxHp (mult=0.20).");
        }
    }
}
