using DiplomaGame.Runtime.Core;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DiplomaGame.Runtime.CameraControl
{
    /// <summary>
    /// Управляет RTS-камерой: панорамирование target-объекта по XZ и масштаб Follow Offset.
    /// Активен только в RTS-режиме (подписывается на GameModeController.ModeChanged).
    /// </summary>
    public sealed class RtsCameraController : MonoBehaviour
    {
        [SerializeField] private Transform           target;
        [SerializeField] private CinemachineFollow   rtsFollow;
        [SerializeField] private GameModeController  modeController;
        [SerializeField] private InputActionAsset    actions;

        [SerializeField] private float panSpeed  = 20f;
        [SerializeField] private float zoomMin   =  5f;
        [SerializeField] private float zoomMax   = 40f;
        [SerializeField] private float zoomSpeed =  5f;

        // ----------------------------------------------------------------
        // Кэш действий
        // ----------------------------------------------------------------

        private InputAction _panAction;
        private InputAction _zoomAction;

        // Активность
        private bool _isRtsMode;

        // ----------------------------------------------------------------
        // Unity lifecycle
        // ----------------------------------------------------------------

        private void OnEnable()
        {
            if (modeController != null)
            {
                modeController.ModeChanged += OnModeChanged;
                _isRtsMode = modeController.CurrentMode == GameMode.Rts;
            }

            BindActions();
        }

        private void OnDisable()
        {
            if (modeController != null)
                modeController.ModeChanged -= OnModeChanged;
        }

        private void Update()
        {
            if (!_isRtsMode) return;
            if (target == null) return;

            HandlePan();
            HandleZoom();
        }

        // ----------------------------------------------------------------
        // Подписка на режим
        // ----------------------------------------------------------------

        private void OnModeChanged(GameMode mode)
        {
            _isRtsMode = mode == GameMode.Rts;
        }

        // ----------------------------------------------------------------
        // Биндинг
        // ----------------------------------------------------------------

        private void BindActions()
        {
            if (actions == null) return;

            var rtsMap = actions.FindActionMap("RTS");
            if (rtsMap == null) return;

            _panAction  = rtsMap.FindAction("PanCamera");
            _zoomAction = rtsMap.FindAction("ZoomCamera");
        }

        // ----------------------------------------------------------------
        // Логика управления
        // ----------------------------------------------------------------

        private void HandlePan()
        {
            if (_panAction == null) return;

            Vector2 input = _panAction.ReadValue<Vector2>();
            if (input == Vector2.zero) return;

            Vector3 delta = new Vector3(input.x, 0f, input.y) * (panSpeed * Time.deltaTime);
            target.position += delta;
        }

        private void HandleZoom()
        {
            if (_zoomAction == null) return;
            if (rtsFollow == null) return;

            float scroll = _zoomAction.ReadValue<float>();
            if (Mathf.Approximately(scroll, 0f)) return;

            var offset = rtsFollow.FollowOffset;
            // Масштабируем Y-компоненту offset (высота = дальность)
            float newY = Mathf.Clamp(offset.y - scroll * zoomSpeed * Time.deltaTime, zoomMin, zoomMax);
            // Пропорционально масштабируем Z, сохраняя соотношение
            float ratio = newY / (offset.y > 0.001f ? offset.y : 1f);
            rtsFollow.FollowOffset = new Vector3(offset.x, newY, offset.z * ratio);
        }
    }
}
