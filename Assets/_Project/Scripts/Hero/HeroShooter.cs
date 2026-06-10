using System;
using DiplomaGame.Runtime.Combat;
using DiplomaGame.Runtime.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DiplomaGame.Runtime.Hero
{
    /// <summary>
    /// Стрельба героя в TPS-режиме: raycast из центра экрана,
    /// кулдаун, простейший crosshair на OnGUI.
    /// </summary>
    public sealed class HeroShooter : MonoBehaviour
    {
        [SerializeField] private float              fireCooldown   = 0.15f;
        [SerializeField] private float              maxDistance    = 100f;
        [SerializeField] private float              damage         = 10f;    // задел для M4

        [SerializeField] private Camera             shootCamera;             // null → Camera.main
        [SerializeField] private GameModeController modeController;
        [SerializeField] private InputActionAsset   actions;

        // ----------------------------------------------------------------
        // Событие (VFX-шина — M8)
        // ----------------------------------------------------------------

        /// <summary>Вызывается после каждого выстрела.</summary>
        public event Action<Vector3, Vector3, bool> ShotFired;

        // ----------------------------------------------------------------
        // Кэш (Awake)
        // ----------------------------------------------------------------

        private InputAction _fireAction;
        private Camera      _camera;
        private float       _lastFireTime;

        // ----------------------------------------------------------------
        // Crosshair texture (кэш, создаётся один раз)
        // ----------------------------------------------------------------

        private Texture2D _crosshairTex;

        // ----------------------------------------------------------------
        // Unity lifecycle
        // ----------------------------------------------------------------

        private void Awake()
        {
            _camera = shootCamera != null ? shootCamera : Camera.main;

            if (actions != null)
                _fireAction = actions.FindActionMap("TPS")?.FindAction("Fire");

            // 4×4 белая текстура для прицела
            _crosshairTex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            var pixels = new Color[16];
            for (int i = 0; i < 16; i++)
                pixels[i] = Color.white;
            _crosshairTex.SetPixels(pixels);
            _crosshairTex.Apply();
        }

        private void OnEnable()
        {
            if (_fireAction != null)
                _fireAction.performed += OnFirePerformed;

            if (modeController != null)
                modeController.ModeChanged += OnModeChanged;
        }

        private void OnDisable()
        {
            if (_fireAction != null)
                _fireAction.performed -= OnFirePerformed;

            if (modeController != null)
                modeController.ModeChanged -= OnModeChanged;
        }

        private void OnDestroy()
        {
            if (_crosshairTex != null)
                Destroy(_crosshairTex);
        }

        private void OnGUI()
        {
            if (modeController == null || modeController.CurrentMode != GameMode.Tps)
                return;

            // Рисуем крестик в центре экрана
            const int size    = 4;
            const int armLen  = 8;
            int cx = Screen.width  / 2;
            int cy = Screen.height / 2;

            // Горизонтальная черта
            GUI.DrawTexture(new Rect(cx - armLen, cy - size / 2, armLen * 2, size), _crosshairTex);
            // Вертикальная черта
            GUI.DrawTexture(new Rect(cx - size / 2, cy - armLen, size, armLen * 2), _crosshairTex);
        }

        // ----------------------------------------------------------------
        // Internal — для PlayMode-тестов
        // ----------------------------------------------------------------

        /// <summary>
        /// Инициализация для тестов: подставляет Camera и GameModeController без InputActionAsset.
        /// </summary>
        internal void InitForTest(GameModeController controller, Camera cam)
        {
            modeController = controller;
            actions        = null;
            _fireAction    = null;
            _camera        = cam;
        }

        /// <summary>
        /// Вызывает выстрел напрямую — для PlayMode-тестов.
        /// </summary>
        internal void TryFire()
        {
            PerformShot();
        }

        // ----------------------------------------------------------------
        // Приватные методы
        // ----------------------------------------------------------------

        private void OnModeChanged(GameMode mode)
        {
            // Ничего специального — просто не стреляем вне TPS (проверка в PerformShot)
        }

        private void OnFirePerformed(InputAction.CallbackContext ctx)
        {
            if (modeController == null || modeController.CurrentMode != GameMode.Tps)
                return;

            PerformShot();
        }

        private void PerformShot()
        {
            float now = Time.time;
            if (!FireRateLogic.CanFire(_lastFireTime, fireCooldown, now))
                return;

            _lastFireTime = now;

            if (_camera == null)
                _camera = Camera.main;

            if (_camera == null)
                return;

            var ray = _camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            bool hit;
            Vector3 endPoint;

            if (Physics.Raycast(ray, out RaycastHit hitInfo, maxDistance))
            {
                endPoint = hitInfo.point;
                hit      = true;

                var damageable = hitInfo.collider.GetComponentInParent<IDamageable>();
                damageable?.TakeDamage(damage);
            }
            else
            {
                endPoint = ray.origin + ray.direction * maxDistance;
                hit      = false;
            }

            ShotFired?.Invoke(ray.origin, endPoint, hit);
        }
    }
}
