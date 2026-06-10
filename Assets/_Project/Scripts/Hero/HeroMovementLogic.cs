using UnityEngine;

namespace DiplomaGame.Runtime.Hero
{
    /// <summary>
    /// Чистая статика без зависимостей от MonoBehaviour.
    /// Полностью тестируется в EditMode.
    /// </summary>
    public static class HeroMovementLogic
    {
        /// <summary>
        /// Возвращает направление движения в мировых координатах по входному вектору
        /// и текущему yaw-углу камеры (в градусах).
        /// Y-компонента всегда равна нулю. Результат нормализован (или zero при нулевом вводе).
        /// </summary>
        /// <param name="input">Ввод WASD в плоскости XY (X — горизонталь, Y — вертикаль).</param>
        /// <param name="cameraYawDegrees">Угол поворота камеры вокруг оси Y в градусах.</param>
        public static Vector3 GetWorldMoveDirection(Vector2 input, float cameraYawDegrees)
        {
            if (input.sqrMagnitude < 1e-6f)
                return Vector3.zero;

            // Локальное направление: input.y — вперёд, input.x — вправо
            var localDir = new Vector3(input.x, 0f, input.y);

            // Поворачиваем на yaw камеры, чтобы движение было относительно взгляда
            var rotation  = Quaternion.Euler(0f, cameraYawDegrees, 0f);
            var worldDir  = rotation * localDir;

            // Нормализуем для одинаковой скорости по диагонали
            return worldDir.normalized;
        }

        /// <summary>
        /// Применяет гравитацию к вертикальной составляющей скорости.
        /// На земле сбрасывает к небольшому отрицательному значению (прижимает к земле).
        /// </summary>
        /// <param name="verticalVelocity">Текущая вертикальная скорость.</param>
        /// <param name="gravity">Ускорение свободного падения (отрицательное, напр. -20).</param>
        /// <param name="deltaTime">Time.deltaTime.</param>
        /// <param name="isGrounded">CharacterController.isGrounded.</param>
        public static float ApplyGravity(float verticalVelocity, float gravity, float deltaTime, bool isGrounded)
        {
            if (isGrounded && verticalVelocity < 0f)
                return -2f; // Константное прижатие к земле — стабильнее нуля

            return verticalVelocity + gravity * deltaTime;
        }
    }
}
