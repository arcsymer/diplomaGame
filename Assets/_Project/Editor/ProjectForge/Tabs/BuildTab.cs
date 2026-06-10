using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace DiplomaGame.Editor
{
    /// <summary>
    /// Сборка проекта. Путь и таргет фиксированы, параметры меняются здесь.
    /// </summary>
    internal sealed class BuildTab : IForgeTab
    {
        private const string BuildPath = "Builds/Windows/diplomaGame.exe";

        public string Title => "Build";

        public void OnGUI()
        {
            GUILayout.Space(8);
            GUILayout.Label("Сборка проекта", EditorStyles.boldLabel);
            GUILayout.Space(4);

            EditorGUILayout.HelpBox(
                $"Таргет: Windows x64 (Mono)\nВыходной путь: {BuildPath}",
                MessageType.Info);

            GUILayout.Space(4);

            if (GUILayout.Button("Build Windows x64 (Mono)", GUILayout.Height(32)))
                BuildWindows();
        }

        internal static void BuildWindows()
        {
            var enabledScenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            if (enabledScenes.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "Project Forge — Build",
                    "Нет сцен в Build Settings. Добавьте хотя бы одну сцену на вкладке Scenes.",
                    "OK");
                return;
            }

            var options = new BuildPlayerOptions
            {
                scenes            = enabledScenes,
                locationPathName  = BuildPath,
                target            = BuildTarget.StandaloneWindows64,
                options           = BuildOptions.None,
            };

            var startTime = DateTime.Now;
            var report = BuildPipeline.BuildPlayer(options);
            var elapsed = DateTime.Now - startTime;

            LogBuildResult(report, elapsed);

            if (report.summary.result == BuildResult.Succeeded)
                EditorUtility.RevealInFinder(BuildPath);
        }

        private static void LogBuildResult(BuildReport report, TimeSpan elapsed)
        {
            var summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                long sizeKb = (long)(summary.totalSize / 1024);
                Debug.Log(
                    $"[Project Forge] Build SUCCEEDED  |  " +
                    $"Размер: {sizeKb:N0} KB  |  " +
                    $"Время: {elapsed.TotalSeconds:F1}s  |  " +
                    $"Предупреждения: {summary.totalWarnings}");
            }
            else
            {
                Debug.LogError(
                    $"[Project Forge] Build FAILED  |  " +
                    $"Ошибок: {summary.totalErrors}  |  " +
                    $"Время: {elapsed.TotalSeconds:F1}s");
            }
        }
    }
}
