using DiplomaGame.Runtime.Core;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace DiplomaGame.Runtime.Hero
{
    /// <summary>
    /// Управляет движением героя в TPS-режиме через CharacterController.
    /// В RTS-режиме уступает управление NavMeshAgent'у.
    /// Подписывается на GameModeController.ModeChanged в OnEnable, отписывается в OnDisable.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class HeroController : MonoBehaviour
    {
        [SerializeField] private float              moveSpeed      = 6f;
        [SerializeField] private float              rotationSpeed  = 720f;   // градусов/сек
        [SerializeField] private float              gravity        = -20f;
        [SerializeField] private float              lookSensitivity = 0.15f;

        // ----------------------------------------------------------------
        // Sprint / Stamina tunables
        // ----------------------------------------------------------------

        [Header("Sprint / Stamina")]
        [Tooltip("Множитель скорости при спринте. 1.6 × moveSpeed (~9.6 при базе 6).")]
        [SerializeField] private float sprintSpeedMultiplier = 1.6f;

        [Tooltip("Максимальный запас стамины.")]
        [SerializeField] private float staminaMax         = 100f;

        [Tooltip("Убыль стамины в секунду во время спринта.")]
        [SerializeField] private float staminaDrainRate   = 25f;

        [Tooltip("Восстановление стамины в секунду когда не спринтуем.")]
        [SerializeField] private float staminaRegenRate   = 15f;

        [Tooltip("Минимальный запас стамины для начала нового спринта.")]
        [SerializeField] private float staminaMinToStart  = 10f;

        [SerializeField] private GameModeController modeController;
        [SerializeField] private InputActionAsset   actions;

        // ----------------------------------------------------------------
        // Кэшированные ссылки (Awake)
        // ----------------------------------------------------------------

        private CharacterController _cc;
        private NavMeshAgent        _agent;         // может быть null на hero-объекте без NavMesh

        // ----------------------------------------------------------------
        // Состояние движения
        // ----------------------------------------------------------------

        private float _verticalVelocity;
        private float _yaw;

        // ----------------------------------------------------------------
        // Состояние спринта / стамины
        // ----------------------------------------------------------------

        private float _stamina;
        private bool  _isSprinting;

        // ----------------------------------------------------------------
        // Input actions (кэш)
        // ----------------------------------------------------------------

        private InputAction _moveAction;
        private InputAction _lookAction;
        private InputAction _sprintAction;

        // ----------------------------------------------------------------
        // Публичный API для UI и FOV (frontend читает каждый кадр)
        // ----------------------------------------------------------------

        /// <summary>True, пока герой активно спринтует в этом кадре.</summary>
        public bool  IsSprinting       => _isSprinting;

        /// <summary>Стамина в диапазоне 0..1 (0 = пусто, 1 = полная).</summary>
        public float StaminaNormalized => staminaMax > 0f ? _stamina / staminaMax : 0f;

        // ----------------------------------------------------------------
        // Unity lifecycle
        // ----------------------------------------------------------------

        private void Awake()
        {
            _cc    = GetComponent<CharacterController>();
            _agent = GetComponent<NavMeshAgent>();  // null-безопасно

            if (actions != null)
            {
                var tpsMap    = actions.FindActionMap("TPS");
                _moveAction   = tpsMap?.FindAction("Move");
                _lookAction   = tpsMap?.FindAction("Look");
                _sprintAction = tpsMap?.FindAction("Sprint");
            }

            // Читаем чувствительность из SettingsService при старте
            lookSensitivity = SettingsService.LoadMouseSensitivity();

            // Стамина начинается полной
            _stamina = staminaMax;

            // Подписываемся в Awake/OnDestroy, а не OnEnable/OnDisable,
            // иначе self-disable обрывает подписку и повторный TPS-переход игнорируется.
            if (modeController != null)
                modeController.ModeChanged += OnModeChanged;
        }

        // ----------------------------------------------------------------
        // Публичный сеттер чувствительности — вызывается из SettingsPanel при изменении
        // ----------------------------------------------------------------

        /// <summary>
        /// Устанавливает чувствительность мыши и немедленно применяет её.
        /// </summary>
        public void SetLookSensitivity(float sensitivity)
        {
            lookSensitivity = SettingsLogic.ClampSensitivity(sensitivity);
        }

        private void OnDestroy()
        {
            if (modeController != null)
                modeController.ModeChanged -= OnModeChanged;
        }

        private void Start()
        {
            // Синхронизируем начальное состояние: если режим уже установлен — применяем.
            if (modeController != null)
                OnModeChanged(modeController.CurrentMode);
        }

        private void OnDisable()
        {
            // Намеренно пусто: отписка в OnDestroy для сохранения подписки
            // при self-disable в RTS-режиме.
        }

        private void Update()
        {
            // Компонент выключается в RTS-режиме, поэтому Update работает только в TPS
            var moveInput = _moveAction   != null ? _moveAction.ReadValue<Vector2>()  : Vector2.zero;
            var lookInput = _lookAction   != null ? _lookAction.ReadValue<Vector2>()  : Vector2.zero;
            bool sprintHeld = _sprintAction != null ? _sprintAction.IsPressed()       : false;

            // Вращение по yaw (горизонталь)
            _yaw = transform.eulerAngles.y + lookInput.x * lookSensitivity * rotationSpeed * Time.deltaTime;
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);

            // Направление движения относительно yaw героя
            var moveDir  = HeroMovementLogic.GetWorldMoveDirection(moveInput, _yaw);
            bool isMoving = moveInput.sqrMagnitude > 1e-6f;

            // Тик спринта / стамины (чистая логика без аллокаций)
            (_stamina, _isSprinting) = SprintStaminaLogic.Tick(
                currentStamina: _stamina,
                sprintHeld:     sprintHeld,
                isMoving:       isMoving,
                dt:             Time.deltaTime,
                drainRate:      staminaDrainRate,
                regenRate:      staminaRegenRate,
                max:            staminaMax,
                minToStart:     staminaMinToStart,
                wasSprinting:   _isSprinting);

            // Гравитация
            _verticalVelocity = HeroMovementLogic.ApplyGravity(
                _verticalVelocity, gravity, Time.deltaTime, _cc.isGrounded);

            float effectiveSpeed = moveSpeed * (_isSprinting ? sprintSpeedMultiplier : 1f);
            var   velocity       = moveDir * effectiveSpeed;
            velocity.y           = _verticalVelocity;

            _cc.Move(velocity * Time.deltaTime);
        }

        // ----------------------------------------------------------------
        // Публичный API (для AbilitySystem — Dash)
        // ----------------------------------------------------------------

        /// <summary>
        /// Мгновенный рывок вперёд на заданную дистанцию.
        /// </summary>
        public void Dash(float distance)
        {
            _cc.Move(transform.forward * distance);
        }

        // ----------------------------------------------------------------
        // Internal — для PlayMode-тестов
        // ----------------------------------------------------------------

        /// <summary>
        /// Инициализация для PlayMode-тестов без InputActionAsset.
        /// Переподписывается на новый контроллер.
        /// </summary>
        internal void InitForTest(GameModeController controller)
        {
            // Отписываемся от старого контроллера (если был)
            if (modeController != null)
                modeController.ModeChanged -= OnModeChanged;

            modeController = controller;
            actions        = null;
            _moveAction    = null;
            _lookAction    = null;
            _sprintAction  = null;

            // Подписываемся на новый
            if (modeController != null)
                modeController.ModeChanged += OnModeChanged;
        }

        // ----------------------------------------------------------------
        // Приватные методы
        // ----------------------------------------------------------------

        private void OnModeChanged(GameMode mode)
        {
            bool isTps = mode == GameMode.Tps;

            // Переключаем компоненты движения
            enabled      = isTps;
            _cc.enabled  = isTps;

            if (_agent != null)
                _agent.enabled = !isTps;
        }
    }
}
