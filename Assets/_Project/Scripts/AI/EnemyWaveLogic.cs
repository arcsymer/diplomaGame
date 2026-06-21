using UnityEngine;

namespace DiplomaGame.Runtime.AI
{
    /// <summary>
    /// Чистая статическая логика принятия решений ИИ-противника.
    /// Без MonoBehaviour — полностью тестируема в EditMode.
    /// </summary>
    public static class EnemyWaveLogic
    {
        /// <summary>
        /// Возвращает true, если ИИ должен исследовать технологию.
        /// Условие: баланс покрывает стоимость технологии плюс 50 резервных единиц.
        /// </summary>
        public static bool ShouldResearch(int balance, int techCost)
        {
            if (techCost <= 0) return false;
            return balance >= techCost + 50;
        }

        /// <summary>
        /// Версия ShouldResearch с настраиваемым резервом из DifficultyProfileSO.
        /// reserve &lt; 0 → никогда не исследовать (возвращает false).
        /// reserve == 0 → исследовать, если хватает ровно на стоимость.
        /// </summary>
        public static bool ShouldResearchWithReserve(int balance, int techCost, int reserve)
        {
            if (reserve < 0)   return false;
            if (techCost <= 0) return false;
            return balance >= techCost + reserve;
        }

        /// <summary>
        /// Возвращает true, если ИИ должен произвести очередного юнита.
        /// Условие: баланс покрывает стоимость И текущее количество юнитов меньше лимита.
        /// </summary>
        public static bool ShouldProduce(int balance, int unitCost, int currentUnits, int maxUnits)
        {
            if (unitCost <= 0)         return false;
            if (currentUnits >= maxUnits) return false;
            return balance >= unitCost;
        }

        /// <summary>
        /// Возвращает true, если ИИ должен отправить волну.
        /// Условие A: накопилось не менее waveSize юнитов.
        /// Условие B: прошло maxWaitTime секунд с последней волны И есть хотя бы 2 юнита.
        /// </summary>
        public static bool ShouldLaunchWave(
            int   idleCombatUnits,
            int   waveSize,
            float timeSinceLastWave,
            float maxWaitTime)
        {
            if (idleCombatUnits <= 0) return false;

            // Условие A — накопилась полная волна
            if (idleCombatUnits >= waveSize) return true;

            // Условие B — вышло время ожидания, хотя бы 2 юнита
            if (timeSinceLastWave >= maxWaitTime && idleCombatUnits >= 2) return true;

            return false;
        }

        /// <summary>
        /// Возвращает желаемый размер волны в зависимости от времени матча (в секундах).
        /// До 3 мин — 3, до 7 мин — 5, после — 7.
        /// </summary>
        public static int GetWaveSizeForTime(float matchTime)
        {
            if (matchTime < 180f) return 3;   // 0–3 мин
            if (matchTime < 420f) return 5;   // 3–7 мин
            return 7;                          // 7+ мин
        }

        // ----------------------------------------------------------------
        // Circle-14: фланговые волны + экстренная волна
        // ----------------------------------------------------------------

        /// <summary>
        /// Возвращает целевую точку атакующей волны.
        /// Если flankWaypoints null/пуст ИЛИ хэш(seed) даёт процент >= flankProbability*100
        /// — возвращает hqPos (прямая атака).
        /// Иначе — один из flankWaypoints, выбранный детерминированно по хэшу seed.
        /// Использует MurmurHash3 finalizer (тот же алгоритм, что в CombatLogic).
        /// Без аллокаций, без UnityEngine.Random — детерминировано.
        /// </summary>
        /// <param name="hqPos">Позиция HQ игрока.</param>
        /// <param name="flankWaypoints">Точки флангового обхода. null или пустой → всегда HQ.</param>
        /// <param name="flankProbability">0..1 шанс флангового маршрута.</param>
        /// <param name="seed">Детерминированный сид (например, номер волны * 1000 + matchTime*10).</param>
        public static Vector3 GetWaveTarget(
            Vector3   hqPos,
            Vector3[] flankWaypoints,
            float     flankProbability,
            int       seed)
        {
            // Нет точек фланга — всегда HQ
            if (flankWaypoints == null || flankWaypoints.Length == 0)
                return hqPos;

            // Probability == 0 → всегда HQ
            if (flankProbability <= 0f)
                return hqPos;

            // MurmurHash3 finalizer — тот же avalanche, что в CombatLogic
            uint h = (uint)seed;
            h ^= h >> 16;
            h *= 0x85ebca6bu;
            h ^= h >> 13;
            h *= 0xc2b2ae35u;
            h ^= h >> 16;

            // Нормализуем в [0, 100) для сравнения с вероятностью
            int percentRoll = (int)(h % 100u);
            int threshold   = Mathf.RoundToInt(flankProbability * 100f);

            if (percentRoll >= threshold)
                return hqPos; // хэш не попал во фланк-окно

            // Выбираем точку фланга детерминировано по хэшу
            // Используем второй проход хэша для выбора индекса
            h ^= h >> 16;
            h *= 0x85ebca6bu;
            h ^= h >> 13;
            int idx = (int)(h % (uint)flankWaypoints.Length);
            return flankWaypoints[idx];
        }

        /// <summary>
        /// Одноразовый триггер экстренной волны.
        /// Возвращает true только тогда, когда timer истёк (≤ 0f), но ещё не помечен
        /// sentinel-значением (-1f). Sentinel-окно: (-1f, 0f].
        /// После срабатывания вызывающий обязан установить timer = -1f.
        /// </summary>
        /// <param name="timer">Значение таймера. Sentinel = -1f.</param>
        public static bool ShouldEmergencyWave(float timer)
        {
            // Истёк (≤ 0) И не в sentinel-состоянии (> -1f)
            return timer <= 0f && timer > -1f;
        }

        /// <summary>
        /// Выбирает индекс записи в таблице производства здания (ProductionEntries).
        /// infantryCount — количество пехотных юнитов (AoeRadius == 0).
        /// tankCount     — количество танков (AoeRadius &gt; 0).
        /// infantryRatio — соотношение пехота/всё: если пехоты меньше infantryRatio на каждого танка → index 0 (пехота).
        /// Если пехоты уже достаточно → index 1 (вариант с AoE / танк).
        /// </summary>
        public static int PickProductionEntryIndex(int infantryCount, int tankCount, int infantryRatio = 3)
        {
            int total = infantryCount + tankCount;
            if (total == 0) return 0;   // нет юнитов — берём пехоту

            // Если у нас меньше infantryRatio пехотинцев на каждого танка — докупаем пехоту
            // Граница: infantry < infantryRatio * (tankCount + 1)
            // т.е. хотим infantry / (tankCount + 1) >= infantryRatio
            if (infantryCount < infantryRatio * (tankCount + 1))
                return 0;

            return 1;
        }
    }
}
