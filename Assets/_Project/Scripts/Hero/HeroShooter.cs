using System;
using DiplomaGame.Runtime.Combat;
using DiplomaGame.Runtime.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DiplomaGame.Runtime.Hero
{
    /// <summary>
    /// Стрельба героя в TPS-режиме: raycast из центра экрана, кулдаун.
    /// Прицел вынесен в CrosshairUI (M6a).
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
        // Бафф Overcharge (способность героя, слот 4)
        // ----------------------------------------------------------------

        private float _overchargeUntil = float.NegativeInfinity;
        private float _overchargeFireRateMult = 1f;
        private float _overchargeDamageMult   = 1f;

        /// <summary>Активна ли сейчас перегрузка.</summary>
        public bool IsOvercharged => AbilityEffectLogic.IsBuffActive(Time.time, _overchargeUntil);

        /// <summary>
        /// Включает временный бафф скорострельности и урона (способность Overcharge).
        /// Повторное применение перезапускает таймер.
        /// </summary>
        public void ApplyOvercharge(float duration, float fireRateMultiplier, float damageMultiplier)
        {
            _overchargeUntil        = Time.time + duration;
            _overchargeFireRateMult = fireRateMultiplier;
            _overchargeDamageMult   = damageMultiplier;
        }

        // ----------------------------------------------------------------
        // Unity lifecycle
        // ----------------------------------------------------------------

        private void Awake()
        {
            _camera = shootCamera != null ? shootCamera : Camera.main;

            if (actions != null)
                _fireAction = actions.FindActionMap("TPS")?.FindAction("Fire");
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

            bool  overcharged       = AbilityEffectLogic.IsBuffActive(now, _overchargeUntil);
            float effectiveCooldown = overcharged
                ? AbilityEffectLogic.EffectiveFireCooldown(fireCooldown, _overchargeFireRateMult)
                : fireCooldown;

            if (!FireRateLogic.CanFire(_lastFireTime, effectiveCooldown, now))
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
                damageable?.TakeDamage(overcharged ? damage * _overchargeDamageMult : damage);
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
