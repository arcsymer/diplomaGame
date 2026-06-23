using System.Collections;
using DiplomaGame.Runtime.Combat;
using DiplomaGame.Runtime.GameFeel;
using UnityEngine;
using UnityEngine.UI;

namespace DiplomaGame.Runtime.UI
{
    /// <summary>
    /// Индикатор урона героя (Circle-21 / Circle-23).
    ///
    /// Circle-21 — full-edge flash:
    ///   Подписывается на <see cref="Health.AnyDamaged"/> (без позиции источника).
    ///   Показывает red full-edge вспышку по периметру экрана, затухающую за ~1 с.
    ///   Является fallback-слоем для урона без источника (AOE, DoT и т.п.).
    ///
    /// Circle-23 — направленный индикатор:
    ///   Подписывается на <see cref="Health.AnyDamagedFrom"/> (с позицией источника).
    ///   Вычисляет угол от camera-forward героя до атакующего в плоскости XZ.
    ///   Вращает <see cref="_directionArrow"/> (Image на кольце HUD) на вычисленный угол,
    ///   показывает её с fade ~1 с, interrupt-restart при повторных попаданиях.
    ///
    /// Оба слоя независимы: при уроне с позицией срабатывают оба.
    /// При уроне без позиции срабатывает только edge flash.
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

        [Tooltip("Transform камеры TPS (camera-forward используется как опорное направление). " +
                 "Если не задан — используется Camera.main.")]
        [SerializeField] private Transform _tpsCameraTransform;

        [Header("UI")]
        [Tooltip("Image с alpha=0 и full-stretch поверх TPS-блока (красный edge flash, C21).")]
        [SerializeField] private Image _edgeFlash;

        [Tooltip("Image стрелки/дуги на кольце HUD (направленный индикатор, C23). " +
                 "RectTransform вращается через eulerAngles.z. " +
                 "Если null — направленный индикатор не показывается (только edge flash).")]
        [SerializeField] private Image _directionArrow;

        [Header("Settings")]
        [Tooltip("GameFeelSettings — содержит параметры длительности и пика альфы.")]
        [SerializeField] private GameFeelSettings _settings;

        // ----------------------------------------------------------------
        // Внутреннее состояние
        // ----------------------------------------------------------------

        private Coroutine _flashRoutine;
        private Coroutine _arrowRoutine;

        // ----------------------------------------------------------------
        // Unity lifecycle
        // ----------------------------------------------------------------

        private void OnEnable()
        {
            Health.AnyDamaged     += OnAnyDamaged;
            Health.AnyDamagedFrom += OnAnyDamagedFrom;
        }

        private void OnDisable()
        {
            Health.AnyDamaged     -= OnAnyDamaged;
            Health.AnyDamagedFrom -= OnAnyDamagedFrom;
            CancelFlash();
            CancelArrow();
        }

        // ----------------------------------------------------------------
        // Обработчики событий (горячий путь — без аллокаций)
        // ----------------------------------------------------------------

        private void OnAnyDamaged(Health health, float amount)
        {
            if (!IsHero(health)) return;
            TriggerFlash();
        }

        private void OnAnyDamagedFrom(Health health, float amount, Vector3 sourcePos)
        {
            if (!IsHero(health)) return;
            TriggerDirectionalArrow(sourcePos);
        }

        // ----------------------------------------------------------------
        // Internal — для PlayMode-тестов и Forge
        // ----------------------------------------------------------------

        /// <summary>Проставляет ссылки для PlayMode-тестов без SerializedObject.</summary>
        internal void InitForTest(
            Health heroHealth,
            Image edgeFlash,
            Image directionArrow,
            GameFeelSettings settings,
            Transform tpsCameraTransform = null)
        {
            _heroHealth          = heroHealth;
            _edgeFlash           = edgeFlash;
            _directionArrow      = directionArrow;
            _settings            = settings;
            _tpsCameraTransform  = tpsCameraTransform;
        }

        /// <summary>Флаг активного edge flash (для тестов).</summary>
        internal bool FlashActive => _flashRoutine != null;

        /// <summary>Флаг активного направленного индикатора (для тестов).</summary>
        internal bool ArrowActive => _arrowRoutine != null;

        // ----------------------------------------------------------------
        // Приватные методы — фильтрация
        // ----------------------------------------------------------------

        private bool IsHero(Health health)
        {
            if (health == null) return false;
            if (_heroHealth == null) return false;
            if (!ReferenceEquals(health, _heroHealth)) return false;
            if (_heroHealth.IsDead) return false;
            return true;
        }

        // ----------------------------------------------------------------
        // Edge flash (Circle-21)
        // ----------------------------------------------------------------

        private void TriggerFlash()
        {
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
        // Направленный индикатор (Circle-23)
        // ----------------------------------------------------------------

        private void TriggerDirectionalArrow(Vector3 sourcePos)
        {
            if (_directionArrow == null) return;

            // Вычисляем опорное направление: camera-forward, спроецированное в XZ
            Vector3 refForward = GetCameraForwardXZ();

            float angleDeg = HeroDamageIndicatorLogic.ComputeIndicatorAngleDegrees(
                refForward,
                _heroHealth.transform.position,
                sourcePos);

            // В uGUI RectTransform.eulerAngles.z = 0 → верх изображения смотрит вверх экрана.
            // Поворачиваем ПРОТИВ часовой стрелки на угол, чтобы стрелка указывала на источник.
            // SignedAngle даёт +90 = справа; uGUI z-rotation положительная = CCW.
            // Чтобы стрелка развернулась вправо при +90 — z = −angleDeg.
            var rt = _directionArrow.rectTransform;
            rt.localEulerAngles = new Vector3(0f, 0f, -angleDeg);

            if (_arrowRoutine != null)
                StopCoroutine(_arrowRoutine);

            _arrowRoutine = StartCoroutine(ArrowRoutine());
        }

        private IEnumerator ArrowRoutine()
        {
            if (_directionArrow == null)
            {
                _arrowRoutine = null;
                yield break;
            }

            float duration  = _settings != null ? _settings.damageIndicatorDuration : 1f;
            float peakAlpha = _settings != null ? _settings.damageIndicatorPeakAlpha : 0.6f;
            duration = Mathf.Max(duration, 0.1f);

            _directionArrow.gameObject.SetActive(true);
            SetAlpha(_directionArrow, peakAlpha);

            float elapsed = 0f;
            while (!HeroDamageIndicatorLogic.IsFadeDone(elapsed, duration))
            {
                elapsed += Time.unscaledDeltaTime;
                float t     = Mathf.Clamp01(elapsed / duration);
                float alpha = HeroDamageIndicatorLogic.ComputeFadeAlpha(t, peakAlpha);
                SetAlpha(_directionArrow, alpha);
                yield return null;
            }

            SetAlpha(_directionArrow, 0f);
            _directionArrow.gameObject.SetActive(false);
            _arrowRoutine = null;
        }

        private void CancelArrow()
        {
            if (_arrowRoutine != null)
            {
                StopCoroutine(_arrowRoutine);
                _arrowRoutine = null;
            }

            if (_directionArrow != null)
            {
                SetAlpha(_directionArrow, 0f);
                _directionArrow.gameObject.SetActive(false);
            }
        }

        // ----------------------------------------------------------------
        // Вспомогательные методы
        // ----------------------------------------------------------------

        /// <summary>
        /// Возвращает нормализованный camera-forward в плоскости XZ.
        /// Использует _tpsCameraTransform если задан; иначе Camera.main.
        /// Гарантированно не аллоцирует в горячем пути.
        /// </summary>
        private Vector3 GetCameraForwardXZ()
        {
            Transform camTransform = _tpsCameraTransform;
            if (camTransform == null && Camera.main != null)
                camTransform = Camera.main.transform;

            if (camTransform != null)
            {
                Vector3 fwd = camTransform.forward;
                fwd.y = 0f;
                if (fwd.sqrMagnitude > 1e-10f)
                    return fwd.normalized;
            }

            // Fallback: если камеры нет — используем transform.forward самого HUD-объекта
            // (практически не встречается, но предотвращает 0-вектор)
            return Vector3.forward;
        }

        private static void SetAlpha(Graphic graphic, float alpha)
        {
            var c = graphic.color;
            c.a   = alpha;
            graphic.color = c;
        }
    }
}
