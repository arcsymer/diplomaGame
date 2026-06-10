using UnityEngine;
using UnityEngine.EventSystems;

namespace DiplomaGame.Runtime.Audio
{
    /// <summary>
    /// Компонент на кнопку: при клике воспроизводит UI-клик через AudioManager.
    /// Forge навешивает его на все Button в сцене.
    /// </summary>
    [RequireComponent(typeof(UnityEngine.UI.Button))]
    public sealed class UiButtonSound : MonoBehaviour, IPointerClickHandler
    {
        public void OnPointerClick(PointerEventData eventData)
        {
            AudioManager.Instance?.PlayUiClick();
        }
    }
}
