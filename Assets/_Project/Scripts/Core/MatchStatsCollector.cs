using System.Collections;
using System.Collections.Generic;
using DiplomaGame.Runtime.Buildings;
using DiplomaGame.Runtime.Combat;
using DiplomaGame.Runtime.Economy;
using DiplomaGame.Runtime.Hero;
using DiplomaGame.Runtime.Units;
using UnityEngine;

namespace DiplomaGame.Runtime.Core
{
    /// <summary>
    /// Собирает статистику матча, подписываясь на игровые события.
    /// Размещается на GameManagers. Свойство Stats доступно из GameWatcher.
    /// </summary>
    public sealed class MatchStatsCollector : MonoBehaviour
    {
        // ----------------------------------------------------------------
        // Зависимости
        // ----------------------------------------------------------------

        [SerializeField] private ResourceBank _bank;

        // ----------------------------------------------------------------
        // Состояние
        // ----------------------------------------------------------------

        private readonly MatchStats _stats = new MatchStats();

        private float _startTime;

        // Буфер без аллокаций для опроса UnitRegistry
        private readonly List<Unit> _unitBuffer = new List<Unit>(64);

        private const float ArmyPeakInterval = 0.5f;

        // ----------------------------------------------------------------
        // Публичный API
        // ----------------------------------------------------------------

        /// <summary>Накопленная статистика матча.</summary>
        public MatchStats Stats => _stats;

        // ----------------------------------------------------------------
        // Unity lifecycle
        // ----------------------------------------------------------------

        private void Start()
        {
            _startTime = Time.time;

            // ResourceBank — ищем если не проставлен через SerializedObject
            if (_bank == null)
                _bank = UnityEngine.Object.FindFirstObjectByType<ResourceBank>();

            SubscribeAll();

            StartCoroutine(ArmyPeakRoutine());
        }

        private void OnDestroy()
        {
            UnsubscribeAll();
        }

        // ----------------------------------------------------------------
        // Подписки
        // ----------------------------------------------------------------

        private void SubscribeAll()
        {
            Health.AnyDied    += OnAnyDied;
            Health.AnyDamaged += OnAnyDamaged;

            if (_bank != null)
                _bank.CrystalsMined += OnCrystalsMined;

            // Подписываемся на все ProductionBuilding в сцене
            var buildings = UnityEngine.Object.FindObjectsByType<ProductionBuilding>(
                FindObjectsSortMode.None);
            foreach (var pb in buildings)
                pb.UnitProduced += OnUnitProduced;
        }

        private void UnsubscribeAll()
        {
            Health.AnyDied    -= OnAnyDied;
            Health.AnyDamaged -= OnAnyDamaged;

            if (_bank != null)
                _bank.CrystalsMined -= OnCrystalsMined;

            // Отписываемся от ProductionBuilding (они могут уже быть уничтожены)
            var buildings = UnityEngine.Object.FindObjectsByType<ProductionBuilding>(
                FindObjectsSortMode.None);
            foreach (var pb in buildings)
                pb.UnitProduced -= OnUnitProduced;
        }

        // ----------------------------------------------------------------
        // Обработчики событий
        // ----------------------------------------------------------------

        private void OnAnyDied(Health health)
        {
            // Смерть героя не считается потерей юнита
            if (health.GetComponent<HeroController>() != null) return;

            // Только юниты (не здания) учитываются в Kill/Lost
            var unit = health.GetComponent<Unit>();
            if (unit == null) return;

            _stats.RecordKill(unit.Faction);
        }

        private void OnAnyDamaged(Health health, float amount)
        {
            // Определяем фракцию жертвы — сначала Unit, затем Building
            var unit = health.GetComponent<Unit>();
            if (unit != null)
            {
                _stats.RecordDamage(unit.Faction, amount);
                return;
            }

            var building = health.GetComponent<Building>();
            if (building != null)
            {
                // Урон зданиям учитывается в Damage, но не в Kill (RecordKill не вызывается здесь)
                _stats.RecordDamage(building.Faction, amount);
            }
        }

        private void OnCrystalsMined(Faction faction, int amount)
        {
            _stats.RecordMined(faction, amount);
        }

        private void OnUnitProduced(Unit unit)
        {
            if (unit == null) return;
            _stats.RecordProduced(unit.Faction);
        }

        // ----------------------------------------------------------------
        // Пик армии (раз в 0.5с)
        // ----------------------------------------------------------------

        private IEnumerator ArmyPeakRoutine()
        {
            var wait = new WaitForSeconds(ArmyPeakInterval);

            while (true)
            {
                UnitRegistry.GetUnits(Faction.Player, _unitBuffer);
                _stats.UpdateArmyPeak(Faction.Player, _unitBuffer.Count);

                UnitRegistry.GetUnits(Faction.Enemy, _unitBuffer);
                _stats.UpdateArmyPeak(Faction.Enemy, _unitBuffer.Count);

                yield return wait;
            }
        }

        // ----------------------------------------------------------------
        // Фиксация длительности
        // ----------------------------------------------------------------

        /// <summary>
        /// Фиксирует длительность матча. Вызывается из GameWatcher при конце матча.
        /// </summary>
        public void FinalizeStats()
        {
            _stats.SetDuration(Time.time - _startTime);
        }
    }
}
