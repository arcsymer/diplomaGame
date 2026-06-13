using DiplomaGame.Runtime.AI;
using DiplomaGame.Runtime.Buildings;
using DiplomaGame.Runtime.CameraControl;
using DiplomaGame.Runtime.Combat;
using DiplomaGame.Runtime.Commands;
using DiplomaGame.Runtime.Core;
using DiplomaGame.Runtime.Data;
using DiplomaGame.Runtime.Economy;
using DiplomaGame.Runtime.Hero;
using DiplomaGame.Runtime.Selection;
using DiplomaGame.Runtime.UI;
using DiplomaGame.Runtime.Units;
using TMPro;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

namespace DiplomaGame.Editor
{
    /// <summary>
    /// Вкладка Managers — настройка игровых менеджеров сцены.
    /// M1: кнопка "Setup Mode Rig" идемпотентно собирает камерный риг
    /// и GameModeController в открытой сцене.
    /// M2: кнопка "Setup RTS Control" добавляет SelectionSystem, CommandInput,
    /// RtsCameraController и спавнит 5 TestUnit.
    /// M3: кнопка "Setup Hero (M3)" настраивает HeroController, HeroShooter, AbilitySystem.
    /// M5: кнопка "Setup Economy (M5)" настраивает ResourceBank, BuildingPlacer, здания и ноды.
    /// </summary>
    internal sealed class ManagersTab : IForgeTab
    {
        private const string InputActionsPath    = "Assets/_Project/Settings/GameControls.inputactions";
        private const string TestUnitPrefabPath  = "Assets/_Project/Prefabs/Units/TestUnit.prefab";
        private const string EnemyUnitPrefabPath = "Assets/_Project/Prefabs/Units/EnemyUnit.prefab";
        private const string AbilitiesFolder     = "Assets/_Project/Data/Abilities";

        // M5 пути
        private const string HQPrefabPath          = "Assets/_Project/Prefabs/Buildings/HQ.prefab";
        private const string BarracksPrefabPath     = "Assets/_Project/Prefabs/Buildings/Barracks.prefab";
        private const string ExtractorPrefabPath    = "Assets/_Project/Prefabs/Buildings/Extractor.prefab";
        private const string ResourceNodePrefabPath = "Assets/_Project/Prefabs/Props/ResourceNode.prefab";
        private const string HQDataPath             = "Assets/_Project/Data/Buildings/HQ.asset";
        private const string BarracksDataPath       = "Assets/_Project/Data/Buildings/Barracks.asset";
        private const string ExtractorDataPath      = "Assets/_Project/Data/Buildings/Extractor.asset";
        private const string GhostValidMatPath      = "Assets/_Project/Art/Materials/GhostValid.mat";
        private const string GhostInvalidMatPath    = "Assets/_Project/Art/Materials/GhostInvalid.mat";

        // M9 пути
        private const string GameOverCanvasName = "GameOver";

        // v3 Tank пути
        private const string WarFactoryPrefabPath    = "Assets/_Project/Prefabs/Buildings/WarFactory.prefab";
        private const string WarFactoryDataPath      = "Assets/_Project/Data/Buildings/WarFactory.asset";
        private const string EnemyTankUnitPrefabPath = "Assets/_Project/Prefabs/Units/EnemyTankUnit.prefab";

        public string Title => "Managers";

        public void OnGUI()
        {
            GUILayout.Space(8);
            GUILayout.Label("Игровые менеджеры", EditorStyles.boldLabel);
            GUILayout.Space(4);

            EditorGUILayout.HelpBox(
                "Настраивает камерный риг M1 в открытой сцене (Sandbox).\n" +
                "Операция идемпотентна — повторный запуск ничего не дублирует.",
                MessageType.Info);

            GUILayout.Space(4);

            if (GUILayout.Button("Setup Mode Rig (M1)", GUILayout.Height(32)))
                SetupModeRig();

            GUILayout.Space(8);

            EditorGUILayout.HelpBox(
                "Добавляет SelectionSystem, CommandInput и RtsCameraController " +
                "на GameManagers, проставляет ссылки и спавнит 5 тестовых юнитов.\n" +
                "Операция идемпотентна.",
                MessageType.Info);

            GUILayout.Space(4);

            if (GUILayout.Button("Setup RTS Control (M2)", GUILayout.Height(32)))
                SetupRtsControl();

            GUILayout.Space(8);

            EditorGUILayout.HelpBox(
                "Настраивает TPS-героя: CharacterController, NavMeshAgent, Unit, HeroController, " +
                "HeroShooter, AbilitySystem + SO-ассеты способностей.\n" +
                "Операция идемпотентна.",
                MessageType.Info);

            GUILayout.Space(4);

            if (GUILayout.Button("Setup Hero (M3)", GUILayout.Height(32)))
                SetupHero();

            GUILayout.Space(8);

            EditorGUILayout.HelpBox(
                "M4: добавляет Health на Hero (maxHp 150), спавнит 3 EnemyUnit у EnemyBaseSpawn, " +
                "переспавнивает TestUnit из обновлённого префаба (5 шт.).\n" +
                "Операция идемпотентна.",
                MessageType.Info);

            GUILayout.Space(4);

            if (GUILayout.Button("Setup Combat (M4)", GUILayout.Height(32)))
                SetupCombat();

            GUILayout.Space(8);

            EditorGUILayout.HelpBox(
                "M5: добавляет ResourceBank и BuildingPlacer на GameManagers; " +
                "расставляет 4 ResourceNode (по 2 у каждой базы); " +
                "создаёт HQ и Barracks игрока у PlayerBaseSpawn, HQ врага у EnemyBaseSpawn.\n" +
                "Операция идемпотентна.",
                MessageType.Info);

            GUILayout.Space(4);

            if (GUILayout.Button("Setup Economy (M5)", GUILayout.Height(32)))
                SetupEconomy();

            GUILayout.Space(8);

            EditorGUILayout.HelpBox(
                "M9: добавляет EnemyCommander + GameWatcher на GameManagers; " +
                "создаёт Barracks_Enemy у EnemyBaseSpawn (фракция Enemy); " +
                "создаёт маркер PlayerBaseSpawn при отсутствии. " +
                "Операция идемпотентна.",
                MessageType.Info);

            GUILayout.Space(4);

            if (GUILayout.Button("Setup Scenario (M9)", GUILayout.Height(32)))
                SetupScenario();

            GUILayout.Space(8);

            EditorGUILayout.HelpBox(
                "v3 Tank: размещает WarFactory_Player у базы игрока и WarFactory_Enemy у базы врага. " +
                "Проставляет EnemyCommander._enemyWarFactory.\n" +
                "Требует выполненного Setup Scenario (M9).",
                MessageType.Info);

            GUILayout.Space(4);

            if (GUILayout.Button("Setup Tank (v3)", GUILayout.Height(32)))
                SetupTank();

            GUILayout.Space(8);

            EditorGUILayout.HelpBox(
                "Setup Difficulty (v13):\n" +
                "• Создаёт DifficultyEasy/Normal/Hard.asset в Data/Difficulty/\n" +
                "• Прошивает EnemyCommander._profiles в Sandbox\n" +
                "• Открывает MainMenu-сцену, строит DifficultyRow (Label + TMP_Dropdown) над кнопкой Играть\n" +
                "• Прошивает MainMenuController.difficultyDropdown\n" +
                "• Обновляет LocTable (4 ключа menu.difficulty_*)\n" +
                "Операция идемпотентна.",
                MessageType.Info);

            GUILayout.Space(4);

            if (GUILayout.Button("Setup Difficulty (v13)", GUILayout.Height(32)))
                SetupDifficulty();
        }

        // ----------------------------------------------------------------
        // Основная операция
        // ----------------------------------------------------------------

        internal static void SetupModeRig()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                EditorUtility.DisplayDialog("Project Forge", "Нет открытой сцены.", "OK");
                return;
            }

            // --- Hero ---
            // Ищем существующий. Если нет — создаём примитив-капсулу (MeshFilter+Renderer+Collider).
            // Если есть, но нет CapsuleCollider — считаем его капсулой и просто добавляем коллайдер.
            var heroExisting = GameObject.Find("Hero");
            GameObject hero;
            if (heroExisting == null)
            {
                hero = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                hero.name = "Hero";
            }
            else
            {
                hero = heroExisting;
                if (!hero.GetComponent<CapsuleCollider>())
                    hero.AddComponent<CapsuleCollider>();
            }
            hero.transform.position = new Vector3(0f, 1f, 0f);

            // --- RtsCameraTarget ---
            // Ставим на позицию PlayerBaseSpawn, чтобы RTS-камера стартовала у базы игрока.
            // Fallback (-30, 0, -30) — если маркер ещё не создан (M9 запускается позже).
            var rtsTarget = EnsureGameObject("RtsCameraTarget");
            {
                var playerBaseSpawn = GameObject.Find("PlayerBaseSpawn");
                rtsTarget.transform.position = playerBaseSpawn != null
                    ? playerBaseSpawn.transform.position
                    : new Vector3(-30f, 0f, -30f);
            }

            // --- Main Camera: CinemachineBrain ---
            var mainCam = Camera.main;
            if (mainCam == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                mainCam = camGo.AddComponent<Camera>();
            }
            EnsureComponent<CinemachineBrain>(mainCam.gameObject);

            // --- RTS Camera ---
            var rtsCamGo = EnsureGameObject("RTS Camera");
            var rtsCam   = EnsureComponent<CinemachineCamera>(rtsCamGo);
            rtsCam.Priority = 20;
            rtsCamGo.transform.position = new Vector3(0f, 25f, -18f);
            rtsCamGo.transform.rotation = Quaternion.Euler(55f, 0f, 0f);
            rtsCam.Follow = rtsTarget.transform;

            var rtsFollow = EnsureComponent<CinemachineFollow>(rtsCamGo);
            rtsFollow.FollowOffset = new Vector3(0f, 25f, -18f);

            // Для RTS-камеры composer вращения не нужен — фиксированный наклон.
            // Удаляем CinemachineRotationComposer, если он вдруг есть.
            var rtsComposer = rtsCamGo.GetComponent<CinemachineRotationComposer>();
            if (rtsComposer != null)
                Object.DestroyImmediate(rtsComposer);

            // --- TPS Camera ---
            var tpsCamGo = EnsureGameObject("TPS Camera");
            var tpsCam   = EnsureComponent<CinemachineCamera>(tpsCamGo);
            tpsCam.Priority = 10;
            // В Cinemachine 3.x цели Follow/LookAt задаются на самой CinemachineCamera;
            // RotationComposer берёт LookAt-цель оттуда.
            tpsCam.Follow = hero.transform;
            tpsCam.LookAt = hero.transform;

            var tpsFollow = EnsureComponent<CinemachineFollow>(tpsCamGo);
            tpsFollow.FollowOffset = new Vector3(0.7f, 2.2f, -5f);

            EnsureComponent<CinemachineRotationComposer>(tpsCamGo);

            // --- GameManagers ---
            var managersGo  = EnsureGameObject("GameManagers");
            var controller   = EnsureComponent<GameModeController>(managersGo);

            // Проставляем приватные поля через SerializedObject
            var so = new SerializedObject(controller);

            var propRts     = so.FindProperty("rtsCamera");
            var propTps     = so.FindProperty("tpsCamera");
            var propActions = so.FindProperty("actions");

            propRts.objectReferenceValue     = rtsCam;
            propTps.objectReferenceValue     = tpsCam;

            var inputAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(InputActionsPath);
            if (inputAsset == null)
                Debug.LogWarning($"[Project Forge] InputActionAsset не найден: {InputActionsPath}. " +
                                 "Создайте ассет через Setup или проверьте путь.");
            propActions.objectReferenceValue = inputAsset;

            so.ApplyModifiedPropertiesWithoutUndo();

            // --- Сохранение сцены ---
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[Project Forge] Setup Mode Rig (M1) выполнен.");
        }

        // ----------------------------------------------------------------
        // M2: Setup RTS Control
        // ----------------------------------------------------------------

        internal static void SetupRtsControl()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                EditorUtility.DisplayDialog("Project Forge", "Нет открытой сцены.", "OK");
                return;
            }

            var inputAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(InputActionsPath);
            if (inputAsset == null)
                Debug.LogWarning($"[Project Forge] InputActionAsset не найден: {InputActionsPath}.");

            // --- GameManagers ---
            var managersGo = EnsureGameObject("GameManagers");

            // Берём GameModeController (должен уже быть после M1)
            var modeController = EnsureComponent<GameModeController>(managersGo);

            // --- SelectionSystem ---
            var selectionSystem = EnsureComponent<SelectionSystem>(managersGo);
            {
                var so = new SerializedObject(selectionSystem);
                so.FindProperty("actions").objectReferenceValue       = inputAsset;
                so.FindProperty("modeController").objectReferenceValue = modeController;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // --- CommandInput ---
            var commandInput = EnsureComponent<CommandInput>(managersGo);
            {
                var so = new SerializedObject(commandInput);
                so.FindProperty("selectionSystem").objectReferenceValue = selectionSystem;
                so.FindProperty("actions").objectReferenceValue          = inputAsset;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // --- RtsCameraTarget ---
            var rtsTarget = EnsureGameObject("RtsCameraTarget");

            // --- RTS Camera CinemachineFollow ---
            var rtsCamGo = EnsureGameObject("RTS Camera");
            var rtsFollow = EnsureComponent<CinemachineFollow>(rtsCamGo);

            // --- RtsCameraController ---
            var rtsCamCtrl = EnsureComponent<RtsCameraController>(managersGo);
            {
                var so = new SerializedObject(rtsCamCtrl);
                so.FindProperty("target").objectReferenceValue         = rtsTarget.transform;
                so.FindProperty("rtsFollow").objectReferenceValue      = rtsFollow;
                so.FindProperty("modeController").objectReferenceValue = modeController;
                so.FindProperty("actions").objectReferenceValue        = inputAsset;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // --- Спавн 5 TestUnit (идемпотентно — считаем существующие) ---
            SpawnTestUnits(scene);

            // --- Сохранение сцены ---
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[Project Forge] Setup RTS Control (M2) выполнен.");
        }

        // ----------------------------------------------------------------
        // M3: Setup Hero
        // ----------------------------------------------------------------

        internal static void SetupHero()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                EditorUtility.DisplayDialog("Project Forge", "Нет открытой сцены.", "OK");
                return;
            }

            var inputAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.InputSystem.InputActionAsset>(InputActionsPath);
            if (inputAsset == null)
                Debug.LogWarning($"[Project Forge] InputActionAsset не найден: {InputActionsPath}.");

            // --- Hero GO ---
            var heroExisting = GameObject.Find("Hero");
            GameObject hero;
            if (heroExisting == null)
            {
                hero = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                hero.name = "Hero";
            }
            else
            {
                hero = heroExisting;
            }

            // --- CharacterController не совместим с CapsuleCollider на том же GO.
            //     SetupModeRig (M1) добавляет CapsuleCollider к капсуле-примитиву — удаляем его. ---
            var capsuleCollider = hero.GetComponent<CapsuleCollider>();
            if (capsuleCollider != null)
                Object.DestroyImmediate(capsuleCollider);

            // --- CharacterController (height 2, radius 0.5, center y=0) ---
            var cc = EnsureComponent<CharacterController>(hero);
            cc.height = 2f;
            cc.radius = 0.5f;
            cc.center = new Vector3(0f, 0f, 0f);

            // --- NavMeshAgent (enabled, speed 6) ---
            var agent = EnsureComponent<NavMeshAgent>(hero);
            agent.speed   = 6f;
            agent.enabled = true;

            // --- Unit (faction Player) ---
            var unit   = EnsureComponent<Unit>(hero);
            var unitSo = new SerializedObject(unit);
            unitSo.FindProperty("_faction").enumValueIndex = (int)Faction.Player;
            unitSo.ApplyModifiedPropertiesWithoutUndo();

            // --- HeroController ---
            var heroCtrl = EnsureComponent<HeroController>(hero);

            // --- HeroShooter ---
            var heroShooter = EnsureComponent<HeroShooter>(hero);

            // --- AbilitySystem ---
            var abilitySystem = EnsureComponent<AbilitySystem>(hero);

            // --- SelectionRing (дочерний цилиндр, как у TestUnit) ---
            var existingRing = hero.transform.Find("SelectionRing");
            GameObject ring;
            if (existingRing != null)
            {
                ring = existingRing.gameObject;
            }
            else
            {
                ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ring.name = "SelectionRing";
                ring.transform.SetParent(hero.transform, false);
            }
            ring.transform.localScale    = new Vector3(1.4f, 0.02f, 1.4f);
            ring.transform.localPosition = new Vector3(0f, -0.95f, 0f);
            var ringCollider = ring.GetComponent<Collider>();
            if (ringCollider != null)
                Object.DestroyImmediate(ringCollider);
            ring.SetActive(false);

            // --- Находим GameModeController ---
            var managersGo     = EnsureGameObject("GameManagers");
            var modeController = managersGo.GetComponent<GameModeController>();
            if (modeController == null)
            {
                Debug.LogWarning("[Project Forge] GameModeController не найден — сначала запустите Setup Mode Rig (M1).");
                modeController = EnsureComponent<GameModeController>(managersGo);
            }

            // --- SO-ассеты способностей (идемпотентно; баланс — ADR-013) ---
            EnsureFolder(AbilitiesFolder);
            var dashAsset = EnsureAbilityAsset(
                "Dash", "Рывок", AbilityType.Dash, cooldown: 4f, dashDistance: 6f);

            var ab2Asset = EnsureAbilityAsset(
                "Ability2", "Ударная волна", AbilityType.Shockwave,
                cooldown: 10f, effectRadius: 6f, effectAmount: 40f);

            var ab3Asset = EnsureAbilityAsset(
                "Ability3", "Ремонтное поле", AbilityType.RepairField,
                cooldown: 12f, effectRadius: 8f, effectAmount: 30f);

            var ab4Asset = EnsureAbilityAsset(
                "Ability4", "Перегрузка", AbilityType.Overcharge,
                cooldown: 15f, buffDuration: 5f, fireRateMultiplier: 2f, damageMultiplier: 1.5f);

            // --- Проставляем ссылки через SerializedObject ---

            // HeroController
            {
                var so = new SerializedObject(heroCtrl);
                so.FindProperty("modeController").objectReferenceValue = modeController;
                so.FindProperty("actions").objectReferenceValue        = inputAsset;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // HeroShooter
            {
                var so = new SerializedObject(heroShooter);
                so.FindProperty("modeController").objectReferenceValue = modeController;
                so.FindProperty("actions").objectReferenceValue        = inputAsset;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // AbilitySystem — abilities + ссылки
            {
                var so          = new SerializedObject(abilitySystem);
                var abilitiesProp = so.FindProperty("abilities");
                abilitiesProp.arraySize = 4;
                abilitiesProp.GetArrayElementAtIndex(0).objectReferenceValue = dashAsset;
                abilitiesProp.GetArrayElementAtIndex(1).objectReferenceValue = ab2Asset;
                abilitiesProp.GetArrayElementAtIndex(2).objectReferenceValue = ab3Asset;
                abilitiesProp.GetArrayElementAtIndex(3).objectReferenceValue = ab4Asset;
                so.FindProperty("modeController").objectReferenceValue       = modeController;
                so.FindProperty("actions").objectReferenceValue              = inputAsset;
                so.FindProperty("heroController").objectReferenceValue       = heroCtrl;
                so.FindProperty("heroShooter").objectReferenceValue          = heroShooter;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // --- Сохранение сцены ---
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[Project Forge] Setup Hero (M3) выполнен.");
        }

        // ----------------------------------------------------------------
        // M4: Setup Combat
        // ----------------------------------------------------------------

        internal static void SetupCombat()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                EditorUtility.DisplayDialog("Project Forge", "Нет открытой сцены.", "OK");
                return;
            }

            // --- Hero: добавить Health с maxHp 150 ---
            var heroGo = GameObject.Find("Hero");
            if (heroGo != null)
            {
                var health = EnsureComponent<Health>(heroGo);
                var so     = new SerializedObject(health);
                so.FindProperty("_maxHp").floatValue = 150f;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                Debug.LogWarning("[Project Forge] Hero не найден в сцене — сначала запустите Setup Hero (M3).");
            }

            // --- Переспавнить TestUnit (удалить старые, инстанцировать из префаба) ---
            RespawnTestUnits(scene);

            // --- Заспавнить 3 EnemyUnit у EnemyBaseSpawn (идемпотентно) ---
            SpawnEnemyUnits(scene);

            // --- Сохранение сцены ---
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[Project Forge] Setup Combat (M4) выполнен.");
        }

        // ----------------------------------------------------------------
        // M5: Setup Economy
        // ----------------------------------------------------------------

        internal static void SetupEconomy()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                EditorUtility.DisplayDialog("Project Forge", "Нет открытой сцены.", "OK");
                return;
            }

            var managersGo = EnsureGameObject("GameManagers");

            // --- ResourceBank ---
            var bank = EnsureComponent<ResourceBank>(managersGo);

            // --- BuildingPlacer ---
            var placer = EnsureComponent<BuildingPlacer>(managersGo);

            // Загружаем ассеты
            var hqData         = AssetDatabase.LoadAssetAtPath<BuildingData>(HQDataPath);
            var barracksData   = AssetDatabase.LoadAssetAtPath<BuildingData>(BarracksDataPath);
            var extractorData  = AssetDatabase.LoadAssetAtPath<BuildingData>(ExtractorDataPath);
            var hqPrefab       = AssetDatabase.LoadAssetAtPath<GameObject>(HQPrefabPath);
            var barracksPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BarracksPrefabPath);
            var extractorPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ExtractorPrefabPath);

            if (hqData == null || barracksData == null || extractorData == null ||
                hqPrefab == null || barracksPrefab == null || extractorPrefab == null)
            {
                Debug.LogWarning("[Project Forge] Не все Building-ассеты/префабы найдены. " +
                                 "Сначала запустите 'Create/Update Building Data (M5)' и 'Create/Update Building Prefabs (M5)'.");
            }

            // Создаём / обновляем ghost-материалы
            var ghostValid   = EnsureGhostMaterial(GhostValidMatPath,   new Color(0.2f, 0.9f, 0.2f, 0.5f));
            var ghostInvalid = EnsureGhostMaterial(GhostInvalidMatPath, new Color(0.9f, 0.2f, 0.2f, 0.5f));

            // Проставляем ссылки в BuildingPlacer
            var modeController = managersGo.GetComponent<GameModeController>();
            {
                var so = new SerializedObject(placer);
                so.FindProperty("_bank").objectReferenceValue            = bank;
                so.FindProperty("_modeController").objectReferenceValue  = modeController;
                so.FindProperty("_barracksData").objectReferenceValue    = barracksData;
                so.FindProperty("_extractorData").objectReferenceValue   = extractorData;
                so.FindProperty("_barracksPrefab").objectReferenceValue  = barracksPrefab;
                so.FindProperty("_extractorPrefab").objectReferenceValue = extractorPrefab;
                so.FindProperty("_ghostValid").objectReferenceValue      = ghostValid;
                so.FindProperty("_ghostInvalid").objectReferenceValue    = ghostInvalid;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // --- 4 ResourceNode (идемпотентно по имени) ---
            Vector3 playerBase = GetBasePosition("PlayerBaseSpawn");
            Vector3 enemyBase  = GetBasePosition("EnemyBaseSpawn");

            var nodePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ResourceNodePrefabPath);
            EnsureResourceNode(scene, "ResourceNode_Player_1", playerBase + new Vector3(-8f, 0f, -5f), nodePrefab);
            EnsureResourceNode(scene, "ResourceNode_Player_2", playerBase + new Vector3( 8f, 0f, -5f), nodePrefab);
            EnsureResourceNode(scene, "ResourceNode_Enemy_1",  enemyBase  + new Vector3(-8f, 0f,  5f), nodePrefab);
            EnsureResourceNode(scene, "ResourceNode_Enemy_2",  enemyBase  + new Vector3( 8f, 0f,  5f), nodePrefab);

            // --- HQ игрока у PlayerBaseSpawn (идемпотентно) ---
            EnsureBuilding(scene, "HQ_Player",    hqPrefab,       playerBase + new Vector3(0f, 0f, 0f),  Faction.Player, bank);
            EnsureBuilding(scene, "HQ_Enemy",     hqPrefab,       enemyBase  + new Vector3(0f, 0f, 0f),  Faction.Enemy,  bank);
            EnsureBuilding(scene, "Barracks_Player", barracksPrefab, playerBase + new Vector3(6f, 0f, 4f), Faction.Player, bank);

            // --- Запечь NavMesh (здания статичны) ---
            NavMeshTab.BakeNavMesh();

            // --- Сохранение сцены ---
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[Project Forge] Setup Economy (M5) выполнен.");
        }

        private static void EnsureResourceNode(
            UnityEngine.SceneManagement.Scene scene,
            string name,
            Vector3 position,
            GameObject prefab)
        {
            // Идемпотентно — если объект с таким именем уже есть, пропускаем
            if (GameObject.Find(name) != null) return;

            if (prefab == null)
            {
                // Создаём вручную без префаба
                var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                go.name                   = name;
                go.transform.position     = position;
                go.transform.localScale   = new Vector3(2f, 0.5f, 2f);
                go.AddComponent<ResourceNode>();
                return;
            }

            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            inst.name                 = name;
            inst.transform.position   = position;
        }

        private static void EnsureBuilding(
            UnityEngine.SceneManagement.Scene scene,
            string name,
            GameObject prefab,
            Vector3 position,
            Faction faction,
            ResourceBank bank)
        {
            if (GameObject.Find(name) != null) return;

            if (prefab == null)
            {
                Debug.LogWarning($"[Project Forge] Префаб не найден для '{name}' — пропускаем.");
                return;
            }

            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            inst.name               = name;
            inst.transform.position = position;

            var building = inst.GetComponent<Building>();
            if (building != null)
            {
                var so = new SerializedObject(building);
                so.FindProperty("_faction").enumValueIndex         = (int)faction;
                so.FindProperty("_bank").objectReferenceValue      = bank;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static Vector3 GetBasePosition(string markerName)
        {
            var go = GameObject.Find(markerName);
            return go != null ? go.transform.position : Vector3.zero;
        }

        private static Material EnsureGhostMaterial(string path, Color color)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
                return existing;

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            if (shader == null)
                return null;

            var mat = new Material(shader);

            // Делаем полупрозрачным (URP)
            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1f); // 1 = Transparent
                mat.SetFloat("_Blend",   0f); // Alpha blend
                mat.renderQueue = 3000;
            }

            mat.color = color;
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);

            EnsureFolder("Assets/_Project/Art/Materials");
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        // ----------------------------------------------------------------
        // M9: Setup Scenario
        // ----------------------------------------------------------------

        internal static void SetupScenario()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                EditorUtility.DisplayDialog("Project Forge", "Нет открытой сцены.", "OK");
                return;
            }

            var managersGo = EnsureGameObject("GameManagers");

            // --- ResourceBank (должен уже быть от M5, убеждаемся) ---
            var bank = EnsureComponent<ResourceBank>(managersGo);

            // --- Маркер PlayerBaseSpawn (создать, если отсутствует) ---
            if (GameObject.Find("PlayerBaseSpawn") == null)
            {
                var spawnMarker = new GameObject("PlayerBaseSpawn");
                spawnMarker.transform.position = new Vector3(0f, 0f, -20f);
                Debug.Log("[Project Forge] Создан маркер PlayerBaseSpawn.");
            }

            // --- Barracks_Enemy у EnemyBaseSpawn (идемпотентно) ---
            var barracksPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BarracksPrefabPath);
            var enemyBarracks  = SetupEnemyBarracks(scene, barracksPrefab, bank);

            // --- EnemyCommander на GameManagers ---
            var commander = EnsureComponent<EnemyCommander>(managersGo);
            {
                var so = new SerializedObject(commander);
                so.FindProperty("_bank").objectReferenceValue         = bank;

                if (enemyBarracks != null)
                {
                    var prodBuilding = enemyBarracks.GetComponent<ProductionBuilding>();
                    so.FindProperty("_enemyBarracks").objectReferenceValue = prodBuilding;
                }

                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // --- GameWatcher на GameManagers ---
            var watcher = EnsureComponent<GameWatcher>(managersGo);
            {
                // Находим GameOverController в сцене
                GameOverController gameOverCtrl = null;
                foreach (var root in scene.GetRootGameObjects())
                {
                    gameOverCtrl = root.GetComponentInChildren<GameOverController>(includeInactive: true);
                    if (gameOverCtrl != null) break;
                }

                var so = new SerializedObject(watcher);
                so.FindProperty("_gameOver").objectReferenceValue = gameOverCtrl;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // --- Сохранение сцены ---
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[Project Forge] Setup Scenario (M9) выполнен.");
        }

        // ----------------------------------------------------------------
        // v3: Setup Tank
        // ----------------------------------------------------------------

        /// <summary>
        /// Размещает WarFactory_Player и WarFactory_Enemy в открытой сцене,
        /// проставляет EnemyCommander._enemyWarFactory.
        /// Идемпотентно.
        /// </summary>
        internal static void SetupTank()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                EditorUtility.DisplayDialog("Project Forge", "Нет открытой сцены.", "OK");
                return;
            }

            var warFactoryPrefab   = AssetDatabase.LoadAssetAtPath<GameObject>(WarFactoryPrefabPath);
            var warFactoryData     = AssetDatabase.LoadAssetAtPath<BuildingData>(WarFactoryDataPath);
            var enemyTankPrefab    = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyTankUnitPrefabPath);

            if (warFactoryPrefab == null)
            {
                Debug.LogWarning("[Project Forge] WarFactory.prefab не найден — сначала 'Create/Update Tank Prefabs (v3)'.");
                return;
            }

            var managersGo = EnsureGameObject("GameManagers");
            var bank       = EnsureComponent<ResourceBank>(managersGo);

            Vector3 playerBase = GetBasePosition("PlayerBaseSpawn");
            Vector3 enemyBase  = GetBasePosition("EnemyBaseSpawn");

            // ---- WarFactory_Player ----
            GameObject playerWF = GameObject.Find("WarFactory_Player");
            if (playerWF == null)
            {
                playerWF = (GameObject)PrefabUtility.InstantiatePrefab(warFactoryPrefab, scene);
                playerWF.name = "WarFactory_Player";
            }
            playerWF.transform.position = playerBase + new Vector3(10f, 0f, 4f);

            var playerWFBuilding = playerWF.GetComponent<Building>();
            if (playerWFBuilding != null)
            {
                var so = new SerializedObject(playerWFBuilding);
                so.FindProperty("_faction").enumValueIndex    = (int)Faction.Player;
                so.FindProperty("_bank").objectReferenceValue = bank;
                if (warFactoryData != null)
                    so.FindProperty("_data").objectReferenceValue = warFactoryData;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // ---- WarFactory_Enemy ----
            GameObject enemyWF = GameObject.Find("WarFactory_Enemy");
            if (enemyWF == null)
            {
                enemyWF = (GameObject)PrefabUtility.InstantiatePrefab(warFactoryPrefab, scene);
                enemyWF.name = "WarFactory_Enemy";
            }
            enemyWF.transform.position = enemyBase + new Vector3(10f, 0f, -4f);

            var enemyWFBuilding = enemyWF.GetComponent<Building>();
            if (enemyWFBuilding != null)
            {
                var so = new SerializedObject(enemyWFBuilding);
                so.FindProperty("_faction").enumValueIndex    = (int)Faction.Enemy;
                so.FindProperty("_bank").objectReferenceValue = bank;
                if (warFactoryData != null)
                    so.FindProperty("_data").objectReferenceValue = warFactoryData;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // Проставляем EnemyTankUnit.prefab как _unitPrefab врагу
            var enemyWFProd = enemyWF.GetComponent<ProductionBuilding>();
            if (enemyWFProd != null && enemyTankPrefab != null)
            {
                var pso = new SerializedObject(enemyWFProd);
                pso.FindProperty("_unitPrefab").objectReferenceValue = enemyTankPrefab;
                pso.ApplyModifiedPropertiesWithoutUndo();

                // Rally point к центру карты
                enemyWFProd.SetRallyPoint(Vector3.zero);
            }

            // ---- EnemyCommander: _enemyWarFactory ----
            var commander = managersGo.GetComponent<EnemyCommander>();
            if (commander == null)
            {
                Debug.LogWarning("[Project Forge] EnemyCommander не найден на GameManagers — сначала запустите Setup Scenario (M9).");
            }
            else
            {
                var cmdSo = new SerializedObject(commander);
                var enemyWFProdForCommander = enemyWF.GetComponent<ProductionBuilding>();
                cmdSo.FindProperty("_enemyWarFactory").objectReferenceValue = enemyWFProdForCommander;
                cmdSo.ApplyModifiedPropertiesWithoutUndo();
            }

            // Перезапекаем NavMesh (новые здания-препятствия)
            NavMeshTab.BakeNavMesh();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[Project Forge] Setup Tank (v3) выполнен.");
        }

        private static GameObject SetupEnemyBarracks(
            UnityEngine.SceneManagement.Scene scene,
            GameObject prefab,
            ResourceBank bank)
        {
            // Идемпотентность — если объект с именем Barracks_Enemy уже есть
            var existing = GameObject.Find("Barracks_Enemy");
            if (existing != null)
                return existing;

            Vector3 enemyBase = GetBasePosition("EnemyBaseSpawn");
            // Барак размещаем чуть в стороне от EnemyBaseSpawn, ральная точка — к центру
            Vector3 barracksPos = enemyBase + new Vector3(6f, 0f, -4f);

            GameObject inst;
            if (prefab != null)
            {
                inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            }
            else
            {
                Debug.LogWarning("[Project Forge] Barracks-префаб не найден. Создаём пустой GO.");
                inst = new GameObject();
                EnsureComponent<Building>(inst);
                EnsureComponent<ProductionBuilding>(inst);
            }

            inst.name               = "Barracks_Enemy";
            inst.transform.position = barracksPos;

            // Загружаем BarracksData и EnemyUnit-префаб
            var barracksData  = AssetDatabase.LoadAssetAtPath<BuildingData>(BarracksDataPath);
            var enemyUnitPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyUnitPrefabPath);

            // Устанавливаем фракцию Enemy через SerializedObject
            var building = inst.GetComponent<Building>();
            if (building != null)
            {
                var buildSo = new SerializedObject(building);
                buildSo.FindProperty("_faction").enumValueIndex    = (int)Faction.Enemy;
                buildSo.FindProperty("_bank").objectReferenceValue = bank;
                if (barracksData != null)
                    buildSo.FindProperty("_data").objectReferenceValue = barracksData;
                buildSo.ApplyModifiedPropertiesWithoutUndo();
            }

            // Проставляем unit prefab и rally point в ProductionBuilding
            var production = inst.GetComponent<ProductionBuilding>();
            if (production != null)
            {
                var prodSo = new SerializedObject(production);
                if (enemyUnitPrefab != null)
                    prodSo.FindProperty("_unitPrefab").objectReferenceValue = enemyUnitPrefab;
                prodSo.ApplyModifiedPropertiesWithoutUndo();

                // Rally point к центру карты (0,0,0)
                production.SetRallyPoint(Vector3.zero);
            }

            return inst;
        }

        private static void RespawnTestUnits(UnityEngine.SceneManagement.Scene scene)
        {
            // Удаляем все существующие TestUnit*
            var toDestroy = new System.Collections.Generic.List<GameObject>();
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name.StartsWith("TestUnit"))
                    toDestroy.Add(root);
                foreach (Transform child in root.transform)
                {
                    if (child.name.StartsWith("TestUnit"))
                        toDestroy.Add(child.gameObject);
                }
            }
            foreach (var go in toDestroy)
                Object.DestroyImmediate(go);

            // Спавним 5 из обновлённого префаба
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TestUnitPrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[Project Forge] TestUnit prefab не найден: {TestUnitPrefabPath}. " +
                                 "Сначала нажмите 'Create/Update TestUnit Prefab' во вкладке Prefabs.");
                return;
            }

            Vector3 spawnOrigin = Vector3.zero;
            var playerBase = GameObject.Find("PlayerBaseSpawn");
            if (playerBase != null)
                spawnOrigin = playerBase.transform.position;

            const int   count = 5;
            const float space = 2f;
            for (int i = 0; i < count; i++)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                go.name                = $"TestUnit_{i + 1}";
                go.transform.position  = spawnOrigin + new Vector3((i - 2) * space, 0f, 0f);
            }
        }

        private static void SpawnEnemyUnits(UnityEngine.SceneManagement.Scene scene)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyUnitPrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[Project Forge] EnemyUnit prefab не найден: {EnemyUnitPrefabPath}. " +
                                 "Сначала нажмите 'Create/Update EnemyUnit Prefab' во вкладке Prefabs.");
                return;
            }

            Vector3 spawnOrigin = Vector3.zero;
            var enemyBase = GameObject.Find("EnemyBaseSpawn");
            if (enemyBase != null)
                spawnOrigin = enemyBase.transform.position;

            // Считаем уже существующих EnemyUnit*
            int existing = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name.StartsWith("EnemyUnit"))
                    existing++;
                foreach (Transform child in root.transform)
                {
                    if (child.name.StartsWith("EnemyUnit"))
                        existing++;
                }
            }

            const int   total = 3;
            const float space = 2f;
            for (int i = existing; i < total; i++)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                go.name               = $"EnemyUnit_{i + 1}";
                go.transform.position = spawnOrigin + new Vector3((i - 1) * space, 0f, 0f);
            }
        }

        // ----------------------------------------------------------------
        // Вспомогательные методы M3
        // ----------------------------------------------------------------

        /// <summary>
        /// Идемпотентно создаёт или обновляет AbilityData ScriptableObject.
        /// Поля перезаписываются всегда — Forge является источником истины конфигурации
        /// (как ConfigTab для UnitData/BuildingData).
        /// </summary>
        private static AbilityData EnsureAbilityAsset(
            string assetName,
            string displayName,
            AbilityType type,
            float cooldown,
            float dashDistance       = 0f,
            float effectRadius       = 6f,
            float effectAmount       = 40f,
            float buffDuration       = 5f,
            float fireRateMultiplier = 2f,
            float damageMultiplier   = 1.5f)
        {
            var path = $"{AbilitiesFolder}/{assetName}.asset";
            var data = AssetDatabase.LoadAssetAtPath<AbilityData>(path);

            bool created = data == null;
            if (created)
                data = ScriptableObject.CreateInstance<AbilityData>();

            var so = new SerializedObject(data);
            so.FindProperty("_displayName").stringValue          = displayName;
            so.FindProperty("_cooldown").floatValue              = cooldown;
            so.FindProperty("_abilityType").enumValueIndex       = (int)type;
            so.FindProperty("_dashDistance").floatValue          = dashDistance;
            so.FindProperty("_effectRadius").floatValue          = effectRadius;
            so.FindProperty("_effectAmount").floatValue          = effectAmount;
            so.FindProperty("_buffDuration").floatValue          = buffDuration;
            so.FindProperty("_fireRateMultiplier").floatValue    = fireRateMultiplier;
            so.FindProperty("_damageMultiplier").floatValue      = damageMultiplier;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (created)
                AssetDatabase.CreateAsset(data, path);

            AssetDatabase.SaveAssets();
            return data;
        }

        private static void SpawnTestUnits(UnityEngine.SceneManagement.Scene scene)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TestUnitPrefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[Project Forge] TestUnit prefab не найден: {TestUnitPrefabPath}. " +
                                 "Сначала нажмите «Create/Update TestUnit Prefab» во вкладке Prefabs.");
                return;
            }

            // Находим базовую точку спавна
            Vector3 spawnOrigin = Vector3.zero;
            var playerBase = GameObject.Find("PlayerBaseSpawn");
            if (playerBase != null)
                spawnOrigin = playerBase.transform.position;

            const int count   = 5;
            const float space = 2f;

            // Считаем сколько TestUnit уже в сцене
            int existing = 0;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name.StartsWith("TestUnit"))
                    existing++;
                // Также ищем вложенные
                foreach (Transform child in root.transform)
                {
                    if (child.name.StartsWith("TestUnit"))
                        existing++;
                }
            }

            for (int i = existing; i < count; i++)
            {
                var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                go.name = $"TestUnit_{i + 1}";
                go.transform.position = spawnOrigin + new Vector3((i - 2) * space, 0f, 0f);
            }
        }

        // ----------------------------------------------------------------
        // Вспомогательные методы
        // ----------------------------------------------------------------

        /// <summary>Находит GO по имени или создаёт новый пустой.</summary>
        private static GameObject EnsureGameObject(string goName)
        {
            var existing = GameObject.Find(goName);
            return existing != null ? existing : new GameObject(goName);
        }

        /// <summary>Возвращает существующий компонент или добавляет новый.</summary>
        private static T EnsureComponent<T>(GameObject go) where T : Component
        {
            var existing = go.GetComponent<T>();
            return existing != null ? existing : go.AddComponent<T>();
        }

        /// <summary>Идемпотентно создаёт цепочку папок через AssetDatabase.</summary>
        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath)) return;

            var parts   = folderPath.Split('/');
            var current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        // ----------------------------------------------------------------
        // v13: уровни сложности
        // ----------------------------------------------------------------

        private const string DifficultyDataFolder    = "Assets/_Project/Data/Difficulty";
        private const string MainMenuScenePath       = "Assets/_Project/Scenes/MainMenu.unity";

        /// <summary>
        /// Создаёт ассеты DifficultyEasy/Normal/Hard; прошивает EnemyCommander._profiles в Sandbox;
        /// открывает MainMenu-сцену и строит DifficultyRow над кнопкой BtnPlay.
        /// Идемпотентно.
        /// </summary>
        internal static void SetupDifficulty()
        {
            // ---- 1. Создаём ассеты сложности ----
            EnsureFolder(DifficultyDataFolder);

            var easyAsset   = EnsureDifficultyAsset("DifficultyEasy",   "Легко",      4.0f, 0.6f, 50f,  8,  -1, 3, 0);
            var normalAsset = EnsureDifficultyAsset("DifficultyNormal", "Нормально",  2.0f, 1.0f, 30f, 12,  50, 3, 0);
            var hardAsset   = EnsureDifficultyAsset("DifficultyHard",   "Сложно",     1.0f, 1.4f, 20f, 16,  25, 2, 100);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Project Forge v13] DifficultyEasy/Normal/Hard.asset созданы/обновлены.");

            // ---- 2. Прошиваем EnemyCommander._profiles в Sandbox ----
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Sandbox.unity", OpenSceneMode.Single);
            var managersGo = GameObject.Find("GameManagers");
            if (managersGo != null)
            {
                var commander = managersGo.GetComponent<EnemyCommander>();
                if (commander != null)
                {
                    var so = new SerializedObject(commander);
                    var profilesProp = so.FindProperty("_profiles");
                    profilesProp.arraySize = 3;
                    profilesProp.GetArrayElementAtIndex(0).objectReferenceValue = easyAsset;
                    profilesProp.GetArrayElementAtIndex(1).objectReferenceValue = normalAsset;
                    profilesProp.GetArrayElementAtIndex(2).objectReferenceValue = hardAsset;
                    so.ApplyModifiedPropertiesWithoutUndo();
                    Debug.Log("[Project Forge v13] EnemyCommander._profiles прошиты в Sandbox.");
                }
                else
                {
                    Debug.LogWarning("[Project Forge v13] EnemyCommander не найден на GameManagers в Sandbox. Запустите Setup Scenario (M9) сначала.");
                }
            }
            else
            {
                Debug.LogWarning("[Project Forge v13] GameManagers не найден в Sandbox.");
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            // ---- 3. Обновляем LocTable (добавляем ключи difficulty) ----
            ConfigTab.CreateOrUpdateLocTableV10();

            // ---- 4. MainMenu: строим DifficultyRow и прошиваем MainMenuController ----
            if (!System.IO.File.Exists(System.IO.Path.GetFullPath(MainMenuScenePath)))
            {
                Debug.LogWarning("[Project Forge v13] MainMenu.unity не найдена — сначала запустите SetupM6Menus. DifficultyRow не построен.");
                EditorSceneManager.OpenScene("Assets/_Project/Scenes/Sandbox.unity", OpenSceneMode.Single);
                return;
            }

            EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);

            var mainMenuCanvas = GameObject.Find("MainMenu");
            if (mainMenuCanvas == null)
            {
                Debug.LogWarning("[Project Forge v13] Canvas 'MainMenu' не найден в MainMenu.unity.");
                EditorSceneManager.OpenScene("Assets/_Project/Scenes/Sandbox.unity", OpenSceneMode.Single);
                return;
            }

            var menuCtrl = mainMenuCanvas.GetComponent<DiplomaGame.Runtime.UI.MainMenuController>();

            // Ищем контейнер кнопок главного меню
            var buttonsGo = FindDescendantByNameStatic(mainMenuCanvas, "Buttons");
            if (buttonsGo == null)
            {
                Debug.LogWarning("[Project Forge v13] 'Buttons' не найден в MainMenu-Canvas.");
            }
            else
            {
                // Строим DifficultyRow выше кнопки BtnPlay (sibling index 0)
                AddDifficultyRowToMainMenu(mainMenuCanvas, buttonsGo, menuCtrl);
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            // Возвращаемся в Sandbox
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Sandbox.unity", OpenSceneMode.Single);
            Debug.Log("[Project Forge v13] Setup Difficulty (v13) выполнен.");
        }

        /// <summary>
        /// Идемпотентно создаёт или обновляет DifficultyProfileSO-ассет.
        /// </summary>
        private static DifficultyProfileSO EnsureDifficultyAsset(
            string assetName,
            string displayName,
            float  decisionInterval,
            float  waveSizeScale,
            float  maxWaitTime,
            int    maxUnits,
            int    researchReserve,
            int    infantryRatio,
            int    enemyStartingBonusGold)
        {
            string path     = $"{DifficultyDataFolder}/{assetName}.asset";
            var    existing = AssetDatabase.LoadAssetAtPath<DifficultyProfileSO>(path);

            DifficultyProfileSO data;
            if (existing != null)
            {
                data = existing;
            }
            else
            {
                data = ScriptableObject.CreateInstance<DifficultyProfileSO>();
                AssetDatabase.CreateAsset(data, path);
            }

            var so = new SerializedObject(data);
            so.FindProperty("_displayName").stringValue             = displayName;
            so.FindProperty("_decisionInterval").floatValue         = decisionInterval;
            so.FindProperty("_waveSizeScale").floatValue            = waveSizeScale;
            so.FindProperty("_maxWaitTime").floatValue              = maxWaitTime;
            so.FindProperty("_maxUnits").intValue                   = maxUnits;
            so.FindProperty("_researchReserve").intValue            = researchReserve;
            so.FindProperty("_infantryRatio").intValue              = infantryRatio;
            so.FindProperty("_enemyStartingBonusGold").intValue     = enemyStartingBonusGold;
            so.ApplyModifiedPropertiesWithoutUndo();

            return data;
        }

        /// <summary>
        /// Строит DifficultyRow в MainMenu-сцене: Label «Сложность:» + TMP_Dropdown.
        /// Помещает строку перед кнопками (sibling 0 или над BtnPlay).
        /// Прошивает MainMenuController.difficultyDropdown.
        /// </summary>
        private static void AddDifficultyRowToMainMenu(
            GameObject canvasGo,
            GameObject buttonsGo,
            DiplomaGame.Runtime.UI.MainMenuController menuCtrl)
        {
            // Ищем или создаём DifficultyRow внутри buttonsGo
            var existingRow = buttonsGo.transform.Find("DifficultyRow");
            GameObject rowGo;
            if (existingRow != null)
            {
                rowGo = existingRow.gameObject;
            }
            else
            {
                rowGo = new GameObject("DifficultyRow");
                rowGo.AddComponent<RectTransform>();
                rowGo.transform.SetParent(buttonsGo.transform, false);
                // Помещаем перед BtnPlay (sibling 0)
                rowGo.transform.SetSiblingIndex(0);
            }

            // Размер строки
            var rowRt = rowGo.GetComponent<RectTransform>();
            rowRt.sizeDelta = new Vector2(0f, 50f);

            // Label
            var labelGo = EnsureChildObject(rowGo, "DifficultyLabel");
            var labelTmp = EnsureComponentOn<TMPro.TextMeshProUGUI>(labelGo);
            labelTmp.text      = "Сложность:";
            labelTmp.fontSize  = 22f;
            labelTmp.color     = Color.white;
            {
                var lrt = labelGo.GetComponent<RectTransform>();
                lrt.anchorMin        = new Vector2(0f, 0f);
                lrt.anchorMax        = new Vector2(0.4f, 1f);
                lrt.offsetMin        = Vector2.zero;
                lrt.offsetMax        = Vector2.zero;
            }

            // TMP_Dropdown
            var dropdownGo = EnsureChildObject(rowGo, "DifficultyDropdown");
            var dropdown   = EnsureComponentOn<TMPro.TMP_Dropdown>(dropdownGo);
            {
                var drt = dropdownGo.GetComponent<RectTransform>();
                drt.anchorMin        = new Vector2(0.42f, 0.1f);
                drt.anchorMax        = new Vector2(1f,    0.9f);
                drt.offsetMin        = Vector2.zero;
                drt.offsetMax        = Vector2.zero;
            }

            // Прошиваем ссылку в MainMenuController
            if (menuCtrl != null)
            {
                var so = new SerializedObject(menuCtrl);
                so.FindProperty("difficultyDropdown").objectReferenceValue = dropdown;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static GameObject EnsureChildObject(GameObject parent, string childName)
        {
            var existing = parent.transform.Find(childName);
            if (existing != null) return existing.gameObject;

            var go = new GameObject(childName);
            go.transform.SetParent(parent.transform, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        private static T EnsureComponentOn<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }

        private static GameObject FindDescendantByNameStatic(GameObject root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            foreach (Transform child in root.transform)
            {
                var found = FindDescendantByNameStatic(child.gameObject, name);
                if (found != null) return found;
            }
            return null;
        }

        // ----------------------------------------------------------------
        // v11: перепрошивка вражеского производства
        // ----------------------------------------------------------------

        /// <summary>
        /// Идемпотентно переводит Barracks_Enemy и WarFactory_Enemy на СОБСТВЕННЫЕ
        /// BuildingData (EnemyBarracks/EnemyWarFactory.asset) и пер-инстансный маппинг
        /// _unitPrefabs на вражеские префабы. Причина (круг 11): общий префаб нёс
        /// v6-маппинг на player-префабы — враг производил юнитов фракции игрока.
        /// </summary>
        internal static void RewireEnemyProductionV11()
        {
            var scene = EditorSceneManager.GetActiveScene();

            var enemyBarracksData = AssetDatabase.LoadAssetAtPath<BuildingData>(
                "Assets/_Project/Data/Buildings/EnemyBarracks.asset");
            var enemyWFData = AssetDatabase.LoadAssetAtPath<BuildingData>(
                "Assets/_Project/Data/Buildings/EnemyWarFactory.asset");
            var gruntData = AssetDatabase.LoadAssetAtPath<UnitData>(
                "Assets/_Project/Data/Units/EnemyGrunt.asset");
            var eTankData = AssetDatabase.LoadAssetAtPath<UnitData>(
                "Assets/_Project/Data/Units/EnemyTank.asset");
            var enemyUnitPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyUnitPrefabPath);
            var enemyTankPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyTankUnitPrefabPath);

            if (enemyBarracksData == null || enemyWFData == null)
            {
                Debug.LogWarning("[Project Forge v11] Enemy BuildingData не найдены — " +
                                 "сначала ConfigTab.CreateOrUpdateEnemyBuildingDataV11().");
                return;
            }

            RewireEnemyBuilding("Barracks_Enemy",   enemyBarracksData, gruntData,  enemyUnitPrefab);
            RewireEnemyBuilding("WarFactory_Enemy", enemyWFData,       eTankData,  enemyTankPrefab);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[Project Forge] v11: вражеское производство перепрошито (свои данные и префабы).");
        }

        private static void RewireEnemyBuilding(
            string objectName, BuildingData data, UnitData unitData, GameObject unitPrefab)
        {
            var go = GameObject.Find(objectName);
            if (go == null)
            {
                Debug.LogWarning($"[Project Forge v11] {objectName} не найден в сцене.");
                return;
            }

            var building = go.GetComponent<Building>();
            if (building != null)
            {
                var so = new SerializedObject(building);
                so.FindProperty("_data").objectReferenceValue = data;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            var prod = go.GetComponent<ProductionBuilding>();
            if (prod != null)
            {
                var so = new SerializedObject(prod);

                // Пер-инстансный маппинг: ВРАЖЕСКИЙ UnitData -> вражеский префаб.
                // Перекрывает унаследованный из общего префаба player-маппинг.
                var map = so.FindProperty("_unitPrefabs");
                map.arraySize = 1;
                var elem = map.GetArrayElementAtIndex(0);
                elem.FindPropertyRelative("unitData").objectReferenceValue = unitData;
                elem.FindPropertyRelative("prefab").objectReferenceValue   = unitPrefab;

                // Legacy-fallback тоже на вражеский префаб
                so.FindProperty("_unitPrefab").objectReferenceValue = unitPrefab;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

    }
}
