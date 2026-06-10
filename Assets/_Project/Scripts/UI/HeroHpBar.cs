using DiplomaGame.Runtime.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace DiplomaGame.Runtime.UI
{
    /// <summary>
    /// Экранная (Screen Space) полоса HP героя в TPS HUD.
    /// Bind вызывается из UITab.BuildGameHud в Play или через Forge.
    /// </summary>
    public sealed class HeroHpBar : MonoBehaviour
    {
        [SerializeField] private Image fill;

        private Health _health;

        // ----------------------------------------------------------------
        // Unity lifecycle
        // ----------------------------------------------------------------

        private void OnDestroy()
        {
            Unbind();
        }

        // ----------------------------------------------------------------
        // Публичный API
        // ----------------------------------------------------------------

        public void Bind(Health heroHealth)
        {
            Unbind();

            _health = heroHealth;
            if (_health != null)
            {
                _health.Damaged += OnDamaged;
                _health.Died    += OnDied;
                Refresh();
            }
        }

        // ----------------------------------------------------------------
        // Приватные методы
        // ----------------------------------------------------------------

        private void Unbind()
        {
            if (_health != null)
            {
                _health.Damaged -= OnDamaged;
                _health.Died    -= OnDied;
                _health = null;
            }
        }

        private void OnDamaged(float amount, float currentHp)
        {
            Refresh();
        }

        private void OnDied()
        {
            Refresh();
        }

        private void Refresh()
        {
            if (fill == null || _health == null) return;
            fill.fillAmount = _health.Fraction;
        }
    }
}
