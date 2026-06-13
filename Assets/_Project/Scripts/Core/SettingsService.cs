using DiplomaGame.Runtime.Audio;
using UnityEngine;

namespace DiplomaGame.Runtime.Core
{
    /// <summary>
    /// Статический сервис настроек поверх PlayerPrefs.
    /// Загружает, сохраняет и применяет параметры: качество, полный экран,
    /// громкости, чувствительность мыши.
    /// </summary>
    public static class SettingsService
    {
        // ----------------------------------------------------------------
        // Ключи PlayerPrefs
        // ----------------------------------------------------------------

        private const string KeyQuality     = "Settings.Quality";
        private const string KeyFullscreen  = "Settings.Fullscreen";
        private const string KeyMasterVol   = "Settings.MasterVolume";
        private const string KeyMusicVol    = "Settings.MusicVolume";
        private const string KeySfxVol      = "Settings.SfxVolume";
        private const string KeySensitivity = "Settings.MouseSensitivity";

        // ----------------------------------------------------------------
        // Quality
        // ----------------------------------------------------------------

        public static int LoadQuality()
        {
            int defaultQuality = QualitySettings.GetQualityLevel();
            return PlayerPrefs.GetInt(KeyQuality, defaultQuality);
        }

        public static void SaveQuality(int level)
        {
            int clamped = SettingsLogic.ClampQuality(level, QualitySettings.names.Length);
            PlayerPrefs.SetInt(KeyQuality, clamped);
            PlayerPrefs.Save();
        }

        // ----------------------------------------------------------------
        // Fullscreen
        // ----------------------------------------------------------------

        public static bool LoadFullscreen()
        {
            return PlayerPrefs.GetInt(KeyFullscreen, 1) != 0;
        }

        public static void SaveFullscreen(bool value)
        {
            PlayerPrefs.SetInt(KeyFullscreen, value ? 1 : 0);
            PlayerPrefs.Save();
        }

        // ----------------------------------------------------------------
        // Master Volume
        // ----------------------------------------------------------------

        public static float LoadMasterVolume()
        {
            return PlayerPrefs.GetFloat(KeyMasterVol, 1f);
        }

        public static void SaveMasterVolume(float value)
        {
            PlayerPrefs.SetFloat(KeyMasterVol, SettingsLogic.ClampVolume01(value));
            PlayerPrefs.Save();
        }

        // ----------------------------------------------------------------
        // Music Volume
        // ----------------------------------------------------------------

        public static float LoadMusicVolume()
        {
            return PlayerPrefs.GetFloat(KeyMusicVol, 1f);
        }

        public static void SaveMusicVolume(float value)
        {
            PlayerPrefs.SetFloat(KeyMusicVol, SettingsLogic.ClampVolume01(value));
            PlayerPrefs.Save();
        }

        // ----------------------------------------------------------------
        // SFX Volume
        // ----------------------------------------------------------------

        public static float LoadSfxVolume()
        {
            return PlayerPrefs.GetFloat(KeySfxVol, 1f);
        }

        public static void SaveSfxVolume(float value)
        {
            PlayerPrefs.SetFloat(KeySfxVol, SettingsLogic.ClampVolume01(value));
            PlayerPrefs.Save();
        }

        // ----------------------------------------------------------------
        // Mouse Sensitivity
        // ----------------------------------------------------------------

        public static float LoadMouseSensitivity()
        {
            return PlayerPrefs.GetFloat(KeySensitivity, 0.15f);
        }

        public static void SaveMouseSensitivity(float value)
        {
            PlayerPrefs.SetFloat(KeySensitivity, SettingsLogic.ClampSensitivity(value));
            PlayerPrefs.Save();
        }

        // ----------------------------------------------------------------
        // Difficulty
        // ----------------------------------------------------------------

        private const string KeyDifficulty = "Settings.Difficulty";

        /// <summary>Загружает индекс сложности (0=Easy, 1=Normal, 2=Hard). Дефолт 1 (Normal).</summary>
        public static int LoadDifficulty()
        {
            return PlayerPrefs.GetInt(KeyDifficulty, 1);
        }

        /// <summary>Сохраняет индекс сложности (clamp 0..2).</summary>
        public static void SaveDifficulty(int index)
        {
            int clamped = Mathf.Clamp(index, 0, 2);
            PlayerPrefs.SetInt(KeyDifficulty, clamped);
            PlayerPrefs.Save();
        }

        // ----------------------------------------------------------------
        // ApplyAll
        // ----------------------------------------------------------------

        /// <summary>Ключ PlayerPrefs для UI-громкости.</summary>
        private const string KeyUiVol    = "Settings.UiVolume";

        /// <summary>Ключ PlayerPrefs для Voice-громкости.</summary>
        private const string KeyVoiceVol = "Settings.VoiceVolume";

        public static float LoadUiVolume()    => PlayerPrefs.GetFloat(KeyUiVol,    1f);
        public static float LoadVoiceVolume() => PlayerPrefs.GetFloat(KeyVoiceVol, 1f);

        public static void SaveUiVolume(float value)
        {
            PlayerPrefs.SetFloat(KeyUiVol, SettingsLogic.ClampVolume01(value));
            PlayerPrefs.Save();
        }

        public static void SaveVoiceVolume(float value)
        {
            PlayerPrefs.SetFloat(KeyVoiceVol, SettingsLogic.ClampVolume01(value));
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Применяет все сохранённые настройки к движку.
        /// M7: громкости идут в AudioManager (через mixer или fallback).
        /// </summary>
        public static void ApplyAll()
        {
            // Качество
            int quality = LoadQuality();
            quality = SettingsLogic.ClampQuality(quality, QualitySettings.names.Length);
            QualitySettings.SetQualityLevel(quality, true);

            // Полный экран
            Screen.fullScreen = LoadFullscreen();

            // Master — AudioListener (глобальный множитель)
            float master = SettingsLogic.ClampVolume01(LoadMasterVolume());
            AudioListener.volume = master;

            // Остальные категории — AudioManager
            var am = AudioManager.Instance;
            if (am != null)
            {
                am.SetCategoryVolume(AudioManager.VolumeCategory.Master, master);
                am.SetCategoryVolume(AudioManager.VolumeCategory.Music,  SettingsLogic.ClampVolume01(LoadMusicVolume()));
                am.SetCategoryVolume(AudioManager.VolumeCategory.Sfx,    SettingsLogic.ClampVolume01(LoadSfxVolume()));
                am.SetCategoryVolume(AudioManager.VolumeCategory.Ui,     SettingsLogic.ClampVolume01(LoadUiVolume()));
                am.SetCategoryVolume(AudioManager.VolumeCategory.Voice,  SettingsLogic.ClampVolume01(LoadVoiceVolume()));
            }
        }
    }
}
