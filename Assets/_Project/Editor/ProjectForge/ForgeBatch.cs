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

        /// <summary>
        /// Полная настройка M2: открыть Sandbox → запечь NavMesh → создать префаб TestUnit → настроить RTS-управление.
        /// </summary>
        public static void SetupM2()
        {
            EditorSceneManager.OpenScene(SandboxScenePath, OpenSceneMode.Single);
            NavMeshTab.BakeNavMesh();
            PrefabsTab.CreateOrUpdateTestUnitPrefab();
            ManagersTab.SetupRtsControl();
        }

        /// <summary>
        /// Полная настройка M3: открыть Sandbox → настроить TPS-героя.
        /// </summary>
        public static void SetupM3()
        {
            EditorSceneManager.OpenScene(SandboxScenePath, OpenSceneMode.Single);
            ManagersTab.SetupHero();
        }

        /// <summary>
        /// Полная настройка M4: открыть Sandbox → создать UnitData-ассеты →
        /// обновить оба префаба → настроить бой в сцене.
        /// </summary>
        public static void SetupM4()
        {
            EditorSceneManager.OpenScene(SandboxScenePath, OpenSceneMode.Single);
            ConfigTab.CreateOrUpdateUnitDataAssets();
            PrefabsTab.CreateOrUpdateTestUnitPrefab();
            PrefabsTab.CreateOrUpdateEnemyUnitPrefab();
            ManagersTab.SetupCombat();
        }
    }
}
