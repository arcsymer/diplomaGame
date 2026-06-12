using DiplomaGame.Runtime.Core.Localization;
using TMPro;
using UnityEngine;

namespace DiplomaGame.Runtime.UI
{
    /// <summary>
    /// Компонент на GameObject с TMP_Text: подписывается на LocService.LanguageChanged
    /// и обновляет текст при смене языка.
    /// locKey задаётся в Inspector; текст берётся из LocService.Get(locKey).
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public sealed class LocalizedText : MonoBehaviour
    {
        [SerializeField] private string locKey;

        private TMP_Text _label;

        private void Awake()
        {
            _label = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            LocService.LanguageChanged += Refresh;
            Refresh();
        }

        private void OnDisable()
        {
            LocService.LanguageChanged -= Refresh;
        }

        private void Refresh()
        {
            if (_label == null || string.IsNullOrEmpty(locKey)) return;
            _label.SetText(LocService.Get(locKey));
        }
    }
}
