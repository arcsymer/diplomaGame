using System.Collections;
using DiplomaGame.Runtime.Audio;
using DiplomaGame.Runtime.Buildings;
using DiplomaGame.Runtime.Combat;
using DiplomaGame.Runtime.Units;
using UnityEngine;
using UnityEngine.UI;

namespace DiplomaGame.Runtime.UI
{
    /// <summary>
    /// Система оповещения «под атакой» (Circle-16).
    /// Подписывается на <see cref="Health.AnyDamaged"/>, фильтрует фракцию Player,
    /// дросселирует алерт через <see cref="UnderAttackAlertLogic"/> и в момент триггера:
    /// <list type="number">
    ///   <item>Показывает пульсирующий красный маркер на миникарте.</item>
    ///   <item>Воспроизводит вспышку-виньетку по краям экрана.</item>
    ///   <item>Воспроизводит UI-звук через <see cref="AudioManager"/>.</item>
    /// </list>
    /// Авто-скрытие через ~3 с. Безопасен при нескольких вызовах подряд (дросселирование).
    /// </summary>
    public sealed class UnderAttackAlert : MonoBehaviour
    {
        // ----------------------------------------------------------------
        // Сериализованные поля (проставляются через Forge)
        // ----------------------------------------------------------------

        [Header("Minimap")]
        [Tooltip("Image — красный маркер угрозы, дочерний элемент RawImage миникарты.")]
        [SerializeField] private RectTransform _minimapMarker;

        [Header("HUD")]
        [Tooltip("Image с alpha=0 и full-stretch поверх HUD-Canvas (виньетка по краям).")]
        [SerializeField] private Image _edgeVignette;

        [Header("Minimap Camera Reference (для world→minimap проекции)")]
        [Tooltip("Ортографическая камера миникарты (задаётся через Forge).")]
        [SerializeField] private Camera _minimapCamera;

        [Tooltip("RawImage миникарты (для получения размера rect).")]
        [SerializeField] private RawImage _minimapDisplay;

        // ----------------------------------------------------------------
        // Внутреннее состояние
        // ----------------------------------------------------------------

        private float   _lastTriggerTime = float.NegativeInfinity;
        private bool    _alertActive;
        private Vector3 _lastThreatWorldPos;

        // Корутины анимаций
        private Coroutine _markerPulseRoutine;
        private Coroutine _vignetteRoutine;

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
            StopAllAlertCoroutines();
        }

        private void Update()
        {
            if (!_alertActive) return;

            if (UnderAttackAlertLogic.ShouldClear(_lastTriggerTime, Time.time))
                HideAlert();
        }

        // ----------------------------------------------------------------
        // Internal — для тестов и Forge
        // ----------------------------------------------------------------

        /// <summary>Флаг активного состояния алерта (для PlayMode-тестов).</summary>
        internal bool AlertActive => _alertActive;

        /// <summary>Последнее время триггера (для PlayMode-тестов).</summary>
        internal float LastTriggerTime => _lastTriggerTime;

        /// <summary>Последняя мировая позиция угрозы (для PlayMode-тестов).</summary>
        internal Vector3 LastThreatWorldPos => _lastThreatWorldPos;

        /// <summary>
        /// Инициализирует ссылки без SerializedObject — для PlayMode-тестов.
        /// </summary>
        internal void InitForTest(
            RectTransform minimapMarker,
            Image edgeVignette,
            Camera minimapCamera,
            RawImage minimapDisplay)
        {
            _minimapMarker  = minimapMarker;
            _edgeVignette   = edgeVignette;
            _minimapCamera  = minimapCamera;
            _minimapDisplay = minimapDisplay;
        }

        // ----------------------------------------------------------------
        // Обработчик события (горячий путь — без аллокаций)
        // ----------------------------------------------------------------

        private void OnAnyDamaged(Health health, float amount)
        {
            if (health == null) return;

            // Фильтр: только юниты/здания/герой фракции Player
            Faction? faction = GetFaction(health);
            if (faction == null || faction.Value != Faction.Player) return;

            // Дросселирование: не чаще чем раз в ThrottleWindow секунд
            if (!UnderAttackAlertLogic.ShouldTrigger(_lastTriggerTime, Time.time)) return;

            _lastTriggerTime    = Time.time;
            _lastThreatWorldPos = health.transform.position;

            TriggerAlert();
        }

        // ----------------------------------------------------------------
        // Приватные методы
        // ----------------------------------------------------------------

        /// <summary>
        /// Извлекает фракцию из Health-компонента.
        /// Порядок: Unit → Building → HeroController (всегда Player).
        /// Возвращает null если тип объекта неизвестен.
        /// Не выполняет аллокаций: GetComponent кэшируется Unity внутри.
        /// </summary>
        private static Faction? GetFaction(Health health)
        {
            var unit = health.GetComponent<Unit>();
            if (unit != null) return unit.Faction;

            var building = health.GetComponent<Building>();
            if (building != null) return building.Faction;

            // Герой не имеет отдельного Faction-поля, но всегда Player
            var hero = health.GetComponent<DiplomaGame.Runtime.Hero.HeroController>();
            if (hero != null) return Faction.Player;

            return null;
        }

        private void TriggerAlert()
        {
            _alertActive = true;

            UpdateMinimapMarker();
            StartVignette();
            PlayAudioCue();
        }

        /// <summary>
        /// Пересчитывает позицию маркера на миникарте из мировой позиции угрозы
        /// и (пере)запускает корутину пульсации.
        /// </summary>
        private void UpdateMinimapMarker()
        {
            if (_minimapMarker == null) return;

            if (_minimapCamera != null && _minimapDisplay != null)
            {
                // Проецируем мировую позицию в viewport камеры миникарты (0..1)
                Vector3 viewportPos = _minimapCamera.WorldToViewportPoint(_lastThreatWorldPos);

                Rect displayRect = _minimapDisplay.rectTransform.rect;
                float w = displayRect.width;
                float h = displayRect.height;

                // anchoredPosition относительно pivot=0.5 RawImage
                _minimapMarker.anchorMin        = new Vector2(0.5f, 0.5f);
                _minimapMarker.anchorMax        = new Vector2(0.5f, 0.5f);
                _minimapMarker.pivot            = new Vector2(0.5f, 0.5f);
                _minimapMarker.anchoredPosition = new Vector2(
                    (viewportPos.x - 0.5f) * w,
                    (viewportPos.y - 0.5f) * h);
            }

            _minimapMarker.gameObject.SetActive(true);

            // (Пере)запускаем пульс
            if (_markerPulseRoutine != null)
                StopCoroutine(_markerPulseRoutine);

            _markerPulseRoutine = StartCoroutine(MarkerPulseRoutine());
        }

        /// <summary>
        /// Пульсация маркера: alpha 1 ↔ 0.25 с периодом 0.5 с,
        /// прерывается при HideAlert (корутина остановлена вручную).
        /// </summary>
        private IEnumerator MarkerPulseRoutine()
        {
            var img = _minimapMarker != null ? _minimapMarker.GetComponent<Image>() : null;
            if (img == null) yield break;

            const float Half = 0.25f;

            while (true)
            {
                // 1 → 0.25
                float elapsed = 0f;
                while (elapsed < Half)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / Half);
                    SetAlpha(img, Mathf.Lerp(1f, 0.25f, t));
                    yield return null;
                }

                // 0.25 → 1
                elapsed = 0f;
                while (elapsed < Half)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / Half);
                    SetAlpha(img, Mathf.Lerp(0.25f, 1f, t));
                    yield return null;
                }
            }
        }

        /// <summary>
        /// Однократная вспышка-виньетка: alpha 0 → 0.55 → 0 за 0.3 с.
        /// </summary>
        private void StartVignette()
        {
            if (_edgeVignette == null) return;

            if (_vignetteRoutine != null)
                StopCoroutine(_vignetteRoutine);

            _vignetteRoutine = StartCoroutine(VignetteFlashRoutine());
        }

        private IEnumerator VignetteFlashRoutine()
        {
            if (_edgeVignette == null) yield break;

            _edgeVignette.gameObject.SetActive(true);

            const float InDur  = 0.12f;
            const float OutDur = 0.18f;

            // Fade in
            float elapsed = 0f;
            while (elapsed < InDur)
            {
                elapsed += Time.deltaTime;
                SetAlpha(_edgeVignette, Mathf.Lerp(0f, 0.55f, elapsed / InDur));
                yield return null;
            }

            // Fade out
            elapsed = 0f;
            while (elapsed < OutDur)
            {
                elapsed += Time.deltaTime;
                SetAlpha(_edgeVignette, Mathf.Lerp(0.55f, 0f, elapsed / OutDur));
                yield return null;
            }

            SetAlpha(_edgeVignette, 0f);
            _vignetteRoutine = null;
        }

        /// <summary>
        /// Воспроизводит UI-звук через AudioManager.
        /// Использует PlayUiError() (bong_001.ogg — тревожный, CC0 Kenney).
        /// TODO (ADR-009 zero-budget): если bong_001.ogg не подходит по ощущению —
        /// заменить на специальный CC0 алерт-клип.
        /// Кандидаты: Kenney "Interface Sounds" alert_high.ogg / Freesound CC0 radar beep.
        /// Хук уже готов — достаточно проставить новый клип в AudioManager._uiError[].
        /// </summary>
        private static void PlayAudioCue()
        {
            var mgr = AudioManager.Instance;
            if (mgr == null) return;
            mgr.PlayUiError();
        }

        private void HideAlert()
        {
            _alertActive = false;

            StopAllAlertCoroutines();

            if (_minimapMarker != null)
                _minimapMarker.gameObject.SetActive(false);

            if (_edgeVignette != null)
                SetAlpha(_edgeVignette, 0f);
        }

        private void StopAllAlertCoroutines()
        {
            if (_markerPulseRoutine != null)
            {
                StopCoroutine(_markerPulseRoutine);
                _markerPulseRoutine = null;
            }

            if (_vignetteRoutine != null)
            {
                StopCoroutine(_vignetteRoutine);
                _vignetteRoutine = null;
            }
        }

        // ----------------------------------------------------------------
        // Вспомогательный метод — без аллокации (изменяем struct Color напрямую)
        // ----------------------------------------------------------------

        private static void SetAlpha(Graphic graphic, float alpha)
        {
            var c = graphic.color;
            c.a   = alpha;
            graphic.color = c;
        }
    }
}
