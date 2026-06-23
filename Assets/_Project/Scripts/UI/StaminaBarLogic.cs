using UnityEngine;

namespace DiplomaGame.Runtime.UI
{
    /// <summary>
    /// Чистая статическая логика полосы стамины героя (Circle-24).
    /// Без зависимостей от MonoBehaviour — полностью тестируется в EditMode.
    ///
    /// Покрываемые контракты:
    ///   • StaminaNormalized → fillAmount (0..1 clamp)
    ///   • StaminaNormalized → Color (yellow → red при низкой стамине)
    ///   • ShouldShowBar: показывать только когда isSprinting ИЛИ staminaNormalized &lt; 1
    /// </summary>
    public static class StaminaBarLogic
    {
        // ----------------------------------------------------------------
        // Константы цвета
        // ----------------------------------------------------------------

        /// <summary>Доля стамины, ниже которой начинается переход к красному (0..1).</summary>
        public const float RedThreshold = 0.35f;

        // ----------------------------------------------------------------
        // FillAmount
        // ----------------------------------------------------------------

        /// <summary>
        /// Возвращает fillAmount для заданной нормализованной стамины.
        /// Гарантирован диапазон 0..1 (Clamp01).
        /// </summary>
        /// <param name="staminaNormalized">StaminaNormalized (0..1).</param>
        public static float GetFillAmount(float staminaNormalized)
        {
            return Mathf.Clamp01(staminaNormalized);
        }

        // ----------------------------------------------------------------
        // Цвет полосы
        // ----------------------------------------------------------------

        /// <summary>
        /// Возвращает цвет полосы стамины по нормализованной доле.
        /// Алгоритм (без аллокаций — Color — struct):
        ///   stamina &gt;= RedThreshold → lerp(yellow, yellow) (полностью жёлтый)
        ///   stamina &lt;  RedThreshold → lerp(yellow, red, t) где t = 1 - stamina / RedThreshold
        /// При полной стамине — желтоватый (не зелёный, чтобы не путать с HP).
        /// </summary>
        /// <param name="staminaNormalized">StaminaNormalized (0..1).</param>
        public static Color GetBarColor(float staminaNormalized)
        {
            float f = Mathf.Clamp01(staminaNormalized);

            if (f >= RedThreshold)
            {
                return Color.yellow;
            }
            else
            {
                float t = 1f - f / RedThreshold;
                return Color.Lerp(Color.yellow, Color.red, t);
            }
        }

        // ----------------------------------------------------------------
        // Видимость
        // ----------------------------------------------------------------

        /// <summary>
        /// Возвращает true если полоса стамины должна быть видна.
        /// Политика: показываем только когда герой спринтует ИЛИ стамина не полная.
        /// При полной стамине в покое — скрываем, чтобы не загрязнять HUD.
        /// </summary>
        /// <param name="isSprinting">HeroController.IsSprinting.</param>
        /// <param name="staminaNormalized">HeroController.StaminaNormalized (0..1).</param>
        public static bool ShouldShowBar(bool isSprinting, float staminaNormalized)
        {
            if (isSprinting) return true;
            return staminaNormalized < 1f - 1e-4f;
        }
    }
}
