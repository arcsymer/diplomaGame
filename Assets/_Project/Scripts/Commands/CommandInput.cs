using System;
using DiplomaGame.Runtime.Buildings;
using DiplomaGame.Runtime.Selection;
using DiplomaGame.Runtime.Units;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DiplomaGame.Runtime.Commands
{
    /// <summary>
    /// Преобразует RMB-клик (RTS/Command) и клавишу Hold (RTS/Hold) в приказы юнитам.
    /// M5: RMB при выделенном здании → SetRallyPoint; T → TryEnqueue у Barracks.
    /// Раздаёт приказы всем выделенным юнитам с формационным смещением.
    /// Move по умолчанию; зажата A → AttackMove; зажата P → Patrol.
    /// </summary>
    public sealed class CommandInput : MonoBehaviour
    {
        [SerializeField] private SelectionSystem selectionSystem;
        [SerializeField] private InputActionAsset actions;

        // ----------------------------------------------------------------
        // Событие (UI-шина M6a)
        // ----------------------------------------------------------------

        /// <summary>
        /// Вызывается при каждом выданном приказе движения/атаки.
        /// Параметры: точка назначения, тип приказа.
        /// </summary>
        public event Action<Vector3, UnitCommandType> OrderIssued;

        // ----------------------------------------------------------------
        // Кэш действий (Awake/OnEnable)
        // ----------------------------------------------------------------

        private InputAction _commandAction;
        private InputAction _holdAction;

        // Кэш Camera.main (заполняется в Awake, null-safe повторная попытка в OnCommand)
        private Camera _cachedCamera;

        // ----------------------------------------------------------------
        // Unity lifecycle
        // ----------------------------------------------------------------

        private void Awake()
        {
            _cachedCamera = Camera.main;
        }

        private void OnEnable()
        {
            BindActions();
        }

        private void OnDisable()
        {
            if (_commandAction != null)
                _commandAction.performed -= OnCommand;

            if (_holdAction != null)
                _holdAction.performed -= OnHold;
        }

        // ----------------------------------------------------------------
        // Биндинг действий
        // ----------------------------------------------------------------

        private void BindActions()
        {
            if (actions == null) return;

            var rtsMap = actions.FindActionMap("RTS");
            if (rtsMap == null) return;

            _commandAction = rtsMap.FindAction("Command");
            if (_commandAction != null)
                _commandAction.performed += OnCommand;

            _holdAction = rtsMap.FindAction("Hold");
            if (_holdAction != null)
                _holdAction.performed += OnHold;
        }

        // ----------------------------------------------------------------
        // Обработчики входных событий
        // ----------------------------------------------------------------

        private void Update()
        {
            HandleBuildingCommands();
        }

        private void HandleBuildingCommands()
        {
            if (selectionSystem == null) return;

            var building = selectionSystem.SelectedBuilding;
            if (building == null) return;

            // T → TryEnqueue у Barracks
            if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
            {
                var prod = building.GetComponent<ProductionBuilding>();
                if (prod != null)
                    prod.TryEnqueue();
            }

            // RMB → SetRallyPoint
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            {
                if (_cachedCamera == null)
                    _cachedCamera = Camera.main;
                if (_cachedCamera == null) return;

                var mousePos = Mouse.current.position.ReadValue();
                var ray      = _cachedCamera.ScreenPointToRay(new Vector3(mousePos.x, mousePos.y, 0f));

                if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
                {
                    var prod = building.GetComponent<ProductionBuilding>();
                    if (prod != null)
                        prod.SetRallyPoint(hit.point);
                }
            }
        }

        private void OnCommand(InputAction.CallbackContext ctx)
        {
            if (selectionSystem == null) return;

            // Если выделено здание — RMB обрабатывается в Update (HandleBuildingCommands)
            if (selectionSystem.SelectedBuilding != null) return;

            if (selectionSystem.Selected.Count == 0) return;
            if (Mouse.current == null) return;

            // Null-safe повторная попытка (как в SelectionSystem)
            if (_cachedCamera == null)
                _cachedCamera = Camera.main;
            if (_cachedCamera == null) return;

            Vector2 mousePos = Mouse.current.position.ReadValue();
            var ray = _cachedCamera.ScreenPointToRay(new Vector3(mousePos.x, mousePos.y, 0f));

            if (!Physics.Raycast(ray, out RaycastHit hit, 1000f)) return;

            Vector3 targetPoint = hit.point;

            bool pressA = Keyboard.current != null && Keyboard.current.aKey.isPressed;
            bool pressP = Keyboard.current != null && Keyboard.current.pKey.isPressed;

            var selected = selectionSystem.Selected;

            UnitCommandType issuedType = pressA ? UnitCommandType.AttackMove
                                      : pressP  ? UnitCommandType.Patrol
                                      :            UnitCommandType.Move;

            // v3 crowd-avoidance: при больших группах увеличиваем расстояние
            // между ячейками формации, чтобы юниты не давились у цели.
            float formationSpacing = selected.Count > 6 ? 2.5f : 2.0f;

            for (int i = 0; i < selected.Count; i++)
            {
                var unit = selected[i];
                if (unit == null) continue;

                Vector3 offset = UnitCommandLogic.GetFormationOffset(i, formationSpacing);
                Vector3 point  = targetPoint + offset;

                UnitCommand cmd;
                if (pressA)
                    cmd = UnitCommand.AttackMove(point);
                else if (pressP)
                    cmd = UnitCommand.Patrol(point);
                else
                    cmd = UnitCommand.Move(point);

                unit.IssueCommand(cmd);
            }

            // Уведомляем подписчиков (OrderMarkerFeedback и другие) о выданном приказе
            OrderIssued?.Invoke(targetPoint, issuedType);
        }

        private void OnHold(InputAction.CallbackContext ctx)
        {
            if (selectionSystem == null) return;

            var selected = selectionSystem.Selected;
            for (int i = 0; i < selected.Count; i++)
            {
                var unit = selected[i];
                if (unit == null) continue;
                unit.IssueCommand(UnitCommand.Hold());
            }
        }
    }
}
