using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DiplomaGame.Editor
{
    /// <summary>
    /// Управление сценами проекта. Все операции идемпотентны.
    /// </summary>
    internal sealed class ScenesTab : IForgeTab
    {
        private const string SandboxScenePath = "Assets/_Project/Scenes/Sandbox.unity";

        public string Title => "Scenes";

        public void OnGUI()
        {
            GUILayout.Space(8);
            GUILayout.Label("Управление сценами", EditorStyles.boldLabel);
            GUILayout.Space(4);

            if (GUILayout.Button("Create/Update Sandbox Scene", GUILayout.Height(32)))
                CreateOrUpdateSandboxScene();
        }

        /// <summary>
        /// Идемпотентно создаёт или обновляет сцену Sandbox:
        /// Plane, Directional Light, два маркера спавна.
        /// </summary>
        private static void CreateOrUpdateSandboxScene()
        {
            // Сохраняем текущую открытую сцену, чтобы вернуться к ней
            var originalScene = SceneManager.GetActiveScene();
            bool shouldReturnToOriginal = originalScene.IsValid() && !string.IsNullOrEmpty(originalScene.path);

            // Создаём папку Scenes, если её нет
            const string scenesDir = "Assets/_Project/Scenes";
            if (!AssetDatabase.IsValidFolder(scenesDir))
                AssetDatabase.CreateFolder("Assets/_Project", "Scenes");

            // Открываем или создаём сцену
            Scene sandbox;
            if (File.Exists(SandboxScenePath))
            {
                sandbox = EditorSceneManager.OpenScene(SandboxScenePath, OpenSceneMode.Single);
            }
            else
            {
                sandbox = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }

            // --- Ground (Plane 100×100) ---
            EnsurePlane();

            // --- Directional Light "Sun" ---
            EnsureDirectionalLight();

            // --- Маркеры спавна ---
            EnsureMarker("PlayerBaseSpawn", new Vector3(-30f, 0f, -30f));
            EnsureMarker("EnemyBaseSpawn",  new Vector3(30f,  0f,  30f));

            // Сохраняем сцену
            EditorSceneManager.SaveScene(sandbox, SandboxScenePath);
            AssetDatabase.Refresh();

            // Добавляем в Build Settings без дублей
            AddSceneToBuildSettings(SandboxScenePath);

            Debug.Log("[Project Forge] Sandbox scene создана/обновлена: " + SandboxScenePath);
        }

        private static void EnsurePlane()
        {
            const string objName = "Ground";
            var existing = GameObject.Find(objName);
            if (existing == null)
            {
                existing = GameObject.CreatePrimitive(PrimitiveType.Plane);
                existing.name = objName;
            }

            existing.transform.position = Vector3.zero;
            // Plane по умолчанию 10×10 units; scale (10,1,10) = 100×100
            existing.transform.localScale = new Vector3(10f, 1f, 10f);

            // Помечаем статичным для NavMesh и прочего
            GameObjectUtility.SetStaticEditorFlags(existing,
                StaticEditorFlags.ContributeGI |
                StaticEditorFlags.NavigationStatic |
                StaticEditorFlags.BatchingStatic |
                StaticEditorFlags.OccluderStatic);
        }

        private static void EnsureDirectionalLight()
        {
            const string objName = "Sun";

            // Ищем существующий Directional Light с нужным именем
            var existing = GameObject.Find(objName);
            if (existing == null)
            {
                existing = new GameObject(objName);
                var light = existing.AddComponent<Light>();
                light.type = LightType.Directional;
                light.shadows = LightShadows.Soft;
            }

            existing.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private static void EnsureMarker(string markerName, Vector3 position)
        {
            var existing = GameObject.Find(markerName);
            if (existing == null)
                existing = new GameObject(markerName);

            existing.transform.position = position;
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            bool alreadyPresent = scenes.Any(s => s.path == scenePath);
            if (!alreadyPresent)
            {
                scenes.Add(new EditorBuildSettingsScene(scenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
                Debug.Log("[Project Forge] Sandbox добавлена в Build Settings.");
            }
        }
    }
}
