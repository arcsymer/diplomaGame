using DiplomaGame.Runtime.Selection;
using DiplomaGame.Runtime.Units;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DiplomaGame.Runtime.Commands
{
    /// <summary>
    /// Преобразует RMB-клик (RTS/Command) и клавишу Hold (RTS/Hold) в приказы юнитам.
    /// Раздаёт приказы всем выделенным юнитам с формационным смещением.
    /// Move по умолчанию; зажата A → AttackMove; зажата P → Patrol.
    /// </summary>
    public sealed class CommandInput : MonoBehaviour
    {
        [SerializeField] private SelectionSystem selectionSystem;
        [SerializeField] private InputActionAsset actions;

        // ----------------------------------------------------------------
        // Кэш действий (Awake/OnEnable)
        // ----------------------------------------------------------------

        private InputAction _commandAction;
        private InputAction _holdAction;

        // ----------------------------------------------------------------
        // Unity lifecycle
        // ----------------------------------------------------------------

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

        private void OnCommand(InputAction.CallbackContext ctx)
        {
            if (selectionSystem == null) return;
            if (selectionSystem.Selected.Count == 0) return;
            if (Camera.main == null) return;
            if (Mouse.current == null) return;

            Vector2 mousePos = Mouse.current.position.ReadValue();
            var ray = Camera.main.ScreenPointToRay(new Vector3(mousePos.x, mousePos.y, 0f));

            if (!Physics.Raycast(ray, out RaycastHit hit, 1000f)) return;

            Vector3 targetPoint = hit.point;

            bool pressA = Keyboard.current != null && Keyboard.current.aKey.isPressed;
            bool pressP = Keyboard.current != null && Keyboard.current.pKey.isPressed;

            var selected = selectionSystem.Selected;
            for (int i = 0; i < selected.Count; i++)
            {
                var unit = selected[i];
                if (unit == null) continue;

                Vector3 offset = UnitCommandLogic.GetFormationOffset(i);
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
