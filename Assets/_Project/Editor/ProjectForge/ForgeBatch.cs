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
        /// Сборка WebGL (v8) в Builds/WebGL/.
        /// PlayerSettings (Gzip, DecompressionFallback, template Default, runInBackground)
        /// выставляются идемпотентно перед сборкой — безопасно запускать повторно.
        /// Batch entry-point: -executeMethod DiplomaGame.Editor.ForgeBatch.BuildWebGL
        /// </summary>
        public static void BuildWebGL()
        {
            BuildTab.BuildWebGL();
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

        /// <summary>
        /// v9 RebuildMapLayout: открывает Sandbox → перемещает маркеры баз (±35),
        /// здания, расставляет чок-скалы (x=±8), 2 экспанд-ноды и 15 декор-объектов →
        /// перезапекает NavMesh → сохраняет сцену.
        /// Batch entry-point: -executeMethod DiplomaGame.Editor.ForgeBatch.RebuildMapLayout
        /// </summary>
        public static void RebuildMapLayout()
        {
            EditorSceneManager.OpenScene(SandboxScenePath, OpenSceneMode.Single);
            ScenesTab.RebuildMapLayout();
        }

        /// <summary>
        /// Полная настройка локализации (v10):
        /// Sandbox → MigrateLocalizationEnFields + CreateOrUpdateLocTable + ApplyLocalizationToSceneUI →
        /// MainMenu → ApplyLocalizationToSceneUI → возврат в Sandbox.
        /// Batch entry-point: -executeMethod DiplomaGame.Editor.ForgeBatch.SetupLocalization
        /// </summary>
        public static void SetupLocalization()
        {
            const string MainMenuScenePath = "Assets/_Project/Scenes/MainMenu.unity";

            // 1. Sandbox: мигрируем EN поля + создаём LocTable + применяем к UI
            EditorSceneManager.OpenScene(SandboxScenePath, OpenSceneMode.Single);
            ConfigTab.MigrateLocalizationEnFieldsV10();
            ConfigTab.CreateOrUpdateLocTableV10();
            UITab.ApplyLocalizationToSceneUI_V10();

            // 2. MainMenu (если существует): применяем к UI
            if (System.IO.File.Exists(System.IO.Path.GetFullPath(MainMenuScenePath)))
            {
                EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
                UITab.ApplyLocalizationToSceneUI_V10();
            }

            // 3. Возвращаемся в Sandbox
            EditorSceneManager.OpenScene(SandboxScenePath, OpenSceneMode.Single);
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
        /// <summary>
        /// v11: фикс вражеского производства — собственные BuildingData и префабы.
        /// Batch: -executeMethod DiplomaGame.Editor.ForgeBatch.FixEnemyProductionV11
        /// </summary>
        public static void FixEnemyProductionV11()
        {
            EditorSceneManager.OpenScene(SandboxScenePath, OpenSceneMode.Single);
            ConfigTab.CreateOrUpdateEnemyBuildingDataV11();
            ManagersTab.RewireEnemyProductionV11();
        }

        /// <summary>
        /// Game Feel (circle-12): открывает Sandbox →
        /// создаёт GameFeelSettings.asset → строит ShockwaveRing.prefab →
        /// добавляет HitFlashHandler ко всем юнит-префабам →
        /// настраивает GameFeelManager → подключает ShockwaveRing к VfxManager →
        /// добавляет DashTrail на Hero/Visual → сохраняет сцену и ассеты.
        /// Batch entry-point: -executeMethod DiplomaGame.Editor.ForgeBatch.SetupGameFeel
        /// </summary>
        public static void SetupGameFeel()
        {
            EditorSceneManager.OpenScene(SandboxScenePath, OpenSceneMode.Single);
            GameFeelTab.SetupAll();
        }

        /// <summary>
        /// v13 Difficulty Levels: создаёт DifficultyEasy/Normal/Hard.asset →
        /// прошивает EnemyCommander._profiles в Sandbox →
        /// открывает MainMenu-сцену, строит DifficultyRow над кнопкой Играть →
        /// прошивает MainMenuController.difficultyDropdown →
        /// обновляет LocTable (4 ключа menu.difficulty_*) →
        /// возвращается в Sandbox.
        /// Batch entry-point: -executeMethod DiplomaGame.Editor.ForgeBatch.SetupDifficulty
        /// </summary>
        public static void SetupDifficulty()
        {
            ManagersTab.SetupDifficulty();
        }

        /// <summary>
        /// Circle-14 Flanking AI: обновляет три DifficultyProfileSO-ассета новыми полями
        /// тактики (EmergencyWaveDelay, ProductionPauseDuration, FlankProbability) →
        /// прошивает _flankWaypoints на EnemyCommander в Sandbox (две фланковые точки
        /// ~(±38, 0, 0), снапнутые к NavMesh) → сохраняет сцену и ассеты.
        /// Идемпотентно.
        /// Batch entry-point: -executeMethod DiplomaGame.Editor.ForgeBatch.SetupFlankingAI
        /// </summary>
        public static void SetupFlankingAI()
        {
            EditorSceneManager.OpenScene(SandboxScenePath, OpenSceneMode.Single);
            ManagersTab.SetupFlankingAI();
        }

        /// <summary>
        /// Circle-16 Under Attack Alert: открывает Sandbox →
        /// создаёт UnderAttackVignette (full-stretch Image) в GameHUD →
        /// создаёт ThreatMarker (16×16 Image) в MinimapDisplay →
        /// добавляет UnderAttackAlert на GameManagers →
        /// прошивает _minimapMarker, _edgeVignette, _minimapCamera, _minimapDisplay →
        /// сохраняет сцену и ассеты.
        /// Prerequisite: SetupM6Hud (BuildGameHud) уже выполнен.
        /// Идемпотентно.
        /// Batch entry-point: -executeMethod DiplomaGame.Editor.ForgeBatch.SetupUnderAttackAlert
        /// </summary>
        public static void SetupUnderAttackAlert()
        {
            EditorSceneManager.OpenScene(SandboxScenePath, OpenSceneMode.Single);
            UITab.BuildUnderAttackAlert();
        }

        /// <summary>
        /// Circle-17 Idle Army Indicator: открывает Sandbox →
        /// создаёт IdleArmyBadge (Button, 90×32, жёлтый фон) в левом нижнем углу RTS_Block →
        /// добавляет IdleArmyIndicator на IdleArmyBadge →
        /// прошивает _selectionSystem, _countLabel, _pulse →
        /// сохраняет сцену и ассеты.
        /// Prerequisite: SetupM6Hud (BuildGameHud) уже выполнен.
        /// Идемпотентно.
        /// Batch entry-point: -executeMethod DiplomaGame.Editor.ForgeBatch.SetupIdleArmyIndicator
        /// </summary>
        public static void SetupIdleArmyIndicator()
        {
            EditorSceneManager.OpenScene(SandboxScenePath, OpenSceneMode.Single);
            UITab.SetupIdleArmyIndicator();
        }

        /// <summary>
        /// Circle-18 Unit Health Bars: открывает Sandbox →
        /// создаёт HealthBarsCanvas (Screen Space Overlay, sortingOrder=20) →
        /// добавляет UnitHealthBarSystem на GameManagers →
        /// прошивает _modeController, _selectionSystem, _barCanvas →
        /// сохраняет сцену и ассеты.
        /// Prerequisite: SetupM6Hud (BuildGameHud) уже выполнен.
        /// Идемпотентно.
        /// Batch entry-point: -executeMethod DiplomaGame.Editor.ForgeBatch.SetupUnitHealthBars
        /// </summary>
        public static void SetupUnitHealthBars()
        {
            EditorSceneManager.OpenScene(SandboxScenePath, OpenSceneMode.Single);
            UITab.SetupUnitHealthBars();
        }

        /// <summary>
        /// Circle-20 Crosshair Hitmarker: открывает Sandbox →
        /// создаёт 4 Image-полоски на Crosshair (если их нет) →
        /// прошивает CrosshairUI._shooter + CrosshairUI._settings →
        /// записывает дефолты hitmarker (warm orange, expand=1.15, miss=1.05, dur=0.10)
        /// в GameFeelSettings.asset через SerializedObject →
        /// добавляет UiPulse на AbilitySlot_1..4 →
        /// сохраняет сцену и ассеты.
        /// Prerequisite: SetupM6Hud и SetupGameFeel уже выполнены.
        /// Идемпотентно.
        /// Batch entry-point: -executeMethod DiplomaGame.Editor.ForgeBatch.SetupCrosshairHitmarker
        /// </summary>
        public static void SetupCrosshairHitmarker()
        {
            EditorSceneManager.OpenScene(SandboxScenePath, OpenSceneMode.Single);
            UITab.SetupCrosshairHitmarker();
        }

        /// <summary>
        /// Идемпотентно создаёт Hero Damage Indicator (Circle-21 + Circle-23) в Sandbox.unity:
        /// [C21] добавляет HeroDamageFlash Image (full-stretch, red alpha=0) в TPS_Block →
        /// [C23] добавляет HeroDamageArrow Image (64×64, центр+220px, красный, скрыт) в TPS_Block →
        /// навешивает HeroDamageIndicator на HeroDamageFlash →
        /// прошивает _heroHealth, _tpsCameraTransform, _edgeFlash, _directionArrow, _settings →
        /// записывает дефолты C21+C23 (duration=1.0, peakAlpha=0.6, arrowPeakAlpha=0.8) в GameFeelSettings.asset →
        /// сохраняет сцену и ассеты.
        /// [C21] Health.AnyDamaged → full-edge red flash (fallback для урона без источника).
        /// [C23] Health.AnyDamagedFrom → вращает _directionArrow к атакующему (camera-forward как oporna).
        /// Prerequisite: BuildGameHUD (M6a) и SetupGameFeel (C12) уже выполнены.
        /// Идемпотентно.
        /// Batch entry-point: -executeMethod DiplomaGame.Editor.ForgeBatch.SetupHeroDamageIndicator
        /// </summary>
        public static void SetupHeroDamageIndicator()
        {
            EditorSceneManager.OpenScene(SandboxScenePath, OpenSceneMode.Single);
            UITab.SetupHeroDamageIndicator();
        }

        /// <summary>
        /// Dynamic FOV TPS-камеры (Circle-22 / Circle-24 update): открывает Sandbox →
        /// добавляет/обновляет DynamicFovController на GameManagers →
        /// прошивает _tpsCamera, _abilitySystem, _modeController, _settings, _heroController →
        /// записывает дефолты C22+C24 (fovKickAmount=9, fovKickDuration=0.08, fovReturnSpeed=12, fovSprintWiden=4)
        /// в GameFeelSettings.asset через SerializedObject + ForceReserializeAssets →
        /// сохраняет сцену и ассеты.
        /// Kick-триггеры: AbilityType.Dash и AbilityType.Overcharge.
        /// Sprint-widen (C24): +4° пока HeroController.IsSprinting == true.
        /// Prerequisite: SetupGameFeel (C12) уже выполнен.
        /// Идемпотентно.
        /// Batch entry-point: -executeMethod DiplomaGame.Editor.ForgeBatch.SetupDynamicFov
        /// </summary>
        public static void SetupDynamicFov()
        {
            EditorSceneManager.OpenScene(SandboxScenePath, OpenSceneMode.Single);
            GameFeelTab.SetupDynamicFov();
        }

        /// <summary>
        /// Stamina Bar (Circle-24): открывает Sandbox →
        /// создаёт HeroStaminaBar в GameHUD/TPS_Block →
        /// прошивает _fill (Image) и _heroController (HeroController) →
        /// по умолчанию скрыт (activeSelf=false; появляется при спринте/убыли стамины).
        /// Prerequisite: BuildGameHUD (M6a) уже выполнен.
        /// Идемпотентно.
        /// Batch entry-point: -executeMethod DiplomaGame.Editor.ForgeBatch.SetupStaminaBar
        /// </summary>
        public static void SetupStaminaBar()
        {
            EditorSceneManager.OpenScene(SandboxScenePath, OpenSceneMode.Single);
            GameFeelTab.SetupStaminaBar();
        }

        /// <summary>
        /// Circle-24 полный пакет: открывает Sandbox →
        /// SetupDynamicFov (обновляет C22 + добавляет sprint-widen) →
        /// SetupStaminaBar (создаёт/обновляет полосу стамины).
        /// Batch entry-point: -executeMethod DiplomaGame.Editor.ForgeBatch.SetupCircle24
        /// </summary>
        public static void SetupCircle24()
        {
            EditorSceneManager.OpenScene(SandboxScenePath, OpenSceneMode.Single);
            GameFeelTab.SetupDynamicFov();
            GameFeelTab.SetupStaminaBar();
        }

    }
}
