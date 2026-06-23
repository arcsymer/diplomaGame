using System.Collections;
using DiplomaGame.Runtime.Combat;
using DiplomaGame.Runtime.GameFeel;
using DiplomaGame.Runtime.Hero;
using UnityEngine;
using UnityEngine.UI;

namespace DiplomaGame.Runtime.UI
{
    /// <summary>
    /// Индикатор урона героя (Circle-21).
    /// Когда герой получает урон, показывает red-edge flash по периметру экрана,
    /// затухающий за ~1 с (настраивается через <see cref="GameFeelSettings"/>).
    ///
    /// Дросселирование: быстрые повторные попадания обновляют (extend) текущий флэш,
    /// а не стакаются новыми вспышками — interrupt-restart coroutine pattern.
    ///
    /// Limitation: <see cref="Health.AnyDamaged"/> не несёт позицию источника урона,
    /// поэтому индикатор является full-edge flash, а не направленной стрелкой.
    /// Направленный вариант возможен только при изменении API Health (передача attackerPosition).
    ///
    /// Компонент должен жить в TPS_Block (активируется только в TPS-режиме).
    /// </summary>
    public sealed class HeroDamageIndicator : MonoBehaviour
    {
        // ----------------------------------------------------------------
        // Сериализованные ссылки (проставляются через Forge)
        // ----------------------------------------------------------------

        [Header("Hero Reference")]
        [Tooltip("Health героя. Проставляется через Forge (Hero/Health).")]
        [SerializeField] private Health _heroHealth;

        [Header("UI")]
        [Tooltip("Image с alpha=0 и full-stretch поверх TPS-блока (красный edge flash).")]
        [SerializeField] private Image _edgeFlash;

        [Header("Settings")]
        [Tooltip("GameFeelSettings — содержит параметры длительности и пика альфы.")]
        [SerializeField] private GameFeelSettings _settings;

        // ----------------------------------------------------------------
        // Внутреннее состояние
        // ----------------------------------------------------------------

        private Coroutine _flashRoutine;

        // ----------------------------------------------------------------
        // Unity lifecycle
        // ----------------------------------------------------------------

        private void OnEnable()
        {
            Health.AnyDamaged += OnAnyDamaged;
        }

        private void OnDisable()
        {
            Health.AnyDamaged -= OnAnyDamaged;
            CancelFlash();
        }

        // ----------------------------------------------------------------
        // Обработчик события (горячий путь — без аллокаций)
        // ----------------------------------------------------------------

        private void OnAnyDamaged(Health health, float amount)
        {
            // Фильтр: только наш герой
            if (health == null) return;
            if (_heroHealth == null) return;
            if (!ReferenceEquals(health, _heroHealth)) return;

            // Герой мёртв — не показываем индикатор
            if (_heroHealth.IsDead) return;

            TriggerFlash();
        }

        // ----------------------------------------------------------------
        // Internal — для PlayMode-тестов и Forge
        // ----------------------------------------------------------------

        /// <summary>Проставляет ссылки для PlayMode-тестов без SerializedObject.</summary>
        internal void InitForTest(Health heroHealth, Image edgeFlash, GameFeelSettings settings)
        {
            _heroHealth = heroHealth;
            _edgeFlash  = edgeFlash;
            _settings   = settings;
        }

        /// <summary>Флаг активного флэша (для тестов).</summary>
        internal bool FlashActive => _flashRoutine != null;

        // ----------------------------------------------------------------
        // Приватные методы
        // ----------------------------------------------------------------

        private void TriggerFlash()
        {
            // Interrupt-restart: если флэш уже идёт — рестартуем (extends duration)
            if (_flashRoutine != null)
                StopCoroutine(_flashRoutine);

            _flashRoutine = StartCoroutine(FlashRoutine());
        }

        private IEnumerator FlashRoutine()
        {
            if (_edgeFlash == null)
            {
                _flashRoutine = null;
                yield break;
            }

            float duration  = _settings != null ? _settings.damageIndicatorDuration : 1f;
            float peakAlpha = _settings != null ? _settings.damageIndicatorPeakAlpha : 0.6f;
            duration = Mathf.Max(duration, 0.1f);

            _edgeFlash.gameObject.SetActive(true);
            SetAlpha(_edgeFlash, peakAlpha);

            float elapsed = 0f;
            while (!HeroDamageIndicatorLogic.IsFadeDone(elapsed, duration))
            {
                elapsed += Time.unscaledDeltaTime;
                float t     = Mathf.Clamp01(elapsed / duration);
                float alpha = HeroDamageIndicatorLogic.ComputeFadeAlpha(t, peakAlpha);
                SetAlpha(_edgeFlash, alpha);
                yield return null;
            }

            SetAlpha(_edgeFlash, 0f);
            _flashRoutine = null;
        }

        private void CancelFlash()
        {
            if (_flashRoutine != null)
            {
                StopCoroutine(_flashRoutine);
                _flashRoutine = null;
            }

            if (_edgeFlash != null)
                SetAlpha(_edgeFlash, 0f);
        }

        // ----------------------------------------------------------------
        // Вспомогательный метод — без аллокации
        // ----------------------------------------------------------------

        private static void SetAlpha(Graphic graphic, float alpha)
        {
            var c = graphic.color;
            c.a   = alpha;
            graphic.color = c;
        }
    }
}
