using System.Collections.Generic;
using System.IO;
using System.Linq;
using DiplomaGame.Runtime.Economy;
using DiplomaGame.Runtime.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DiplomaGame.Editor
{
    /// <summary>
    /// Управление сценами проекта. Все операции идемпотентны.
    /// </summary>
    internal sealed class ScenesTab : IForgeTab
    {
        private const string SandboxScenePath  = "Assets/_Project/Scenes/Sandbox.unity";
        private const string MainMenuScenePath = "Assets/_Project/Scenes/MainMenu.unity";

        public string Title => "Scenes";

        public void OnGUI()
        {
            GUILayout.Space(8);
            GUILayout.Label("Управление сценами", EditorStyles.boldLabel);
            GUILayout.Space(4);

            if (GUILayout.Button("Create/Update Sandbox Scene", GUILayout.Height(32)))
                CreateOrUpdateSandboxScene();

            GUILayout.Space(4);

            if (GUILayout.Button("Create/Update MainMenu Scene", GUILayout.Height(32)))
                CreateOrUpdateMainMenuScene();

            GUILayout.Space(8);

            EditorGUILayout.HelpBox(
                "v9: Переставляет маркеры баз (±35), здания, расставляет 6 скал-чокпоинт\n" +
                "(x=±8, z=+2/0/-2), 2 экспанд-ноды (±12/±18, reserve=2000),\n" +
                "15 объектов декора (без коллайдеров) и перезапекает NavMesh.\n" +
                "Требует открытой сцены Sandbox. Идемпотентно.",
                MessageType.Info);

            GUILayout.Space(4);

            if (GUILayout.Button("Rebuild Map Layout (v9)", GUILayout.Height(32)))
                RebuildMapLayout();
        }

        /// <summary>
        /// Идемпотентно создаёт или обновляет сцену Sandbox:
        /// Plane, Directional Light, два маркера спавна.
        /// </summary>
        internal static void CreateOrUpdateSandboxScene()
        {
            // Сохраняем текущую открытую сцену, чтобы вернуться к ней
            var originalScene = SceneManager.GetActiveScene();
            bool shouldReturnToOriginal = originalScene.IsValid() && !string.IsNullOrEmpty(originalScene.path);

            // Создаём папку Scenes, если её нет
            const string scenesDir = "Assets/_Project/Scenes";
            if (!AssetDatabase.IsValidFolder(scenesDir))
                AssetDatabase.CreateFolder("Assets/_Project", "Scenes");

            // Открываем или создаём сцену
            Scene sandbox;
            if (File.Exists(SandboxScenePath))
            {
                sandbox = EditorSceneManager.OpenScene(SandboxScenePath, OpenSceneMode.Single);
            }
            else
            {
                sandbox = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }

            // --- Ground (Plane 100×100) ---
            EnsurePlane();

            // --- Directional Light "Sun" ---
            EnsureDirectionalLight();

            // --- Маркеры спавна ---
            EnsureMarker("PlayerBaseSpawn", new Vector3(-30f, 0f, -30f));
            EnsureMarker("EnemyBaseSpawn",  new Vector3(30f,  0f,  30f));

            // Сохраняем сцену
            EditorSceneManager.SaveScene(sandbox, SandboxScenePath);
            AssetDatabase.Refresh();

            // Добавляем в Build Settings без дублей
            AddSceneToBuildSettings(SandboxScenePath);

            Debug.Log("[Project Forge] Sandbox scene создана/обновлена: " + SandboxScenePath);
        }

        // ----------------------------------------------------------------
        // CreateOrUpdateMainMenuScene
        // ----------------------------------------------------------------

        /// <summary>
        /// Идемпотентно создаёт или обновляет сцену MainMenu:
        /// камера, Canvas, заголовок, кнопки Play/Settings/Quit, SettingsPanel.
        /// Build Settings: MainMenu — индекс 0, Sandbox — индекс 1 (перезаписывает полностью).
        /// </summary>
        internal static void CreateOrUpdateMainMenuScene()
        {
            const string scenesDir = "Assets/_Project/Scenes";
            if (!AssetDatabase.IsValidFolder(scenesDir))
                AssetDatabase.CreateFolder("Assets/_Project", "Scenes");

            // Открываем или создаём сцену MainMenu
            Scene mainMenu;
            if (File.Exists(MainMenuScenePath))
                mainMenu = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Single);
            else
                mainMenu = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // --- TMP Essentials ---
            if (TMP_Settings.instance == null)
            {
                const string pkgPath =
                    "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage";
                AssetDatabase.ImportPackage(pkgPath, false);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }

            // --- Камера ---
            EnsureMainCamera();

            // --- EventSystem ---
            EnsureMainMenuEventSystem();

            // --- Canvas "MainMenu" ---
            BuildMainMenuCanvas();

            // Сохраняем
            EditorSceneManager.SaveScene(mainMenu, MainMenuScenePath);
            AssetDatabase.Refresh();

            // --- Build Settings: MainMenu[0], Sandbox[1] ---
            RewriteBuildSettings();

            Debug.Log("[Project Forge] MainMenu scene создана/обновлена: " + MainMenuScenePath);
        }

        private static void EnsureMainCamera()
        {
            const string camName = "Main Camera";
            var existing = GameObject.Find(camName);
            if (existing == null)
            {
                existing = new GameObject(camName);
                existing.tag = "MainCamera";
                existing.AddComponent<Camera>();
                existing.AddComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
            }

            var cam = existing.GetComponent<Camera>();
            if (cam == null) cam = existing.AddComponent<Camera>();
            cam.clearFlags      = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.05f, 0.08f, 1f);
            cam.orthographic    = false;
            existing.transform.position = new Vector3(0f, 0f, -10f);
        }

        private static void EnsureMainMenuEventSystem()
        {
            var existing = Object.FindFirstObjectByType<EventSystem>();
            if (existing != null)
            {
                EnsureSceneComponent<InputSystemUIInputModule>(existing.gameObject);
                return;
            }

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        private static T EnsureSceneComponent<T>(GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }

        private static void BuildMainMenuCanvas()
        {
            var canvasGo = EnsureSceneObject("MainMenu");
            var canvas   = EnsureSceneComponent<Canvas>(canvasGo);
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            var scaler = EnsureSceneComponent<CanvasScaler>(canvasGo);
            scaler.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight  = 0.5f;

            EnsureSceneComponent<GraphicRaycaster>(canvasGo);

            // --- Заголовок ---
            var titleGo  = EnsureSceneChild(canvasGo, "Title");
            var titleTmp = EnsureSceneComponent<TextMeshProUGUI>(titleGo);
            titleTmp.text      = "DIPLOMA GAME";
            titleTmp.fontSize  = 80f;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color     = Color.white;
            {
                var trt = titleGo.GetComponent<RectTransform>();
                trt.anchorMin        = new Vector2(0.5f, 1f);
                trt.anchorMax        = new Vector2(0.5f, 1f);
                trt.pivot            = new Vector2(0.5f, 1f);
                trt.anchoredPosition = new Vector2(0f, -80f);
                trt.sizeDelta        = new Vector2(800f, 100f);
            }

            // --- Вертикальный столбец кнопок ---
            var buttonsGo = EnsureSceneChild(canvasGo, "Buttons");
            {
                var brt = buttonsGo.GetComponent<RectTransform>();
                brt.anchorMin        = new Vector2(0.5f, 0.5f);
                brt.anchorMax        = new Vector2(0.5f, 0.5f);
                brt.pivot            = new Vector2(0.5f, 0.5f);
                brt.anchoredPosition = new Vector2(0f, -40f);
                brt.sizeDelta        = new Vector2(300f, 240f);

                var layout = EnsureSceneComponent<VerticalLayoutGroup>(buttonsGo);
                layout.spacing               = 20f;
                layout.childAlignment        = TextAnchor.MiddleCenter;
                layout.childControlWidth     = true;
                layout.childControlHeight    = false;
                layout.childForceExpandWidth  = true;
                layout.childForceExpandHeight = false;
            }

            EnsureMainMenuButton(buttonsGo, "BtnPlay",     "Играть");
            EnsureMainMenuButton(buttonsGo, "BtnSettings", "Настройки");
            EnsureMainMenuButton(buttonsGo, "BtnQuit",     "Выйти");

            // --- SettingsPanel (скрыта, поверх) ---
            var settingsPanelGo = EnsureSceneChild(canvasGo, "SettingsPanel");
            {
                var spI = EnsureSceneComponent<Image>(settingsPanelGo);
                spI.color = new Color(0.05f, 0.05f, 0.08f, 0.97f);

                var rt = settingsPanelGo.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;

                // Строим содержимое (повторно использует логику UITab через прямую сборку)
                BuildMainMenuSettingsContent(settingsPanelGo);
                settingsPanelGo.SetActive(false);
            }

            // --- MainMenuController ---
            var menuCtrl = EnsureSceneComponent<MainMenuController>(canvasGo);
            {
                var so = new SerializedObject(menuCtrl);
                so.FindProperty("settingsPanel").objectReferenceValue =
                    settingsPanelGo.GetComponent<SettingsPanel>();
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // Подключаем кнопки меню
            WireMainMenuButtons(buttonsGo, canvasGo);
        }

        private static void BuildMainMenuSettingsContent(GameObject panelGo)
        {
            // Заголовок
            var titleGo  = EnsureSceneChild(panelGo, "Title");
            var titleTmp = EnsureSceneComponent<TextMeshProUGUI>(titleGo);
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

            // Контент
            var contentGo = EnsureSceneChild(panelGo, "Content");
            {
                var crt = contentGo.GetComponent<RectTransform>();
                crt.anchorMin        = new Vector2(0.5f, 0.5f);
                crt.anchorMax        = new Vector2(0.5f, 0.5f);
                crt.pivot            = new Vector2(0.5f, 0.5f);
                crt.anchoredPosition = new Vector2(0f, 20f);
                crt.sizeDelta        = new Vector2(500f, 500f);

                var layout = EnsureSceneComponent<VerticalLayoutGroup>(contentGo);
                layout.spacing               = 12f;
                layout.childAlignment        = TextAnchor.UpperCenter;
                layout.childControlWidth     = true;
                layout.childControlHeight    = false;
                layout.childForceExpandWidth  = true;
                layout.childForceExpandHeight = false;
                layout.padding               = new RectOffset(20, 20, 0, 0);
            }

            // Строки настроек (качество, полный экран, громкости, чувствительность)
            var qualityRow = EnsureSceneChild(contentGo, "QualityRow");
            SetSceneRowSize(qualityRow, 50f);
            EnsureSceneRowLabel(qualityRow, "Качество");
            var qualityDropdown = EnsureSceneComponent<TMP_Dropdown>(EnsureSceneChild(qualityRow, "Dropdown"));
            SetSceneControlRectRight(qualityDropdown.gameObject, 220f, 40f);

            var fsRow = EnsureSceneChild(contentGo, "FullscreenRow");
            SetSceneRowSize(fsRow, 50f);
            EnsureSceneRowLabel(fsRow, "Полный экран");
            var fsToggle = EnsureSceneComponent<Toggle>(EnsureSceneChild(fsRow, "Toggle"));
            SetSceneControlRectRight(fsToggle.gameObject, 40f, 40f);
            EnsureSceneToggleGraphics(fsToggle.gameObject);

            var masterRow = EnsureSceneChild(contentGo, "MasterVolumeRow");
            SetSceneRowSize(masterRow, 50f);
            EnsureSceneRowLabel(masterRow, "Громкость");
            var masterSlider = EnsureSceneComponent<Slider>(EnsureSceneChild(masterRow, "Slider"));
            SetupSceneSlider(masterSlider, 0f, 1f, 1f);
            SetSceneControlRectRight(masterSlider.gameObject, 220f, 24f);

            var musicRow = EnsureSceneChild(contentGo, "MusicVolumeRow");
            SetSceneRowSize(musicRow, 50f);
            EnsureSceneRowLabel(musicRow, "Музыка");
            var musicSlider = EnsureSceneComponent<Slider>(EnsureSceneChild(musicRow, "Slider"));
            SetupSceneSlider(musicSlider, 0f, 1f, 1f);
            SetSceneControlRectRight(musicSlider.gameObject, 220f, 24f);

            var sfxRow = EnsureSceneChild(contentGo, "SfxVolumeRow");
            SetSceneRowSize(sfxRow, 50f);
            EnsureSceneRowLabel(sfxRow, "Эффекты");
            var sfxSlider = EnsureSceneComponent<Slider>(EnsureSceneChild(sfxRow, "Slider"));
            SetupSceneSlider(sfxSlider, 0f, 1f, 1f);
            SetSceneControlRectRight(sfxSlider.gameObject, 220f, 24f);

            var sensRow = EnsureSceneChild(contentGo, "SensitivityRow");
            SetSceneRowSize(sensRow, 50f);
            EnsureSceneRowLabel(sensRow, "Чувствительность");
            var sensSlider = EnsureSceneComponent<Slider>(EnsureSceneChild(sensRow, "Slider"));
            SetupSceneSlider(sensSlider, 0.01f, 1f, 0.15f);
            SetSceneControlRectRight(sensSlider.gameObject, 220f, 24f);

            // Кнопка «Назад»
            var backGo = EnsureSceneChild(panelGo, "BtnBack");
            {
                var brt = backGo.GetComponent<RectTransform>();
                brt.anchorMin        = new Vector2(0.5f, 0f);
                brt.anchorMax        = new Vector2(0.5f, 0f);
                brt.pivot            = new Vector2(0.5f, 0f);
                brt.anchoredPosition = new Vector2(0f, 40f);
                brt.sizeDelta        = new Vector2(200f, 50f);
            }
            var backBg = EnsureSceneComponent<Image>(backGo);
            backBg.color = new Color(0.15f, 0.15f, 0.2f, 1f);
            EnsureSceneComponent<Button>(backGo);
            var backLabel = EnsureSceneChild(backGo, "Label");
            var backTmp   = EnsureSceneComponent<TextMeshProUGUI>(backLabel);
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

            // SettingsPanel-компонент
            var settingsComp = EnsureSceneComponent<SettingsPanel>(panelGo);
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

        private static void EnsureMainMenuButton(GameObject parent, string name, string label)
        {
            var go = EnsureSceneChild(parent, name);
            {
                var rt = go.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(0f, 60f);
            }
            var bg = EnsureSceneComponent<Image>(go);
            bg.color = new Color(0.15f, 0.15f, 0.2f, 1f);
            EnsureSceneComponent<Button>(go);

            var labelGo  = EnsureSceneChild(go, "Label");
            var labelTmp = EnsureSceneComponent<TextMeshProUGUI>(labelGo);
            labelTmp.text      = label;
            labelTmp.fontSize  = 28f;
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

        private static void WireMainMenuButtons(GameObject buttonsGo, GameObject canvasGo)
        {
            var menuCtrl = canvasGo.GetComponent<MainMenuController>();
            if (menuCtrl == null) return;

            WireSceneButton(buttonsGo, "BtnPlay",     menuCtrl, nameof(MainMenuController.OnPlayClicked));
            WireSceneButton(buttonsGo, "BtnSettings", menuCtrl, nameof(MainMenuController.OnSettingsClicked));
            WireSceneButton(buttonsGo, "BtnQuit",     menuCtrl, nameof(MainMenuController.OnQuitClicked));
        }

        private static void WireSceneButton(GameObject parent, string childName,
            Component target, string methodName)
        {
            var child = FindSceneDescendant(parent, childName);
            if (child == null) return;

            var btn = child.GetComponent<Button>();
            if (btn == null) return;

            var so          = new SerializedObject(btn);
            var onClickProp = so.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
            if (onClickProp == null) return;

            onClickProp.ClearArray();
            onClickProp.InsertArrayElementAtIndex(0);

            var call = onClickProp.GetArrayElementAtIndex(0);
            call.FindPropertyRelative("m_Target").objectReferenceValue  = target;
            call.FindPropertyRelative("m_MethodName").stringValue       = methodName;
            call.FindPropertyRelative("m_Mode").intValue                = 1; // EventDefined
            call.FindPropertyRelative("m_CallState").intValue           = 2; // RuntimeOnly

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // Сцено-специфичные вспомогательные методы (чтобы не конфликтовать с UITab)

        private static GameObject EnsureSceneObject(string goName)
        {
            var existing = GameObject.Find(goName);
            return existing != null ? existing : new GameObject(goName);
        }

        private static GameObject EnsureSceneChild(GameObject parent, string childName)
        {
            var existing = parent.transform.Find(childName);
            if (existing != null) return existing.gameObject;

            var go = new GameObject(childName);
            go.transform.SetParent(parent.transform, false);
            go.AddComponent<RectTransform>();
            return go;
        }

        private static GameObject FindSceneDescendant(GameObject root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            foreach (Transform child in root.transform)
            {
                var found = FindSceneDescendant(child.gameObject, name);
                if (found != null) return found;
            }
            return null;
        }

        private static void SetSceneRowSize(GameObject row, float height)
        {
            var rt = row.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0f, height);
        }

        private static void EnsureSceneRowLabel(GameObject row, string text)
        {
            var labelGo  = EnsureSceneChild(row, "Label");
            var labelTmp = EnsureSceneComponent<TextMeshProUGUI>(labelGo);
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

        private static void SetSceneControlRectRight(GameObject go, float width, float height)
        {
            var rt = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
            rt.anchorMin        = new Vector2(1f, 0.5f);
            rt.anchorMax        = new Vector2(1f, 0.5f);
            rt.pivot            = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(-10f, 0f);
            rt.sizeDelta        = new Vector2(width, height);
        }

        private static void SetupSceneSlider(Slider slider, float min, float max, float value)
        {
            slider.minValue     = min;
            slider.maxValue     = max;
            slider.value        = value;
            slider.wholeNumbers = false;

            EnsureSceneSliderGraphics(slider.gameObject);
        }

        private static void EnsureSceneSliderGraphics(GameObject sliderGo)
        {
            var bgGo = EnsureSceneChild(sliderGo, "Background");
            var bgI  = EnsureSceneComponent<Image>(bgGo);
            bgI.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = new Vector2(0f, 0.25f);
            bgRt.anchorMax = new Vector2(1f, 0.75f);
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;

            var fillAreaGo = EnsureSceneChild(sliderGo, "Fill Area");
            var fillAreaRt = fillAreaGo.GetComponent<RectTransform>();
            fillAreaRt.anchorMin = new Vector2(0f, 0.25f);
            fillAreaRt.anchorMax = new Vector2(1f, 0.75f);
            fillAreaRt.offsetMin = new Vector2(5f, 0f);
            fillAreaRt.offsetMax = new Vector2(-15f, 0f);

            var fillGo = EnsureSceneChild(fillAreaGo, "Fill");
            var fillI  = EnsureSceneComponent<Image>(fillGo);
            fillI.color = new Color(0.2f, 0.6f, 1f, 1f);
            var fillRt = fillGo.GetComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = new Vector2(0f, 1f);
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = new Vector2(10f, 0f);

            var handleAreaGo = EnsureSceneChild(sliderGo, "Handle Slide Area");
            var handleAreaRt = handleAreaGo.GetComponent<RectTransform>();
            handleAreaRt.anchorMin = new Vector2(0f, 0f);
            handleAreaRt.anchorMax = new Vector2(1f, 1f);
            handleAreaRt.offsetMin = new Vector2(10f, 0f);
            handleAreaRt.offsetMax = new Vector2(-10f, 0f);

            var handleGo = EnsureSceneChild(handleAreaGo, "Handle");
            var handleI  = EnsureSceneComponent<Image>(handleGo);
            handleI.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            var handleRt = handleGo.GetComponent<RectTransform>();
            handleRt.anchorMin        = new Vector2(0f, 0f);
            handleRt.anchorMax        = new Vector2(0f, 1f);
            handleRt.sizeDelta        = new Vector2(20f, 0f);
            handleRt.anchoredPosition = Vector2.zero;

            var slider = sliderGo.GetComponent<Slider>();
            if (slider != null)
            {
                var so = new SerializedObject(slider);
                so.FindProperty("m_FillRect").objectReferenceValue   = fillRt;
                so.FindProperty("m_HandleRect").objectReferenceValue = handleRt;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void EnsureSceneToggleGraphics(GameObject toggleGo)
        {
            var bgGo = EnsureSceneChild(toggleGo, "Background");
            var bgI  = EnsureSceneComponent<Image>(bgGo);
            bgI.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero;
            bgRt.offsetMax = Vector2.zero;

            var checkGo = EnsureSceneChild(bgGo, "Checkmark");
            var checkI  = EnsureSceneComponent<Image>(checkGo);
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
                so.FindProperty("graphic").objectReferenceValue = checkI;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        /// <summary>
        /// Перезаписывает EditorBuildSettings.scenes: MainMenu[0], Sandbox[1].
        /// Другие сцены (если они были) — удаляются (идемпотентно).
        /// </summary>
        private static void RewriteBuildSettings()
        {
            var scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(MainMenuScenePath, true),
                new EditorBuildSettingsScene(SandboxScenePath,  true),
            };
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("[Project Forge] Build Settings обновлены: MainMenu[0], Sandbox[1].");
        }

        // ----------------------------------------------------------------
        // Приватные вспомогательные
        // ----------------------------------------------------------------

        private static void EnsurePlane()
        {
            const string objName = "Ground";
            var existing = GameObject.Find(objName);
            if (existing == null)
            {
                existing = GameObject.CreatePrimitive(PrimitiveType.Plane);
                existing.name = objName;
            }

            existing.transform.position = Vector3.zero;
            // Plane по умолчанию 10×10 units; scale (10,1,10) = 100×100
            existing.transform.localScale = new Vector3(10f, 1f, 10f);

            // Помечаем статичным для NavMesh и прочего
            GameObjectUtility.SetStaticEditorFlags(existing,
                StaticEditorFlags.ContributeGI |
                StaticEditorFlags.NavigationStatic |
                StaticEditorFlags.BatchingStatic |
                StaticEditorFlags.OccluderStatic);
        }

        private static void EnsureDirectionalLight()
        {
            const string objName = "Sun";

            // Ищем существующий Directional Light с нужным именем
            var existing = GameObject.Find(objName);
            if (existing == null)
            {
                existing = new GameObject(objName);
                var light = existing.AddComponent<Light>();
                light.type = LightType.Directional;
                light.shadows = LightShadows.Soft;
            }

            existing.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        }

        private static void EnsureMarker(string markerName, Vector3 position)
        {
            var existing = GameObject.Find(markerName);
            if (existing == null)
                existing = new GameObject(markerName);

            existing.transform.position = position;
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            var scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);

            bool alreadyPresent = scenes.Any(s => s.path == scenePath);
            if (!alreadyPresent)
            {
                scenes.Add(new EditorBuildSettingsScene(scenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
                Debug.Log("[Project Forge] Sandbox добавлена в Build Settings.");
            }
        }

        // ----------------------------------------------------------------
        // v9 RebuildMapLayout
        // ----------------------------------------------------------------

        /// <summary>
        /// Переставляет маркеры баз, здания, расставляет скалы-чокпоинт,
        /// два экспанд-узла и декор. Идемпотентно (EnsureChild-паттерн).
        /// Завершается перезапечкой NavMesh и сохранением сцены.
        /// </summary>
        internal static void RebuildMapLayout()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                EditorUtility.DisplayDialog("Project Forge", "Нет открытой сцены.", "OK");
                return;
            }

            // ── 1. Маркеры баз ──────────────────────────────────────────
            const float baseExtent = 35f;
            var playerBasePos = new Vector3(-baseExtent, 0f, -baseExtent);
            var enemyBasePos  = new Vector3( baseExtent, 0f,  baseExtent);

            EnsureMarker("PlayerBaseSpawn", playerBasePos);
            EnsureMarker("EnemyBaseSpawn",  enemyBasePos);

            Debug.Log("[Project Forge] v9: маркеры баз перемещены.");

            // ── 2. Перемещение зданий относительно новых позиций баз ────
            // Офсеты соответствуют тем, что устанавливали SetupEconomy / SetupScenario / SetupTank.
            // SetupEconomy:   HQ_Player  @ playerBase + (0,0,0)
            //                 HQ_Enemy   @ enemyBase  + (0,0,0)
            //                 Barracks_Player @ playerBase + (6,0,4)
            // SetupScenario:  Barracks_Enemy  @ enemyBase  + (6,0,-4)
            // SetupTank:      WarFactory_Player @ playerBase + (10,0,4)
            //                 WarFactory_Enemy  @ enemyBase  + (10,0,-4)
            MoveGameObjectIfExists("HQ_Player",         playerBasePos + new Vector3( 0f, 0f,  0f));
            MoveGameObjectIfExists("HQ_Enemy",          enemyBasePos  + new Vector3( 0f, 0f,  0f));
            MoveGameObjectIfExists("Barracks_Player",   playerBasePos + new Vector3( 6f, 0f,  4f));
            MoveGameObjectIfExists("Barracks_Enemy",    enemyBasePos  + new Vector3( 6f, 0f, -4f));
            MoveGameObjectIfExists("WarFactory_Player", playerBasePos + new Vector3(10f, 0f,  4f));
            MoveGameObjectIfExists("WarFactory_Enemy",  enemyBasePos  + new Vector3(10f, 0f, -4f));

            // M5 ResourceNode'ы с оригинальными офсетами
            MoveGameObjectIfExists("ResourceNode_Player_1", playerBasePos + new Vector3(-8f, 0f, -5f));
            MoveGameObjectIfExists("ResourceNode_Player_2", playerBasePos + new Vector3( 8f, 0f, -5f));
            MoveGameObjectIfExists("ResourceNode_Enemy_1",  enemyBasePos  + new Vector3(-8f, 0f,  5f));
            MoveGameObjectIfExists("ResourceNode_Enemy_2",  enemyBasePos  + new Vector3( 8f, 0f,  5f));

            // Герой и тестовые юниты — сдвигаем к игровой базе
            MoveGameObjectIfExists("Hero",              playerBasePos + new Vector3(0f, 1f, 0f));
            MoveGameObjectIfExists("RtsCameraTarget",   playerBasePos);

            Debug.Log("[Project Forge] v9: здания и юниты переставлены.");

            // ── 3. Родительский контейнер MapLayout ─────────────────────
            var mapLayout = EnsureGameObjectRoot("MapLayout");

            // ── 4. Choke_Obstacles: 6 скал по 2 колонны x=±8, z∈{+2,0,-2} ──
            // Центральный чокпоинт: проход ~16 ед. в центре, открытые фланги.
            // Колонны x=±8 дают коридор шириной 16 ед. (от -8 до +8),
            // скалы с scale 2.5 на x занимают ~2.5 ед. — реальная ширина прохода
            // у самих камней ≈ 16 - 2*2.5 = 11 ед. Открытые фланги сохраняются.
            const float chokeX    = 8f;
            var chokeContainer = EnsureChildObject(mapLayout, "Choke_Obstacles");

            var rockMesh = LoadMeshFromFbx("Assets/_Project/Art/Models/Props/rock_largeA.fbx");
            var chokeScale = new Vector3(2.5f, 3f, 2.5f);
            const StaticEditorFlags chokeFlags =
                StaticEditorFlags.NavigationStatic |
                StaticEditorFlags.BatchingStatic   |
                StaticEditorFlags.ContributeGI;

            // L-колонна (x = -8): имена Choke_L1, Choke_L2, Choke_L3
            // R-колонна (x = +8): имена Choke_R1, Choke_R2, Choke_R3
            float[] chokeZValues = { 2f, 0f, -2f };
            string[] lNames = { "Choke_L1", "Choke_L2", "Choke_L3" };
            string[] rNames = { "Choke_R1", "Choke_R2", "Choke_R3" };

            for (int i = 0; i < 3; i++)
            {
                float zVal = chokeZValues[i];

                // L-скала
                EnsureRock(chokeContainer, lNames[i],
                    new Vector3(-chokeX, 0f, zVal), chokeScale, rockMesh, chokeFlags);

                // R-скала
                EnsureRock(chokeContainer, rNames[i],
                    new Vector3( chokeX, 0f, zVal), chokeScale, rockMesh, chokeFlags);
            }

            Debug.Log("[Project Forge] v9: Choke_Obstacles расставлены (x=±8, z=+2/0/-2, scale 2.5×3×2.5).");

            // ── 5. Expand_Nodes: 2 ResourceNode (точечно-симметрично) ───
            var expandContainer = EnsureChildObject(mapLayout, "Expand_Nodes");
            var nodePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/_Project/Prefabs/Props/ResourceNode.prefab");

            EnsureExpandNode(expandContainer, scene, "ExpandNode_Player",
                new Vector3(-12f, 0f, -18f), nodePrefab, 2000);
            EnsureExpandNode(expandContainer, scene, "ExpandNode_Enemy",
                new Vector3( 12f, 0f,  18f), nodePrefab, 2000);

            Debug.Log("[Project Forge] v9: Expand_Nodes расставлены.");

            // ── 6. Decor: 15 объектов без коллайдеров ───────────────────
            BuildDecor(mapLayout, scene);

            // ── 7. MinimapCamera orthographicSize ≥ 52 ──────────────────
            var minimapCamGo = GameObject.Find("MinimapCamera");
            if (minimapCamGo != null)
            {
                var cam = minimapCamGo.GetComponent<Camera>();
                if (cam != null && cam.orthographicSize < 52f)
                {
                    cam.orthographicSize = 60f;
                    Debug.Log("[Project Forge] v9: MinimapCamera.orthographicSize обновлён до 60.");
                }
            }

            // ── 8. NavMesh перебейк ──────────────────────────────────────
            NavMeshTab.BakeNavMesh();

            // ── 9. Сохранение ────────────────────────────────────────────
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            Debug.Log("[Project Forge] v9: RebuildMapLayout завершён. " +
                      $"PlayerBaseSpawn={playerBasePos}, EnemyBaseSpawn={enemyBasePos}. " +
                      "Choke: x=±8, z=+2/0/-2, проход ~11 ед.");
        }

        private static void MoveGameObjectIfExists(string goName, Vector3 position)
        {
            var go = GameObject.Find(goName);
            if (go != null)
                go.transform.position = position;
        }

        /// <summary>Возвращает GO с данным именем на корневом уровне или создаёт новый.</summary>
        private static GameObject EnsureGameObjectRoot(string goName)
        {
            var existing = GameObject.Find(goName);
            return existing != null ? existing : new GameObject(goName);
        }

        /// <summary>Возвращает дочерний GO с данным именем или создаёт его.</summary>
        private static GameObject EnsureChildObject(GameObject parent, string childName)
        {
            var t = parent.transform.Find(childName);
            if (t != null) return t.gameObject;
            var go = new GameObject(childName);
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        private static Mesh LoadMeshFromFbx(string fbxPath)
        {
            // Загружаем все ассеты из fbx, возвращаем первый Mesh
            var allAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            foreach (var a in allAssets)
            {
                if (a is Mesh m)
                    return m;
            }
            return null;
        }

        private static void EnsureRock(
            GameObject parent,
            string     goName,
            Vector3    localPosition,
            Vector3    scale,
            Mesh       mesh,
            StaticEditorFlags flags)
        {
            var t = parent.transform.Find(goName);
            GameObject go;
            if (t != null)
            {
                go = t.gameObject;
            }
            else
            {
                go = new GameObject(goName);
                go.transform.SetParent(parent.transform, false);

                if (mesh != null)
                {
                    go.AddComponent<MeshFilter>().sharedMesh = mesh;
                    go.AddComponent<MeshRenderer>();
                }

                // BoxCollider для навигационного препятствия
                go.AddComponent<BoxCollider>();
            }

            go.transform.localPosition = localPosition;
            go.transform.localScale    = scale;
            GameObjectUtility.SetStaticEditorFlags(go, flags);
        }

        private static void EnsureExpandNode(
            GameObject  parent,
            Scene scene,
            string      goName,
            Vector3     localPosition,
            GameObject  prefab,
            int         reserve)
        {
            var t = parent.transform.Find(goName);
            GameObject go;
            if (t != null)
            {
                go = t.gameObject;
            }
            else
            {
                if (prefab != null)
                {
                    go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                    go.name = goName;
                    go.transform.SetParent(parent.transform, false);
                }
                else
                {
                    go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    go.name = goName;
                    go.transform.SetParent(parent.transform, false);
                    go.transform.localScale = new Vector3(2f, 0.5f, 2f);
                    if (!go.GetComponent<ResourceNode>())
                        go.AddComponent<ResourceNode>();
                }
            }

            go.transform.localPosition = localPosition;

            // Устанавливаем _reserve = reserve через SerializedObject
            var node = go.GetComponentInChildren<ResourceNode>(true);
            if (node == null)
                node = go.AddComponent<ResourceNode>();

            var so = new SerializedObject(node);
            so.FindProperty("_reserve").intValue = reserve;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ----------------------------------------------------------------
        // BuildDecor: 15 объектов без коллайдеров
        // ----------------------------------------------------------------

        private static void BuildDecor(GameObject parent,
            Scene _scene)
        {
            var decorContainer = EnsureChildObject(parent, "Decor");

            const StaticEditorFlags decorFlags =
                StaticEditorFlags.BatchingStatic | StaticEditorFlags.ContributeGI;

            // Таблица декора: имя GO, путь к FBX, локальная позиция, поворот Y, масштаб
            var decorTable = new (string name, string fbx, Vector3 pos, float rotY, Vector3 scale)[]
            {
                // Скалы средние
                ("Decor_Rock_01",           "Assets/_Project/Art/Models/Props/rock.fbx",
                    new Vector3(-20f, 0f, 5f),   15f, new Vector3(1.5f, 1.5f, 1.5f)),
                ("Decor_Rock_02",           "Assets/_Project/Art/Models/Props/rock.fbx",
                    new Vector3( 22f, 0f, -8f),  45f, new Vector3(1.2f, 1.2f, 1.2f)),
                ("Decor_Rock_03",           "Assets/_Project/Art/Models/Props/rock.fbx",
                    new Vector3( -5f, 0f, 20f),   0f, new Vector3(1.8f, 1.8f, 1.8f)),

                // Кристаллы малые
                ("Decor_Crystals_01",       "Assets/_Project/Art/Models/Props/rock_crystals.fbx",
                    new Vector3(-15f, 0f, 10f),  30f, new Vector3(1f, 1f, 1f)),
                ("Decor_Crystals_02",       "Assets/_Project/Art/Models/Props/rock_crystals.fbx",
                    new Vector3( 18f, 0f, -15f), 60f, new Vector3(1.2f, 1.2f, 1.2f)),
                ("Decor_Crystals_03",       "Assets/_Project/Art/Models/Props/rock_crystals.fbx",
                    new Vector3(-25f, 0f, -10f), 90f, new Vector3(0.9f, 0.9f, 0.9f)),

                // Кристаллы крупные
                ("Decor_CrystalsLarge_01",  "Assets/_Project/Art/Models/Props/rock_crystalsLargeA.fbx",
                    new Vector3( 25f, 0f,  12f),  20f, new Vector3(1.3f, 1.3f, 1.3f)),
                ("Decor_CrystalsLarge_02",  "Assets/_Project/Art/Models/Props/rock_crystalsLargeA.fbx",
                    new Vector3(-22f, 0f,  18f), 120f, new Vector3(1.1f, 1.1f, 1.1f)),

                // Кратеры
                ("Decor_Crater_01",         "Assets/_Project/Art/Models/Props/crater.fbx",
                    new Vector3( 5f, 0f, -20f),  0f, new Vector3(2f, 2f, 2f)),
                ("Decor_Crater_02",         "Assets/_Project/Art/Models/Props/crater.fbx",
                    new Vector3(-8f, 0f,  15f), 45f, new Vector3(1.5f, 1.5f, 1.5f)),
                ("Decor_Crater_03",         "Assets/_Project/Art/Models/Props/crater.fbx",
                    new Vector3( 12f, 0f, 22f), 90f, new Vector3(1.8f, 1.8f, 1.8f)),

                // Бочки
                ("Decor_Barrel_01",         "Assets/_Project/Art/Models/Props/barrel.fbx",
                    new Vector3(-18f, 0f, -5f),  0f, new Vector3(1f, 1f, 1f)),
                ("Decor_Barrel_02",         "Assets/_Project/Art/Models/Props/barrel.fbx",
                    new Vector3( 15f, 0f,  5f), 30f, new Vector3(1f, 1f, 1f)),

                // Промышленные бочки
                ("Decor_MachineBrrl_01",    "Assets/_Project/Art/Models/Props/machine_barrel.fbx",
                    new Vector3(-12f, 0f, 22f),   0f, new Vector3(1f, 1f, 1f)),
                ("Decor_MachineBrrl_02",    "Assets/_Project/Art/Models/Props/machine_barrel.fbx",
                    new Vector3( 20f, 0f, -20f), 60f, new Vector3(1f, 1f, 1f)),
            };

            foreach (var entry in decorTable)
            {
                var t = decorContainer.transform.Find(entry.name);
                if (t != null)
                {
                    // Обновляем позицию идемпотентно
                    t.localPosition = entry.pos;
                    t.localRotation = Quaternion.Euler(0f, entry.rotY, 0f);
                    t.localScale    = entry.scale;
                    continue;
                }

                var mesh = LoadMeshFromFbx(entry.fbx);
                var go   = new GameObject(entry.name);
                go.transform.SetParent(decorContainer.transform, false);
                go.transform.localPosition = entry.pos;
                go.transform.localRotation = Quaternion.Euler(0f, entry.rotY, 0f);
                go.transform.localScale    = entry.scale;

                if (mesh != null)
                {
                    go.AddComponent<MeshFilter>().sharedMesh = mesh;
                    go.AddComponent<MeshRenderer>();
                }

                // Явно удаляем все коллайдеры (если появились)
                foreach (var col in go.GetComponentsInChildren<Collider>())
                    Object.DestroyImmediate(col);

                GameObjectUtility.SetStaticEditorFlags(go, decorFlags);
            }
        }
    }
}
