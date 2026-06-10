using System;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DiplomaGame.Runtime.Core
{
    /// <summary>
    /// Ядро M1: управляет переключением режимов RTS↔TPS.
    /// Переключает приоритеты Cinemachine-камер и активные InputActionMap'ы.
    /// Чистая логика приоритетов вынесена в <see cref="ModeSwitchLogic"/>.
    /// </summary>
    public sealed class GameModeController : MonoBehaviour
    {
        [SerializeField] private CinemachineCamera rtsCamera;
        [SerializeField] private CinemachineCamera tpsCamera;
        [SerializeField] private InputActionAsset  actions;

        // ----------------------------------------------------------------
        // Публичный API
        // ----------------------------------------------------------------

        public GameMode CurrentMode { get; private set; }

        /// <summary>Вызывается после каждого успешного переключения режима.</summary>
        public event Action<GameMode> ModeChanged;

        // Флаг: Start уже вызывался, CurrentMode содержит осмысленное значение.
        private bool _initialized;

        // ----------------------------------------------------------------
        // Unity lifecycle
        // ----------------------------------------------------------------

        private void Start()
        {
            SetMode(GameMode.Rts);
        }

        private void OnEnable()
        {
            BindSwitchAction("RTS");
            BindSwitchAction("TPS");
        }

        private void OnDisable()
        {
            UnbindSwitchAction("RTS");
            UnbindSwitchAction("TPS");
        }

        // ----------------------------------------------------------------
        // Переключение режима
        // ----------------------------------------------------------------

        /// <summary>Инвертирует текущий режим.</summary>
        public void SwitchMode()
        {
            SetMode(ModeSwitchLogic.Toggle(CurrentMode));
        }

        /// <summary>
        /// Устанавливает режим идемпотентно: повторный вызов с тем же режимом
        /// не вызывает события и не меняет состояние.
        /// </summary>
        public void SetMode(GameMode mode)
        {
            // Идемпотентность: повторный вызов с тем же режимом после инициализации — пропускаем.
            if (_initialized && CurrentMode == mode)
                return;

            CurrentMode  = mode;
            _initialized = true;

            ApplyCameraPriorities(mode);
            ApplyInputMaps(mode);

            ModeChanged?.Invoke(mode);
        }

        // ----------------------------------------------------------------
        // Internal — доступны PlayMode-тестам через InternalsVisibleTo
        // ----------------------------------------------------------------

        /// <summary>
        /// Инициализация без InputActionAsset — для PlayMode-тестов,
        /// где InputActionAsset не требуется.
        /// </summary>
        internal void InitForTest(CinemachineCamera rts, CinemachineCamera tps)
        {
            rtsCamera    = rts;
            tpsCamera    = tps;
            actions      = null;
            _initialized = false;   // Сброс — позволяет SetMode работать как при первом запуске
        }

        // ----------------------------------------------------------------
        // Приватные методы
        // ----------------------------------------------------------------

        private void ApplyCameraPriorities(GameMode mode)
        {
            var (rtsPriority, tpsPriority) = ModeSwitchLogic.GetPriorities(mode);

            if (rtsCamera != null) rtsCamera.Priority = rtsPriority;
            if (tpsCamera != null) tpsCamera.Priority = tpsPriority;
        }

        private void ApplyInputMaps(GameMode mode)
        {
            if (actions == null) return;

            var rtsMap = actions.FindActionMap("RTS");
            var tpsMap = actions.FindActionMap("TPS");

            if (mode == GameMode.Rts)
            {
                rtsMap?.Enable();
                tpsMap?.Disable();
            }
            else
            {
                tpsMap?.Enable();
                rtsMap?.Disable();
            }
        }

        private void BindSwitchAction(string mapName)
        {
            if (actions == null) return;

            var action = actions.FindActionMap(mapName)?.FindAction("SwitchMode");
            if (action != null)
                action.started += OnSwitchModeInput;
        }

        private void UnbindSwitchAction(string mapName)
        {
            if (actions == null) return;

            var action = actions.FindActionMap(mapName)?.FindAction("SwitchMode");
            if (action != null)
                action.started -= OnSwitchModeInput;
        }

        private void OnSwitchModeInput(InputAction.CallbackContext ctx)
        {
            SwitchMode();
        }
    }
}
