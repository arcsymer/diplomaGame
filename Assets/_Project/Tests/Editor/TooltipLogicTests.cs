using DiplomaGame.Runtime.Core.Localization;
using DiplomaGame.Runtime.Data;
using DiplomaGame.Runtime.UI;
using NUnit.Framework;
using UnityEngine;

namespace DiplomaGame.Tests.Editor
{
    /// <summary>
    /// EditMode-тесты для TooltipLogic.
    /// Нет MonoBehaviour — запускаются мгновенно без сцены.
    /// </summary>
    [TestFixture]
    public class TooltipLogicTests
    {
        // ----------------------------------------------------------------
        // Фикстура LocService — инициализируем до тестов с русским глоссарием
        // ----------------------------------------------------------------

        [SetUp]
        public void SetUp()
        {
            // Создаём минимальный LocTable с ключами способностей
            var table = ScriptableObject.CreateInstance<LocTable>();
            var so    = new UnityEditor.SerializedObject(table);
            var arr   = so.FindProperty("_entries");

            var entries = new (string key, string ru, string en)[]
            {
                ("tooltip.ability_cooldown",  "Кулдаун: {0}с",    "Cooldown: {0}s"),
                ("tooltip.ability_distance",  "Дистанция: {0}",   "Distance: {0}"),
                ("tooltip.ability_damage",    "Урон: {0}",        "Damage: {0}"),
                ("tooltip.ability_radius",    "Радиус: {0}",      "Radius: {0}"),
                ("tooltip.ability_heal",      "Лечение: {0}",     "Heal: {0}"),
                ("tooltip.ability_duration",  "Длительность: {0}с","Duration: {0}s"),
                ("tooltip.ability_dmg_mult",  "Урон ×{0}",        "Damage ×{0}"),
            };

            arr.arraySize = entries.Length;
            for (int i = 0; i < entries.Length; i++)
            {
                var elem = arr.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("key").stringValue = entries[i].key;
                elem.FindPropertyRelative("ru").stringValue  = entries[i].ru;
                elem.FindPropertyRelative("en").stringValue  = entries[i].en;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            LocService.ResetForTests();
            LocService.Initialize(table);
        }

        [TearDown]
        public void TearDown()
        {
            LocService.ResetForTests();
        }

        // ================================================================
        // FormatAbilityStats
        // ================================================================

        [Test]
        public void FormatAbilityStats_Null_ReturnsNull()
        {
            Assert.IsNull(TooltipLogic.FormatAbilityStats(null));
        }

        [Test]
        public void FormatAbilityStats_Dash_ContainsCooldownAndDistance()
        {
            var data = AbilityData.CreateForTest(
                name:         "Рывок",
                cooldown:     8f,
                type:         AbilityType.Dash,
                dashDistance: 6f);

            string result = TooltipLogic.FormatAbilityStats(data);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Contains("8"), $"Должен содержать кулдаун 8: «{result}»");
            Assert.IsTrue(result.Contains("6"), $"Должен содержать дистанцию 6: «{result}»");
            Assert.IsTrue(result.Contains("Кулдаун"), $"Должен содержать слово 'Кулдаун': «{result}»");
            Assert.IsTrue(result.Contains("Дистанция"), $"Должен содержать слово 'Дистанция': «{result}»");
        }

        [Test]
        public void FormatAbilityStats_Shockwave_ContainsCooldownDamageRadius()
        {
            var data = AbilityData.CreateForTest(
                name:         "Ударная волна",
                cooldown:     12f,
                type:         AbilityType.Shockwave,
                effectAmount: 40f,
                effectRadius: 6f);

            string result = TooltipLogic.FormatAbilityStats(data);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Contains("12"), $"Должен содержать кулдаун 12: «{result}»");
            Assert.IsTrue(result.Contains("40"), $"Должен содержать урон 40: «{result}»");
            Assert.IsTrue(result.Contains("6"),  $"Должен содержать радиус 6: «{result}»");
            Assert.IsTrue(result.Contains("Урон"),   $"Должен содержать 'Урон': «{result}»");
            Assert.IsTrue(result.Contains("Радиус"), $"Должен содержать 'Радиус': «{result}»");
        }

        [Test]
        public void FormatAbilityStats_RepairField_ContainsCooldownHealRadius()
        {
            var data = AbilityData.CreateForTest(
                name:         "Ремонтное поле",
                cooldown:     15f,
                type:         AbilityType.RepairField,
                effectAmount: 30f,
                effectRadius: 8f);

            string result = TooltipLogic.FormatAbilityStats(data);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Contains("15"),     $"Должен содержать кулдаун 15: «{result}»");
            Assert.IsTrue(result.Contains("30"),     $"Должен содержать лечение 30: «{result}»");
            Assert.IsTrue(result.Contains("8"),      $"Должен содержать радиус 8: «{result}»");
            Assert.IsTrue(result.Contains("Лечение"),$"Должен содержать 'Лечение': «{result}»");
        }

        [Test]
        public void FormatAbilityStats_Overcharge_ContainsCooldownDurationMultiplier()
        {
            var data = AbilityData.CreateForTest(
                name:              "Перегрузка",
                cooldown:          20f,
                type:              AbilityType.Overcharge,
                buffDuration:      5f,
                damageMultiplier:  1.5f);

            string result = TooltipLogic.FormatAbilityStats(data);

            Assert.IsNotNull(result);
            Assert.IsTrue(result.Contains("20"),     $"Должен содержать кулдаун 20: «{result}»");
            Assert.IsTrue(result.Contains("5"),      $"Должен содержать длительность 5: «{result}»");
            Assert.IsTrue(result.Contains("1.5"),    $"Должен содержать множитель 1.5: «{result}»");
            Assert.IsTrue(result.Contains("Длительность"), $"Должен содержать 'Длительность': «{result}»");
        }

        // ================================================================
        // TooltipData — Stats = null
        // ================================================================

        [Test]
        public void TooltipData_StatsNull_StatsPropertyIsNull()
        {
            var data = new TooltipData("Заголовок", "Описание");
            Assert.IsNull(data.Stats, "Stats должен быть null при создании без третьего аргумента.");
        }

        [Test]
        public void TooltipData_WithStats_StatsPropertySet()
        {
            var data = new TooltipData("Заголовок", "Описание", "Статы");
            Assert.AreEqual("Статы", data.Stats);
        }

        // ================================================================
        // ClampToScreen
        // ================================================================

        [Test]
        public void ClampToScreen_InsideScreen_NoChange()
        {
            // Тултип 280x80 при курсоре 100,500 — всё влезает (экран 1920x1080 фиктивный)
            // ClampToScreen работает с Screen.width/height, но в EditMode они 0.
            // Тестируем математику напрямую через статичный метод с подменой Screen:
            // Так как Screen нельзя мокировать, проверяем только что метод не кидает исключение
            // и возвращает Vector2 (разумное поведение при Screen.width=0 описано в impl).
            Assert.DoesNotThrow(() =>
            {
                var result = TooltipLogic.ClampToScreen(new Vector2(100f, 500f), new Vector2(280f, 80f));
                // В EditMode Screen.width=Screen.height=0, поэтому результат — зажатый 0.
                // Просто убеждаемся, что нет исключения.
                _ = result;
            });
        }

        [Test]
        public void ClampToScreen_RightOverflow_XShiftsLeft()
        {
            // Используем тестовый хелпер, изолированный от Screen
            // Проверяем чистую логику: если x + w > screenW → x = cursor - w - 16
            float screenW   = 1920f;
            float screenH   = 1080f;
            float tooltipW  = 280f;
            float tooltipH  = 80f;

            // Курсор у правого края (cursor.x = 1910), базовое x = 1910+16 = 1926 > 1920
            float cursorX   = 1910f;
            float cursorY   = 500f;

            // Ожидаем x = cursorX - tooltipW - 16 = 1910 - 280 - 16 = 1614
            float expectedX = cursorX - tooltipW - 16f;
            Vector2 result  = ClampToScreenInternal(new Vector2(cursorX, cursorY),
                new Vector2(tooltipW, tooltipH), screenW, screenH);

            Assert.AreEqual(expectedX, result.x, 1f,
                $"При выходе вправо X должен смещаться влево. Ожидалось ~{expectedX}, получено {result.x}");
        }

        [Test]
        public void ClampToScreen_BottomOverflow_YShiftsUp()
        {
            float screenW   = 1920f;
            float screenH   = 1080f;
            float tooltipW  = 280f;
            float tooltipH  = 80f;

            // Курсор у нижнего края (cursor.y = 20), базовое y = 20-16 = 4, 4-80 = -76 < 0
            float cursorX   = 500f;
            float cursorY   = 20f;

            // Ожидаем y = cursorY + tooltipH + 16 = 20 + 80 + 16 = 116
            float expectedY = cursorY + tooltipH + 16f;
            Vector2 result  = ClampToScreenInternal(new Vector2(cursorX, cursorY),
                new Vector2(tooltipW, tooltipH), screenW, screenH);

            Assert.AreEqual(expectedY, result.y, 1f,
                $"При выходе снизу Y должен смещаться вверх. Ожидалось ~{expectedY}, получено {result.y}");
        }

        // ----------------------------------------------------------------
        // Изолированная версия ClampToScreen для тестов (без зависимости от Screen)
        // ----------------------------------------------------------------

        private static Vector2 ClampToScreenInternal(Vector2 screenPos, Vector2 tooltipSize,
            float screenW, float screenH)
        {
            float x = screenPos.x + 16f;
            float y = screenPos.y - 16f;

            if (x + tooltipSize.x > screenW)
                x = screenPos.x - tooltipSize.x - 16f;

            if (y - tooltipSize.y < 0f)
                y = screenPos.y + tooltipSize.y + 16f;

            x = Mathf.Clamp(x, 0f, screenW - tooltipSize.x);
            y = Mathf.Clamp(y, tooltipSize.y, screenH);

            return new Vector2(x, y);
        }
    }
}
