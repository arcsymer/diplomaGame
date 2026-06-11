using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using DiplomaGame.Runtime.Combat;
using DiplomaGame.Runtime.Data;
using DiplomaGame.Runtime.Units;
using NUnit.Framework;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.TestTools;

namespace DiplomaGame.Tests.Runtime
{
    /// <summary>
    /// Диагностическая серия 12 раундов 8v8 Marine vs Marine.
    /// Категория BalanceDiag — запускается отдельно от категории Balance.
    ///
    /// Пишет подробный JSON в Docs-Vault/Stats/balance-diag.json:
    ///   - per-round: first-shot faction, first-kill faction, first-retreat faction, winner, EntityId-хэши
    ///   - корреляции: first-shot->win, first-kill->win, first-retreat->loss
    ///
    /// Цель: найти механизм систематической асимметрии (4:14 в пользу Enemy).
    /// </summary>
    [TestFixture]
    [Category("BalanceDiag")]
    public class BalanceDiagnosticsTests
    {
        private GameObject     _groundGo;
        private NavMeshSurface _surface;

        [SetUp]
        public void SetUp()
        {
            _groundGo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            _groundGo.name = "DiagGround";
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
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;

            foreach (var go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
            {
                if (go == null) continue;
                if (go.name.StartsWith("Diag") || go.name.EndsWith("BaseSpawn"))
                    UnityEngine.Object.DestroyImmediate(go);
            }

            NavMesh.RemoveAllNavMeshData();
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

        private static void SpawnLine(Faction faction, UnitData data, float x, int count, string prefix)
        {
            float zStart = -(count - 1);
            for (int i = 0; i < count; i++)
                CreateUnit($"{prefix}_{i}", faction, new Vector3(x, 0f, zStart + i * 2f), data);
        }

        private static UnitData MarineData() => UnitData.CreateForTest(
            displayName: "Marine", maxHp: 100f, damage: 10f, attackRange: 8f,
            attackCooldown: 1f, aggroRadius: 12f, moveSpeed: 5f,
            retreatHpFraction: 0.25f, retreatDisabled: false);

        // ----------------------------------------------------------------
        // Ожидание конца боя
        // ----------------------------------------------------------------

        private static IEnumerator WaitForBattleEnd(float simLimitSeconds, List<Unit> buffer)
        {
            const float PollStep = 0.25f;
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
                var h = buffer[i].GetComponent<Health>();
                if (h != null && !h.IsDead) alive++;
            }
            return alive;
        }

        private static void DestroyRoundUnits(List<Unit> buffer)
        {
            UnitRegistry.GetUnits(Faction.Player, buffer);
            for (int i = buffer.Count - 1; i >= 0; i--)
                if (buffer[i] != null) UnityEngine.Object.DestroyImmediate(buffer[i].gameObject);

            UnitRegistry.GetUnits(Faction.Enemy, buffer);
            for (int i = buffer.Count - 1; i >= 0; i--)
                if (buffer[i] != null) UnityEngine.Object.DestroyImmediate(buffer[i].gameObject);
        }

        // ----------------------------------------------------------------
        // Диагностический тест: 12 раундов с полным логированием
        // ----------------------------------------------------------------

        [UnityTest]
        [Timeout(600000)]
        public IEnumerator DiagnosticSeries_12Rounds_LogCorrelations()
        {
            const int   Rounds        = 12;
            const float RoundSimLimit = 240f;

            var buffer = new List<Unit>(64);
            var rounds = new List<RoundData>(Rounds);

            for (int round = 0; round < Rounds; round++)
            {
                var rd = new RoundData { roundIndex = round };
                rounds.Add(rd);

                // Флаги для захвата первых событий
                bool firstShotCaptured    = false;
                bool firstKillCaptured    = false;
                bool firstRetreatCaptured = false;

                // --- Собираем EntityId-хэши ---
                // (до спавна, чтобы знать сколько объектов уже есть)
                int preSpawnCount = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None).Length;
                rd.preSpawnObjectCount = preSpawnCount;

                // --- Подписки на события ---
                Action<Faction, Vector3, int> onAttack = (faction, pos, entityId) =>
                {
                    if (!firstShotCaptured)
                    {
                        firstShotCaptured    = true;
                        rd.firstShotFaction  = faction;
                        rd.firstShotEntityId = entityId;
                        rd.firstShotTime     = Time.time;
                        // Записываем хэш-offset фактического ID
                        rd.firstShotHashOffset = ComputeHashOffset(1.0f, entityId);
                        rd.firstShotScanOffset = ComputeHashOffset(0.25f, entityId * 31);
                    }
                };

                Action<Health> onDied = (health) =>
                {
                    if (!firstKillCaptured)
                    {
                        firstKillCaptured    = true;
                        var unit             = health.GetComponent<Unit>();
                        rd.firstKillFaction  = unit != null ? unit.Faction : Faction.Player;
                        rd.firstKillTime     = Time.time;
                    }
                };

                Action<Faction, Vector3> onRetreat = (faction, pos) =>
                {
                    if (!firstRetreatCaptured)
                    {
                        firstRetreatCaptured    = true;
                        rd.firstRetreatFaction  = faction;
                        rd.firstRetreatTime     = Time.time;
                    }
                };

                UnitCombat.AnyAttackedWithFaction += onAttack;
                Health.AnyDied                    += onDied;
                UnitCombat.AnyRetreatStarted      += onRetreat;

                // --- Спавн: чередование ---
                float spawnStart = Time.time;
                if (round % 2 == 0)
                {
                    SpawnLine(Faction.Player, MarineData(), x: -5f, count: 8, prefix: "DiagP");
                    SpawnLine(Faction.Enemy,  MarineData(), x: +5f, count: 8, prefix: "DiagE");
                    rd.spawnOrder = "Player_first";
                }
                else
                {
                    SpawnLine(Faction.Enemy,  MarineData(), x: +5f, count: 8, prefix: "DiagE");
                    SpawnLine(Faction.Player, MarineData(), x: -5f, count: 8, prefix: "DiagP");
                    rd.spawnOrder = "Enemy_first";
                }

                // --- Собираем фактические EntityId и offset-ы всех юнитов ---
                CollectEntityIdOffsets(rd, buffer);

                Time.timeScale = 10f;
                yield return null;

                float simStart = Time.time;
                yield return WaitForBattleEnd(RoundSimLimit, buffer);
                float simEnd = Time.time;

                rd.simDuration = simEnd - simStart;

                // --- Отписка ---
                UnitCombat.AnyAttackedWithFaction -= onAttack;
                Health.AnyDied                    -= onDied;
                UnitCombat.AnyRetreatStarted      -= onRetreat;

                // --- Итог ---
                int playerAlive = CountAlive(Faction.Player, buffer);
                int enemyAlive  = CountAlive(Faction.Enemy, buffer);

                rd.playerAliveEnd = playerAlive;
                rd.enemyAliveEnd  = enemyAlive;
                rd.timeout        = (playerAlive > 0 && enemyAlive > 0);

                if      (enemyAlive == 0 && playerAlive > 0) rd.winner = "Player";
                else if (playerAlive == 0 && enemyAlive > 0) rd.winner = "Enemy";
                else                                          rd.winner = "Draw";

                rd.firstShotWon    = rd.firstShotFaction.HasValue  && rd.winner == rd.firstShotFaction.Value.ToString();
                rd.firstKillWon    = rd.firstKillFaction.HasValue  && rd.winner == rd.firstKillFaction.Value.ToString();
                rd.firstRetreatLost = rd.firstRetreatFaction.HasValue && rd.winner != rd.firstRetreatFaction.Value.ToString();

                Debug.Log($"[BalanceDiag] Round {round + 1}/{Rounds} ({rd.spawnOrder}): " +
                          $"Winner={rd.winner} | " +
                          $"FirstShot={rd.firstShotFaction?.ToString() ?? "none"} " +
                          $"FirstKill={rd.firstKillFaction?.ToString() ?? "none"} " +
                          $"FirstRetreat={rd.firstRetreatFaction?.ToString() ?? "none"} | " +
                          $"P_alive={playerAlive} E_alive={enemyAlive} | " +
                          $"Dur={rd.simDuration:F0}s");

                DestroyRoundUnits(buffer);
                Time.timeScale = 1f;
                yield return null;
            }

            // ----------------------------------------------------------------
            // Вычисляем корреляции
            // ----------------------------------------------------------------

            int playerWins = 0, enemyWins = 0;
            int firstShotWinCount = 0, firstShotTotal = 0;
            int firstKillWinCount = 0, firstKillTotal = 0;
            int firstRetreatLossCount = 0, firstRetreatTotal = 0;
            int firstShotPlayer = 0, firstShotEnemy = 0;
            int firstKillPlayer = 0, firstKillEnemy = 0;
            int firstRetreatPlayer = 0, firstRetreatEnemy = 0;

            foreach (var rd in rounds)
            {
                if (rd.winner == "Player") playerWins++;
                if (rd.winner == "Enemy")  enemyWins++;

                if (rd.firstShotFaction.HasValue)
                {
                    firstShotTotal++;
                    if (rd.firstShotWon) firstShotWinCount++;
                    if (rd.firstShotFaction.Value == Faction.Player) firstShotPlayer++;
                    else firstShotEnemy++;
                }
                if (rd.firstKillFaction.HasValue)
                {
                    firstKillTotal++;
                    if (rd.firstKillWon) firstKillWinCount++;
                    if (rd.firstKillFaction.Value == Faction.Player) firstKillPlayer++;
                    else firstKillEnemy++;
                }
                if (rd.firstRetreatFaction.HasValue)
                {
                    firstRetreatTotal++;
                    if (rd.firstRetreatLost) firstRetreatLossCount++;
                    if (rd.firstRetreatFaction.Value == Faction.Player) firstRetreatPlayer++;
                    else firstRetreatEnemy++;
                }
            }

            // ----------------------------------------------------------------
            // JSON-отчёт
            // ----------------------------------------------------------------

            WriteJson(rounds, new CorrelationSummary
            {
                totalRounds         = Rounds,
                playerWins          = playerWins,
                enemyWins           = enemyWins,
                draws               = Rounds - playerWins - enemyWins,
                firstShotPlayer     = firstShotPlayer,
                firstShotEnemy      = firstShotEnemy,
                firstShotWinRate    = firstShotTotal > 0 ? (float)firstShotWinCount / firstShotTotal : 0f,
                firstKillPlayer     = firstKillPlayer,
                firstKillEnemy      = firstKillEnemy,
                firstKillWinRate    = firstKillTotal > 0 ? (float)firstKillWinCount / firstKillTotal : 0f,
                firstRetreatPlayer  = firstRetreatPlayer,
                firstRetreatEnemy   = firstRetreatEnemy,
                firstRetreatLossRate = firstRetreatTotal > 0 ? (float)firstRetreatLossCount / firstRetreatTotal : 0f,
            });

            Debug.Log($"[BalanceDiag] === SUMMARY {Rounds} rounds ===");
            Debug.Log($"[BalanceDiag] Wins: Player={playerWins} Enemy={enemyWins}");
            Debug.Log($"[BalanceDiag] FirstShot: P={firstShotPlayer} E={firstShotEnemy} -> win rate of first-shooter: {(firstShotTotal > 0 ? (float)firstShotWinCount/firstShotTotal : 0):P0}");
            Debug.Log($"[BalanceDiag] FirstKill: P={firstKillPlayer} E={firstKillEnemy} -> win rate of first-killer: {(firstKillTotal > 0 ? (float)firstKillWinCount/firstKillTotal : 0):P0}");
            Debug.Log($"[BalanceDiag] FirstRetreat: P={firstRetreatPlayer} E={firstRetreatEnemy} -> loss rate of first-retreater: {(firstRetreatTotal > 0 ? (float)firstRetreatLossCount/firstRetreatTotal : 0):P0}");

            // Тест считается успешным всегда — его цель диагностика, не assertion
            Assert.IsTrue(playerWins + enemyWins + (Rounds - playerWins - enemyWins) == Rounds,
                "Sanity: all rounds counted.");
        }

        // ----------------------------------------------------------------
        // Вспомогательные методы
        // ----------------------------------------------------------------

        private static float ComputeHashOffset(float cooldown, int seed)
        {
            // MurmurHash3 finalizer — должен совпадать с CombatLogic.RandomiseInitialCooldownOffset
            uint h = (uint)seed;
            h ^= h >> 16;
            h *= 0x85ebca6bu;
            h ^= h >> 13;
            h *= 0xc2b2ae35u;
            h ^= h >> 16;
            float t = (h & 0x7FFFFFFFu) / (float)0x7FFFFFFF;
            return t * cooldown;
        }

        // Faction salts — должны совпадать с UnitCombat.Start / InitForTest
        private const int PlayerFactionSalt = 0x27D4EB2F;
        private const int EnemyFactionSalt  = unchecked((int)0x9B2257E5u);

        private static void CollectEntityIdOffsets(RoundData rd, List<Unit> buffer)
        {
            var playerOffsets = new List<float>();
            var enemyOffsets  = new List<float>();
            var playerIds     = new List<int>();
            var enemyIds      = new List<int>();

            UnitRegistry.GetUnits(Faction.Player, buffer);
            foreach (var u in buffer)
            {
                if (u == null) continue;
                int eid = (int)u.gameObject.GetEntityId();
                playerIds.Add(eid);
                // Применяем ту же faction-salt, что и UnitCombat, чтобы offset совпадал
                playerOffsets.Add(ComputeHashOffset(1.0f, eid ^ PlayerFactionSalt));
            }

            UnitRegistry.GetUnits(Faction.Enemy, buffer);
            foreach (var u in buffer)
            {
                if (u == null) continue;
                int eid = (int)u.gameObject.GetEntityId();
                enemyIds.Add(eid);
                enemyOffsets.Add(ComputeHashOffset(1.0f, eid ^ EnemyFactionSalt));
            }

            rd.playerEntityIds  = playerIds.ToArray();
            rd.enemyEntityIds   = enemyIds.ToArray();
            rd.playerAttackOffsets = playerOffsets.ToArray();
            rd.enemyAttackOffsets  = enemyOffsets.ToArray();

            if (playerOffsets.Count > 0)
                rd.playerAvgOffset = AverageOf(playerOffsets);
            if (enemyOffsets.Count > 0)
                rd.enemyAvgOffset = AverageOf(enemyOffsets);

            // Минимальный offset = самый ранний первый выстрел в команде
            rd.playerMinOffset = playerOffsets.Count > 0 ? MinOf(playerOffsets) : -1f;
            rd.enemyMinOffset  = enemyOffsets.Count > 0  ? MinOf(enemyOffsets)  : -1f;
        }

        private static float AverageOf(List<float> list)
        {
            float s = 0f;
            for (int i = 0; i < list.Count; i++) s += list[i];
            return s / list.Count;
        }

        private static float MinOf(List<float> list)
        {
            float m = float.MaxValue;
            for (int i = 0; i < list.Count; i++) if (list[i] < m) m = list[i];
            return m;
        }

        // ----------------------------------------------------------------
        // JSON-запись
        // ----------------------------------------------------------------

        private static void WriteJson(List<RoundData> rounds, CorrelationSummary summary)
        {
            try
            {
                string dir = Path.GetFullPath(Path.Combine(
                    Application.dataPath, "..", "Docs-Vault", "Stats"));
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                var sb = new StringBuilder();
                sb.AppendLine("{");
                sb.AppendLine($"  \"generatedAt\": \"{DateTime.UtcNow:yyyy-MM-ddTHH:mm:ssZ}\",");

                // Summary
                sb.AppendLine("  \"summary\": {");
                sb.AppendLine($"    \"totalRounds\": {summary.totalRounds},");
                sb.AppendLine($"    \"playerWins\": {summary.playerWins},");
                sb.AppendLine($"    \"enemyWins\": {summary.enemyWins},");
                sb.AppendLine($"    \"draws\": {summary.draws},");
                sb.AppendLine($"    \"firstShotPlayer\": {summary.firstShotPlayer},");
                sb.AppendLine($"    \"firstShotEnemy\": {summary.firstShotEnemy},");
                sb.AppendLine($"    \"firstShotWinRate\": {summary.firstShotWinRate:F3},");
                sb.AppendLine($"    \"firstKillPlayer\": {summary.firstKillPlayer},");
                sb.AppendLine($"    \"firstKillEnemy\": {summary.firstKillEnemy},");
                sb.AppendLine($"    \"firstKillWinRate\": {summary.firstKillWinRate:F3},");
                sb.AppendLine($"    \"firstRetreatPlayer\": {summary.firstRetreatPlayer},");
                sb.AppendLine($"    \"firstRetreatEnemy\": {summary.firstRetreatEnemy},");
                sb.AppendLine($"    \"firstRetreatLossRate\": {summary.firstRetreatLossRate:F3}");
                sb.AppendLine("  },");

                // Per-round data
                sb.AppendLine("  \"rounds\": [");
                for (int i = 0; i < rounds.Count; i++)
                {
                    var rd = rounds[i];
                    sb.AppendLine("    {");
                    sb.AppendLine($"      \"index\": {rd.roundIndex},");
                    sb.AppendLine($"      \"spawnOrder\": \"{rd.spawnOrder}\",");
                    sb.AppendLine($"      \"winner\": \"{rd.winner}\",");
                    sb.AppendLine($"      \"playerAliveEnd\": {rd.playerAliveEnd},");
                    sb.AppendLine($"      \"enemyAliveEnd\": {rd.enemyAliveEnd},");
                    sb.AppendLine($"      \"timeout\": {(rd.timeout ? "true" : "false")},");
                    sb.AppendLine($"      \"simDuration\": {rd.simDuration:F2},");
                    sb.AppendLine($"      \"firstShotFaction\": \"{rd.firstShotFaction?.ToString() ?? "none"}\",");
                    sb.AppendLine($"      \"firstShotEntityId\": {rd.firstShotEntityId},");
                    sb.AppendLine($"      \"firstShotHashOffset\": {rd.firstShotHashOffset:F4},");
                    sb.AppendLine($"      \"firstShotScanOffset\": {rd.firstShotScanOffset:F4},");
                    sb.AppendLine($"      \"firstShotTime\": {rd.firstShotTime:F3},");
                    sb.AppendLine($"      \"firstShotWon\": {(rd.firstShotWon ? "true" : "false")},");
                    sb.AppendLine($"      \"firstKillFaction\": \"{rd.firstKillFaction?.ToString() ?? "none"}\",");
                    sb.AppendLine($"      \"firstKillTime\": {rd.firstKillTime:F3},");
                    sb.AppendLine($"      \"firstKillWon\": {(rd.firstKillWon ? "true" : "false")},");
                    sb.AppendLine($"      \"firstRetreatFaction\": \"{rd.firstRetreatFaction?.ToString() ?? "none"}\",");
                    sb.AppendLine($"      \"firstRetreatTime\": {rd.firstRetreatTime:F3},");
                    sb.AppendLine($"      \"firstRetreatLost\": {(rd.firstRetreatLost ? "true" : "false")},");
                    sb.AppendLine($"      \"playerAvgOffset\": {rd.playerAvgOffset:F4},");
                    sb.AppendLine($"      \"enemyAvgOffset\": {rd.enemyAvgOffset:F4},");
                    sb.AppendLine($"      \"playerMinOffset\": {rd.playerMinOffset:F4},");
                    sb.AppendLine($"      \"enemyMinOffset\": {rd.enemyMinOffset:F4},");
                    sb.AppendLine($"      \"hashAdvantageFaction\": \"{(rd.playerMinOffset < rd.enemyMinOffset ? "Player" : "Enemy")}\",");
                    sb.AppendLine($"      \"preSpawnObjectCount\": {rd.preSpawnObjectCount},");
                    // EntityIds
                    sb.Append("      \"playerEntityIds\": [");
                    if (rd.playerEntityIds != null)
                        sb.Append(string.Join(",", rd.playerEntityIds));
                    sb.AppendLine("],");
                    sb.Append("      \"enemyEntityIds\": [");
                    if (rd.enemyEntityIds != null)
                        sb.Append(string.Join(",", rd.enemyEntityIds));
                    sb.AppendLine("]");
                    sb.Append("    }");
                    if (i < rounds.Count - 1) sb.Append(",");
                    sb.AppendLine();
                }
                sb.AppendLine("  ]");
                sb.AppendLine("}");

                File.WriteAllText(Path.Combine(dir, "balance-diag.json"), sb.ToString());
                Debug.Log($"[BalanceDiag] Wrote balance-diag.json to {dir}");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[BalanceDiag] Failed to write JSON: {e.Message}");
            }
        }

        // ----------------------------------------------------------------
        // Data structures
        // ----------------------------------------------------------------

        private class RoundData
        {
            public int     roundIndex;
            public string  spawnOrder  = "";
            public string  winner      = "Draw";
            public int     playerAliveEnd;
            public int     enemyAliveEnd;
            public bool    timeout;
            public float   simDuration;
            public int     preSpawnObjectCount;

            // First shot
            public Faction? firstShotFaction;
            public int      firstShotEntityId;
            public float    firstShotHashOffset;
            public float    firstShotScanOffset;
            public float    firstShotTime;
            public bool     firstShotWon;

            // First kill (faction that was killed = losing side first kill)
            public Faction? firstKillFaction;
            public float    firstKillTime;
            public bool     firstKillWon;

            // First retreat
            public Faction? firstRetreatFaction;
            public float    firstRetreatTime;
            public bool     firstRetreatLost;

            // Hash offset analysis
            public float   playerAvgOffset;
            public float   enemyAvgOffset;
            public float   playerMinOffset = -1f;
            public float   enemyMinOffset  = -1f;
            public int[]   playerEntityIds;
            public int[]   enemyEntityIds;
            public float[] playerAttackOffsets;
            public float[] enemyAttackOffsets;
        }

        private class CorrelationSummary
        {
            public int   totalRounds;
            public int   playerWins;
            public int   enemyWins;
            public int   draws;
            public int   firstShotPlayer;
            public int   firstShotEnemy;
            public float firstShotWinRate;
            public int   firstKillPlayer;
            public int   firstKillEnemy;
            public float firstKillWinRate;
            public int   firstRetreatPlayer;
            public int   firstRetreatEnemy;
            public float firstRetreatLossRate;
        }
    }
}
