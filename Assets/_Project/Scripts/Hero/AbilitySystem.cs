using System;
using DiplomaGame.Runtime.Core;
using DiplomaGame.Runtime.Data;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DiplomaGame.Runtime.Hero
{
    /// <summary>
    /// Система способностей героя: 4 слота, кулдауны, применение эффектов.
    /// Активна только в TPS-режиме.
    /// </summary>
    public sealed class AbilitySystem : MonoBehaviour
    {
        [SerializeField] private AbilityData[]      abilities       = new AbilityData[4];
        [SerializeField] private GameModeController modeController;
        [SerializeField] private InputActionAsset   actions;
        [SerializeField] private HeroController     heroController;

        // ----------------------------------------------------------------
        // Событие (HUD-шина — M6)
        // ----------------------------------------------------------------

        /// <summary>Вызывается при успешном применении способности.</summary>
        public event Action<int, AbilityData> AbilityCast;

        // ----------------------------------------------------------------
        // Состояние кулдаунов (без аллокаций)
        // ----------------------------------------------------------------

        private readonly float[] _cooldowns = new float[4];

        // ----------------------------------------------------------------
        // Input actions (кэш)
        // ----------------------------------------------------------------

        private readonly InputAction[]                              _abilityActions   = new InputAction[4];
        private readonly Action<InputAction.CallbackContext>[]      _abilityDelegates = new Action<InputAction.CallbackContext>[4];

        // ----------------------------------------------------------------
        // Unity lifecycle
        // ----------------------------------------------------------------

        private void Awake()
        {
            // Создаём делегаты один раз — для корректной отписки в OnDisable
            for (int i = 0; i < 4; i++)
            {
                int captured = i;
                _abilityDelegates[i] = _ => OnAbilityPerformed(captured);
            }

            if (actions != null)
            {
                var tpsMap = actions.FindActionMap("TPS");
                if (tpsMap != null)
                {
                    _abilityActions[0] = tpsMap.FindAction("Ability1");
                    _abilityActions[1] = tpsMap.FindAction("Ability2");
                    _abilityActions[2] = tpsMap.FindAction("Ability3");
                    _abilityActions[3] = tpsMap.FindAction("Ability4");
                }
            }
        }

        private void OnEnable()
        {
            for (int i = 0; i < 4; i++)
            {
                if (_abilityActions[i] != null)
                    _abilityActions[i].performed += _abilityDelegates[i];
            }
        }

        private void OnDisable()
        {
            for (int i = 0; i < 4; i++)
            {
                if (_abilityActions[i] != null)
                    _abilityActions[i].performed -= _abilityDelegates[i];
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < 4; i++)
                _cooldowns[i] = AbilityCooldownLogic.Tick(_cooldowns[i], dt);
        }

        // ----------------------------------------------------------------
        // Публичный API (для HUD — M6)
        // ----------------------------------------------------------------

        /// <summary>Возвращает оставшийся кулдаун слота i (0..3). 0 → готова.</summary>
        public float GetRemainingCooldown(int index)
        {
            if (index < 0 || index >= 4) return 0f;
            return _cooldowns[index];
        }

        /// <summary>Возвращает AbilityData для слота i (может быть null, если слот пуст).</summary>
        public AbilityData GetAbility(int index)
        {
            if (index < 0 || index >= abilities.Length) return null;
            return abilities[index];
        }

        /// <summary>
        /// Возвращает полное время кулдауна слота i (0..3).
        /// Берётся из AbilityData.Cooldown; 0 если слот пуст.
        /// </summary>
        public float GetCooldownDuration(int index)
        {
            var data = GetAbility(index);
            return data != null ? data.Cooldown : 0f;
        }

        // ----------------------------------------------------------------
        // Internal — для PlayMode-тестов
        // ----------------------------------------------------------------

        /// <summary>
        /// Инициализация для тестов: подставляет зависимости без InputActionAsset.
        /// </summary>
        internal void InitForTest(GameModeController controller, HeroController hero, AbilityData[] testAbilities)
        {
            modeController = controller;
            heroController = hero;
            actions        = null;

            if (testAbilities != null)
            {
                int count = Mathf.Min(testAbilities.Length, 4);
                for (int i = 0; i < count; i++)
                    abilities[i] = testAbilities[i];
            }
        }

        /// <summary>
        /// Попытка применить способность из слота i напрямую — для PlayMode-тестов.
        /// </summary>
        internal bool TryCast(int index)
        {
            return TryCastInternal(index);
        }

        // ----------------------------------------------------------------
        // Приватные методы
        // ----------------------------------------------------------------

        private void OnAbilityPerformed(int index)
        {
            TryCastInternal(index);
        }

        private bool TryCastInternal(int index)
        {
            if (index < 0 || index >= 4)
                return false;

            if (modeController == null || modeController.CurrentMode != GameMode.Tps)
                return false;

            if (abilities[index] == null)
                return false;

            if (!AbilityCooldownLogic.IsReady(_cooldowns[index]))
                return false;

            ApplyEffect(index, abilities[index]);
            _cooldowns[index] = AbilityCooldownLogic.StartCooldown(abilities[index].Cooldown);
            AbilityCast?.Invoke(index, abilities[index]);

            return true;
        }

        private void ApplyEffect(int index, AbilityData data)
        {
            switch (data.AbilityType)
            {
                case AbilityType.Dash:
                    if (heroController != null)
                        heroController.Dash(data.DashDistance);
                    break;

                default:
                    Debug.Log($"[AbilitySystem] Способность '{data.DisplayName}' (слот {index}) применена — placeholder.");
                    break;
            }
        }
    }
}
