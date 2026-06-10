using System;
using System.Collections.Generic;
using DiplomaGame.Runtime.Buildings;
using DiplomaGame.Runtime.Core;
using DiplomaGame.Runtime.Units;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DiplomaGame.Runtime.Selection
{
    /// <summary>
    /// Управляет выделением юнитов: клик, рамка, контрол-группы (Ctrl+1..5 / 1..5).
    /// M5: добавлено выделение зданий — клик по Building (Player) заполняет SelectedBuilding.
    /// Рисует рамку выделения через OnGUI.
    /// Активна только в RTS-режиме — контрол-группы опрашиваются только при RTS.
    /// </summary>
    public sealed class SelectionSystem : MonoBehaviour
    {
        [SerializeField] private UnityEngine.InputSystem.InputActionAsset actions;
        [SerializeField] private Camera overrideCamera;
        [SerializeField] private GameModeController modeController;

        // ----------------------------------------------------------------
        // Публичный API
        // ----------------------------------------------------------------

        public IReadOnlyList<Unit> Selected => _selected;

        /// <summary>Текущее выделенное здание игрока; null если выделены юниты или клик был пустым.</summary>
        public Building SelectedBuilding { get; private set; }

        /// <summary>Вызывается после каждого изменения состава выделения.</summary>
        public event Action SelectionChanged;

        /// <summary>Вызывается при выделении здания. Параметр — выбранное здание.</summary>
        public event Action<Building> BuildingSelected;

        // ----------------------------------------------------------------
        // Приватные поля — кэш
        // ----------------------------------------------------------------

        private Camera _camera;

        // Действия Input System
        private InputAction _selectAction;

        // Выделение
        private readonly List<Unit> _selected      = new List<Unit>(32);
        private Vector2             _mouseDownPos;
        private bool                _isDragging;

        // Контрол-группы: ключ 1..5
        private readonly Dictionary<int, List<Unit>> _controlGroups = new Dictionary<int, List<Unit>>(5);

        // Рамка (OnGUI — нет аллокаций)
        private static Texture2D _boxTexture;

        // Буфер для GetPlayerUnits (без аллокаций)
        private readonly List<Unit> _unitBuffer = new List<Unit>(64);

        // ----------------------------------------------------------------
        // Unity lifecycle
        // ----------------------------------------------------------------

        private void Awake()
        {
            _camera = overrideCamera != null ? overrideCamera : Camera.main;

            // Инициализируем слоты контрол-групп
            for (int i = 1; i <= 5; i++)
                _controlGroups[i] = new List<Unit>(16);

            // Создаём текстуру для рамки (кэшируем)
            EnsureBoxTexture();
        }

        private void OnEnable()
        {
            if (actions == null) return;

            var rtsMap = actions.FindActionMap("RTS");
            if (rtsMap == null) return;

            _selectAction = rtsMap.FindAction("Select");
            if (_selectAction != null)
            {
                _selectAction.started  += OnSelectStarted;
                _selectAction.canceled += OnSelectCanceled;
            }
        }

        private void OnDisable()
        {
            if (_selectAction != null)
            {
                _selectAction.started  -= OnSelectStarted;
                _selectAction.canceled -= OnSelectCanceled;
            }
        }

        private void Update()
        {
            // Кэш камеры — мог смениться (Camera.main ленивый)
            if (_camera == null)
                _camera = overrideCamera != null ? overrideCamera : Camera.main;

            HandleControlGroups();
        }

        // ----------------------------------------------------------------
        // Рамка выделения (OnGUI)
        // ----------------------------------------------------------------

        private void OnGUI()
        {
            if (!_isDragging) return;

            var current = Mouse.current != null ? (Vector2)Mouse.current.position.ReadValue() : _mouseDownPos;
            var screenRect = SelectionLogic.GetScreenRect(_mouseDownPos, current);

            // GUI.DrawTexture работает в GUI-координатах (y=0 сверху),
            // а _mouseDownPos/current — в Input-координатах (y=0 снизу).
            // Пересчитываем в GUI-пространство.
            float guiY    = Screen.height - screenRect.yMax;
            var   guiRect = new Rect(screenRect.x, guiY, screenRect.width, screenRect.height);

            var color = new Color(0.2f, 0.8f, 0.2f, 0.25f);
            var old   = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(guiRect, _boxTexture);
            GUI.color = old;

            // Рамка (контур)
            GUI.color = new Color(0.2f, 0.8f, 0.2f, 0.8f);
            GUI.DrawTexture(new Rect(guiRect.x, guiRect.y, guiRect.width, 1f), _boxTexture);
            GUI.DrawTexture(new Rect(guiRect.x, guiRect.yMax - 1f, guiRect.width, 1f), _boxTexture);
            GUI.DrawTexture(new Rect(guiRect.x, guiRect.y, 1f, guiRect.height), _boxTexture);
            GUI.DrawTexture(new Rect(guiRect.xMax - 1f, guiRect.y, 1f, guiRect.height), _boxTexture);
            GUI.color = old;
        }

        // ----------------------------------------------------------------
        // Обработка Input Actions
        // ----------------------------------------------------------------

        private void OnSelectStarted(InputAction.CallbackContext ctx)
        {
            if (Mouse.current == null) return;
            _mouseDownPos = Mouse.current.position.ReadValue();
            _isDragging   = true;
        }

        private void OnSelectCanceled(InputAction.CallbackContext ctx)
        {
            if (!_isDragging) return;
            _isDragging = false;

            if (Mouse.current == null) return;
            Vector2 mouseUpPos = Mouse.current.position.ReadValue();

            bool shift = Keyboard.current != null && Keyboard.current.shiftKey.isPressed;

            if (SelectionLogic.IsClick(_mouseDownPos, mouseUpPos))
                HandleClickSelection(mouseUpPos, shift);
            else
                HandleBoxSelection(_mouseDownPos, mouseUpPos, shift);
        }

        // ----------------------------------------------------------------
        // Клик / рамка
        // ----------------------------------------------------------------

        private void HandleClickSelection(Vector2 screenPos, bool additive)
        {
            if (_camera == null) return;

            var ray = _camera.ScreenPointToRay(new Vector3(screenPos.x, screenPos.y, 0f));
            Unit     hitUnit     = null;
            Building hitBuilding = null;

            if (Physics.Raycast(ray, out RaycastHit info, 1000f))
            {
                hitUnit     = info.collider.GetComponentInParent<Unit>();
                hitBuilding = info.collider.GetComponentInParent<Building>();
            }

            if (hitUnit != null && hitUnit.Faction == Faction.Player)
            {
                // Клик по юниту сбрасывает выделение здания
                SelectedBuilding = null;

                if (additive)
                    AddToSelection(hitUnit);
                else
                    SetSelection(hitUnit);
            }
            else if (hitBuilding != null && hitBuilding.Faction == Faction.Player)
            {
                // Клик по зданию игрока
                ClearSelection();
                SelectedBuilding = hitBuilding;
                BuildingSelected?.Invoke(hitBuilding);
                NotifyChanged();
            }
            else if (!additive)
            {
                SelectedBuilding = null;
                ClearSelection();
                NotifyChanged();
            }
        }

        private void HandleBoxSelection(Vector2 down, Vector2 up, bool additive)
        {
            if (_camera == null) return;

            var screenRect = SelectionLogic.GetScreenRect(down, up);

            if (!additive)
            {
                SelectedBuilding = null;
                ClearSelection();
            }

            UnitRegistry.GetPlayerUnits(_unitBuffer);

            for (int i = 0; i < _unitBuffer.Count; i++)
            {
                var unit = _unitBuffer[i];
                if (unit == null) continue;

                // WorldToScreenPoint возвращает y снизу (как Input System),
                // поэтому координаты совместимы с screenRect напрямую.
                Vector3 sp = _camera.WorldToScreenPoint(unit.transform.position);
                if (sp.z < 0f) continue; // за камерой

                if (SelectionLogic.IsInside(screenRect, new Vector2(sp.x, sp.y)))
                    AddToSelectionInternal(unit);
            }

            ApplySelectionRings();
            NotifyChanged();
        }

        // ----------------------------------------------------------------
        // Управление списком выделения
        // ----------------------------------------------------------------

        private void SetSelection(Unit unit)
        {
            ClearSelection();
            _selected.Add(unit);
            unit.SetSelected(true);
            NotifyChanged();
        }

        private void AddToSelection(Unit unit)
        {
            if (!_selected.Contains(unit))
            {
                _selected.Add(unit);
                unit.SetSelected(true);
                NotifyChanged();
            }
        }

        /// <summary>Добавляет без вызова SetSelected и NotifyChanged — для batch-операций.</summary>
        private void AddToSelectionInternal(Unit unit)
        {
            if (!_selected.Contains(unit))
                _selected.Add(unit);
        }

        private void ClearSelection()
        {
            for (int i = 0; i < _selected.Count; i++)
            {
                if (_selected[i] != null)
                    _selected[i].SetSelected(false);
            }
            _selected.Clear();
        }

        private void ApplySelectionRings()
        {
            for (int i = 0; i < _selected.Count; i++)
            {
                if (_selected[i] != null)
                    _selected[i].SetSelected(true);
            }
        }

        private void NotifyChanged()
        {
            // Вычищаем null-ссылки перед уведомлением
            for (int i = _selected.Count - 1; i >= 0; i--)
            {
                if (_selected[i] == null)
                    _selected.RemoveAt(i);
            }
            SelectionChanged?.Invoke();
        }

        // ----------------------------------------------------------------
        // Контрол-группы
        // ----------------------------------------------------------------

        private void HandleControlGroups()
        {
            // Только в RTS-режиме
            if (modeController != null && modeController.CurrentMode != GameMode.Rts)
                return;

            if (Keyboard.current == null) return;

            bool ctrl = Keyboard.current.ctrlKey.isPressed;

            // Проверяем клавиши 1..5
            for (int i = 1; i <= 5; i++)
            {
                if (!GetDigitKeyDown(i)) continue;

                if (ctrl)
                    SaveControlGroup(i);
                else
                    LoadControlGroup(i);

                break; // обрабатываем только одну клавишу за кадр
            }
        }

        private void SaveControlGroup(int slot)
        {
            var group = _controlGroups[slot];
            group.Clear();
            for (int i = 0; i < _selected.Count; i++)
            {
                if (_selected[i] != null)
                    group.Add(_selected[i]);
            }
        }

        private void LoadControlGroup(int slot)
        {
            var group = _controlGroups[slot];

            ClearSelection();

            for (int i = 0; i < group.Count; i++)
            {
                var unit = group[i];
                if (unit == null) continue;
                _selected.Add(unit);
                unit.SetSelected(true);
            }

            NotifyChanged();
        }

        // ----------------------------------------------------------------
        // Вспомогательные методы
        // ----------------------------------------------------------------

        private static bool GetDigitKeyDown(int digit)
        {
            var kb = Keyboard.current;
            return digit switch
            {
                1 => kb.digit1Key.wasPressedThisFrame,
                2 => kb.digit2Key.wasPressedThisFrame,
                3 => kb.digit3Key.wasPressedThisFrame,
                4 => kb.digit4Key.wasPressedThisFrame,
                5 => kb.digit5Key.wasPressedThisFrame,
                _ => false,
            };
        }

        private static void EnsureBoxTexture()
        {
            if (_boxTexture != null) return;
            _boxTexture = new Texture2D(1, 1);
            _boxTexture.SetPixel(0, 0, Color.white);
            _boxTexture.Apply();
        }
    }
}
