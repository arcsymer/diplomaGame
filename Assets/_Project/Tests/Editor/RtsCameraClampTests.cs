using DiplomaGame.Runtime.CameraControl;
using NUnit.Framework;
using UnityEngine;

namespace DiplomaGame.Tests.Editor
{
    /// <summary>
    /// EditMode-тесты для RtsCameraController.ClampPosition — чистая функция без MonoBehaviour.
    /// </summary>
    [TestFixture]
    public class RtsCameraClampTests
    {
        private static readonly Vector2 MinBounds = new Vector2(-48f, -48f);
        private static readonly Vector2 MaxBounds = new Vector2( 48f,  48f);

        // ================================================================
        // Позиция внутри границ — не меняется
        // ================================================================

        [Test]
        public void ClampPosition_InsideBounds_ReturnsUnchanged()
        {
            var pos    = new Vector3(10f, 5f, -15f);
            var result = RtsCameraController.ClampPosition(pos, MinBounds, MaxBounds);

            Assert.AreEqual(10f,  result.x, 0.001f, "X не должен измениться.");
            Assert.AreEqual(5f,   result.y, 0.001f, "Y не должен изменяться (высота игнорируется).");
            Assert.AreEqual(-15f, result.z, 0.001f, "Z не должен измениться.");
        }

        // ================================================================
        // X выходит за правую границу (+max)
        // ================================================================

        [Test]
        public void ClampPosition_XOverMaxBound_ClampedToMax()
        {
            var pos    = new Vector3(100f, 0f, 0f);
            var result = RtsCameraController.ClampPosition(pos, MinBounds, MaxBounds);

            Assert.AreEqual(48f, result.x, 0.001f, "X должен зажаться до maxBounds.x.");
        }

        // ================================================================
        // X выходит за левую границу (-min)
        // ================================================================

        [Test]
        public void ClampPosition_XUnderMinBound_ClampedToMin()
        {
            var pos    = new Vector3(-100f, 0f, 0f);
            var result = RtsCameraController.ClampPosition(pos, MinBounds, MaxBounds);

            Assert.AreEqual(-48f, result.x, 0.001f, "X должен зажаться до minBounds.x.");
        }

        // ================================================================
        // Z выходит за дальнюю границу
        // ================================================================

        [Test]
        public void ClampPosition_ZOverMaxBound_ClampedToMax()
        {
            var pos    = new Vector3(0f, 0f, 200f);
            var result = RtsCameraController.ClampPosition(pos, MinBounds, MaxBounds);

            Assert.AreEqual(48f, result.z, 0.001f, "Z должен зажаться до maxBounds.y.");
        }

        // ================================================================
        // Z выходит за ближнюю границу
        // ================================================================

        [Test]
        public void ClampPosition_ZUnderMinBound_ClampedToMin()
        {
            var pos    = new Vector3(0f, 0f, -200f);
            var result = RtsCameraController.ClampPosition(pos, MinBounds, MaxBounds);

            Assert.AreEqual(-48f, result.z, 0.001f, "Z должен зажаться до minBounds.y.");
        }

        // ================================================================
        // Y не изменяется (высота камеры не кламплируется)
        // ================================================================

        [Test]
        public void ClampPosition_YIsPreserved()
        {
            var pos    = new Vector3(0f, 35f, 0f);
            var result = RtsCameraController.ClampPosition(pos, MinBounds, MaxBounds);

            Assert.AreEqual(35f, result.y, 0.001f, "Y (высота) не должен зажиматься — не входит в XZ-клямп.");
        }

        // ================================================================
        // Ровно на границе — не изменяется
        // ================================================================

        [Test]
        public void ClampPosition_ExactlyOnBound_ReturnsUnchanged()
        {
            var pos    = new Vector3(48f, 0f, -48f);
            var result = RtsCameraController.ClampPosition(pos, MinBounds, MaxBounds);

            Assert.AreEqual(48f,  result.x, 0.001f, "Значение на правой границе должно остаться.");
            Assert.AreEqual(-48f, result.z, 0.001f, "Значение на нижней границе должно остаться.");
        }
    }
}
