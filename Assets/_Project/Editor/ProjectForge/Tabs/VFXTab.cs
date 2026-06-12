using System.Collections.Generic;
using DiplomaGame.Runtime.VFX;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DiplomaGame.Editor
{
    /// <summary>
    /// Вкладка VFX — визуальная доводка M8.
    /// Четыре операции: импорт моделей, применение визуала к префабам,
    /// создание VFX-префабов, настройка освещения и постобработки.
    /// </summary>
    internal sealed class VFXTab : IForgeTab
    {
        public string Title => "VFX";

        public void OnGUI()
        {
            GUILayout.Space(8);
            GUILayout.Label("Визуал M8", EditorStyles.boldLabel);
            GUILayout.Space(4);

            EditorGUILayout.HelpBox(
                "Настраивает параметры импорта FBX-моделей и конвертирует материалы в URP.\n" +
                "Идемпотентно.",
                MessageType.Info);

            if (GUILayout.Button("Fix Model Import & Materials (M8)", GUILayout.Height(32)))
                FixModelImports();

            GUILayout.Space(8);

            EditorGUILayout.HelpBox(
                "Обновляет префабы юнитов и зданий: заменяет капсулы/боксы на 3D-модели,\n" +
                "расставляет декор в сцене.",
                MessageType.Info);

            if (GUILayout.Button("Apply Visuals (M8)", GUILayout.Height(32)))
                ApplyVisuals();

            GUILayout.Space(8);

            EditorGUILayout.HelpBox(
                "Создаёт VFX-префабы (частицы) в Assets/_Project/Prefabs/VFX/.",
                MessageType.Info);

            if (GUILayout.Button("Build VFX Prefabs (M8)", GUILayout.Height(32)))
                BuildVfxPrefabs();

            GUILayout.Space(8);

            EditorGUILayout.HelpBox(
                "Настраивает освещение, скайбокс, материал земли и постобработку в открытой сцене.",
                MessageType.Info);

            if (GUILayout.Button("Setup Lighting & Post (M8)", GUILayout.Height(32)))
                SetupLightingAndPost();
        }

        // ================================================================
        // 1. Fix Model Import & Materials
        // ================================================================

        internal static void FixModelImports()
        {
            const string modelsRoot = "Assets/_Project/Art/Models";
            EnsureFolder("Assets/_Project/Art/Materials/Extracted");

            // Находим все FBX
            var guids = AssetDatabase.FindAssets("t:Model", new[] { modelsRoot });
            int count = 0;

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase))
                    continue;

                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null)
                    continue;

                bool changed = false;

                if (importer.importCameras != false) { importer.importCameras = false; changed = true; }
                if (importer.importLights  != false) { importer.importLights  = false; changed = true; }
                // globalScale: Kenney-модели уже в правильном масштабе (1 ед. ≈ 1 м)
                if (!Mathf.Approximately(importer.globalScale, 1f))
                {
                    importer.globalScale = 1f;
                    changed = true;
                }

                if (changed)
                    importer.SaveAndReimport();
            }

            // Конвертируем материалы в URP
            ConvertMaterialsToUrp(modelsRoot);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Project Forge] Fix Model Import & Materials (M8) завершён. FBX обработано: {count}.");
        }

        private static void ConvertMaterialsToUrp(string modelsRoot)
        {
            // Пробуем встроенный конвертер URP (публичный API)
            bool converterUsed = TryRunUrpConverter();
            if (converterUsed)
                return;

            // Fallback: ручная конвертация — для каждого материала в папке Extracted
            // и во всех суб-папках моделей
            var matGuids = AssetDatabase.FindAssets("t:Material", new[]
            {
                modelsRoot,
                "Assets/_Project/Art/Materials/Extracted"
            });

            var urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLitShader == null)
            {
                Debug.LogWarning("[Project Forge] URP Lit шейдер не найден — конвертация материалов пропущена.");
                return;
            }

            foreach (var guid in matGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var mat  = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null)
                    continue;

                // Уже URP — пропускаем
                if (mat.shader != null && mat.shader.name.Contains("Universal Render Pipeline"))
                    continue;

                // Переносим базовый цвет и текстуру
                Color baseColor = Color.white;
                Texture mainTex = null;

                if (mat.HasProperty("_Color"))
                    baseColor = mat.GetColor("_Color");
                if (mat.HasProperty("_MainTex"))
                    mainTex = mat.GetTexture("_MainTex");

                mat.shader = urpLitShader;

                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", baseColor);
                if (mat.HasProperty("_BaseMap") && mainTex != null)
                    mat.SetTexture("_BaseMap", mainTex);

                EditorUtility.SetDirty(mat);
            }

            AssetDatabase.SaveAssets();
        }

        private static bool TryRunUrpConverter()
        {
            // Пытаемся вызвать официальный batch-конвертер из URP через рефлексию
            try
            {
                var converterType = System.Type.GetType(
                    "UnityEditor.Rendering.Universal.URPConverterUtility, Unity.RenderPipelines.Universal.Editor");
                if (converterType == null)
                    return false;

                var method = converterType.GetMethod("UpgradeProjectMaterials",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                if (method == null)
                    return false;

                method.Invoke(null, null);
                Debug.Log("[Project Forge] URP Material Converter запущен успешно.");
                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[Project Forge] URP batch-конвертер недоступен: {ex.Message}. Используется ручной fallback.");
                return false;
            }
        }

        // ================================================================
        // 2. Apply Visuals
        // ================================================================

        internal static void ApplyVisuals()
        {
            EnsureFolder("Assets/_Project/Art/Materials");

            // --- Материалы юнитов ---
            var unitBlueMat = EnsureUnitMaterial(
                "Assets/_Project/Art/Materials/UnitBlue.mat",
                "Assets/_Project/Art/Textures/Models/texture-a.png",
                new Color(0.7f, 0.8f, 1.0f, 1f));

            var unitRedMat = EnsureUnitMaterial(
                "Assets/_Project/Art/Materials/UnitRed.mat",
                "Assets/_Project/Art/Textures/Models/texture-b.png",
                new Color(1.0f, 0.75f, 0.75f, 1f));

            // --- Материал кристалла ---
            var crystalMat = EnsureCrystalMaterial(
                "Assets/_Project/Art/Materials/CrystalEmerald.mat");

            // --- Префабы юнитов ---
            ApplyUnitVisual(
                "Assets/_Project/Prefabs/Units/TestUnit.prefab",
                "Assets/_Project/Art/Models/Units/character-a.fbx",
                unitBlueMat);

            ApplyUnitVisual(
                "Assets/_Project/Prefabs/Units/EnemyUnit.prefab",
                "Assets/_Project/Art/Models/Units/character-b.fbx",
                unitRedMat);

            // --- Префабы зданий (целевой footprint = max(size.x, size.z)) ---
            ApplyBuildingVisual(
                "Assets/_Project/Prefabs/Buildings/HQ.prefab",
                "Assets/_Project/Art/Models/Buildings/hangar_largeA.fbx",
                targetFootprint: 5f);

            ApplyBuildingVisual(
                "Assets/_Project/Prefabs/Buildings/Barracks.prefab",
                "Assets/_Project/Art/Models/Buildings/hangar_smallA.fbx",
                targetFootprint: 3.5f);

            ApplyBuildingVisual(
                "Assets/_Project/Prefabs/Buildings/Extractor.prefab",
                "Assets/_Project/Art/Models/Buildings/hangar_roundA.fbx",
                targetFootprint: 2.5f);

            // --- ResourceNode ---
            ApplyResourceNodeVisual(
                "Assets/_Project/Prefabs/Props/ResourceNode.prefab",
                "Assets/_Project/Art/Models/Props/rock_crystalsLargeA.fbx",
                crystalMat);

            // --- Hero в сцене ---
            ApplyHeroVisualInScene();

            // --- Декор сцены ---
            PlaceSceneDecor();

            // --- VfxManager на GameManagers ---
            SetupVfxManagerInScene();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }

            Debug.Log("[Project Forge] Apply Visuals (M8) выполнен.");
        }

        private static void ApplyUnitVisual(string prefabPath, string modelPath, Material tintMat)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[Project Forge] Префаб не найден: {prefabPath}");
                return;
            }

            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (modelAsset == null)
            {
                Debug.LogWarning($"[Project Forge] Модель не найдена: {modelPath}");
                return;
            }

            using (var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
            {
                var root = scope.prefabContentsRoot;

                // Скрыть MeshRenderer капсулы корня, коллайдер оставить
                var capsuleMr = root.GetComponent<MeshRenderer>();
                if (capsuleMr != null)
                    capsuleMr.enabled = false;

                // Создать или найти child "Visual"
                var visualTf = root.transform.Find("Visual");
                GameObject visual;
                if (visualTf == null)
                {
                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset, root.transform);
                    inst.name = "Visual";
                    visual = inst;
                }
                else
                {
                    visual = visualTf.gameObject;
                }

                // Нормализация: сбрасываем, потом масштабируем под высоту 1.8
                // (капсула height=2, центр=0 → низ на y=-1; ноги Visual тоже на y=-1)
                visual.transform.localScale    = Vector3.one;
                visual.transform.localPosition = Vector3.zero;
                // У Kenney-персонажей forward = -Z: разворачиваем по ходу движения
                visual.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                NormalizeVisualByHeight(visual, targetHeight: 1.8f);
                // Сдвигаем низ баундов на y=-1 (низ капсулы)
                AlignVisualBottom(visual, bottomY: -1f);

                // Применить тинт-материал на все рендеры Visual
                if (tintMat != null)
                {
                    foreach (var mr in visual.GetComponentsInChildren<MeshRenderer>(true))
                        mr.sharedMaterial = tintMat;
                }
            }
        }

        private static void ApplyBuildingVisual(string prefabPath, string modelPath, float targetFootprint)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[Project Forge] Префаб не найден: {prefabPath}");
                return;
            }

            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (modelAsset == null)
            {
                Debug.LogWarning($"[Project Forge] Модель не найдена: {modelPath}");
                return;
            }

            using (var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
            {
                var root = scope.prefabContentsRoot;

                // Скрыть корневой MeshRenderer (бокс/цилиндр)
                var mr = root.GetComponent<MeshRenderer>();
                if (mr != null)
                    mr.enabled = false;

                // Visual child
                var visualTf = root.transform.Find("Visual");
                GameObject visual;
                if (visualTf == null)
                {
                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset, root.transform);
                    inst.name = "Visual";
                    visual = inst;
                }
                else
                {
                    visual = visualTf.gameObject;
                }

                // Нормализация: сбрасываем, масштабируем по max(size.x, size.z) = targetFootprint
                visual.transform.localScale    = Vector3.one;
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                NormalizeVisualByFootprint(visual, targetFootprint);
                // Низ здания на y=0 относительно корня префаба
                AlignVisualBottom(visual, bottomY: 0f);
            }
        }

        private static void ApplyResourceNodeVisual(string prefabPath, string modelPath, Material crystalMat)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[Project Forge] Префаб не найден: {prefabPath}");
                return;
            }

            var modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (modelAsset == null)
            {
                Debug.LogWarning($"[Project Forge] Модель не найдена: {modelPath}");
                return;
            }

            using (var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
            {
                var root = scope.prefabContentsRoot;

                // Скрыть цилиндр
                var mr = root.GetComponent<MeshRenderer>();
                if (mr != null)
                    mr.enabled = false;

                var visualTf = root.transform.Find("Visual");
                GameObject visual;
                if (visualTf == null)
                {
                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(modelAsset, root.transform);
                    inst.name = "Visual";
                    visual = inst;
                }
                else
                {
                    visual = visualTf.gameObject;
                }

                // Нормализация: высота 1.5, низ на y=0
                visual.transform.localScale    = Vector3.one;
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                NormalizeVisualByHeight(visual, targetHeight: 1.5f);
                AlignVisualBottom(visual, bottomY: 0f);

                if (crystalMat != null)
                {
                    foreach (var meshMr in visual.GetComponentsInChildren<MeshRenderer>(true))
                        meshMr.sharedMaterial = crystalMat;
                }
            }
        }

        private static void ApplyHeroVisualInScene()
        {
            var heroGo = GameObject.Find("Hero");
            if (heroGo == null)
            {
                Debug.LogWarning("[Project Forge] Hero не найден в сцене — пропуск ApplyHeroVisualInScene.");
                return;
            }

            // Дефект 3: скрыть MeshRenderer капсулы героя (коллайдер/CharacterController не трогаем)
            var heroMr = heroGo.GetComponent<MeshRenderer>();
            if (heroMr != null)
                heroMr.enabled = false;

            var characterModel = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Art/Models/Units/character-h.fbx");
            if (characterModel == null)
            {
                Debug.LogWarning("[Project Forge] character-h.fbx не найден.");
                return;
            }

            // Visual child
            var visualTf = heroGo.transform.Find("Visual");
            GameObject visual;
            if (visualTf == null)
            {
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(characterModel);
                inst.transform.SetParent(heroGo.transform, false);
                inst.name = "Visual";
                visual    = inst;
            }
            else
            {
                visual = visualTf.gameObject;
            }

            // Нормализация: сбрасываем, масштабируем под высоту 1.8, ноги на y=-1
            visual.transform.localScale    = Vector3.one;
            visual.transform.localPosition = Vector3.zero;
            // У Kenney-персонажей forward = -Z: герой должен смотреть от камеры
            visual.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            NormalizeVisualByHeight(visual, targetHeight: 1.8f);
            AlignVisualBottom(visual, bottomY: -1f);

            // Бластер в правой руке
            var blasterModel = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Art/Models/Weapons/blaster-a.fbx");
            if (blasterModel != null)
            {
                var weaponSlot = visual.transform.Find("Blaster");
                GameObject blasterInst;
                if (weaponSlot == null)
                {
                    blasterInst = (GameObject)PrefabUtility.InstantiatePrefab(blasterModel);
                    blasterInst.transform.SetParent(visual.transform, false);
                    blasterInst.name = "Blaster";
                }
                else
                {
                    blasterInst = weaponSlot.gameObject;
                }

                // Нормализация бластера по длине (size.z) = 0.6
                blasterInst.transform.localScale    = Vector3.one;
                blasterInst.transform.localPosition = Vector3.zero;
                blasterInst.transform.localRotation = Quaternion.identity;
                NormalizeVisualByDepth(blasterInst, targetDepth: 0.6f);
                blasterInst.transform.localPosition = new Vector3(0.35f, 0.4f, 0.25f);
                blasterInst.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            }
        }

        private static void PlaceSceneDecor()
        {
            // Декор: фиксированные позиции вне баз.
            // targetHeight: целевая высота для нормализации (камни крупные = 2, остальные = 1).
            var decorItems = new[]
            {
                (name: "Deco_Rock_01",          path: "Assets/_Project/Art/Models/Props/rock.fbx",             pos: new Vector3(-15f, 0f, 10f),  targetH: 1f),
                (name: "Deco_Rock_02",          path: "Assets/_Project/Art/Models/Props/rock_largeA.fbx",      pos: new Vector3( 12f, 0f, -8f),  targetH: 2f),
                (name: "Deco_Rock_03",          path: "Assets/_Project/Art/Models/Props/rock.fbx",             pos: new Vector3(  8f, 0f,  15f), targetH: 1f),
                (name: "Deco_Crater_01",        path: "Assets/_Project/Art/Models/Props/crater.fbx",           pos: new Vector3(-10f, 0f, -12f), targetH: 1f),
                (name: "Deco_Crater_02",        path: "Assets/_Project/Art/Models/Props/crater.fbx",           pos: new Vector3( 18f, 0f,  8f),  targetH: 1f),
                (name: "Deco_Barrel_01",        path: "Assets/_Project/Art/Models/Props/barrel.fbx",           pos: new Vector3( -5f, 0f,  20f), targetH: 1f),
                (name: "Deco_Barrel_02",        path: "Assets/_Project/Art/Models/Props/barrel.fbx",           pos: new Vector3(  3f, 0f, -18f), targetH: 1f),
                (name: "Deco_Crystal_01",       path: "Assets/_Project/Art/Models/Props/rock_crystals.fbx",    pos: new Vector3(-20f, 0f,  5f),  targetH: 1.5f),
                (name: "Deco_MachineBarrel_01", path: "Assets/_Project/Art/Models/Props/machine_barrel.fbx",   pos: new Vector3( 14f, 0f, -15f), targetH: 1f),
            };

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.IsValid()) return;

            foreach (var item in decorItems)
            {
                // Идемпотентно: существующий объект не создаём заново,
                // но перенормализуем масштаб (декор мог быть создан до фикса масштабов)
                var existing = GameObject.Find(item.name);
                if (existing != null)
                {
                    existing.transform.localScale = Vector3.one;
                    NormalizeVisualByHeight(existing, item.targetH);
                    AlignVisualBottom(existing, bottomY: 0f);
                    var epos = existing.transform.position;
                    existing.transform.position = new Vector3(item.pos.x, epos.y, item.pos.z);
                    continue;
                }

                var model = AssetDatabase.LoadAssetAtPath<GameObject>(item.path);
                if (model == null)
                {
                    Debug.LogWarning($"[Project Forge] Декор-модель не найдена: {item.path}");
                    continue;
                }

                var go = (GameObject)PrefabUtility.InstantiatePrefab(model, scene);
                go.name               = item.name;
                go.transform.rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

                // Нормализация масштаба
                go.transform.localScale = Vector3.one;
                NormalizeVisualByHeight(go, item.targetH);
                // Низ объекта на y=0 (на уровне земли)
                AlignVisualBottom(go, bottomY: 0f);
                // Применяем горизонтальную позицию после выравнивания
                var pos = go.transform.position;
                go.transform.position = new Vector3(item.pos.x, pos.y, item.pos.z);
            }
        }

        private static void SetupVfxManagerInScene()
        {
            var managersGo = GameObject.Find("GameManagers");
            if (managersGo == null)
            {
                Debug.LogWarning("[Project Forge] GameManagers не найден — VfxManager не добавлен.");
                return;
            }

            var vfxMgr = managersGo.GetComponent<VfxManager>();
            if (vfxMgr == null)
                vfxMgr = managersGo.AddComponent<VfxManager>();

            // Проставляем ссылки на VFX-префабы
            var so = new SerializedObject(vfxMgr);

            SetPrefabRef(so, "_muzzleFlashPrefab",  "Assets/_Project/Prefabs/VFX/MuzzleFlash.prefab");
            SetPrefabRef(so, "_hitImpactPrefab",    "Assets/_Project/Prefabs/VFX/HitImpact.prefab");
            SetPrefabRef(so, "_explosionPrefab",    "Assets/_Project/Prefabs/VFX/Explosion.prefab");
            SetPrefabRef(so, "_buildEffectPrefab",  "Assets/_Project/Prefabs/VFX/BuildEffect.prefab");

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetPrefabRef(SerializedObject so, string propName, string path)
        {
            var prop = so.FindProperty(propName);
            if (prop == null) return;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            prop.objectReferenceValue = prefab;
        }

        // ================================================================
        // 3. Build VFX Prefabs
        // ================================================================

        internal static void BuildVfxPrefabs()
        {
            EnsureFolder("Assets/_Project/Prefabs/VFX");
            EnsureFolder("Assets/_Project/Art/Materials/VFX");

            // Создаём материалы для частиц
            var particleFlame  = EnsureParticleMaterial("ParticleFlame",  "Assets/_Project/Art/Textures/Particles/flame_01.png",  additive: true);
            var particleSpark  = EnsureParticleMaterial("ParticleSpark",  "Assets/_Project/Art/Textures/Particles/spark_04.png",  additive: true);
            var particleSmoke  = EnsureParticleMaterial("ParticleSmoke",  "Assets/_Project/Art/Textures/Particles/smoke_01.png",  additive: false);
            var particleGlow   = EnsureParticleMaterial("ParticleGlow",   "Assets/_Project/Art/Textures/Particles/light_01.png",  additive: true);
            var particleTwirl  = EnsureParticleMaterial("ParticleTwirl",  "Assets/_Project/Art/Textures/Particles/twirl_01.png",  additive: true);

            // Создаём VFX-префабы
            BuildMuzzleFlashPrefab(particleSpark, particleGlow);
            BuildHitImpactPrefab(particleSpark, particleGlow);
            BuildExplosionPrefab(particleFlame, particleSmoke, particleGlow);
            BuildBuildEffectPrefab(particleTwirl, particleGlow);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Project Forge] Build VFX Prefabs (M8) выполнен.");
        }

        private static void BuildMuzzleFlashPrefab(Material sparkMat, Material glowMat)
        {
            const string path = "Assets/_Project/Prefabs/VFX/MuzzleFlash.prefab";

            var root = new GameObject("MuzzleFlash");

            // Искры
            var sparksGo = new GameObject("Sparks");
            sparksGo.transform.SetParent(root.transform, false);
            var sparkPs = sparksGo.AddComponent<ParticleSystem>();
            ConfigureBurstPs(sparkPs, sparkMat,
                burstCount:   6,
                lifetime:     0.15f,
                startSize:    0.3f,
                startColor:   new Color(1f, 0.8f, 0.2f),
                speed:        3f);

            // Glow
            var glowGo = new GameObject("Glow");
            glowGo.transform.SetParent(root.transform, false);
            var glowPs = glowGo.AddComponent<ParticleSystem>();
            ConfigureBurstPs(glowPs, glowMat,
                burstCount:   2,
                lifetime:     0.1f,
                startSize:    0.5f,
                startColor:   new Color(1f, 0.9f, 0.3f),
                speed:        0.5f);

            SaveVfxPrefab(root, path);
        }

        private static void BuildHitImpactPrefab(Material sparkMat, Material glowMat)
        {
            const string path = "Assets/_Project/Prefabs/VFX/HitImpact.prefab";

            var root = new GameObject("HitImpact");

            var sparksGo = new GameObject("Sparks");
            sparksGo.transform.SetParent(root.transform, false);
            var sparkPs = sparksGo.AddComponent<ParticleSystem>();
            ConfigureBurstPs(sparkPs, sparkMat,
                burstCount:   8,
                lifetime:     0.3f,
                startSize:    0.25f,
                startColor:   new Color(1f, 0.7f, 0.2f),
                speed:        4f);

            var glowGo = new GameObject("Flash");
            glowGo.transform.SetParent(root.transform, false);
            var glowPs = glowGo.AddComponent<ParticleSystem>();
            ConfigureBurstPs(glowPs, glowMat,
                burstCount:   1,
                lifetime:     0.2f,
                startSize:    0.6f,
                startColor:   new Color(1f, 0.95f, 0.5f),
                speed:        0.1f);

            SaveVfxPrefab(root, path);
        }

        private static void BuildExplosionPrefab(Material flameMat, Material smokeMat, Material glowMat)
        {
            const string path = "Assets/_Project/Prefabs/VFX/Explosion.prefab";

            var root = new GameObject("Explosion");

            // Огонь
            var flameGo = new GameObject("Flame");
            flameGo.transform.SetParent(root.transform, false);
            var flamePs = flameGo.AddComponent<ParticleSystem>();
            ConfigureBurstPs(flamePs, flameMat,
                burstCount:   12,
                lifetime:     0.6f,
                startSize:    1.8f,
                startColor:   new Color(1f, 0.45f, 0.1f),
                speed:        3f);

            // Дым
            var smokeGo = new GameObject("Smoke");
            smokeGo.transform.SetParent(root.transform, false);
            var smokePs = smokeGo.AddComponent<ParticleSystem>();
            ConfigureBurstPs(smokePs, smokeMat,
                burstCount:   6,
                lifetime:     1.5f,
                startSize:    1.0f,
                startColor:   new Color(0.5f, 0.5f, 0.5f, 0.8f),
                speed:        1f);

            // Настраиваем размер дыма — растёт со временем
            var smokeSizeOverLifetime = smokePs.sizeOverLifetime;
            smokeSizeOverLifetime.enabled = true;
            var smokeSizeCurve = new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(1f, 2.5f));
            smokeSizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, smokeSizeCurve);

            // Вспышка
            var glowGo = new GameObject("Flash");
            glowGo.transform.SetParent(root.transform, false);
            var glowPs = glowGo.AddComponent<ParticleSystem>();
            ConfigureBurstPs(glowPs, glowMat,
                burstCount:   1,
                lifetime:     0.25f,
                startSize:    2f,
                startColor:   new Color(1f, 0.9f, 0.6f),
                speed:        0.1f);

            SaveVfxPrefab(root, path);
        }

        private static void BuildBuildEffectPrefab(Material twirlMat, Material glowMat)
        {
            const string path = "Assets/_Project/Prefabs/VFX/BuildEffect.prefab";

            var root = new GameObject("BuildEffect");

            // Twirl
            var twirlGo = new GameObject("Twirl");
            twirlGo.transform.SetParent(root.transform, false);
            var twirlPs = twirlGo.AddComponent<ParticleSystem>();
            ConfigureBurstPs(twirlPs, twirlMat,
                burstCount:   4,
                lifetime:     1.0f,
                startSize:    0.8f,
                startColor:   new Color(0.2f, 1f, 0.9f),
                speed:        2f);

            // Поднимающиеся glow-частицы
            var glowGo = new GameObject("Glow");
            glowGo.transform.SetParent(root.transform, false);
            var glowPs = glowGo.AddComponent<ParticleSystem>();
            ConfigureBurstPs(glowPs, glowMat,
                burstCount:   8,
                lifetime:     1.0f,
                startSize:    0.4f,
                startColor:   new Color(0.3f, 0.9f, 1f),
                speed:        2f);

            // Гравитация вверх для glow
            var glowMain = glowPs.main;
            glowMain.gravityModifier = -0.5f;

            SaveVfxPrefab(root, path);
        }

        // ----------------------------------------------------------------
        // Вспомогательные методы частиц
        // ----------------------------------------------------------------

        private static void ConfigureBurstPs(
            ParticleSystem ps,
            Material mat,
            int   burstCount,
            float lifetime,
            float startSize,
            Color startColor,
            float speed)
        {
            // main
            var main = ps.main;
            main.loop              = false;
            main.playOnAwake       = false;
            main.stopAction        = ParticleSystemStopAction.None;
            main.startLifetime     = lifetime;
            main.startSize         = startSize;
            main.startColor        = startColor;
            main.startSpeed        = speed;
            main.maxParticles      = burstCount * 2;

            // emission — только burst
            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)burstCount) });

            // renderer
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (renderer != null && mat != null)
            {
                renderer.material     = mat;
                renderer.renderMode   = ParticleSystemRenderMode.Billboard;
            }

            // fade out over lifetime
            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(startColor, 0f), new GradientColorKey(startColor, 0.7f) },
                new[] { new GradientAlphaKey(1f, 0f),         new GradientAlphaKey(0f, 1f) });
            colorOverLifetime.color = gradient;
        }

        private static void SaveVfxPrefab(GameObject root, string path)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);

            if (prefab != null)
                Debug.Log($"[Project Forge] VFX-префаб сохранён: {path}");
            else
                Debug.LogError($"[Project Forge] Не удалось сохранить VFX-префаб: {path}");
        }

        // ================================================================
        // 4. Setup Lighting & Post
        // ================================================================

        internal static void SetupLightingAndPost()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                EditorUtility.DisplayDialog("Project Forge", "Нет открытой сцены.", "OK");
                return;
            }

            EnsureFolder("Assets/_Project/Settings");
            EnsureFolder("Assets/_Project/Art/Materials");

            // --- Скайбокс ---
            SetupSkybox();

            // --- Солнце ---
            SetupSun();

            // --- Ambient освещение ---
            SetupAmbient();

            // --- Материал земли ---
            SetupGroundMaterial();

            // --- PostFX Volume ---
            SetupPostFxVolume();

            // --- Камера: HDR + Post ---
            SetupMainCamera();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Project Forge] Setup Lighting & Post (M8) выполнен.");
        }

        private static void SetupSkybox()
        {
            const string skyboxMatPath = "Assets/_Project/Art/Materials/SpaceSky.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(skyboxMatPath);
            if (mat == null)
            {
                var skyShader = Shader.Find("Skybox/Procedural");
                if (skyShader == null)
                {
                    Debug.LogWarning("[Project Forge] Skybox/Procedural шейдер не найден.");
                    return;
                }

                mat = new Material(skyShader);
                if (mat.HasProperty("_AtmosphereThickness"))
                    mat.SetFloat("_AtmosphereThickness", 0.6f);
                if (mat.HasProperty("_Exposure"))
                    mat.SetFloat("_Exposure", 0.7f);
                // Тёмный скайбокс: уменьшаем цвет неба
                if (mat.HasProperty("_SkyTint"))
                    mat.SetColor("_SkyTint", new Color(0.08f, 0.09f, 0.15f));
                if (mat.HasProperty("_GroundColor"))
                    mat.SetColor("_GroundColor", new Color(0.05f, 0.05f, 0.07f));

                AssetDatabase.CreateAsset(mat, skyboxMatPath);
            }

            RenderSettings.skybox = mat;
            DynamicGI.UpdateEnvironment();
        }

        private static void SetupSun()
        {
            // Ищем существующий Directional Light или создаём
            Light sun = null;
            var allLights = UnityEngine.Object.FindObjectsByType<Light>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var l in allLights)
            {
                if (l.type == LightType.Directional)
                {
                    sun = l;
                    break;
                }
            }

            if (sun == null)
            {
                var sunGo = new GameObject("Sun");
                sun = sunGo.AddComponent<Light>();
            }
            else
            {
                sun.gameObject.name = "Sun";
            }

            sun.type      = LightType.Directional;
            sun.color     = new Color(1f, 0.95f, 0.85f);
            sun.intensity = 1.1f;
            sun.shadows   = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

            RenderSettings.sun = sun;
        }

        private static void SetupAmbient()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor      = new Color(0.35f, 0.45f, 0.65f);
            RenderSettings.ambientEquatorColor  = new Color(0.2f, 0.22f, 0.28f);
            RenderSettings.ambientGroundColor   = new Color(0.05f, 0.05f, 0.08f);
        }

        private static void SetupGroundMaterial()
        {
            const string groundMatPath = "Assets/_Project/Art/Materials/GroundDark.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(groundMatPath);
            if (mat == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                if (shader == null)
                {
                    Debug.LogWarning("[Project Forge] URP Lit шейдер не найден для Ground.");
                    return;
                }

                mat = new Material(shader);
                var groundColor = new Color(0.16f, 0.18f, 0.22f);
                mat.color = groundColor;
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", groundColor);
                if (mat.HasProperty("_Smoothness"))
                    mat.SetFloat("_Smoothness", 0.15f);

                AssetDatabase.CreateAsset(mat, groundMatPath);
            }

            // Применяем к объекту "Ground" в сцене
            var groundGo = GameObject.Find("Ground");
            if (groundGo != null)
            {
                var mr = groundGo.GetComponent<MeshRenderer>();
                if (mr != null)
                    mr.sharedMaterial = mat;
            }
            else
            {
                Debug.LogWarning("[Project Forge] GameObject 'Ground' не найден в сцене — материал не применён.");
            }
        }

        private static void SetupPostFxVolume()
        {
            const string profilePath = "Assets/_Project/Settings/PostFX.asset";

            // Ищем или создаём Volume в сцене
            var existingVolume = UnityEngine.Object.FindFirstObjectByType<Volume>();
            GameObject volumeGo;
            Volume volume;

            if (existingVolume != null && existingVolume.gameObject.name == "PostFX")
            {
                volumeGo = existingVolume.gameObject;
                volume   = existingVolume;
            }
            else
            {
                volumeGo = new GameObject("PostFX");
                volume   = volumeGo.AddComponent<Volume>();
            }

            volume.isGlobal = true;

            // Создаём или загружаем VolumeProfile
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(profilePath);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, profilePath);
            }

            volume.sharedProfile = profile;

            // Bloom
            if (!profile.TryGet<Bloom>(out var bloom))
                bloom = profile.Add<Bloom>(overrides: true);
            bloom.active          = true;
            bloom.intensity.value     = 0.35f;
            bloom.threshold.value     = 0.9f;
            bloom.intensity.overrideState  = true;
            bloom.threshold.overrideState  = true;

            // Vignette
            if (!profile.TryGet<Vignette>(out var vignette))
                vignette = profile.Add<Vignette>(overrides: true);
            vignette.active           = true;
            vignette.intensity.value  = 0.25f;
            vignette.intensity.overrideState = true;

            // ColorAdjustments
            if (!profile.TryGet<ColorAdjustments>(out var colorAdj))
                colorAdj = profile.Add<ColorAdjustments>(overrides: true);
            colorAdj.active                  = true;
            colorAdj.saturation.value         = 8f;
            colorAdj.postExposure.value       = 0.1f;
            colorAdj.saturation.overrideState  = true;
            colorAdj.postExposure.overrideState = true;

            // Tonemapping
            if (!profile.TryGet<Tonemapping>(out var tonemapping))
                tonemapping = profile.Add<Tonemapping>(overrides: true);
            tonemapping.active       = true;
            tonemapping.mode.value   = TonemappingMode.ACES;
            tonemapping.mode.overrideState = true;

            EditorUtility.SetDirty(profile);
        }

        private static void SetupMainCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                Debug.LogWarning("[Project Forge] Main Camera не найдена в сцене.");
                return;
            }

            cam.allowHDR = true;

            var camData = cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            if (camData == null)
                camData = cam.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();

            camData.renderPostProcessing = true;
        }

        // ================================================================
        // Материалы-вспомогательные методы
        // ================================================================

        private static Material EnsureUnitMaterial(string matPath, string texturePath, Color tint)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (existing != null)
                return existing;

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");
            if (shader == null)
                return null;

            var mat = new Material(shader);
            mat.color = tint;
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", tint);

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (tex != null)
            {
                if (mat.HasProperty("_BaseMap"))
                    mat.SetTexture("_BaseMap", tex);
                else if (mat.HasProperty("_MainTex"))
                    mat.SetTexture("_MainTex", tex);
            }

            AssetDatabase.CreateAsset(mat, matPath);
            return mat;
        }

        private static Material EnsureCrystalMaterial(string matPath)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (existing != null)
                return existing;

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");
            if (shader == null)
                return null;

            var mat = new Material(shader);
            var emerald = new Color(0.1f, 0.85f, 0.45f);
            mat.color = emerald;

            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", emerald);

            // Emission
            mat.EnableKeyword("_EMISSION");
            if (mat.HasProperty("_EmissionColor"))
                mat.SetColor("_EmissionColor", new Color(0.05f, 0.4f, 0.2f));

            if (mat.HasProperty("_Smoothness"))
                mat.SetFloat("_Smoothness", 0.85f);

            if (mat.HasProperty("_Metallic"))
                mat.SetFloat("_Metallic", 0.0f);

            AssetDatabase.CreateAsset(mat, matPath);
            return mat;
        }

        private static Material EnsureParticleMaterial(string name, string texturePath, bool additive)
        {
            var matPath  = $"Assets/_Project/Art/Materials/VFX/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (existing != null)
                return existing;

            // URP Particles/Unlit — лучший выбор для VFX
            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
                shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null)
                shader = Shader.Find("Legacy Shaders/Particles/Additive");
            if (shader == null)
            {
                Debug.LogWarning($"[Project Forge] Particle-шейдер не найден для {name}.");
                return null;
            }

            var mat = new Material(shader);

            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (tex != null)
            {
                if (mat.HasProperty("_BaseMap"))
                    mat.SetTexture("_BaseMap", tex);
                else if (mat.HasProperty("_MainTex"))
                    mat.SetTexture("_MainTex", tex);
            }

            if (additive)
            {
                // Аддитивный blending
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
                mat.SetInt("_ZWrite",   0);
                mat.renderQueue = 3000;

                // URP-specific blend mode
                if (mat.HasProperty("_Surface"))
                    mat.SetFloat("_Surface", 1f);
                if (mat.HasProperty("_Blend"))
                    mat.SetFloat("_Blend", 3f); // Additive
            }
            else
            {
                // Alpha blend
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite",   0);
                mat.renderQueue = 3000;

                if (mat.HasProperty("_Surface"))
                    mat.SetFloat("_Surface", 1f);
                if (mat.HasProperty("_Blend"))
                    mat.SetFloat("_Blend", 0f); // Alpha blend
            }

            AssetDatabase.CreateAsset(mat, matPath);
            return mat;
        }

        // ================================================================
        // Нормализация масштаба по баундам
        // ================================================================

        /// <summary>
        /// Вычисляет суммарный AABB всех Renderer'ов объекта в мировых координатах.
        /// Возвращает bounds с центром (0,0,0) и нулевым размером, если рендереров нет.
        /// Важно: вызывать ПОСЛЕ установки localScale и localPosition.
        /// </summary>
        private static Bounds CalcBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return new Bounds(go.transform.position, Vector3.zero);

            var b = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                b.Encapsulate(renderers[i].bounds);
            return b;
        }

        /// <summary>
        /// Масштабирует go так, чтобы высота суммарного AABB (size.y) равнялась targetHeight.
        /// Идемпотентен: сначала сбрасывает localScale в Vector3.one, потом применяет коэффициент.
        /// </summary>
        private static void NormalizeVisualByHeight(GameObject go, float targetHeight)
        {
            go.transform.localScale = Vector3.one;
            var b = CalcBounds(go);
            float currentH = b.size.y;
            if (currentH < 0.0001f) return;
            float factor = targetHeight / currentH;
            go.transform.localScale = Vector3.one * factor;
        }

        /// <summary>
        /// Масштабирует go так, чтобы max(size.x, size.z) суммарного AABB равнялся targetFootprint.
        /// Используется для зданий, где важен горизонтальный gabарит.
        /// </summary>
        private static void NormalizeVisualByFootprint(GameObject go, float targetFootprint)
        {
            go.transform.localScale = Vector3.one;
            var b = CalcBounds(go);
            float currentFp = Mathf.Max(b.size.x, b.size.z);
            if (currentFp < 0.0001f) return;
            float factor = targetFootprint / currentFp;
            go.transform.localScale = Vector3.one * factor;
        }

        /// <summary>
        /// Масштабирует go так, чтобы размер по оси Z (глубина/длина) равнялся targetDepth.
        /// Используется для бластера в руке героя.
        /// </summary>
        private static void NormalizeVisualByDepth(GameObject go, float targetDepth)
        {
            go.transform.localScale = Vector3.one;
            var b = CalcBounds(go);
            float currentD = b.size.z;
            if (currentD < 0.0001f) return;
            float factor = targetDepth / currentD;
            go.transform.localScale = Vector3.one * factor;
        }

        /// <summary>
        /// После нормализации масштаба сдвигает go по Y так, чтобы нижняя граница
        /// суммарного AABB находилась на bottomY (в координатах родителя).
        /// </summary>
        private static void AlignVisualBottom(GameObject go, float bottomY)
        {
            var b = CalcBounds(go);
            // b.min.y — мировая нижняя граница. Нам нужно перевести в локальные координаты родителя.
            // go.transform.position.y — текущая мировая Y позиция go.
            // Смещение: bottomY_world = bottomY + parent.position.y
            var parent = go.transform.parent;
            float parentWorldY = parent != null ? parent.position.y : 0f;
            float targetWorldBottomY = parentWorldY + bottomY;
            float deltaY = targetWorldBottomY - b.min.y;
            var p = go.transform.localPosition;
            go.transform.localPosition = new Vector3(p.x, p.y + deltaY, p.z);
        }

        // ================================================================
        // Утилиты (дублируют паттерн из других вкладок)
        // ================================================================

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
