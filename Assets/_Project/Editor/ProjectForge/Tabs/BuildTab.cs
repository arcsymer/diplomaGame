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
        private const string BuildPathWindows = "Builds/Windows/diplomaGame.exe";
        private const string BuildPathWebGL   = "Builds/WebGL";

        public string Title => "Build";

        public void OnGUI()
        {
            GUILayout.Space(8);
            GUILayout.Label("Сборка проекта", EditorStyles.boldLabel);
            GUILayout.Space(4);

            EditorGUILayout.HelpBox(
                $"Таргет: Windows x64 (Mono)\nВыходной путь: {BuildPathWindows}",
                MessageType.Info);

            GUILayout.Space(4);

            if (GUILayout.Button("Build Windows x64 (Mono)", GUILayout.Height(32)))
                BuildWindows();

            GUILayout.Space(8);

            EditorGUILayout.HelpBox(
                $"Таргет: WebGL (v8)\nВыходной путь: {BuildPathWebGL}\n" +
                "Compression: Gzip + DecompressionFallback (GitHub Pages compatible)",
                MessageType.Info);

            GUILayout.Space(4);

            if (GUILayout.Button("Build WebGL (v8)", GUILayout.Height(32)))
                BuildWebGL();
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
                locationPathName  = BuildPathWindows,
                target            = BuildTarget.StandaloneWindows64,
                options           = BuildOptions.None,
            };

            var startTime = DateTime.Now;
            var report = BuildPipeline.BuildPlayer(options);
            var elapsed = DateTime.Now - startTime;

            LogBuildResult(report, elapsed);

            if (report.summary.result == BuildResult.Succeeded)
                EditorUtility.RevealInFinder(BuildPathWindows);
        }

        /// <summary>
        /// Сборка WebGL (v8). PlayerSettings выставляются идемпотентно перед сборкой
        /// и рассчитаны на хостинг без спец-заголовков (GitHub Pages):
        /// Gzip + DecompressionFallback, runInBackground, template Default.
        /// </summary>
        internal static void BuildWebGL()
        {
            var enabledScenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            if (enabledScenes.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "Project Forge — Build WebGL",
                    "Нет сцен в Build Settings. Добавьте хотя бы одну сцену на вкладке Scenes.",
                    "OK");
                return;
            }

            // ----------------------------------------------------------------
            // PlayerSettings для WebGL — идемпотентно
            // ----------------------------------------------------------------

            // Gzip + fallback: позволяет хостить на GitHub Pages без Content-Encoding заголовков
            PlayerSettings.WebGL.compressionFormat     = WebGLCompressionFormat.Gzip;
            PlayerSettings.WebGL.decompressionFallback = true;

            // Стандартный шаблон Unity (APPLICATION:Default)
            PlayerSettings.WebGL.template = "APPLICATION:Default";

            // Игра не должна останавливаться при потере фокуса браузерной вкладки
            PlayerSettings.runInBackground = true;

            // Графика: WebGL 2 через авто-выбор (не задаём конкретный API,
            // Unity сам ставит WebGL2 как единственный поддерживаемый)
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.WebGL, true);

            // ----------------------------------------------------------------
            // Сборка
            // ----------------------------------------------------------------

            var options = new BuildPlayerOptions
            {
                scenes           = enabledScenes,
                locationPathName = BuildPathWebGL,
                target           = BuildTarget.WebGL,
                options          = BuildOptions.None,
            };

            var startTime = DateTime.Now;
            var report    = BuildPipeline.BuildPlayer(options);
            var elapsed   = DateTime.Now - startTime;

            LogBuildResult(report, elapsed);

            if (report.summary.result == BuildResult.Succeeded)
                EditorUtility.RevealInFinder(BuildPathWebGL);
        }

        private static void LogBuildResult(BuildReport report, TimeSpan elapsed)
        {
            var summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                long sizeMb = (long)(summary.totalSize / (1024 * 1024));
                Debug.Log(
                    $"[Project Forge] Build SUCCEEDED  |  " +
                    $"Размер: {sizeMb:N0} MB  |  " +
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
