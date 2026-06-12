using DiplomaGame.Runtime.Data;
using UnityEngine;
using UnityEngine.UI;

namespace DiplomaGame.Runtime.UI
{
    /// <summary>
    /// Один слот очереди производства в командной карте (v6).
    /// У слота с индексом 0 отображается progressOverlay (fillAmount = прогресс текущего юнита).
    /// Остальные слоты используют только иконку.
    /// </summary>
    public sealed class QueueSlotUI : MonoBehaviour
    {
        [SerializeField] private Image iconImage;

        /// <summary>
        /// Overlay прогресса — используется только у слота 0.
        /// В остальных слотах оставьте null или отключённым.
        /// </summary>
        [SerializeField] private Image progressOverlay;

        // Placeholder-цвет для слотов без иконки
        private static readonly Color PlaceholderColor = new Color(0.35f, 0.35f, 0.45f, 1f);

        // ----------------------------------------------------------------
        // Публичный API
        // ----------------------------------------------------------------

        /// <summary>
        /// Показать слот с данной записью. progress01 [0..1] — для слота 0 заполняет overlay.
        /// Поддерживает как production-записи (entry.unitData), так и tech-записи (entry.techData).
        /// </summary>
        public void ShowEntry(ProductionEntry entry, float progress01)
        {
            gameObject.SetActive(true);

            if (iconImage != null)
            {
                // Иконка: сначала entry.icon, потом techData.Icon как fallback
                Sprite icon = null;
                if (entry != null)
                {
                    icon = entry.icon;
                    if (icon == null && entry.techData != null)
                        icon = entry.techData.Icon;
                }

                if (icon != null)
                {
                    iconImage.sprite = icon;
                    iconImage.color  = Color.white;
                }
                else
                {
                    iconImage.sprite = null;
                    iconImage.color  = PlaceholderColor;
                }
            }

            if (progressOverlay != null)
            {
                // Overlay — только у слота 0 (у остальных progressOverlay == null)
                progressOverlay.fillAmount = Mathf.Clamp01(progress01);
            }
        }

        /// <summary>Скрыть слот — нет ничего в очереди на этой позиции.</summary>
        public void Hide()
        {
            gameObject.SetActive(false);
        }

        // ----------------------------------------------------------------
        // Горячий путь Update — обновить только overlay без аллокаций
        // ----------------------------------------------------------------

        /// <summary>
        /// Обновляет fillAmount у overlay (только слот 0, без GC).
        /// Вызывается каждый кадр из SelectionPanel.Update.
        /// </summary>
        public void SetProgress(float progress01)
        {
            if (progressOverlay != null)
                progressOverlay.fillAmount = Mathf.Clamp01(progress01);
        }
    }
}
