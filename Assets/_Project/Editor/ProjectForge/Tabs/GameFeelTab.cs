using DiplomaGame.Runtime.GameFeel;
using DiplomaGame.Runtime.UI;
using DiplomaGame.Runtime.VFX;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

namespace DiplomaGame.Editor
{
    /// <summary>
    /// Вкладка GameFeel — идемпотентная настройка game-feel героя.
    /// Кнопки:
    ///   1. Create GameFeelSettings.asset
    ///   2. Setup GameFeelManager (на GameManagers)
    ///   3. Add HitFlashHandler ко всем юнит-префабам
    ///   4. Add DashTrail to Hero (TrailRenderer на Hero/Visual)
    ///   5. Build ShockwaveRing Prefab + Wire к VfxManager
    /// </summary>
    internal sealed class GameFeelTab : IForgeTab
    {
        private const string SettingsAssetPath  = "Assets/_Project/Data/GameFeel/GameFeelSettings.asset";
        private const string UnitPrefabsFolder  = "Assets/_Project/Prefabs/Units";
        private const string ShockwaveRingPath  = "Assets/_Project/Prefabs/VFX/ShockwaveRing.prefab";
        private const string ParticleGlowMatPath = "Assets/_Project/Art/Materials/VFX/ParticleGlow.mat";

        public string Title => "GameFeel";

        public void OnGUI()
        {
            GUILayout.Space(8);
            GUILayout.Label("Game Feel (Circle-12)", EditorStyles.boldLabel);
            GUILayout.Space(4);

            EditorGUILayout.HelpBox(
                "Создаёт ScriptableObject GameFeelSettings с дефолтными параметрами.\n" +
                "Путь: Assets/_Project/Data/GameFeel/GameFeelSettings.asset. Идемпотентно.",
                MessageType.Info);

            if (GUILayout.Button("Create GameFeelSettings.asset", GUILayout.Height(28)))
                CreateGameFeelSettings();

            GUILayout.Space(6);

            EditorGUILayout.HelpBox(
                "Добавляет GameFeelManager на GameManagers, проставляет ссылки на SO, TPS-камеру.\n" +
                "Идемпотентно.",
                MessageType.Info);

            if (GUILayout.Button("Setup GameFeelManager", GUILayout.Height(28)))
                SetupGameFeelManager();

            GUILayout.Space(6);

            EditorGUILayout.HelpBox(
                "Добавляет HitFlashHandler ко всем префабам юнитов в Prefabs/Units/.\n" +
                "Идемпотентно.",
                MessageType.Info);

            if (GUILayout.Button("Add HitFlashHandler to Unit Prefabs", GUILayout.Height(28)))
                AddHitFlashToUnitPrefabs();

            GUILayout.Space(6);

            EditorGUILayout.HelpBox(
                "Добавляет DashTrailHandler и TrailRenderer на Hero/Visual в открытой сцене.\n" +
                "Параметры: time=0.2, width 0.4→0, цвет (0.4,0.8,1)→alpha0. Идемпотентно.",
                MessageType.Info);

            if (GUILayout.Button("Add DashTrail to Hero", GUILayout.Height(28)))
                AddDashTrailToHero();

            GUILayout.Space(6);

            EditorGUILayout.HelpBox(
                "Создаёт ShockwaveRing.prefab (ParticleSystem Circle burst-32) " +
                "и прописывает его как _shockwaveRingPrefab в VfxManager сцены.\n" +
                "Идемпотентно.",
                MessageType.Info);

            if (GUILayout.Button("Build ShockwaveRing Prefab + Wire", GUILayout.Height(28)))
            {
                BuildShockwaveRingPrefab();
                WireShockwaveToVfxManager();
            }

            GUILayout.Space(6);

            EditorGUILayout.HelpBox(
                "Запускает все шаги разом (для ForgeBatch.SetupGameFeel).",
                MessageType.Info);

            if (GUILayout.Button("Setup All GameFeel", GUILayout.Height(32)))
                SetupAll();

            GUILayout.Space(8);
            GUILayout.Label("Circle-24: Sprint / Stamina", EditorStyles.boldLabel);
            GUILayout.Space(4);

            EditorGUILayout.HelpBox(
                "Dynamic FOV (C22/C24 update):\n" +
                "• Добавляет/обновляет DynamicFovController на GameManagers\n" +
                "• Прошивает _heroController (Hero/HeroController) для sprint-widen\n" +
                "• Записывает fovSprintWiden=4 в GameFeelSettings.asset (ForceReserialize)\n" +
                "• Сохраняет прежние дефолты C22 (kickAmount=9, kickDuration=0.08, returnSpeed=12)\n" +
                "Требует: SetupGameFeel (C12) выполнен. Идемпотентно.",
                MessageType.Info);

            if (GUILayout.Button("Setup Dynamic FOV (C22/C24)", GUILayout.Height(28)))
                SetupDynamicFov();

            GUILayout.Space(6);

            EditorGUILayout.HelpBox(
                "Stamina Bar (C24):\n" +
                "• Создаёт HeroStaminaBar в TPS_Block (над HP-баром, 280×12, y=42)\n" +
                "• Фон серый 0.15a, fill жёлтый → красный при низкой стамине\n" +
                "• По умолчанию скрыт — появляется при спринте или убыли стамины\n" +
                "• Прошивает _fill и _heroController\n" +
                "Требует: BuildGameHUD (M6a) выполнен. Идемпотентно.",
                MessageType.Info);

            if (GUILayout.Button("Setup Stamina Bar (C24)", GUILayout.Height(28)))
                SetupStaminaBar();
        }

        // ================================================================
        // Публичные методы (используются из ForgeBatch)
        // ================================================================

        internal static void CreateGameFeelSettings()
        {
            EnsureFolder("Assets/_Project/Data/GameFeel");

            var existing = AssetDatabase.LoadAssetAtPath<GameFeelSettings>(SettingsAssetPath);
            if (existing != null)
            {
                Debug.Log("[GameFeel] GameFeelSettings.asset уже существует — пропуск.");
                return;
            }

            var settings = ScriptableObject.CreateInstance<GameFeelSettings>();
            AssetDatabase.CreateAsset(settings, SettingsAssetPath);
            AssetDatabase.SaveAssets();
            Debug.Log("[GameFeel] Создан GameFeelSettings.asset.");
        }

        internal static void SetupGameFeelManager()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.IsValid()) return;

            var managersGo = GameObject.Find("GameManagers");
            if (managersGo == null)
            {
                Debug.LogWarning("[GameFeel] GameManagers не найден — сначала Setup Mode Rig.");
                return;
            }

            // GameFeelManager
            var mgr = managersGo.GetComponent<GameFeelManager>();
            if (mgr == null)
                mgr = managersGo.AddComponent<GameFeelManager>();

            var settings = AssetDatabase.LoadAssetAtPath<GameFeelSettings>(SettingsAssetPath);
            var so       = new SerializedObject(mgr);

            so.FindProperty("_settings").objectReferenceValue = settings;

            // TPS Camera
            var tpsCamGo = GameObject.Find("TPS Camera");
            if (tpsCamGo != null)
            {
                var tpsCam = tpsCamGo.GetComponent<CinemachineCamera>();
                so.FindProperty("_tpsCamera").objectReferenceValue = tpsCam;
            }
            else
            {
                Debug.LogWarning("[GameFeel] 'TPS Camera' не найдена в сцене.");
            }

            // HeroShooter
            var heroGo = GameObject.Find("Hero");
            if (heroGo != null)
            {
                var shooter = heroGo.GetComponent<DiplomaGame.Runtime.Hero.HeroShooter>();
                so.FindProperty("_heroShooter").objectReferenceValue = shooter;

                var ability = heroGo.GetComponent<DiplomaGame.Runtime.Hero.AbilitySystem>();
                so.FindProperty("_abilitySystem").objectReferenceValue = ability;
            }

            // PauseController
            var pauseCtrl = Object.FindFirstObjectByType<DiplomaGame.Runtime.UI.PauseController>();
            so.FindProperty("_pauseController").objectReferenceValue = pauseCtrl;

            // DashTrailHandler (ищем на Hero/Visual)
            if (heroGo != null)
            {
                var visualTf = heroGo.transform.Find("Visual");
                if (visualTf != null)
                {
                    var dashTrail = visualTf.GetComponent<DashTrailHandler>();
                    so.FindProperty("_dashTrail").objectReferenceValue = dashTrail;
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[GameFeel] GameFeelManager настроен.");
        }

        internal static void AddHitFlashToUnitPrefabs()
        {
            var guids = AssetDatabase.FindAssets("t:Prefab", new[] { UnitPrefabsFolder });
            int count = 0;

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                using (var scope = new PrefabUtility.EditPrefabContentsScope(path))
                {
                    var root = scope.prefabContentsRoot;
                    if (root.GetComponent<HitFlashHandler>() == null)
                    {
                        root.AddComponent<HitFlashHandler>();
                        count++;
                    }
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[GameFeel] HitFlashHandler добавлен к {count} префабам юнитов.");
        }

        internal static void AddDashTrailToHero()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.IsValid()) return;

            var heroGo = GameObject.Find("Hero");
            if (heroGo == null)
            {
                Debug.LogWarning("[GameFeel] Hero не найден в сцене.");
                return;
            }

            // Находим или создаём Visual child
            var visualTf = heroGo.transform.Find("Visual");
            if (visualTf == null)
            {
                Debug.LogWarning("[GameFeel] Hero/Visual не найден — сначала Apply Visuals (M8).");
                return;
            }

            var visualGo = visualTf.gameObject;

            // TrailRenderer
            var trail = visualGo.GetComponent<TrailRenderer>();
            if (trail == null)
                trail = visualGo.AddComponent<TrailRenderer>();

            trail.time      = 0.2f;
            trail.startWidth = 0.4f;
            trail.endWidth   = 0f;
            trail.material   = AssetDatabase.LoadAssetAtPath<Material>(ParticleGlowMatPath);
            trail.emitting   = false;

            // Настройка цвета: (0.4,0.8,1) → alpha0
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(new Color(0.4f, 0.8f, 1f), 0f),
                        new GradientColorKey(new Color(0.4f, 0.8f, 1f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = gradient;

            // DashTrailHandler
            if (visualGo.GetComponent<DashTrailHandler>() == null)
                visualGo.AddComponent<DashTrailHandler>();

            // Обновляем ссылку в GameFeelManager
            var managersGo = GameObject.Find("GameManagers");
            if (managersGo != null)
            {
                var mgr = managersGo.GetComponent<GameFeelManager>();
                if (mgr != null)
                {
                    var dashHandler = visualGo.GetComponent<DashTrailHandler>();
                    var so = new SerializedObject(mgr);
                    so.FindProperty("_dashTrail").objectReferenceValue = dashHandler;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[GameFeel] DashTrail добавлен на Hero/Visual.");
        }

        internal static void BuildShockwaveRingPrefab()
        {
            EnsureFolder("Assets/_Project/Prefabs/VFX");
            EnsureFolder("Assets/_Project/Art/Materials/VFX");

            // Загружаем ParticleGlow.mat (создан VFXTab)
            var glowMat = AssetDatabase.LoadAssetAtPath<Material>(ParticleGlowMatPath);
            if (glowMat == null)
            {
                Debug.LogWarning($"[GameFeel] {ParticleGlowMatPath} не найден — сначала Build VFX Prefabs (M8).");
                // Создаём fallback материал
                glowMat = EnsureParticleMaterial();
            }

            var root = new GameObject("ShockwaveRing");
            var ps   = root.AddComponent<ParticleSystem>();

            // main
            var main = ps.main;
            main.loop        = false;
            main.playOnAwake = false;
            main.startLifetime = 0.4f;
            main.startSize     = 0.5f;
            main.startColor    = new Color(0.4f, 0.7f, 1f, 0.8f);
            main.startSpeed    = 4f;
            main.maxParticles  = 64;

            // emission — burst 32
            var emission = ps.emission;
            emission.enabled      = true;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 32) });

            // shape — Circle
            var shape = ps.shape;
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius    = 0.5f;

            // renderer — аддитивный
            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (renderer != null && glowMat != null)
            {
                renderer.material   = glowMat;
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
            }

            // colorOverLifetime — fade out
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(
                new[] { new GradientColorKey(new Color(0.4f, 0.7f, 1f), 0f),
                        new GradientColorKey(new Color(0.4f, 0.7f, 1f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            col.color = grad;

            // sizeOverLifetime — расширяется
            var sizeOL = ps.sizeOverLifetime;
            sizeOL.enabled = true;
            sizeOL.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 0.5f), new Keyframe(1f, 3f)));

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, ShockwaveRingPath);
            Object.DestroyImmediate(root);

            if (prefab != null)
                Debug.Log($"[GameFeel] ShockwaveRing.prefab создан: {ShockwaveRingPath}");
            else
                Debug.LogError($"[GameFeel] Не удалось сохранить ShockwaveRing.prefab");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        internal static void WireShockwaveToVfxManager()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.IsValid()) return;

            var managersGo = GameObject.Find("GameManagers");
            if (managersGo == null)
            {
                Debug.LogWarning("[GameFeel] GameManagers не найден.");
                return;
            }

            var vfxMgr = managersGo.GetComponent<VfxManager>();
            if (vfxMgr == null)
            {
                Debug.LogWarning("[GameFeel] VfxManager не найден на GameManagers — сначала Apply Visuals (M8).");
                return;
            }

            var shockwavePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ShockwaveRingPath);
            if (shockwavePrefab == null)
            {
                Debug.LogWarning("[GameFeel] ShockwaveRing.prefab не найден — сначала Build ShockwaveRing Prefab.");
                return;
            }

            var so = new SerializedObject(vfxMgr);
            so.FindProperty("_shockwaveRingPrefab").objectReferenceValue = shockwavePrefab;
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[GameFeel] ShockwaveRing подключён к VfxManager.");
        }

        internal static void SetupAll()
        {
            CreateGameFeelSettings();
            BuildShockwaveRingPrefab();
            AddHitFlashToUnitPrefabs();
            SetupGameFeelManager();
            WireShockwaveToVfxManager();
            AddDashTrailToHero();
            // Переоткрываем гарантию: сохраняем ещё раз
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.IsValid())
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            Debug.Log("[GameFeel] SetupAll завершён.");
        }

        /// <summary>
        /// Circle-22 Dynamic FOV: идемпотентно добавляет DynamicFovController на GameManagers,
        /// прошивает ссылки (_tpsCamera, _abilitySystem, _modeController, _settings) в Sandbox.unity,
        /// записывает дефолты Circle-22 в GameFeelSettings.asset.
        ///
        /// КРИТИЧНО: новые SerializeField-поля GameFeelSettings получают type-default (0) на
        /// существующем asset'е — явная запись через SerializedObject + ForceReserializeAssets
        /// обязательна (паттерн Circle-20/21).
        ///
        /// Prerequisite: SetupGameFeel (C12) уже выполнен (GameFeelSettings.asset, GameManagers существуют).
        /// </summary>
        internal static void SetupDynamicFov()
        {
            const string SettingsAssetPath = "Assets/_Project/Data/GameFeel/GameFeelSettings.asset";

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                Debug.LogWarning("[Forge C22] Нет открытой сцены.");
                return;
            }

            // ---- 1. GameManagers ----
            var managersGo = GameObject.Find("GameManagers");
            if (managersGo == null)
            {
                Debug.LogWarning("[Forge C22] GameManagers не найден — сначала Setup Mode Rig.");
                return;
            }

            // ---- 2. Идемпотентно добавляем DynamicFovController ----
            var ctrl = managersGo.GetComponent<DiplomaGame.Runtime.GameFeel.DynamicFovController>();
            if (ctrl == null)
                ctrl = managersGo.AddComponent<DiplomaGame.Runtime.GameFeel.DynamicFovController>();

            // ---- 3. Собираем ссылки ----
            var tpsCamGo = GameObject.Find("TPS Camera");
            var tpsCam   = tpsCamGo != null ? tpsCamGo.GetComponent<CinemachineCamera>() : null;

            var heroGo       = GameObject.Find("Hero");
            var abilitySystem = heroGo != null
                ? heroGo.GetComponent<DiplomaGame.Runtime.Hero.AbilitySystem>()
                : null;

            var modeController = managersGo.GetComponent<DiplomaGame.Runtime.Core.GameModeController>();

            var settings = AssetDatabase.LoadAssetAtPath<DiplomaGame.Runtime.GameFeel.GameFeelSettings>(SettingsAssetPath);

            // ---- 4. Собираем HeroController ----
            var heroController = heroGo != null
                ? heroGo.GetComponent<DiplomaGame.Runtime.Hero.HeroController>()
                : null;

            // ---- 5. Прошиваем через SerializedObject ----
            var so = new SerializedObject(ctrl);
            so.FindProperty("_tpsCamera").objectReferenceValue        = tpsCam;
            so.FindProperty("_abilitySystem").objectReferenceValue    = abilitySystem;
            so.FindProperty("_modeController").objectReferenceValue   = modeController;
            so.FindProperty("_settings").objectReferenceValue         = settings;
            so.FindProperty("_heroController").objectReferenceValue   = heroController;
            so.ApplyModifiedPropertiesWithoutUndo();

            if (tpsCam == null)
                Debug.LogWarning("[Forge C22/C24] 'TPS Camera' не найдена — _tpsCamera не прошита. Назначьте вручную.");
            if (abilitySystem == null)
                Debug.LogWarning("[Forge C22/C24] Hero/AbilitySystem не найден — _abilitySystem не прошита. Назначьте вручную.");
            if (heroController == null)
                Debug.LogWarning("[Forge C24] Hero/HeroController не найден — _heroController не прошит. Sprint-widen не будет работать.");

            // ---- 6. Записываем дефолты Circle-22/24 в GameFeelSettings.asset ----
            // Новые поля получают type-default (0) на существующем ассете.
            // SerializedObject + ForceReserializeAssets гарантирует правильные значения.
            if (settings != null)
            {
                var settingsSo = new SerializedObject(settings);

                var kickAmtProp = settingsSo.FindProperty("fovKickAmount");
                if (kickAmtProp != null && Mathf.Approximately(kickAmtProp.floatValue, 0f))
                    kickAmtProp.floatValue = 9f;

                var kickDurProp = settingsSo.FindProperty("fovKickDuration");
                if (kickDurProp != null && Mathf.Approximately(kickDurProp.floatValue, 0f))
                    kickDurProp.floatValue = 0.08f;

                var returnSpdProp = settingsSo.FindProperty("fovReturnSpeed");
                if (returnSpdProp != null && Mathf.Approximately(returnSpdProp.floatValue, 0f))
                    returnSpdProp.floatValue = 12f;

                // Circle-24: fovSprintWiden — записываем дефолт если 0 (новое поле на старом ассете)
                var sprintWidenProp = settingsSo.FindProperty("fovSprintWiden");
                if (sprintWidenProp != null && Mathf.Approximately(sprintWidenProp.floatValue, 0f))
                    sprintWidenProp.floatValue = 4f;

                settingsSo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
                AssetDatabase.ForceReserializeAssets(
                    new[] { SettingsAssetPath },
                    ForceReserializeAssetsOptions.ReserializeAssets);
            }
            else
            {
                Debug.LogWarning("[Forge C22/C24] GameFeelSettings.asset не найден — дефолты не записаны. " +
                                 "Сначала запустите Setup GameFeel (C12).");
            }

            // ---- 7. Сохранение сцены ----
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.Refresh();

            Debug.Log("[Forge C22/C24] SetupDynamicFov завершён." +
                      "\n  • DynamicFovController → GameManagers" +
                      "\n  • _tpsCamera       → " + (tpsCam         != null ? tpsCam.gameObject.name         : "null (назначьте вручную)") +
                      "\n  • _abilitySystem   → " + (abilitySystem  != null ? abilitySystem.gameObject.name  : "null (назначьте вручную)") +
                      "\n  • _heroController  → " + (heroController != null ? heroController.gameObject.name : "null (sprint-widen неактивен)") +
                      "\n  • _modeController  → " + (modeController != null ? modeController.gameObject.name : "null") +
                      "\n  • _settings        → " + (settings       != null ? SettingsAssetPath               : "null") +
                      "\n  • GameFeelSettings: fovKickAmount=9, fovKickDuration=0.08, fovReturnSpeed=12, fovSprintWiden=4");
        }

        /// <summary>
        /// Circle-24 Stamina Bar: идемпотентно создаёт HeroStaminaBar UI-элемент в TPS_Block и
        /// прошивает _fill (Image) и _heroController (HeroController с Hero).
        ///
        /// Layout: левый нижний угол, над HP-баром (y = 16 + 22 + 4 = 42), та же ширина 280×12.
        /// Фон: тёмный (0.15, 0.15, 0.15, 0.8); fill: жёлтый (логика в StaminaBarLogic).
        ///
        /// КРИТИЧНО: новые поля fovSprintWiden на существующем ассете нужно записать явно —
        /// используется SetDirty + SaveAssets + ForceReserializeAssets (тот же паттерн C20/21/22).
        ///
        /// Prerequisite: BuildGameHUD (M6a) уже выполнен (TPS_Block существует).
        /// </summary>
        internal static void SetupStaminaBar()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                Debug.LogWarning("[Forge C24] Нет открытой сцены.");
                return;
            }

            // ---- 1. TPS_Block ----
            var hudGo    = GameObject.Find("GameHUD");
            if (hudGo == null)
            {
                Debug.LogWarning("[Forge C24] 'GameHUD' не найден — сначала BuildGameHUD (M6a).");
                return;
            }

            var tpsBlockTf = hudGo.transform.Find("TPS_Block");
            if (tpsBlockTf == null)
            {
                Debug.LogWarning("[Forge C24] 'TPS_Block' не найден внутри GameHUD.");
                return;
            }
            var tpsBlock = tpsBlockTf.gameObject;

            // ---- 2. Идемпотентно создаём HeroStaminaBar ----
            // Размещаем над HP-баром: HP → y=16, h=22 → StaminaBar → y=16+22+4=42, h=12
            const float StaminaBarY      = 42f;
            const float StaminaBarH      = 12f;
            const float StaminaBarW      = 280f;
            const float StaminaBarX      = 16f;

            var existingTf = tpsBlockTf.Find("HeroStaminaBar");
            var staminaBarGo = existingTf != null ? existingTf.gameObject : null;

            if (staminaBarGo == null)
            {
                staminaBarGo = new GameObject("HeroStaminaBar");
                staminaBarGo.transform.SetParent(tpsBlock.transform, false);
                staminaBarGo.AddComponent<RectTransform>();
            }

            // RectTransform — левый нижний угол
            var barRt = staminaBarGo.GetComponent<RectTransform>();
            barRt.anchorMin        = new Vector2(0f, 0f);
            barRt.anchorMax        = new Vector2(0f, 0f);
            barRt.pivot            = new Vector2(0f, 0f);
            barRt.anchoredPosition = new Vector2(StaminaBarX, StaminaBarY);
            barRt.sizeDelta        = new Vector2(StaminaBarW, StaminaBarH);

            // Фоновый Image
            var bgImg = staminaBarGo.GetComponent<Image>();
            if (bgImg == null) bgImg = staminaBarGo.AddComponent<Image>();
            bgImg.color = new Color(0.15f, 0.15f, 0.15f, 0.8f);

            // Fill child
            var fillTf = staminaBarGo.transform.Find("Fill");
            var fillGo = fillTf != null ? fillTf.gameObject : null;
            if (fillGo == null)
            {
                fillGo = new GameObject("Fill");
                fillGo.transform.SetParent(staminaBarGo.transform, false);
                fillGo.AddComponent<RectTransform>();
            }

            var fillRt = fillGo.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;

            var fillImg = fillGo.GetComponent<Image>();
            if (fillImg == null) fillImg = fillGo.AddComponent<Image>();
            fillImg.color      = Color.yellow;
            fillImg.type       = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillAmount = 1f;

            // HeroStaminaBar компонент
            var staminaBar = staminaBarGo.GetComponent<HeroStaminaBar>();
            if (staminaBar == null) staminaBar = staminaBarGo.AddComponent<HeroStaminaBar>();

            var heroGo   = GameObject.Find("Hero");
            var heroCtrl = heroGo != null
                ? heroGo.GetComponent<DiplomaGame.Runtime.Hero.HeroController>()
                : null;

            var soBar = new SerializedObject(staminaBar);
            soBar.FindProperty("_fill").objectReferenceValue           = fillImg;
            soBar.FindProperty("_heroController").objectReferenceValue = heroCtrl;
            soBar.ApplyModifiedPropertiesWithoutUndo();

            if (heroCtrl == null)
                Debug.LogWarning("[Forge C24] Hero/HeroController не найден — _heroController не прошит. Назначьте вручную.");

            // ---- 3. По умолчанию скрываем (показывается только при спринте/убыли стамины) ----
            staminaBarGo.SetActive(false);

            // ---- 4. Сохранение сцены ----
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[Forge C24] SetupStaminaBar завершён." +
                      "\n  • HeroStaminaBar → TPS_Block/HeroStaminaBar" +
                      "\n  • Позиция: (" + StaminaBarX + ", " + StaminaBarY + "), размер: " + StaminaBarW + "×" + StaminaBarH +
                      "\n  • _fill           → " + (fillImg  != null ? fillGo.name  : "null") +
                      "\n  • _heroController → " + (heroCtrl != null ? heroCtrl.gameObject.name : "null (назначьте вручную)"));
        }

        // ================================================================
        // Вспомогательные методы
        // ================================================================

        private static Material EnsureParticleMaterial()
        {
            var matPath = ParticleGlowMatPath;
            var existing = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (existing != null) return existing;

            EnsureFolder("Assets/_Project/Art/Materials/VFX");

            var shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Additive");
            if (shader == null) return null;

            var mat = new Material(shader);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3000;

            AssetDatabase.CreateAsset(mat, matPath);
            AssetDatabase.SaveAssets();
            return mat;
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
