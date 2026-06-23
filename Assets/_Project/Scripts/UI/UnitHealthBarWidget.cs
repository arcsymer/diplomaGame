using UnityEngine;
using UnityEngine.UI;

namespace DiplomaGame.Runtime.UI
{
    /// <summary>
    /// Одна переиспользуемая плашка HP-бара (элемент пула).
    /// Живёт в Screen-Space Overlay Canvas "HealthBarsCanvas".
    /// Содержит Image-фон, Image-заполнение и (опционально) Image-рамку фракции.
    ///
    /// Управляется UnitHealthBarSystem: Bind/Unbind, SetVisible, SetPosition.
    /// Нет собственного Update, никаких подписок на события — всё идёт через систему.
    /// Нет аллокаций: все операции работают со struct-ами (Color, Vector2).
    /// </summary>
    public sealed class UnitHealthBarWidget : MonoBehaviour
    {
        // ----------------------------------------------------------------
        // Сериализованные поля (проставляются через Forge)
        // ----------------------------------------------------------------

        [Header("Заполнение")]
        [Tooltip("Image HP-заполнения (fillMethod = Horizontal, fillOrigin = Left).")]
        [SerializeField] private Image _fillImage;

        [Header("Рамка фракции (опционально)")]
        [Tooltip("Тонкая рамка, цвет которой задаётся по фракции юнита. Может быть null.")]
        [SerializeField] private Image _borderImage;

        // ----------------------------------------------------------------
        // Публичный API
        // ----------------------------------------------------------------

        /// <summary>Установить fillAmount и цвет заполнения. Нет аллокаций.</summary>
        public void SetFill(float fillAmount, Color color)
        {
            if (_fillImage == null) return;
            _fillImage.fillAmount = fillAmount;
            _fillImage.color      = color;
        }

        /// <summary>Установить цвет рамки фракции (если рамка присутствует).</summary>
        public void SetBorderColor(Color color)
        {
            if (_borderImage == null) return;
            _borderImage.color = color;
        }

        /// <summary>Переместить виджет в экранную позицию (anchoredPosition). Нет аллокаций.</summary>
        public void SetScreenPosition(Vector2 screenPos)
        {
            (transform as RectTransform).anchoredPosition = screenPos;
        }

        /// <summary>Показать или скрыть виджет. Использует gameObject.SetActive — дешевле Enabled.</summary>
        public void SetVisible(bool visible)
        {
            if (gameObject.activeSelf != visible)
                gameObject.SetActive(visible);
        }

        // ----------------------------------------------------------------
        // Internal — для тестов и Forge
        // ----------------------------------------------------------------

        internal Image FillImage   => _fillImage;
        internal Image BorderImage => _borderImage;

        /// <summary>Инъекция зависимостей для тестов без сцены.</summary>
        internal void InitForTest(Image fill, Image border = null)
        {
            _fillImage   = fill;
            _borderImage = border;
        }
    }
}
