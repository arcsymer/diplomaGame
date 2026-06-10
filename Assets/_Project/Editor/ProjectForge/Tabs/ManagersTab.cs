using DiplomaGame.Runtime.CameraControl;
using DiplomaGame.Runtime.Commands;
using DiplomaGame.Runtime.Core;
using DiplomaGame.Runtime.Data;
using DiplomaGame.Runtime.Hero;
using DiplomaGame.Runtime.Selection;
using DiplomaGame.Runtime.Units;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace DiplomaGame.Editor
{
    /// <summary>
    /// Вкладка Managers — настройка игровых менеджеров сцены.
    /// M1: кнопка "Setup Mode Rig" идемпотентно собирает камерный риг
    /// и GameModeController в открытой сцене.
    /// M2: кнопка "Setup RTS Control" добавляет SelectionSystem, CommandInput,
    /// RtsCameraController и спавнит 5 TestUnit.
    /// M3: кнопка "Setup Hero (M3)" настраивает HeroController, HeroShooter, AbilitySystem.
    /// </summary>
    internal sealed class ManagersTab : IForgeTab
    {
        private const string InputActionsPath    = "Assets/_Project/Settings/GameControls.inputactions";
        private const string TestUnitPrefabPath  = "Assets/_Project/Prefabs/Units/TestUnit.prefab";
        private const string AbilitiesFolder     = "Assets/_Project/Data/Abilities";

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
            var rtsTarget = EnsureGameObject("RtsCameraTarget");
            rtsTarget.transform.position = Vector3.zero;

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
            tpsFollow.FollowOffset = new Vector3(0.5f, 2f, -4f);

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

            // --- SO-ассеты способностей (идемпотентно) ---
            EnsureFolder(AbilitiesFolder);
            var dashAsset   = EnsureAbilityAsset("Dash",     AbilityType.Dash,         4f,  6f);
            var ab2Asset    = EnsureAbilityAsset("Ability2", AbilityType.Placeholder2,  8f,  0f);
            var ab3Asset    = EnsureAbilityAsset("Ability3", AbilityType.Placeholder3,  8f,  0f);
            var ab4Asset    = EnsureAbilityAsset("Ability4", AbilityType.Placeholder4,  8f,  0f);

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
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // --- Сохранение сцены ---
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[Project Forge] Setup Hero (M3) выполнен.");
        }

        // ----------------------------------------------------------------
        // Вспомогательные методы M3
        // ----------------------------------------------------------------

        /// <summary>
        /// Идемпотентно создаёт AbilityData ScriptableObject или загружает существующий.
        /// </summary>
        private static AbilityData EnsureAbilityAsset(string assetName, AbilityType type, float cooldown, float dashDistance)
        {
            var path     = $"{AbilitiesFolder}/{assetName}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<AbilityData>(path);
            if (existing != null)
                return existing;

            var data = ScriptableObject.CreateInstance<AbilityData>();
            var so   = new SerializedObject(data);
            so.FindProperty("_displayName").stringValue       = assetName;
            so.FindProperty("_cooldown").floatValue           = cooldown;
            so.FindProperty("_abilityType").enumValueIndex    = (int)type;
            so.FindProperty("_dashDistance").floatValue       = dashDistance;
            so.ApplyModifiedPropertiesWithoutUndo();

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

    }
}
