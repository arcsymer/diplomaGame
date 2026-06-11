using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DiplomaGame.Runtime.UI
{
    /// <summary>
    /// Визуальный компонент тултипа: принимает TooltipData и обновляет TMP-тексты.
    /// Работает через CanvasGroup для fade-in.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class TooltipView : MonoBehaviour
    {
        [SerializeField] private TMP_Text  titleText;
        [SerializeField] private GameObject separator;
        [SerializeField] private TMP_Text  descriptionText;
        [SerializeField] private TMP_Text  statsText;

        private CanvasGroup _canvasGroup;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
        }

        // ----------------------------------------------------------------
        // Публичный API
        // ----------------------------------------------------------------

        // Ленивая инициализация: объект сохранён в сцене неактивным, поэтому Awake()
        // не вызывается до первого SetActive(true), а CanvasGroup нужен раньше.
        public CanvasGroup CanvasGroup =>
            _canvasGroup != null ? _canvasGroup : _canvasGroup = GetComponent<CanvasGroup>();

        /// <summary>Заполняет тултип данными и скрывает пустые блоки.</summary>
        public void Populate(TooltipData data)
        {
            if (titleText != null)
                titleText.SetText(data.Title ?? string.Empty);

            bool hasDescription = !string.IsNullOrEmpty(data.Description);
            bool hasStats       = !string.IsNullOrEmpty(data.Stats);

            if (descriptionText != null)
            {
                descriptionText.gameObject.SetActive(hasDescription);
                if (hasDescription)
                    descriptionText.SetText(data.Description);
            }

            if (separator != null)
                separator.SetActive(hasDescription || hasStats);

            if (statsText != null)
            {
                statsText.gameObject.SetActive(hasStats);
                if (hasStats)
                    statsText.SetText(data.Stats);
            }
        }

        // ----------------------------------------------------------------
        // Internal — для тестов
        // ----------------------------------------------------------------

        internal void InitForTest(TMP_Text title, TMP_Text description, TMP_Text stats, GameObject sep)
        {
            titleText       = title;
            descriptionText = description;
            statsText       = stats;
            separator       = sep;
            _canvasGroup    = GetComponent<CanvasGroup>();
        }
    }
}
