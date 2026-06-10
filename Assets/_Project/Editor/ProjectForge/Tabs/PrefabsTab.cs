using DiplomaGame.Runtime.Buildings;
using DiplomaGame.Runtime.Combat;
using DiplomaGame.Runtime.Data;
using DiplomaGame.Runtime.Economy;
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
    /// M5: Building-префабы HQ, Barracks, Extractor; ResourceNode.
    /// </summary>
    internal sealed class PrefabsTab : IForgeTab
    {
        private const string TestUnitPrefabPath  = "Assets/_Project/Prefabs/Units/TestUnit.prefab";
        private const string EnemyUnitPrefabPath = "Assets/_Project/Prefabs/Units/EnemyUnit.prefab";
        private const string MarineDataPath      = "Assets/_Project/Data/Units/Marine.asset";
        private const string EnemyGruntDataPath  = "Assets/_Project/Data/Units/EnemyGrunt.asset";
        private const string EnemyMatPath        = "Assets/_Project/Art/Materials/EnemyRed.mat";

        // M5 пути
        private const string HQPrefabPath           = "Assets/_Project/Prefabs/Buildings/HQ.prefab";
        private const string BarracksPrefabPath      = "Assets/_Project/Prefabs/Buildings/Barracks.prefab";
        private const string ExtractorPrefabPath     = "Assets/_Project/Prefabs/Buildings/Extractor.prefab";
        private const string ResourceNodePrefabPath  = "Assets/_Project/Prefabs/Props/ResourceNode.prefab";
        private const string PlayerBlueMatPath       = "Assets/_Project/Art/Materials/PlayerBlue.mat";
        private const string CrystalYellowMatPath    = "Assets/_Project/Art/Materials/CrystalYellow.mat";
        private const string HQDataPath              = "Assets/_Project/Data/Buildings/HQ.asset";
        private const string BarracksDataPath        = "Assets/_Project/Data/Buildings/Barracks.asset";
        private const string ExtractorDataPath       = "Assets/_Project/Data/Buildings/Extractor.asset";

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

            GUILayout.Space(8);

            EditorGUILayout.HelpBox(
                "M5: создаёт или обновляет строительные префабы:\n" +
                "• HQ.prefab — штаб (бокс 4×3×4, синий)\n" +
                "• Barracks.prefab — казарма (бокс 3×2×3)\n" +
                "• Extractor.prefab — экстрактор (цилиндр 2×1×2)\n" +
                "• Props/ResourceNode.prefab — месторождение кристаллов (цилиндр жёлтый)\n" +
                "Операция идемпотентна.",
                MessageType.Info);

            GUILayout.Space(4);

            if (GUILayout.Button("Create/Update Building Prefabs (M5)", GUILayout.Height(32)))
                CreateOrUpdateBuildingPrefabs();
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
        // M5: Building Prefabs
        // ----------------------------------------------------------------

        internal static void CreateOrUpdateBuildingPrefabs()
        {
            EnsureFolder("Assets/_Project/Prefabs/Buildings");
            EnsureFolder("Assets/_Project/Prefabs/Props");
            EnsureFolder("Assets/_Project/Art/Materials");

            var playerBlueMat    = EnsureMaterial(PlayerBlueMatPath,    new Color(0.2f, 0.4f, 0.9f));
            var crystalYellowMat = EnsureMaterial(CrystalYellowMatPath, new Color(1f, 0.9f, 0.1f));

            // Загружаем Building Data ассеты
            var hqData        = AssetDatabase.LoadAssetAtPath<BuildingData>(HQDataPath);
            var barracksData  = AssetDatabase.LoadAssetAtPath<BuildingData>(BarracksDataPath);
            var extractorData = AssetDatabase.LoadAssetAtPath<BuildingData>(ExtractorDataPath);
            var testUnitPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TestUnitPrefabPath);

            if (hqData == null || barracksData == null || extractorData == null)
                Debug.LogWarning("[Project Forge] BuildingData ассеты не найдены — сначала запустите 'Create/Update Building Data (M5)'.");

            // --- HQ (бокс 4×3×4) ---
            {
                var root = CreateBuildingRoot("HQ", PrimitiveType.Cube, new Vector3(4f, 3f, 4f), playerBlueMat);
                var building = EnsureComponent<Building>(root);
                var health   = EnsureComponent<Health>(root);
                EnsureNavMeshObstacle(root, new Vector3(4f, 3f, 4f));

                if (hqData != null)
                {
                    var so = new SerializedObject(building);
                    so.FindProperty("_data").objectReferenceValue = hqData;
                    so.ApplyModifiedPropertiesWithoutUndo();

                    var hso = new SerializedObject(health);
                    hso.FindProperty("_maxHp").floatValue = hqData.MaxHp;
                    hso.ApplyModifiedPropertiesWithoutUndo();
                }

                SavePrefab(root, HQPrefabPath);
            }

            // --- Barracks (бокс 3×2×3) ---
            {
                var root = CreateBuildingRoot("Barracks", PrimitiveType.Cube, new Vector3(3f, 2f, 3f), null);
                var building  = EnsureComponent<Building>(root);
                var health    = EnsureComponent<Health>(root);
                var prodBldg  = EnsureComponent<ProductionBuilding>(root);
                EnsureNavMeshObstacle(root, new Vector3(3f, 2f, 3f));

                if (barracksData != null)
                {
                    var so = new SerializedObject(building);
                    so.FindProperty("_data").objectReferenceValue = barracksData;
                    so.ApplyModifiedPropertiesWithoutUndo();

                    var hso = new SerializedObject(health);
                    hso.FindProperty("_maxHp").floatValue = barracksData.MaxHp;
                    hso.ApplyModifiedPropertiesWithoutUndo();
                }

                if (testUnitPrefab != null)
                {
                    var pso = new SerializedObject(prodBldg);
                    pso.FindProperty("_unitPrefab").objectReferenceValue = testUnitPrefab;
                    pso.ApplyModifiedPropertiesWithoutUndo();
                }
                else
                {
                    Debug.LogWarning("[Project Forge] TestUnit prefab не найден — unitPrefab у Barracks не проставлен.");
                }

                SavePrefab(root, BarracksPrefabPath);
            }

            // --- Extractor (цилиндр 2×1×2) ---
            {
                var root = CreateBuildingRoot("Extractor", PrimitiveType.Cylinder, new Vector3(2f, 1f, 2f), null);
                var building = EnsureComponent<Building>(root);
                var health   = EnsureComponent<Health>(root);

                if (extractorData != null)
                {
                    var so = new SerializedObject(building);
                    so.FindProperty("_data").objectReferenceValue = extractorData;
                    so.ApplyModifiedPropertiesWithoutUndo();

                    var hso = new SerializedObject(health);
                    hso.FindProperty("_maxHp").floatValue = extractorData.MaxHp;
                    hso.ApplyModifiedPropertiesWithoutUndo();
                }

                SavePrefab(root, ExtractorPrefabPath);
            }

            // --- ResourceNode (цилиндр жёлтый) ---
            {
                var root = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                root.name = "ResourceNode";
                root.transform.localScale = new Vector3(2f, 0.5f, 2f);

                if (crystalYellowMat != null)
                {
                    var mr = root.GetComponent<MeshRenderer>();
                    if (mr != null)
                        mr.sharedMaterial = crystalYellowMat;
                }

                EnsureComponent<ResourceNode>(root);

                // ResourceNode не является NavMeshObstacle
                SavePrefab(root, ResourceNodePrefabPath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Project Forge] Building-префабы (M5) созданы/обновлены.");
        }

        private static GameObject CreateBuildingRoot(
            string        name,
            PrimitiveType primitiveType,
            Vector3       scale,
            Material      mat)
        {
            var root = GameObject.CreatePrimitive(primitiveType);
            root.name = name;
            root.transform.localScale = scale;

            if (mat != null)
            {
                var mr = root.GetComponent<MeshRenderer>();
                if (mr != null)
                    mr.sharedMaterial = mat;
            }

            return root;
        }

        private static void EnsureNavMeshObstacle(GameObject go, Vector3 size)
        {
            var obs = EnsureComponent<NavMeshObstacle>(go);
            obs.carving = true;
            obs.size    = size;
            obs.center  = new Vector3(0f, size.y * 0.5f, 0f);
        }

        private static Material EnsureMaterial(string path, Color color)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
                return existing;

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            if (shader == null)
            {
                Debug.LogWarning($"[Project Forge] Шейдер не найден, материал не создан: {path}");
                return null;
            }

            var mat = new Material(shader);
            mat.color = color;
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);

            AssetDatabase.CreateAsset(mat, path);
            return mat;
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
