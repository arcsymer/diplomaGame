using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace DiplomaGame.Editor
{
    /// <summary>
    /// Вкладка Improve — харнессы фазы улучшения (промпт v3, §5):
    /// сбор перф/баланс-метрик из последних Balance-плейтестов и кодовой статистики,
    /// накопительный экспорт в Docs-Vault/Improvements/Metrics.md.
    ///
    /// Balance-плейтесты запускаются Unity CLI:
    ///   -runTests -testPlatform PlayMode -testCategory Balance
    /// после чего кнопка/батч подбирает их JSON-результаты из Docs-Vault/Stats/.
    /// </summary>
    internal sealed class ImproveTab : IForgeTab
    {
        private const string MetricsFilePath = "Docs-Vault/Improvements/Metrics.md";

        private const string TableHeader =
            "| Дата | Билд, МБ | Скрипты | Строки | Тесты | Avg кадр, мс | p95, мс | Худший, мс | GC Δ, КБ | Зеркальный бой | Асимметричный бой |\n" +
            "|------|----------|---------|--------|-------|--------------|---------|------------|----------|----------------|-------------------|\n";

        public string Title => "Improve";

        public void OnGUI()
        {
            GUILayout.Space(8);
            GUILayout.Label("Фаза улучшения — метрики и харнессы", EditorStyles.boldLabel);
            GUILayout.Space(4);

            EditorGUILayout.HelpBox(
                "1. Прогоните Balance-плейтесты (CLI):\n" +
                "   Unity -runTests -testPlatform PlayMode -testCategory Balance\n" +
                "2. Нажмите кнопку — строка метрик допишется в:\n" +
                $"   {MetricsFilePath}\n" +
                "Существующие строки не перезаписываются (история накапливается).",
                MessageType.Info);

            GUILayout.Space(4);

            if (GUILayout.Button("Append Improvement Metrics Row", GUILayout.Height(32)))
                AppendMetricsRow();
        }

        // ----------------------------------------------------------------
        // Основная операция (используется и из ForgeBatch)
        // ----------------------------------------------------------------

        internal static void AppendMetricsRow()
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

            // 1. Код-статистика — переиспользуем ForgeStats (как ReportsTab)
            var snap = ForgeStats.Collect();

            // 2. Размер билда
            float buildMb = GetDirectorySizeMb(Path.Combine(projectRoot, "Builds", "Windows"));

            // 3. Результаты последних Balance-плейтестов
            var perf  = ReadJson<PerfDto>(projectRoot,  "balance-stress-perf.json");
            var mirr  = ReadJson<ClashDto>(projectRoot, "balance-mirror.json");
            var asym  = ReadJson<ClashDto>(projectRoot, "balance-asymmetric.json");

            string perfCells = perf != null
                ? $"{perf.avgFrameMs:F2} | {perf.p95FrameMs:F2} | {perf.worstFrameMs:F2} | {perf.managedDeltaBytes / 1024f:F0}"
                : "— | — | — | —";

            string mirrorCell = mirr != null
                ? $"{mirr.winner} ({mirr.playerAlive}:{mirr.enemyAlive})"
                : "—";

            string asymCell = asym != null
                ? $"{asym.winner} ({asym.playerAlive}:{asym.enemyAlive})"
                : "—";

            string row =
                $"| {DateTime.Now:yyyy-MM-dd} | {buildMb:F0} | {snap.ScriptCount} | {snap.TotalLines} | {snap.TestCount} " +
                $"| {perfCells} | {mirrorCell} | {asymCell} |";

            AppendRowToFile(Path.Combine(projectRoot, MetricsFilePath), row);

            Debug.Log($"[Project Forge] Improvement metrics: {row}");
        }

        // ----------------------------------------------------------------
        // Вспомогательные методы
        // ----------------------------------------------------------------

        [Serializable]
        private class PerfDto
        {
            public float avgFrameMs;
            public float p95FrameMs;
            public float worstFrameMs;
            public long  managedDeltaBytes;
        }

        [Serializable]
        private class ClashDto
        {
            public string winner;
            public int    playerAlive;
            public int    enemyAlive;
        }

        private static T ReadJson<T>(string projectRoot, string fileName) where T : class
        {
            string path = Path.Combine(projectRoot, "Docs-Vault", "Stats", fileName);
            if (!File.Exists(path))
                return null;

            try
            {
                return JsonUtility.FromJson<T>(File.ReadAllText(path));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Project Forge] Не удалось прочитать {fileName}: {e.Message}");
                return null;
            }
        }

        private static float GetDirectorySizeMb(string dir)
        {
            if (!Directory.Exists(dir))
                return 0f;

            long bytes = 0;
            foreach (var file in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
                bytes += new FileInfo(file).Length;

            return bytes / (1024f * 1024f);
        }

        private static void AppendRowToFile(string fullPath, string row)
        {
            string dir = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (!File.Exists(fullPath))
            {
                File.WriteAllText(fullPath,
                    "# Метрики фазы улучшения (накопительно)\n\n" +
                    "> Дописывается кнопкой Improve → Append Improvement Metrics Row " +
                    "(или ForgeBatch.ExportImprovementMetrics). Строки не перезаписываются.\n\n" +
                    TableHeader + row + "\n",
                    Encoding.UTF8);
            }
            else
            {
                File.AppendAllText(fullPath, row + "\n", Encoding.UTF8);
            }
        }
    }
}
