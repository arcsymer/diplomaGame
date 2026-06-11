using DiplomaGame.Runtime.Core;
using DiplomaGame.Runtime.Units;
using NUnit.Framework;

namespace DiplomaGame.Tests.Editor
{
    /// <summary>
    /// EditMode-тесты для MatchStats (чистый C#-класс, без MonoBehaviour).
    /// </summary>
    [TestFixture]
    public class MatchStatsTests
    {
        private MatchStats _stats;

        [SetUp]
        public void SetUp()
        {
            _stats = new MatchStats();
        }

        // ----------------------------------------------------------------
        // RecordKill — обе фракции
        // ----------------------------------------------------------------

        [Test]
        public void RecordKill_PlayerVictim_EnemyGetsKill_PlayerGetsLoss()
        {
            _stats.RecordKill(Faction.Player);

            Assert.AreEqual(0, _stats.UnitsKilled(Faction.Player), "Player не убивал.");
            Assert.AreEqual(1, _stats.UnitsKilled(Faction.Enemy),  "Enemy должен получить +1 kill.");
            Assert.AreEqual(1, _stats.UnitsLost(Faction.Player),   "Player должен получить +1 loss.");
            Assert.AreEqual(0, _stats.UnitsLost(Faction.Enemy),    "Enemy ничего не терял.");
        }

        [Test]
        public void RecordKill_EnemyVictim_PlayerGetsKill_EnemyGetsLoss()
        {
            _stats.RecordKill(Faction.Enemy);

            Assert.AreEqual(1, _stats.UnitsKilled(Faction.Player), "Player должен получить +1 kill.");
            Assert.AreEqual(0, _stats.UnitsKilled(Faction.Enemy),  "Enemy не убивал.");
            Assert.AreEqual(0, _stats.UnitsLost(Faction.Player),   "Player ничего не терял.");
            Assert.AreEqual(1, _stats.UnitsLost(Faction.Enemy),    "Enemy должен получить +1 loss.");
        }

        [Test]
        public void RecordKill_MultipleKills_Accumulate()
        {
            _stats.RecordKill(Faction.Enemy);
            _stats.RecordKill(Faction.Enemy);
            _stats.RecordKill(Faction.Enemy);

            Assert.AreEqual(3, _stats.UnitsKilled(Faction.Player), "После 3 убийств — 3 kill у Player.");
            Assert.AreEqual(3, _stats.UnitsLost(Faction.Enemy),    "После 3 убийств — 3 loss у Enemy.");
        }

        // ----------------------------------------------------------------
        // RecordDamage — накопление и zero-ignore
        // ----------------------------------------------------------------

        [Test]
        public void RecordDamage_AccumulatesCorrectly()
        {
            _stats.RecordDamage(Faction.Player, 50f);
            _stats.RecordDamage(Faction.Player, 30f);

            Assert.AreEqual(80f, _stats.DamageTaken(Faction.Player), 0.001f, "Player должен взять 80 урона.");
            Assert.AreEqual(80f, _stats.DamageDealt(Faction.Enemy),  0.001f, "Enemy должен нанести 80 урона.");
        }

        [Test]
        public void RecordDamage_ZeroAmount_IsIgnored()
        {
            _stats.RecordDamage(Faction.Player, 0f);

            Assert.AreEqual(0f, _stats.DamageTaken(Faction.Player), 0.001f, "Нулевой урон не учитывается.");
            Assert.AreEqual(0f, _stats.DamageDealt(Faction.Enemy),  0.001f, "Нулевой урон не учитывается.");
        }

        [Test]
        public void RecordDamage_NegativeAmount_IsIgnored()
        {
            _stats.RecordDamage(Faction.Enemy, -10f);

            Assert.AreEqual(0f, _stats.DamageTaken(Faction.Enemy),  0.001f, "Отрицательный урон не учитывается.");
            Assert.AreEqual(0f, _stats.DamageDealt(Faction.Player), 0.001f, "Отрицательный урон не учитывается.");
        }

        // ----------------------------------------------------------------
        // RecordMined
        // ----------------------------------------------------------------

        [Test]
        public void RecordMined_AccumulatesPerFaction()
        {
            _stats.RecordMined(Faction.Player, 100);
            _stats.RecordMined(Faction.Player, 50);
            _stats.RecordMined(Faction.Enemy,  200);

            Assert.AreEqual(150, _stats.CrystalsMined(Faction.Player), "Player: 100+50=150.");
            Assert.AreEqual(200, _stats.CrystalsMined(Faction.Enemy),  "Enemy: 200.");
        }

        // ----------------------------------------------------------------
        // RecordProduced
        // ----------------------------------------------------------------

        [Test]
        public void RecordProduced_IncrementsCounter()
        {
            _stats.RecordProduced(Faction.Player);
            _stats.RecordProduced(Faction.Player);
            _stats.RecordProduced(Faction.Enemy);

            Assert.AreEqual(2, _stats.UnitsProduced(Faction.Player), "Player произвёл 2 юнита.");
            Assert.AreEqual(1, _stats.UnitsProduced(Faction.Enemy),  "Enemy произвёл 1 юнита.");
        }

        // ----------------------------------------------------------------
        // UpdateArmyPeak — только рост / ноль не перетирает
        // ----------------------------------------------------------------

        [Test]
        public void UpdateArmyPeak_OnlyGrows()
        {
            _stats.UpdateArmyPeak(Faction.Player, 5);
            _stats.UpdateArmyPeak(Faction.Player, 10);
            _stats.UpdateArmyPeak(Faction.Player, 3);   // меньше — не должен затереть

            Assert.AreEqual(10, _stats.ArmyPeak(Faction.Player), "Пик должен быть 10 (максимум из 5,10,3).");
        }

        [Test]
        public void UpdateArmyPeak_ZeroDoesNotOverwritePositive()
        {
            _stats.UpdateArmyPeak(Faction.Player, 7);
            _stats.UpdateArmyPeak(Faction.Player, 0);

            Assert.AreEqual(7, _stats.ArmyPeak(Faction.Player), "Ноль не должен перетирать пик 7.");
        }

        // ----------------------------------------------------------------
        // Reset
        // ----------------------------------------------------------------

        [Test]
        public void Reset_ClearsAllCounters()
        {
            _stats.RecordKill(Faction.Enemy);
            _stats.RecordDamage(Faction.Player, 100f);
            _stats.RecordMined(Faction.Player, 200);
            _stats.RecordProduced(Faction.Enemy);
            _stats.UpdateArmyPeak(Faction.Player, 15);
            _stats.SetDuration(300f);

            _stats.Reset();

            Assert.AreEqual(0, _stats.UnitsKilled(Faction.Player),   "UnitsKilled Player = 0 после Reset.");
            Assert.AreEqual(0, _stats.UnitsKilled(Faction.Enemy),    "UnitsKilled Enemy = 0 после Reset.");
            Assert.AreEqual(0, _stats.UnitsLost(Faction.Player),     "UnitsLost Player = 0 после Reset.");
            Assert.AreEqual(0, _stats.UnitsLost(Faction.Enemy),      "UnitsLost Enemy = 0 после Reset.");
            Assert.AreEqual(0f, _stats.DamageDealt(Faction.Player),  0.001f, "DamageDealt Player = 0.");
            Assert.AreEqual(0f, _stats.DamageDealt(Faction.Enemy),   0.001f, "DamageDealt Enemy = 0.");
            Assert.AreEqual(0f, _stats.DamageTaken(Faction.Player),  0.001f, "DamageTaken Player = 0.");
            Assert.AreEqual(0f, _stats.DamageTaken(Faction.Enemy),   0.001f, "DamageTaken Enemy = 0.");
            Assert.AreEqual(0, _stats.CrystalsMined(Faction.Player), "CrystalsMined Player = 0.");
            Assert.AreEqual(0, _stats.CrystalsMined(Faction.Enemy),  "CrystalsMined Enemy = 0.");
            Assert.AreEqual(0, _stats.UnitsProduced(Faction.Player), "UnitsProduced Player = 0.");
            Assert.AreEqual(0, _stats.UnitsProduced(Faction.Enemy),  "UnitsProduced Enemy = 0.");
            Assert.AreEqual(0, _stats.ArmyPeak(Faction.Player),      "ArmyPeak Player = 0.");
            Assert.AreEqual(0, _stats.ArmyPeak(Faction.Enemy),       "ArmyPeak Enemy = 0.");
            Assert.AreEqual(0f, _stats.MatchDurationSeconds,         0.001f, "Duration = 0 после Reset.");
        }
    }
}
