using DiplomaGame.Runtime.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace DiplomaGame.Runtime.UI
{
    /// <summary>
    /// World-space мини-полоса HP над юнитом/зданием.
    /// Скрыта при полном HP — показывается после первого урона.
    /// LateUpdate: билборд к Camera.main.
    /// </summary>
    public sealed class HealthBar : MonoBehaviour
    {
        [SerializeField] private Image fill;

        private Health  _health;
        private Camera  _cam;
        private bool    _everDamaged;

        // ----------------------------------------------------------------
        // Unity lifecycle
        // ----------------------------------------------------------------

        private void Awake()
        {
            // Ищем Health на родителе (или на том же GO)
            _health = GetComponentInParent<Health>(includeInactive: true);

            // Скрыт по умолчанию (полное HP — нет смысла показывать)
            gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            if (_health != null)
            {
                _health.Damaged += OnDamaged;
                _health.Died    += OnDied;
            }
        }

        private void OnDisable()
        {
            if (_health != null)
            {
                _health.Damaged -= OnDamaged;
                _health.Died    -= OnDied;
            }
        }

        private void LateUpdate()
        {
            // Кэшируем камеру (может смениться)
            if (_cam == null)
                _cam = Camera.main;

            if (_cam == null) return;

            // Билборд: поворот к камере (только Y-ось не вращаем, чтобы UI читался горизонтально)
            transform.rotation = Quaternion.LookRotation(
                transform.position - _cam.transform.position,
                Vector3.up);
        }

        // ----------------------------------------------------------------
        // Internal — для PlayMode-тестов (прямое связывание без сцены)
        // ----------------------------------------------------------------

        internal void InitForTest(Health health)
        {
            _health = health;
        }

        // ----------------------------------------------------------------
        // Приватные методы
        // ----------------------------------------------------------------

        private void OnDamaged(float amount, float currentHp)
        {
            if (!_everDamaged)
            {
                _everDamaged = true;
                gameObject.SetActive(true);
            }

            UpdateFill();
        }

        private void OnDied()
        {
            UpdateFill();
        }

        private void UpdateFill()
        {
            if (_health == null || fill == null) return;
            fill.fillAmount = _health.Fraction;
        }
    }
}
