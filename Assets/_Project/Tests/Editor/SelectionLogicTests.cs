using NUnit.Framework;
using DiplomaGame.Runtime.Selection;
using UnityEngine;

namespace DiplomaGame.Tests.Editor
{
    /// <summary>
    /// EditMode-тесты для SelectionLogic.
    /// Не требуют сцены — вся логика статическая.
    /// </summary>
    [TestFixture]
    public class SelectionLogicTests
    {
        // ----------------------------------------------------------------
        // GetScreenRect
        // ----------------------------------------------------------------

        [Test]
        public void GetScreenRect_NormalOrder_CorrectRect()
        {
            var result = SelectionLogic.GetScreenRect(new Vector2(10f, 20f), new Vector2(50f, 80f));

            Assert.AreEqual(10f, result.xMin, 0.001f, "xMin должен быть 10.");
            Assert.AreEqual(50f, result.xMax, 0.001f, "xMax должен быть 50.");
            Assert.AreEqual(20f, result.yMin, 0.001f, "yMin должен быть 20.");
            Assert.AreEqual(80f, result.yMax, 0.001f, "yMax должен быть 80.");
        }

        [Test]
        public void GetScreenRect_InvertedX_Normalizes()
        {
            // a.x > b.x — должно нормализоваться
            var result = SelectionLogic.GetScreenRect(new Vector2(80f, 10f), new Vector2(20f, 60f));

            Assert.AreEqual(20f, result.xMin, 0.001f, "xMin должен нормализоваться к меньшему X.");
            Assert.AreEqual(80f, result.xMax, 0.001f, "xMax должен нормализоваться к большему X.");
        }

        [Test]
        public void GetScreenRect_InvertedY_Normalizes()
        {
            // a.y > b.y — должно нормализоваться
            var result = SelectionLogic.GetScreenRect(new Vector2(10f, 90f), new Vector2(50f, 30f));

            Assert.AreEqual(30f, result.yMin, 0.001f, "yMin должен нормализоваться к меньшему Y.");
            Assert.AreEqual(90f, result.yMax, 0.001f, "yMax должен нормализоваться к большему Y.");
        }

        [Test]
        public void GetScreenRect_BothInverted_Normalizes()
        {
            var result = SelectionLogic.GetScreenRect(new Vector2(100f, 200f), new Vector2(10f, 20f));

            Assert.AreEqual(10f,  result.xMin, 0.001f);
            Assert.AreEqual(100f, result.xMax, 0.001f);
            Assert.AreEqual(20f,  result.yMin, 0.001f);
            Assert.AreEqual(200f, result.yMax, 0.001f);
        }

        [Test]
        public void GetScreenRect_SamePoint_ZeroSizeRect()
        {
            var result = SelectionLogic.GetScreenRect(new Vector2(5f, 5f), new Vector2(5f, 5f));

            Assert.AreEqual(0f, result.width,  0.001f, "Ширина должна быть 0.");
            Assert.AreEqual(0f, result.height, 0.001f, "Высота должна быть 0.");
        }

        // ----------------------------------------------------------------
        // IsInside
        // ----------------------------------------------------------------

        [Test]
        public void IsInside_PointInsideRect_ReturnsTrue()
        {
            var rect = new Rect(0f, 0f, 100f, 100f);
            Assert.IsTrue(SelectionLogic.IsInside(rect, new Vector2(50f, 50f)),
                "Центральная точка должна быть внутри.");
        }

        [Test]
        public void IsInside_PointOnBorder_ReturnsTrue()
        {
            var rect = new Rect(0f, 0f, 100f, 100f);
            Assert.IsTrue(SelectionLogic.IsInside(rect, new Vector2(0f, 0f)),
                "Точка на левом-нижнем углу должна быть внутри (граничный случай).");
            Assert.IsTrue(SelectionLogic.IsInside(rect, new Vector2(100f, 100f)),
                "Точка на правом-верхнем углу должна быть внутри (граничный случай).");
            Assert.IsTrue(SelectionLogic.IsInside(rect, new Vector2(50f, 0f)),
                "Точка на нижней границе должна быть внутри.");
            Assert.IsTrue(SelectionLogic.IsInside(rect, new Vector2(0f, 50f)),
                "Точка на левой границе должна быть внутри.");
        }

        [Test]
        public void IsInside_PointOutsideRect_ReturnsFalse()
        {
            var rect = new Rect(10f, 10f, 80f, 80f);
            Assert.IsFalse(SelectionLogic.IsInside(rect, new Vector2(0f, 50f)),
                "Точка левее rect должна быть снаружи.");
            Assert.IsFalse(SelectionLogic.IsInside(rect, new Vector2(200f, 50f)),
                "Точка правее rect должна быть снаружи.");
            Assert.IsFalse(SelectionLogic.IsInside(rect, new Vector2(50f, 0f)),
                "Точка ниже rect должна быть снаружи.");
            Assert.IsFalse(SelectionLogic.IsInside(rect, new Vector2(50f, 200f)),
                "Точка выше rect должна быть снаружи.");
        }

        // ----------------------------------------------------------------
        // IsClick
        // ----------------------------------------------------------------

        [Test]
        public void IsClick_SamePoint_ReturnsTrue()
        {
            Assert.IsTrue(SelectionLogic.IsClick(Vector2.zero, Vector2.zero),
                "Одна и та же точка — это клик.");
        }

        [Test]
        public void IsClick_BelowThreshold_ReturnsTrue()
        {
            var down = new Vector2(0f, 0f);
            var up   = new Vector2(7f, 0f); // расстояние = 7, порог = 8
            Assert.IsTrue(SelectionLogic.IsClick(down, up),
                "Расстояние 7 < порог 8 — это клик.");
        }

        [Test]
        public void IsClick_ExactlyAtThreshold_ReturnsFalse()
        {
            var down = new Vector2(0f, 0f);
            var up   = new Vector2(8f, 0f); // расстояние = 8, порог = 8 → не клик (< threshold)
            Assert.IsFalse(SelectionLogic.IsClick(down, up),
                "Расстояние равно порогу — не клик (нужно строго меньше).");
        }

        [Test]
        public void IsClick_AboveThreshold_ReturnsFalse()
        {
            var down = new Vector2(0f, 0f);
            var up   = new Vector2(20f, 0f);
            Assert.IsFalse(SelectionLogic.IsClick(down, up),
                "Расстояние 20 > порог 8 — это рамка, не клик.");
        }

        [Test]
        public void IsClick_CustomThreshold_Respected()
        {
            var down = new Vector2(0f, 0f);
            var up   = new Vector2(4f, 0f);
            Assert.IsTrue( SelectionLogic.IsClick(down, up, 5f),  "Расстояние 4 < порог 5.");
            Assert.IsFalse(SelectionLogic.IsClick(down, up, 3f),  "Расстояние 4 > порог 3.");
        }
    }
}
