using DiplomaGame.Runtime.Core;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DiplomaGame.Editor
{
    /// <summary>
    /// Вкладка Managers — настройка игровых менеджеров сцены.
    /// M1: кнопка "Setup Mode Rig" идемпотентно собирает камерный риг
    /// и GameModeController в открытой сцене.
    /// </summary>
    internal sealed class ManagersTab : IForgeTab
    {
        private const string InputActionsPath = "Assets/_Project/Settings/GameControls.inputactions";

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

    }
}
