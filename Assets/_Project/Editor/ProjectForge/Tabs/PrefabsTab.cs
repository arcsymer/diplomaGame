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

        // v3 Tank пути
        private const string TankUnitPrefabPath      = "Assets/_Project/Prefabs/Units/TankUnit.prefab";
        private const string EnemyTankUnitPrefabPath = "Assets/_Project/Prefabs/Units/EnemyTankUnit.prefab";
        private const string WarFactoryPrefabPath    = "Assets/_Project/Prefabs/Buildings/WarFactory.prefab";
        private const string TankDataPath            = "Assets/_Project/Data/Units/Tank.asset";
        private const string EnemyTankDataPath       = "Assets/_Project/Data/Units/EnemyTank.asset";
        private const string WarFactoryDataPath      = "Assets/_Project/Data/Buildings/WarFactory.asset";
        private const string TankModelPath           = "Assets/_Project/Art/Models/Units/craft_speederA.fbx";
        private const string WarFactoryModelPath     = "Assets/_Project/Art/Models/Buildings/hangar_largeA.fbx";

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

            GUILayout.Space(8);

            EditorGUILayout.HelpBox(
                "v3 Tank:\n" +
                "• TankUnit.prefab      — craft_speederA.fbx, Tank.asset, NavMesh r=0.7\n" +
                "• EnemyTankUnit.prefab — EnemyRed, EnemyTank.asset\n" +
                "• WarFactory.prefab    — hangar_largeA.fbx, WarFactory.asset, unitPrefab=TankUnit\n" +
                "Также обновляет crowd-avoidance параметры TestUnit и EnemyUnit (r=0.45, HighQuality).",
                MessageType.Info);

            GUILayout.Space(4);

            if (GUILayout.Button("Create/Update Tank Prefabs (v3)", GUILayout.Height(32)))
                CreateOrUpdateTankPrefabs();

            GUILayout.Space(8);

            EditorGUILayout.HelpBox(
                "v5 Animated Units:\n" +
                "• Marine (TestUnit) → Mike.fbx, PlayerBlue материал\n" +
                "• EnemyGrunt (EnemyUnit) → George.fbx, EnemyRed материал\n" +
                "Создаёт AnimatorController, добавляет Animator + UnitAnimator.\n" +
                "Prerequisite: FBX скопированы в Assets/_Project/Art/Models/Units/Animated/.\n" +
                "Операция идемпотентна.",
                MessageType.Info);

            GUILayout.Space(4);

            if (GUILayout.Button("Setup Animated Units (v5)", GUILayout.Height(32)))
                AnimatedUnitsSetup.SetupAnimatedUnits();
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

            // --- NavMeshAgent (v3 crowd-avoidance tuning) ---
            var agent = EnsureComponent<NavMeshAgent>(root);
            agent.speed                  = 5f;
            agent.angularSpeed           = 360f;
            agent.acceleration           = 12f;
            agent.radius                 = 0.45f;
            agent.obstacleAvoidanceType  = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            agent.avoidancePriority      = 50;
            agent.stoppingDistance       = 0.5f;

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

            // --- NavMeshAgent (v3 crowd-avoidance tuning) ---
            var agent = EnsureComponent<NavMeshAgent>(root);
            agent.speed                  = 4.5f;
            agent.angularSpeed           = 360f;
            agent.acceleration           = 12f;
            agent.radius                 = 0.45f;
            agent.obstacleAvoidanceType  = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
            agent.avoidancePriority      = 50;
            agent.stoppingDistance       = 0.5f;

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

        // ----------------------------------------------------------------
        // v3: Tank Prefabs
        // ----------------------------------------------------------------

        /// <summary>
        /// Создаёт или обновляет TankUnit.prefab, EnemyTankUnit.prefab, WarFactory.prefab.
        /// Идемпотентно.
        /// </summary>
        internal static void CreateOrUpdateTankPrefabs()
        {
            EnsureFolder("Assets/_Project/Prefabs/Units");
            EnsureFolder("Assets/_Project/Prefabs/Buildings");

            var tankData      = AssetDatabase.LoadAssetAtPath<UnitData>(TankDataPath);
            var enemyTankData = AssetDatabase.LoadAssetAtPath<UnitData>(EnemyTankDataPath);
            var warFactoryData = AssetDatabase.LoadAssetAtPath<BuildingData>(WarFactoryDataPath);
            var enemyMat      = EnsureEnemyMaterial();
            var tankModel     = AssetDatabase.LoadAssetAtPath<GameObject>(TankModelPath);

            if (tankData == null || enemyTankData == null)
                Debug.LogWarning("[Project Forge] Tank.asset / EnemyTank.asset не найдены — сначала 'Create/Update Tank Data (v3)'.");

            // ---- TankUnit.prefab ----
            {
                var root = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                root.name = "TankUnit";
                root.transform.localScale = new Vector3(1.4f, 0.7f, 1.4f); // низкий профиль

                // Скрываем дефолтную геометрию капсулы; Visual добавляет VFXTab позже
                var capsuleMr = root.GetComponent<MeshRenderer>();
                if (capsuleMr != null)
                    Object.DestroyImmediate(capsuleMr);

                // Visual child из FBX
                if (tankModel != null)
                {
                    var visual = (GameObject)PrefabUtility.InstantiatePrefab(tankModel, root.transform);
                    visual.name = "Visual";
                    visual.transform.localPosition = Vector3.zero;
                    visual.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                    // Нормализуем под «tank height» ~1.2 м в мировых единицах
                    // (капсула масштабирована * 0.7 по Y → half-height ~0.7 → low tank)
                    var renderers = visual.GetComponentsInChildren<Renderer>(true);
                    if (renderers.Length > 0)
                    {
                        var b = renderers[0].bounds;
                        for (int ri = 1; ri < renderers.Length; ri++) b.Encapsulate(renderers[ri].bounds);
                        float currentH = b.size.y;
                        if (currentH > 0.0001f)
                        {
                            float factor = 1.2f / currentH;
                            visual.transform.localScale = Vector3.one * factor;
                        }
                    }
                }
                else
                {
                    Debug.LogWarning($"[Project Forge] {TankModelPath} не найден — TankUnit получит только примитив.");
                }

                // NavMeshAgent — Tank v3 параметры
                var agent = EnsureComponent<NavMeshAgent>(root);
                agent.speed                 = 3f;
                agent.angularSpeed          = 240f;
                agent.acceleration          = 8f;
                agent.radius                = 0.7f;
                agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
                agent.avoidancePriority     = 30;
                agent.stoppingDistance      = 1.0f;

                // Unit (Player)
                var unit   = EnsureComponent<Unit>(root);
                var unitSo = new SerializedObject(unit);
                unitSo.FindProperty("_faction").enumValueIndex = (int)Faction.Player;
                unitSo.ApplyModifiedPropertiesWithoutUndo();

                // Health
                EnsureComponent<Health>(root);

                // UnitCombat
                var combat   = EnsureComponent<UnitCombat>(root);
                if (tankData != null)
                {
                    var combatSo = new SerializedObject(combat);
                    combatSo.FindProperty("_data").objectReferenceValue = tankData;
                    combatSo.ApplyModifiedPropertiesWithoutUndo();
                }

                // SelectionRing
                var ring = EnsureSelectionRing(root);
                ring.SetActive(false);

                SavePrefab(root, TankUnitPrefabPath);
            }

            // ---- EnemyTankUnit.prefab ----
            {
                var root = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                root.name = "EnemyTankUnit";
                root.transform.localScale = new Vector3(1.4f, 0.7f, 1.4f);

                // Красный материал
                if (enemyMat != null)
                {
                    var mr = root.GetComponent<MeshRenderer>();
                    if (mr != null) mr.sharedMaterial = enemyMat;
                }
                else
                {
                    var mr = root.GetComponent<MeshRenderer>();
                    if (mr != null) Object.DestroyImmediate(mr);
                }

                // Visual из FBX с Enemy-тинтом
                if (tankModel != null)
                {
                    var visual = (GameObject)PrefabUtility.InstantiatePrefab(tankModel, root.transform);
                    visual.name = "Visual";
                    visual.transform.localPosition = Vector3.zero;
                    visual.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                    var renderers = visual.GetComponentsInChildren<Renderer>(true);
                    if (renderers.Length > 0)
                    {
                        var b = renderers[0].bounds;
                        for (int ri = 1; ri < renderers.Length; ri++) b.Encapsulate(renderers[ri].bounds);
                        float currentH = b.size.y;
                        if (currentH > 0.0001f)
                        {
                            float factor = 1.2f / currentH;
                            visual.transform.localScale = Vector3.one * factor;
                        }
                    }
                    if (enemyMat != null)
                    {
                        foreach (var mr in visual.GetComponentsInChildren<MeshRenderer>(true))
                            mr.sharedMaterial = enemyMat;
                    }
                }

                var agent = EnsureComponent<NavMeshAgent>(root);
                agent.speed                 = 3f;
                agent.angularSpeed          = 240f;
                agent.acceleration          = 8f;
                agent.radius                = 0.7f;
                agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
                agent.avoidancePriority     = 30;
                agent.stoppingDistance      = 1.0f;

                var unit   = EnsureComponent<Unit>(root);
                var unitSo = new SerializedObject(unit);
                unitSo.FindProperty("_faction").enumValueIndex = (int)Faction.Enemy;
                unitSo.ApplyModifiedPropertiesWithoutUndo();

                EnsureComponent<Health>(root);

                var combat   = EnsureComponent<UnitCombat>(root);
                if (enemyTankData != null)
                {
                    var combatSo = new SerializedObject(combat);
                    combatSo.FindProperty("_data").objectReferenceValue = enemyTankData;
                    combatSo.ApplyModifiedPropertiesWithoutUndo();
                }

                var ring = EnsureSelectionRing(root);
                ring.SetActive(false);

                SavePrefab(root, EnemyTankUnitPrefabPath);
            }

            // ---- WarFactory.prefab ----
            {
                var tankUnitPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TankUnitPrefabPath);

                var root = CreateBuildingRoot("WarFactory", PrimitiveType.Cube, new Vector3(5f, 3f, 5f), null);
                var building  = EnsureComponent<Building>(root);
                var health    = EnsureComponent<Health>(root);
                var prodBldg  = EnsureComponent<ProductionBuilding>(root);
                EnsureNavMeshObstacle(root, new Vector3(5f, 3f, 5f));

                if (warFactoryData != null)
                {
                    var so = new SerializedObject(building);
                    so.FindProperty("_data").objectReferenceValue = warFactoryData;
                    so.ApplyModifiedPropertiesWithoutUndo();

                    var hso = new SerializedObject(health);
                    hso.FindProperty("_maxHp").floatValue = warFactoryData.MaxHp;
                    hso.ApplyModifiedPropertiesWithoutUndo();
                }

                if (tankUnitPrefab != null)
                {
                    var pso = new SerializedObject(prodBldg);
                    pso.FindProperty("_unitPrefab").objectReferenceValue = tankUnitPrefab;
                    pso.ApplyModifiedPropertiesWithoutUndo();
                }
                else
                {
                    Debug.LogWarning("[Project Forge] TankUnit.prefab не найден для WarFactory._unitPrefab — префаб ещё не сохранён?");
                }

                // Применяем VFX-визуал здания, если модель доступна
                var wfModel = AssetDatabase.LoadAssetAtPath<GameObject>(WarFactoryModelPath);
                if (wfModel != null)
                {
                    // Скрыть куб-рендерер; VFX-чайлд добавляется как Visual
                    var mr = root.GetComponent<MeshRenderer>();
                    if (mr != null) mr.enabled = false;

                    var visual = (GameObject)PrefabUtility.InstantiatePrefab(wfModel, root.transform);
                    visual.name = "Visual";
                    visual.transform.localPosition = Vector3.zero;
                    visual.transform.localRotation = Quaternion.identity;
                    var renderers = visual.GetComponentsInChildren<Renderer>(true);
                    if (renderers.Length > 0)
                    {
                        var b = renderers[0].bounds;
                        for (int ri = 1; ri < renderers.Length; ri++) b.Encapsulate(renderers[ri].bounds);
                        float currentFp = Mathf.Max(b.size.x, b.size.z);
                        if (currentFp > 0.0001f)
                        {
                            float factor = 4.5f / currentFp; // footprint ~4.5 м
                            visual.transform.localScale = Vector3.one * factor;
                        }
                    }
                }

                SavePrefab(root, WarFactoryPrefabPath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Project Forge] Tank Prefabs (v3) созданы/обновлены.");
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
