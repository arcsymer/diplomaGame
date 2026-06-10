using System.Collections.Generic;
using System.IO;
using System.Linq;
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
    }
}
