using DiplomaGame.Runtime.Combat;
using DiplomaGame.Runtime.Commands;
using DiplomaGame.Runtime.Core;
using DiplomaGame.Runtime.Hero;
using DiplomaGame.Runtime.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using TMPro;

namespace DiplomaGame.Editor
{
    /// <summary>
    /// Вкладка UI — настройка игрового HUD (M6a).
    /// Кнопка "Build Game HUD (M6a)" идемпотентно собирает всю UI-иерархию в открытой сцене.
    /// </summary>
    internal sealed class UITab : IForgeTab
    {
        private const string MinimapRTPath         = "Assets/_Project/UI/MinimapRT.renderTexture";
        private const string OrderMoveMatPath      = "Assets/_Project/Art/Materials/OrderMove.mat";
        private const string OrderAttackMatPath    = "Assets/_Project/Art/Materials/OrderAttack.mat";

        public string Title => "UI";

        public void OnGUI()
        {
            GUILayout.Space(8);
            GUILayout.Label("HUD / UI", EditorStyles.boldLabel);
            GUILayout.Space(4);

            EditorGUILayout.HelpBox(
                "M6a: собирает игровой HUD обоих режимов в открытой сцене.\n" +
                "Canvas «GameHUD» (Screen Space Overlay), RTS-блок, TPS-блок, миникарта.\n" +
                "Операция идемпотентна.",
                MessageType.Info);

            GUILayout.Space(4);

            if (GUILayout.Button("Build Game HUD (M6a)", GUILayout.Height(32)))
                BuildGameHud();
        }

        // ----------------------------------------------------------------
        // Основная операция — доступна из ForgeBatch
        // ----------------------------------------------------------------

        internal static void BuildGameHud()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                EditorUtility.DisplayDialog("Project Forge", "Нет открытой сцены.", "OK");
                return;
            }

            // --- TMP Essential Resources (TMP Settings обязателен для SetText) ---
            EnsureTmpEssentials();

            // --- EventSystem (с InputSystemUIInputModule) ---
            EnsureEventSystem();

            // --- Canvas "GameHUD" ---
            var canvasGo = EnsureGameObject("GameHUD");
            var canvas   = EnsureComponent<Canvas>(canvasGo);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = EnsureComponent<CanvasScaler>(canvasGo);
            scaler.uiScaleMode            = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution    = new Vector2(1920f, 1080f);
            scaler.screenMatchMode        = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight     = 0.5f;

            EnsureComponent<GraphicRaycaster>(canvasGo);

            // Ищем или создаём корневые блоки
            var rtsBlock = EnsureChild(canvasGo, "RTS_Block");
            var tpsBlock = EnsureChild(canvasGo, "TPS_Block");

            // --- RTS-блок ---
            BuildRtsBlock(rtsBlock, scene);

            // --- TPS-блок ---
            BuildTpsBlock(tpsBlock);

            // --- HudController на Canvas ---
            var hudCtrl = EnsureComponent<HudController>(canvasGo);
            {
                var so = new SerializedObject(hudCtrl);
                so.FindProperty("rtsBlock").objectReferenceValue = rtsBlock;
                so.FindProperty("tpsBlock").objectReferenceValue = tpsBlock;

                var managersGo     = GameObject.Find("GameManagers");
                var modeController = managersGo != null ? managersGo.GetComponent<GameModeController>() : null;
                so.FindProperty("modeController").objectReferenceValue = modeController;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // --- MinimapController на Canvas ---
            var minimapCtrl = EnsureComponent<MinimapController>(canvasGo);
            {
                var minimapCamGo = GameObject.Find("MinimapCamera");
                var minimapCam   = minimapCamGo != null ? minimapCamGo.GetComponent<Camera>() : null;
                var minimapRtGo  = FindDescendantByName(rtsBlock, "MinimapDisplay");
                var rawImage     = minimapRtGo != null ? minimapRtGo.GetComponent<RawImage>() : null;

                var so = new SerializedObject(minimapCtrl);
                so.FindProperty("minimapCamera").objectReferenceValue  = minimapCam;
                so.FindProperty("minimapDisplay").objectReferenceValue = rawImage;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // --- OrderMarkerFeedback на GameManagers ---
            BuildOrderMarkerFeedback();

            // --- Обновить prefabs (HealthBar canvas) ---
            AddHealthBarToPrefab("Assets/_Project/Prefabs/Units/TestUnit.prefab");
            AddHealthBarToPrefab("Assets/_Project/Prefabs/Units/EnemyUnit.prefab");
            AddHealthBarToPrefab("Assets/_Project/Prefabs/Buildings/HQ.prefab");
            AddHealthBarToPrefab("Assets/_Project/Prefabs/Buildings/Barracks.prefab");
            AddHealthBarToPrefab("Assets/_Project/Prefabs/Buildings/Extractor.prefab");

            // --- HealthBar для Hero в сцене ---
            AddHealthBarToHeroInScene();

            // --- Сохранение ---
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Project Forge] Build Game HUD (M6a) выполнен.");
        }

        // ----------------------------------------------------------------
        // RTS-блок
        // ----------------------------------------------------------------

        /// <summary>
        /// Импортирует TMP Essential Resources, если TMP Settings ещё нет в проекте
        /// (без него любой TMP_Text.SetText кидает NRE).
        /// </summary>
        private static void EnsureTmpEssentials()
        {
            if (TMP_Settings.instance != null) return;

            const string packagePath = "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage";
            AssetDatabase.ImportPackage(packagePath, false);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("[Project Forge] TMP Essential Resources импортированы.");
        }

        private static void BuildRtsBlock(GameObject rtsBlock, UnityEngine.SceneManagement.Scene scene)
        {
            // --- Верхняя панель (ResourceDisplay) ---
            var topPanel = EnsureChild(rtsBlock, "TopPanel");
            {
                var rt   = topPanel.GetComponent<RectTransform>() ?? topPanel.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot     = new Vector2(0.5f, 1f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                rt.sizeDelta = new Vector2(0f, 50f);

                var bg = EnsureComponent<Image>(topPanel);
                bg.color = new Color(0f, 0f, 0f, 0.55f);

                // ResourceDisplay label
                var resGo  = EnsureChild(topPanel, "ResourceText");
                var resTmp = EnsureComponent<TextMeshProUGUI>(resGo);
                resTmp.text      = "Crystals: 150";
                resTmp.fontSize  = 20f;
                resTmp.alignment = TextAlignmentOptions.MidlineLeft;
                resTmp.color     = Color.white;

                var resRt = resGo.GetComponent<RectTransform>() ?? resGo.AddComponent<RectTransform>();
                resRt.anchorMin = Vector2.zero;
                resRt.anchorMax = Vector2.one;
                resRt.offsetMin = new Vector2(12f, 0f);
                resRt.offsetMax = new Vector2(-12f, 0f);

                // UiPulse на лейбл
                var pulse = EnsureComponent<UiPulse>(resGo);

                // ResourceDisplay на лейбл
                var resDis = EnsureComponent<ResourceDisplay>(resGo);
                {
                    var so = new SerializedObject(resDis);
                    so.FindProperty("pulse").objectReferenceValue = pulse;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            // --- Нижняя панель (SelectionPanel) ---
            var bottomPanel = EnsureChild(rtsBlock, "SelectionPanel");
            {
                var rt   = bottomPanel.GetComponent<RectTransform>() ?? bottomPanel.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot     = new Vector2(0.5f, 0f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                rt.sizeDelta = new Vector2(0f, 100f);

                var bg = EnsureComponent<Image>(bottomPanel);
                bg.color = new Color(0f, 0f, 0f, 0.6f);

                // Текст информации (юниты / здание)
                var infoGo  = EnsureChild(bottomPanel, "InfoText");
                var infoTmp = EnsureComponent<TextMeshProUGUI>(infoGo);
                infoTmp.text      = "";
                infoTmp.fontSize  = 18f;
                infoTmp.alignment = TextAlignmentOptions.MidlineLeft;
                infoTmp.color     = Color.white;

                var infoRt = infoGo.GetComponent<RectTransform>() ?? infoGo.AddComponent<RectTransform>();
                infoRt.anchorMin = new Vector2(0f, 0.5f);
                infoRt.anchorMax = new Vector2(0.6f, 1f);
                infoRt.offsetMin = new Vector2(12f, 0f);
                infoRt.offsetMax = Vector2.zero;

                // Блок прогресса производства
                var progressRoot = EnsureChild(bottomPanel, "ProductionBlock");
                {
                    var prt = progressRoot.GetComponent<RectTransform>() ?? progressRoot.AddComponent<RectTransform>();
                    prt.anchorMin = new Vector2(0f, 0f);
                    prt.anchorMax = new Vector2(0.6f, 0.5f);
                    prt.offsetMin = new Vector2(12f, 4f);
                    prt.offsetMax = new Vector2(-4f, -4f);

                    // Фон полосы прогресса
                    var bgProg  = EnsureChild(progressRoot, "ProgressBg");
                    var bgProgI = EnsureComponent<Image>(bgProg);
                    bgProgI.color = new Color(0.2f, 0.2f, 0.2f, 1f);
                    var bgProgRt = bgProg.GetComponent<RectTransform>() ?? bgProg.AddComponent<RectTransform>();
                    bgProgRt.anchorMin = new Vector2(0f, 0.5f);
                    bgProgRt.anchorMax = new Vector2(1f, 1f);
                    bgProgRt.offsetMin = Vector2.zero;
                    bgProgRt.offsetMax = Vector2.zero;

                    // Fill полосы
                    var fillProg  = EnsureChild(bgProg, "ProgressFill");
                    var fillProgI = EnsureComponent<Image>(fillProg);
                    fillProgI.color    = new Color(0.2f, 0.8f, 0.2f, 1f);
                    fillProgI.type     = Image.Type.Filled;
                    fillProgI.fillMethod    = Image.FillMethod.Horizontal;
                    fillProgI.fillAmount    = 0f;

                    var fillRt = fillProg.GetComponent<RectTransform>() ?? fillProg.AddComponent<RectTransform>();
                    fillRt.anchorMin = Vector2.zero;
                    fillRt.anchorMax = Vector2.one;
                    fillRt.offsetMin = Vector2.zero;
                    fillRt.offsetMax = Vector2.zero;

                    // Текст очереди
                    var queueGo  = EnsureChild(progressRoot, "QueueText");
                    var queueTmp = EnsureComponent<TextMeshProUGUI>(queueGo);
                    queueTmp.text      = "";
                    queueTmp.fontSize  = 14f;
                    queueTmp.color     = Color.white;
                    queueTmp.alignment = TextAlignmentOptions.MidlineLeft;

                    var queueRt = queueGo.GetComponent<RectTransform>() ?? queueGo.AddComponent<RectTransform>();
                    queueRt.anchorMin = new Vector2(0f, 0f);
                    queueRt.anchorMax = new Vector2(1f, 0.5f);
                    queueRt.offsetMin = Vector2.zero;
                    queueRt.offsetMax = Vector2.zero;
                }

                // Подсказка клавиш (правая часть панели)
                var hintGo  = EnsureChild(bottomPanel, "HintText");
                var hintTmp = EnsureComponent<TextMeshProUGUI>(hintGo);
                hintTmp.text      = "[T] — обучить    ПКМ — точка сбора";
                hintTmp.fontSize  = 13f;
                hintTmp.color     = new Color(0.8f, 0.8f, 0.8f, 1f);
                hintTmp.alignment = TextAlignmentOptions.MidlineLeft;

                var hintRt = hintGo.GetComponent<RectTransform>() ?? hintGo.AddComponent<RectTransform>();
                hintRt.anchorMin = new Vector2(0f, 0.5f);
                hintRt.anchorMax = new Vector2(0.6f, 1f);
                hintRt.offsetMin = new Vector2(12f, 0f);
                hintRt.offsetMax = Vector2.zero;

                // SelectionPanel-скрипт
                var managersGo      = GameObject.Find("GameManagers");
                var selectionSystem = managersGo != null
                    ? managersGo.GetComponent<DiplomaGame.Runtime.Selection.SelectionSystem>()
                    : null;

                var selPanel = EnsureComponent<SelectionPanel>(bottomPanel);
                {
                    var so = new SerializedObject(selPanel);
                    so.FindProperty("selectionSystem").objectReferenceValue  = selectionSystem;
                    so.FindProperty("infoText").objectReferenceValue         = infoTmp;
                    so.FindProperty("progressRoot").objectReferenceValue     = progressRoot;
                    so.FindProperty("progressFill").objectReferenceValue     = FindDescendantByName(progressRoot, "ProgressFill")?.GetComponent<Image>();
                    so.FindProperty("queueText").objectReferenceValue        = FindDescendantByName(progressRoot, "QueueText")?.GetComponent<TMP_Text>();
                    so.FindProperty("hintText").objectReferenceValue         = hintTmp;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            // --- Подсказки клавиш (левый нижний угол) ---
            var keysHint = EnsureChild(rtsBlock, "KeysHint");
            {
                var rt = keysHint.GetComponent<RectTransform>() ?? keysHint.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(0f, 0f);
                rt.pivot     = new Vector2(0f, 0f);
                rt.anchoredPosition = new Vector2(12f, 108f);
                rt.sizeDelta        = new Vector2(280f, 80f);

                var keysText = EnsureComponent<TextMeshProUGUI>(keysHint);
                keysText.text      = "Tab — режим    B/E — строить\nT — обучить    H — стоять";
                keysText.fontSize  = 13f;
                keysText.color     = new Color(0.8f, 0.8f, 0.8f, 0.8f);
                keysText.alignment = TextAlignmentOptions.BottomLeft;
            }

            // --- Миникарта (правый нижний угол) ---
            BuildMinimap(rtsBlock, scene);
        }

        private static void BuildMinimap(GameObject rtsBlock, UnityEngine.SceneManagement.Scene scene)
        {
            // RenderTexture ассет
            var rt = EnsureMinimapRT();

            // MinimapCamera (ортографическая камера сверху)
            var camGo = EnsureGameObject("MinimapCamera");
            var cam   = EnsureComponent<Camera>(camGo);
            cam.orthographic     = true;
            cam.orthographicSize = 60f;
            cam.transform.position = new Vector3(0f, 80f, 0f);
            cam.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            cam.targetTexture      = rt;
            cam.clearFlags         = CameraClearFlags.SolidColor;
            cam.backgroundColor    = new Color(0.05f, 0.1f, 0.05f, 1f);

            // Убираем post-processing (URP: дополнительные данные)
            var camData = cam.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            if (camData == null)
                camData = cam.gameObject.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            camData.renderPostProcessing = false;

            // RawImage в углу
            var displayGo  = EnsureChild(rtsBlock, "MinimapDisplay");
            var rawImage   = EnsureComponent<RawImage>(displayGo);
            rawImage.texture = rt;

            var drect = displayGo.GetComponent<RectTransform>() ?? displayGo.AddComponent<RectTransform>();
            drect.anchorMin = new Vector2(1f, 0f);
            drect.anchorMax = new Vector2(1f, 0f);
            drect.pivot     = new Vector2(1f, 0f);
            drect.anchoredPosition = new Vector2(-10f, 108f);
            drect.sizeDelta        = new Vector2(256f, 256f);
        }

        // ----------------------------------------------------------------
        // TPS-блок
        // ----------------------------------------------------------------

        private static void BuildTpsBlock(GameObject tpsBlock)
        {
            // --- Crosshair в центре ---
            var crosshairGo = EnsureChild(tpsBlock, "Crosshair");
            {
                EnsureComponent<CrosshairUI>(crosshairGo);

                var rt = crosshairGo.GetComponent<RectTransform>() ?? crosshairGo.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot     = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta        = new Vector2(30f, 30f);

                // 4 полоски крестика
                BuildCrosshairLines(crosshairGo);
            }

            // --- HeroHpBar (нижний левый угол) ---
            var heroHpGo = EnsureChild(tpsBlock, "HeroHpBar");
            {
                var rt = heroHpGo.GetComponent<RectTransform>() ?? heroHpGo.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(0f, 0f);
                rt.pivot     = new Vector2(0f, 0f);
                rt.anchoredPosition = new Vector2(20f, 30f);
                rt.sizeDelta        = new Vector2(200f, 20f);

                // Фон
                var bgI = EnsureComponent<Image>(heroHpGo);
                bgI.color = new Color(0.15f, 0.15f, 0.15f, 0.8f);

                // Fill
                var fillGo = EnsureChild(heroHpGo, "Fill");
                var fillI  = EnsureComponent<Image>(fillGo);
                fillI.color      = new Color(0.2f, 0.8f, 0.2f, 1f);
                fillI.type       = Image.Type.Filled;
                fillI.fillMethod = Image.FillMethod.Horizontal;
                fillI.fillAmount = 1f;

                var fillRt = fillGo.GetComponent<RectTransform>() ?? fillGo.AddComponent<RectTransform>();
                fillRt.anchorMin = Vector2.zero;
                fillRt.anchorMax = Vector2.one;
                fillRt.offsetMin = Vector2.zero;
                fillRt.offsetMax = Vector2.zero;

                // HeroHpBar-скрипт + bind на Health героя
                var heroHpBar = EnsureComponent<HeroHpBar>(heroHpGo);
                {
                    var so = new SerializedObject(heroHpBar);
                    so.FindProperty("fill").objectReferenceValue = fillI;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                // Bind к Hero.Health в runtime (через Start или прямо сейчас)
                var heroGo     = GameObject.Find("Hero");
                var heroHealth = heroGo != null ? heroGo.GetComponent<Health>() : null;
                if (heroHealth != null)
                    heroHpBar.Bind(heroHealth);
            }

            // --- 4 AbilitySlotUI (нижний правый угол) ---
            var abilitySystem = GetAbilitySystem();

            for (int i = 0; i < 4; i++)
            {
                string slotName = "AbilitySlot_" + (i + 1).ToString();
                var slotGo = EnsureChild(tpsBlock, slotName);

                var rt = slotGo.GetComponent<RectTransform>() ?? slotGo.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(1f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot     = new Vector2(1f, 0f);
                rt.anchoredPosition = new Vector2(-10f - i * 70f, 30f);
                rt.sizeDelta        = new Vector2(60f, 60f);

                // Иконка (фон-заглушка)
                var iconI = EnsureComponent<Image>(slotGo);
                iconI.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

                // Overlay кулдауна
                var overlayGo = EnsureChild(slotGo, "CooldownOverlay");
                var overlayI  = EnsureComponent<Image>(overlayGo);
                overlayI.color      = new Color(0f, 0f, 0f, 0.65f);
                overlayI.type       = Image.Type.Filled;
                overlayI.fillMethod = Image.FillMethod.Vertical;
                overlayI.fillOrigin = (int)Image.OriginVertical.Top;
                overlayI.fillAmount = 0f;

                var overlayRt = overlayGo.GetComponent<RectTransform>() ?? overlayGo.AddComponent<RectTransform>();
                overlayRt.anchorMin = Vector2.zero;
                overlayRt.anchorMax = Vector2.one;
                overlayRt.offsetMin = Vector2.zero;
                overlayRt.offsetMax = Vector2.zero;

                // Клавиша
                var keyGo  = EnsureChild(slotGo, "KeyLabel");
                var keyTmp = EnsureComponent<TextMeshProUGUI>(keyGo);
                keyTmp.text      = (i + 1).ToString();
                keyTmp.fontSize  = 14f;
                keyTmp.alignment = TextAlignmentOptions.BottomRight;
                keyTmp.color     = Color.white;

                var keyRt = keyGo.GetComponent<RectTransform>() ?? keyGo.AddComponent<RectTransform>();
                keyRt.anchorMin = Vector2.zero;
                keyRt.anchorMax = Vector2.one;
                keyRt.offsetMin = new Vector2(2f, 2f);
                keyRt.offsetMax = new Vector2(-2f, -2f);

                // AbilitySlotUI-скрипт
                var slotUi = EnsureComponent<AbilitySlotUI>(slotGo);
                {
                    var so = new SerializedObject(slotUi);
                    so.FindProperty("icon").objectReferenceValue            = iconI;
                    so.FindProperty("cooldownOverlay").objectReferenceValue = overlayI;
                    so.FindProperty("keyLabel").objectReferenceValue        = keyTmp;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                if (abilitySystem != null)
                    slotUi.Bind(abilitySystem, i);
            }
        }

        private static void BuildCrosshairLines(GameObject crosshairGo)
        {
            // 4 полоски: лево, право, вверх, вниз
            var dirs = new (string name, Vector2 anchor, Vector2 size)[]
            {
                ("Left",  new Vector2(-14f,  0f),  new Vector2(10f, 2f)),
                ("Right", new Vector2( 14f,  0f),  new Vector2(10f, 2f)),
                ("Up",    new Vector2(  0f,  14f), new Vector2(2f, 10f)),
                ("Down",  new Vector2(  0f, -14f), new Vector2(2f, 10f)),
            };

            foreach (var (name, pos, size) in dirs)
            {
                var lineGo  = EnsureChild(crosshairGo, name);
                var lineI   = EnsureComponent<Image>(lineGo);
                lineI.color = Color.white;

                var lineRt = lineGo.GetComponent<RectTransform>() ?? lineGo.AddComponent<RectTransform>();
                lineRt.anchorMin        = new Vector2(0.5f, 0.5f);
                lineRt.anchorMax        = new Vector2(0.5f, 0.5f);
                lineRt.pivot            = new Vector2(0.5f, 0.5f);
                lineRt.anchoredPosition = pos;
                lineRt.sizeDelta        = size;
            }
        }

        // ----------------------------------------------------------------
        // OrderMarkerFeedback на GameManagers
        // ----------------------------------------------------------------

        private static void BuildOrderMarkerFeedback()
        {
            var managersGo = EnsureGameObject("GameManagers");

            var moveMat   = EnsureOrderMaterial(OrderMoveMatPath,   new Color(0.2f, 0.9f, 0.2f, 0.8f));
            var attackMat = EnsureOrderMaterial(OrderAttackMatPath,  new Color(0.9f, 0.2f, 0.2f, 0.8f));

            var commandInput = managersGo.GetComponent<CommandInput>();
            var feedback     = EnsureComponent<OrderMarkerFeedback>(managersGo);

            var so = new SerializedObject(feedback);
            so.FindProperty("commandInput").objectReferenceValue    = commandInput;
            so.FindProperty("moveMaterial").objectReferenceValue    = moveMat;
            so.FindProperty("attackMoveMaterial").objectReferenceValue = attackMat;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ----------------------------------------------------------------
        // HealthBar на префабах
        // ----------------------------------------------------------------

        private static void AddHealthBarToPrefab(string prefabPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[Project Forge] Префаб не найден: {prefabPath} — HealthBar пропущен.");
                return;
            }

            // Загружаем содержимое префаба для редактирования
            using (var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
            {
                var root = scope.prefabContentsRoot;
                var existingCanvas = root.transform.Find("HealthBarCanvas");

                // Идемпотентно
                if (existingCanvas != null) return;

                var canvasGo = new GameObject("HealthBarCanvas");
                canvasGo.transform.SetParent(root.transform, false);
                canvasGo.transform.localPosition = new Vector3(0f, 2.2f, 0f);

                var wCanvas = canvasGo.AddComponent<Canvas>();
                wCanvas.renderMode  = RenderMode.WorldSpace;
                wCanvas.worldCamera = null; // камера проставляется в runtime через Camera.main

                var wScaler = canvasGo.AddComponent<CanvasScaler>();
                wScaler.dynamicPixelsPerUnit = 10f;

                var wRt = canvasGo.GetComponent<RectTransform>();
                wRt.sizeDelta = new Vector2(1f, 0.15f);

                // Фон
                var bgGo  = new GameObject("Background");
                bgGo.transform.SetParent(canvasGo.transform, false);
                var bgI   = bgGo.AddComponent<Image>();
                bgI.color = new Color(0.15f, 0.15f, 0.15f, 0.8f);
                var bgRt  = bgGo.GetComponent<RectTransform>();
                bgRt.anchorMin = Vector2.zero;
                bgRt.anchorMax = Vector2.one;
                bgRt.offsetMin = Vector2.zero;
                bgRt.offsetMax = Vector2.zero;

                // Fill
                var fillGo = new GameObject("Fill");
                fillGo.transform.SetParent(canvasGo.transform, false);
                var fillI  = fillGo.AddComponent<Image>();
                fillI.color      = new Color(0.2f, 0.9f, 0.2f, 1f);
                fillI.type       = Image.Type.Filled;
                fillI.fillMethod = Image.FillMethod.Horizontal;
                fillI.fillAmount = 1f;
                var fillRt = fillGo.GetComponent<RectTransform>();
                fillRt.anchorMin = Vector2.zero;
                fillRt.anchorMax = Vector2.one;
                fillRt.offsetMin = Vector2.zero;
                fillRt.offsetMax = Vector2.zero;

                // HealthBar-скрипт
                var healthBar = canvasGo.AddComponent<HealthBar>();
                var so        = new SerializedObject(healthBar);
                so.FindProperty("fill").objectReferenceValue = fillI;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            Debug.Log($"[Project Forge] HealthBar добавлен в префаб: {prefabPath}");
        }

        private static void AddHealthBarToHeroInScene()
        {
            var heroGo = GameObject.Find("Hero");
            if (heroGo == null) return;

            // Идемпотентно
            if (heroGo.transform.Find("HealthBarCanvas") != null) return;

            var canvasGo = new GameObject("HealthBarCanvas");
            canvasGo.transform.SetParent(heroGo.transform, false);
            canvasGo.transform.localPosition = new Vector3(0f, 2.2f, 0f);

            var wCanvas = canvasGo.AddComponent<Canvas>();
            wCanvas.renderMode = RenderMode.WorldSpace;

            var wScaler = canvasGo.AddComponent<CanvasScaler>();
            wScaler.dynamicPixelsPerUnit = 10f;

            var wRt = canvasGo.GetComponent<RectTransform>();
            wRt.sizeDelta = new Vector2(1f, 0.15f);

            // Фон
            var bgGo  = new GameObject("Background");
            bgGo.transform.SetParent(canvasGo.transform, false);
            var bgI   = bgGo.AddComponent<Image>();
            bgI.color = new Color(0.15f, 0.15f, 0.15f, 0.8f);
            var bgRt  = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;

            // Fill
            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(canvasGo.transform, false);
            var fillI  = fillGo.AddComponent<Image>();
            fillI.color      = new Color(0.2f, 0.9f, 0.2f, 1f);
            fillI.type       = Image.Type.Filled;
            fillI.fillMethod = Image.FillMethod.Horizontal;
            fillI.fillAmount = 1f;
            var fillRt = fillGo.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;

            var healthBar = canvasGo.AddComponent<HealthBar>();
            var so        = new SerializedObject(healthBar);
            so.FindProperty("fill").objectReferenceValue = fillI;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ----------------------------------------------------------------
        // EventSystem
        // ----------------------------------------------------------------

        private static void EnsureEventSystem()
        {
            var existing = Object.FindFirstObjectByType<EventSystem>();
            if (existing != null)
            {
                // Убеждаемся, что на EventSystem стоит InputSystemUIInputModule
                EnsureComponent<InputSystemUIInputModule>(existing.gameObject);
                return;
            }

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        // ----------------------------------------------------------------
        // Вспомогательные методы — ассеты
        // ----------------------------------------------------------------

        private static RenderTexture EnsureMinimapRT()
        {
            var existing = AssetDatabase.LoadAssetAtPath<RenderTexture>(MinimapRTPath);
            if (existing != null) return existing;

            EnsureFolder("Assets/_Project/UI");

            var rt = new RenderTexture(256, 256, 16, RenderTextureFormat.ARGB32);
            rt.name         = "MinimapRT";
            rt.filterMode   = FilterMode.Bilinear;
            rt.antiAliasing = 1;
            rt.Create();

            AssetDatabase.CreateAsset(rt, MinimapRTPath);
            AssetDatabase.SaveAssets();

            return rt;
        }

        private static Material EnsureOrderMaterial(string path, Color color)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            EnsureFolder("Assets/_Project/Art/Materials");

            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");
            if (shader == null)
                shader = Shader.Find("Standard");

            var mat = new Material(shader);
            mat.color = color;

            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);

            // Полупрозрачный
            if (mat.HasProperty("_Surface"))
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_Blend",   0f);
                mat.renderQueue = 3000;
            }

            AssetDatabase.CreateAsset(mat, path);
            AssetDatabase.SaveAssets();
            return mat;
        }

        // ----------------------------------------------------------------
        // Вспомогательные методы — сцена
        // ----------------------------------------------------------------

        private static AbilitySystem GetAbilitySystem()
        {
            var heroGo = GameObject.Find("Hero");
            return heroGo != null ? heroGo.GetComponent<AbilitySystem>() : null;
        }

        private static GameObject EnsureGameObject(string goName)
        {
            var existing = GameObject.Find(goName);
            return existing != null ? existing : new GameObject(goName);
        }

        private static T EnsureComponent<T>(GameObject go) where T : Component
        {
            var existing = go.GetComponent<T>();
            return existing != null ? existing : go.AddComponent<T>();
        }

        private static GameObject EnsureChild(GameObject parent, string childName)
        {
            var existing = parent.transform.Find(childName);
            if (existing != null)
                return existing.gameObject;

            var go = new GameObject(childName);
            go.transform.SetParent(parent.transform, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        private static GameObject FindDescendantByName(GameObject root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            foreach (Transform child in root.transform)
            {
                var found = FindDescendantByName(child.gameObject, name);
                if (found != null) return found;
            }
            return null;
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
