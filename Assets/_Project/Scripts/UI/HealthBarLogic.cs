using UnityEngine;

namespace DiplomaGame.Runtime.UI
{
    /// <summary>
    /// Чистая логика HP-баров над юнитами (Circle-18).
    /// Без MonoBehaviour, без Unity API, кроме UnityEngine-структур (Color, Vector3, Rect, Camera).
    /// Тестируется в EditMode без сцены.
    ///
    /// Покрываемые контракты:
    ///   • HpFraction → fillAmount (0..1 clamp)
    ///   • HpFraction → Color (green→yellow→red gradient)
    ///   • ShouldShowBar: только если selected OR damaged (hp &lt; max)
    ///   • IsOnScreen: простая Viewport-проверка (z &gt; 0 и x/y в [cullMargin..1-cullMargin])
    /// </summary>
    public static class HealthBarLogic
    {
        // ----------------------------------------------------------------
        // Константы цвета
        // ----------------------------------------------------------------

        /// <summary>Доля HP, ниже которой цвет становится жёлтым (0..1).</summary>
        public const float YellowThreshold = 0.5f;

        /// <summary>Доля HP, ниже которой цвет становится красным (0..1).</summary>
        public const float RedThreshold = 0.25f;

        // ----------------------------------------------------------------
        // FillAmount
        // ----------------------------------------------------------------

        /// <summary>
        /// Возвращает fillAmount для заданной доли HP.
        /// Гарантирован диапазон 0..1 (Clamp01).
        /// </summary>
        /// <param name="hpFraction">CurrentHp / MaxHp.</param>
        public static float GetFillAmount(float hpFraction)
        {
            return Mathf.Clamp01(hpFraction);
        }

        // ----------------------------------------------------------------
        // Цвет полосы
        // ----------------------------------------------------------------

        /// <summary>
        /// Возвращает цвет полосы HP по доле HP.
        /// Алгоритм (без аллокаций — Color — struct):
        ///   hp &gt;= YellowThreshold  → lerp(green, yellow, t) где t = 1 - (hp - Yellow) / (1 - Yellow)
        ///   hp &lt;  YellowThreshold  → lerp(yellow, red, t)  где t = 1 - hp / Yellow
        /// </summary>
        /// <param name="hpFraction">CurrentHp / MaxHp (0..1).</param>
        public static Color GetBarColor(float hpFraction)
        {
            float f = Mathf.Clamp01(hpFraction);

            if (f >= YellowThreshold)
            {
                // green ↔ yellow: high HP → green, as HP drops towards YellowThreshold → yellow
                float t = 1f - (f - YellowThreshold) / (1f - YellowThreshold);
                return Color.Lerp(Color.green, Color.yellow, t);
            }
            else
            {
                // yellow ↔ red: HP drops from YellowThreshold towards 0 → red
                float t = 1f - f / YellowThreshold;
                return Color.Lerp(Color.yellow, Color.red, t);
            }
        }

        // ----------------------------------------------------------------
        // Видимость
        // ----------------------------------------------------------------

        /// <summary>
        /// Возвращает true если бар должен быть показан.
        /// Политика: показываем только если выделен ИЛИ повреждён (hp &lt; max).
        /// Мёртвые юниты (isDead) не показываем.
        /// </summary>
        /// <param name="isSelected">Юнит в списке выделения SelectionSystem.</param>
        /// <param name="hpFraction">CurrentHp / MaxHp (0..1).</param>
        /// <param name="isDead">Health.IsDead.</param>
        public static bool ShouldShowBar(bool isSelected, float hpFraction, bool isDead)
        {
            if (isDead) return false;
            // Повреждён: hp строго меньше 1 (с небольшим допуском на float-точность)
            bool isDamaged = hpFraction < 1f - 1e-4f;
            return isSelected || isDamaged;
        }

        // ----------------------------------------------------------------
        // Frustum cull
        // ----------------------------------------------------------------

        /// <summary>
        /// Возвращает true если viewport-точка считается "на экране".
        /// Отсекает: z &lt;= 0 (за камерой) и нормализованные x/y за пределами [cullMargin, 1−cullMargin].
        /// cullMargin = 0 соответствует строго [0,1] (весь экран).
        /// Принимает plain struct — нет аллокаций.
        /// </summary>
        /// <param name="viewportPoint">Результат Camera.WorldToViewportPoint.</param>
        /// <param name="cullMargin">Отступ от края viewport (0 = весь экран, 0.05 = 5% отступ).</param>
        public static bool IsOnScreen(Vector3 viewportPoint, float cullMargin = 0f)
        {
            if (viewportPoint.z <= 0f) return false;
            float lo = cullMargin;
            float hi = 1f - cullMargin;
            return viewportPoint.x >= lo && viewportPoint.x <= hi
                && viewportPoint.y >= lo && viewportPoint.y <= hi;
        }

        // ----------------------------------------------------------------
        // Позиция бара
        // ----------------------------------------------------------------

        /// <summary>
        /// Переводит мировую позицию головы юнита в экранную позицию для UI-элемента.
        /// worldHeadPos — transform.position + Vector3.up * headOffset.
        /// Возвращает Vector2 в экранных координатах (y снизу, как Input System).
        /// Вызывающий код проверяет IsOnScreen перед вызовом.
        /// Нет аллокаций: Camera.WorldToScreenPoint возвращает struct.
        /// </summary>
        /// <param name="camera">RTS-камера (Camera.main в RTS-режиме).</param>
        /// <param name="worldHeadPos">Мировая позиция верхней точки юнита.</param>
        public static Vector2 WorldToScreenBarPosition(Camera camera, Vector3 worldHeadPos)
        {
            Vector3 sp = camera.WorldToScreenPoint(worldHeadPos);
            return new Vector2(sp.x, sp.y);
        }
    }
}
