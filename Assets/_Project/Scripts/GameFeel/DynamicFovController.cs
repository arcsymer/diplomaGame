using DiplomaGame.Runtime.Core;
using DiplomaGame.Runtime.Data;
using DiplomaGame.Runtime.Hero;
using Unity.Cinemachine;
using UnityEngine;

namespace DiplomaGame.Runtime.GameFeel
{
    /// <summary>
    /// MonoBehaviour, управляющий динамическим FOV TPS-камеры.
    ///
    /// Работает ТОЛЬКО в TPS-режиме (подписывается на GameModeController.ModeChanged).
    /// В RTS-режиме сбрасывает камеру в baseFov и не трогает.
    ///
    /// Kick-триггеры:
    ///   • AbilitySystem.AbilityCast с AbilityType.Dash    → kick
    ///   • AbilitySystem.AbilityCast с AbilityType.Overcharge → kick
    ///
    /// Sprint-widen: не реализован — HeroController не экспонирует отдельный флаг
    /// спринта/быстрого движения (единственный moveSpeed без состояния IsSprinting).
    ///
    /// CM 3.x API: CinemachineCamera.Lens — struct LensSettings.
    /// Мутация: var lens = _tpsCamera.Lens; lens.FieldOfView = v; _tpsCamera.Lens = lens;
    /// </summary>
    public sealed class DynamicFovController : MonoBehaviour
    {
        // ----------------------------------------------------------------
        // Сериализованные поля
        // ----------------------------------------------------------------

        [SerializeField] private CinemachineCamera  _tpsCamera;
        [SerializeField] private AbilitySystem       _abilitySystem;
        [SerializeField] private GameModeController  _modeController;
        [SerializeField] private GameFeelSettings    _settings;

        // ----------------------------------------------------------------
        // Runtime state (нет аллокаций — только float)
        // ----------------------------------------------------------------

        private float _baseFov;          // оригинальный FOV камеры (захватывается в Awake / кеш-reset)
        private float _currentFov;       // текущий интерполированный FOV
        private float _kickRemaining;    // оставшийся kick-таймер (с)
        private bool  _isTpsMode;

        // ----------------------------------------------------------------
        // Unity lifecycle
        // ----------------------------------------------------------------

        private void Awake()
        {
            CacheFov();
        }

        private void OnEnable()
        {
            if (_modeController != null)
            {
                _modeController.ModeChanged += OnModeChanged;
                _isTpsMode = _modeController.CurrentMode == GameMode.Tps;
            }

            if (_abilitySystem != null)
                _abilitySystem.AbilityCast += OnAbilityCast;
        }

        private void OnDisable()
        {
            if (_modeController != null)
                _modeController.ModeChanged -= OnModeChanged;

            if (_abilitySystem != null)
                _abilitySystem.AbilityCast -= OnAbilityCast;

            // Восстанавливаем базовый FOV при отключении
            ApplyFov(_baseFov);
            _currentFov    = _baseFov;
            _kickRemaining = 0f;
        }

        private void LateUpdate()
        {
            if (!_isTpsMode) return;
            if (_tpsCamera == null || _settings == null) return;

            float kickAmt   = _settings.fovKickAmount;
            float returnSpd = _settings.fovReturnSpeed;

            var (nextFov, nextKick) = DynamicFovLogic.Tick(
                _currentFov,
                _baseFov,
                kickAmt,
                _kickRemaining,
                returnSpd,
                Time.deltaTime);

            _currentFov    = nextFov;
            _kickRemaining = nextKick;

            ApplyFov(_currentFov);
        }

        // ----------------------------------------------------------------
        // Internal (для тестов)
        // ----------------------------------------------------------------

        /// <summary>Базовый FOV (для тестов).</summary>
        internal float BaseFov        => _baseFov;

        /// <summary>Текущий интерполированный FOV (для тестов).</summary>
        internal float CurrentFov     => _currentFov;

        /// <summary>Оставшийся kick-таймер (для тестов).</summary>
        internal float KickRemaining  => _kickRemaining;

        /// <summary>Принудительно инициализирует зависимости без сцены (для тестов).</summary>
        internal void InitForTest(
            CinemachineCamera tpsCam,
            AbilitySystem abilitySystem,
            GameModeController modeController,
            GameFeelSettings settings)
        {
            _tpsCamera     = tpsCam;
            _abilitySystem = abilitySystem;
            _modeController = modeController;
            _settings      = settings;
            CacheFov();
        }

        // ----------------------------------------------------------------
        // Приватные методы
        // ----------------------------------------------------------------

        private void CacheFov()
        {
            if (_tpsCamera != null)
            {
                _baseFov    = _tpsCamera.Lens.FieldOfView;
                _currentFov = _baseFov;
            }
        }

        private void ApplyFov(float fov)
        {
            if (_tpsCamera == null) return;
            var lens          = _tpsCamera.Lens;
            lens.FieldOfView  = fov;
            _tpsCamera.Lens   = lens;
        }

        private void OnModeChanged(GameMode mode)
        {
            _isTpsMode = mode == GameMode.Tps;

            if (!_isTpsMode)
            {
                // Переход в RTS — сбросить FOV и таймер немедленно
                _kickRemaining = 0f;
                _currentFov    = _baseFov;
                ApplyFov(_baseFov);
            }
        }

        private void OnAbilityCast(int slot, AbilityData data)
        {
            if (data == null || _settings == null) return;
            if (!_isTpsMode) return;

            switch (data.AbilityType)
            {
                case AbilityType.Dash:
                case AbilityType.Overcharge:
                    _kickRemaining = DynamicFovLogic.TriggerKick(_settings.fovKickDuration);
                    break;
            }
        }
    }
}
