using DiplomaGame.Runtime.Units;

namespace DiplomaGame.Runtime.Core
{
    /// <summary>
    /// Модель статистики матча. Чистый C#-класс, без MonoBehaviour.
    /// Индексируется по (int)Faction: 0 = Player, 1 = Enemy.
    /// </summary>
    public sealed class MatchStats
    {
        // ----------------------------------------------------------------
        // Массивы [2] по (int)Faction
        // ----------------------------------------------------------------

        private readonly int[]   _unitsKilled   = new int[2];
        private readonly int[]   _unitsLost     = new int[2];
        private readonly float[] _damageDealt   = new float[2];
        private readonly float[] _damageTaken   = new float[2];
        private readonly int[]   _crystalsMined = new int[2];
        private readonly int[]   _unitsProduced = new int[2];
        private readonly int[]   _armyPeak      = new int[2];

        // ----------------------------------------------------------------
        // Публичный API — чтение
        // ----------------------------------------------------------------

        public int   UnitsKilled  (Faction f) => _unitsKilled  [(int)f];
        public int   UnitsLost    (Faction f) => _unitsLost    [(int)f];
        public float DamageDealt  (Faction f) => _damageDealt  [(int)f];
        public float DamageTaken  (Faction f) => _damageTaken  [(int)f];
        public int   CrystalsMined(Faction f) => _crystalsMined[(int)f];
        public int   UnitsProduced(Faction f) => _unitsProduced[(int)f];
        public int   ArmyPeak     (Faction f) => _armyPeak     [(int)f];

        /// <summary>Длительность матча в секундах.</summary>
        public float MatchDurationSeconds { get; private set; }

        // ----------------------------------------------------------------
        // Мутаторы
        // ----------------------------------------------------------------

        /// <summary>
        /// Регистрирует убийство. Убийцей считается фракция, противоположная жертве.
        /// </summary>
        public void RecordKill(Faction victim)
        {
            int killer = 1 - (int)victim;
            _unitsKilled[killer]++;
            _unitsLost  [(int)victim]++;
        }

        /// <summary>
        /// Регистрирует нанесённый урон. Жертве +DamageTaken, атакующему +DamageDealt.
        /// Нулевой или отрицательный урон игнорируется.
        /// </summary>
        public void RecordDamage(Faction victim, float amount)
        {
            if (amount <= 0f) return;

            int attacker = 1 - (int)victim;
            _damageTaken[(int)victim] += amount;
            _damageDealt[attacker]    += amount;
        }

        /// <summary>Добавляет добытые кристаллы фракции.</summary>
        public void RecordMined(Faction faction, int amount)
        {
            if (amount <= 0) return;
            _crystalsMined[(int)faction] += amount;
        }

        /// <summary>Увеличивает счётчик произведённых юнитов фракции на 1.</summary>
        public void RecordProduced(Faction faction)
        {
            _unitsProduced[(int)faction]++;
        }

        /// <summary>
        /// Обновляет пик армии фракции. Пик только растёт — уменьшение игнорируется.
        /// </summary>
        public void UpdateArmyPeak(Faction faction, int current)
        {
            if (current > _armyPeak[(int)faction])
                _armyPeak[(int)faction] = current;
        }

        /// <summary>Задаёт длительность матча (вызывается при завершении).</summary>
        public void SetDuration(float seconds)
        {
            MatchDurationSeconds = seconds;
        }

        /// <summary>Сбрасывает все счётчики в ноль.</summary>
        public void Reset()
        {
            for (int i = 0; i < 2; i++)
            {
                _unitsKilled  [i] = 0;
                _unitsLost    [i] = 0;
                _damageDealt  [i] = 0f;
                _damageTaken  [i] = 0f;
                _crystalsMined[i] = 0;
                _unitsProduced[i] = 0;
                _armyPeak     [i] = 0;
            }
            MatchDurationSeconds = 0f;
        }
    }
}
