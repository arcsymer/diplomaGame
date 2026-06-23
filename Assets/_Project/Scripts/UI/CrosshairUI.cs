using System.Collections;
using DiplomaGame.Runtime.GameFeel;
using DiplomaGame.Runtime.Hero;
using UnityEngine;
using UnityEngine.UI;

namespace DiplomaGame.Runtime.UI
{
    /// <summary>
    /// uGUI прицел TPS.
    /// Живёт в TPS-блоке HudController — активируется/деактивируется вместе с блоком,
    /// дополнительной логики переключения не требует.
    /// Визуал строится из 4 дочерних Image-полосок через Forge.
    ///
    /// Circle-20: подписывается на HeroShooter.ShotFired и запускает
    /// interrupt-restartable хитмаркерную анимацию (цвет + масштаб).
    /// </summary>
    public sealed class CrosshairUI : MonoBehaviour
    {
        // ----------------------------------------------------------------
        // Сериализованные ссылки (прошиваются через Forge)
        // ----------------------------------------------------------------

        [SerializeField] private HeroShooter      _shooter;
        [SerializeField] private GameFeelSettings _settings;

        // ----------------------------------------------------------------
        // Кэш полосок (Awake)
        // ----------------------------------------------------------------

        private Image[]   _strips;      // 4 дочерних Image (Left/Right/Up/Down)
        private Color     _baseColor = Color.white;

        // ----------------------------------------------------------------
        // Состояние анимации
        // ----------------------------------------------------------------

        private Coroutine _hitmarkerRoutine;

        // ----------------------------------------------------------------
        // Unity lifecycle
        // ----------------------------------------------------------------

        private void Awake()
        {
            CacheStrips();
        }

        private void OnEnable()
        {
            if (_shooter != null)
                _shooter.ShotFired += OnShotFired;
        }

        private void OnDisable()
        {
            if (_shooter != null)
                _shooter.ShotFired -= OnShotFired;

            // Если анимация прервана деактивацией — восстанавливаем цвет немедленно
            if (_hitmarkerRoutine != null)
            {
                StopCoroutine(_hitmarkerRoutine);
                _hitmarkerRoutine = null;
                ResetStrips();
            }
        }

        // ----------------------------------------------------------------
        // Событие выстрела
        // ----------------------------------------------------------------

        private void OnShotFired(Vector3 origin, Vector3 end, bool hit)
        {
            if (_settings == null) return;

            // Interrupt-restart: остановить предыдущую итерацию
            if (_hitmarkerRoutine != null)
                StopCoroutine(_hitmarkerRoutine);

            _hitmarkerRoutine = StartCoroutine(HitmarkerRoutine(hit));
        }

        // ----------------------------------------------------------------
        // Корутина анимации
        // ----------------------------------------------------------------

        private IEnumerator HitmarkerRoutine(bool hit)
        {
            // Вычисляем целевые значения через чистую логику
            CrosshairHitmarkerLogic.Resolve(
                hit,
                _baseColor,
                _settings.hitmarkerColorHit,
                _settings.hitmarkerExpandScale,
                _settings.hitmarkerMissScale,
                out Color targetColor,
                out float peakScale);

            float duration = Mathf.Max(_settings.hitmarkerDuration, 0.001f);
            float elapsed  = 0f;

            while (elapsed < duration)
            {
                float t = elapsed / duration;

                Color  currentColor = CrosshairHitmarkerLogic.PingPongColor(t, _baseColor, targetColor);
                float  currentScale = CrosshairHitmarkerLogic.PingPongScale(t, peakScale);

                ApplyToStrips(currentColor, currentScale);

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            ResetStrips();
            _hitmarkerRoutine = null;
        }

        // ----------------------------------------------------------------
        // Вспомогательные методы
        // ----------------------------------------------------------------

        private void CacheStrips()
        {
            // Собираем все Image на прямых дочерних GO (Left/Right/Up/Down)
            int childCount = transform.childCount;
            var list = new System.Collections.Generic.List<Image>(childCount);
            for (int i = 0; i < childCount; i++)
            {
                var img = transform.GetChild(i).GetComponent<Image>();
                if (img != null)
                    list.Add(img);
            }
            _strips = list.ToArray();

            // Снимаем базовый цвет с первой полоски (или белый по умолчанию)
            if (_strips.Length > 0)
                _baseColor = _strips[0].color;
            else
                _baseColor = Color.white;
        }

        private void ApplyToStrips(Color color, float uniformScale)
        {
            if (_strips == null) return;
            for (int i = 0; i < _strips.Length; i++)
            {
                if (_strips[i] == null) continue;
                _strips[i].color                   = color;
                _strips[i].transform.localScale    = new Vector3(uniformScale, uniformScale, 1f);
            }
        }

        private void ResetStrips()
        {
            if (_strips == null) return;
            for (int i = 0; i < _strips.Length; i++)
            {
                if (_strips[i] == null) continue;
                _strips[i].color                = _baseColor;
                _strips[i].transform.localScale = Vector3.one;
            }
        }

        // ----------------------------------------------------------------
        // Internal (для тестов и Forge)
        // ----------------------------------------------------------------

        /// <summary>Задаёт ссылки для тестов без вызова Awake.</summary>
        internal void InitForTest(HeroShooter shooter, GameFeelSettings settings)
        {
            _shooter  = shooter;
            _settings = settings;
        }

        /// <summary>Количество закэшированных полосок (для Forge-валидации).</summary>
        internal int StripCount => _strips != null ? _strips.Length : 0;
    }
}
