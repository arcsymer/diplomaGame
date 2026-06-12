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

        /// <summary>
        /// Полная настройка M5: открыть Sandbox → создать BuildingData-ассеты →
        /// создать Building-префабы → настроить экономику в сцене.
        /// </summary>
        public static void SetupM5()
        {
            EditorSceneManager.OpenScene(SandboxScenePath, OpenSceneMode.Single);
            ConfigTab.CreateOrUpdateBuildingDataAssets();
            PrefabsTab.CreateOrUpdateBuildingPrefabs();
            ManagersTab.SetupEconomy();
        }

        /// <summary>
        /// Полная настройка M6a: открыть Sandbox → собрать игровой HUD обоих режимов.
        /// </summary>
        public static void SetupM6Hud()
        {
            EditorSceneManager.OpenScene(SandboxScenePath, OpenSceneMode.Single);
            UITab.BuildGameHud();
        }

        /// <summary>
        /// Сборка системы тултипов: открыть Sandbox → обновить описания ассетов → собрать TooltipCanvas.
        /// Вызывается как отдельный шаг или включается в полный батч через SetupFull.
        /// </summary>
        public static void SetupTooltips()
        {
            EditorSceneManager.OpenScene(SandboxScenePath, OpenSceneMode.Single);
            ConfigTab.UpdateTooltipDescriptions();
            UITab.BuildTooltipSystem();
        }

        /// <summary>
        /// Полная настройка M6b: открыть Sandbox → собрать меню паузы и GameOver →
        /// создать/обновить MainMenu-сцену.
        /// Sandbox остаётся последней сохранённой (MainMenu сохраняется внутри CreateOrUpdateMainMenuScene,
        /// после чего мы переоткрываем Sandbox).
        /// </summary>
        public static void SetupM6Menus()
        {
            // 1. Sandbox: добавляем PauseMenu + GameOver
            EditorSceneManager.OpenScene(SandboxScenePath, OpenSceneMode.Single);
            UITab.BuildMenus();

            // 2. Создаём/обновляем MainMenu-сцену (открывает и сохраняет её)
            ScenesTab.CreateOrUpdateMainMenuScene();

            // 3. Возвращаемся в Sandbox, чтобы он был активным после batch
            EditorSceneManager.OpenScene(SandboxScenePath, OpenSceneMode.Single);
        }

        /// <summary>
        /// Полная настройка M7: аудио.
        /// Создаёт GameMixer, добавляет AudioManager в Sandbox и MainMenu,
        /// навешивает UiButtonSound на кнопки. Возвращается в Sandbox.
        /// </summary>
        public static void SetupM7()
        {
            const string MainMenuScenePath = "Assets/_Project/Scenes/MainMenu.unity";

            // 1. Sandbox
            EditorSceneManager.OpenScene(SandboxScenePath, OpenSceneMode.Single);
            AudioTab.SetupAudio();

            // 2. MainMenu (если существует)
            if (System.IO.File.Exists(System.IO.Path.GetFullPath(MainMenuScenePath)))
            {
                EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
                AudioTab.SetupAudio();
            }

            // 3. Возвращаемся в Sandbox
            EditorSceneManager.OpenScene(SandboxScenePath, OpenSceneMode.Single);
        }

        /// <summary>
        /// Полная настройка M8: визуал.
        /// Открывает Sandbox → фиксит импорт моделей → применяет визуал к префабам →
        /// создаёт VFX-префабы → добавляет VfxManager → настраивает освещение и постобработку.
        /// </summary>
        public static void SetupM8()
        {
            EditorSceneManager.OpenScene(SandboxScenePath, OpenSceneMode.Single);

            VFXTab.FixModelImports();
            VFXTab.ApplyVisuals();
            VFXTab.BuildVfxPrefabs();
            VFXTab.SetupLightingAndPost();

            EditorSceneManager.SaveScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        /// <summary>
        /// Полная настройка M9: сценарий.
        /// Открывает Sandbox → добавляет EnemyCommander, GameWatcher,
        /// создаёт Barracks_Enemy и маркер PlayerBaseSpawn.
        /// </summary>
        public static void SetupM9()
        {
            EditorSceneManager.OpenScene(SandboxScenePath, OpenSceneMode.Single);
            ManagersTab.SetupScenario();
        }

        /// <summary>
        /// Фаза улучшения: дописывает строку метрик (билд, код, перф, баланс) в
        /// Docs-Vault/Improvements/Metrics.md. Перф/баланс берутся из JSON-результатов
        /// последнего прогона Balance-плейтестов (-testCategory Balance).
        /// </summary>
        public static void ExportImprovementMetrics()
        {
            ImproveTab.AppendMetricsRow();
        }

        /// <summary>Сборка Windows x64 билда (та же логика, что кнопка Build).</summary>
        public static void BuildWindows()
        {
            BuildTab.BuildWindows();
        }

        /// <summary>
        /// v3 Tank: открывает Sandbox → создаёт Tank-ассеты данных →
        /// создаёт Tank-префабы → размещает WarFactory в сцене → сохраняет.
        /// Prerequisite: M9 (SetupScenario) уже выполнен.
        /// </summary>
        public static void SetupTank()
        {
            EditorSceneManager.OpenScene(SandboxScenePath, OpenSceneMode.Single);
            ConfigTab.CreateOrUpdateTankDataAssets();
            PrefabsTab.CreateOrUpdateTankPrefabs();
            ManagersTab.SetupTank();
        }

        /// <summary>
        /// Сборка экрана статистики матча: открывает Sandbox → строит StatsPanel в Victory/Defeat
        /// → добавляет MatchStatsCollector на GameManagers → сохраняет сцену.
        /// Prerequisite: SetupM6Menus (BuildMenus) уже выполнен.
        /// </summary>
        public static void SetupMatchStats()
        {
            EditorSceneManager.OpenScene(SandboxScenePath, OpenSceneMode.Single);
            UITab.BuildMatchStatsPanel();
        }

        /// <summary>
        /// v5 Animated Units: настраивает импорт FBX, создаёт AnimatorController'ы,
        /// заменяет Visual в TestUnit/EnemyUnit на анимированные мехи Quaternius,
        /// добавляет Animator + UnitAnimator.
        /// Не требует открытой сцены — работает только с префабами и ассетами.
        /// </summary>
        public static void SetupAnimatedUnits()
        {
            AnimatedUnitsSetup.SetupAnimatedUnits();
        }

        /// <summary>
        /// Полный пакет улучшений circle-6:
        /// открывает Sandbox → мигрирует ProductionEntries (HeavyMarine.asset) →
        /// создаёт HeavyMarineUnit.prefab → строит CommandCardRoot + QueueSlotsRoot в SelectionPanel →
        /// проставляет _unitPrefabs у Barracks/WarFactory →
        /// добавляет BuildingSpawnEffect на Barracks/WarFactory/Extractor →
        /// устанавливает _waveStingerClip = fight.ogg на AudioManager сцены →
        /// сохраняет сцену и ассеты.
        /// </summary>
        public static void SetupCircle6()
        {
            EditorSceneManager.OpenScene(SandboxScenePath, OpenSceneMode.Single);
            ConfigTab.MigrateProductionEntriesV6();
            UITab.SetupCommandCardV6();
        }

        /// <summary>
        /// Дерево технологий (v7): открывает Sandbox → создаёт/обновляет Tech-ассеты →
        /// строит TechCardRoot (3 tech-кнопки) в SelectionPanel →
        /// прописывает techCardSlots → сохраняет сцену и ассеты.
        /// Prerequisite: SetupCircle6() / SetupCommandCardV6 уже выполнен.
        /// Batch entry-point: -executeMethod DiplomaGame.Editor.ForgeBatch.SetupTechTree
        /// </summary>
        public static void SetupTechTree()
        {
            EditorSceneManager.OpenScene(SandboxScenePath, OpenSceneMode.Single);
            UITab.SetupTechTreeV7();
        }

        /// <summary>Добавляет ScreenshotDirector на GameManagers в Sandbox (для авто-скриншотов README).</summary>
        public static void AddScreenshotDirector()
        {
            EditorSceneManager.OpenScene(SandboxScenePath, OpenSceneMode.Single);
            var managers = UnityEngine.GameObject.Find("GameManagers");
            if (managers == null)
            {
                UnityEngine.Debug.LogWarning("[Forge] GameManagers не найден.");
                return;
            }

            var director = managers.GetComponent<DiplomaGame.Runtime.Core.ScreenshotDirector>();
            if (director == null)
                director = managers.AddComponent<DiplomaGame.Runtime.Core.ScreenshotDirector>();

            var so = new UnityEditor.SerializedObject(director);
            var prop = so.FindProperty("modeController");
            if (prop != null)
                prop.objectReferenceValue = managers.GetComponent<DiplomaGame.Runtime.Core.GameModeController>();
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            UnityEngine.Debug.Log("[Forge] ScreenshotDirector добавлен.");
        }
    }
}
