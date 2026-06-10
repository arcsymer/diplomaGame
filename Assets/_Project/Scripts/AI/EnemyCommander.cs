using System.Collections.Generic;
using DiplomaGame.Runtime.Buildings;
using DiplomaGame.Runtime.Data;
using DiplomaGame.Runtime.Economy;
using DiplomaGame.Runtime.Units;
using UnityEngine;

namespace DiplomaGame.Runtime.AI
{
    /// <summary>
    /// Мозг ИИ-противника. Раз в decisionInterval секунд принимает решения:
    /// 1) производить юнитов, пока позволяют ресурсы и лимит;
    /// 2) отправлять волну атаки на HQ игрока.
    /// Размещается на GameManagers.
    /// </summary>
    public sealed class EnemyCommander : MonoBehaviour
    {
        // ----------------------------------------------------------------
        // Сериализованные поля
        // ----------------------------------------------------------------

        [SerializeField] private ResourceBank       _bank;
        [SerializeField] private ProductionBuilding _enemyBarracks;
        [SerializeField] private float              _decisionInterval = 2f;

        // Лимит юнитов противника
        private const int MaxUnits = 12;

        // ----------------------------------------------------------------
        // Публичные свойства (для тестов / HUD)
        // ----------------------------------------------------------------

        /// <summary>Количество живых боевых юнитов противника.</summary>
        public int UnitsAlive => _enemyUnitBuffer.Count;

        /// <summary>Время (сек) с последней запущенной волны.</summary>
        public float TimeSinceLastWave { get; private set; }

        // ----------------------------------------------------------------
        // Внутреннее состояние
        // ----------------------------------------------------------------

        private float             _decisionTimer;
        private float             _matchTime;

        // Кэш HQ игрока — обновляется при смерти
        private Building          _playerHQ;

        // Буферы без аллокаций
        private readonly List<Unit>     _enemyUnitBuffer = new List<Unit>(32);
        private readonly List<Building> _buildingBuffer  = new List<Building>(16);

        // ----------------------------------------------------------------
        // Unity lifecycle
        // ----------------------------------------------------------------

        private void Start()
        {
            CachePlayerHQ();
        }

        private void Update()
        {
            _matchTime         += Time.deltaTime;
            TimeSinceLastWave  += Time.deltaTime;
            _decisionTimer     += Time.deltaTime;

            if (_decisionTimer < _decisionInterval) return;
            _decisionTimer = 0f;

            MakeDecisions();
        }

        // ----------------------------------------------------------------
        // Internal — для тестов
        // ----------------------------------------------------------------

        /// <summary>
        /// Инициализирует компонент для PlayMode-тестов без SerializedObject.
        /// </summary>
        internal void InitForTest(ResourceBank bank, ProductionBuilding barracks, float decisionInterval = 0.5f)
        {
            _bank              = bank;
            _enemyBarracks     = barracks;
            _decisionInterval  = decisionInterval;
        }

        // ----------------------------------------------------------------
        // Принятие решений
        // ----------------------------------------------------------------

        private void MakeDecisions()
        {
            DecideProduction();
            DecideWave();
        }

        private void DecideProduction()
        {
            if (_bank == null || _enemyBarracks == null) return;

            var buildingComp = _enemyBarracks.GetComponent<Building>();
            if (buildingComp == null || buildingComp.Data == null) return;

            int balance   = _bank.GetBalance(Faction.Enemy);
            int unitCost  = buildingComp.Data.ProductionCost;

            // Считаем живых юнитов противника
            UnitRegistry.GetUnits(Faction.Enemy, _enemyUnitBuffer);
            int currentUnits = _enemyUnitBuffer.Count;

            if (EnemyWaveLogic.ShouldProduce(balance, unitCost, currentUnits, MaxUnits))
                _enemyBarracks.TryEnqueue();
        }

        private void DecideWave()
        {
            // Собираем юниты противника в состоянии Idle/None
            UnitRegistry.GetUnits(Faction.Enemy, _enemyUnitBuffer);

            int idleCount = 0;
            for (int i = 0; i < _enemyUnitBuffer.Count; i++)
            {
                var u = _enemyUnitBuffer[i];
                if (u == null) continue;

                bool isIdle = u.CurrentState == UnitState.Idle;

                // Проверяем боевой статус (UnitCombat может отсутствовать)
                var combat = u.GetComponent<UnitCombat>();
                bool combatIdle = combat == null || combat.CurrentCombatState == CombatState.None;

                if (isIdle && combatIdle)
                    idleCount++;
            }

            int waveSize = EnemyWaveLogic.GetWaveSizeForTime(_matchTime);

            if (!EnemyWaveLogic.ShouldLaunchWave(idleCount, waveSize, TimeSinceLastWave, 30f))
                return;

            // Обновляем кэш HQ — может быть уничтожен
            if (_playerHQ == null || !_playerHQ.gameObject.activeInHierarchy)
                CachePlayerHQ();

            if (_playerHQ == null) return; // HQ игрока нет — не атаковать

            Vector3 targetPos = _playerHQ.transform.position;

            // Отправляем всех idle-юнитов в атаку
            for (int i = 0; i < _enemyUnitBuffer.Count; i++)
            {
                var u = _enemyUnitBuffer[i];
                if (u == null) continue;

                bool isIdle    = u.CurrentState == UnitState.Idle;
                var  combat    = u.GetComponent<UnitCombat>();
                bool combatIdle = combat == null || combat.CurrentCombatState == CombatState.None;

                if (isIdle && combatIdle)
                    u.IssueCommand(UnitCommand.AttackMove(targetPos));
            }

            TimeSinceLastWave = 0f;
        }

        private void CachePlayerHQ()
        {
            BuildingRegistry.GetBuildings(Faction.Player, _buildingBuffer);
            _playerHQ = null;

            for (int i = 0; i < _buildingBuffer.Count; i++)
            {
                var b = _buildingBuffer[i];
                if (b == null || b.Data == null) continue;
                if (b.Data.BuildingType == BuildingType.Headquarters)
                {
                    _playerHQ = b;
                    break;
                }
            }
        }
    }
}
