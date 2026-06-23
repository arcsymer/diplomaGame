using System.Collections;
using System.Collections.Generic;
using DiplomaGame.Runtime.Combat;
using DiplomaGame.Runtime.Core;
using DiplomaGame.Runtime.Selection;
using DiplomaGame.Runtime.Units;
using UnityEngine;
using UnityEngine.UI;

namespace DiplomaGame.Runtime.UI
{
    /// <summary>
    /// Система HP-баров над юнитами в RTS-режиме (Circle-18).
    ///
    /// Архитектура:
    ///   • Screen Space Overlay Canvas «HealthBarsCanvas» живёт отдельно от GameHUD.
    ///     Виджеты в нём — Image-полоски, позиция обновляется каждый кадр через
    ///     Camera.WorldToScreenPoint (нет world-space Billboards, нет LateUpdate на каждый виджет).
    ///   • Пул виджетов UnitHealthBarWidget: pre-allocated, растёт при нехватке.
    ///   • Поллинг через корутину (WaitForSeconds) — не Update, нет боксинга.
    ///   • Горячий путь позиционирования (LateUpdate) — только для видимых баров.
    ///     Итерируем по _activeBars: List(T), нет LINQ, нет аллокаций.
    ///
    /// Политика видимости (HealthBarLogic.ShouldShowBar):
    ///   • Показываем только если юнит выделен ИЛИ повреждён (hp &lt; max), и не мёртв.
    ///   • Скрываем все бары в TPS-режиме.
    ///   • Скрываем бар если юнит ушёл за экран (frustum cull по viewport).
    ///
    /// Требования к производительности (ADR-019):
    ///   • Нет аллокаций в Update/LateUpdate (буферы предаллоцированы в Awake).
    ///   • Pool grow — только при нехватке (редко), не в горячем пути.
    ///   • Poll через WaitForSeconds(0.1s) — видимость/размер пула пересчитываются
    ///     не каждый кадр; позиционирование обновляется каждый кадр (LateUpdate).
    /// </summary>
    public sealed class UnitHealthBarSystem : MonoBehaviour
    {
        // ----------------------------------------------------------------
        // Сериализованные поля (проставляются через Forge)
        // ----------------------------------------------------------------

        [Header("Зависимости сцены")]
        [Tooltip("GameModeController — отслеживаем переключение RTS/TPS.")]
        [SerializeField] private GameModeController _modeController;

        [Tooltip("SelectionSystem — проверяем, выделен ли юнит.")]
        [SerializeField] private SelectionSystem _selectionSystem;

        [Header("Canvas и пул")]
        [Tooltip("Screen Space Overlay Canvas для баров (HealthBarsCanvas).")]
        [SerializeField] private Canvas _barCanvas;

        [Tooltip("Префаб виджета одного HP-бара. Должен содержать UnitHealthBarWidget.")]
        [SerializeField] private UnitHealthBarWidget _barWidgetPrefab;

        [Header("Параметры пула")]
        [Tooltip("Начальный размер пула (число заранее созданных виджетов).")]
        [SerializeField] private int _initialPoolSize = 48;

        [Header("Параметры позиционирования")]
        [Tooltip("Вертикальный офсет над юнитом (метры в мировых единицах). " +
                 "Camera.WorldToScreenPoint переводит эту точку в экран.")]
        [SerializeField] private float _worldHeadOffset = 2.2f;

        [Tooltip("Отступ от края viewport для culling (0 = точно по границе экрана).")]
        [SerializeField] private float _cullMargin = 0.02f;

        [Header("Тайминги")]
        [Tooltip("Интервал пересчёта видимости и привязки виджетов к юнитам (секунды).")]
        [SerializeField] private float _pollInterval = 0.1f;

        // ----------------------------------------------------------------
        // Цвета рамок по фракции
        // ----------------------------------------------------------------

        [Header("Цвета рамок")]
        [Tooltip("Цвет рамки для юнитов фракции Player.")]
        [SerializeField] private Color _playerBorderColor = new Color(0.2f, 0.5f, 1.0f, 0.8f);

        [Tooltip("Цвет рамки для юнитов фракции Enemy.")]
        [SerializeField] private Color _enemyBorderColor  = new Color(1.0f, 0.2f, 0.2f, 0.8f);

        // ----------------------------------------------------------------
        // Пул
        // ----------------------------------------------------------------

        // Все созданные виджеты
        private readonly List<UnitHealthBarWidget> _pool       = new List<UnitHealthBarWidget>(64);
        // Свободные виджеты
        private readonly Stack<UnitHealthBarWidget> _freeStack  = new Stack<UnitHealthBarWidget>(64);

        // ----------------------------------------------------------------
        // Активные бары: unit → widget
        // ----------------------------------------------------------------

        // Используем параллельные списки вместо Dictionary, чтобы итерация LateUpdate
        // была cache-friendly и не генерировала KeyValuePair boxing.
        private readonly List<Unit>                 _activeUnits   = new List<Unit>(48);
        private readonly List<UnitHealthBarWidget>  _activeWidgets = new List<UnitHealthBarWidget>(48);

        // ----------------------------------------------------------------
        // Буферы (предаллоцированы в Awake, повторно используются)
        // ----------------------------------------------------------------

        // Буфер GetAllUnits (UnitRegistry.AllUnits итерируется напрямую — нет копии)
        // Буфер для юнитов, которых надо убрать из _activeBars (poll-шаг)
        private readonly List<int> _indicesToRemove = new List<int>(32);

        // ----------------------------------------------------------------
        // Состояние
        // ----------------------------------------------------------------

        private bool      _isRtsMode;
        private Coroutine _pollRoutine;
        private Camera    _cachedCamera;

        // ----------------------------------------------------------------
        // Internal — для тестов
        // ----------------------------------------------------------------

        internal int PoolSize       => _pool.Count;
        internal int ActiveBarCount => _activeUnits.Count;
        internal bool IsRtsMode     => _isRtsMode;

        internal void InitForTest(
            GameModeController mode,
            SelectionSystem selection,
            Canvas canvas,
            UnitHealthBarWidget prefab)
        {
            _modeController  = mode;
            _selectionSystem = selection;
            _barCanvas       = canvas;
            _barWidgetPrefab = prefab;
        }

        // ----------------------------------------------------------------
        // Unity lifecycle
        // ----------------------------------------------------------------

        private void Awake()
        {
            // Предаллоцируем пул
            for (int i = 0; i < _initialPoolSize; i++)
                _freeStack.Push(CreateWidget());
        }

        private void OnEnable()
        {
            if (_modeController != null)
            {
                _modeController.ModeChanged += OnModeChanged;
                _isRtsMode = _modeController.CurrentMode == GameMode.Rts;
            }

            _pollRoutine = StartCoroutine(PollRoutine());
        }

        private void OnDisable()
        {
            if (_modeController != null)
                _modeController.ModeChanged -= OnModeChanged;

            if (_pollRoutine != null)
            {
                StopCoroutine(_pollRoutine);
                _pollRoutine = null;
            }

            // Скрываем все активные бары (не разрушаем — это пул)
            ReturnAllToPool();
        }

        private void LateUpdate()
        {
            // В TPS-режиме все бары скрыты — нечего обновлять
            if (!_isRtsMode) return;

            Camera cam = GetRtsCamera();
            if (cam == null) return;

            // Обновляем позиции всех активных баров
            // Нет аллокаций: List<T>.Count, индексатор — нет боксинга
            for (int i = 0; i < _activeUnits.Count; i++)
            {
                Unit unit = _activeUnits[i];

                // Санити-чек: юнит мог деструктиться между поллами
                if (unit == null)
                {
                    _activeWidgets[i].SetVisible(false);
                    continue;
                }

                Health health = unit.CachedHealth;
                if (health == null || health.IsDead)
                {
                    _activeWidgets[i].SetVisible(false);
                    continue;
                }

                float fraction = health.Fraction;
                bool selected  = IsUnitSelected(unit);

                bool shouldShow = HealthBarLogic.ShouldShowBar(selected, fraction, health.IsDead);
                if (!shouldShow)
                {
                    _activeWidgets[i].SetVisible(false);
                    continue;
                }

                // Frustum cull
                Vector3 worldHead    = unit.transform.position + Vector3.up * _worldHeadOffset;
                Vector3 viewportPos  = cam.WorldToViewportPoint(worldHead);
                if (!HealthBarLogic.IsOnScreen(viewportPos, _cullMargin))
                {
                    _activeWidgets[i].SetVisible(false);
                    continue;
                }

                // Обновляем fill и позицию
                UnitHealthBarWidget widget = _activeWidgets[i];
                widget.SetFill(
                    HealthBarLogic.GetFillAmount(fraction),
                    HealthBarLogic.GetBarColor(fraction));

                // Переводим viewport → экранные координаты
                // Camera.WorldToScreenPoint: x,y — экранные пиксели (y снизу)
                Vector2 screenPos = HealthBarLogic.WorldToScreenBarPosition(cam, worldHead);
                widget.SetScreenPosition(screenPos);

                widget.SetVisible(true);
            }
        }

        // ----------------------------------------------------------------
        // Режим камеры
        // ----------------------------------------------------------------

        private void OnModeChanged(GameMode mode)
        {
            _isRtsMode = mode == GameMode.Rts;

            if (!_isRtsMode)
                HideAllBars();
        }

        // ----------------------------------------------------------------
        // Поллинг: обновление привязки виджетов к юнитам
        // ----------------------------------------------------------------

        private IEnumerator PollRoutine()
        {
            var wait = new WaitForSeconds(_pollInterval);

            while (true)
            {
                PollVisibility();
                yield return wait;
            }
        }

        /// <summary>
        /// Синхронизирует _activeBars с текущим состоянием UnitRegistry:
        /// — добавляет юнитов, которым нужен бар (selected || damaged)
        /// — возвращает в пул юнитов, которым бар больше не нужен или которые умерли
        /// Нет аллокаций: работаем с предаллоцированными буферами.
        /// </summary>
        private void PollVisibility()
        {
            if (!_isRtsMode)
            {
                ReturnAllToPool();
                return;
            }

            var allUnits = UnitRegistry.AllUnits;

            // 1. Убираем из активных бары для юнитов, которых больше нет / умерли
            //    или которым бар больше не нужен.
            _indicesToRemove.Clear();
            for (int i = 0; i < _activeUnits.Count; i++)
            {
                Unit unit = _activeUnits[i];
                if (unit == null)
                {
                    _indicesToRemove.Add(i);
                    continue;
                }

                Health health = unit.CachedHealth;
                if (health == null || health.IsDead)
                {
                    _indicesToRemove.Add(i);
                    continue;
                }

                bool selected  = IsUnitSelected(unit);
                bool shouldBar = HealthBarLogic.ShouldShowBar(selected, health.Fraction, health.IsDead);
                if (!shouldBar)
                    _indicesToRemove.Add(i);
            }

            // Удаляем с конца, чтобы индексы не сдвигались
            for (int i = _indicesToRemove.Count - 1; i >= 0; i--)
            {
                int idx = _indicesToRemove[i];
                _activeWidgets[idx].SetVisible(false);
                _freeStack.Push(_activeWidgets[idx]);
                _activeUnits.RemoveAt(idx);
                _activeWidgets.RemoveAt(idx);
            }

            // 2. Добавляем юнитов, которым нужен бар и у которых ещё нет виджета
            for (int i = 0; i < allUnits.Count; i++)
            {
                Unit unit = allUnits[i];
                if (unit == null) continue;

                Health health = unit.CachedHealth;
                if (health == null || health.IsDead) continue;

                bool selected  = IsUnitSelected(unit);
                bool shouldBar = HealthBarLogic.ShouldShowBar(selected, health.Fraction, health.IsDead);
                if (!shouldBar) continue;

                // Уже в активных?
                if (_activeUnits.Contains(unit)) continue;

                // Берём виджет из пула (или создаём новый при нехватке)
                UnitHealthBarWidget widget = Rent();

                // Проставляем цвет рамки по фракции
                Color borderColor = unit.Faction == Faction.Player
                    ? _playerBorderColor
                    : _enemyBorderColor;
                widget.SetBorderColor(borderColor);

                _activeUnits.Add(unit);
                _activeWidgets.Add(widget);
            }
        }

        // ----------------------------------------------------------------
        // Вспомогательные
        // ----------------------------------------------------------------

        private void HideAllBars()
        {
            for (int i = 0; i < _activeWidgets.Count; i++)
                _activeWidgets[i].SetVisible(false);
        }

        private void ReturnAllToPool()
        {
            for (int i = 0; i < _activeWidgets.Count; i++)
            {
                _activeWidgets[i].SetVisible(false);
                _freeStack.Push(_activeWidgets[i]);
            }
            _activeUnits.Clear();
            _activeWidgets.Clear();
        }

        /// <summary>Берёт виджет из пула. Если пул пуст — создаёт новый (grow, редко).</summary>
        private UnitHealthBarWidget Rent()
        {
            if (_freeStack.Count > 0)
                return _freeStack.Pop();

            // Пул исчерпан — создаём новый (крайне редко, только при > _initialPoolSize одновременных баров)
            var w = CreateWidget();
            _pool.Add(w);
            return w;
        }

        /// <summary>
        /// Создаёт один виджет.
        ///   • Если задан _barWidgetPrefab — Instantiate из него (кастомный визуал художника).
        ///   • Иначе — программно строит полную иерархию: фон + fill + border.
        ///     Это штатный путь: Forge не требует никакого ручного назначения префаба.
        /// Вызывается только в Awake (pre-alloc) и при grow пула — не в горячем пути LateUpdate.
        /// </summary>
        private UnitHealthBarWidget CreateWidget()
        {
            UnitHealthBarWidget widget;

            if (_barWidgetPrefab != null && _barCanvas != null)
            {
                widget = Instantiate(_barWidgetPrefab, _barCanvas.transform, false);
            }
            else
            {
                widget = BuildWidgetProgrammatically();
            }

            widget.SetVisible(false);
            _pool.Add(widget);
            return widget;
        }

        /// <summary>
        /// Программно строит GO-иерархию одного HP-бара и возвращает настроенный виджет.
        /// Иерархия:
        ///   HpBarWidget (RectTransform 40×6, UnitHealthBarWidget)
        ///     Background (Image, тёмный semi-transparent, full-stretch)
        ///     Fill      (Image, Filled/Horizontal/Left, full-stretch — fillAmount управляет шириной)
        ///     Border    (Image, рамка, full-stretch, alpha управляется через SetBorderColor)
        /// Аллокации допустимы — метод вызывается только при создании пула (Awake / grow).
        /// </summary>
        private UnitHealthBarWidget BuildWidgetProgrammatically()
        {
            // ---- Корень ----
            var rootGo = new GameObject("HpBarWidget");
            if (_barCanvas != null)
                rootGo.transform.SetParent(_barCanvas.transform, false);

            var rootRt = rootGo.AddComponent<RectTransform>();
            // Якорь и пивот — левый нижний угол; позиция будет перезаписана SetScreenPosition.
            rootRt.anchorMin = new Vector2(0f, 0f);
            rootRt.anchorMax = new Vector2(0f, 0f);
            rootRt.pivot     = new Vector2(0.5f, 0f);
            rootRt.sizeDelta = new Vector2(40f, 6f);

            // ---- Фон (тёмный, semi-transparent) — полный stretch ----
            var bgGo = new GameObject("Background");
            bgGo.transform.SetParent(rootGo.transform, false);
            var bgImage = bgGo.AddComponent<Image>();
            bgImage.color        = new Color(0f, 0f, 0f, 0.55f);
            bgImage.raycastTarget = false;
            SetFullStretchRect(bgGo);

            // ---- Fill (Filled/Horizontal) — полный stretch ----
            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(rootGo.transform, false);
            var fillImage = fillGo.AddComponent<Image>();
            fillImage.color        = Color.green;   // будет переопределён через SetFill каждый кадр
            fillImage.type         = Image.Type.Filled;
            fillImage.fillMethod   = Image.FillMethod.Horizontal;
            fillImage.fillOrigin   = (int)Image.OriginHorizontal.Left;
            fillImage.fillAmount   = 1f;
            fillImage.raycastTarget = false;
            SetFullStretchRect(fillGo);

            // ---- Border (тонкая рамка) — полный stretch ----
            var borderGo = new GameObject("Border");
            borderGo.transform.SetParent(rootGo.transform, false);
            var borderImage = borderGo.AddComponent<Image>();
            borderImage.color        = new Color(1f, 1f, 1f, 0f); // цвет проставит SetBorderColor
            borderImage.raycastTarget = false;
            SetFullStretchRect(borderGo);

            // ---- Прошиваем UnitHealthBarWidget ----
            var widget = rootGo.AddComponent<UnitHealthBarWidget>();
            widget.InitForTest(fillImage, borderImage);   // инжектируем ссылки (InitForTest — публичный internal, доступен в том же assembly)
            return widget;
        }

        /// <summary>Растягивает RectTransform дочернего GO на всего родителя (offsets = 0).</summary>
        private static void SetFullStretchRect(GameObject go)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// Кэш камеры: Camera.main в RTS-режиме.
        /// Один вызов Camera.main при null-кэше, не каждый кадр.
        /// </summary>
        private Camera GetRtsCamera()
        {
            if (_cachedCamera == null)
                _cachedCamera = Camera.main;
            return _cachedCamera;
        }

        /// <summary>
        /// Проверяет, выделен ли юнит. Нет аллокаций: IReadOnlyList&lt;Unit&gt; — без боксинга.
        /// </summary>
        private bool IsUnitSelected(Unit unit)
        {
            if (_selectionSystem == null) return false;
            var selected = _selectionSystem.Selected;
            // Ручной for-loop вместо Contains/LINQ — нет боксинга
            for (int i = 0; i < selected.Count; i++)
            {
                if (selected[i] == unit) return true;
            }
            return false;
        }
    }
}
