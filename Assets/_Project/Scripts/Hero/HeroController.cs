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
        // Input actions (кэш)
        // ----------------------------------------------------------------

        private InputAction _moveAction;
        private InputAction _lookAction;

        // ----------------------------------------------------------------
        // Unity lifecycle
        // ----------------------------------------------------------------

        private void Awake()
        {
            _cc    = GetComponent<CharacterController>();
            _agent = GetComponent<NavMeshAgent>();  // null-безопасно

            if (actions != null)
            {
                _moveAction = actions.FindActionMap("TPS")?.FindAction("Move");
                _lookAction = actions.FindActionMap("TPS")?.FindAction("Look");
            }

            // Читаем чувствительность из SettingsService при старте
            lookSensitivity = SettingsService.LoadMouseSensitivity();

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
            var moveInput = _moveAction != null ? _moveAction.ReadValue<Vector2>() : Vector2.zero;
            var lookInput = _lookAction != null ? _lookAction.ReadValue<Vector2>() : Vector2.zero;

            // Вращение по yaw (горизонталь)
            _yaw = transform.eulerAngles.y + lookInput.x * lookSensitivity * rotationSpeed * Time.deltaTime;
            transform.rotation = Quaternion.Euler(0f, _yaw, 0f);

            // Направление движения относительно yaw героя
            var moveDir = HeroMovementLogic.GetWorldMoveDirection(moveInput, _yaw);

            // Гравитация
            _verticalVelocity = HeroMovementLogic.ApplyGravity(
                _verticalVelocity, gravity, Time.deltaTime, _cc.isGrounded);

            var velocity = moveDir * moveSpeed;
            velocity.y   = _verticalVelocity;

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
