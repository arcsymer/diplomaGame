namespace DiplomaGame.Runtime.UI
{
    /// <summary>
    /// Чистая логика системы «под атакой»: дросселирование, решение о запуске алерта,
    /// проверка автоочистки. Не зависит от MonoBehaviour — тестируется EditMode без сцены.
    /// </summary>
    public static class UnderAttackAlertLogic
    {
        /// <summary>Окно дросселирования: алерт не повторится раньше чем через это время (с).</summary>
        public const float ThrottleWindow = 3f;

        /// <summary>Время автоочистки алерта после последнего триггера (с).</summary>
        public const float ClearAfterSeconds = 3f;

        /// <summary>
        /// Возвращает true если алерт должен сработать.
        /// Условия: прошло не менее <see cref="ThrottleWindow"/> с с момента последнего триггера
        /// (или алерт ни разу не срабатывал — lastTriggerTime отрицательное / очень малое).
        /// </summary>
        /// <param name="lastTriggerTime">Время последнего успешного триггера (Time.time-шкала).</param>
        /// <param name="now">Текущее время (Time.time).</param>
        public static bool ShouldTrigger(float lastTriggerTime, float now)
        {
            return now - lastTriggerTime >= ThrottleWindow;
        }

        /// <summary>
        /// Возвращает true если алерт должен быть снят.
        /// Условие: прошло не менее <see cref="ClearAfterSeconds"/> с момента последнего триггера.
        /// </summary>
        /// <param name="lastTriggerTime">Время последнего успешного триггера.</param>
        /// <param name="now">Текущее время.</param>
        public static bool ShouldClear(float lastTriggerTime, float now)
        {
            return now - lastTriggerTime >= ClearAfterSeconds;
        }
    }
}
