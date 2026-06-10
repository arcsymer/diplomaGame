using System.Collections.Generic;
using System.Linq;
using DiplomaGame.Runtime.Core;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DiplomaGame.Editor
{
    /// <summary>
    /// Статический класс без состояния. Содержит всю логику валидации проекта.
    /// Вынесен отдельно для прямого тестирования из EditMode-тестов.
    /// </summary>
    public static class ForgeValidator
    {
        private const string TestUnitPrefabPath = "Assets/_Project/Prefabs/Units/TestUnit.prefab";

        private static readonly string[] RequiredFolders =
        {
            "Assets/_Project/Scripts",
            "Assets/_Project/Scenes",
            "Assets/_Project/Prefabs",
            "Assets/_Project/Data",
            "Assets/_Project/Art",
            "Assets/_Project/Audio",
            "Assets/_Project/UI",
            "Assets/_Project/VFX",
        };

        private const string SandboxScenePath = "Assets/_Project/Scenes/Sandbox.unity";

        /// <summary>
        /// Запускает все проверки. Возвращает список проблем;
        /// пустой список означает, что всё в порядке.
        /// </summary>
        public static List<string> Validate()
        {
            var issues = new List<string>();

            CheckRequiredFolders(issues);
            CheckSandboxInBuildSettings(issues);
            CheckMissingScriptsInOpenScene(issues);
            CheckGameModeControllerRefs(issues);
            CheckTestUnitPrefabExists(issues);
            CheckNavMeshSurfaceOnGround(issues);

            return issues;
        }

        /// <summary>
        /// Идемпотентно создаёт папки структуры проекта через AssetDatabase.
        /// </summary>
        public static void BootstrapProjectStructure()
        {
            foreach (var folder in RequiredFolders)
                EnsureFolder(folder);

            AssetDatabase.Refresh();
        }

        // ----------------------------------------------------------------
        // Приватные проверки
        // ----------------------------------------------------------------

        private static void CheckRequiredFolders(List<string> issues)
        {
            foreach (var folder in RequiredFolders)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                    issues.Add($"Отсутствует папка: {folder}");
            }
        }

        private static void CheckSandboxInBuildSettings(List<string> issues)
        {
            bool found = EditorBuildSettings.scenes
                .Any(s => s.path == SandboxScenePath);

            if (!found)
                issues.Add($"Sandbox не добавлена в Build Settings: {SandboxScenePath}");
        }

        private static void CheckMissingScriptsInOpenScene(List<string> issues)
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid()) return;

            int count = 0;
            foreach (var root in scene.GetRootGameObjects())
                count += CountMissingScripts(root);

            if (count > 0)
                issues.Add($"Missing scripts в открытой сцене ({scene.name}): {count} шт.");
        }

        private static void CheckGameModeControllerRefs(List<string> issues)
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid()) return;

            // Ищем GameManagers с GameModeController в открытой сцене
            foreach (var root in scene.GetRootGameObjects())
            {
                var controller = root.GetComponentInChildren<GameModeController>(includeInactive: true);
                if (controller == null) continue;

                var so = new SerializedObject(controller);

                if (so.FindProperty("rtsCamera").objectReferenceValue == null)
                    issues.Add("GameModeController: поле rtsCamera не заполнено (запустите Setup Mode Rig).");

                if (so.FindProperty("tpsCamera").objectReferenceValue == null)
                    issues.Add("GameModeController: поле tpsCamera не заполнено (запустите Setup Mode Rig).");

                if (so.FindProperty("actions").objectReferenceValue == null)
                    issues.Add("GameModeController: поле actions (InputActionAsset) не заполнено (запустите Setup Mode Rig).");

                // Проверяем только первый найденный контроллер в сцене
                break;
            }
        }

        private static void CheckTestUnitPrefabExists(List<string> issues)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TestUnitPrefabPath);
            if (prefab == null)
                issues.Add($"Префаб TestUnit не найден: {TestUnitPrefabPath} (запустите Create/Update TestUnit Prefab).");
        }

        private static void CheckNavMeshSurfaceOnGround(List<string> issues)
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid()) return;

            var ground = GameObject.Find("Ground");
            if (ground == null) return; // Ground не в сцене — не проверяем

            if (ground.GetComponent<NavMeshSurface>() == null)
                issues.Add("На объекте Ground нет NavMeshSurface (запустите Bake NavMesh).");
        }

        private static int CountMissingScripts(GameObject go)
        {
            int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);

            foreach (Transform child in go.transform)
                count += CountMissingScripts(child.gameObject);

            return count;
        }

        // ----------------------------------------------------------------
        // Вспомогательный метод создания папок
        // ----------------------------------------------------------------

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath)) return;

            // Рекурсивно создаём всю цепочку родительских папок
            var parts = folderPath.Split('/');
            var current = parts[0]; // "Assets"
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
