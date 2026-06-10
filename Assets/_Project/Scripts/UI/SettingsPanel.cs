using System;
using DiplomaGame.Runtime.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiplomaGame.Runtime.UI
{
    /// <summary>
    /// Панель настроек. Используется и в главном меню, и в паузе.
    /// OnEnable — загружает текущие значения из SettingsService.
    /// Любое изменение контрола немедленно сохраняет и применяет настройку.
    /// Кнопка «Назад» поднимает событие <see cref="Closed"/>.
    /// </summary>
    public sealed class SettingsPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Dropdown qualityDropdown;
        [SerializeField] private Toggle       fullscreenToggle;
        [SerializeField] private Slider       masterVolumeSlider;
        [SerializeField] private Slider       musicVolumeSlider;
        [SerializeField] private Slider       sfxVolumeSlider;
        [SerializeField] private Slider       sensitivitySlider;
        [SerializeField] private Button       backButton;

        /// <summary>Вызывается при нажатии кнопки «Назад».</summary>
        public event Action Closed;

        // ----------------------------------------------------------------
        // Unity lifecycle
        // ----------------------------------------------------------------

        private void OnEnable()
        {
            PopulateQualityDropdown();
            LoadAllValues();
            SubscribeHandlers();
        }

        private void OnDisable()
        {
            UnsubscribeHandlers();
        }

        // ----------------------------------------------------------------
        // Внутренние методы
        // ----------------------------------------------------------------

        private void PopulateQualityDropdown()
        {
            if (qualityDropdown == null) return;

            qualityDropdown.ClearOptions();
            var options = new System.Collections.Generic.List<string>(QualitySettings.names);
            qualityDropdown.AddOptions(options);
        }

        private void LoadAllValues()
        {
            if (qualityDropdown != null)
                qualityDropdown.value = SettingsLogic.ClampQuality(
                    SettingsService.LoadQuality(),
                    QualitySettings.names.Length);

            if (fullscreenToggle != null)
                fullscreenToggle.isOn = SettingsService.LoadFullscreen();

            if (masterVolumeSlider != null)
                masterVolumeSlider.value = SettingsService.LoadMasterVolume();

            if (musicVolumeSlider != null)
                musicVolumeSlider.value = SettingsService.LoadMusicVolume();

            if (sfxVolumeSlider != null)
                sfxVolumeSlider.value = SettingsService.LoadSfxVolume();

            if (sensitivitySlider != null)
                sensitivitySlider.value = SettingsService.LoadMouseSensitivity();
        }

        private void SubscribeHandlers()
        {
            if (qualityDropdown != null)
                qualityDropdown.onValueChanged.AddListener(OnQualityChanged);

            if (fullscreenToggle != null)
                fullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);

            if (masterVolumeSlider != null)
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);

            if (musicVolumeSlider != null)
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);

            if (sfxVolumeSlider != null)
                sfxVolumeSlider.onValueChanged.AddListener(OnSfxVolumeChanged);

            if (sensitivitySlider != null)
                sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);

            if (backButton != null)
                backButton.onClick.AddListener(OnBackClicked);
        }

        private void UnsubscribeHandlers()
        {
            if (qualityDropdown != null)
                qualityDropdown.onValueChanged.RemoveListener(OnQualityChanged);

            if (fullscreenToggle != null)
                fullscreenToggle.onValueChanged.RemoveListener(OnFullscreenChanged);

            if (masterVolumeSlider != null)
                masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);

            if (musicVolumeSlider != null)
                musicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);

            if (sfxVolumeSlider != null)
                sfxVolumeSlider.onValueChanged.RemoveListener(OnSfxVolumeChanged);

            if (sensitivitySlider != null)
                sensitivitySlider.onValueChanged.RemoveListener(OnSensitivityChanged);

            if (backButton != null)
                backButton.onClick.RemoveListener(OnBackClicked);
        }

        // ----------------------------------------------------------------
        // Обработчики контролов
        // ----------------------------------------------------------------

        private void OnQualityChanged(int index)
        {
            SettingsService.SaveQuality(index);
            SettingsService.ApplyAll();
        }

        private void OnFullscreenChanged(bool value)
        {
            SettingsService.SaveFullscreen(value);
            SettingsService.ApplyAll();
        }

        private void OnMasterVolumeChanged(float value)
        {
            SettingsService.SaveMasterVolume(value);
            SettingsService.ApplyAll();
        }

        private void OnMusicVolumeChanged(float value)
        {
            SettingsService.SaveMusicVolume(value);
            // TODO M7: применить MusicVolume к AudioMixer немедленно
        }

        private void OnSfxVolumeChanged(float value)
        {
            SettingsService.SaveSfxVolume(value);
            // TODO M7: применить SfxVolume к AudioMixer немедленно
        }

        private void OnSensitivityChanged(float value)
        {
            SettingsService.SaveMouseSensitivity(value);

            // Если в сцене есть HeroController — применяем немедленно
            var hero = UnityEngine.Object.FindFirstObjectByType<DiplomaGame.Runtime.Hero.HeroController>();
            if (hero != null)
                hero.SetLookSensitivity(value);
        }

        private void OnBackClicked()
        {
            Closed?.Invoke();
        }
    }
}
