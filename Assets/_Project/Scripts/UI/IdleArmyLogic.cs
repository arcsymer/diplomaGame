using System.Collections.Generic;
using DiplomaGame.Runtime.Units;

namespace DiplomaGame.Runtime.UI
{
    /// <summary>
    /// Чистая логика индикатора «бездействующая армия»: без MonoBehaviour, без Unity API
    /// (кроме UnityEngine.Time — заменяется инъекцией float в тестах).
    /// Тестируется в EditMode без сцены.
    ///
    /// Алгоритм:
    ///   Для каждого юнита хранится время последнего выхода из Idle
    ///   (lastNonIdleTime). Если юнит находится в Idle и (now - lastNonIdleTime)
    ///   >= IdleThreshold — он считается "застоявшимся" и попадает в badge.
    /// </summary>
    public static class IdleArmyLogic
    {
        /// <summary>Порог бездействия в секундах. Юнит «застоял» после этого времени в Idle.</summary>
        public const float IdleThreshold = 5f;

        /// <summary>
        /// Обновляет словарь lastNonIdleTimes на основе текущих состояний юнитов.
        /// Если юнит НЕ в Idle — записывает now как время последней активности.
        /// Вызывается при каждом интервальном поллинге из IdleArmyIndicator.
        /// Без аллокаций: итерируем по переданному списку.
        /// </summary>
        /// <param name="units">Текущий список юнитов игрока (переиспользуемый буфер).</param>
        /// <param name="lastNonIdleTimes">Словарь unit→время последней НЕ-Idle активности.</param>
        /// <param name="now">Текущее время (Time.time).</param>
        public static void UpdateTimestamps(
            List<Unit>              units,
            Dictionary<Unit, float> lastNonIdleTimes,
            float                   now)
        {
            for (int i = 0; i < units.Count; i++)
            {
                var unit = units[i];
                if (unit == null) continue;

                if (unit.CurrentState != UnitState.Idle)
                    lastNonIdleTimes[unit] = now;
                else if (!lastNonIdleTimes.ContainsKey(unit))
                    // Первая встреча юнита — он сразу в Idle; seed timestamp = now
                    // (начнём отсчёт с момента первого обнаружения).
                    lastNonIdleTimes[unit] = now;
            }
        }

        /// <summary>
        /// Удаляет записи о юнитах, которых больше нет в activeUnits
        /// (умерли / деспавнились). Предотвращает утечку памяти в словаре.
        /// Использует переданный буфер для ключей, не создаёт новых списков.
        /// </summary>
        /// <param name="activeUnits">Текущий список живых юнитов игрока.</param>
        /// <param name="lastNonIdleTimes">Словарь для очистки.</param>
        /// <param name="deadBuffer">Временный буфер — очищается и используется внутри, не аллоцируется.</param>
        public static void PruneDeadUnits(
            List<Unit>              activeUnits,
            Dictionary<Unit, float> lastNonIdleTimes,
            List<Unit>              deadBuffer)
        {
            deadBuffer.Clear();

            foreach (var key in lastNonIdleTimes.Keys)
            {
                if (key == null || !activeUnits.Contains(key))
                    deadBuffer.Add(key);
            }

            for (int i = 0; i < deadBuffer.Count; i++)
                lastNonIdleTimes.Remove(deadBuffer[i]);
        }

        /// <summary>
        /// Заполняет <paramref name="idleResult"/> юнитами, бездействующими
        /// дольше <see cref="IdleThreshold"/> секунд.
        /// Без аллокаций — результат записывается в переданный буфер.
        /// </summary>
        /// <param name="units">Текущий список юнитов игрока.</param>
        /// <param name="lastNonIdleTimes">Словарь с метками времени последней активности.</param>
        /// <param name="now">Текущее время (Time.time).</param>
        /// <param name="idleResult">Буфер для результата — очищается перед заполнением.</param>
        public static void GetIdleUnits(
            List<Unit>              units,
            Dictionary<Unit, float> lastNonIdleTimes,
            float                   now,
            List<Unit>              idleResult)
        {
            idleResult.Clear();

            for (int i = 0; i < units.Count; i++)
            {
                var unit = units[i];
                if (unit == null) continue;
                if (unit.CurrentState != UnitState.Idle) continue;

                if (lastNonIdleTimes.TryGetValue(unit, out float lastActive))
                {
                    if (now - lastActive >= IdleThreshold)
                        idleResult.Add(unit);
                }
            }
        }

        /// <summary>
        /// Возвращает количество бездействующих юнитов (>= IdleThreshold).
        /// Используется когда нужен только счётчик, без списка.
        /// </summary>
        public static int CountIdleUnits(
            List<Unit>              units,
            Dictionary<Unit, float> lastNonIdleTimes,
            float                   now)
        {
            int count = 0;

            for (int i = 0; i < units.Count; i++)
            {
                var unit = units[i];
                if (unit == null) continue;
                if (unit.CurrentState != UnitState.Idle) continue;

                if (lastNonIdleTimes.TryGetValue(unit, out float lastActive))
                {
                    if (now - lastActive >= IdleThreshold)
                        count++;
                }
            }

            return count;
        }
    }
}
