using System;
using DiplomaGame.Runtime.Units;
using UnityEngine;

namespace DiplomaGame.Runtime.Economy
{
    /// <summary>
    /// MonoBehaviour-хранилище балансов обеих фракций (один на сцену, на GameManagers).
    /// Потокобезопасность не нужна — всё в главном потоке Unity.
    /// </summary>
    public sealed class ResourceBank : MonoBehaviour
    {
        [SerializeField] private int _startingCrystals = 150;

        // ----------------------------------------------------------------
        // Состояние
        // ----------------------------------------------------------------

        private readonly int[] _balances = new int[2];

        // ----------------------------------------------------------------
        // Публичный API
        // ----------------------------------------------------------------

        /// <summary>Вызывается при любом изменении баланса фракции.</summary>
        public event Action<Faction, int> BalanceChanged;

        // ----------------------------------------------------------------
        // Unity lifecycle
        // ----------------------------------------------------------------

        private void Awake()
        {
            _balances[(int)Faction.Player] = _startingCrystals;
            _balances[(int)Faction.Enemy]  = _startingCrystals;
        }

        // ----------------------------------------------------------------
        // Операции
        // ----------------------------------------------------------------

        /// <summary>Возвращает текущий баланс фракции.</summary>
        public int GetBalance(Faction faction)
        {
            return _balances[(int)faction];
        }

        /// <summary>
        /// Пытается списать amount у фракции.
        /// Возвращает true и изменяет баланс, если средств достаточно.
        /// Возвращает false и не трогает баланс при нехватке.
        /// </summary>
        public bool TrySpend(Faction faction, int amount)
        {
            int current = _balances[(int)faction];
            if (!EconomyLogic.CanAfford(current, amount))
                return false;

            _balances[(int)faction] = EconomyLogic.Spend(current, amount);
            BalanceChanged?.Invoke(faction, _balances[(int)faction]);
            return true;
        }

        /// <summary>Добавляет amount к балансу фракции (отрицательные значения игнорируются).</summary>
        public void Add(Faction faction, int amount)
        {
            if (amount <= 0) return;

            _balances[(int)faction] += amount;
            BalanceChanged?.Invoke(faction, _balances[(int)faction]);
        }

        // ----------------------------------------------------------------
        // Internal — для тестов
        // ----------------------------------------------------------------

        /// <summary>
        /// Устанавливает стартовый баланс и сбрасывает оба счёта.
        /// Используется в PlayMode-тестах вместо Awake.
        /// </summary>
        internal void InitForTest(int playerBalance, int enemyBalance)
        {
            _balances[(int)Faction.Player] = playerBalance;
            _balances[(int)Faction.Enemy]  = enemyBalance;
        }
    }
}
