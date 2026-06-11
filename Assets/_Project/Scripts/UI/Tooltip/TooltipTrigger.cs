using UnityEngine;
using UnityEngine.EventSystems;

namespace DiplomaGame.Runtime.UI
{
    /// <summary>
    /// Компонент-триггер тултипа. Навешивается на любой интерактивный UI-элемент.
    /// Ищет ITooltipProvider через GetComponent на том же объекте.
    /// </summary>
    public sealed class TooltipTrigger : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerMoveHandler,
        IPointerDownHandler
    {
        private ITooltipProvider _provider;

        private void Awake()
        {
            _provider = GetComponent<ITooltipProvider>();
        }

        // ----------------------------------------------------------------
        // IPointerEnterHandler
        // ----------------------------------------------------------------

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (_provider == null) return;

            var system = TooltipSystem.Instance;
            if (system == null) return;

            Vector2 screenPos = eventData.position;

            if (system.IsVisible)
                system.SwitchTo(_provider, screenPos);
            else
                system.RequestShow(_provider, screenPos);
        }

        // ----------------------------------------------------------------
        // IPointerExitHandler
        // ----------------------------------------------------------------

        public void OnPointerExit(PointerEventData eventData)
        {
            TooltipSystem.Instance?.Hide();
        }

        // ----------------------------------------------------------------
        // IPointerMoveHandler
        // ----------------------------------------------------------------

        public void OnPointerMove(PointerEventData eventData)
        {
            TooltipSystem.Instance?.UpdatePosition(eventData.position);
        }

        // ----------------------------------------------------------------
        // IPointerDownHandler — MouseDown скрывает тултип мгновенно
        // ----------------------------------------------------------------

        public void OnPointerDown(PointerEventData eventData)
        {
            TooltipSystem.Instance?.Hide();
        }
    }
}
