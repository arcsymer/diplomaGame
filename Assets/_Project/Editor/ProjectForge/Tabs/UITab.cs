using DiplomaGame.Runtime.Buildings;
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

            GUILayout.Space(8);

            EditorGUILayout.HelpBox(
                "M6b: добавляет в открытую игровую сцену Canvas PauseMenu и Canvas GameOver.\n" +
                "PauseController и GameOverController проставляются через SerializedObject.\n" +
                "Операция идемпотентна.",
                MessageType.Info);

            GUILayout.Space(4);

            if (GUILayout.Button("Build Menus (M6b)", GUILayout.Height(32)))
                BuildMenus();
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

            // Оба блока — full-stretch контейнеры (иначе дочерние элементы
            // позиционируются относительно дефолтного rect 100×100 в центре).
            SetFullStretch(rtsBlock);
            SetFullStretch(tpsBlock);

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
        // M6b: меню паузы и экрана победы/поражения в Sandbox
        // ----------------------------------------------------------------

        /// <summary>
        /// Идемпотентно собирает Canvas PauseMenu и Canvas GameOver в открытой игровой сцене.
        /// </summary>
        internal static void BuildMenus()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                EditorUtility.DisplayDialog("Project Forge", "Нет открытой сцены.", "OK");
                return;
            }

            EnsureTmpEssentials();
            EnsureEventSystem();

            // --- Canvas "PauseMenu" ---
            BuildPauseMenuCanvas();

            // --- Canvas "GameOver" ---
            BuildGameOverCanvas();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Project Forge] Build Menus (M6b) выполнен.");
        }

        private static void BuildPauseMenuCanvas()
        {
            var canvasGo = EnsureGameObject("PauseMenu");
            var canvas   = EnsureComponent<Canvas>(canvasGo);
            canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;

            var scaler = EnsureComponent<CanvasScaler>(canvasGo);
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight  = 0.5f;

            EnsureComponent<GraphicRaycaster>(canvasGo);

            // --- Панель паузы (скрыта по умолчанию) ---
            var panelGo = EnsureChild(canvasGo, "Panel");
            {
                var panelI = EnsureComponent<Image>(panelGo);
                panelI.color = new Color(0f, 0f, 0f, 0.75f);

                var rt = panelGo.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                // Заголовок «Пауза»
                var titleGo  = EnsureChild(panelGo, "Title");
                var titleTmp = EnsureComponent<TextMeshProUGUI>(titleGo);
                titleTmp.text      = "ПАУЗА";
                titleTmp.fontSize  = 60f;
                titleTmp.alignment = TextAlignmentOptions.Center;
                titleTmp.color     = Color.white;
                {
                    var trt = titleGo.GetComponent<RectTransform>();
                    trt.anchorMin       = new Vector2(0.5f, 1f);
                    trt.anchorMax       = new Vector2(0.5f, 1f);
                    trt.pivot           = new Vector2(0.5f, 1f);
                    trt.anchoredPosition = new Vector2(0f, -120f);
                    trt.sizeDelta        = new Vector2(400f, 80f);
                }

                // Столбец кнопок
                var buttonsGo = EnsureChild(panelGo, "Buttons");
                {
                    var brt = buttonsGo.GetComponent<RectTransform>();
                    brt.anchorMin        = new Vector2(0.5f, 0.5f);
                    brt.anchorMax        = new Vector2(0.5f, 0.5f);
                    brt.pivot            = new Vector2(0.5f, 0.5f);
                    brt.anchoredPosition = Vector2.zero;
                    brt.sizeDelta        = new Vector2(300f, 300f);

                    var layout = EnsureComponent<UnityEngine.UI.VerticalLayoutGroup>(buttonsGo);
                    layout.spacing              = 16f;
                    layout.childAlignment       = TextAnchor.MiddleCenter;
                    layout.childControlWidth    = true;
                    layout.childControlHeight   = false;
                    layout.childForceExpandWidth = true;
                    layout.childForceExpandHeight = false;
                }

                // Кнопки
                EnsurePauseButton(buttonsGo, "BtnContinue",  "Продолжить");
                EnsurePauseButton(buttonsGo, "BtnSettings",  "Настройки");
                EnsurePauseButton(buttonsGo, "BtnExitMenu",  "Выйти в меню");
                EnsurePauseButton(buttonsGo, "BtnQuit",      "Выйти из игры");
            }

            // --- Вложенная SettingsPanel (скрыта) ---
            var settingsPanelGo = EnsureChild(canvasGo, "SettingsPanel");
            {
                var spI = EnsureComponent<Image>(settingsPanelGo);
                spI.color = new Color(0.05f, 0.05f, 0.08f, 0.95f);

                var rt = settingsPanelGo.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                BuildSettingsPanelContent(settingsPanelGo);
                settingsPanelGo.SetActive(false);
            }

            // --- PauseController на корне Canvas ---
            var pauseCtrl = EnsureComponent<PauseController>(canvasGo);
            {
                var so          = new SerializedObject(pauseCtrl);
                var managersGo  = GameObject.Find("GameManagers");
                var placer      = managersGo != null ? managersGo.GetComponent<BuildingPlacer>() : null;
                var settingsComp = settingsPanelGo.GetComponent<SettingsPanel>();

                so.FindProperty("pausePanel").objectReferenceValue    = panelGo;
                so.FindProperty("settingsPanel").objectReferenceValue = settingsComp;
                so.FindProperty("buildingPlacer").objectReferenceValue = placer;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // Скрываем панель паузы по умолчанию
            panelGo.SetActive(false);

            // Подключаем onClick кнопок
            WireUpPauseButtons(panelGo, settingsPanelGo, canvasGo);
        }

        private static void WireUpPauseButtons(GameObject panelGo, GameObject settingsPanelGo, GameObject canvasGo)
        {
            var pauseCtrl = canvasGo.GetComponent<PauseController>();
            if (pauseCtrl == null) return;

            var buttonsGo = panelGo.transform.Find("Buttons");
            if (buttonsGo == null) return;

            WireButton(buttonsGo.gameObject, "BtnContinue", pauseCtrl,
                nameof(PauseController.OnContinueClicked));
            WireButton(buttonsGo.gameObject, "BtnSettings", pauseCtrl,
                nameof(PauseController.OnSettingsClicked));
            WireButton(buttonsGo.gameObject, "BtnExitMenu", pauseCtrl,
                nameof(PauseController.OnExitToMenuClicked));
            WireButton(buttonsGo.gameObject, "BtnQuit", pauseCtrl,
                nameof(PauseController.OnQuitClicked));
        }

        private static void WireButton(GameObject parent, string childName,
            UnityEngine.Component target, string methodName)
        {
            var child = FindDescendantByName(parent, childName);
            if (child == null) return;

            var btn = child.GetComponent<Button>();
            if (btn == null) return;

            // Используем SerializedObject для записи PersistentListener
            var so           = new SerializedObject(btn);
            var onClickProp  = so.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
            if (onClickProp == null) return;

            // Идемпотентно: очищаем и ставим один вызов
            onClickProp.ClearArray();
            onClickProp.InsertArrayElementAtIndex(0);

            var call = onClickProp.GetArrayElementAtIndex(0);
            call.FindPropertyRelative("m_Target").objectReferenceValue  = target;
            call.FindPropertyRelative("m_MethodName").stringValue       = methodName;
            call.FindPropertyRelative("m_Mode").intValue                = 1; // EventDefined
            call.FindPropertyRelative("m_CallState").intValue           = 2; // RuntimeOnly

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void EnsurePauseButton(GameObject parent, string name, string label)
        {
            var go = EnsureChild(parent, name);
            {
                var rt = go.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(0f, 60f);
            }

            var bg = EnsureComponent<Image>(go);
            bg.color = new Color(0.15f, 0.15f, 0.2f, 1f);

            EnsureComponent<Button>(go);

            var labelGo  = EnsureChild(go, "Label");
            var labelTmp = EnsureComponent<TextMeshProUGUI>(labelGo);
            labelTmp.text      = label;
            labelTmp.fontSize  = 24f;
            labelTmp.alignment = TextAlignmentOptions.Center;
            labelTmp.color     = Color.white;
            {
                var lrt = labelGo.GetComponent<RectTransform>();
                lrt.anchorMin = Vector2.zero;
                lrt.anchorMax = Vector2.one;
                lrt.offsetMin = Vector2.zero;
                lrt.offsetMax = Vector2.zero;
            }
        }

        private static void BuildSettingsPanelContent(GameObject panelGo)
        {
            // Заголовок
            var titleGo  = EnsureChild(panelGo, "Title");
            var titleTmp = EnsureComponent<TextMeshProUGUI>(titleGo);
            titleTmp.text      = "НАСТРОЙКИ";
            titleTmp.fontSize  = 40f;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color     = Color.white;
            {
                var trt = titleGo.GetComponent<RectTransform>();
                trt.anchorMin        = new Vector2(0.5f, 1f);
                trt.anchorMax        = new Vector2(0.5f, 1f);
                trt.pivot            = new Vector2(0.5f, 1f);
                trt.anchoredPosition = new Vector2(0f, -60f);
                trt.sizeDelta        = new Vector2(400f, 60f);
            }

            // Контейнер настроек
            var contentGo = EnsureChild(panelGo, "Content");
            {
                var crt = contentGo.GetComponent<RectTransform>();
                crt.anchorMin        = new Vector2(0.5f, 0.5f);
                crt.anchorMax        = new Vector2(0.5f, 0.5f);
                crt.pivot            = new Vector2(0.5f, 0.5f);
                crt.anchoredPosition = new Vector2(0f, 20f);
                crt.sizeDelta        = new Vector2(500f, 500f);

                var layout = EnsureComponent<VerticalLayoutGroup>(contentGo);
                layout.spacing               = 12f;
                layout.childAlignment        = TextAnchor.UpperCenter;
                layout.childControlWidth     = true;
                layout.childControlHeight    = false;
                layout.childForceExpandWidth  = true;
                layout.childForceExpandHeight = false;
                layout.padding               = new RectOffset(20, 20, 0, 0);
            }

            // Качество
            var qualityRow = EnsureChild(contentGo, "QualityRow");
            SetRowSize(qualityRow, 50f);
            EnsureRowLabel(qualityRow, "Качество");
            var qualityDropdown = EnsureComponent<TMP_Dropdown>(EnsureChild(qualityRow, "Dropdown"));
            SetControlRectRight(qualityDropdown.gameObject, 220f, 40f);

            // Полный экран
            var fsRow = EnsureChild(contentGo, "FullscreenRow");
            SetRowSize(fsRow, 50f);
            EnsureRowLabel(fsRow, "Полный экран");
            var fsToggle = EnsureComponent<Toggle>(EnsureChild(fsRow, "Toggle"));
            SetControlRectRight(fsToggle.gameObject, 40f, 40f);
            EnsureToggleGraphics(fsToggle.gameObject);

            // Громкость мастер
            var masterRow = EnsureChild(contentGo, "MasterVolumeRow");
            SetRowSize(masterRow, 50f);
            EnsureRowLabel(masterRow, "Громкость");
            var masterSlider = EnsureComponent<Slider>(EnsureChild(masterRow, "Slider"));
            SetupSlider(masterSlider, 0f, 1f, 1f);
            SetControlRectRight(masterSlider.gameObject, 220f, 24f);

            // Музыка
            var musicRow = EnsureChild(contentGo, "MusicVolumeRow");
            SetRowSize(musicRow, 50f);
            EnsureRowLabel(musicRow, "Музыка");
            var musicSlider = EnsureComponent<Slider>(EnsureChild(musicRow, "Slider"));
            SetupSlider(musicSlider, 0f, 1f, 1f);
            SetControlRectRight(musicSlider.gameObject, 220f, 24f);

            // SFX
            var sfxRow = EnsureChild(contentGo, "SfxVolumeRow");
            SetRowSize(sfxRow, 50f);
            EnsureRowLabel(sfxRow, "Эффекты");
            var sfxSlider = EnsureComponent<Slider>(EnsureChild(sfxRow, "Slider"));
            SetupSlider(sfxSlider, 0f, 1f, 1f);
            SetControlRectRight(sfxSlider.gameObject, 220f, 24f);

            // Чувствительность
            var sensRow = EnsureChild(contentGo, "SensitivityRow");
            SetRowSize(sensRow, 50f);
            EnsureRowLabel(sensRow, "Чувствительность");
            var sensSlider = EnsureComponent<Slider>(EnsureChild(sensRow, "Slider"));
            SetupSlider(sensSlider, 0.01f, 1f, 0.15f);
            SetControlRectRight(sensSlider.gameObject, 220f, 24f);

            // Кнопка «Назад»
            var backGo = EnsureChild(panelGo, "BtnBack");
            {
                var brt = backGo.GetComponent<RectTransform>();
                brt.anchorMin        = new Vector2(0.5f, 0f);
                brt.anchorMax        = new Vector2(0.5f, 0f);
                brt.pivot            = new Vector2(0.5f, 0f);
                brt.anchoredPosition = new Vector2(0f, 40f);
                brt.sizeDelta        = new Vector2(200f, 50f);
            }
            var backBg  = EnsureComponent<Image>(backGo);
            backBg.color = new Color(0.15f, 0.15f, 0.2f, 1f);
            EnsureComponent<Button>(backGo);
            var backLabel = EnsureChild(backGo, "Label");
            var backTmp   = EnsureComponent<TextMeshProUGUI>(backLabel);
            backTmp.text      = "Назад";
            backTmp.fontSize  = 22f;
            backTmp.alignment = TextAlignmentOptions.Center;
            backTmp.color     = Color.white;
            {
                var lrt = backLabel.GetComponent<RectTransform>();
                lrt.anchorMin = Vector2.zero;
                lrt.anchorMax = Vector2.one;
                lrt.offsetMin = Vector2.zero;
                lrt.offsetMax = Vector2.zero;
            }

            // SettingsPanel-компонент с привязками
            var settingsComp = EnsureComponent<SettingsPanel>(panelGo);
            {
                var so = new SerializedObject(settingsComp);
                so.FindProperty("qualityDropdown").objectReferenceValue    = qualityDropdown;
                so.FindProperty("fullscreenToggle").objectReferenceValue   = fsToggle;
                so.FindProperty("masterVolumeSlider").objectReferenceValue = masterSlider;
                so.FindProperty("musicVolumeSlider").objectReferenceValue  = musicSlider;
                so.FindProperty("sfxVolumeSlider").objectReferenceValue    = sfxSlider;
                so.FindProperty("sensitivitySlider").objectReferenceValue  = sensSlider;
                so.FindProperty("backButton").objectReferenceValue         = backGo.GetComponent<Button>();
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetRowSize(GameObject row, float height)
        {
            var rt = row.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, height);
        }

        private static void EnsureRowLabel(GameObject row, string text)
        {
            var labelGo  = EnsureChild(row, "Label");
            var labelTmp = EnsureComponent<TextMeshProUGUI>(labelGo);
            labelTmp.text      = text;
            labelTmp.fontSize  = 20f;
            labelTmp.alignment = TextAlignmentOptions.MidlineLeft;
            labelTmp.color     = Color.white;

            var lrt = labelGo.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0f, 0f);
            lrt.anchorMax = new Vector2(0.5f, 1f);
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
        }

        private static void SetControlRectRight(GameObject go, float width, float height)
        {
            var rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
            rt.anchorMin        = new Vector2(1f, 0.5f);
            rt.anchorMax        = new Vector2(1f, 0.5f);
            rt.pivot            = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(-10f, 0f);
            rt.sizeDelta        = new Vector2(width, height);
        }

        private static void SetupSlider(Slider slider, float min, float max, float value)
        {
            slider.minValue = min;
            slider.maxValue = max;
            slider.value    = value;
            slider.wholeNumbers = false;

            // Добавляем минимальную графику для слайдера, если её нет
            EnsureSliderGraphics(slider.gameObject);
        }

        private static void EnsureSliderGraphics(GameObject sliderGo)
        {
            // Background
            var bgGo = EnsureChild(sliderGo, "Background");
            var bgI  = EnsureComponent<Image>(bgGo);
            bgI.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0f, 0.25f);
            bgRt.anchorMax = new Vector2(1f, 0.75f);
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;

            // Fill Area
            var fillAreaGo = EnsureChild(sliderGo, "Fill Area");
            var fillAreaRt = fillAreaGo.GetComponent<RectTransform>();
            fillAreaRt.anchorMin = new Vector2(0f, 0.25f);
            fillAreaRt.anchorMax = new Vector2(1f, 0.75f);
            fillAreaRt.offsetMin = new Vector2(5f, 0f);
            fillAreaRt.offsetMax = new Vector2(-15f, 0f);

            var fillGo = EnsureChild(fillAreaGo, "Fill");
            var fillI  = EnsureComponent<Image>(fillGo);
            fillI.color = new Color(0.2f, 0.6f, 1f, 1f);
            var fillRt = fillGo.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = new Vector2(0f, 1f);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = new Vector2(10f, 0f);

            // Handle Slide Area
            var handleAreaGo = EnsureChild(sliderGo, "Handle Slide Area");
            var handleAreaRt = handleAreaGo.GetComponent<RectTransform>();
            handleAreaRt.anchorMin = new Vector2(0f, 0f);
            handleAreaRt.anchorMax = new Vector2(1f, 1f);
            handleAreaRt.offsetMin = new Vector2(10f, 0f);
            handleAreaRt.offsetMax = new Vector2(-10f, 0f);

            var handleGo = EnsureChild(handleAreaGo, "Handle");
            var handleI  = EnsureComponent<Image>(handleGo);
            handleI.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            var handleRt = handleGo.GetComponent<RectTransform>();
            handleRt.anchorMin        = new Vector2(0f, 0f);
            handleRt.anchorMax        = new Vector2(0f, 1f);
            handleRt.sizeDelta        = new Vector2(20f, 0f);
            handleRt.anchoredPosition = Vector2.zero;

            // Связываем Slider
            var slider = sliderGo.GetComponent<Slider>();
            if (slider != null)
            {
                var so = new SerializedObject(slider);
                so.FindProperty("m_FillRect").objectReferenceValue   = fillRt;
                so.FindProperty("m_HandleRect").objectReferenceValue = handleRt;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void EnsureToggleGraphics(GameObject toggleGo)
        {
            var bgGo = EnsureChild(toggleGo, "Background");
            var bgI  = EnsureComponent<Image>(bgGo);
            bgI.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;

            var checkGo = EnsureChild(bgGo, "Checkmark");
            var checkI  = EnsureComponent<Image>(checkGo);
            checkI.color = new Color(0.2f, 0.8f, 0.2f, 1f);
            var checkRt = checkGo.GetComponent<RectTransform>();
            checkRt.anchorMin = new Vector2(0.1f, 0.1f);
            checkRt.anchorMax = new Vector2(0.9f, 0.9f);
            checkRt.offsetMin = Vector2.zero;
            checkRt.offsetMax = Vector2.zero;

            var toggle = toggleGo.GetComponent<Toggle>();
            if (toggle != null)
            {
                var so = new SerializedObject(toggle);
                // Toggle.graphic — публичное поле, сериализуется без префикса m_
                so.FindProperty("graphic").objectReferenceValue = checkI;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void BuildGameOverCanvas()
        {
            var canvasGo = EnsureGameObject("GameOver");
            var canvas   = EnsureComponent<Canvas>(canvasGo);
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 60;

            var scaler = EnsureComponent<CanvasScaler>(canvasGo);
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight  = 0.5f;

            EnsureComponent<GraphicRaycaster>(canvasGo);

            // --- Панель Победы ---
            var victoryGo = EnsureChild(canvasGo, "VictoryPanel");
            {
                var bg = EnsureComponent<Image>(victoryGo);
                bg.color = new Color(0f, 0f, 0f, 0.8f);
                var rt = victoryGo.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                var titleGo  = EnsureChild(victoryGo, "Title");
                var titleTmp = EnsureComponent<TextMeshProUGUI>(titleGo);
                titleTmp.text      = "ПОБЕДА";
                titleTmp.fontSize  = 80f;
                titleTmp.alignment = TextAlignmentOptions.Center;
                titleTmp.color     = new Color(0.2f, 1f, 0.2f, 1f);
                {
                    var trt = titleGo.GetComponent<RectTransform>();
                    trt.anchorMin        = new Vector2(0.5f, 0.5f);
                    trt.anchorMax        = new Vector2(0.5f, 0.5f);
                    trt.pivot            = new Vector2(0.5f, 0.5f);
                    trt.anchoredPosition = new Vector2(0f, 80f);
                    trt.sizeDelta        = new Vector2(600f, 100f);
                }

                BuildGameOverButtons(victoryGo, false);
                victoryGo.SetActive(false);
            }

            // --- Панель Поражения ---
            var defeatGo = EnsureChild(canvasGo, "DefeatPanel");
            {
                var bg = EnsureComponent<Image>(defeatGo);
                bg.color = new Color(0f, 0f, 0f, 0.8f);
                var rt = defeatGo.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                var titleGo  = EnsureChild(defeatGo, "Title");
                var titleTmp = EnsureComponent<TextMeshProUGUI>(titleGo);
                titleTmp.text      = "ПОРАЖЕНИЕ";
                titleTmp.fontSize  = 80f;
                titleTmp.alignment = TextAlignmentOptions.Center;
                titleTmp.color     = new Color(1f, 0.2f, 0.2f, 1f);
                {
                    var trt = titleGo.GetComponent<RectTransform>();
                    trt.anchorMin        = new Vector2(0.5f, 0.5f);
                    trt.anchorMax        = new Vector2(0.5f, 0.5f);
                    trt.pivot            = new Vector2(0.5f, 0.5f);
                    trt.anchoredPosition = new Vector2(0f, 80f);
                    trt.sizeDelta        = new Vector2(600f, 100f);
                }

                BuildGameOverButtons(defeatGo, true);
                defeatGo.SetActive(false);
            }

            // --- GameOverController ---
            var gameOverCtrl = EnsureComponent<GameOverController>(canvasGo);
            {
                var so = new SerializedObject(gameOverCtrl);
                so.FindProperty("victoryPanel").objectReferenceValue = victoryGo;
                so.FindProperty("defeatPanel").objectReferenceValue  = defeatGo;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // Подключаем кнопки
            WireGameOverButtons(victoryGo, defeatGo, gameOverCtrl);
        }

        private static void BuildGameOverButtons(GameObject panelGo, bool showRestart)
        {
            var buttonsGo = EnsureChild(panelGo, "Buttons");
            {
                var brt = buttonsGo.GetComponent<RectTransform>();
                brt.anchorMin        = new Vector2(0.5f, 0.5f);
                brt.anchorMax        = new Vector2(0.5f, 0.5f);
                brt.pivot            = new Vector2(0.5f, 0.5f);
                brt.anchoredPosition = new Vector2(0f, -60f);
                brt.sizeDelta        = new Vector2(280f, 160f);

                var layout = EnsureComponent<VerticalLayoutGroup>(buttonsGo);
                layout.spacing               = 16f;
                layout.childAlignment        = TextAnchor.MiddleCenter;
                layout.childControlWidth     = true;
                layout.childControlHeight    = false;
                layout.childForceExpandWidth  = true;
                layout.childForceExpandHeight = false;
            }

            EnsurePauseButton(buttonsGo, "BtnRestart",  "Заново");
            EnsurePauseButton(buttonsGo, "BtnExitMenu", "Выйти в меню");
        }

        private static void WireGameOverButtons(GameObject victoryGo, GameObject defeatGo,
            GameOverController ctrl)
        {
            foreach (var panelGo in new[] { victoryGo, defeatGo })
            {
                var buttonsGo = FindDescendantByName(panelGo, "Buttons");
                if (buttonsGo == null) continue;

                WireButton(buttonsGo, "BtnRestart",  ctrl, nameof(GameOverController.OnRestartClicked));
                WireButton(buttonsGo, "BtnExitMenu", ctrl, nameof(GameOverController.OnExitToMenuClicked));
            }
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
                rt.sizeDelta = new Vector2(0f, 44f);

                var bg = EnsureComponent<Image>(topPanel);
                bg.color = new Color(0f, 0f, 0f, 0.55f);

                // ResourceDisplay label
                var resGo  = EnsureChild(topPanel, "ResourceText");
                var resTmp = EnsureComponent<TextMeshProUGUI>(resGo);
                resTmp.text                = "Crystals: 150";
                resTmp.fontSize            = 22f;
                resTmp.alignment           = TextAlignmentOptions.MidlineLeft;
                resTmp.color               = Color.white;
                resTmp.enableWordWrapping  = false;

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
                rt.anchorMin = new Vector2(0.25f, 0f);
                rt.anchorMax = new Vector2(0.75f, 0f);
                rt.pivot     = new Vector2(0.5f, 0f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                rt.sizeDelta = new Vector2(0f, 90f);

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
                rt.anchoredPosition = new Vector2(12f, 12f);
                rt.sizeDelta        = new Vector2(280f, 80f);

                var keysText = EnsureComponent<TextMeshProUGUI>(keysHint);
                keysText.text      = "Tab — режим    B/E — строить\nT — обучить    H — стоять";
                keysText.fontSize  = 16f;
                keysText.color     = new Color(1f, 1f, 1f, 0.5f);
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
            drect.anchoredPosition = new Vector2(-12f, 12f);
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
                rt.anchoredPosition = new Vector2(16f, 16f);
                rt.sizeDelta        = new Vector2(280f, 22f);

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
                rt.anchoredPosition = new Vector2(-16f - i * 62f, 16f);
                rt.sizeDelta        = new Vector2(56f, 56f);

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

        /// <summary>
        /// Задаёт RectTransform full-stretch (anchorMin=0,0 anchorMax=1,1 pivot=0.5,0.5 offset=0).
        /// Вызывается для корневых блоков-контейнеров, чтобы они покрывали весь Canvas,
        /// а дочерние элементы позиционировались относительно полного экрана.
        /// </summary>
        private static void SetFullStretch(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();
            rt.anchorMin  = Vector2.zero;
            rt.anchorMax  = Vector2.one;
            rt.pivot      = new Vector2(0.5f, 0.5f);
            rt.offsetMin  = Vector2.zero;
            rt.offsetMax  = Vector2.zero;
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
