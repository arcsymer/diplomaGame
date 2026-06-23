namespace DiplomaGame.Runtime.Hero
{
    /// <summary>
    /// Чистая детерминированная логика стамины спринта без зависимостей от MonoBehaviour.
    /// Полностью тестируется в EditMode.
    /// </summary>
    public static class SprintStaminaLogic
    {
        /// <summary>
        /// Один тик стамины спринта. Возвращает новое значение стамины и флаг активного спринта.
        /// </summary>
        /// <param name="currentStamina">Текущая стамина (0..max).</param>
        /// <param name="sprintHeld">Игрок удерживает кнопку спринта.</param>
        /// <param name="isMoving">Герой реально двигается (moveInput.sqrMagnitude > 0).</param>
        /// <param name="dt">Время кадра (deltaTime).</param>
        /// <param name="drainRate">Убыль стамины в секунду при активном спринте (e.g. 25).</param>
        /// <param name="regenRate">Восстановление стамины в секунду когда не спринтуем (e.g. 15).</param>
        /// <param name="max">Максимальная стамина (e.g. 100).</param>
        /// <param name="minToStart">Минимум стамины для начала нового спринта (e.g. 10).
        ///     Если спринт уже активен — продолжается до 0.</param>
        /// <param name="wasSprinting">Был ли спринт активен в прошлом кадре.</param>
        /// <returns>
        ///     (nextStamina, isSprintingNow) — следующая стамина (0..max) и активен ли спринт.
        /// </returns>
        public static (float nextStamina, bool isSprintingNow) Tick(
            float currentStamina,
            bool  sprintHeld,
            bool  isMoving,
            float dt,
            float drainRate,
            float regenRate,
            float max,
            float minToStart,
            bool  wasSprinting)
        {
            // Определяем, активен ли спринт в этом кадре.
            // Условие старта: кнопка зажата + движение + стамина выше порога (если спринт ещё не шёл).
            // Условие продолжения: кнопка зажата + движение + стамина > 0.
            bool isSprinting;
            if (sprintHeld && isMoving)
            {
                if (wasSprinting)
                    // Уже спринтовали — продолжаем пока есть хоть что-то (auto-stop при 0)
                    isSprinting = currentStamina > 0f;
                else
                    // Новый спринт — нужен порог
                    isSprinting = currentStamina >= minToStart;
            }
            else
            {
                isSprinting = false;
            }

            float next;
            if (isSprinting)
            {
                next = currentStamina - drainRate * dt;
                if (next < 0f) next = 0f;
                // Стамина кончилась в этом тике — спринт тоже гасим
                if (next == 0f)
                    isSprinting = false;
            }
            else
            {
                next = currentStamina + regenRate * dt;
                if (next > max) next = max;
            }

            return (next, isSprinting);
        }
    }
}
