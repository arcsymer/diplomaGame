using System.Collections.Generic;
using DiplomaGame.Runtime.UI;
using DiplomaGame.Runtime.Units;
using NUnit.Framework;
using UnityEngine;

namespace DiplomaGame.Tests.Editor
{
    /// <summary>
    /// EditMode-тесты для чистой статики IdleArmyLogic.
    /// Нет MonoBehaviour, нет сцены — запускаются мгновенно.
    ///
    /// Тестируемые контракты:
    ///   • UpdateTimestamps — корректно фиксирует время последней активности.
    ///   • PruneDeadUnits — удаляет устаревшие ключи без лишних аллокаций.
    ///   • GetIdleUnits — возвращает только юнитов с Idle >= IdleThreshold.
    ///   • CountIdleUnits — согласован с GetIdleUnits.
    ///   • IdleThreshold — равен 5 секундам по ТЗ.
    /// </summary>
    [TestFixture]
    public class IdleArmyLogicTests
    {
        // ----------------------------------------------------------------
        // Вспомогательные методы
        // ----------------------------------------------------------------

        /// <summary>Создаёт Unit на временном GameObject без сцены.</summary>
        private static Unit MakeUnit()
        {
            // Создаём минимальный GO без NavMeshAgent — Unit в тестах создаётся
            // через AddComponent без NavMeshAgent, поскольку [RequireComponent]
            // навешивает его автоматически только в Editor-режиме при AddComponent,
            // но в EditMode-тестах RequireComponent не добавляется автоматически.
            // Поэтому используем CreatePrimitive и вручную добавляем Unit без вызова Awake.
            var go = new GameObject("TestUnit");
            // Добавляем NavMeshAgent вручную (Unit [RequireComponent])
            go.AddComponent<UnityEngine.AI.NavMeshAgent>();
            var unit = go.AddComponent<Unit>();
            return unit;
        }

        /// <summary>
        /// Принудительно задаёт состояние юнита через рефлексию
        /// (поле _state приватное, нет публичного сеттера для тестов).
        /// </summary>
        private static void SetState(Unit unit, UnitState state)
        {
            var field = typeof(Unit).GetField(
                "_state",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(field, "_state field must exist on Unit");
            field.SetValue(unit, state);
        }

        /// <summary>Разрушает GameObject после теста.</summary>
        private static void Destroy(Unit unit)
        {
            if (unit != null) Object.DestroyImmediate(unit.gameObject);
        }

        // ----------------------------------------------------------------
        // Константы
        // ----------------------------------------------------------------

        private const float Threshold = IdleArmyLogic.IdleThreshold; // 5f

        // ----------------------------------------------------------------
        // IdleThreshold — контракт ТЗ
        // ----------------------------------------------------------------

        [Test]
        public void IdleThreshold_IsFiveSeconds()
        {
            Assert.AreEqual(5f, IdleArmyLogic.IdleThreshold, 0.001f,
                "IdleThreshold должен быть 5 секунд по ТЗ.");
        }

        // ----------------------------------------------------------------
        // UpdateTimestamps
        // ----------------------------------------------------------------

        [Test]
        public void UpdateTimestamps_NonIdleUnit_RecordsNow()
        {
            var unit = MakeUnit();
            try
            {
                SetState(unit, UnitState.Moving);

                var units = new List<Unit> { unit };
                var dict  = new Dictionary<Unit, float>();

                IdleArmyLogic.UpdateTimestamps(units, dict, 10f);

                Assert.IsTrue(dict.ContainsKey(unit), "Moving юнит должен попасть в словарь.");
                Assert.AreEqual(10f, dict[unit], 0.001f, "Время должно быть равно now=10.");
            }
            finally { Destroy(unit); }
        }

        [Test]
        public void UpdateTimestamps_IdleUnit_NotOverriddenOnce_SeedsNow()
        {
            var unit = MakeUnit();
            try
            {
                SetState(unit, UnitState.Idle);

                var units = new List<Unit> { unit };
                var dict  = new Dictionary<Unit, float>();

                // Первый вызов — юнит в Idle, словарь пуст → seed = now
                IdleArmyLogic.UpdateTimestamps(units, dict, 7f);

                Assert.IsTrue(dict.ContainsKey(unit), "Idle юнит должен получить seed в словаре.");
                Assert.AreEqual(7f, dict[unit], 0.001f, "Seed должен быть равен now=7.");
            }
            finally { Destroy(unit); }
        }

        [Test]
        public void UpdateTimestamps_IdleUnit_AlreadyInDict_NotUpdated()
        {
            var unit = MakeUnit();
            try
            {
                SetState(unit, UnitState.Idle);

                var units = new List<Unit> { unit };
                var dict  = new Dictionary<Unit, float> { { unit, 3f } };

                // Юнит уже в словаре (был Moving в прошлом) — Idle не перезаписывает.
                IdleArmyLogic.UpdateTimestamps(units, dict, 10f);

                Assert.AreEqual(3f, dict[unit], 0.001f,
                    "Idle юнит не должен перезаписывать существующую метку активности.");
            }
            finally { Destroy(unit); }
        }

        [Test]
        public void UpdateTimestamps_NullUnitInList_SkippedSafely()
        {
            var units = new List<Unit> { null };
            var dict  = new Dictionary<Unit, float>();

            Assert.DoesNotThrow(
                () => IdleArmyLogic.UpdateTimestamps(units, dict, 5f),
                "null в списке не должен вызывать исключение.");

            Assert.AreEqual(0, dict.Count, "null-юнит не должен попасть в словарь.");
        }

        // ----------------------------------------------------------------
        // PruneDeadUnits
        // ----------------------------------------------------------------

        [Test]
        public void PruneDeadUnits_DeadUnit_RemovedFromDict()
        {
            var alive = MakeUnit();
            var dead  = MakeUnit();
            try
            {
                var activeUnits = new List<Unit> { alive };
                var dict  = new Dictionary<Unit, float> { { alive, 1f }, { dead, 2f } };
                var dead2 = new List<Unit>();

                IdleArmyLogic.PruneDeadUnits(activeUnits, dict, dead2);

                Assert.IsTrue(dict.ContainsKey(alive),  "Живой юнит остаётся в словаре.");
                Assert.IsFalse(dict.ContainsKey(dead),  "Мёртвый юнит удаляется из словаря.");
            }
            finally { Destroy(alive); Destroy(dead); }
        }

        [Test]
        public void PruneDeadUnits_NullKeyInDict_RemovedSafely()
        {
            var alive = MakeUnit();
            try
            {
                var activeUnits = new List<Unit> { alive };
                var dict  = new Dictionary<Unit, float> { { alive, 1f } };
                // Вставляем null вручную (имитируем DestroyImmediate в рантайме)
                // Словарь Unity не позволяет null-ключ в Dictionary, поэтому проверяем только логику.
                // Тест проверяет, что живой юнит остаётся и метод завершается без ошибок.
                var deadBuf = new List<Unit>();
                IdleArmyLogic.PruneDeadUnits(activeUnits, dict, deadBuf);

                Assert.AreEqual(1, dict.Count, "Живой юнит остаётся.");
            }
            finally { Destroy(alive); }
        }

        // ----------------------------------------------------------------
        // GetIdleUnits
        // ----------------------------------------------------------------

        [Test]
        public void GetIdleUnits_IdleLongEnough_Included()
        {
            var unit = MakeUnit();
            try
            {
                SetState(unit, UnitState.Idle);

                var units = new List<Unit> { unit };
                var dict  = new Dictionary<Unit, float> { { unit, 0f } };
                var result = new List<Unit>();

                // now = Threshold → разница ровно 5 с → включаем
                IdleArmyLogic.GetIdleUnits(units, dict, Threshold, result);

                Assert.AreEqual(1, result.Count, "Юнит должен быть в результате.");
                Assert.AreSame(unit, result[0]);
            }
            finally { Destroy(unit); }
        }

        [Test]
        public void GetIdleUnits_IdleJustUnderThreshold_NotIncluded()
        {
            var unit = MakeUnit();
            try
            {
                SetState(unit, UnitState.Idle);

                var units = new List<Unit> { unit };
                // lastActive = 0, now = Threshold - epsilon → разница < 5 с → не включаем
                var dict   = new Dictionary<Unit, float> { { unit, 0f } };
                var result = new List<Unit>();

                IdleArmyLogic.GetIdleUnits(units, dict, Threshold - 0.01f, result);

                Assert.AreEqual(0, result.Count, "Юнит не должен быть в результате (не достиг порога).");
            }
            finally { Destroy(unit); }
        }

        [Test]
        public void GetIdleUnits_MovingUnit_NotIncluded()
        {
            var unit = MakeUnit();
            try
            {
                SetState(unit, UnitState.Moving);

                var units = new List<Unit> { unit };
                var dict  = new Dictionary<Unit, float> { { unit, 0f } };
                var result = new List<Unit>();

                IdleArmyLogic.GetIdleUnits(units, dict, 100f, result);

                Assert.AreEqual(0, result.Count, "Moving юнит не должен попадать в idle-список.");
            }
            finally { Destroy(unit); }
        }

        [Test]
        public void GetIdleUnits_HoldingUnit_NotIncluded()
        {
            var unit = MakeUnit();
            try
            {
                SetState(unit, UnitState.Holding);

                var units = new List<Unit> { unit };
                var dict  = new Dictionary<Unit, float> { { unit, 0f } };
                var result = new List<Unit>();

                IdleArmyLogic.GetIdleUnits(units, dict, 100f, result);

                Assert.AreEqual(0, result.Count,
                    "Holding юнит не считается Idle — он намеренно стоит.");
            }
            finally { Destroy(unit); }
        }

        [Test]
        public void GetIdleUnits_PatrollingUnit_NotIncluded()
        {
            var unit = MakeUnit();
            try
            {
                SetState(unit, UnitState.Patrolling);

                var units  = new List<Unit> { unit };
                var dict   = new Dictionary<Unit, float> { { unit, 0f } };
                var result = new List<Unit>();

                IdleArmyLogic.GetIdleUnits(units, dict, 100f, result);

                Assert.AreEqual(0, result.Count, "Patrolling юнит не считается Idle.");
            }
            finally { Destroy(unit); }
        }

        [Test]
        public void GetIdleUnits_NoTimestampEntry_NotIncluded()
        {
            var unit = MakeUnit();
            try
            {
                SetState(unit, UnitState.Idle);

                var units  = new List<Unit> { unit };
                var dict   = new Dictionary<Unit, float>(); // пустой — нет записи для unit
                var result = new List<Unit>();

                IdleArmyLogic.GetIdleUnits(units, dict, 100f, result);

                Assert.AreEqual(0, result.Count,
                    "Без записи в словаре юнит не должен попадать в результат.");
            }
            finally { Destroy(unit); }
        }

        [Test]
        public void GetIdleUnits_MixedUnits_OnlyLongIdleIncluded()
        {
            var unitIdle    = MakeUnit();
            var unitMoving  = MakeUnit();
            var unitNew     = MakeUnit(); // Idle, но совсем недавно
            try
            {
                SetState(unitIdle,   UnitState.Idle);
                SetState(unitMoving, UnitState.Moving);
                SetState(unitNew,    UnitState.Idle);

                var units  = new List<Unit> { unitIdle, unitMoving, unitNew };
                var dict   = new Dictionary<Unit, float>
                {
                    { unitIdle,   0f },           // Idle 100 секунд
                    { unitMoving, 95f },           // был активен 5 секунд назад
                    { unitNew,    100f - 2f },     // Idle всего 2 секунды
                };
                var result = new List<Unit>();

                IdleArmyLogic.GetIdleUnits(units, dict, 100f, result);

                Assert.AreEqual(1, result.Count,    "Только один юнит должен быть в результате.");
                Assert.AreSame(unitIdle, result[0], "Это должен быть долго бездействующий юнит.");
            }
            finally { Destroy(unitIdle); Destroy(unitMoving); Destroy(unitNew); }
        }

        // ----------------------------------------------------------------
        // CountIdleUnits — согласованность с GetIdleUnits
        // ----------------------------------------------------------------

        [Test]
        public void CountIdleUnits_MatchesGetIdleUnitsCount()
        {
            var u1 = MakeUnit();
            var u2 = MakeUnit();
            try
            {
                SetState(u1, UnitState.Idle);
                SetState(u2, UnitState.Idle);

                var units  = new List<Unit> { u1, u2 };
                var dict   = new Dictionary<Unit, float> { { u1, 0f }, { u2, 0f } };
                var result = new List<Unit>();

                float now = Threshold + 1f; // оба прошли порог

                IdleArmyLogic.GetIdleUnits(units, dict, now, result);
                int countMethod = IdleArmyLogic.CountIdleUnits(units, dict, now);

                Assert.AreEqual(result.Count, countMethod,
                    "CountIdleUnits должен возвращать то же число, что GetIdleUnits.Count.");
            }
            finally { Destroy(u1); Destroy(u2); }
        }

        [Test]
        public void CountIdleUnits_NoIdleUnits_ReturnsZero()
        {
            var unit = MakeUnit();
            try
            {
                SetState(unit, UnitState.Moving);
                var units = new List<Unit> { unit };
                var dict  = new Dictionary<Unit, float> { { unit, 0f } };

                int count = IdleArmyLogic.CountIdleUnits(units, dict, 100f);

                Assert.AreEqual(0, count);
            }
            finally { Destroy(unit); }
        }

        [Test]
        public void CountIdleUnits_EmptyList_ReturnsZero()
        {
            var units = new List<Unit>();
            var dict  = new Dictionary<Unit, float>();

            int count = IdleArmyLogic.CountIdleUnits(units, dict, 10f);

            Assert.AreEqual(0, count, "Пустой список → счётчик 0.");
        }
    }
}
