using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DiplomaGame.Runtime.UI
{
    /// <summary>
    /// Singleton-система тултипов. Живёт на отдельном TooltipCanvas (sortingOrder=100, ScreenSpaceOverlay).
    /// State machine: Idle → Pending (таймер 0.35с) → Visible.
    /// MouseDown → Hide (мгновенно).
    /// Fade-in alpha 0→1 за 0.12с через корутину (PrimeTween не используется — его нет в проекте).
    /// </summary>
    public sealed class TooltipSystem : MonoBehaviour
    {
        // ----------------------------------------------------------------
        // Singleton
        // ----------------------------------------------------------------

        public static TooltipSystem Instance { get; private set; }

        // ----------------------------------------------------------------
        // Константы
        // ----------------------------------------------------------------

        private const float ShowDelay  = 0.35f;
        private const float FadeInTime = 0.12f;

        // ----------------------------------------------------------------
        // Serialized
        // ----------------------------------------------------------------

        [SerializeField] private TooltipView tooltipView;

        // ----------------------------------------------------------------
        // Состояние
        // ----------------------------------------------------------------

        private enum State { Idle, Pending, Visible }

        private State              _state        = State.Idle;
        private ITooltipProvider   _provider;
        private float              _pendingTimer;
        private Coroutine          _fadeCoroutine;
        private RectTransform      _viewRect;
        private Canvas             _canvas;

        // ----------------------------------------------------------------
        // Публичный API — удобный геттер состояния
        // ----------------------------------------------------------------

        public bool IsVisible => _state == State.Visible;

        // ----------------------------------------------------------------
        // Unity lifecycle
        // ----------------------------------------------------------------

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[TooltipSystem] Дублирующий экземпляр уничтожен.", this);
                Destroy(gameObject);
                return;
            }

            Instance = this;

            _canvas = GetComponent<Canvas>();
            if (tooltipView != null)
                _viewRect = tooltipView.GetComponent<RectTransform>();

            if (tooltipView != null)
            {
                tooltipView.CanvasGroup.alpha          = 0f;
                tooltipView.CanvasGroup.interactable   = false;
                tooltipView.CanvasGroup.blocksRaycasts = false;
                tooltipView.gameObject.SetActive(false);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        private void Update()
        {
            if (_state != State.Pending) return;

            _pendingTimer -= Time.unscaledDeltaTime;
            if (_pendingTimer <= 0f)
                ShowNow();
        }

        // ----------------------------------------------------------------
        // Публичный API
        // ----------------------------------------------------------------

        /// <summary>
        /// Запрашивает показ тултипа с задержкой 0.35с.
        /// Если уже виден — выполняет SwitchTo (мгновенная замена).
        /// </summary>
        public void RequestShow(ITooltipProvider provider, Vector2 screenPos)
        {
            if (_state == State.Visible)
            {
                SwitchTo(provider, screenPos);
                return;
            }

            _provider     = provider;
            _state        = State.Pending;
            _pendingTimer = ShowDelay;

            UpdatePositionInternal(screenPos);
        }

        /// <summary>Обновляет позицию тултипа (вызывается из OnPointerMove).</summary>
        public void UpdatePosition(Vector2 screenPos)
        {
            if (_state == State.Idle) return;
            UpdatePositionInternal(screenPos);
        }

        /// <summary>
        /// Мгновенно скрывает тултип и сбрасывает таймер ожидания.
        /// </summary>
        public void Hide()
        {
            _state    = State.Idle;
            _provider = null;

            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }

            if (tooltipView != null)
            {
                tooltipView.CanvasGroup.alpha = 0f;
                tooltipView.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Мгновенно заменяет содержимое без задержки (переход между соседними кнопками).
        /// </summary>
        public void SwitchTo(ITooltipProvider provider, Vector2 screenPos)
        {
            // Если провайдер возвращает пустые данные — скрываем
            if (provider == null || string.IsNullOrEmpty(provider.GetTooltipData().Title))
            {
                Hide();
                return;
            }

            _state    = State.Visible;
            _provider = provider;

            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
                _fadeCoroutine = null;
            }

            ShowView(provider);
            UpdatePositionInternal(screenPos);
        }

        // ----------------------------------------------------------------
        // Приватные методы
        // ----------------------------------------------------------------

        private void ShowNow()
        {
            if (_provider == null)
            {
                Hide();
                return;
            }

            // Если провайдер возвращает пустые данные (нет заголовка) — не показываем тултип
            var data = _provider.GetTooltipData();
            if (string.IsNullOrEmpty(data.Title))
            {
                Hide();
                return;
            }

            _state = State.Visible;
            ShowView(_provider);
        }

        private void ShowView(ITooltipProvider provider)
        {
            if (tooltipView == null) return;

            tooltipView.gameObject.SetActive(true);
            tooltipView.Populate(provider.GetTooltipData());

            if (_fadeCoroutine != null)
                StopCoroutine(_fadeCoroutine);

            _fadeCoroutine = StartCoroutine(FadeInRoutine());
        }

        private IEnumerator FadeInRoutine()
        {
            var cg = tooltipView.CanvasGroup;
            cg.alpha = 0f;

            float elapsed = 0f;
            while (elapsed < FadeInTime)
            {
                elapsed += Time.unscaledDeltaTime;
                cg.alpha = Mathf.Clamp01(elapsed / FadeInTime);
                yield return null;
            }

            cg.alpha       = 1f;
            _fadeCoroutine = null;
        }

        private void UpdatePositionInternal(Vector2 screenPos)
        {
            if (_viewRect == null) return;

            // rect.size — в reference-единицах CanvasScaler; кламп считаем в реальных
            // пикселях экрана, потом конвертируем обратно через scaleFactor.
            float scale = _canvas != null && _canvas.scaleFactor > 0f ? _canvas.scaleFactor : 1f;

            var size = _viewRect.rect.size;
            if (size.x <= 0f) size.x = TooltipLogic.TooltipMaxWidth;
            if (size.y <= 0f) size.y = 80f;

            Vector2 clampedPx = TooltipLogic.ClampToScreen(screenPos, size * scale);
            _viewRect.anchoredPosition = clampedPx / scale;
        }
    }
}
