using DiplomaGame.Runtime.Economy;
using DiplomaGame.Runtime.Units;
using TMPro;
using UnityEngine;

namespace DiplomaGame.Runtime.UI
{
    /// <summary>
    /// Отображает баланс кристаллов фракции Player.
    /// Подписывается на ResourceBank.BalanceChanged; обновляет TMP_Text без GC-аллокаций.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    public sealed class ResourceDisplay : MonoBehaviour
    {
        [SerializeField] private ResourceBank bank;
        [SerializeField] private UiPulse      pulse;   // опционально, для juice

        private TMP_Text _label;

        // ----------------------------------------------------------------
        // Unity lifecycle
        // ----------------------------------------------------------------

        private void Awake()
        {
            _label = GetComponent<TMP_Text>();
        }

        private void Start()
        {
            // Если bank не проставлен в Inspector — ищем в сцене
            if (bank == null)
                bank = Object.FindFirstObjectByType<ResourceBank>();

            if (bank != null)
            {
                bank.BalanceChanged += OnBalanceChanged;
                // Начальное отображение
                Refresh(bank.GetBalance(Faction.Player));
            }
        }

        private void OnDestroy()
        {
            if (bank != null)
                bank.BalanceChanged -= OnBalanceChanged;
        }

        // ----------------------------------------------------------------
        // Internal — для PlayMode-тестов
        // ----------------------------------------------------------------

        internal void InitForTest(ResourceBank testBank)
        {
            bank = testBank;
        }

        // ----------------------------------------------------------------
        // Приватные методы
        // ----------------------------------------------------------------

        private void OnBalanceChanged(Faction faction, int newBalance)
        {
            if (faction != Faction.Player) return;
            Refresh(newBalance);

            if (pulse != null)
                pulse.TriggerPulse();
        }

        private void Refresh(int amount)
        {
            if (_label == null) return;
            // SetText(string) с int-аргументом в TMP 3.x использует внутренний конвертер без аллокаций
            _label.SetText(HudLogic.FormatCrystals(amount));
        }
    }
}
