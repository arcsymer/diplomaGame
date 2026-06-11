using System.Collections;
using System.Collections.Generic;
using System.IO;
using DiplomaGame.Runtime.Combat;
using DiplomaGame.Runtime.Data;
using DiplomaGame.Runtime.Diagnostics;
using DiplomaGame.Runtime.Units;
using NUnit.Framework;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace DiplomaGame.Tests.Runtime
{
    /// <summary>
    /// Авто-плейтесты баланса (фаза улучшения v3, §3 «измерительные харнессы»):
    /// армия против армии под управлением боевой FSM, без участия игрока.
    /// Результаты пишутся в Docs-Vault/Stats/balance-*.json — их подбирает
    /// вкладка Improve в Project Forge (ForgeBatch.ExportImprovementMetrics).
    /// </summary>
    [TestFixture]
    [Category("Balance")]
    public class BalanceSimulationTests
    {
        private GameObject     _groundGo;
        private NavMeshSurface _surface;

        [SetUp]
        public void SetUp()
        {
            _groundGo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            _groundGo.name = "BalanceGround";
            _groundGo.transform.position   = Vector3.zero;
            _groundGo.transform.localScale = new Vector3(8f, 1f, 8f); // 80×80

            _surface = _groundGo.AddComponent<NavMeshSurface>();
            _surface.BuildNavMesh();

            // Маркеры баз — нужны логике отступления (rally-точки)
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
                if (go.name.StartsWith("Balance") || go.name.EndsWith("BaseSpawn"))
                    Object.DestroyImmediate(go);
            }

            NavMesh.RemoveAllNavMeshData();
        }

        // ----------------------------------------------------------------
        // Фабрика юнитов (паттерн CombatTests)
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

        /// <summary>Спавнит линию юнитов фракции вдоль оси Z.</summary>
        private static void SpawnLine(Faction faction, UnitData data, float x, int count, string namePrefix)
        {
            float zStart = -(count - 1); // шаг 2 м, центрировано
            for (int i = 0; i < count; i++)
                CreateUnit($"{namePrefix}_{i}", faction, new Vector3(x, 0f, zStart + i * 2f), data);
        }

        // ----------------------------------------------------------------
        // Ожидание исхода боя
        // ----------------------------------------------------------------

        private static IEnumerator WaitForBattleEnd(float simLimitSeconds, List<Unit> buffer)
        {
            const float PollStep = 0.25f; // реальных секунд
            float simElapsed = 0f;

            while (simElapsed < simLimitSeconds)
            {
                yield return new WaitForSecondsRealtime(PollStep);
                simElapsed += PollStep * Time.timeScale;

                if (CountAlive(Faction.Player, buffer) == 0 || CountAlive(Faction.Enemy, buffer) == 0)
                    yield break;
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
        // Реальные статы юнитов (зеркало ConfigTab — источник истины Forge)
        // ----------------------------------------------------------------

        private static UnitData MarineData() => UnitData.CreateForTest(
            displayName: "Marine", maxHp: 100f, damage: 10f, attackRange: 8f,
            attackCooldown: 1f, aggroRadius: 12f, moveSpeed: 5f,
            retreatHpFraction: 0.25f, retreatDisabled: false);

        private static UnitData GruntData() => UnitData.CreateForTest(
            displayName: "Enemy Grunt", maxHp: 80f, damage: 8f, attackRange: 7f,
            attackCooldown: 1.2f, aggroRadius: 12f, moveSpeed: 4.5f,
            retreatHpFraction: 0.25f, retreatDisabled: false);

        // ----------------------------------------------------------------
        // Тест 1: зеркальный бой Marine vs Marine — проверка симметрии
        // ----------------------------------------------------------------

        /// <summary>
        /// Серия зеркальных боёв: один матч с ретритом почти всегда заканчивается
        /// разгромом (отступающий не отстреливается — ранний перевес превращается
        /// в снежный ком), поэтому симметрию ИИ измеряем ВИНРЕЙТОМ по серии.
        /// Порядок создания команд чередуется по раундам — гасит остаточный
        /// перекос от порядка Update (создан первым → Update первым).
        /// </summary>
        [UnityTest]
        [Timeout(300000)]
        public IEnumerator MirrorClash_MarineVsMarine_SeriesTerminatesAndIsRecorded()
        {
            const int   Rounds        = 6;
            const float RoundSimLimit = 240f;

            var buffer = new List<Unit>(64);

            int playerWins = 0, enemyWins = 0, draws = 0, timeouts = 0;
            float totalDuration = 0f;

            for (int round = 0; round < Rounds; round++)
            {
                // Чередуем порядок создания (= порядок Update)
                if (round % 2 == 0)
                {
                    SpawnLine(Faction.Player, MarineData(), x: -5f, count: 8, namePrefix: "BalanceP");
                    SpawnLine(Faction.Enemy,  MarineData(), x: +5f, count: 8, namePrefix: "BalanceE");
                }
                else
                {
                    SpawnLine(Faction.Enemy,  MarineData(), x: +5f, count: 8, namePrefix: "BalanceE");
                    SpawnLine(Faction.Player, MarineData(), x: -5f, count: 8, namePrefix: "BalanceP");
                }

                Time.timeScale = 10f;
                yield return null;

                float simStart = Time.time;
                yield return WaitForBattleEnd(RoundSimLimit, buffer);
                float duration = Time.time - simStart;
                totalDuration += duration;

                int playerAlive = CountAlive(Faction.Player, buffer);
                int enemyAlive  = CountAlive(Faction.Enemy, buffer);

                if (playerAlive > 0 && enemyAlive > 0) timeouts++;

                if      (enemyAlive == 0 && playerAlive > 0) playerWins++;
                else if (playerAlive == 0 && enemyAlive > 0) enemyWins++;
                else draws++;

                Debug.Log($"[Balance] Mirror round {round + 1}/{Rounds}: " +
                          $"P {playerAlive} — E {enemyAlive}, {duration:F0} сим-с");

                // Зачистка раунда (юниты сами снимаются с регистрации в OnDestroy)
                DestroyRoundUnits(buffer);
                Time.timeScale = 1f;
                yield return null;
            }

            string winner = playerWins > enemyWins ? "Player" :
                            enemyWins > playerWins ? "Enemy"  : "Draw";

            BalanceReport.Write("balance-mirror.json", new BalanceReport.ClashResult
            {
                scenario       = $"Mirror 8v8 Marine vs Marine — серия {Rounds} раундов (винрейт)",
                winner         = $"{winner} (серия)",
                playerAlive    = playerWins, // в серии: число побед, не выживших
                enemyAlive     = enemyWins,
                playerHpLeft   = draws,
                enemyHpLeft    = timeouts,
                simDurationSec = totalDuration / Rounds,
            });

            // Главная гарантия круга 2: бои сходятся, таймаутов нет
            Assert.AreEqual(0, timeouts,
                $"Все {Rounds} раундов обязаны завершиться до {RoundSimLimit} сим-с (фикс ретрита, ADR-016).");

            // Серия не должна быть тотально односторонней: при честной монете
            // P(6:0) ≈ 3% — допускаем 5:1, но 6:0 сигналит о системном перекосе
            Assert.IsTrue(playerWins < Rounds && enemyWins < Rounds,
                $"Серия {Rounds}:0 — системная асимметрия ИИ. Player {playerWins}, Enemy {enemyWins}.");
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
        // Тест 2: асимметричный бой Marine vs Grunt — фактический баланс
        // ----------------------------------------------------------------

        [UnityTest]
        [Timeout(180000)]
        public IEnumerator AsymmetricClash_MarineVsGrunt_ResolvesAndIsRecorded()
        {
            var buffer = new List<Unit>(64);

            SpawnLine(Faction.Player, MarineData(), x: -5f, count: 8, namePrefix: "BalanceP");
            SpawnLine(Faction.Enemy,  GruntData(),  x: +5f, count: 8, namePrefix: "BalanceE");

            Time.timeScale = 10f;
            yield return null;

            float simStart = Time.time;
            yield return WaitForBattleEnd(simLimitSeconds: 240f, buffer);
            float duration = Time.time - simStart;

            Time.timeScale = 1f;

            int   playerAlive = CountAlive(Faction.Player, buffer);
            int   enemyAlive  = CountAlive(Faction.Enemy, buffer);
            float playerHp    = SumHp(Faction.Player, buffer);
            float enemyHp     = SumHp(Faction.Enemy, buffer);

            string winner = enemyAlive == 0 ? "Player" : playerAlive == 0 ? "Enemy"
                          : playerHp >= enemyHp ? "Player (по HP)" : "Enemy (по HP)";

            BalanceReport.Write("balance-asymmetric.json", new BalanceReport.ClashResult
            {
                scenario       = "Asymmetric 8v8 Marine vs Grunt",
                winner         = winner,
                playerAlive    = playerAlive,
                enemyAlive     = enemyAlive,
                playerHpLeft   = playerHp,
                enemyHpLeft    = enemyHp,
                simDurationSec = duration,
            });

            Assert.IsTrue(
                playerAlive < 8 || enemyAlive < 8,
                "За 240 сим-секунд асимметричного боя должны быть потери.");
        }

        // ----------------------------------------------------------------
        // Тест 3: стресс 20v20 + перф-метрики (baseline FPS/GC под нагрузкой)
        // ----------------------------------------------------------------

        [UnityTest]
        [Timeout(180000)]
        public IEnumerator StressClash_20v20_PerfBaselineRecorded()
        {
            var buffer = new List<Unit>(64);

            // retreatDisabled — максимальная плотность боя для стресса
            var stressMarine = UnitData.CreateForTest(
                displayName: "Marine", maxHp: 100f, damage: 10f, attackRange: 8f,
                attackCooldown: 1f, aggroRadius: 25f, moveSpeed: 5f,
                retreatHpFraction: 0f, retreatDisabled: true);

            var stressGrunt = UnitData.CreateForTest(
                displayName: "Enemy Grunt", maxHp: 80f, damage: 8f, attackRange: 7f,
                attackCooldown: 1.2f, aggroRadius: 25f, moveSpeed: 4.5f,
                retreatHpFraction: 0f, retreatDisabled: true);

            SpawnLine(Faction.Player, stressMarine, x: -8f, count: 20, namePrefix: "BalanceP");
            SpawnLine(Faction.Enemy,  stressGrunt,  x: +8f, count: 20, namePrefix: "BalanceE");

            var probeGo = new GameObject("BalancePerfProbe");
            var probe   = probeGo.AddComponent<PerfProbe>();

            Time.timeScale = 10f;
            yield return null;

            probe.StartRecording();

            // Записываем перф в разгар боя: 3 реальных секунды = 30 сим-секунд
            yield return new WaitForSecondsRealtime(3f);

            probe.StopRecording();
            Time.timeScale = 1f;

            BalanceReport.Write("balance-stress-perf.json", new BalanceReport.PerfResult
            {
                scenario          = "Stress 20v20, timeScale 10",
                frames            = probe.FrameTimesMs.Count,
                avgFrameMs        = probe.AverageMs,
                p95FrameMs        = probe.P95Ms,
                worstFrameMs      = probe.WorstMs,
                avgFps            = PerfStatsLogic.ToFps(probe.AverageMs),
                managedDeltaBytes = probe.ManagedDeltaBytes,
            });

            Assert.Greater(probe.FrameTimesMs.Count, 0, "PerfProbe должен записать хотя бы один кадр.");
            Assert.Greater(probe.AverageMs, 0f, "Среднее время кадра должно быть положительным.");
        }
    }

    /// <summary>
    /// Запись результатов баланс-плейтестов в Docs-Vault/Stats/ (JSON через JsonUtility).
    /// Файлы «latest» перезаписываются — историю накапливает вкладка Improve (Metrics.md).
    /// </summary>
    internal static class BalanceReport
    {
        [System.Serializable]
        public class ClashResult
        {
            public string scenario;
            public string winner;
            public int    playerAlive;
            public int    enemyAlive;
            public float  playerHpLeft;
            public float  enemyHpLeft;
            public float  simDurationSec;
        }

        [System.Serializable]
        public class PerfResult
        {
            public string scenario;
            public int    frames;
            public float  avgFrameMs;
            public float  p95FrameMs;
            public float  worstFrameMs;
            public float  avgFps;
            public long   managedDeltaBytes;
        }

        public static void Write(string fileName, object result)
        {
            try
            {
                string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Docs-Vault", "Stats"));
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                File.WriteAllText(Path.Combine(dir, fileName), JsonUtility.ToJson(result, prettyPrint: true));
            }
            catch (System.Exception e)
            {
                // Не валим тест из-за файловой системы (например, read-only CI)
                Debug.LogWarning($"[BalanceReport] Не удалось записать {fileName}: {e.Message}");
            }
        }
    }
}
