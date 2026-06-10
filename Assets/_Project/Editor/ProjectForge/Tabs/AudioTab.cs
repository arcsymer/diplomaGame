using System;
using System.Reflection;
using DiplomaGame.Runtime.Audio;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace DiplomaGame.Editor
{
    /// <summary>
    /// Вкладка Audio — настройка M7.
    /// Кнопка "Setup Audio (M7)": создаёт GameMixer, добавляет AudioManager в сцену,
    /// проставляет клипы, навешивает UiButtonSound на кнопки.
    /// </summary>
    internal sealed class AudioTab : IForgeTab
    {
        // Пути к аудио-ассетам
        private const string MixerPath   = "Assets/_Project/Audio/GameMixer.mixer";

        private const string MenuMusicPath    = "Assets/_Project/Audio/Music/Floating_Cities.mp3";
        private const string AmbientMusicPath = "Assets/_Project/Audio/Music/Deep_Haze.mp3";
        private const string CombatMusicPath  = "Assets/_Project/Audio/Music/Volatile_Reaction.mp3";

        private static readonly string[] HeroShotPaths = {
            "Assets/_Project/Audio/SFX/laserRetro_000.ogg",
            "Assets/_Project/Audio/SFX/laserRetro_001.ogg",
            "Assets/_Project/Audio/SFX/laserRetro_002.ogg",
        };
        private static readonly string[] UnitShotPaths = {
            "Assets/_Project/Audio/SFX/laserLarge_000.ogg",
            "Assets/_Project/Audio/SFX/laserLarge_001.ogg",
        };
        private static readonly string[] ExplosionPaths = {
            "Assets/_Project/Audio/SFX/explosionCrunch_000.ogg",
            "Assets/_Project/Audio/SFX/explosionCrunch_001.ogg",
            "Assets/_Project/Audio/SFX/explosionCrunch_002.ogg",
        };
        private static readonly string[] BuildPlacePaths = {
            "Assets/_Project/Audio/SFX/forceField_000.ogg",
            "Assets/_Project/Audio/SFX/forceField_001.ogg",
        };
        private static readonly string[] HitHeavyPaths = {
            "Assets/_Project/Audio/SFX/impactMetal_heavy_000.ogg",
            "Assets/_Project/Audio/SFX/impactMetal_heavy_001.ogg",
        };
        private static readonly string[] HitLightPaths = {
            "Assets/_Project/Audio/SFX/impactMetal_light_000.ogg",
            "Assets/_Project/Audio/SFX/impactMetal_light_001.ogg",
        };
        private static readonly string[] UiClickPaths = {
            "Assets/_Project/Audio/UI/click_001.ogg",
            "Assets/_Project/Audio/UI/click_002.ogg",
        };
        private static readonly string[] UiBackPaths = {
            "Assets/_Project/Audio/UI/back_001.ogg",
        };
        private static readonly string[] UiConfirmPaths = {
            "Assets/_Project/Audio/UI/confirmation_001.ogg",
            "Assets/_Project/Audio/UI/confirmation_002.ogg",
        };
        private static readonly string[] UiErrorPaths = {
            "Assets/_Project/Audio/UI/bong_001.ogg",
        };
        private static readonly string[] UnitAckPaths = {
            "Assets/_Project/Audio/Voice/ready.ogg",
            "Assets/_Project/Audio/Voice/begin.ogg",
            "Assets/_Project/Audio/Voice/fight.ogg",
            "Assets/_Project/Audio/Voice/prepare_yourself.ogg",
        };
        private const string VictoryClipPath = "Assets/_Project/Audio/Voice/flawless_victory.ogg";
        private const string DefeatClipPath  = "Assets/_Project/Audio/Voice/game_over.ogg";

        public string Title => "Audio";

        public void OnGUI()
        {
            GUILayout.Space(8);
            GUILayout.Label("Аудио (M7)", EditorStyles.boldLabel);
            GUILayout.Space(4);

            EditorGUILayout.HelpBox(
                "Создаёт GameMixer.mixer (идемпотентно), добавляет AudioManager в открытую сцену, " +
                "проставляет все клипы, навешивает UiButtonSound на Button-компоненты.\n" +
                "Настраивает import-параметры аудиофайлов (музыка → Streaming, SFX → DecompressOnLoad).",
                MessageType.Info);

            GUILayout.Space(4);

            if (GUILayout.Button("Setup Audio (M7)", GUILayout.Height(32)))
                SetupAudio();
        }

        // ----------------------------------------------------------------
        // Основная операция — доступна из ForgeBatch
        // ----------------------------------------------------------------

        internal static void SetupAudio()
        {
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (!scene.IsValid())
            {
                EditorUtility.DisplayDialog("Project Forge", "Нет открытой сцены.", "OK");
                return;
            }

            // 1. Import-настройки клипов
            ApplyImportSettings();

            // 2. Создать/получить GameMixer
            var mixer = EnsureMixer();

            // 3. AudioManager в сцене
            SetupAudioManagerInScene(mixer);

            // 4. UiButtonSound на все Button
            AttachUiButtonSounds();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Project Forge] Setup Audio (M7) выполнен.");
        }

        // ----------------------------------------------------------------
        // Mixer
        // ----------------------------------------------------------------

        private static AudioMixer EnsureMixer()
        {
            // Идемпотентно — если уже есть, просто загружаем
            var existing = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
            if (existing != null)
            {
                Debug.Log("[Project Forge] GameMixer.mixer уже существует, пропускаем создание.");
                return existing;
            }

            // Пытаемся создать через reflection
            AudioMixer mixer = null;
            try
            {
                mixer = CreateMixerViaReflection();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Project Forge] Reflection-создание AudioMixer не удалось: {ex.Message}\n" +
                                 "Используется fallback-режим без .mixer (громкости через AudioSource.volume).");
            }

            if (mixer != null)
                Debug.Log("[Project Forge] GameMixer.mixer создан успешно.");
            else
                Debug.Log("[Project Forge] AudioManager будет работать в fallback-режиме без mixer.");

            return mixer;
        }

        private static AudioMixer CreateMixerViaReflection()
        {
            // Находим internal-тип AudioMixerController
            var controllerType = Type.GetType(
                "UnityEditor.Audio.AudioMixerController, UnityEditor");

            if (controllerType == null)
                throw new InvalidOperationException("AudioMixerController тип не найден.");

            // Создаём инстанс
            var controller = ScriptableObject.CreateInstance(controllerType);
            if (controller == null)
                throw new InvalidOperationException("Не удалось создать инстанс AudioMixerController.");

            // Папка Audio должна существовать
            EnsureFolder("Assets/_Project/Audio");

            // Сохраняем как ассет
            AssetDatabase.CreateAsset(controller, MixerPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Переименуем Master-snapshot если он есть
            try
            {
                var snapshotsProp = controllerType.GetProperty("snapshots",
                    BindingFlags.Public | BindingFlags.Instance);
                if (snapshotsProp != null)
                {
                    var snapshots = snapshotsProp.GetValue(controller) as System.Collections.IList;
                    if (snapshots != null && snapshots.Count > 0)
                    {
                        var snap = snapshots[0] as UnityEngine.Object;
                        if (snap != null) snap.name = "Snapshot";
                    }
                }
            }
            catch { /* игнорируем — snapshot необязателен */ }

            // Создаём дочерние группы: Music, SFX, UI, Voice
            string[] groupNames = { "Music", "SFX", "UI", "Voice" };
            var masterGroupProp = controllerType.GetProperty("masterGroup",
                BindingFlags.Public | BindingFlags.Instance);
            var createGroupMethod = controllerType.GetMethod("CreateNewGroup",
                BindingFlags.Public | BindingFlags.Instance);
            var addChildMethod = controllerType.GetMethod("AddChildToParent",
                BindingFlags.Public | BindingFlags.Instance);

            if (masterGroupProp == null || createGroupMethod == null || addChildMethod == null)
                throw new InvalidOperationException("Не найдены нужные методы AudioMixerController.");

            var masterGroup = masterGroupProp.GetValue(controller);

            foreach (var groupName in groupNames)
            {
                try
                {
                    var newGroup = createGroupMethod.Invoke(controller,
                        new object[] { groupName, false });

                    if (newGroup != null && masterGroup != null)
                        addChildMethod.Invoke(controller,
                            new object[] { newGroup, masterGroup });
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Project Forge] Не удалось создать группу {groupName}: {ex.Message}");
                }
            }

            // Пробуем экспонировать параметры громкости групп
            TryExposeVolumeParameters(controller, controllerType);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
        }

        private static void TryExposeVolumeParameters(
            ScriptableObject controller, Type controllerType)
        {
            // Рефлексия для экспонирования параметров — API нестабильное,
            // поэтому весь блок в try/catch. При ошибке — fallback без экспонирования.
            try
            {
                var exposeMethod = controllerType.GetMethod("ExposeParameter",
                    BindingFlags.Public | BindingFlags.Instance);
                if (exposeMethod == null) return;

                var masterGroupProp = controllerType.GetProperty("masterGroup",
                    BindingFlags.Public | BindingFlags.Instance);
                if (masterGroupProp == null) return;

                var masterGroup = masterGroupProp.GetValue(controller);
                if (masterGroup == null) return;

                // У masterGroup пробуем экспонировать MasterVol
                ExposeGroupVolume(controller, controllerType, exposeMethod, masterGroup, "MasterVol");

                // Дочерние группы — по имени
                var groupNames = new[]
                {
                    ("Music", "MusicVol"),
                    ("SFX",   "SfxVol"),
                    ("UI",    "UiVol"),
                    ("Voice", "VoiceVol"),
                };

                // Получаем список всех групп
                var allGroupsMethod = controllerType.GetMethod("GetAllAudioGroupsSlow",
                    BindingFlags.Public | BindingFlags.Instance);
                if (allGroupsMethod == null) return;

                var allGroups = allGroupsMethod.Invoke(controller, null) as System.Collections.IList;
                if (allGroups == null) return;

                foreach (var (name, paramName) in groupNames)
                {
                    foreach (var grp in allGroups)
                    {
                        var grpObj = grp as UnityEngine.Object;
                        if (grpObj == null || grpObj.name != name) continue;
                        ExposeGroupVolume(controller, controllerType, exposeMethod, grp, paramName);
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Project Forge] Экспонирование параметров AudioMixer не удалось: {ex.Message}\n" +
                                 "SettingsService будет использовать fallback через AudioManager.SetCategoryVolume.");
            }
        }

        private static void ExposeGroupVolume(
            ScriptableObject controller, Type controllerType,
            MethodInfo exposeMethod, object group, string paramName)
        {
            try
            {
                // Получаем GUID громкости группы
                var groupType = group.GetType();
                var guidMethod = groupType.GetMethod("GetGUIDForVolume",
                    BindingFlags.Public | BindingFlags.Instance);
                if (guidMethod == null) return;

                var guid = guidMethod.Invoke(group, null);

                // Экспонируем
                exposeMethod.Invoke(controller, new[] { guid });

                // Переименовываем параметр
                var renameMethod = controllerType.GetMethod("RenameExposedParameter",
                    BindingFlags.Public | BindingFlags.Instance);
                renameMethod?.Invoke(controller, new[] { guid, (object)paramName });
            }
            catch { /* не критично */ }
        }

        // ----------------------------------------------------------------
        // AudioManager в сцене
        // ----------------------------------------------------------------

        private static void SetupAudioManagerInScene(AudioMixer mixer)
        {
            // В MainMenu — объект "AudioSystem", в остальных сценах — "GameManagers"
            string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            string goName    = sceneName == "MainMenu" ? "AudioSystem" : "GameManagers";

            var go  = EnsureGameObject(goName);
            var mgr = EnsureComponent<AudioManager>(go);

            var so = new SerializedObject(mgr);

            // Mixer (может быть null — это нормально, fallback-режим)
            so.FindProperty("_mixer").objectReferenceValue = mixer;

            // Mixer Groups
            if (mixer != null)
            {
                so.FindProperty("_musicGroup").objectReferenceValue  = FindMixerGroup(mixer, "Music");
                so.FindProperty("_sfxGroup").objectReferenceValue    = FindMixerGroup(mixer, "SFX");
                so.FindProperty("_uiGroup").objectReferenceValue     = FindMixerGroup(mixer, "UI");
                so.FindProperty("_voiceGroup").objectReferenceValue  = FindMixerGroup(mixer, "Voice");
            }

            // Музыка
            so.FindProperty("_menuMusic").objectReferenceValue    = Load<AudioClip>(MenuMusicPath);
            so.FindProperty("_ambientMusic").objectReferenceValue = Load<AudioClip>(AmbientMusicPath);
            so.FindProperty("_combatMusic").objectReferenceValue  = Load<AudioClip>(CombatMusicPath);

            // SFX-массивы
            SetClipArray(so, "_heroShot",    HeroShotPaths);
            SetClipArray(so, "_unitShot",    UnitShotPaths);
            SetClipArray(so, "_explosion",   ExplosionPaths);
            SetClipArray(so, "_buildPlace",  BuildPlacePaths);
            SetClipArray(so, "_hitHeavy",    HitHeavyPaths);
            SetClipArray(so, "_hitLight",    HitLightPaths);

            // UI
            SetClipArray(so, "_uiClick",    UiClickPaths);
            SetClipArray(so, "_uiBack",     UiBackPaths);
            SetClipArray(so, "_uiConfirm",  UiConfirmPaths);
            SetClipArray(so, "_uiError",    UiErrorPaths);

            // Voice
            SetClipArray(so, "_unitAck",    UnitAckPaths);
            so.FindProperty("_victoryClip").objectReferenceValue = Load<AudioClip>(VictoryClipPath);
            so.FindProperty("_defeatClip").objectReferenceValue  = Load<AudioClip>(DefeatClipPath);

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static AudioMixerGroup FindMixerGroup(AudioMixer mixer, string name)
        {
            if (mixer == null) return null;
            var groups = mixer.FindMatchingGroups(name);
            return groups != null && groups.Length > 0 ? groups[0] : null;
        }

        private static void SetClipArray(SerializedObject so, string propName, string[] paths)
        {
            var prop = so.FindProperty(propName);
            if (prop == null) return;

            prop.arraySize = paths.Length;
            for (int i = 0; i < paths.Length; i++)
            {
                var clip = Load<AudioClip>(paths[i]);
                prop.GetArrayElementAtIndex(i).objectReferenceValue = clip;
            }
        }

        // ----------------------------------------------------------------
        // UiButtonSound на кнопки
        // ----------------------------------------------------------------

        private static void AttachUiButtonSounds()
        {
            var buttons = UnityEngine.Object.FindObjectsByType<Button>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (var btn in buttons)
            {
                if (btn.GetComponent<UiButtonSound>() == null)
                    btn.gameObject.AddComponent<UiButtonSound>();
            }
        }

        // ----------------------------------------------------------------
        // Import-настройки
        // ----------------------------------------------------------------

        private static void ApplyImportSettings()
        {
            // Музыка — Streaming
            ApplyMusicImport(MenuMusicPath);
            ApplyMusicImport(AmbientMusicPath);
            ApplyMusicImport(CombatMusicPath);

            // SFX и Voice — DecompressOnLoad
            string[] sfxAndVoice =
            {
                "Assets/_Project/Audio/SFX/laserRetro_000.ogg",
                "Assets/_Project/Audio/SFX/laserRetro_001.ogg",
                "Assets/_Project/Audio/SFX/laserRetro_002.ogg",
                "Assets/_Project/Audio/SFX/laserLarge_000.ogg",
                "Assets/_Project/Audio/SFX/laserLarge_001.ogg",
                "Assets/_Project/Audio/SFX/explosionCrunch_000.ogg",
                "Assets/_Project/Audio/SFX/explosionCrunch_001.ogg",
                "Assets/_Project/Audio/SFX/explosionCrunch_002.ogg",
                "Assets/_Project/Audio/SFX/forceField_000.ogg",
                "Assets/_Project/Audio/SFX/forceField_001.ogg",
                "Assets/_Project/Audio/SFX/impactMetal_heavy_000.ogg",
                "Assets/_Project/Audio/SFX/impactMetal_heavy_001.ogg",
                "Assets/_Project/Audio/SFX/impactMetal_light_000.ogg",
                "Assets/_Project/Audio/SFX/impactMetal_light_001.ogg",
                "Assets/_Project/Audio/SFX/computerNoise_000.ogg",
                "Assets/_Project/Audio/UI/click_001.ogg",
                "Assets/_Project/Audio/UI/click_002.ogg",
                "Assets/_Project/Audio/UI/back_001.ogg",
                "Assets/_Project/Audio/UI/confirmation_001.ogg",
                "Assets/_Project/Audio/UI/confirmation_002.ogg",
                "Assets/_Project/Audio/UI/bong_001.ogg",
                "Assets/_Project/Audio/Voice/ready.ogg",
                "Assets/_Project/Audio/Voice/begin.ogg",
                "Assets/_Project/Audio/Voice/fight.ogg",
                "Assets/_Project/Audio/Voice/prepare_yourself.ogg",
                "Assets/_Project/Audio/Voice/game_over.ogg",
                "Assets/_Project/Audio/Voice/flawless_victory.ogg",
            };

            foreach (var path in sfxAndVoice)
                ApplySfxImport(path);
        }

        private static void ApplyMusicImport(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null) return;

            var settings = importer.defaultSampleSettings;
            if (settings.loadType      == AudioClipLoadType.Streaming &&
                importer.loadInBackground == true)
                return; // уже правильно, пропускаем

            settings.loadType             = AudioClipLoadType.Streaming;
            importer.defaultSampleSettings = settings;
            importer.loadInBackground      = true;
            importer.SaveAndReimport();
        }

        private static void ApplySfxImport(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null) return;

            var settings = importer.defaultSampleSettings;
            if (settings.loadType == AudioClipLoadType.DecompressOnLoad)
                return; // уже правильно

            settings.loadType             = AudioClipLoadType.DecompressOnLoad;
            importer.defaultSampleSettings = settings;
            importer.SaveAndReimport();
        }

        // ----------------------------------------------------------------
        // Вспомогательные методы
        // ----------------------------------------------------------------

        private static T Load<T>(string path) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
                Debug.LogWarning($"[Project Forge] Ассет не найден: {path}");
            return asset;
        }

        private static GameObject EnsureGameObject(string goName)
        {
            var existing = GameObject.Find(goName);
            return existing != null ? existing : new GameObject(goName);
        }

        private static T EnsureComponent<T>(GameObject go) where T : Component
        {
            return go.GetComponent<T>() ?? go.AddComponent<T>();
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
