using DiplomaGame.Runtime.Audio;
using DiplomaGame.Runtime.Buildings;
using DiplomaGame.Runtime.Combat;
using DiplomaGame.Runtime.Commands;
using DiplomaGame.Runtime.Core;
using DiplomaGame.Runtime.Data;
using DiplomaGame.Runtime.Economy;
using DiplomaGame.Runtime.Hero;
using DiplomaGame.Runtime.Selection;
using DiplomaGame.Runtime.UI;
using DiplomaGame.Runtime.Units;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
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
        private const string MinimapRTPath            = "Assets/_Project/UI/MinimapRT.renderTexture";
        private const string OrderMoveMatPath         = "Assets/_Project/Art/Materials/OrderMove.mat";
        private const string OrderAttackMatPath       = "Assets/_Project/Art/Materials/OrderAttack.mat";

        // v6 Command Card
        private const string StanFbxPath             = "Assets/_Project/Art/Models/Units/Animated/Stan.fbx";
        private const string HeavyMarinePrefabPath   = "Assets/_Project/Prefabs/Units/HeavyMarineUnit.prefab";
        private const string HeavyMarineControllerPath = "Assets/_Project/Art/Animations/HeavyMarine_Controller.controller";
        private const string PlayerBluePath           = "Assets/_Project/Art/Materials/PlayerBlue.mat";
        private const string HeavyMarineDataPath      = "Assets/_Project/Data/Units/HeavyMarine.asset";
        private const string MarineDataPath           = "Assets/_Project/Data/Units/Marine.asset";
        private const string TankDataPath             = "Assets/_Project/Data/Units/Tank.asset";
        private const string MarinePrefabPath         = "Assets/_Project/Prefabs/Units/TestUnit.prefab";
        private const string TankPrefabPath           = "Assets/_Project/Prefabs/Units/TankUnit.prefab";
        private const string BarracksPrefabPath       = "Assets/_Project/Prefabs/Buildings/Barracks.prefab";
        private const string WarFactoryPrefabPath     = "Assets/_Project/Prefabs/Buildings/WarFactory.prefab";
        private const string ExtractorPrefabPath      = "Assets/_Project/Prefabs/Buildings/Extractor.prefab";
        private const string FightOggPath             = "Assets/_Project/Audio/Voice/fight.ogg";
        // Целевая высота HeavyMarine (примерно в 1.35x больше Marine 1.2f)
        private const float  HeavyMarineTargetHeight  = 1.62f;

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

            GUILayout.Space(8);

            EditorGUILayout.HelpBox(
                "Tooltip System: создаёт TooltipCanvas + TooltipView-иерархию,\n" +
                "навешивает триггеры и провайдеры на AbilitySlot_1..4,\n" +
                "ResourceText (Crystals), MinimapDisplay, hit-area производства.\n" +
                "Операция идемпотентна.",
                MessageType.Info);

            GUILayout.Space(4);

            if (GUILayout.Button("Build Tooltip System", GUILayout.Height(32)))
                BuildTooltipSystem();

            GUILayout.Space(8);

            EditorGUILayout.HelpBox(
                "Match Stats Panel: создаёт StatsPanel внутри VictoryPanel и DefeatPanel,\n" +
                "добавляет MatchStatsView и MatchStatsCollector, прописывает ссылки.\n" +
                "Операция идемпотентна.",
                MessageType.Info);

            GUILayout.Space(4);

            if (GUILayout.Button("Build Match Stats Panel", GUILayout.Height(32)))
                BuildMatchStatsPanel();

            GUILayout.Space(8);

            EditorGUILayout.HelpBox(
                "Setup Command Card (v6):\n" +
                "• Миграция ProductionEntries (HeavyMarine.asset, Barracks/WarFactory entries)\n" +
                "• Создаёт HeavyMarineUnit.prefab (Stan.fbx, PlayerBlue, NavMeshAgent, анимации)\n" +
                "• CommandCardRoot (3 кнопки) + QueueSlotsRoot (5 слотов) в SelectionPanel\n" +
                "• Проставляет _unitPrefabs у Barracks и WarFactory\n" +
                "• BuildingSpawnEffect на Barracks/WarFactory/Extractor\n" +
                "• _waveStingerClip = fight.ogg на AudioManager сцены\n" +
                "• TooltipTrigger на кнопки карты\n" +
                "Операция идемпотентна.",
                MessageType.Info);

            GUILayout.Space(4);

            if (GUILayout.Button("Setup Command Card (v6)", GUILayout.Height(32)))
                SetupCommandCardV6();

            GUILayout.Space(8);

            EditorGUILayout.HelpBox(
                "Setup Tech Tree (v7):\n" +
                "• Создаёт TechCardRoot (2-й ряд из 3 tech-кнопок) под CommandCardRoot\n" +
                "• Добавляет researchedOverlay (Image зелёный + TMP ✓) к каждой кнопке\n" +
                "• Прописывает techCardSlots в SelectionPanel\n" +
                "• Навешивает TooltipTrigger на tech-кнопки\n" +
                "Требует: BuildGameHUD (M6a) и SetupCommandCard (v6) уже выполнены.\n" +
                "Операция идемпотентна.",
                MessageType.Info);

            GUILayout.Space(4);

            if (GUILayout.Button("Setup Tech Tree (v7)", GUILayout.Height(32)))
                SetupTechTreeV7();

            GUILayout.Space(8);

            EditorGUILayout.HelpBox(
                "Apply Localization to Scene UI (v10):\n" +
                "• Добавляет LanguageRow (TMP_Dropdown RU/EN) в SettingsPanel\n" +
                "• Навешивает LocalizedText на статические TMP-надписи (PauseMenu, GameOver, Заголовок настроек)\n" +
                "• Добавляет LocServiceBootstrap на GameManagers (со ссылкой на LocTable.asset)\n" +
                "Требует: BuildMenus (M6b) уже выполнен. Работает и для Sandbox, и для MainMenu.\n" +
                "Операция идемпотентна.",
                MessageType.Info);

            GUILayout.Space(4);

            if (GUILayout.Button("Apply Localization to Scene UI (v10)", GUILayout.Height(32)))
                ApplyLocalizationToSceneUI_V10();

            GUILayout.Space(8);

            EditorGUILayout.HelpBox(
                "Under Attack Alert (C16):\n" +
                "• Создаёт UnderAttackVignette (Image, full-stretch, красный, alpha=0) в GameHUD\n" +
                "• Создаёт ThreatMarker (Image, 16×16, красный, скрытый) в MinimapDisplay\n" +
                "• Добавляет UnderAttackAlert на GameManagers\n" +
                "• Прописывает _minimapMarker, _edgeVignette, _minimapCamera, _minimapDisplay\n" +
                "Требует: BuildGameHUD (M6a) уже выполнен. Операция идемпотентна.",
                MessageType.Info);

            GUILayout.Space(4);

            if (GUILayout.Button("Build Under Attack Alert (C16)", GUILayout.Height(32)))
                BuildUnderAttackAlert();

            GUILayout.Space(8);

            EditorGUILayout.HelpBox(
                "Idle Army Indicator (C17):\n" +
                "• Создаёт IdleArmyBadge (Button, 90×32) в левом нижнем углу RTS_Block\n" +
                "• Добавляет IdleArmyIndicator на GameManagers\n" +
                "• Прописывает _selectionSystem, _countLabel, _pulse\n" +
                "• По умолчанию скрыт (activeSelf=false); появляется при count > 0\n" +
                "Требует: BuildGameHUD (M6a) уже выполнен. Операция идемпотентна.",
                MessageType.Info);

            GUILayout.Space(4);

            if (GUILayout.Button("Setup Idle Army Indicator (C17)", GUILayout.Height(32)))
                SetupIdleArmyIndicator();

            GUILayout.Space(8);

            EditorGUILayout.HelpBox(
                "Unit Health Bars (C18):\n" +
                "• Создаёт HealthBarsCanvas (Screen Space Overlay, order=20) в сцене\n" +
                "• Добавляет UnitHealthBarSystem на GameManagers\n" +
                "• Прошивает _modeController, _selectionSystem, _barCanvas\n" +
                "• Пул 48 виджетов строится ПРОГРАММНО (фон + fill + border) — prefab не нужен\n" +
                "• Бары видны только выделенным или повреждённым юнитам; в TPS скрываются\n" +
                "Требует: BuildGameHUD (M6a) уже выполнен. Операция идемпотентна.\n" +
                "Ручное назначение префаба в Inspector НЕ требуется.",
                MessageType.Info);

            GUILayout.Space(4);

            if (GUILayout.Button("Setup Unit Health Bars (C18)", GUILayout.Height(32)))
                SetupUnitHealthBars();

            GUILayout.Space(8);

            EditorGUILayout.HelpBox(
                "Crosshair Hitmarker (C20):\n" +
                "• Создаёт 4 дочерних Image-полоски на Crosshair (если их нет)\n" +
                "• Прошивает CrosshairUI._shooter (Hero/HeroShooter) + CrosshairUI._settings\n" +
                "• Записывает дефолтные значения hitmarker в GameFeelSettings.asset через SerializedObject\n" +
                "  (hitmarkerColorHit = warm orange, expandScale=1.15, missScale=1.05, duration=0.10)\n" +
                "• Добавляет UiPulse на каждый AbilitySlot_1..4 (если его нет)\n" +
                "Требует: BuildGameHUD (M6a) и SetupGameFeel (C12) уже выполнены. Идемпотентно.",
                MessageType.Info);

            GUILayout.Space(4);

            if (GUILayout.Button("Setup Crosshair Hitmarker (C20)", GUILayout.Height(32)))
                SetupCrosshairHitmarker();

            GUILayout.Space(8);

            EditorGUILayout.HelpBox(
                "Hero Damage Indicator (C21 + C23):\n" +
                "• [C21] Создаёт HeroDamageFlash (Image, full-stretch, red alpha=0) в TPS_Block\n" +
                "• [C23] Создаёт HeroDamageArrow (Image 64×64, центр+220px, red alpha=0, скрыт)\n" +
                "• Добавляет HeroDamageIndicator на HeroDamageFlash\n" +
                "• Прошивает _heroHealth, _tpsCameraTransform, _edgeFlash, _directionArrow, _settings\n" +
                "• Записывает дефолты в GameFeelSettings.asset через SerializedObject\n" +
                "  (damageIndicatorDuration=1.0, damageIndicatorPeakAlpha=0.6, damageArrowPeakAlpha=0.8)\n" +
                "• [C23] Направленная стрелка вращается к атакующему (Health.AnyDamagedFrom)\n" +
                "• [C21] Full-edge flash остаётся fallback для урона без источника (Health.AnyDamaged)\n" +
                "Требует: BuildGameHUD (M6a) и SetupGameFeel (C12) уже выполнены. Идемпотентно.",
                MessageType.Info);

            GUILayout.Space(4);

            if (GUILayout.Button("Setup Hero Damage Indicator (C21)", GUILayout.Height(32)))
                SetupHeroDamageIndicator();

            GUILayout.Space(8);

            EditorGUILayout.HelpBox(
                "Dynamic FOV (C22/C24):\n" +
                "• Добавляет/обновляет DynamicFovController на GameManagers\n" +
                "• Прошивает _tpsCamera, _abilitySystem, _modeController, _settings, _heroController\n" +
                "• Записывает дефолты C22+C24 в GameFeelSettings.asset через SerializedObject\n" +
                "  (fovKickAmount=9, fovKickDuration=0.08, fovReturnSpeed=12, fovSprintWiden=4)\n" +
                "Kick-триггеры: AbilityType.Dash и AbilityType.Overcharge.\n" +
                "Sprint-widen (C24): +4° пока IsSprinting, суммируется с kick.\n" +
                "Требует: SetupGameFeel (C12) уже выполнен. Только TPS-камера. Идемпотентно.",
                MessageType.Info);

            GUILayout.Space(4);

            if (GUILayout.Button("Setup Dynamic FOV (C22/C24)", GUILayout.Height(32)))
                GameFeelTab.SetupDynamicFov();
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
        // Tooltip System
        // ----------------------------------------------------------------

        /// <summary>
        /// Идемпотентно создаёт TooltipCanvas + TooltipView-иерархию и
        /// навешивает TooltipTrigger + провайдеры на нужные UI-элементы.
        /// </summary>
        internal static void BuildTooltipSystem()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                EditorUtility.DisplayDialog("Project Forge", "Нет открытой сцены.", "OK");
                return;
            }

            EnsureTmpEssentials();
            EnsureEventSystem();

            // --- TooltipCanvas ---
            var canvasGo = EnsureGameObject("TooltipCanvas");
            var canvas   = EnsureComponent<Canvas>(canvasGo);
            canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = EnsureComponent<CanvasScaler>(canvasGo);
            scaler.uiScaleMode            = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution    = new Vector2(1920f, 1080f);
            scaler.screenMatchMode        = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight     = 0.5f;

            EnsureComponent<GraphicRaycaster>(canvasGo);

            // --- TooltipSystem-компонент ---
            var tooltipSystemComp = EnsureComponent<TooltipSystem>(canvasGo);

            // --- TooltipView-иерархия ---
            var viewGo = EnsureChild(canvasGo, "TooltipView");
            {
                // Позиционирование: anchor верхний-левый, pivot верхний-левый
                var vrt = viewGo.GetComponent<RectTransform>();
                vrt.anchorMin = Vector2.zero;
                vrt.anchorMax = Vector2.zero;
                vrt.pivot     = new Vector2(0f, 1f);
                vrt.anchoredPosition = new Vector2(100f, -100f);

                // Фон
                var bg = EnsureComponent<Image>(viewGo);
                bg.color = new Color(0.05f, 0.05f, 0.08f, 0.92f);

                // ContentSizeFitter
                var csf = EnsureComponent<ContentSizeFitter>(viewGo);
                csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
                csf.verticalFit   = ContentSizeFitter.FitMode.PreferredSize;

                // LayoutElement — ограничивает ширину
                var le = EnsureComponent<LayoutElement>(viewGo);
                le.preferredWidth = TooltipLogic.TooltipMaxWidth;

                // VerticalLayoutGroup
                var vlg = EnsureComponent<VerticalLayoutGroup>(viewGo);
                vlg.padding               = new RectOffset(12, 12, 10, 10);
                vlg.spacing               = 4f;
                vlg.childAlignment        = TextAnchor.UpperLeft;
                vlg.childControlWidth     = true;
                vlg.childControlHeight    = true;
                vlg.childForceExpandWidth  = true;
                vlg.childForceExpandHeight = false;

                // CanvasGroup для fade-in
                var cg = EnsureComponent<CanvasGroup>(viewGo);
                cg.alpha          = 0f;
                cg.interactable   = false;
                cg.blocksRaycasts = false;

                // TitleText
                var titleGo  = EnsureChild(viewGo, "TitleText");
                var titleTmp = EnsureComponent<TextMeshProUGUI>(titleGo);
                titleTmp.text      = "Название";
                titleTmp.fontSize  = 16f;
                titleTmp.fontStyle = FontStyles.Bold;
                titleTmp.color     = Color.white;
                titleTmp.enableWordWrapping = true;

                // Separator
                var sepGo = EnsureChild(viewGo, "Separator");
                var sepI  = EnsureComponent<Image>(sepGo);
                sepI.color = new Color(0.4f, 0.4f, 0.5f, 0.6f);
                {
                    var sepLe = EnsureComponent<LayoutElement>(sepGo);
                    sepLe.preferredHeight = 1f;
                    sepLe.flexibleWidth   = 1f;
                }

                // DescriptionText
                var descGo  = EnsureChild(viewGo, "DescriptionText");
                var descTmp = EnsureComponent<TextMeshProUGUI>(descGo);
                descTmp.text      = "Описание.";
                descTmp.fontSize  = 13f;
                descTmp.color     = new Color(0.8f, 0.85f, 0.8f, 1f);
                descTmp.enableWordWrapping = true;

                // StatsText
                var statsGo  = EnsureChild(viewGo, "StatsText");
                var statsTmp = EnsureComponent<TextMeshProUGUI>(statsGo);
                statsTmp.text      = "Статы";
                statsTmp.fontSize  = 12f;
                statsTmp.color     = new Color(0.6f, 0.8f, 1.0f, 1f);
                statsTmp.enableWordWrapping = false;

                // TooltipView-компонент
                var tooltipView = EnsureComponent<TooltipView>(viewGo);
                {
                    var so = new SerializedObject(tooltipView);
                    so.FindProperty("titleText").objectReferenceValue       = titleTmp;
                    so.FindProperty("separator").objectReferenceValue       = sepGo;
                    so.FindProperty("descriptionText").objectReferenceValue = descTmp;
                    so.FindProperty("statsText").objectReferenceValue       = statsTmp;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                // Связываем TooltipSystem → TooltipView
                {
                    var so = new SerializedObject(tooltipSystemComp);
                    so.FindProperty("tooltipView").objectReferenceValue = tooltipView;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                // По умолчанию скрыт
                viewGo.SetActive(false);
            }

            // --- Навешиваем триггеры + провайдеры на HUD-элементы ---
            AttachTooltipTriggers();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Project Forge] Build Tooltip System выполнен.");
        }

        private static void AttachTooltipTriggers()
        {
            // AbilitySlot_1..4 в TPS_Block
            var gameHud  = GameObject.Find("GameHUD");
            var tpsBlock = gameHud != null ? FindDescendantByName(gameHud, "TPS_Block") : null;
            var rtsBlock = gameHud != null ? FindDescendantByName(gameHud, "RTS_Block") : null;

            if (tpsBlock != null)
            {
                for (int i = 1; i <= 4; i++)
                {
                    var slotGo = FindDescendantByName(tpsBlock, "AbilitySlot_" + i.ToString());
                    if (slotGo == null) continue;

                    // Нужен Graphic-компонент для работы EventSystem raycast
                    var img = slotGo.GetComponent<Image>();
                    if (img != null) img.raycastTarget = true;

                    EnsureComponent<TooltipTrigger>(slotGo);
                    EnsureComponent<AbilityTooltipProvider>(slotGo);
                }
            }

            if (rtsBlock != null)
            {
                // ResourceText (Crystals)
                var resourceGo = FindDescendantByName(rtsBlock, "ResourceText");
                if (resourceGo != null)
                {
                    // На ResourceText уже есть TMP_Text (Graphic) — второй Graphic (Image)
                    // добавить нельзя, да и не нужно: текст сам является raycast-целью.
                    var graphic = resourceGo.GetComponent<UnityEngine.UI.Graphic>();
                    if (graphic != null) graphic.raycastTarget = true;

                    EnsureComponent<TooltipTrigger>(resourceGo);
                    EnsureComponent<ResourceTooltipProvider>(resourceGo);
                }

                // MinimapDisplay
                var minimapGo = FindDescendantByName(rtsBlock, "MinimapDisplay");
                if (minimapGo != null)
                {
                    var rawImg = minimapGo.GetComponent<UnityEngine.UI.RawImage>();
                    if (rawImg != null) rawImg.raycastTarget = true;

                    EnsureComponent<TooltipTrigger>(minimapGo);
                    EnsureComponent<MinimapTooltipProvider>(minimapGo);
                }

                // Hit-area производства поверх хинта [T] в SelectionPanel
                var selPanelGo = FindDescendantByName(rtsBlock, "SelectionPanel");
                if (selPanelGo != null)
                {
                    var hintGo = FindDescendantByName(selPanelGo, "HintText");
                    if (hintGo != null)
                    {
                        // Прозрачный Image поверх хинта — hit-area для тултипа производства
                        var hitAreaGo = EnsureChild(hintGo, "ProductionTooltipHitArea");
                        var hitImg    = EnsureComponent<Image>(hitAreaGo);
                        hitImg.color        = new Color(0f, 0f, 0f, 0f);
                        hitImg.raycastTarget = true;

                        var hitRt = hitAreaGo.GetComponent<RectTransform>();
                        hitRt.anchorMin = Vector2.zero;
                        hitRt.anchorMax = Vector2.one;
                        hitRt.offsetMin = Vector2.zero;
                        hitRt.offsetMax = Vector2.zero;

                        EnsureComponent<TooltipTrigger>(hitAreaGo);

                        var provider = EnsureComponent<UnitProductionTooltipProvider>(hitAreaGo);
                        {
                            var managersGo      = GameObject.Find("GameManagers");
                            var selectionSystem = managersGo != null
                                ? managersGo.GetComponent<SelectionSystem>()
                                : null;

                            var so = new SerializedObject(provider);
                            so.FindProperty("selectionSystem").objectReferenceValue = selectionSystem;
                            so.ApplyModifiedPropertiesWithoutUndo();
                        }
                    }
                }
            }
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
        // Match Stats Panel
        // ----------------------------------------------------------------

        /// <summary>
        /// Идемпотентно создаёт единственный StatsPanel (640×260) в центре GameOver-Canvas
        /// (дочерний элемент Canvas, перекрывает обе панели), добавляет MatchStatsView,
        /// проставляет ссылки в GameOverController и добавляет MatchStatsCollector на GameManagers.
        /// Кнопки Victory/Defeat смещаются на (0,−240) для освобождения места.
        /// </summary>
        internal static void BuildMatchStatsPanel()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                EditorUtility.DisplayDialog("Project Forge", "Нет открытой сцены.", "OK");
                return;
            }

            EnsureTmpEssentials();

            // --- Найти GameOver Canvas ---
            var gameOverCanvas = GameObject.Find("GameOver");
            if (gameOverCanvas == null)
            {
                Debug.LogWarning("[Project Forge] GameOver Canvas не найден. Сначала запустите Build Menus (M6b).");
                return;
            }

            // --- Смещаем кнопки в обеих панелях ---
            var victoryGo = FindDescendantByName(gameOverCanvas, "VictoryPanel");
            var defeatGo  = FindDescendantByName(gameOverCanvas, "DefeatPanel");

            if (victoryGo != null) ShiftButtonsContainer(victoryGo, -240f);
            if (defeatGo  != null) ShiftButtonsContainer(defeatGo,  -240f);

            // --- Один StatsPanel на уровне Canvas (поверх обеих панелей) ---
            var statsView = BuildStatsPanelInside(gameOverCanvas);

            // --- Проставляем _statsView в GameOverController ---
            var gameOverCtrl = gameOverCanvas.GetComponent<GameOverController>();
            if (gameOverCtrl != null)
            {
                var so = new SerializedObject(gameOverCtrl);
                so.FindProperty("_statsView").objectReferenceValue = statsView;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // --- MatchStatsCollector на GameManagers ---
            var managersGo = GameObject.Find("GameManagers");
            if (managersGo != null)
            {
                var collector = EnsureComponent<MatchStatsCollector>(managersGo);

                // Проставляем ResourceBank в коллектор
                var bank = managersGo.GetComponent<ResourceBank>();
                if (bank != null)
                {
                    var so = new SerializedObject(collector);
                    so.FindProperty("_bank").objectReferenceValue = bank;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                // Проставляем коллектор в GameWatcher
                var watcher = managersGo.GetComponent<GameWatcher>();
                if (watcher != null)
                {
                    var so = new SerializedObject(watcher);
                    so.FindProperty("_statsCollector").objectReferenceValue = collector;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Project Forge] Build Match Stats Panel выполнен.");
        }

        /// <summary>
        /// Строит StatsPanel 640×260 внутри parentPanel (или на уровне Canvas).
        /// Возвращает MatchStatsView, созданный или найденный на StatsPanel.
        /// </summary>
        private static MatchStatsView BuildStatsPanelInside(GameObject parentPanel)
        {
            // --- Корень StatsPanel ---
            var statsPanelGo = EnsureChild(parentPanel, "StatsPanel");
            {
                var rt = statsPanelGo.GetComponent<RectTransform>();
                rt.anchorMin        = new Vector2(0.5f, 0.5f);
                rt.anchorMax        = new Vector2(0.5f, 0.5f);
                rt.pivot            = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(0f, 20f);
                rt.sizeDelta        = new Vector2(640f, 260f);

                var bg = EnsureComponent<Image>(statsPanelGo);
                bg.color = new Color(0.05f, 0.05f, 0.08f, 0.85f);
            }

            // --- Заголовок таблицы ---
            var headerGo = EnsureChild(statsPanelGo, "Header");
            {
                var rt = headerGo.GetComponent<RectTransform>();
                rt.anchorMin        = new Vector2(0f, 1f);
                rt.anchorMax        = new Vector2(1f, 1f);
                rt.pivot            = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -8f);
                rt.sizeDelta        = new Vector2(0f, 26f);

                BuildStatsHeaderRow(headerGo, "Показатель", "Вы", "Противник");
            }

            // --- Строки таблицы ---
            string[] rowNames  = { "Kills", "Losses", "DamageDealt", "DamageTaken", "Crystals", "Produced", "ArmyPeak" };
            string[] rowLabels = { "Убито врагов", "Потеряно", "Урон нанесён", "Урон получен",
                                   "Кристаллов добыто", "Произведено юнитов", "Пик армии" };

            float rowHeight = 26f;
            float rowStartY = -36f; // после заголовка

            for (int i = 0; i < rowNames.Length; i++)
            {
                var rowGo = EnsureChild(statsPanelGo, "Row_" + rowNames[i]);
                var rt    = rowGo.GetComponent<RectTransform>();
                rt.anchorMin        = new Vector2(0f, 1f);
                rt.anchorMax        = new Vector2(1f, 1f);
                rt.pivot            = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, rowStartY - i * rowHeight);
                rt.sizeDelta        = new Vector2(0f, rowHeight);

                BuildStatsDataRow(rowGo, rowLabels[i], rowNames[i]);
            }

            // --- Строка длительности ---
            var durationGo = EnsureChild(statsPanelGo, "DurationRow");
            {
                var rt = durationGo.GetComponent<RectTransform>();
                rt.anchorMin        = new Vector2(0f, 0f);
                rt.anchorMax        = new Vector2(1f, 0f);
                rt.pivot            = new Vector2(0.5f, 0f);
                rt.anchoredPosition = new Vector2(0f, 8f);
                rt.sizeDelta        = new Vector2(0f, 22f);

                var durationLabelGo = EnsureChild(durationGo, "DurationText");
                var durationTmp     = EnsureComponent<TextMeshProUGUI>(durationLabelGo);
                durationTmp.text      = "Длительность: --:--";
                durationTmp.fontSize  = 12f;
                durationTmp.color     = Color.white;
                durationTmp.alignment = TextAlignmentOptions.Center;

                var lrt = durationLabelGo.GetComponent<RectTransform>();
                lrt.anchorMin = Vector2.zero;
                lrt.anchorMax = Vector2.one;
                lrt.offsetMin = Vector2.zero;
                lrt.offsetMax = Vector2.zero;
            }

            // --- MatchStatsView ---
            var view = EnsureComponent<MatchStatsView>(statsPanelGo);
            {
                var so = new SerializedObject(view);

                // Игрок
                so.FindProperty("_playerKills")      .objectReferenceValue = GetRowValueTmp(statsPanelGo, "Row_Kills",       "PlayerValue");
                so.FindProperty("_playerLosses")     .objectReferenceValue = GetRowValueTmp(statsPanelGo, "Row_Losses",      "PlayerValue");
                so.FindProperty("_playerDamageDealt").objectReferenceValue = GetRowValueTmp(statsPanelGo, "Row_DamageDealt", "PlayerValue");
                so.FindProperty("_playerDamageTaken").objectReferenceValue = GetRowValueTmp(statsPanelGo, "Row_DamageTaken", "PlayerValue");
                so.FindProperty("_playerCrystals")   .objectReferenceValue = GetRowValueTmp(statsPanelGo, "Row_Crystals",    "PlayerValue");
                so.FindProperty("_playerProduced")   .objectReferenceValue = GetRowValueTmp(statsPanelGo, "Row_Produced",    "PlayerValue");
                so.FindProperty("_playerArmyPeak")   .objectReferenceValue = GetRowValueTmp(statsPanelGo, "Row_ArmyPeak",    "PlayerValue");

                // Враг
                so.FindProperty("_enemyKills")       .objectReferenceValue = GetRowValueTmp(statsPanelGo, "Row_Kills",       "EnemyValue");
                so.FindProperty("_enemyLosses")      .objectReferenceValue = GetRowValueTmp(statsPanelGo, "Row_Losses",      "EnemyValue");
                so.FindProperty("_enemyDamageDealt") .objectReferenceValue = GetRowValueTmp(statsPanelGo, "Row_DamageDealt", "EnemyValue");
                so.FindProperty("_enemyDamageTaken") .objectReferenceValue = GetRowValueTmp(statsPanelGo, "Row_DamageTaken", "EnemyValue");
                so.FindProperty("_enemyCrystals")    .objectReferenceValue = GetRowValueTmp(statsPanelGo, "Row_Crystals",    "EnemyValue");
                so.FindProperty("_enemyProduced")    .objectReferenceValue = GetRowValueTmp(statsPanelGo, "Row_Produced",    "EnemyValue");
                so.FindProperty("_enemyArmyPeak")    .objectReferenceValue = GetRowValueTmp(statsPanelGo, "Row_ArmyPeak",    "EnemyValue");

                // Длительность
                var durationTextGo = FindDescendantByName(durationGo, "DurationText");
                so.FindProperty("_durationText").objectReferenceValue =
                    durationTextGo != null ? durationTextGo.GetComponent<TextMeshProUGUI>() : null;

                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // По умолчанию скрыт (активируется из MatchStatsView.Show)
            statsPanelGo.SetActive(false);

            return view;
        }

        private static void BuildStatsHeaderRow(GameObject rowGo, string col1, string col2, string col3)
        {
            BuildStatsRowInternal(rowGo, col1, col2, col3,
                new Color(0.7f, 0.7f, 0.9f, 1f),
                new Color(0.2f, 1f, 0.2f, 1f),
                new Color(1f, 0.2f, 0.2f, 1f),
                fontSize: 13f,
                isHeader: true);
        }

        private static void BuildStatsDataRow(GameObject rowGo, string label, string rowKey)
        {
            BuildStatsRowInternal(rowGo, label, "0", "0",
                Color.white,
                new Color(0.2f, 1f, 0.2f, 1f),
                new Color(1f, 0.2f, 0.2f, 1f),
                fontSize: 12f,
                isHeader: false);
        }

        private static void BuildStatsRowInternal(GameObject rowGo, string label,
            string playerVal, string enemyVal,
            Color labelColor, Color playerColor, Color enemyColor,
            float fontSize, bool isHeader)
        {
            // Колонка 1 — Название показателя (40% ширины)
            var labelGo  = EnsureChild(rowGo, "Label");
            var labelTmp = EnsureComponent<TextMeshProUGUI>(labelGo);
            labelTmp.text      = label;
            labelTmp.fontSize  = fontSize;
            labelTmp.color     = labelColor;
            labelTmp.alignment = TextAlignmentOptions.MidlineLeft;
            if (isHeader) labelTmp.fontStyle = FontStyles.Bold;
            {
                var rt = labelGo.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f,    0f);
                rt.anchorMax = new Vector2(0.45f, 1f);
                rt.offsetMin = new Vector2(12f, 0f);
                rt.offsetMax = Vector2.zero;
            }

            // Колонка 2 — Значение игрока (центр-правый)
            var playerGo  = EnsureChild(rowGo, "PlayerValue");
            var playerTmp = EnsureComponent<TextMeshProUGUI>(playerGo);
            playerTmp.text      = playerVal;
            playerTmp.fontSize  = fontSize;
            playerTmp.color     = playerColor;
            playerTmp.alignment = TextAlignmentOptions.Center;
            if (isHeader) playerTmp.fontStyle = FontStyles.Bold;
            {
                var rt = playerGo.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.45f, 0f);
                rt.anchorMax = new Vector2(0.72f, 1f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }

            // Колонка 3 — Значение врага
            var enemyGo  = EnsureChild(rowGo, "EnemyValue");
            var enemyTmp = EnsureComponent<TextMeshProUGUI>(enemyGo);
            enemyTmp.text      = enemyVal;
            enemyTmp.fontSize  = fontSize;
            enemyTmp.color     = enemyColor;
            enemyTmp.alignment = TextAlignmentOptions.Center;
            if (isHeader) enemyTmp.fontStyle = FontStyles.Bold;
            {
                var rt = enemyGo.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.72f, 0f);
                rt.anchorMax = new Vector2(1f,    1f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = new Vector2(-12f, 0f);
            }
        }

        private static void ShiftButtonsContainer(GameObject panelGo, float deltaY)
        {
            var buttonsGo = panelGo.transform.Find("Buttons");
            if (buttonsGo == null) return;

            var rt = buttonsGo.GetComponent<RectTransform>();
            if (rt == null) return;

            // Идемпотентно: устанавливаем целевую позицию
            rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, deltaY);
        }

        private static TextMeshProUGUI GetRowValueTmp(GameObject statsPanel, string rowName, string childName)
        {
            var row   = FindDescendantByName(statsPanel, rowName);
            if (row == null) return null;
            var child = row.transform.Find(childName);
            return child != null ? child.GetComponent<TextMeshProUGUI>() : null;
        }

        // ----------------------------------------------------------------
        // v6 Command Card
        // ----------------------------------------------------------------

        /// <summary>
        /// Полная идемпотентная сборка CommandCard v6.
        /// Вызывается кнопкой и ForgeBatch.SetupCircle6().
        /// </summary>
        internal static void SetupCommandCardV6()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                EditorUtility.DisplayDialog("Project Forge", "Нет открытой сцены.", "OK");
                return;
            }

            EnsureTmpEssentials();

            // 1. Миграция данных производства
            ConfigTab.MigrateProductionEntriesV6();

            // 2. Создать HeavyMarine-префаб
            BuildHeavyMarinePrefab();

            // 3. Построить CommandCardRoot + QueueSlotsRoot в SelectionPanel
            BuildCommandCardHierarchy();

            // 4. Заполнить _unitPrefabs у Barracks и WarFactory
            WireUnitPrefabsOnBuildings();

            // 5. BuildingSpawnEffect на Barracks/WarFactory/Extractor
            EnsureBuildingSpawnEffect(BarracksPrefabPath);
            EnsureBuildingSpawnEffect(WarFactoryPrefabPath);
            EnsureBuildingSpawnEffect(ExtractorPrefabPath);

            // 6. _waveStingerClip на AudioManager в сцене
            WireWaveStingerClip();

            // 7. Сохранить сцену
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Project Forge] Setup Command Card (v6) завершён.");
        }

        // ----------------------------------------------------------------
        // v7 Tech Tree
        // ----------------------------------------------------------------

        /// <summary>
        /// Идемпотентная сборка ряда tech-кнопок (v7).
        /// Вызывается кнопкой OnGUI и ForgeBatch.SetupTechTree().
        /// </summary>
        internal static void SetupTechTreeV7()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                EditorUtility.DisplayDialog("Project Forge", "Нет открытой сцены.", "OK");
                return;
            }

            EnsureTmpEssentials();

            // 1. Создать / обновить tech-ассеты
            ConfigTab.CreateOrUpdateTechAssetsV7();

            // 2. Построить TechCardRoot + 3 tech-кнопки
            BuildTechCardRow();

            // 3. Сохранить
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Project Forge] Setup Tech Tree (v7) завершён.");
        }

        private static void BuildTechCardRow()
        {
            var gameHud = GameObject.Find("GameHUD");
            if (gameHud == null)
            {
                Debug.LogWarning("[Project Forge v7] GameHUD не найден. Сначала запустите Build Game HUD (M6a).");
                return;
            }

            var selPanelGo = FindDescendantByName(gameHud, "SelectionPanel");
            if (selPanelGo == null)
            {
                Debug.LogWarning("[Project Forge v7] SelectionPanel не найден в GameHUD.");
                return;
            }

            // --- TechCardRoot ---
            var techCardRoot = EnsureChild(selPanelGo, "TechCardRoot");
            {
                var rt = techCardRoot.GetComponent<RectTransform>();
                // Под CommandCardRoot (второй ряд), правая часть панели
                rt.anchorMin = new Vector2(0.6f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot     = new Vector2(0f, 1f);
                // Сдвигаем ниже CommandCardRoot — смещение 72px (64 + отступ)
                rt.anchoredPosition = new Vector2(0f, -72f);
                rt.offsetMin = new Vector2(8f, 4f);
                rt.offsetMax = new Vector2(-4f, -4f);

                var layout = EnsureComponent<HorizontalLayoutGroup>(techCardRoot);
                layout.spacing               = 4f;
                layout.childAlignment        = TextAnchor.MiddleLeft;
                layout.childControlWidth     = false;
                layout.childControlHeight    = false;
                layout.childForceExpandWidth  = false;
                layout.childForceExpandHeight = false;
            }

            // --- Три CommandCardButton слота для технологий ---
            var techCardButtons = new CommandCardButton[3];

            for (int i = 0; i < 3; i++)
            {
                string slotName = "TechSlot_" + i.ToString();
                var slotGo = EnsureChild(techCardRoot, slotName);

                var slotRt = slotGo.GetComponent<RectTransform>();
                slotRt.sizeDelta = new Vector2(64f, 64f);

                // Фон (чуть темнее/зеленоватый чтобы отличаться от production)
                var bgImg = EnsureComponent<Image>(slotGo);
                bgImg.color = new Color(0.12f, 0.22f, 0.18f, 0.9f);

                // Кнопка
                EnsureComponent<Button>(slotGo);

                // Иконка
                var iconGo  = EnsureChild(slotGo, "Icon");
                var iconImg = EnsureComponent<Image>(iconGo);
                iconImg.color = Color.white;
                {
                    var irt = iconGo.GetComponent<RectTransform>();
                    irt.anchorMin = new Vector2(0.1f, 0.25f);
                    irt.anchorMax = new Vector2(0.9f, 0.95f);
                    irt.offsetMin = Vector2.zero;
                    irt.offsetMax = Vector2.zero;
                }

                // Название
                var nameGo  = EnsureChild(slotGo, "UnitNameText");
                var nameTmp = EnsureComponent<TextMeshProUGUI>(nameGo);
                nameTmp.text      = "";
                nameTmp.fontSize  = 9f;
                nameTmp.color     = Color.white;
                nameTmp.alignment = TextAlignmentOptions.Center;
                nameTmp.enableWordWrapping = false;
                {
                    var nrt = nameGo.GetComponent<RectTransform>();
                    nrt.anchorMin = new Vector2(0f, 0.02f);
                    nrt.anchorMax = new Vector2(1f, 0.28f);
                    nrt.offsetMin = Vector2.zero;
                    nrt.offsetMax = Vector2.zero;
                }

                // Стоимость
                var costGo  = EnsureChild(slotGo, "CostText");
                var costTmp = EnsureComponent<TextMeshProUGUI>(costGo);
                costTmp.text      = "";
                costTmp.fontSize  = 9f;
                costTmp.color     = new Color(0.9f, 0.85f, 0.2f, 1f);
                costTmp.alignment = TextAlignmentOptions.BottomLeft;
                {
                    var crt = costGo.GetComponent<RectTransform>();
                    crt.anchorMin = new Vector2(0f, 0f);
                    crt.anchorMax = new Vector2(0.5f, 0.28f);
                    crt.offsetMin = new Vector2(2f, 0f);
                    crt.offsetMax = Vector2.zero;
                }

                // Хоткей
                var keyGo  = EnsureChild(slotGo, "HotkeyText");
                var keyTmp = EnsureComponent<TextMeshProUGUI>(keyGo);
                keyTmp.text      = "";
                keyTmp.fontSize  = 9f;
                keyTmp.color     = new Color(0.7f, 0.9f, 0.7f, 1f);
                keyTmp.alignment = TextAlignmentOptions.BottomRight;
                {
                    var krt = keyGo.GetComponent<RectTransform>();
                    krt.anchorMin = new Vector2(0.5f, 0f);
                    krt.anchorMax = new Vector2(1f, 0.28f);
                    krt.offsetMin = Vector2.zero;
                    krt.offsetMax = new Vector2(-2f, 0f);
                }

                // ResearchedOverlay — полупрозрачный зелёный Image + TMP "✓"
                var overlayGo = EnsureChild(slotGo, "ResearchedOverlay");
                {
                    var overlayImg = EnsureComponent<Image>(overlayGo);
                    overlayImg.color = new Color(0.2f, 0.8f, 0.3f, 0.6f);

                    var overlayRt = overlayGo.GetComponent<RectTransform>();
                    overlayRt.anchorMin = Vector2.zero;
                    overlayRt.anchorMax = Vector2.one;
                    overlayRt.offsetMin = Vector2.zero;
                    overlayRt.offsetMax = Vector2.zero;

                    // TMP "✓"
                    var checkGo  = EnsureChild(overlayGo, "CheckMark");
                    var checkTmp = EnsureComponent<TextMeshProUGUI>(checkGo);
                    checkTmp.text      = "✓";
                    checkTmp.fontSize  = 28f;
                    checkTmp.color     = Color.white;
                    checkTmp.alignment = TextAlignmentOptions.Center;
                    {
                        var crt = checkGo.GetComponent<RectTransform>();
                        crt.anchorMin = Vector2.zero;
                        crt.anchorMax = Vector2.one;
                        crt.offsetMin = Vector2.zero;
                        crt.offsetMax = Vector2.zero;
                    }

                    overlayGo.SetActive(false);
                }

                // CommandCardButton
                var btn = EnsureComponent<CommandCardButton>(slotGo);
                {
                    var so = new SerializedObject(btn);
                    so.FindProperty("iconImage").objectReferenceValue      = iconImg;
                    so.FindProperty("button").objectReferenceValue         = slotGo.GetComponent<Button>();
                    so.FindProperty("unitNameText").objectReferenceValue   = nameTmp;
                    so.FindProperty("costText").objectReferenceValue       = costTmp;
                    so.FindProperty("hotkeyText").objectReferenceValue     = keyTmp;
                    so.FindProperty("slotIndex").intValue                  = i;
                    so.FindProperty("researchedOverlay").objectReferenceValue = overlayGo;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                // TooltipTrigger
                EnsureComponent<TooltipTrigger>(slotGo);

                techCardButtons[i] = btn;

                // По умолчанию скрыт
                slotGo.SetActive(false);
            }

            // TechCardRoot тоже скрыт по умолчанию
            techCardRoot.SetActive(false);

            // --- Проставить ссылки в SelectionPanel ---
            var selPanel = selPanelGo.GetComponent<SelectionPanel>();
            if (selPanel != null)
            {
                var so = new SerializedObject(selPanel);

                so.FindProperty("techCardRoot").objectReferenceValue = techCardRoot;

                var techSlotsProp = so.FindProperty("techCardSlots");
                techSlotsProp.arraySize = 3;
                for (int i = 0; i < 3; i++)
                    techSlotsProp.GetArrayElementAtIndex(i).objectReferenceValue = techCardButtons[i];

                so.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                Debug.LogWarning("[Project Forge v7] SelectionPanel компонент не найден.");
            }
        }

        // ----------------------------------------------------------------
        // v6 — HeavyMarine префаб
        // ----------------------------------------------------------------

        private static void BuildHeavyMarinePrefab()
        {
            EnsureFolder("Assets/_Project/Prefabs/Units");
            EnsureFolder("Assets/_Project/Art/Animations");

            var stanFbx = AssetDatabase.LoadAssetAtPath<GameObject>(StanFbxPath);
            if (stanFbx == null)
            {
                Debug.LogWarning($"[Project Forge v6] Stan.fbx не найден по пути: {StanFbxPath}. " +
                                 "HeavyMarine-префаб не будет иметь анимированной модели.");
            }

            // Настроить импорт Stan.fbx
            if (stanFbx != null)
                ConfigureHeavyMarineImporter(StanFbxPath);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Загрузить клипы и создать контроллер
            var clips      = LoadAnimClips(StanFbxPath);
            var controller = BuildHeavyMarineController(HeavyMarineControllerPath, clips);

            // Если префаб уже есть — обновляем через EditPrefabContentsScope
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(HeavyMarinePrefabPath);
            if (existing != null)
            {
                ApplyHeavyMarineVisual(HeavyMarinePrefabPath, stanFbx, controller);
            }
            else
            {
                // Создаём новый префаб из capsule (как в PrefabsTab)
                var root = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                root.name = "HeavyMarineUnit";

                // NavMeshAgent
                var agent = EnsureComponent<NavMeshAgent>(root);
                agent.speed                 = 4.2f;
                agent.angularSpeed          = 360f;
                agent.acceleration          = 12f;
                agent.radius                = 0.5f;
                agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
                agent.avoidancePriority     = 45;
                agent.stoppingDistance      = 0.5f;

                // Unit (Player)
                var unit   = EnsureComponent<Unit>(root);
                var unitSo = new SerializedObject(unit);
                unitSo.FindProperty("_faction").enumValueIndex = (int)Faction.Player;
                unitSo.ApplyModifiedPropertiesWithoutUndo();

                // Health
                EnsureComponent<Health>(root);

                // UnitCombat (HeavyMarine.asset)
                var combat     = EnsureComponent<UnitCombat>(root);
                var heavyData  = AssetDatabase.LoadAssetAtPath<UnitData>(HeavyMarineDataPath);
                if (heavyData != null)
                {
                    var combatSo = new SerializedObject(combat);
                    combatSo.FindProperty("_data").objectReferenceValue = heavyData;
                    combatSo.ApplyModifiedPropertiesWithoutUndo();
                }

                // Сохраняем как префаб
                var prefab = PrefabUtility.SaveAsPrefabAsset(root, HeavyMarinePrefabPath);
                UnityEngine.Object.DestroyImmediate(root);

                if (prefab == null)
                {
                    Debug.LogError($"[Project Forge v6] Не удалось сохранить HeavyMarineUnit.prefab: {HeavyMarinePrefabPath}");
                    return;
                }

                // Применяем анимированный визуал
                ApplyHeavyMarineVisual(HeavyMarinePrefabPath, stanFbx, controller);
            }

            Debug.Log($"[Project Forge v6] HeavyMarineUnit.prefab готов: {HeavyMarinePrefabPath}");
        }

        private static void ConfigureHeavyMarineImporter(string fbxPath)
        {
            var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer == null) return;

            bool changed = false;

            if (importer.animationType != ModelImporterAnimationType.Generic)
            { importer.animationType = ModelImporterAnimationType.Generic; changed = true; }

            if (!importer.importAnimation)
            { importer.importAnimation = true; changed = true; }

            if (importer.importCameras) { importer.importCameras = false; changed = true; }
            if (importer.importLights)  { importer.importLights  = false; changed = true; }

            if (importer.materialImportMode != ModelImporterMaterialImportMode.ImportViaMaterialDescription)
            { importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription; changed = true; }

            if (changed)
            {
                importer.SaveAndReimport();
                Debug.Log($"[Project Forge v6] Импорт Stan.fbx обновлён.");
            }
        }

        private static AnimationClip[] LoadAnimClips(string fbxPath)
        {
            var allAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            var clips     = new List<AnimationClip>();
            foreach (var asset in allAssets)
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                    clips.Add(clip);
            }
            return clips.ToArray();
        }

        private static AnimatorController BuildHeavyMarineController(string controllerPath, AnimationClip[] clips)
        {
            AnimationClip FindClip(params string[] keywords)
            {
                foreach (var clip in clips)
                {
                    string lower = clip.name.ToLowerInvariant();
                    foreach (var kw in keywords)
                        if (lower.Contains(kw.ToLowerInvariant()))
                            return clip;
                }
                return null;
            }

            var idleClip   = FindClip("idle");
            // Stan FBX: Walk_Holding — это ходьба
            var runClip    = FindClip("walk_holding", "walk", "run");
            var attackClip = FindClip("shoot", "fire", "attack", "punch", "kick");
            var deathClip  = FindClip("death", "die", "dead");

            if (idleClip == null && clips.Length > 0) idleClip = clips[0];

            // Удалить старый контроллер (идемпотентность)
            var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (existing != null) AssetDatabase.DeleteAsset(controllerPath);

            var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

            controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Attack",   AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Die",      AnimatorControllerParameterType.Trigger);

            var sm = controller.layers[0].stateMachine;

            var idleState   = sm.AddState("Idle");   idleState.motion   = idleClip;
            var runState    = sm.AddState("Run");     runState.motion    = runClip;
            var attackState = sm.AddState("Attack");  attackState.motion = attackClip;
            var deathState  = sm.AddState("Death");   deathState.motion  = deathClip;
            sm.defaultState = idleState;

            // Idle → Run
            var i2r = idleState.AddTransition(runState);
            i2r.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");
            i2r.hasExitTime = false; i2r.duration = 0.1f;

            // Run → Idle
            var r2i = runState.AddTransition(idleState);
            r2i.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");
            r2i.hasExitTime = false; r2i.duration = 0.1f;

            // Any → Attack
            var a2atk = sm.AddAnyStateTransition(attackState);
            a2atk.AddCondition(AnimatorConditionMode.If, 0, "Attack");
            a2atk.hasExitTime = false; a2atk.duration = 0.05f; a2atk.canTransitionToSelf = false;

            // Attack → Idle
            var atk2i = attackState.AddTransition(idleState);
            atk2i.hasExitTime = true; atk2i.exitTime = 0.9f; atk2i.duration = 0.1f;

            // Any → Death
            var a2d = sm.AddAnyStateTransition(deathState);
            a2d.AddCondition(AnimatorConditionMode.If, 0, "Die");
            a2d.hasExitTime = false; a2d.duration = 0.05f; a2d.canTransitionToSelf = false;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Project Forge v6] HeavyMarine AnimatorController создан: {controllerPath} " +
                      $"[Idle={idleClip?.name ?? "null"}, Run={runClip?.name ?? "null"}, " +
                      $"Attack={attackClip?.name ?? "null"}, Death={deathClip?.name ?? "null"}]");

            return controller;
        }

        private static void ApplyHeavyMarineVisual(string prefabPath, GameObject fbxAsset, AnimatorController controller)
        {
            if (fbxAsset == null) return;

            var playerMat = AssetDatabase.LoadAssetAtPath<Material>(PlayerBluePath);

            using (var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
            {
                var root = scope.prefabContentsRoot;

                // Скрыть MeshRenderer капсулы
                var capsuleMr = root.GetComponent<MeshRenderer>();
                if (capsuleMr != null) capsuleMr.enabled = false;

                // Удалить старый Visual
                var oldVisual = root.transform.Find("Visual");
                if (oldVisual != null) UnityEngine.Object.DestroyImmediate(oldVisual.gameObject);

                // Создать Visual из Stan.fbx
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(fbxAsset, root.transform);
                visual.name = "Visual";
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                visual.transform.localScale    = Vector3.one;

                // Нормализация по баундам
                NormalizeVisualByBounds(visual, HeavyMarineTargetHeight);

                // Team-color материал
                if (playerMat != null)
                {
                    foreach (var mr  in visual.GetComponentsInChildren<MeshRenderer>(true))
                        mr.sharedMaterial = playerMat;
                    foreach (var smr in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                        smr.sharedMaterial = playerMat;
                }

                // Animator на Visual («??» не работает с Unity-объектами — fake-null)
                var animator = visual.GetComponent<Animator>();
                if (animator == null) animator = visual.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;

                // UnitAnimator на корне
                if (root.GetComponent<UnitAnimator>() == null)
                    root.AddComponent<UnitAnimator>();
            }
        }

        private static void NormalizeVisualByBounds(GameObject visual, float targetSize)
        {
            var renderers = visual.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;

            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            float maxDim = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (maxDim < 0.0001f) return;

            float factor = targetSize / maxDim;
            visual.transform.localScale = Vector3.one * factor;
        }

        // ----------------------------------------------------------------
        // v6 — CommandCard иерархия в SelectionPanel
        // ----------------------------------------------------------------

        private static void BuildCommandCardHierarchy()
        {
            var gameHud = GameObject.Find("GameHUD");
            if (gameHud == null)
            {
                Debug.LogWarning("[Project Forge v6] GameHUD не найден. Сначала запустите Build Game HUD (M6a).");
                return;
            }

            var selPanelGo = FindDescendantByName(gameHud, "SelectionPanel");
            if (selPanelGo == null)
            {
                Debug.LogWarning("[Project Forge v6] SelectionPanel не найден в GameHUD.");
                return;
            }

            // --- CommandCardRoot ---
            var commandCardRoot = EnsureChild(selPanelGo, "CommandCardRoot");
            {
                var rt = commandCardRoot.GetComponent<RectTransform>();
                // Правая часть панели: anchorMin/Max справа
                rt.anchorMin = new Vector2(0.6f, 0f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.offsetMin = new Vector2(8f,  4f);
                rt.offsetMax = new Vector2(-4f, -4f);

                // Горизонтальный layout для кнопок
                var layout = EnsureComponent<HorizontalLayoutGroup>(commandCardRoot);
                layout.spacing               = 4f;
                layout.childAlignment        = TextAnchor.MiddleLeft;
                layout.childControlWidth     = false;
                layout.childControlHeight    = false;
                layout.childForceExpandWidth  = false;
                layout.childForceExpandHeight = false;
            }

            // --- Три CommandCardButton слота ---
            var commandCardButtons = new CommandCardButton[3];

            for (int i = 0; i < 3; i++)
            {
                string slotName = "CommandSlot_" + i.ToString();
                var slotGo = EnsureChild(commandCardRoot, slotName);

                var slotRt = slotGo.GetComponent<RectTransform>();
                slotRt.sizeDelta = new Vector2(64f, 64f);

                // Фон
                var bgImg = EnsureComponent<Image>(slotGo);
                bgImg.color = new Color(0.15f, 0.20f, 0.30f, 0.9f);

                // Кнопка
                EnsureComponent<Button>(slotGo);

                // Иконка (дочерний Image)
                var iconGo  = EnsureChild(slotGo, "Icon");
                var iconImg = EnsureComponent<Image>(iconGo);
                iconImg.color = Color.white;
                {
                    var irt = iconGo.GetComponent<RectTransform>();
                    irt.anchorMin = new Vector2(0.1f, 0.25f);
                    irt.anchorMax = new Vector2(0.9f, 0.95f);
                    irt.offsetMin = Vector2.zero;
                    irt.offsetMax = Vector2.zero;
                }

                // Название (UnitNameText)
                var nameGo  = EnsureChild(slotGo, "UnitNameText");
                var nameTmp = EnsureComponent<TextMeshProUGUI>(nameGo);
                nameTmp.text      = "";
                nameTmp.fontSize  = 9f;
                nameTmp.color     = Color.white;
                nameTmp.alignment = TextAlignmentOptions.Center;
                nameTmp.enableWordWrapping = false;
                {
                    var nrt = nameGo.GetComponent<RectTransform>();
                    nrt.anchorMin = new Vector2(0f, 0.02f);
                    nrt.anchorMax = new Vector2(1f, 0.28f);
                    nrt.offsetMin = Vector2.zero;
                    nrt.offsetMax = Vector2.zero;
                }

                // Стоимость (левый нижний)
                var costGo  = EnsureChild(slotGo, "CostText");
                var costTmp = EnsureComponent<TextMeshProUGUI>(costGo);
                costTmp.text      = "";
                costTmp.fontSize  = 9f;
                costTmp.color     = new Color(0.9f, 0.85f, 0.2f, 1f);
                costTmp.alignment = TextAlignmentOptions.BottomLeft;
                {
                    var crt = costGo.GetComponent<RectTransform>();
                    crt.anchorMin = new Vector2(0f, 0f);
                    crt.anchorMax = new Vector2(0.5f, 0.28f);
                    crt.offsetMin = new Vector2(2f, 0f);
                    crt.offsetMax = Vector2.zero;
                }

                // Хоткей (правый нижний)
                var keyGo  = EnsureChild(slotGo, "HotkeyText");
                var keyTmp = EnsureComponent<TextMeshProUGUI>(keyGo);
                keyTmp.text      = "";
                keyTmp.fontSize  = 9f;
                keyTmp.color     = new Color(0.7f, 0.9f, 0.7f, 1f);
                keyTmp.alignment = TextAlignmentOptions.BottomRight;
                {
                    var krt = keyGo.GetComponent<RectTransform>();
                    krt.anchorMin = new Vector2(0.5f, 0f);
                    krt.anchorMax = new Vector2(1f, 0.28f);
                    krt.offsetMin = Vector2.zero;
                    krt.offsetMax = new Vector2(-2f, 0f);
                }

                // CommandCardButton компонент
                var btn = EnsureComponent<CommandCardButton>(slotGo);
                {
                    var so = new SerializedObject(btn);
                    so.FindProperty("iconImage").objectReferenceValue    = iconImg;
                    so.FindProperty("button").objectReferenceValue       = slotGo.GetComponent<Button>();
                    so.FindProperty("unitNameText").objectReferenceValue = nameTmp;
                    so.FindProperty("costText").objectReferenceValue     = costTmp;
                    so.FindProperty("hotkeyText").objectReferenceValue   = keyTmp;
                    so.FindProperty("slotIndex").intValue                = i;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                // TooltipTrigger (CommandCardButton сам реализует ITooltipProvider)
                EnsureComponent<TooltipTrigger>(slotGo);

                commandCardButtons[i] = btn;

                // По умолчанию скрыт (SelectionPanel.Hide() включит/выключит)
                slotGo.SetActive(false);
            }

            // --- QueueSlotsRoot ---
            var queueSlotsRoot = EnsureChild(selPanelGo, "QueueSlotsRoot");
            {
                var rt = queueSlotsRoot.GetComponent<RectTransform>();
                // Тонкая полоска над командной картой
                rt.anchorMin = new Vector2(0.6f, 0f);
                rt.anchorMax = new Vector2(1f, 0f);
                rt.pivot     = new Vector2(0f, 1f);
                // Позиционируем над CommandCardRoot
                rt.anchoredPosition = new Vector2(0f, -4f); // relative to anchor bottom
                rt.offsetMin = new Vector2(8f, 4f);
                rt.offsetMax = new Vector2(-4f, -4f);

                // Горизонтальный layout для слотов очереди
                var layout = EnsureComponent<HorizontalLayoutGroup>(queueSlotsRoot);
                layout.spacing               = 3f;
                layout.childAlignment        = TextAnchor.MiddleLeft;
                layout.childControlWidth     = false;
                layout.childControlHeight    = false;
                layout.childForceExpandWidth  = false;
                layout.childForceExpandHeight = false;
            }

            // --- Пять QueueSlot слотов ---
            var queueSlotComponents = new QueueSlotUI[5];

            for (int i = 0; i < 5; i++)
            {
                string slotName = "QueueSlot_" + i.ToString();
                var slotGo = EnsureChild(queueSlotsRoot, slotName);

                var slotRt = slotGo.GetComponent<RectTransform>();
                slotRt.sizeDelta = new Vector2(26f, 26f);

                // Иконка-фон
                var iconImg = EnsureComponent<Image>(slotGo);
                iconImg.color = new Color(0.25f, 0.30f, 0.40f, 0.9f);

                Image progressOverlay = null;

                // Overlay прогресса — только у слота 0
                if (i == 0)
                {
                    var overlayGo = EnsureChild(slotGo, "ProgressOverlay");
                    progressOverlay = EnsureComponent<Image>(overlayGo);
                    progressOverlay.color      = new Color(1f, 1f, 1f, 0.35f);
                    progressOverlay.type       = Image.Type.Filled;
                    progressOverlay.fillMethod = Image.FillMethod.Vertical;
                    progressOverlay.fillOrigin = (int)Image.OriginVertical.Bottom;
                    progressOverlay.fillAmount = 0f;

                    var overlayRt = overlayGo.GetComponent<RectTransform>();
                    overlayRt.anchorMin = Vector2.zero;
                    overlayRt.anchorMax = Vector2.one;
                    overlayRt.offsetMin = Vector2.zero;
                    overlayRt.offsetMax = Vector2.zero;
                }

                // QueueSlotUI компонент
                var qSlot = EnsureComponent<QueueSlotUI>(slotGo);
                {
                    var so = new SerializedObject(qSlot);
                    so.FindProperty("iconImage").objectReferenceValue      = iconImg;
                    so.FindProperty("progressOverlay").objectReferenceValue = progressOverlay;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                queueSlotComponents[i] = qSlot;

                // По умолчанию скрыт
                slotGo.SetActive(false);
            }

            // --- Проставить ссылки в SelectionPanel ---
            var selPanel = selPanelGo.GetComponent<SelectionPanel>();
            if (selPanel != null)
            {
                var so = new SerializedObject(selPanel);

                so.FindProperty("commandCardRoot").objectReferenceValue = commandCardRoot;

                // commandCardSlots — массив из 3 элементов
                var slotsProp = so.FindProperty("commandCardSlots");
                slotsProp.arraySize = 3;
                for (int i = 0; i < 3; i++)
                    slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = commandCardButtons[i];

                // queueSlots — массив из 5 элементов
                var queueProp = so.FindProperty("queueSlots");
                queueProp.arraySize = 5;
                for (int i = 0; i < 5; i++)
                    queueProp.GetArrayElementAtIndex(i).objectReferenceValue = queueSlotComponents[i];

                so.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                Debug.LogWarning("[Project Forge v6] SelectionPanel компонент не найден на GameObject 'SelectionPanel'.");
            }
        }

        // ----------------------------------------------------------------
        // v6 — _unitPrefabs у Building-префабов
        // ----------------------------------------------------------------

        private static void WireUnitPrefabsOnBuildings()
        {
            // Barracks: Marine → TestUnit.prefab, HeavyMarine → HeavyMarineUnit.prefab
            WireUnitPrefabsOnPrefab(BarracksPrefabPath, new[]
            {
                (MarineDataPath,      MarinePrefabPath),
                (HeavyMarineDataPath, HeavyMarinePrefabPath),
            });

            // WarFactory: Tank → TankUnit.prefab
            WireUnitPrefabsOnPrefab(WarFactoryPrefabPath, new[]
            {
                (TankDataPath, TankPrefabPath),
            });
        }

        private static void WireUnitPrefabsOnPrefab(string prefabPath, (string dataPath, string prefabRef)[] mappings)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[Project Forge v6] Префаб не найден: {prefabPath}");
                return;
            }

            using (var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
            {
                var root = scope.prefabContentsRoot;
                var prod = root.GetComponent<ProductionBuilding>();
                if (prod == null)
                {
                    Debug.LogWarning($"[Project Forge v6] ProductionBuilding не найден на: {prefabPath}");
                    return;
                }

                var so          = new SerializedObject(prod);
                var unitPrefabs = so.FindProperty("_unitPrefabs");

                unitPrefabs.arraySize = mappings.Length;

                for (int i = 0; i < mappings.Length; i++)
                {
                    var (dataPath, prefabRef) = mappings[i];
                    var unitData    = AssetDatabase.LoadAssetAtPath<UnitData>(dataPath);
                    var unitPrefab  = AssetDatabase.LoadAssetAtPath<GameObject>(prefabRef);

                    var element = unitPrefabs.GetArrayElementAtIndex(i);
                    element.FindPropertyRelative("unitData").objectReferenceValue = unitData;
                    element.FindPropertyRelative("prefab").objectReferenceValue   = unitPrefab;
                }

                so.ApplyModifiedPropertiesWithoutUndo();
            }

            Debug.Log($"[Project Forge v6] _unitPrefabs настроен: {prefabPath}");
        }

        // ----------------------------------------------------------------
        // v6 — BuildingSpawnEffect на префабах
        // ----------------------------------------------------------------

        private static void EnsureBuildingSpawnEffect(string prefabPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[Project Forge v6] Префаб не найден: {prefabPath}");
                return;
            }

            using (var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
            {
                var root = scope.prefabContentsRoot;
                // Идемпотентно — EnsureComponent не дублирует
                if (root.GetComponent<BuildingSpawnEffect>() == null)
                    root.AddComponent<BuildingSpawnEffect>();
            }

            Debug.Log($"[Project Forge v6] BuildingSpawnEffect добавлен/проверен: {prefabPath}");
        }

        // ----------------------------------------------------------------
        // v6 — _waveStingerClip на AudioManager в сцене
        // ----------------------------------------------------------------

        private static void WireWaveStingerClip()
        {
            var audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>(FightOggPath);
            if (audioClip == null)
            {
                Debug.LogWarning($"[Project Forge v6] fight.ogg не найден: {FightOggPath}");
                return;
            }

            var audioManager = UnityEngine.Object.FindFirstObjectByType<AudioManager>();
            if (audioManager == null)
            {
                Debug.LogWarning("[Project Forge v6] AudioManager не найден в сцене. " +
                                 "Запустите Setup Audio (M7) сначала.");
                return;
            }

            var so = new SerializedObject(audioManager);
            var prop = so.FindProperty("_waveStingerClip");
            if (prop != null)
            {
                prop.objectReferenceValue = audioClip;
                so.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log("[Project Forge v6] _waveStingerClip = fight.ogg проставлен.");
            }
            else
            {
                Debug.LogWarning("[Project Forge v6] Свойство _waveStingerClip не найдено на AudioManager.");
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
            var existing = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
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

        // ----------------------------------------------------------------
        // v10: Apply Localization to Scene UI
        // ----------------------------------------------------------------

        /// <summary>
        /// Идемпотентно:
        /// 1. Добавляет LocServiceBootstrap на GameManagers (со ссылкой на LocTable.asset).
        /// 2. Добавляет LanguageRow в SettingsPanel (TMP_Dropdown RU/EN + локализованный Label).
        /// 3. Навешивает LocalizedText на известные статические TMP-надписи.
        /// Работает для Sandbox и MainMenu.
        /// </summary>
        internal static void ApplyLocalizationToSceneUI_V10()
        {
            const string locTablePath = "Assets/_Project/Data/Localization/LocTable.asset";

            var locTable = AssetDatabase.LoadAssetAtPath<DiplomaGame.Runtime.Data.LocTable>(locTablePath);
            if (locTable == null)
                Debug.LogWarning("[Forge v10] LocTable.asset не найден — запустите 'Create/Update LocTable (v10)' сначала. LocServiceBootstrap будет добавлен без ссылки.");

            // ---- 1. LocServiceBootstrap на GameManagers ----
            var managers = GameObject.Find("GameManagers");
            if (managers != null)
            {
                var bootstrap = EnsureComponent<DiplomaGame.Runtime.Core.Localization.LocServiceBootstrap>(managers);
                if (locTable != null)
                {
                    var so = new SerializedObject(bootstrap);
                    so.FindProperty("_locTable").objectReferenceValue = locTable;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                Debug.Log("[Forge v10] LocServiceBootstrap добавлен на GameManagers.");
            }
            else
            {
                Debug.LogWarning("[Forge v10] GameManagers не найден в сцене — LocServiceBootstrap пропущен.");
            }

            // ---- 2. LanguageRow в SettingsPanel ----
            // Ищем SettingsPanel в иерархии (внутри PauseMenu или как self-object)
            var allSettings = new List<SettingsPanel>(
                GameObject.FindObjectsByType<SettingsPanel>(FindObjectsSortMode.None));

            foreach (var sp in allSettings)
                AddLanguageRowToSettingsPanel(sp);

            // ---- 3. LocalizedText на статические TMP-надписи ----
            // PauseMenu
            AttachLocalizedText("PauseMenuCanvas/PausePanel/Title",       "pause.title");
            AttachLocalizedText("PauseMenuCanvas/PausePanel/BtnContinue/Label", "pause.btn_continue");
            AttachLocalizedText("PauseMenuCanvas/PausePanel/BtnSettings/Label", "pause.btn_settings");
            AttachLocalizedText("PauseMenuCanvas/PausePanel/BtnExitMenu/Label", "pause.btn_exit_menu");
            AttachLocalizedText("PauseMenuCanvas/PausePanel/BtnQuit/Label",     "pause.btn_quit");

            // GameOver — Victory
            AttachLocalizedText("GameOverCanvas/VictoryPanel/Title",          "gameover.victory");
            AttachLocalizedText("GameOverCanvas/VictoryPanel/BtnRestart/Label",   "gameover.btn_restart");
            AttachLocalizedText("GameOverCanvas/VictoryPanel/BtnMainMenu/Label",  "gameover.btn_main_menu");

            // GameOver — Defeat
            AttachLocalizedText("GameOverCanvas/DefeatPanel/Title",           "gameover.defeat");
            AttachLocalizedText("GameOverCanvas/DefeatPanel/BtnRestart/Label",    "gameover.btn_restart");
            AttachLocalizedText("GameOverCanvas/DefeatPanel/BtnMainMenu/Label",   "gameover.btn_main_menu");

            // SettingsPanel — заголовок (Title внутри найденной SettingsPanel)
            foreach (var sp in allSettings)
            {
                var titleGo = FindDescendantByName(sp.gameObject, "Title");
                if (titleGo != null)
                    AttachLocalizedTextToGo(titleGo, "settings.title");

                var backLabelGo = FindDescendantByName(sp.gameObject, "Label");
                // "Label" встречается много раз — ищем именно в BtnBack
                var btnBack = FindDescendantByName(sp.gameObject, "BtnBack");
                if (btnBack != null)
                {
                    var lbl = FindDescendantByName(btnBack, "Label");
                    if (lbl != null) AttachLocalizedTextToGo(lbl, "settings.btn_back");
                }
            }

            // MainMenu кнопки (если сцена — MainMenu)
            AttachLocalizedText("MainMenuCanvas/Panel/BtnPlay/Label",     "menu.btn_play");
            AttachLocalizedText("MainMenuCanvas/Panel/BtnSettings/Label", "menu.btn_settings");
            AttachLocalizedText("MainMenuCanvas/Panel/BtnQuit/Label",     "menu.btn_quit");
            AttachLocalizedText("MainMenuCanvas/Panel/Title",             "menu.title");

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            Debug.Log("[Forge v10] Localization applied to Scene UI.");
        }

        /// <summary>
        /// Добавляет LanguageRow с TMP_Dropdown в контент SettingsPanel, если её ещё нет.
        /// Также прописывает languageDropdown в компонент SettingsPanel.
        /// </summary>
        private static void AddLanguageRowToSettingsPanel(SettingsPanel settingsPanel)
        {
            if (settingsPanel == null) return;

            // Ищем Content (VerticalLayoutGroup)
            var contentGo = FindDescendantByName(settingsPanel.gameObject, "Content");
            if (contentGo == null)
            {
                Debug.LogWarning("[Forge v10] Content в SettingsPanel не найден — пропускаем LanguageRow.");
                return;
            }

            // Идемпотентно: если LanguageRow уже есть — только обновляем ссылку
            var langRowGo = FindDescendantByName(contentGo, "LanguageRow");
            if (langRowGo == null)
            {
                langRowGo = EnsureChild(contentGo, "LanguageRow");
                SetRowSize(langRowGo, 50f);
                EnsureRowLabel(langRowGo, "Язык");

                var dropdownGo = EnsureChild(langRowGo, "LanguageDropdown");
                var langDropdown = EnsureComponent<TMP_Dropdown>(dropdownGo);
                SetControlRectRight(dropdownGo, 220f, 40f);

                // Прописываем languageDropdown в SettingsPanel
                var sp = new SerializedObject(settingsPanel);
                sp.FindProperty("languageDropdown").objectReferenceValue = langDropdown;
                sp.ApplyModifiedPropertiesWithoutUndo();
            }
            else
            {
                // Уже есть — только обновляем ссылку если нужно
                var dropdownGo = FindDescendantByName(langRowGo, "LanguageDropdown");
                if (dropdownGo != null)
                {
                    var langDropdown = dropdownGo.GetComponent<TMP_Dropdown>();
                    if (langDropdown != null)
                    {
                        var sp = new SerializedObject(settingsPanel);
                        sp.FindProperty("languageDropdown").objectReferenceValue = langDropdown;
                        sp.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
            }
        }

        /// <summary>
        /// Находит объект по пути в иерархии сцены (не через Transform.Find — ищет по части пути).
        /// Путь вида "PauseMenuCanvas/PausePanel/Title" ищет через GameObject.Find.
        /// </summary>
        private static void AttachLocalizedText(string hierarchyPath, string locKey)
        {
            var go = GameObject.Find(hierarchyPath);
            if (go == null) return;
            AttachLocalizedTextToGo(go, locKey);
        }

        private static void AttachLocalizedTextToGo(GameObject go, string locKey)
        {
            if (go == null) return;

            // Убеждаемся что на объекте есть TMP_Text
            var tmp = go.GetComponent<TMP_Text>();
            if (tmp == null) return;

            var locText = EnsureComponent<DiplomaGame.Runtime.UI.LocalizedText>(go);
            var so = new SerializedObject(locText);
            so.FindProperty("locKey").stringValue = locKey;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ----------------------------------------------------------------
        // C16: Under Attack Alert
        // ----------------------------------------------------------------

        /// <summary>
        /// Идемпотентно создаёт и проводит систему оповещения «под атакой» (Circle-16):
        /// <list type="number">
        ///   <item>Создаёт "UnderAttackVignette" (Image, full-stretch, красный, alpha=0, raycastTarget=false) в GameHUD.</item>
        ///   <item>Создаёт "ThreatMarker" (Image, 16×16, красный, скрытый) в MinimapDisplay.</item>
        ///   <item>Добавляет <see cref="UnderAttackAlert"/> на "GameManagers".</item>
        ///   <item>Прописывает все поля через SerializedObject.</item>
        /// </list>
        /// Требует: BuildGameHUD (M6a) уже выполнен.
        /// </summary>
        internal static void BuildUnderAttackAlert()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                EditorUtility.DisplayDialog("Project Forge", "Нет открытой сцены.", "OK");
                return;
            }

            // ---- 1. GameHUD Canvas ----
            var gameHudGo = GameObject.Find("GameHUD");
            if (gameHudGo == null)
            {
                Debug.LogError("[Forge C16] GameHUD не найден — сначала запустите 'Build Game HUD (M6a)'.");
                return;
            }

            // ---- 2. UnderAttackVignette в GameHUD ----
            var vignetteGo = EnsureChild(gameHudGo, "UnderAttackVignette");
            SetFullStretch(vignetteGo);

            var vignetteImg = EnsureComponent<Image>(vignetteGo);
            // Красный цвет, полностью прозрачный по умолчанию
            vignetteImg.color          = new Color(0.85f, 0.05f, 0.05f, 0f);
            vignetteImg.raycastTarget  = false;
            // Image Type Simple — не нужно заполнять; alpha управляет видимостью
            vignetteImg.type           = Image.Type.Simple;

            // Убедимся, что виньетка находится поверх остальных дочерних элементов канваса
            vignetteGo.transform.SetAsLastSibling();

            // ---- 3. ThreatMarker в MinimapDisplay ----
            var rtsBlockGo = FindDescendantByName(gameHudGo, "RTS_Block");
            if (rtsBlockGo == null)
            {
                Debug.LogError("[Forge C16] RTS_Block не найден в GameHUD — сначала запустите 'Build Game HUD (M6a)'.");
                return;
            }

            var minimapDisplayGo = FindDescendantByName(rtsBlockGo, "MinimapDisplay");
            if (minimapDisplayGo == null)
            {
                Debug.LogError("[Forge C16] MinimapDisplay не найден в RTS_Block — сначала запустите 'Build Game HUD (M6a)'.");
                return;
            }

            var markerGo = EnsureChild(minimapDisplayGo, "ThreatMarker");
            var markerRt = markerGo.GetComponent<RectTransform>() ?? markerGo.AddComponent<RectTransform>();
            markerRt.anchorMin        = new Vector2(0.5f, 0.5f);
            markerRt.anchorMax        = new Vector2(0.5f, 0.5f);
            markerRt.pivot            = new Vector2(0.5f, 0.5f);
            markerRt.anchoredPosition = Vector2.zero;
            markerRt.sizeDelta        = new Vector2(16f, 16f);

            var markerImg = EnsureComponent<Image>(markerGo);
            markerImg.color         = new Color(1f, 0.1f, 0.1f, 1f);
            markerImg.raycastTarget = false;
            markerGo.SetActive(false); // скрыт до первого алерта

            // ---- 4. Получаем MinimapCamera из MinimapController ----
            var minimapCtrl = UnityEngine.Object.FindFirstObjectByType<MinimapController>();
            Camera minimapCamera = null;
            RawImage minimapDisplay = null;
            if (minimapCtrl != null)
            {
                minimapCamera  = minimapCtrl.MinimapCamera;
                minimapDisplay = minimapDisplayGo.GetComponent<RawImage>();
            }
            else
            {
                // Fallback: ищем камеру по имени
                var camGo  = GameObject.Find("MinimapCamera");
                minimapCamera  = camGo != null ? camGo.GetComponent<Camera>() : null;
                minimapDisplay = minimapDisplayGo.GetComponent<RawImage>();
                Debug.LogWarning("[Forge C16] MinimapController не найден — _minimapCamera проставлена по имени объекта 'MinimapCamera'.");
            }

            // ---- 5. UnderAttackAlert на GameManagers ----
            var managersGo = GameObject.Find("GameManagers");
            if (managersGo == null)
            {
                Debug.LogWarning("[Forge C16] GameManagers не найден — UnderAttackAlert будет добавлен на GameHUD.");
                managersGo = gameHudGo;
            }

            var alert = EnsureComponent<UnderAttackAlert>(managersGo);

            // ---- 6. Прописываем ссылки через SerializedObject ----
            var alertSo = new SerializedObject(alert);
            alertSo.FindProperty("_minimapMarker").objectReferenceValue  = markerRt;
            alertSo.FindProperty("_edgeVignette").objectReferenceValue   = vignetteImg;
            alertSo.FindProperty("_minimapCamera").objectReferenceValue  = minimapCamera;
            alertSo.FindProperty("_minimapDisplay").objectReferenceValue = minimapDisplay;
            alertSo.ApplyModifiedPropertiesWithoutUndo();

            // ---- 7. Сохранение ----
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Forge C16] Under Attack Alert настроен успешно." +
                      "\n  • UnderAttackVignette → GameHUD" +
                      "\n  • ThreatMarker → MinimapDisplay" +
                      "\n  • UnderAttackAlert → " + managersGo.name +
                      (minimapCamera == null ? "\n  ПРЕДУПРЕЖДЕНИЕ: _minimapCamera = null, назначьте вручную." : ""));
        }

        // ----------------------------------------------------------------
        // C17 — Idle Army Indicator
        // ----------------------------------------------------------------

        /// <summary>
        /// Идемпотентно создаёт бейдж «бездействующая армия» (Circle-17):
        /// <list type="number">
        ///   <item>Создаёт "IdleArmyBadge" (Button, 90×32, жёлтый/золотой фон) в левом нижнем углу RTS_Block,
        ///         чуть выше KeysHint (anchoredPosition.y = 100).</item>
        ///   <item>Добавляет UiPulse на бейдж.</item>
        ///   <item>Добавляет TMP_Text "CountLabel" внутри бейджа.</item>
        ///   <item>Добавляет <see cref="IdleArmyIndicator"/> на "GameManagers".</item>
        ///   <item>Прописывает _selectionSystem, _countLabel, _pulse через SerializedObject.</item>
        ///   <item>Бейдж скрыт по умолчанию (SetActive false) — появится при count > 0.</item>
        /// </list>
        /// Требует: BuildGameHUD (M6a) уже выполнен.
        /// </summary>
        internal static void SetupIdleArmyIndicator()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                EditorUtility.DisplayDialog("Project Forge", "Нет открытой сцены.", "OK");
                return;
            }

            EnsureTmpEssentials();

            // ---- 1. Найти GameHUD ----
            var gameHudGo = GameObject.Find("GameHUD");
            if (gameHudGo == null)
            {
                Debug.LogError("[Forge C17] GameHUD не найден — сначала запустите 'Build Game HUD (M6a)'.");
                return;
            }

            // ---- 2. Найти RTS_Block ----
            var rtsBlockGo = FindDescendantByName(gameHudGo, "RTS_Block");
            if (rtsBlockGo == null)
            {
                Debug.LogError("[Forge C17] RTS_Block не найден в GameHUD.");
                return;
            }

            // ---- 3. IdleArmyBadge в RTS_Block ----
            // Позиция: левый нижний угол, anchorMin/Max=(0,0), anchoredPosition=(12, 100)
            // — чуть выше KeysHint (который сидит на y=12, высота 80 → верх y=92 ≈ 12+80).
            var badgeGo = EnsureChild(rtsBlockGo, "IdleArmyBadge");
            {
                var rt = badgeGo.GetComponent<RectTransform>() ?? badgeGo.AddComponent<RectTransform>();
                rt.anchorMin        = new Vector2(0f, 0f);
                rt.anchorMax        = new Vector2(0f, 0f);
                rt.pivot            = new Vector2(0f, 0f);
                rt.anchoredPosition = new Vector2(12f, 100f);
                rt.sizeDelta        = new Vector2(90f, 32f);

                // Фон кнопки — жёлтый/янтарный (спокойный, не тревожный)
                var bg = EnsureComponent<Image>(badgeGo);
                bg.color = new Color(0.85f, 0.65f, 0.05f, 0.88f);

                EnsureComponent<Button>(badgeGo);

                // UiPulse — вздрагивает при изменении счётчика
                EnsureComponent<UiPulse>(badgeGo);
            }

            // ---- 4. CountLabel внутри бейджа ----
            var labelGo  = EnsureChild(badgeGo, "CountLabel");
            var labelTmp = EnsureComponent<TextMeshProUGUI>(labelGo);
            labelTmp.text      = "0 idle";
            labelTmp.fontSize  = 14f;
            labelTmp.alignment = TextAlignmentOptions.Center;
            labelTmp.color     = Color.white;
            labelTmp.fontStyle = FontStyles.Bold;
            {
                var lrt = labelGo.GetComponent<RectTransform>() ?? labelGo.AddComponent<RectTransform>();
                lrt.anchorMin = Vector2.zero;
                lrt.anchorMax = Vector2.one;
                lrt.offsetMin = new Vector2(4f, 2f);
                lrt.offsetMax = new Vector2(-4f, -2f);
            }

            // Бейдж скрыт по умолчанию — IdleArmyIndicator включит его сам.
            badgeGo.SetActive(false);

            // ---- 5. IdleArmyIndicator на GameManagers ----
            var managersGo2 = GameObject.Find("GameManagers");
            if (managersGo2 == null)
            {
                Debug.LogWarning("[Forge C17] GameManagers не найден — IdleArmyIndicator будет добавлен на GameHUD.");
                managersGo2 = gameHudGo;
            }

            // IdleArmyIndicator нужен Button — держим компонент на badgeGo, а MonoBehaviour
            // также на badgeGo (он [RequireComponent(typeof(Button))]).
            var indicator = EnsureComponent<IdleArmyIndicator>(badgeGo);

            // ---- 6. Прописываем ссылки через SerializedObject ----
            var selSystem = managersGo2.GetComponent<DiplomaGame.Runtime.Selection.SelectionSystem>();

            var indSo = new SerializedObject(indicator);
            indSo.FindProperty("_selectionSystem").objectReferenceValue = selSystem;
            indSo.FindProperty("_countLabel").objectReferenceValue      = labelTmp;
            indSo.FindProperty("_pulse").objectReferenceValue           = badgeGo.GetComponent<UiPulse>();
            indSo.ApplyModifiedPropertiesWithoutUndo();

            // ---- 7. Сохранение ----
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Forge C17] Idle Army Indicator настроен успешно." +
                      "\n  • IdleArmyBadge → RTS_Block (левый нижний угол, y=100)" +
                      "\n  • IdleArmyIndicator → IdleArmyBadge" +
                      "\n  • _selectionSystem → " + (selSystem != null ? selSystem.gameObject.name : "null (назначьте вручную)") +
                      "\n  • Бейдж скрыт по умолчанию (activeSelf=false)");
        }

        // ----------------------------------------------------------------
        // C20: Crosshair Hitmarker
        // ----------------------------------------------------------------

        /// <summary>
        /// Идемпотентно настраивает хитмаркер прицела (Circle-20).
        ///
        /// Что делает:
        ///   (a) Создаёт 4 дочерних Image-полоски (Left/Right/Up/Down) на Crosshair, если их нет.
        ///   (b) Прошивает CrosshairUI._shooter = Hero/HeroShooter, CrosshairUI._settings.
        ///   (c) Записывает дефолтные значения hitmarker на существующий GameFeelSettings.asset
        ///       через SerializedObject (обходит Unity-поведение «C# initializers игнорируются
        ///       для уже сериализованных ассетов»).
        ///   (d) Добавляет UiPulse на каждый AbilitySlot_1..4, если его нет.
        /// </summary>
        internal static void SetupCrosshairHitmarker()
        {
            const string SettingsAssetPath = "Assets/_Project/Data/GameFeel/GameFeelSettings.asset";

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                EditorUtility.DisplayDialog("Project Forge", "Нет открытой сцены.", "OK");
                return;
            }

            // ---- 1. Найти Crosshair ----
            var gameHudGo = GameObject.Find("GameHUD");
            if (gameHudGo == null)
            {
                Debug.LogWarning("[Forge C20] GameHUD не найден — сначала запустите Build Game HUD (M6a).");
                return;
            }

            var crosshairGo = FindDescendantByName(gameHudGo, "Crosshair");
            if (crosshairGo == null)
            {
                Debug.LogWarning("[Forge C20] Crosshair не найден в GameHUD — сначала запустите Build Game HUD (M6a).");
                return;
            }

            // ---- 2. Создать 4 Image-полоски, если их нет ----
            EnsureCrosshairLines(crosshairGo);

            // ---- 3. Прошить CrosshairUI._shooter + _settings ----
            var crosshairUi = EnsureComponent<DiplomaGame.Runtime.UI.CrosshairUI>(crosshairGo);

            var heroGo  = GameObject.Find("Hero");
            var shooter = heroGo != null
                ? heroGo.GetComponent<DiplomaGame.Runtime.Hero.HeroShooter>()
                : null;

            var settings = AssetDatabase.LoadAssetAtPath<DiplomaGame.Runtime.GameFeel.GameFeelSettings>(SettingsAssetPath);

            {
                var so = new SerializedObject(crosshairUi);
                so.FindProperty("_shooter").objectReferenceValue  = shooter;
                so.FindProperty("_settings").objectReferenceValue = settings;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            if (shooter == null)
                Debug.LogWarning("[Forge C20] Hero/HeroShooter не найден — _shooter не прошит. Назначьте вручную.");

            // ---- 4. Записать дефолты hitmarker в GameFeelSettings.asset ----
            // КРИТИЧНО: C# field initializers не применяются к уже сериализованному asset'у,
            // поэтому hitmarkerColorHit и другие новые поля получают type-default (black).
            // SerializedObject записывает значения явно.
            if (settings != null)
            {
                var settingsSo = new SerializedObject(settings);

                // hitmarkerColorHit = warm orange (1, 0.55, 0, 1)
                var colorProp = settingsSo.FindProperty("hitmarkerColorHit");
                if (colorProp != null)
                    colorProp.colorValue = new Color(1f, 0.55f, 0f, 1f);

                var expandProp = settingsSo.FindProperty("hitmarkerExpandScale");
                if (expandProp != null && Mathf.Approximately(expandProp.floatValue, 0f))
                    expandProp.floatValue = 1.15f;

                var missProp = settingsSo.FindProperty("hitmarkerMissScale");
                if (missProp != null && Mathf.Approximately(missProp.floatValue, 0f))
                    missProp.floatValue = 1.05f;

                var durProp = settingsSo.FindProperty("hitmarkerDuration");
                if (durProp != null && Mathf.Approximately(durProp.floatValue, 0f))
                    durProp.floatValue = 0.10f;

                settingsSo.ApplyModifiedPropertiesWithoutUndo();
                // SetDirty marks the object so Unity's asset pipeline knows it needs saving.
                // ForceReserializeAssets guarantees the YAML is rewritten (critical in -batchmode).
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
                AssetDatabase.ForceReserializeAssets(
                    new[] { SettingsAssetPath },
                    ForceReserializeAssetsOptions.ReserializeAssets);
            }
            else
            {
                Debug.LogWarning("[Forge C20] GameFeelSettings.asset не найден — дефолты не записаны. " +
                                 "Сначала запустите Setup GameFeel (C12) → Create GameFeelSettings.asset.");
            }

            // ---- 5. UiPulse на AbilitySlot_1..4 ----
            var tpsBlock = FindDescendantByName(gameHudGo, "TPS_Block");
            if (tpsBlock != null)
            {
                for (int i = 1; i <= 4; i++)
                {
                    var slotGo = FindDescendantByName(tpsBlock, "AbilitySlot_" + i.ToString());
                    if (slotGo != null)
                        EnsureComponent<UiPulse>(slotGo);
                }
            }

            // ---- 6. Сохранение сцены ----
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.Refresh();

            Debug.Log("[Forge C20] Setup Crosshair Hitmarker завершён." +
                      "\n  • Crosshair Image-полоски: 4 штуки (Left/Right/Up/Down)" +
                      "\n  • CrosshairUI._shooter → " + (shooter != null ? shooter.gameObject.name : "null (не найден)") +
                      "\n  • CrosshairUI._settings → " + (settings != null ? SettingsAssetPath : "null (не найден)") +
                      "\n  • GameFeelSettings: hitmarkerColorHit=(1,0.55,0), expand=1.15, miss=1.05, dur=0.10" +
                      "\n  • UiPulse добавлен на AbilitySlot_1..4");
        }

        /// <summary>
        /// Идемпотентно создаёт 4 Image-полоски прицела (Left/Right/Up/Down),
        /// если они ещё не существуют.
        /// </summary>
        private static void EnsureCrosshairLines(GameObject crosshairGo)
        {
            // Те же параметры, что в BuildCrosshairLines — дублируем идемпотентно
            var dirs = new (string name, Vector2 pos, Vector2 size)[]
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
        // C18: Unit Health Bars
        // ----------------------------------------------------------------

        /// <summary>
        /// Идемпотентно настраивает пул HP-баров над юнитами (Circle-18).
        ///
        /// Что создаётся:
        ///   • Canvas "HealthBarsCanvas" (Screen Space Overlay, sortingOrder=20)
        ///     — отдельный Canvas, чтобы не смешиваться с HUD-слоями.
        ///   • UnitHealthBarSystem на GameManagers
        ///     — прошивается _modeController, _selectionSystem, _barCanvas.
        ///
        /// Виджеты (UnitHealthBarWidget) создаются кодом в рантайме через пул;
        /// их префаб не нужен — система поднимает заглушки через CreateWidget().
        /// </summary>
        internal static void SetupUnitHealthBars()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                EditorUtility.DisplayDialog("Project Forge", "Нет открытой сцены.", "OK");
                return;
            }

            // ---- 1. Canvas "HealthBarsCanvas" ----
            var canvasGo = EnsureGameObject("HealthBarsCanvas");
            {
                var canvas = EnsureComponent<Canvas>(canvasGo);
                canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 20;   // поверх HUD (10), под Tooltip (100) и меню (50+)

                var scaler = EnsureComponent<CanvasScaler>(canvasGo);
                scaler.uiScaleMode            = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution    = new Vector2(1920f, 1080f);
                scaler.screenMatchMode        = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight     = 0.5f;

                // GraphicRaycaster не нужен (бары не интерактивны), но добавим для
                // совместимости на случай если понадобятся клики по барам в будущем.
                // Оставим без него — экономим raycast-проходы.
            }

            // ---- 2. UnitHealthBarSystem на GameManagers ----
            var managersGo = GameObject.Find("GameManagers");
            if (managersGo == null)
            {
                Debug.LogError("[Forge C18] GameManagers не найден — UnitHealthBarSystem не добавлен.");
                return;
            }

            var system = EnsureComponent<UnitHealthBarSystem>(managersGo);

            // ---- 3. Прошиваем ссылки через SerializedObject ----
            var modeController  = managersGo.GetComponent<DiplomaGame.Runtime.Core.GameModeController>();
            var selectionSystem = managersGo.GetComponent<DiplomaGame.Runtime.Selection.SelectionSystem>();
            var barCanvas       = canvasGo.GetComponent<Canvas>();

            var so = new SerializedObject(system);
            so.FindProperty("_modeController").objectReferenceValue   = modeController;
            so.FindProperty("_selectionSystem").objectReferenceValue  = selectionSystem;
            so.FindProperty("_barCanvas").objectReferenceValue        = barCanvas;
            // _barWidgetPrefab = null → система строит виджеты ПРОГРАММНО (фон + fill + border).
            // Ручное назначение prefab в Inspector не требуется — бары будут видны сразу.
            // _initialPoolSize, _worldHeadOffset, _cullMargin, _pollInterval — дефолты из кода
            // _playerBorderColor, _enemyBorderColor — дефолты (синий/красный) из кода
            so.ApplyModifiedPropertiesWithoutUndo();

            // ---- 4. Сохранение ----
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Forge C18] Unit Health Bars настроены успешно." +
                      "\n  • HealthBarsCanvas → сцена (sortingOrder=20)" +
                      "\n  • UnitHealthBarSystem → GameManagers" +
                      "\n  • _modeController → " + (modeController  != null ? modeController.gameObject.name  : "null (назначьте вручную)") +
                      "\n  • _selectionSystem → " + (selectionSystem != null ? selectionSystem.gameObject.name : "null (назначьте вручную)") +
                      "\n  • _barCanvas      → HealthBarsCanvas" +
                      "\n  • Пул: 48 виджетов, интервал обновления 0.1 с, cullMargin=0.02");
        }

        // ----------------------------------------------------------------
        // C21: Hero Damage Indicator
        // ----------------------------------------------------------------

        /// <summary>
        /// Идемпотентно создаёт HeroDamageFlash (Image, full-stretch, красный, alpha=0)
        /// в TPS_Block, добавляет HeroDamageIndicator и прошивает ссылки.
        /// Также записывает дефолты Circle-21 в GameFeelSettings.asset.
        ///
        /// LIMITATION: Health.AnyDamaged (Action&lt;Health, float&gt;) не несёт позицию
        /// источника урона — направленный указатель невозможен без изменения API.
        /// Реализован full-edge red flash.
        /// </summary>
        internal static void SetupHeroDamageIndicator()
        {
            const string SettingsAssetPath = "Assets/_Project/Data/GameFeel/GameFeelSettings.asset";

            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                EditorUtility.DisplayDialog("Project Forge", "Нет открытой сцены.", "OK");
                return;
            }

            // ---- 1. Найти TPS_Block ----
            var gameHudGo = GameObject.Find("GameHUD");
            if (gameHudGo == null)
            {
                Debug.LogWarning("[Forge C21/C23] GameHUD не найден — сначала запустите Build Game HUD (M6a).");
                return;
            }

            var tpsBlock = FindDescendantByName(gameHudGo, "TPS_Block");
            if (tpsBlock == null)
            {
                Debug.LogWarning("[Forge C21/C23] TPS_Block не найден в GameHUD — сначала запустите Build Game HUD (M6a).");
                return;
            }

            // ---- 2. Создать HeroDamageFlash (Image, full-stretch, красный, alpha=0) — C21 ----
            var flashGo = EnsureChild(tpsBlock, "HeroDamageFlash");
            {
                var img = EnsureComponent<Image>(flashGo);
                // Красный с alpha=0 — невидим в покое, alpha управляется корутиной
                img.color         = new Color(0.85f, 0.05f, 0.05f, 0f);
                img.raycastTarget = false;  // не блокирует ввод

                SetFullStretch(flashGo);
            }

            // ---- 3. Создать HeroDamageArrow (Image, направленная стрелка на кольце HUD) — C23 ----
            // Позиционируется по центру TPS_Block, якорь center, pivot center.
            // Размер 64×64 px (достаточно заметно, не перекрывает игровую область).
            // Форма стрелки задаётся спрайтом — по умолчанию используется встроенный белый спрайт;
            // дизайнер может заменить на кастомный через Inspector.
            var arrowGo = EnsureChild(tpsBlock, "HeroDamageArrow");
            {
                var img = EnsureComponent<Image>(arrowGo);
                img.color         = new Color(0.9f, 0.1f, 0.1f, 0f);  // красный, alpha=0 в покое
                img.raycastTarget = false;

                // Позиция: центр экрана сдвинут вверх на 220 px — кольцо HUD вокруг перекрестья
                var rt = arrowGo.GetComponent<RectTransform>();
                rt.anchorMin        = new Vector2(0.5f, 0.5f);
                rt.anchorMax        = new Vector2(0.5f, 0.5f);
                rt.pivot            = new Vector2(0.5f, 0.5f);
                rt.sizeDelta        = new Vector2(64f, 64f);
                rt.anchoredPosition = new Vector2(0f, 220f);  // верхняя точка кольца

                arrowGo.SetActive(false);  // скрыт по умолчанию
            }

            // ---- 4. Добавить HeroDamageIndicator ----
            var indicator = EnsureComponent<DiplomaGame.Runtime.UI.HeroDamageIndicator>(flashGo);

            // ---- 5. Найти TPS-камеру (CinemachineCamera или Camera с тегом MainCamera) ----
            // Ищем CinemachineCamera в сцене (используется в TPS-режиме).
            Transform tpsCamTransform = null;
            {
                // Ищем любой CinemachineCamera в сцене (используется в TPS-режиме)
                var allCmCams = UnityEngine.Object.FindObjectsByType<Unity.Cinemachine.CinemachineCamera>(
                    UnityEngine.FindObjectsSortMode.None);
                foreach (var cam in allCmCams)
                {
                    // Первая найденная в сцене считается TPS-камерой
                    tpsCamTransform = cam.transform;
                    break;
                }
            }

            // ---- 6. Прошить ссылки через SerializedObject ----
            var heroGo     = GameObject.Find("Hero");
            var heroHealth = heroGo != null
                ? heroGo.GetComponent<DiplomaGame.Runtime.Combat.Health>()
                : null;

            var settings  = AssetDatabase.LoadAssetAtPath<DiplomaGame.Runtime.GameFeel.GameFeelSettings>(SettingsAssetPath);
            var edgeFlash = flashGo.GetComponent<Image>();
            var arrowImg  = arrowGo.GetComponent<Image>();

            {
                var so = new SerializedObject(indicator);
                so.FindProperty("_heroHealth").objectReferenceValue         = heroHealth;
                so.FindProperty("_tpsCameraTransform").objectReferenceValue = tpsCamTransform;
                so.FindProperty("_edgeFlash").objectReferenceValue          = edgeFlash;
                so.FindProperty("_directionArrow").objectReferenceValue     = arrowImg;
                so.FindProperty("_settings").objectReferenceValue           = settings;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            if (heroHealth == null)
                Debug.LogWarning("[Forge C23] Hero/Health не найден — _heroHealth не прошит. Назначьте вручную.");
            if (tpsCamTransform == null)
                Debug.LogWarning("[Forge C23] CinemachineCamera не найдена — _tpsCameraTransform не прошит. " +
                                 "Компонент сам найдёт Camera.main в рантайме как fallback.");

            // ---- 7. Записать дефолты C21+C23 в GameFeelSettings.asset ----
            // КРИТИЧНО: C# field initializers не применяются к уже сериализованному asset'у;
            // новые поля получают type-default (0). SerializedObject записывает явно.
            if (settings != null)
            {
                var settingsSo = new SerializedObject(settings);

                // C21 defaults
                var durProp = settingsSo.FindProperty("damageIndicatorDuration");
                if (durProp != null && Mathf.Approximately(durProp.floatValue, 0f))
                    durProp.floatValue = 1.0f;

                var peakProp = settingsSo.FindProperty("damageIndicatorPeakAlpha");
                if (peakProp != null && Mathf.Approximately(peakProp.floatValue, 0f))
                    peakProp.floatValue = 0.6f;

                // C23 defaults
                var arrowPeakProp = settingsSo.FindProperty("damageArrowPeakAlpha");
                if (arrowPeakProp != null && Mathf.Approximately(arrowPeakProp.floatValue, 0f))
                    arrowPeakProp.floatValue = 0.8f;

                settingsSo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(settings);
                AssetDatabase.SaveAssets();
                AssetDatabase.ForceReserializeAssets(
                    new[] { SettingsAssetPath },
                    ForceReserializeAssetsOptions.ReserializeAssets);
            }
            else
            {
                Debug.LogWarning("[Forge C21/C23] GameFeelSettings.asset не найден — дефолты не записаны. " +
                                 "Сначала запустите Setup GameFeel (C12) → Create GameFeelSettings.asset.");
            }

            // ---- 8. Сохранение сцены ----
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.Refresh();

            Debug.Log("[Forge C21/C23] Setup Hero Damage Indicator завершён." +
                      "\n  [C21] HeroDamageFlash → TPS_Block (full-stretch, red alpha=0, raycastTarget=false)" +
                      "\n  [C23] HeroDamageArrow → TPS_Block (64×64, центр+220px, red alpha=0, скрыт)" +
                      "\n  • HeroDamageIndicator → HeroDamageFlash" +
                      "\n  • _heroHealth         → " + (heroHealth != null ? heroHealth.gameObject.name : "null (не найден, назначьте вручную)") +
                      "\n  • _tpsCameraTransform → " + (tpsCamTransform != null ? tpsCamTransform.gameObject.name : "null (fallback: Camera.main в рантайме)") +
                      "\n  • _edgeFlash          → HeroDamageFlash.Image" +
                      "\n  • _directionArrow     → HeroDamageArrow.Image" +
                      "\n  • _settings           → " + (settings != null ? SettingsAssetPath : "null (не найден)") +
                      "\n  • GameFeelSettings: damageIndicatorDuration=1.0, damageIndicatorPeakAlpha=0.6, damageArrowPeakAlpha=0.8");
        }
    }
}
