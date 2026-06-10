using UnityEditor.SceneManagement;

namespace DiplomaGame.Editor
{
    /// <summary>
    /// Точки входа для batch-режима (Unity -executeMethod).
    /// Используют ту же логику, что и кнопки Project Forge — никакой дублирующей реализации.
    /// </summary>
    public static class ForgeBatch
    {
        private const string SandboxScenePath = "Assets/_Project/Scenes/Sandbox.unity";

        /// <summary>Создаёт/обновляет песочницу и настраивает в ней риг режимов M1 (камеры + GameModeController).</summary>
        public static void SetupSandboxWithModeRig()
        {
            ScenesTab.CreateOrUpdateSandboxScene();
            EditorSceneManager.OpenScene(SandboxScenePath, OpenSceneMode.Single);
            ManagersTab.SetupModeRig();
        }

        /// <summary>Дописывает строку метрик в Docs-Vault/Stats/Statistics.md (та же логика, что кнопка Reports).</summary>
        public static void GenerateStatsReport()
        {
            ReportsTab.GenerateReport();
        }
    }
}
