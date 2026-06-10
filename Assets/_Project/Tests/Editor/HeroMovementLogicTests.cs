using DiplomaGame.Runtime.Hero;
using NUnit.Framework;
using UnityEngine;

namespace DiplomaGame.Tests.Editor
{
    /// <summary>
    /// EditMode-тесты для HeroMovementLogic.
    /// Не требуют сцены — вся логика статическая.
    /// </summary>
    [TestFixture]
    public class HeroMovementLogicTests
    {
        private const float Epsilon = 0.0001f;

        // ----------------------------------------------------------------
        // GetWorldMoveDirection
        // ----------------------------------------------------------------

        [Test]
        public void GetWorldMoveDirection_Yaw0_InputForward_ReturnsPositiveZ()
        {
            // Yaw 0 — герой смотрит вдоль +Z; forward input → +Z
            var result = HeroMovementLogic.GetWorldMoveDirection(new Vector2(0f, 1f), 0f);

            Assert.AreEqual(0f,  result.x, Epsilon, "X должен быть 0 при yaw=0, input=(0,1)");
            Assert.AreEqual(0f,  result.y, Epsilon, "Y должен быть 0 (горизонтальная плоскость)");
            Assert.AreEqual(1f,  result.z, Epsilon, "Z должен быть 1 при yaw=0, input=(0,1)");
        }

        [Test]
        public void GetWorldMoveDirection_Yaw90_InputForward_ReturnsPositiveX()
        {
            // Yaw 90° — герой смотрит вдоль +X; forward input → +X
            var result = HeroMovementLogic.GetWorldMoveDirection(new Vector2(0f, 1f), 90f);

            Assert.AreEqual(1f,  result.x, Epsilon, "X должен быть 1 при yaw=90, input=(0,1)");
            Assert.AreEqual(0f,  result.y, Epsilon, "Y должен быть 0 (горизонтальная плоскость)");
            Assert.AreEqual(0f,  result.z, Epsilon, "Z должен быть 0 при yaw=90, input=(0,1)");
        }

        [Test]
        public void GetWorldMoveDirection_ZeroInput_ReturnsZero()
        {
            var result = HeroMovementLogic.GetWorldMoveDirection(Vector2.zero, 45f);

            Assert.AreEqual(Vector3.zero, result, "Нулевой ввод → нулевое направление.");
        }

        [Test]
        public void GetWorldMoveDirection_DiagonalInput_IsNormalized()
        {
            // Диагональ (1,1) → после нормализации |result| должен быть 1
            var result = HeroMovementLogic.GetWorldMoveDirection(new Vector2(1f, 1f), 0f);

            Assert.AreEqual(1f, result.magnitude, Epsilon, "Диагональный ввод должен быть нормализован.");
        }

        [Test]
        public void GetWorldMoveDirection_Yaw0_InputRight_ReturnsPositiveX()
        {
            var result = HeroMovementLogic.GetWorldMoveDirection(new Vector2(1f, 0f), 0f);

            Assert.AreEqual(1f,  result.x, Epsilon);
            Assert.AreEqual(0f,  result.y, Epsilon);
            Assert.AreEqual(0f,  result.z, Epsilon);
        }

        [Test]
        public void GetWorldMoveDirection_ResultY_AlwaysZero()
        {
            // Независимо от yaw, Y всегда 0
            for (int yaw = 0; yaw < 360; yaw += 30)
            {
                var result = HeroMovementLogic.GetWorldMoveDirection(new Vector2(1f, 1f), yaw);
                Assert.AreEqual(0f, result.y, Epsilon,
                    $"Y-компонента должна быть 0 при yaw={yaw}");
            }
        }

        // ----------------------------------------------------------------
        // ApplyGravity
        // ----------------------------------------------------------------

        [Test]
        public void ApplyGravity_Grounded_ReturnsSmallNegative()
        {
            // На земле, скорость отрицательная → сбрасывается к -2
            float result = HeroMovementLogic.ApplyGravity(-5f, -20f, 0.016f, isGrounded: true);

            Assert.AreEqual(-2f, result, Epsilon, "На земле при отрицательной скорости возвращает -2.");
        }

        [Test]
        public void ApplyGravity_Grounded_PositiveVelocity_Accumulates()
        {
            // На земле, но скорость положительная (прыжок не реализован, но логика правильная):
            // isGrounded=true и velocity >= 0 → гравитация применяется нормально
            float result = HeroMovementLogic.ApplyGravity(0f, -20f, 0.016f, isGrounded: true);

            // Ветка: isGrounded && verticalVelocity < 0 — false (velocity = 0)
            // Применяется обычная гравитация: 0 + (-20)*0.016 = -0.32
            Assert.AreEqual(-0.32f, result, Epsilon, "При velocity=0 и isGrounded гравитация накапливается.");
        }

        [Test]
        public void ApplyGravity_InAir_Accumulates()
        {
            // В воздухе скорость накапливается вниз
            float result = HeroMovementLogic.ApplyGravity(0f, -20f, 0.5f, isGrounded: false);

            // 0 + (-20)*0.5 = -10
            Assert.AreEqual(-10f, result, Epsilon, "В воздухе гравитация накапливается.");
        }

        [Test]
        public void ApplyGravity_InAir_AlreadyFalling_AcceleratesFurther()
        {
            float result = HeroMovementLogic.ApplyGravity(-10f, -20f, 0.5f, isGrounded: false);

            // -10 + (-20)*0.5 = -20
            Assert.AreEqual(-20f, result, Epsilon, "Падение ускоряется при накоплении скорости.");
        }
    }
}
