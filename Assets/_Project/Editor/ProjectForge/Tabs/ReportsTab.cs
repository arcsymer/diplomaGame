using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace DiplomaGame.Editor
{
    /// <summary>
    /// Генерация и накопление статистики проекта в Docs-Vault.
    /// Существующие строки таблицы НЕ перезаписываются — только дописывается новая.
    /// </summary>
    internal sealed class ReportsTab : IForgeTab
    {
        // Путь относительно корня проекта (родитель Assets/)
        private const string StatsFilePath = "Docs-Vault/Stats/Statistics.md";

        private const string TableHeader =
            "| Дата | Скрипты | Строки кода | Сцены | Тесты |\n" +
            "|------|---------|-------------|-------|-------|\n";

        public string Title => "Reports";

        public void OnGUI()
        {
            GUILayout.Space(8);
            GUILayout.Label("Отчёты и статистика", EditorStyles.boldLabel);
            GUILayout.Space(4);

            EditorGUILayout.HelpBox(
                $"Статистика дописывается в: {StatsFilePath}",
                MessageType.Info);

            GUILayout.Space(4);

            if (GUILayout.Button("Generate Stats Report", GUILayout.Height(32)))
                GenerateReport();
        }

        private static void GenerateReport()
        {
            var snap = ForgeStats.Collect();
            string row = FormatRow(snap);

            WriteRowToFile(row);

            Debug.Log(
                $"[Project Forge] Stats: " +
                $"скрипты={snap.ScriptCount}, " +
                $"строки={snap.TotalLines}, " +
                $"сцены={snap.SceneCount}, " +
                $"тесты={snap.TestCount}");
        }

        private static string FormatRow(ForgeStats.Snapshot snap)
        {
            string date = DateTime.Now.ToString("yyyy-MM-dd");
            return $"| {date} | {snap.ScriptCount} | {snap.TotalLines} | {snap.SceneCount} | {snap.TestCount} |";
        }

        private static void WriteRowToFile(string row)
        {
            // Путь от корня репозитория (родитель Assets/)
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            string fullPath    = Path.Combine(projectRoot, StatsFilePath);

            // Создаём папку Stats, если её нет
            string dir = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (!File.Exists(fullPath))
            {
                // Создаём файл с заголовком и первой строкой
                File.WriteAllText(fullPath,
                    "# Статистика проекта\n\n" + TableHeader + row + "\n",
                    Encoding.UTF8);
            }
            else
            {
                // Дописываем строку в конец (существующие данные не трогаем)
                File.AppendAllText(fullPath, row + "\n", Encoding.UTF8);
            }
        }
    }
}
