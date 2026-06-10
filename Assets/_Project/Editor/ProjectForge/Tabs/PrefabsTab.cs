using DiplomaGame.Runtime.Combat;
using DiplomaGame.Runtime.Data;
using DiplomaGame.Runtime.Units;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace DiplomaGame.Editor
{
    /// <summary>
    /// Вкладка Prefabs — создание и обновление игровых префабов.
    /// M2: TestUnit — капсула с NavMeshAgent, Unit и кольцом выделения.
    /// M4: Health и UnitCombat добавлены к TestUnit; новый EnemyUnit с красным материалом.
    /// </summary>
    internal sealed class PrefabsTab : IForgeTab
    {
        private const string TestUnitPrefabPath  = "Assets/_Project/Prefabs/Units/TestUnit.prefab";
        private const string EnemyUnitPrefabPath = "Assets/_Project/Prefabs/Units/EnemyUnit.prefab";
        private const string MarineDataPath      = "Assets/_Project/Data/Units/Marine.asset";
        private const string EnemyGruntDataPath  = "Assets/_Project/Data/Units/EnemyGrunt.asset";
        private const string EnemyMatPath        = "Assets/_Project/Art/Materials/EnemyRed.mat";

        public string Title => "Prefabs";

        public void OnGUI()
        {
            GUILayout.Space(8);
            GUILayout.Label("Префабы", EditorStyles.boldLabel);
            GUILayout.Space(4);

            EditorGUILayout.HelpBox(
                "Создаёт или обновляет префаб TestUnit.\n" +
                "M4: добавлены Health и UnitCombat (data=Marine).\n" +
                "Операция идемпотентна — повторный запуск не дублирует компоненты.",
                MessageType.Info);

            GUILayout.Space(4);

            if (GUILayout.Button("Create/Update TestUnit Prefab", GUILayout.Height(32)))
                CreateOrUpdateTestUnitPrefab();

            GUILayout.Space(8);

            EditorGUILayout.HelpBox(
                "Создаёт или обновляет префаб EnemyUnit (фракция Enemy, data=EnemyGrunt, красный материал).\n" +
                "Операция идемпотентна.",
                MessageType.Info);

            GUILayout.Space(4);

            if (GUILayout.Button("Create/Update EnemyUnit Prefab", GUILayout.Height(32)))
                CreateOrUpdateEnemyUnitPrefab();
        }

        // ----------------------------------------------------------------
        // TestUnit
        // ----------------------------------------------------------------

        internal static void CreateOrUpdateTestUnitPrefab()
        {
            EnsureFolder("Assets/_Project/Prefabs");
            EnsureFolder("Assets/_Project/Prefabs/Units");

            var root = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            root.name = "TestUnit";

            // --- NavMeshAgent ---
            var agent = EnsureComponent<NavMeshAgent>(root);
            agent.speed        = 5f;
            agent.angularSpeed = 360f;
            agent.acceleration = 12f;

            // --- Unit ---
            var unit   = EnsureComponent<Unit>(root);
            var unitSo = new SerializedObject(unit);
            unitSo.FindProperty("_faction").enumValueIndex = (int)Faction.Player;
            unitSo.ApplyModifiedPropertiesWithoutUndo();

            // --- Health (M4) ---
            EnsureComponent<Health>(root);

            // --- UnitCombat (M4) ---
            var combat   = EnsureComponent<UnitCombat>(root);
            var marineData = AssetDatabase.LoadAssetAtPath<UnitData>(MarineDataPath);
            if (marineData != null)
            {
                var combatSo = new SerializedObject(combat);
                combatSo.FindProperty("_data").objectReferenceValue = marineData;
                combatSo.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                Debug.LogWarning($"[Project Forge] Marine.asset не найден: {MarineDataPath}. " +
                                 "Сначала запустите 'Create/Update Unit Data Assets (M4)' во вкладке Config.");
            }

            // --- SelectionRing ---
            var ring = EnsureSelectionRing(root);
            ring.SetActive(false);

            // --- Сохранение ---
            SavePrefab(root, TestUnitPrefabPath);
        }

        // ----------------------------------------------------------------
        // EnemyUnit
        // ----------------------------------------------------------------

        internal static void CreateOrUpdateEnemyUnitPrefab()
        {
            EnsureFolder("Assets/_Project/Prefabs");
            EnsureFolder("Assets/_Project/Prefabs/Units");
            EnsureFolder("Assets/_Project/Art");
            EnsureFolder("Assets/_Project/Art/Materials");

            var root = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            root.name = "EnemyUnit";

            // --- Красный материал (URP Lit) ---
            var enemyMat = EnsureEnemyMaterial();
            if (enemyMat != null)
            {
                var mr = root.GetComponent<MeshRenderer>();
                if (mr != null)
                    mr.sharedMaterial = enemyMat;
            }

            // --- NavMeshAgent ---
            var agent = EnsureComponent<NavMeshAgent>(root);
            agent.speed        = 4.5f;
            agent.angularSpeed = 360f;
            agent.acceleration = 12f;

            // --- Unit (Enemy) ---
            var unit   = EnsureComponent<Unit>(root);
            var unitSo = new SerializedObject(unit);
            unitSo.FindProperty("_faction").enumValueIndex = (int)Faction.Enemy;
            unitSo.ApplyModifiedPropertiesWithoutUndo();

            // --- Health ---
            EnsureComponent<Health>(root);

            // --- UnitCombat ---
            var combat        = EnsureComponent<UnitCombat>(root);
            var enemyGruntData = AssetDatabase.LoadAssetAtPath<UnitData>(EnemyGruntDataPath);
            if (enemyGruntData != null)
            {
                var combatSo = new SerializedObject(combat);
                combatSo.FindProperty("_data").objectReferenceValue = enemyGruntData;
                combatSo.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                Debug.LogWarning($"[Project Forge] EnemyGrunt.asset не найден: {EnemyGruntDataPath}. " +
                                 "Сначала запустите 'Create/Update Unit Data Assets (M4)' во вкладке Config.");
            }

            // --- SelectionRing ---
            var ring = EnsureSelectionRing(root);
            ring.SetActive(false);

            // --- Сохранение ---
            SavePrefab(root, EnemyUnitPrefabPath);
        }

        // ----------------------------------------------------------------
        // Вспомогательные методы
        // ----------------------------------------------------------------

        /// <summary>
        /// Создаёт или возвращает существующий материал EnemyRed (URP Lit, красный).
        /// </summary>
        private static Material EnsureEnemyMaterial()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(EnemyMatPath);
            if (existing != null)
                return existing;

            // Ищем URP Lit-шейдер
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard"); // fallback

            if (shader == null)
            {
                Debug.LogWarning("[Project Forge] Не найден шейдер URP Lit и Standard. Материал не создан.");
                return null;
            }

            var mat = new Material(shader);
            mat.color = Color.red;

            // В URP Lit базовый цвет хранится в _BaseColor
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", Color.red);

            AssetDatabase.CreateAsset(mat, EnemyMatPath);
            AssetDatabase.SaveAssets();
            return mat;
        }

        private static GameObject EnsureSelectionRing(GameObject root)
        {
            var existingRing = root.transform.Find("SelectionRing");
            GameObject ring;
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

            var ringCollider = ring.GetComponent<Collider>();
            if (ringCollider != null)
                Object.DestroyImmediate(ringCollider);

            return ring;
        }

        private static void SavePrefab(GameObject root, string path)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);

            if (prefab != null)
                Debug.Log($"[Project Forge] Префаб сохранён: {path}");
            else
                Debug.LogError($"[Project Forge] Не удалось сохранить префаб: {path}");
        }

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
