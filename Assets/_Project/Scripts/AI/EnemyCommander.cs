using System;
using System.Collections.Generic;
using DiplomaGame.Runtime.Buildings;
using DiplomaGame.Runtime.Data;
using DiplomaGame.Runtime.Economy;
using DiplomaGame.Runtime.Tech;
using DiplomaGame.Runtime.Units;
using UnityEngine;
using UnityEngine.AI;

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
        // Статическое событие
        // ----------------------------------------------------------------

        /// <summary>
        /// Вызывается при старте каждой волны атаки.
        /// Статическое — не требует ссылки на конкретный инстанс.
        /// </summary>
        public static event Action WaveLaunched;
        // ----------------------------------------------------------------
        // Сериализованные поля
        // ----------------------------------------------------------------

        [SerializeField] private ResourceBank       _bank;
        [SerializeField] private ProductionBuilding _enemyBarracks;
        [SerializeField] private ProductionBuilding _enemyWarFactory;
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

        // Кэш Building-компонентов производственных зданий (заполняется в Start,
        // избавляет от GetComponent внутри TryProduceFrom на каждом тике решений).
        private Building          _cachedBarracksBuilding;
        private Building          _cachedWarFactoryBuilding;

        // Буферы без аллокаций
        private readonly List<Unit>     _enemyUnitBuffer = new List<Unit>(32);
        private readonly List<Building> _buildingBuffer  = new List<Building>(16);

        // ----------------------------------------------------------------
        // Unity lifecycle
        // ----------------------------------------------------------------

        private void Start()
        {
            CachePlayerHQ();
            CacheProductionBuildings();
        }

        private void CacheProductionBuildings()
        {
            _cachedBarracksBuilding   = _enemyBarracks   != null ? _enemyBarracks.GetComponent<Building>()   : null;
            _cachedWarFactoryBuilding = _enemyWarFactory != null ? _enemyWarFactory.GetComponent<Building>() : null;
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
        internal void InitForTest(
            ResourceBank       bank,
            ProductionBuilding barracks,
            float              decisionInterval = 0.5f,
            ProductionBuilding warFactory       = null)
        {
            _bank              = bank;
            _enemyBarracks     = barracks;
            _enemyWarFactory   = warFactory;
            _decisionInterval  = decisionInterval;

            // Кэшируем Building-компоненты сразу — Start() в тестах может вызваться позже
            CacheProductionBuildings();
        }

        // ----------------------------------------------------------------
        // Принятие решений
        // ----------------------------------------------------------------

        private void MakeDecisions()
        {
            DecideProduction();
            DecideResearch();
            DecideWave();
        }

        private void DecideProduction()
        {
            // Считаем живых юнитов противника один раз для обоих зданий
            UnitRegistry.GetUnits(Faction.Enemy, _enemyUnitBuffer);
            int currentUnits = _enemyUnitBuffer.Count;

            // Считаем пехоту (AoeRadius == 0) и танки (AoeRadius > 0) для выбора entry
            int infantryCount = 0;
            int tankCount     = 0;
            for (int i = 0; i < _enemyUnitBuffer.Count; i++)
            {
                var u = _enemyUnitBuffer[i];
                if (u == null) continue;
                var combat = u.CachedCombat;
                if (combat == null) continue;

                // UnitCombat.Data теперь открыт (read-only) — без GetComponent в горячем пути
                var unitData = combat.Data;
                if (unitData != null && unitData.AoeRadius > 0f)
                    tankCount++;
                else
                    infantryCount++;
            }

            // Building кэшируем один раз в Start — передаём готовую ссылку
            TryProduceFrom(_enemyBarracks,   _cachedBarracksBuilding,   currentUnits, infantryCount, tankCount);
            TryProduceFrom(_enemyWarFactory, _cachedWarFactoryBuilding,  currentUnits, infantryCount, tankCount);
        }

        private void TryProduceFrom(ProductionBuilding building, Building buildingComp, int currentUnits,
                                     int infantryCount, int tankCount)
        {
            if (_bank == null || building == null || buildingComp == null) return;
            if (buildingComp.Data == null) return;

            int balance = _bank.GetBalance(Faction.Enemy);

            if (buildingComp.Data.HasMultiProduction)
            {
                // Multi-production: выбираем entry по соотношению пехоты/танков
                var entries  = buildingComp.Data.ProductionEntries;
                int entryIdx = EnemyWaveLogic.PickProductionEntryIndex(infantryCount, tankCount);
                // Клипуем индекс в допустимый диапазон
                entryIdx = Mathf.Clamp(entryIdx, 0, entries.Length - 1);
                var entry = entries[entryIdx];

                if (EnemyWaveLogic.ShouldProduce(balance, entry.cost, currentUnits, MaxUnits))
                    building.TryEnqueue(entry);
            }
            else
            {
                // Legacy
                int unitCost = buildingComp.Data.ProductionCost;
                if (EnemyWaveLogic.ShouldProduce(balance, unitCost, currentUnits, MaxUnits))
                    building.TryEnqueue();
            }
        }

        private void DecideResearch()
        {
            if (_bank == null || _enemyBarracks == null || _cachedBarracksBuilding == null) return;

            var data = _cachedBarracksBuilding.Data;
            if (data == null || !data.HasTechEntries) return;

            int balance = _bank.GetBalance(Faction.Enemy);

            var techEntries = data.TechEntries;
            for (int i = 0; i < techEntries.Length; i++)
            {
                var techEntry = techEntries[i];
                if (techEntry == null || techEntry.techData == null) continue;

                var tech = techEntry.techData;

                // Проверяем что можно исследовать и хватает денег
                if (!TechRegistry.Instance.CanResearch(Faction.Enemy, tech)) continue;
                if (!EnemyWaveLogic.ShouldResearch(balance, tech.Cost)) continue;

                // Проверяем, не полна ли очередь
                if (_enemyBarracks.QueueCount >= 5) break;

                // Создаём синтетический ProductionEntry для исследования
                var entry = new ProductionEntry
                {
                    techData       = tech,
                    cost           = tech.Cost,
                    productionTime = tech.ResearchTime,
                    icon           = tech.Icon,
                    hotkeyLabel    = tech.HotkeyLabel,
                    unitData       = null,
                };

                if (_enemyBarracks.TryEnqueue(entry))
                    break; // одно исследование за цикл решений
            }
        }

        private void DecideWave()
        {
            // Собираем юниты противника вне боя: Idle ИЛИ Moving (к rally).
            // Строгое требование Idle давало «голодание волн»: в толпе у rally-точки
            // юнит не достигает её точно (remainingDistance > stoppingDistance из-за
            // соседей), вечно остаётся Moving и никогда не призывался — при 37 юнитах
            // на базе волны переставали формироваться (chokePassCount=1 за весь матч;
            // вскрыто SceneIntegration FullMatch). Командир забирает в волну всех,
            // кто не в бою — как реальный приказ армии.
            UnitRegistry.GetUnits(Faction.Enemy, _enemyUnitBuffer);

            int idleCount = 0;
            for (int i = 0; i < _enemyUnitBuffer.Count; i++)
            {
                var u = _enemyUnitBuffer[i];
                if (u == null) continue;

                if (IsDraftable(u))
                    idleCount++;
            }

            int waveSize = EnemyWaveLogic.GetWaveSizeForTime(_matchTime);

            if (!EnemyWaveLogic.ShouldLaunchWave(idleCount, waveSize, TimeSinceLastWave, 30f))
                return;

            // Обновляем кэш HQ — может быть уничтожен
            if (_playerHQ == null || !_playerHQ.gameObject.activeInHierarchy)
                CachePlayerHQ();

            if (_playerHQ == null) return; // HQ игрока нет — не атаковать

            // Используем ближайшую к HQ точку на NavMesh как цель атаки.
            // HQ имеет NavMeshObstacle с carve (extents 0.5 × scale 4 = 2м в мировых единицах),
            // поэтому его центр вырезан из NavMesh и NavMeshAgent не может туда добраться напрямую.
            // SamplePosition с радиусом 10м с запасом находит точку у периметра здания.
            Vector3 rawHQPos  = _playerHQ.transform.position;
            Vector3 targetPos = rawHQPos;
            if (NavMesh.SamplePosition(rawHQPos, out var navHit, 10f, NavMesh.AllAreas))
            {
                targetPos = navHit.position;
            }

            // Отправляем всех призываемых (вне боя) юнитов в атаку
            for (int i = 0; i < _enemyUnitBuffer.Count; i++)
            {
                var u = _enemyUnitBuffer[i];
                if (u == null) continue;

                if (IsDraftable(u))
                    u.IssueCommand(UnitCommand.AttackMove(targetPos));
            }

            TimeSinceLastWave = 0f;

            // Уведомляем подписчиков (AudioManager и другие) о старте волны
            WaveLaunched?.Invoke();
        }

        /// <summary>
        /// Юнит призываем в волну, если он не в бою и не отступает:
        /// Idle ИЛИ Moving (к rally / в прежней волне). Уже-AttackMove-юниты
        /// получают повторный приказ на ту же цель — безвредно.
        /// </summary>
        private static bool IsDraftable(Unit u)
        {
            if (u.CurrentState == UnitState.Holding) return false;

            var combat = u.CachedCombat;
            return combat == null || combat.CurrentCombatState == CombatState.None;
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
