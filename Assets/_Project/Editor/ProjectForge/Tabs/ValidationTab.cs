using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DiplomaGame.Editor
{
    /// <summary>
    /// Проверка целостности проекта и начальный Bootstrap структуры папок.
    /// </summary>
    internal sealed class ValidationTab : IForgeTab
    {
        private List<string> _lastResult;
        private bool _validated;

        public string Title => "Validation";

        public void OnGUI()
        {
            GUILayout.Space(8);
            GUILayout.Label("Валидация проекта", EditorStyles.boldLabel);
            GUILayout.Space(4);

            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Validate Project", GUILayout.Height(32)))
                    RunValidation();

                if (GUILayout.Button("Bootstrap Project Structure", GUILayout.Height(32)))
                    RunBootstrap();
            }

            GUILayout.Space(8);

            if (_validated)
                DrawResults();
        }

        private void RunValidation()
        {
            _lastResult = ForgeValidator.Validate();
            _validated = true;

            if (_lastResult.Count == 0)
                Debug.Log("[Project Forge] Validation: OK — проблем не найдено.");
            else
                foreach (var issue in _lastResult)
                    Debug.LogWarning("[Project Forge] Validation: " + issue);
        }

        private void RunBootstrap()
        {
            ForgeValidator.BootstrapProjectStructure();
            Debug.Log("[Project Forge] Bootstrap выполнен — структура папок создана.");
            // Автоматически перепроверяем после Bootstrap
            RunValidation();
        }

        private void DrawResults()
        {
            if (_lastResult == null || _lastResult.Count == 0)
            {
                var okStyle = new GUIStyle(EditorStyles.label)
                {
                    normal = { textColor = new Color(0.2f, 0.8f, 0.2f) },
                    fontStyle = FontStyle.Bold
                };
                GUILayout.Label("OK — проблем не обнаружено.", okStyle);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    $"Найдено проблем: {_lastResult.Count}",
                    MessageType.Warning);

                foreach (var issue in _lastResult)
                {
                    using (new GUILayout.HorizontalScope())
                    {
                        GUILayout.Label("•", GUILayout.Width(12));
                        GUILayout.Label(issue, EditorStyles.wordWrappedLabel);
                    }
                }
            }
        }
    }
}
