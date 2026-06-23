using DiplomaGame.Runtime.UI;
using NUnit.Framework;
using UnityEngine;

namespace DiplomaGame.Tests.Editor
{
    /// <summary>
    /// EditMode-тесты для чистой статики HealthBarLogic (Circle-18).
    /// Нет MonoBehaviour — запускаются мгновенно без сцены.
    ///
    /// Тестируемые контракты:
    ///   • GetFillAmount — clamp 0..1, точность граничных значений.
    ///   • GetBarColor — green→yellow→red градиент по порогам.
    ///   • ShouldShowBar — политика видимости (selected, damaged, dead).
    ///   • IsOnScreen — frustum cull по viewport z и x/y.
    /// </summary>
    [TestFixture]
    public class HealthBarLogicTests
    {
        // ----------------------------------------------------------------
        // GetFillAmount
        // ----------------------------------------------------------------

        [Test]
        public void GetFillAmount_FullHp_ReturnsOne()
        {
            Assert.AreEqual(1f, HealthBarLogic.GetFillAmount(1f), 0.001f,
                "Полный HP → fillAmount = 1.");
        }

        [Test]
        public void GetFillAmount_HalfHp_ReturnsHalf()
        {
            Assert.AreEqual(0.5f, HealthBarLogic.GetFillAmount(0.5f), 0.001f,
                "50% HP → fillAmount = 0.5.");
        }

        [Test]
        public void GetFillAmount_Zero_ReturnsZero()
        {
            Assert.AreEqual(0f, HealthBarLogic.GetFillAmount(0f), 0.001f,
                "0 HP → fillAmount = 0.");
        }

        [Test]
        public void GetFillAmount_AboveOne_ClampedToOne()
        {
            Assert.AreEqual(1f, HealthBarLogic.GetFillAmount(1.5f), 0.001f,
                "HP > 1 → clamp к 1.");
        }

        [Test]
        public void GetFillAmount_BelowZero_ClampedToZero()
        {
            Assert.AreEqual(0f, HealthBarLogic.GetFillAmount(-0.5f), 0.001f,
                "HP < 0 → clamp к 0.");
        }

        // ----------------------------------------------------------------
        // GetBarColor
        // ----------------------------------------------------------------

        [Test]
        public void GetBarColor_FullHp_IsGreen()
        {
            Color c = HealthBarLogic.GetBarColor(1f);
            // Зелёный: R=0, G=1, B=0
            Assert.AreEqual(0f, c.r, 0.05f, "Full HP: R должен быть близок к 0 (зелёный).");
            Assert.Greater(c.g, 0.8f, "Full HP: G должен быть высоким (зелёный).");
        }

        [Test]
        public void GetBarColor_AtYellowThreshold_IsYellow()
        {
            // Точно на пороге YellowThreshold → должен вернуть yellow (либо очень близко)
            Color c = HealthBarLogic.GetBarColor(HealthBarLogic.YellowThreshold);
            // Yellow: R=1, G=1, B=0
            Assert.Greater(c.r, 0.8f, "At YellowThreshold: R должен быть высоким.");
            Assert.Greater(c.g, 0.8f, "At YellowThreshold: G должен быть высоким.");
            Assert.Less(c.b, 0.2f,    "At YellowThreshold: B должен быть низким.");
        }

        [Test]
        public void GetBarColor_ZeroHp_IsRed()
        {
            Color c = HealthBarLogic.GetBarColor(0f);
            // Red: R=1, G=0, B=0
            Assert.Greater(c.r, 0.8f, "0 HP: R должен быть высоким (красный).");
            Assert.Less(c.g, 0.2f,    "0 HP: G должен быть низким (красный).");
        }

        [Test]
        public void GetBarColor_AboveYellowThreshold_IsGreenish()
        {
            // При hp = 0.75 (между YellowThreshold и 1) — цвет должен быть «зеленовато-жёлтым»
            // Ключевое: G компонент высокий.
            Color c = HealthBarLogic.GetBarColor(0.75f);
            Assert.Greater(c.g, 0.5f, "HP=0.75: G должен быть существенным.");
        }

        [Test]
        public void GetBarColor_BelowYellowThreshold_HasHighRed()
        {
            // При hp = 0.2 (< YellowThreshold) — красный компонент высокий
            Color c = HealthBarLogic.GetBarColor(0.2f);
            Assert.Greater(c.r, 0.6f, "HP=0.2: R должен быть высоким (ближе к красному).");
        }

        [Test]
        public void GetBarColor_Negative_TreatedAsZero()
        {
            Color c1 = HealthBarLogic.GetBarColor(-1f);
            Color c2 = HealthBarLogic.GetBarColor(0f);
            // После Clamp01 оба должны дать одинаковый результат
            Assert.AreEqual(c2.r, c1.r, 0.001f, "Отрицательный HP = Clamp01 → такой же цвет как 0.");
            Assert.AreEqual(c2.g, c1.g, 0.001f);
        }

        [Test]
        public void GetBarColor_ColorChangesMonotonically_AsHpDecreases()
        {
            // При снижении HP зелёный компонент должен убывать, красный расти
            float hp1 = 1f;
            float hp2 = 0.6f;
            float hp3 = HealthBarLogic.YellowThreshold;
            float hp4 = 0.1f;

            Color c1 = HealthBarLogic.GetBarColor(hp1);
            Color c2 = HealthBarLogic.GetBarColor(hp2);
            Color c3 = HealthBarLogic.GetBarColor(hp3);
            Color c4 = HealthBarLogic.GetBarColor(hp4);

            // G должен убывать при снижении HP
            Assert.GreaterOrEqual(c1.g, c2.g, "G(1.0) >= G(0.6)");
            Assert.GreaterOrEqual(c2.g, c3.g, "G(0.6) >= G(yellow threshold)");

            // R должен расти при снижении HP ниже threshold
            Assert.GreaterOrEqual(c4.r, c3.r, "R(0.1) >= R(yellow threshold)");
        }

        // ----------------------------------------------------------------
        // ShouldShowBar
        // ----------------------------------------------------------------

        [Test]
        public void ShouldShowBar_SelectedAndFullHp_ReturnsTrue()
        {
            Assert.IsTrue(HealthBarLogic.ShouldShowBar(isSelected: true, hpFraction: 1f, isDead: false),
                "Выделенный юнит с полным HP должен показывать бар.");
        }

        [Test]
        public void ShouldShowBar_NotSelectedAndFullHp_ReturnsFalse()
        {
            Assert.IsFalse(HealthBarLogic.ShouldShowBar(isSelected: false, hpFraction: 1f, isDead: false),
                "Невыделенный юнит с полным HP НЕ должен показывать бар.");
        }

        [Test]
        public void ShouldShowBar_NotSelectedButDamaged_ReturnsTrue()
        {
            // Повреждённый (hp < max) невыделенный юнит — показываем
            Assert.IsTrue(HealthBarLogic.ShouldShowBar(isSelected: false, hpFraction: 0.99f, isDead: false),
                "Невыделенный, но повреждённый юнит должен показывать бар.");
        }

        [Test]
        public void ShouldShowBar_NotSelectedFullHpFloatPrecision_ReturnsFalse()
        {
            // Проверяем погрешность: 1.0 должен считаться «не повреждённым»
            Assert.IsFalse(HealthBarLogic.ShouldShowBar(isSelected: false, hpFraction: 1.0f, isDead: false),
                "Точно 1.0 HP (float) не должен давать «повреждён».");
        }

        [Test]
        public void ShouldShowBar_Dead_ReturnsFalse_EvenIfSelected()
        {
            Assert.IsFalse(HealthBarLogic.ShouldShowBar(isSelected: true, hpFraction: 0f, isDead: true),
                "Мёртвый юнит НЕ должен показывать бар, даже если выделен.");
        }

        [Test]
        public void ShouldShowBar_Dead_ReturnsFalse_EvenIfDamaged()
        {
            Assert.IsFalse(HealthBarLogic.ShouldShowBar(isSelected: false, hpFraction: 0.5f, isDead: true),
                "Мёртвый юнит НЕ должен показывать бар.");
        }

        [Test]
        public void ShouldShowBar_ZeroHpNotDead_ReturnsTrue()
        {
            // Редкий случай: hp=0 но isDead=false (промежуточный тик до смерти)
            Assert.IsTrue(HealthBarLogic.ShouldShowBar(isSelected: false, hpFraction: 0f, isDead: false),
                "hp=0, isDead=false — считается повреждённым → бар показываем.");
        }

        // ----------------------------------------------------------------
        // IsOnScreen
        // ----------------------------------------------------------------

        [Test]
        public void IsOnScreen_CenterOfScreen_ReturnsTrue()
        {
            var vp = new Vector3(0.5f, 0.5f, 5f);
            Assert.IsTrue(HealthBarLogic.IsOnScreen(vp),
                "Центр viewport (0.5,0.5) при z>0 должен быть на экране.");
        }

        [Test]
        public void IsOnScreen_BehindCamera_ReturnsFalse()
        {
            var vp = new Vector3(0.5f, 0.5f, -1f);
            Assert.IsFalse(HealthBarLogic.IsOnScreen(vp),
                "z <= 0 означает «за камерой» — не на экране.");
        }

        [Test]
        public void IsOnScreen_ZeroZ_ReturnsFalse()
        {
            var vp = new Vector3(0.5f, 0.5f, 0f);
            Assert.IsFalse(HealthBarLogic.IsOnScreen(vp),
                "z == 0 — ровно на плоскости камеры — считаем вне экрана.");
        }

        [Test]
        public void IsOnScreen_LeftEdge_ReturnsTrue_WithZeroMargin()
        {
            var vp = new Vector3(0f, 0.5f, 1f);
            Assert.IsTrue(HealthBarLogic.IsOnScreen(vp, cullMargin: 0f),
                "Ровно на левом крае (x=0) при margin=0 должен быть на экране.");
        }

        [Test]
        public void IsOnScreen_OutsideLeft_ReturnsFalse()
        {
            var vp = new Vector3(-0.1f, 0.5f, 1f);
            Assert.IsFalse(HealthBarLogic.IsOnScreen(vp, cullMargin: 0f),
                "x < 0 — за левым краем экрана.");
        }

        [Test]
        public void IsOnScreen_OutsideRight_ReturnsFalse()
        {
            var vp = new Vector3(1.1f, 0.5f, 1f);
            Assert.IsFalse(HealthBarLogic.IsOnScreen(vp, cullMargin: 0f),
                "x > 1 — за правым краем экрана.");
        }

        [Test]
        public void IsOnScreen_WithCullMargin_NearEdgeReturnsFalse()
        {
            // x=0.02, margin=0.05 → lo=0.05 → x < lo → false
            var vp = new Vector3(0.02f, 0.5f, 1f);
            Assert.IsFalse(HealthBarLogic.IsOnScreen(vp, cullMargin: 0.05f),
                "Юнит у края (x=0.02) при margin=0.05 должен быть скрыт (вне margin-области).");
        }

        [Test]
        public void IsOnScreen_WithCullMargin_InsideReturnsFalse()
        {
            // x=0.5, margin=0.05 → lo=0.05, hi=0.95 → 0.05 <= 0.5 <= 0.95 → true
            var vp = new Vector3(0.5f, 0.5f, 1f);
            Assert.IsTrue(HealthBarLogic.IsOnScreen(vp, cullMargin: 0.05f),
                "Центр экрана при margin=0.05 должен быть на экране.");
        }

        [Test]
        public void IsOnScreen_BottomEdge_ReturnsTrue_WithZeroMargin()
        {
            var vp = new Vector3(0.5f, 0f, 1f);
            Assert.IsTrue(HealthBarLogic.IsOnScreen(vp, cullMargin: 0f),
                "Нижний край (y=0) при margin=0 должен быть на экране.");
        }

        [Test]
        public void IsOnScreen_OutsideBottom_ReturnsFalse()
        {
            var vp = new Vector3(0.5f, -0.01f, 1f);
            Assert.IsFalse(HealthBarLogic.IsOnScreen(vp, cullMargin: 0f),
                "y < 0 — под экраном.");
        }

        // ----------------------------------------------------------------
        // Константы
        // ----------------------------------------------------------------

        [Test]
        public void Constants_YellowThreshold_IsHalf()
        {
            Assert.AreEqual(0.5f, HealthBarLogic.YellowThreshold, 0.001f,
                "YellowThreshold должен быть 0.5 (50% HP).");
        }

        [Test]
        public void Constants_RedThreshold_IsQuarter()
        {
            Assert.AreEqual(0.25f, HealthBarLogic.RedThreshold, 0.001f,
                "RedThreshold должен быть 0.25 (25% HP).");
        }

        [Test]
        public void Constants_RedThreshold_LessThan_YellowThreshold()
        {
            Assert.Less(HealthBarLogic.RedThreshold, HealthBarLogic.YellowThreshold,
                "RedThreshold должен быть меньше YellowThreshold.");
        }
    }
}
