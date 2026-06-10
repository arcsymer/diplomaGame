using DiplomaGame.Runtime.Units;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace DiplomaGame.Editor
{
    /// <summary>
    /// Вкладка Prefabs — создание и обновление игровых префабов.
    /// M2: TestUnit — капсула с NavMeshAgent, Unit и кольцом выделения.
    /// </summary>
    internal sealed class PrefabsTab : IForgeTab
    {
        private const string TestUnitPrefabPath = "Assets/_Project/Prefabs/Units/TestUnit.prefab";

        public string Title => "Prefabs";

        public void OnGUI()
        {
            GUILayout.Space(8);
            GUILayout.Label("Префабы", EditorStyles.boldLabel);
            GUILayout.Space(4);

            EditorGUILayout.HelpBox(
                "Создаёт или обновляет префаб TestUnit.\n" +
                "Операция идемпотентна — повторный запуск не дублирует компоненты.",
                MessageType.Info);

            GUILayout.Space(4);

            if (GUILayout.Button("Create/Update TestUnit Prefab", GUILayout.Height(32)))
                CreateOrUpdateTestUnitPrefab();
        }

        // ----------------------------------------------------------------
        // Основная операция
        // ----------------------------------------------------------------

        internal static void CreateOrUpdateTestUnitPrefab()
        {
            // Убеждаемся, что папка существует
            EnsureFolder("Assets/_Project/Prefabs");
            EnsureFolder("Assets/_Project/Prefabs/Units");

            // Создаём временный GO в памяти
            var root = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            root.name = "TestUnit";

            // --- NavMeshAgent ---
            var agent = EnsureComponent<NavMeshAgent>(root);
            agent.speed         = 5f;
            agent.angularSpeed  = 360f;
            agent.acceleration  = 12f;

            // --- Unit компонент ---
            var unit = EnsureComponent<Unit>(root);
            var unitSo = new SerializedObject(unit);
            unitSo.FindProperty("_faction").enumValueIndex = (int)Faction.Player;
            unitSo.ApplyModifiedPropertiesWithoutUndo();

            // --- SelectionRing (дочерний цилиндр) ---
            GameObject ring = null;
            var existingRing = root.transform.Find("SelectionRing");
            if (existingRing != null)
            {
                ring = existingRing.gameObject;
            }
            else
            {
                ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                ring.name = "SelectionRing";
                ring.transform.SetParent(root.transform, false);
            }

            ring.transform.localScale    = new Vector3(1.4f, 0.02f, 1.4f);
            ring.transform.localPosition = new Vector3(0f, -0.95f, 0f);

            // Удаляем коллайдер с кольца
            var ringCollider = ring.GetComponent<Collider>();
            if (ringCollider != null)
                Object.DestroyImmediate(ringCollider);

            // Кольцо изначально выключено
            ring.SetActive(false);

            // --- Сохраняем префаб ---
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, TestUnitPrefabPath);
            Object.DestroyImmediate(root);

            if (prefab != null)
                Debug.Log($"[Project Forge] TestUnit prefab сохранён: {TestUnitPrefabPath}");
            else
                Debug.LogError($"[Project Forge] Не удалось сохранить TestUnit prefab: {TestUnitPrefabPath}");
        }

        // ----------------------------------------------------------------
        // Вспомогательные методы
        // ----------------------------------------------------------------

        private static T EnsureComponent<T>(GameObject go) where T : Component
        {
            var existing = go.GetComponent<T>();
            return existing != null ? existing : go.AddComponent<T>();
        }

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
