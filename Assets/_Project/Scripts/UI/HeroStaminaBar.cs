using DiplomaGame.Runtime.Hero;
using UnityEngine;
using UnityEngine.UI;

namespace DiplomaGame.Runtime.UI
{
    /// <summary>
    /// Экранная полоса стамины героя в TPS HUD (Circle-24).
    ///
    /// Опрашивает HeroController.StaminaNormalized и IsSprinting каждый кадр.
    /// Нет аллокаций в Update-пути — все значения — примитивы или struct (Color).
    ///
    /// Видимость:
    ///   • Полоса скрыта, когда стамина полная И герой не спринтует.
    ///   • Появляется сразу при начале спринта или убывании стамины.
    ///
    /// Цвет fill:
    ///   • Жёлтый при нормальной стамине.
    ///   • Переходит в красный ниже StaminaBarLogic.RedThreshold.
    ///
    /// Bind вызывается из Forge или UITab.SetupStaminaBar.
    /// </summary>
    public sealed class HeroStaminaBar : MonoBehaviour
    {
        // ----------------------------------------------------------------
        // Сериализованные поля (кэшируются в Awake)
        // ----------------------------------------------------------------

        [SerializeField] private Image           _fill;
        [SerializeField] private HeroController  _heroController;

        // ----------------------------------------------------------------
        // Unity lifecycle
        // ----------------------------------------------------------------

        private void Update()
        {
            if (_heroController == null || _fill == null) return;

            float stamina    = _heroController.StaminaNormalized;
            bool  sprinting  = _heroController.IsSprinting;

            // Видимость всего бара
            bool shouldShow = StaminaBarLogic.ShouldShowBar(sprinting, stamina);
            if (gameObject.activeSelf != shouldShow)
                gameObject.SetActive(shouldShow);

            if (!shouldShow) return;

            // Fill и цвет (struct — нет аллокаций)
            _fill.fillAmount = StaminaBarLogic.GetFillAmount(stamina);
            _fill.color      = StaminaBarLogic.GetBarColor(stamina);
        }

        // ----------------------------------------------------------------
        // Публичный API (вызывается из Forge)
        // ----------------------------------------------------------------

        /// <summary>
        /// Привязывает полосу к компонентам UI и героя.
        /// Можно вызывать повторно — переприсваивает ссылки.
        /// </summary>
        public void Bind(Image fill, HeroController heroController)
        {
            _fill           = fill;
            _heroController = heroController;
        }

        // ----------------------------------------------------------------
        // Internal — для тестов
        // ----------------------------------------------------------------

        /// <summary>Ссылка на fill (для проверки в PlayMode-тестах).</summary>
        internal Image FillImage => _fill;

        /// <summary>Ссылка на HeroController (для проверки в PlayMode-тестах).</summary>
        internal HeroController Controller => _heroController;
    }
}
