using System.Collections.Generic;
using System.Linq;
using DiplomaGame.Runtime.Audio;
using DiplomaGame.Runtime.Buildings;
using DiplomaGame.Runtime.Combat;
using DiplomaGame.Runtime.Core;
using DiplomaGame.Runtime.Data;
using DiplomaGame.Runtime.Economy;
using DiplomaGame.Runtime.Hero;
using DiplomaGame.Runtime.UI;
using DiplomaGame.Runtime.Units;
using DiplomaGame.Runtime.VFX;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace DiplomaGame.Editor
{
    /// <summary>
    /// Статический класс без состояния. Содержит всю логику валидации проекта.
    /// Вынесен отдельно для прямого тестирования из EditMode-тестов.
    /// </summary>
    public static class ForgeValidator
    {
        private const string TestUnitPrefabPath  = "Assets/_Project/Prefabs/Units/TestUnit.prefab";
        private const string EnemyUnitPrefabPath = "Assets/_Project/Prefabs/Units/EnemyUnit.prefab";
        private const string MarineDataPath      = "Assets/_Project/Data/Units/Marine.asset";
        private const string EnemyGruntDataPath  = "Assets/_Project/Data/Units/EnemyGrunt.asset";
        private const string AbilitiesFolder     = "Assets/_Project/Data/Abilities";

        // M6a
        private const string MinimapRTPath = "Assets/_Project/UI/MinimapRT.renderTexture";

        // M7
        private const string MixerPath = "Assets/_Project/Audio/GameMixer.mixer";

        // M8
        private const string MuzzleFlashPrefabPath = "Assets/_Project/Prefabs/VFX/MuzzleFlash.prefab";
        private const string HitImpactPrefabPath   = "Assets/_Project/Prefabs/VFX/HitImpact.prefab";
        private const string ExplosionPrefabPath   = "Assets/_Project/Prefabs/VFX/Explosion.prefab";
        private const string BuildEffectPrefabPath = "Assets/_Project/Prefabs/VFX/BuildEffect.prefab";

        // M5
        private const string HQDataPath         = "Assets/_Project/Data/Buildings/HQ.asset";
        private const string BarracksDataPath    = "Assets/_Project/Data/Buildings/Barracks.asset";
        private const string ExtractorDataPath   = "Assets/_Project/Data/Buildings/Extractor.asset";
        private const string HQPrefabPath        = "Assets/_Project/Prefabs/Buildings/HQ.prefab";
        private const string BarracksPrefabPath  = "Assets/_Project/Prefabs/Buildings/Barracks.prefab";
        private const string ExtractorPrefabPath = "Assets/_Project/Prefabs/Buildings/Extractor.prefab";
        private const string ResourceNodePrefabPath = "Assets/_Project/Prefabs/Props/ResourceNode.prefab";

        private static readonly string[] RequiredFolders =
        {
            "Assets/_Project/Scripts",
            "Assets/_Project/Scenes",
            "Assets/_Project/Prefabs",
            "Assets/_Project/Data",
            "Assets/_Project/Art",
            "Assets/_Project/Audio",
            "Assets/_Project/UI",
            "Assets/_Project/VFX",
        };

        private const string SandboxScenePath  = "Assets/_Project/Scenes/Sandbox.unity";
        private const string MainMenuScenePath = "Assets/_Project/Scenes/MainMenu.unity";

        /// <summary>
        /// Запускает все проверки. Возвращает список проблем;
        /// пустой список означает, что всё в порядке.
        /// </summary>
        public static List<string> Validate()
        {
            var issues = new List<string>();

            CheckRequiredFolders(issues);
            CheckSandboxInBuildSettings(issues);
            CheckMissingScriptsInOpenScene(issues);
            CheckGameModeControllerRefs(issues);
            CheckTestUnitPrefabExists(issues);
            CheckNavMeshSurfaceOnGround(issues);
            CheckHeroSetup(issues);
            CheckAbilityAssets(issues);
            CheckUnitDataAssets(issues);
            CheckUnitPrefabsHaveCombatComponents(issues);
            CheckHeroHasHealth(issues);
            CheckBuildingDataAssets(issues);
            CheckBuildingPrefabs(issues);
            CheckEconomyInScene(issues);
            CheckHudInScene(issues);
            CheckMainMenuScene(issues);
            CheckGameMenusInScene(issues);
            CheckAudioInScene(issues);
            CheckVfxPrefabsExist(issues);
            CheckVolumeInScene(issues);
            CheckUnitPrefabsHaveVisualChild(issues);

            return issues;
        }

        /// <summary>
        /// Идемпотентно создаёт папки структуры проекта через AssetDatabase.
        /// </summary>
        public static void BootstrapProjectStructure()
        {
            foreach (var folder in RequiredFolders)
                EnsureFolder(folder);

            AssetDatabase.Refresh();
        }

        // ----------------------------------------------------------------
        // Приватные проверки
        // ----------------------------------------------------------------

        private static void CheckRequiredFolders(List<string> issues)
        {
            foreach (var folder in RequiredFolders)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                    issues.Add($"Отсутствует папка: {folder}");
            }
        }

        private static void CheckSandboxInBuildSettings(List<string> issues)
        {
            bool found = EditorBuildSettings.scenes
                .Any(s => s.path == SandboxScenePath);

            if (!found)
                issues.Add($"Sandbox не добавлена в Build Settings: {SandboxScenePath}");
        }

        private static void CheckMissingScriptsInOpenScene(List<string> issues)
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid()) return;

            int count = 0;
            foreach (var root in scene.GetRootGameObjects())
                count += CountMissingScripts(root);

            if (count > 0)
                issues.Add($"Missing scripts в открытой сцене ({scene.name}): {count} шт.");
        }

        private static void CheckGameModeControllerRefs(List<string> issues)
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid()) return;

            // Ищем GameManagers с GameModeController в открытой сцене
            foreach (var root in scene.GetRootGameObjects())
            {
                var controller = root.GetComponentInChildren<GameModeController>(includeInactive: true);
                if (controller == null) continue;

                var so = new SerializedObject(controller);

                if (so.FindProperty("rtsCamera").objectReferenceValue == null)
                    issues.Add("GameModeController: поле rtsCamera не заполнено (запустите Setup Mode Rig).");

                if (so.FindProperty("tpsCamera").objectReferenceValue == null)
                    issues.Add("GameModeController: поле tpsCamera не заполнено (запустите Setup Mode Rig).");

                if (so.FindProperty("actions").objectReferenceValue == null)
                    issues.Add("GameModeController: поле actions (InputActionAsset) не заполнено (запустите Setup Mode Rig).");

                // Проверяем только первый найденный контроллер в сцене
                break;
            }
        }

        private static void CheckTestUnitPrefabExists(List<string> issues)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(TestUnitPrefabPath);
            if (prefab == null)
                issues.Add($"Префаб TestUnit не найден: {TestUnitPrefabPath} (запустите Create/Update TestUnit Prefab).");
        }

        private static void CheckNavMeshSurfaceOnGround(List<string> issues)
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid()) return;

            var ground = GameObject.Find("Ground");
            if (ground == null) return; // Ground не в сцене — не проверяем

            if (ground.GetComponent<NavMeshSurface>() == null)
                issues.Add("На объекте Ground нет NavMeshSurface (запустите Bake NavMesh).");
        }

        private static void CheckHeroSetup(List<string> issues)
        {
            var heroGo = GameObject.Find("Hero");
            if (heroGo == null) return;  // Hero не в сцене — не проверяем

            if (heroGo.GetComponent<HeroController>() == null)
                issues.Add("Hero: отсутствует HeroController (запустите Setup Hero (M3)).");

            if (heroGo.GetComponent<HeroShooter>() == null)
                issues.Add("Hero: отсутствует HeroShooter (запустите Setup Hero (M3)).");

            if (heroGo.GetComponent<AbilitySystem>() == null)
                issues.Add("Hero: отсутствует AbilitySystem (запустите Setup Hero (M3)).");

            if (heroGo.GetComponent<CharacterController>() == null)
                issues.Add("Hero: отсутствует CharacterController (запустите Setup Hero (M3)).");

            if (heroGo.GetComponent<DiplomaGame.Runtime.Units.Unit>() == null)
                issues.Add("Hero: отсутствует Unit (запустите Setup Hero (M3)).");
        }

        private static void CheckAbilityAssets(List<string> issues)
        {
            string[] assetNames = { "Dash", "Ability2", "Ability3", "Ability4" };
            foreach (var name in assetNames)
            {
                var path  = $"{AbilitiesFolder}/{name}.asset";
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset == null)
                    issues.Add($"Ассет способности не найден: {path} (запустите Setup Hero (M3)).");
            }
        }

        // ----------------------------------------------------------------
        // M4 проверки
        // ----------------------------------------------------------------

        private static void CheckUnitDataAssets(List<string> issues)
        {
            if (AssetDatabase.LoadAssetAtPath<ScriptableObject>(MarineDataPath) == null)
                issues.Add($"UnitData ассет не найден: {MarineDataPath} (запустите Create/Update Unit Data Assets (M4)).");

            if (AssetDatabase.LoadAssetAtPath<ScriptableObject>(EnemyGruntDataPath) == null)
                issues.Add($"UnitData ассет не найден: {EnemyGruntDataPath} (запустите Create/Update Unit Data Assets (M4)).");
        }

        private static void CheckUnitPrefabsHaveCombatComponents(List<string> issues)
        {
            CheckPrefabHasCombatComponents(TestUnitPrefabPath, "TestUnit", issues);
            CheckPrefabHasCombatComponents(EnemyUnitPrefabPath, "EnemyUnit", issues);
        }

        private static void CheckPrefabHasCombatComponents(string prefabPath, string prefabName, List<string> issues)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                issues.Add($"Префаб {prefabName} не найден: {prefabPath}.");
                return;
            }

            if (prefab.GetComponent<Health>() == null)
                issues.Add($"Префаб {prefabName}: отсутствует Health (запустите Create/Update {prefabName} Prefab).");

            if (prefab.GetComponent<UnitCombat>() == null)
                issues.Add($"Префаб {prefabName}: отсутствует UnitCombat (запустите Create/Update {prefabName} Prefab).");
        }

        private static void CheckHeroHasHealth(List<string> issues)
        {
            var heroGo = GameObject.Find("Hero");
            if (heroGo == null) return;  // Hero не в сцене — не проверяем

            if (heroGo.GetComponent<Health>() == null)
                issues.Add("Hero: отсутствует Health (запустите Setup Combat (M4)).");
        }

        // ----------------------------------------------------------------
        // M5 проверки
        // ----------------------------------------------------------------

        private static void CheckBuildingDataAssets(List<string> issues)
        {
            if (AssetDatabase.LoadAssetAtPath<BuildingData>(HQDataPath) == null)
                issues.Add($"BuildingData ассет не найден: {HQDataPath} (запустите Create/Update Building Data (M5)).");

            if (AssetDatabase.LoadAssetAtPath<BuildingData>(BarracksDataPath) == null)
                issues.Add($"BuildingData ассет не найден: {BarracksDataPath} (запустите Create/Update Building Data (M5)).");

            if (AssetDatabase.LoadAssetAtPath<BuildingData>(ExtractorDataPath) == null)
                issues.Add($"BuildingData ассет не найден: {ExtractorDataPath} (запустите Create/Update Building Data (M5)).");
        }

        private static void CheckBuildingPrefabs(List<string> issues)
        {
            CheckBuildingPrefabExists(HQPrefabPath,          "HQ",          issues);
            CheckBuildingPrefabExists(BarracksPrefabPath,    "Barracks",    issues);
            CheckBuildingPrefabExists(ExtractorPrefabPath,   "Extractor",   issues);
            CheckBuildingPrefabExists(ResourceNodePrefabPath, "ResourceNode", issues);
        }

        private static void CheckBuildingPrefabExists(string path, string name, List<string> issues)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null)
            {
                issues.Add($"Префаб {name} не найден: {path} (запустите Create/Update Building Prefabs (M5)).");
                return;
            }

            if (name != "ResourceNode" && prefab.GetComponent<Building>() == null)
                issues.Add($"Префаб {name}: отсутствует компонент Building.");

            if (name != "ResourceNode" && prefab.GetComponent<Health>() == null)
                issues.Add($"Префаб {name}: отсутствует компонент Health.");

            if (name == "ResourceNode" && prefab.GetComponent<ResourceNode>() == null)
                issues.Add($"Префаб ResourceNode: отсутствует компонент ResourceNode.");
        }

        private static void CheckEconomyInScene(List<string> issues)
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid()) return;

            // Проверяем наличие ResourceBank в сцене
            bool hasBank = false;
            bool hasPlayerHQ = false;
            bool hasEnemyHQ  = false;

            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.GetComponentInChildren<ResourceBank>(includeInactive: true) != null)
                    hasBank = true;

                var buildings = root.GetComponentsInChildren<Building>(includeInactive: true);
                foreach (var b in buildings)
                {
                    if (b.Data != null && b.Data.BuildingType == BuildingType.Headquarters)
                    {
                        if (b.Faction == Faction.Player) hasPlayerHQ = true;
                        if (b.Faction == Faction.Enemy)  hasEnemyHQ  = true;
                    }
                }
            }

            if (!hasBank)
                issues.Add("В сцене нет ResourceBank (запустите Setup Economy (M5)).");

            if (!hasPlayerHQ)
                issues.Add("В сцене нет HQ фракции Player (запустите Setup Economy (M5)).");

            if (!hasEnemyHQ)
                issues.Add("В сцене нет HQ фракции Enemy (запустите Setup Economy (M5)).");
        }

        // ----------------------------------------------------------------
        // M6a проверки
        // ----------------------------------------------------------------

        private static void CheckHudInScene(List<string> issues)
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid()) return;

            // Canvas "GameHUD"
            bool hasHud = false;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == "GameHUD")
                {
                    hasHud = true;
                    if (root.GetComponent<HudController>() == null)
                        issues.Add("Canvas GameHUD: отсутствует HudController (запустите Build Game HUD (M6a)).");
                    break;
                }
            }

            if (!hasHud)
                issues.Add("В сцене нет Canvas «GameHUD» (запустите Build Game HUD (M6a)).");

            // EventSystem с InputSystemUIInputModule
            var es = Object.FindFirstObjectByType<EventSystem>();
            if (es == null)
            {
                issues.Add("В сцене нет EventSystem (запустите Build Game HUD (M6a)).");
            }
            else if (es.GetComponent<InputSystemUIInputModule>() == null)
            {
                issues.Add("EventSystem: отсутствует InputSystemUIInputModule (запустите Build Game HUD (M6a)).");
            }

            // RenderTexture миникарты
            var rt = AssetDatabase.LoadAssetAtPath<RenderTexture>(MinimapRTPath);
            if (rt == null)
                issues.Add($"RenderTexture миникарты не найден: {MinimapRTPath} (запустите Build Game HUD (M6a)).");
        }

        // ----------------------------------------------------------------
        // M6b проверки
        // ----------------------------------------------------------------

        private static void CheckMainMenuScene(List<string> issues)
        {
            // Сцена MainMenu должна существовать как файл
            if (!System.IO.File.Exists(MainMenuScenePath))
            {
                issues.Add($"Сцена MainMenu не найдена: {MainMenuScenePath} (запустите Create/Update MainMenu Scene).");
                return;
            }

            // MainMenu должна быть в Build Settings под индексом 0
            var scenes = EditorBuildSettings.scenes;
            if (scenes.Length == 0 || scenes[0].path != MainMenuScenePath)
                issues.Add($"MainMenu не находится под индексом 0 в Build Settings (запустите Create/Update MainMenu Scene).");

            // Sandbox должна быть под индексом 1
            if (scenes.Length < 2 || scenes[1].path != SandboxScenePath)
                issues.Add($"Sandbox не находится под индексом 1 в Build Settings (запустите Create/Update MainMenu Scene).");
        }

        private static void CheckGameMenusInScene(List<string> issues)
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid()) return;

            // Проверяем только в игровой сцене (Sandbox), в MainMenu этих канвасов быть не должно
            if (scene.name != "Sandbox") return;

            bool hasPause    = false;
            bool hasGameOver = false;

            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == "PauseMenu")
                {
                    hasPause = true;
                    if (root.GetComponent<DiplomaGame.Runtime.UI.PauseController>() == null)
                        issues.Add("Canvas PauseMenu: отсутствует PauseController (запустите Build Menus (M6b)).");
                }

                if (root.name == "GameOver")
                {
                    hasGameOver = true;
                    if (root.GetComponent<DiplomaGame.Runtime.UI.GameOverController>() == null)
                        issues.Add("Canvas GameOver: отсутствует GameOverController (запустите Build Menus (M6b)).");
                }
            }

            if (!hasPause)
                issues.Add("В сцене нет Canvas «PauseMenu» (запустите Build Menus (M6b)).");

            if (!hasGameOver)
                issues.Add("В сцене нет Canvas «GameOver» (запустите Build Menus (M6b)).");
        }

        // ----------------------------------------------------------------
        // M7 проверки
        // ----------------------------------------------------------------

        private static void CheckAudioInScene(List<string> issues)
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid()) return;

            // AudioManager в сцене
            var am = Object.FindFirstObjectByType<AudioManager>();
            if (am == null)
            {
                issues.Add("В сцене нет AudioManager (запустите Setup Audio (M7)).");
                return;
            }

            var so = new SerializedObject(am);

            // Музыкальные клипы обязательны
            if (so.FindProperty("_menuMusic").objectReferenceValue == null)
                issues.Add("AudioManager: не задан _menuMusic (запустите Setup Audio (M7)).");

            if (so.FindProperty("_ambientMusic").objectReferenceValue == null)
                issues.Add("AudioManager: не задан _ambientMusic (запустите Setup Audio (M7)).");

            if (so.FindProperty("_combatMusic").objectReferenceValue == null)
                issues.Add("AudioManager: не задан _combatMusic (запустите Setup Audio (M7)).");

            // Хотя бы один SFX-клип
            var heroShot = so.FindProperty("_heroShot");
            if (heroShot == null || heroShot.arraySize == 0)
                issues.Add("AudioManager: массив _heroShot пуст (запустите Setup Audio (M7)).");

            // Голосовые подтверждения
            var unitAck = so.FindProperty("_unitAck");
            if (unitAck == null || unitAck.arraySize == 0)
                issues.Add("AudioManager: массив _unitAck пуст (запустите Setup Audio (M7)).");

            // GameMixer — предупреждение (не ошибка: fallback-режим поддерживается)
            var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            if (mixer == null)
            {
                issues.Add($"[Warning] GameMixer.mixer не найден: {MixerPath}. " +
                           "AudioManager будет работать в fallback-режиме без mixer " +
                           "(громкости через AudioSource.volume).");
            }
        }

        // ----------------------------------------------------------------
        // M8 проверки
        // ----------------------------------------------------------------

        private static void CheckVfxPrefabsExist(List<string> issues)
        {
            var vfxPrefabs = new[]
            {
                (MuzzleFlashPrefabPath, "MuzzleFlash"),
                (HitImpactPrefabPath,   "HitImpact"),
                (ExplosionPrefabPath,   "Explosion"),
                (BuildEffectPrefabPath, "BuildEffect"),
            };

            foreach (var (path, name) in vfxPrefabs)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                    issues.Add($"VFX-префаб {name} не найден: {path} (запустите Build VFX Prefabs (M8)).");
            }
        }

        private static void CheckVolumeInScene(List<string> issues)
        {
            var scene = SceneManager.GetActiveScene();
            if (!scene.IsValid()) return;

            // Ищем Volume с именем "PostFX"
            bool found = false;
            foreach (var root in scene.GetRootGameObjects())
            {
                if (root.name == "PostFX" && root.GetComponent<Volume>() != null)
                {
                    found = true;
                    var vol = root.GetComponent<Volume>();
                    if (!vol.isGlobal)
                        issues.Add("Volume 'PostFX': isGlobal = false (должен быть глобальным).");
                    if (vol.sharedProfile == null)
                        issues.Add("Volume 'PostFX': sharedProfile не задан (запустите Setup Lighting & Post (M8)).");
                    break;
                }
            }

            if (!found)
                issues.Add("В сцене нет Global Volume 'PostFX' (запустите Setup Lighting & Post (M8)).");
        }

        private static void CheckUnitPrefabsHaveVisualChild(List<string> issues)
        {
            CheckPrefabHasVisualChild(TestUnitPrefabPath,  "TestUnit",  issues);
            CheckPrefabHasVisualChild(EnemyUnitPrefabPath, "EnemyUnit", issues);
            CheckPrefabHasVisualChild(HQPrefabPath,        "HQ",        issues);
        }

        private static void CheckPrefabHasVisualChild(string prefabPath, string prefabName, List<string> issues)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) return; // Уже проверено в других методах

            var visualTf = prefab.transform.Find("Visual");
            if (visualTf == null)
                issues.Add($"Префаб {prefabName}: нет дочернего объекта 'Visual' (запустите Apply Visuals (M8)).");
        }

        private static int CountMissingScripts(GameObject go)
        {
            int count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);

            foreach (Transform child in go.transform)
                count += CountMissingScripts(child.gameObject);

            return count;
        }

        // ----------------------------------------------------------------
        // Вспомогательный метод создания папок
        // ----------------------------------------------------------------

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath)) return;

            // Рекурсивно создаём всю цепочку родительских папок
            var parts = folderPath.Split('/');
            var current = parts[0]; // "Assets"
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
