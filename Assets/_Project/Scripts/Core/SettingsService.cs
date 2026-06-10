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
        // ApplyAll
        // ----------------------------------------------------------------

        /// <summary>
        /// Применяет все сохранённые настройки к движку.
        /// AudioMixer-интеграция придёт в M7.
        /// </summary>
        public static void ApplyAll()
        {
            // Качество
            int quality = LoadQuality();
            quality = SettingsLogic.ClampQuality(quality, QualitySettings.names.Length);
            QualitySettings.SetQualityLevel(quality, true);

            // Полный экран
            Screen.fullScreen = LoadFullscreen();

            // Громкость — AudioListener (мастер)
            // TODO M7: перепривязать к AudioMixer (MasterVolume, MusicVolume, SfxVolume).
            AudioListener.volume = SettingsLogic.ClampVolume01(LoadMasterVolume());

            // MusicVolume и SfxVolume пока только сохраняются, ждут AudioMixer из M7.
        }
    }
}
