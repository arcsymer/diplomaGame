using System;
using System.Collections.Generic;
using DiplomaGame.Runtime.Data;
using DiplomaGame.Runtime.Units;
using UnityEngine;

namespace DiplomaGame.Runtime.Tech
{
    /// <summary>
    /// Реестр исследованных технологий. Синглтон с ручным сбросом через
    /// [RuntimeInitializeOnLoadMethod(SubsystemRegistration)] — паттерн из UnitCombat.
    /// Без аллокаций в горячих путях чтения множителей (кэши инвалидируются при MarkResearched).
    /// </summary>
    public sealed class TechRegistry
    {
        // ----------------------------------------------------------------
        // Синглтон
        // ----------------------------------------------------------------

        private static TechRegistry _instance;

        /// <summary>Текущий экземпляр реестра. Создаётся лениво при первом обращении.</summary>
        public static TechRegistry Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new TechRegistry();
                return _instance;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetInstance()
        {
            _instance = null;
        }

        // ----------------------------------------------------------------
        // Данные: per-faction множества исследованных технологий
        // ----------------------------------------------------------------

        private readonly HashSet<TechData> _playerResearched = new HashSet<TechData>();
        private readonly HashSet<TechData> _enemyResearched  = new HashSet<TechData>();

        // ----------------------------------------------------------------
        // Кэши множителей (per-faction, два набора: пехота / не-пехота)
        // ----------------------------------------------------------------

        // Player
        private float _playerDmgInfantry;
        private float _playerDmgTank;
        private float _playerHpInfantry;
        private float _playerHpTank;
        private float _playerCdInfantry;
        private float _playerCdTank;
        private bool  _playerCacheValid;

        // Enemy
        private float _enemyDmgInfantry;
        private float _enemyDmgTank;
        private float _enemyHpInfantry;
        private float _enemyHpTank;
        private float _enemyCdInfantry;
        private float _enemyCdTank;
        private bool  _enemyCacheValid;

        // ----------------------------------------------------------------
        // События
        // ----------------------------------------------------------------

        /// <summary>Вызывается после того, как технология помечена как исследованная.</summary>
        public static event Action<Faction, TechData> TechResearched;

        // ----------------------------------------------------------------
        // Конструктор (приватный — только Instance)
        // ----------------------------------------------------------------

        private TechRegistry() { }

        // ----------------------------------------------------------------
        // Основные методы
        // ----------------------------------------------------------------

        /// <summary>Возвращает true, если технология исследована для указанной фракции.</summary>
        public bool IsResearched(Faction faction, TechData tech)
        {
            if (tech == null) return false;
            return GetSet(faction).Contains(tech);
        }

        /// <summary>
        /// Возвращает true, если технология может быть исследована:
        /// все Prerequisites уже исследованы и сама технология ещё не исследована.
        /// </summary>
        public bool CanResearch(Faction faction, TechData tech)
        {
            if (tech == null) return false;
            if (IsResearched(faction, tech)) return false;

            var prereqs = tech.Prerequisites;
            if (prereqs != null)
            {
                for (int i = 0; i < prereqs.Length; i++)
                {
                    if (prereqs[i] != null && !IsResearched(faction, prereqs[i]))
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Помечает технологию как исследованную для фракции.
        /// Инвалидирует кэш множителей и вызывает событие TechResearched.
        /// </summary>
        public void MarkResearched(Faction faction, TechData tech)
        {
            if (tech == null) return;

            GetSet(faction).Add(tech);
            InvalidateCache(faction);

            TechResearched?.Invoke(faction, tech);
        }

        // ----------------------------------------------------------------
        // Множители (горячий путь боя — кэшированные суммы)
        // ----------------------------------------------------------------

        /// <summary>
        /// Сумма EffectMagnitude всех применимых DamageMultiplier-технологий для фракции.
        /// Учитывает InfantryOnly: пехота (AoeRadius == 0) и танки — разные наборы.
        /// </summary>
        public float GetDamageMultiplier(Faction faction, UnitData unit)
        {
            EnsureCache(faction);
            bool infantry = IsInfantry(unit);

            if (faction == Faction.Player)
                return infantry ? _playerDmgInfantry : _playerDmgTank;
            else
                return infantry ? _enemyDmgInfantry  : _enemyDmgTank;
        }

        /// <summary>
        /// Сумма EffectMagnitude всех применимых MaxHpMultiplier-технологий для фракции.
        /// </summary>
        public float GetMaxHpMultiplier(Faction faction, UnitData unit)
        {
            EnsureCache(faction);
            bool infantry = IsInfantry(unit);

            if (faction == Faction.Player)
                return infantry ? _playerHpInfantry : _playerHpTank;
            else
                return infantry ? _enemyHpInfantry  : _enemyHpTank;
        }

        /// <summary>
        /// Сумма EffectMagnitude всех применимых AttackCooldownMultiplier-технологий для фракции.
        /// Для кулдауна значение отрицательное (−0.15 = −15%).
        /// </summary>
        public float GetCooldownMultiplier(Faction faction, UnitData unit)
        {
            EnsureCache(faction);
            bool infantry = IsInfantry(unit);

            if (faction == Faction.Player)
                return infantry ? _playerCdInfantry : _playerCdTank;
            else
                return infantry ? _enemyCdInfantry  : _enemyCdTank;
        }

        // ----------------------------------------------------------------
        // Вспомогательные методы
        // ----------------------------------------------------------------

        private HashSet<TechData> GetSet(Faction faction)
        {
            return faction == Faction.Player ? _playerResearched : _enemyResearched;
        }

        private static bool IsInfantry(UnitData unit)
        {
            return unit == null || unit.AoeRadius == 0f;
        }

        private void InvalidateCache(Faction faction)
        {
            if (faction == Faction.Player)
                _playerCacheValid = false;
            else
                _enemyCacheValid = false;
        }

        private void EnsureCache(Faction faction)
        {
            if (faction == Faction.Player)
            {
                if (!_playerCacheValid)
                {
                    RebuildCache(faction,
                        out _playerDmgInfantry, out _playerDmgTank,
                        out _playerHpInfantry,  out _playerHpTank,
                        out _playerCdInfantry,  out _playerCdTank);
                    _playerCacheValid = true;
                }
            }
            else
            {
                if (!_enemyCacheValid)
                {
                    RebuildCache(faction,
                        out _enemyDmgInfantry, out _enemyDmgTank,
                        out _enemyHpInfantry,  out _enemyHpTank,
                        out _enemyCdInfantry,  out _enemyCdTank);
                    _enemyCacheValid = true;
                }
            }
        }

        private void RebuildCache(Faction faction,
            out float dmgInfantry, out float dmgTank,
            out float hpInfantry,  out float hpTank,
            out float cdInfantry,  out float cdTank)
        {
            dmgInfantry = 0f; dmgTank = 0f;
            hpInfantry  = 0f; hpTank  = 0f;
            cdInfantry  = 0f; cdTank  = 0f;

            var set = GetSet(faction);
            foreach (var tech in set)
            {
                if (tech == null) continue;

                float mag = tech.EffectMagnitude;

                switch (tech.EffectType)
                {
                    case TechEffect.DamageMultiplier:
                        dmgInfantry += mag;                       // пехота всегда
                        if (!tech.InfantryOnly) dmgTank += mag;   // не-пехота — только если не InfantryOnly
                        break;

                    case TechEffect.MaxHpMultiplier:
                        hpInfantry += mag;
                        if (!tech.InfantryOnly) hpTank += mag;
                        break;

                    case TechEffect.AttackCooldownMultiplier:
                        cdInfantry += mag;
                        if (!tech.InfantryOnly) cdTank += mag;
                        break;
                }
            }
        }
    }
}
