using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DiplomaGame.Editor
{
    /// <summary>
    /// Вкладка NavMesh — запекание навигационной сетки для открытой сцены.
    /// </summary>
    internal sealed class NavMeshTab : IForgeTab
    {
        public string Title => "NavMesh";

        public void OnGUI()
        {
            GUILayout.Space(8);
            GUILayout.Label("NavMesh", EditorStyles.boldLabel);
            GUILayout.Space(4);

            EditorGUILayout.HelpBox(
                "Добавляет NavMeshSurface на объект \"Ground\" и запекает NavMesh.\n" +
                "Операция идемпотентна.",
                MessageType.Info);

            GUILayout.Space(4);

            if (GUILayout.Button("Bake NavMesh (открытая сцена)", GUILayout.Height(32)))
                BakeNavMesh();
        }

        // ----------------------------------------------------------------
        // Основная операция
        // ----------------------------------------------------------------

        internal static void BakeNavMesh()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                EditorUtility.DisplayDialog("Project Forge", "Нет открытой сцены.", "OK");
                return;
            }

            var ground = GameObject.Find("Ground");
            if (ground == null)
            {
                EditorUtility.DisplayDialog("Project Forge",
                    "Объект \"Ground\" не найден в открытой сцене. Создайте его или переименуйте плоскость.",
                    "OK");
                return;
            }

            var surface = EnsureComponent<NavMeshSurface>(ground);
            surface.BuildNavMesh();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[Project Forge] Bake NavMesh выполнен для объекта Ground.");
        }

        // ----------------------------------------------------------------
        // Вспомогательные методы
        // ----------------------------------------------------------------

        private static T EnsureComponent<T>(GameObject go) where T : Component
        {
            var existing = go.GetComponent<T>();
            return existing != null ? existing : go.AddComponent<T>();
        }
    }
}
