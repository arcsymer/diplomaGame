using DiplomaGame.Runtime.Core.Localization;
using DiplomaGame.Runtime.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace DiplomaGame.Tests.Editor
{
    /// <summary>
    /// EditMode-тесты для LocService.
    /// 6 случаев: дефолтный язык, Get ru/en, событие, fallback, полнота таблицы.
    /// </summary>
    [TestFixture]
    public class LocServiceTests
    {
        // ----------------------------------------------------------------
        // Помощники
        // ----------------------------------------------------------------

        private static LocTable BuildTable(params (string key, string ru, string en)[] entries)
        {
            var table = ScriptableObject.CreateInstance<LocTable>();
            var so    = new SerializedObject(table);
            var arr   = so.FindProperty("_entries");

            arr.arraySize = entries.Length;
            for (int i = 0; i < entries.Length; i++)
            {
                var elem = arr.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("key").stringValue = entries[i].key;
                elem.FindPropertyRelative("ru").stringValue  = entries[i].ru;
                elem.FindPropertyRelative("en").stringValue  = entries[i].en;
            }
            so.ApplyModifiedPropertiesWithoutUndo();

            return table;
        }

        [SetUp]
        public void SetUp()
        {
            LocService.ResetForTests();
        }

        [TearDown]
        public void TearDown()
        {
            LocService.ResetForTests();
        }

        // ================================================================
        // Тест 1: дефолтный язык — Ru
        // ================================================================

        [Test]
        public void DefaultLanguage_IsRu()
        {
            // ResetForTests() устанавливает Ru — проверяем что текущий язык Ru
            Assert.AreEqual(LocLanguage.Ru, LocService.CurrentLanguage,
                "После ResetForTests язык должен быть Ru.");
        }

        // ================================================================
        // Тест 2: Get возвращает ru в режиме Ru
        // ================================================================

        [Test]
        public void Get_ReturnsRussian_WhenLanguageRu()
        {
            var table = BuildTable(("test.key", "Привет", "Hello"));
            LocService.Initialize(table);

            // Язык Ru по умолчанию
            string result = LocService.Get("test.key");

            Assert.AreEqual("Привет", result,
                "В режиме Ru должна возвращаться русская строка.");
        }

        // ================================================================
        // Тест 3: Get возвращает en в режиме En
        // ================================================================

        [Test]
        public void Get_ReturnsEnglish_WhenLanguageEn()
        {
            var table = BuildTable(("test.key", "Привет", "Hello"));
            LocService.Initialize(table);
            LocService.CurrentLanguage = LocLanguage.En;

            string result = LocService.Get("test.key");

            Assert.AreEqual("Hello", result,
                "В режиме En должна возвращаться английская строка.");
        }

        // ================================================================
        // Тест 4: LanguageChanged стреляет при смене языка
        // ================================================================

        [Test]
        public void LanguageChanged_Fires_OnCurrentLanguageSet()
        {
            var table = BuildTable(("k", "р", "e"));
            LocService.Initialize(table);

            int count = 0;
            LocService.LanguageChanged += () => count++;

            LocService.CurrentLanguage = LocLanguage.En;

            Assert.AreEqual(1, count,
                "LanguageChanged должен сработать ровно один раз при смене языка.");
        }

        // ================================================================
        // Тест 5: Get возвращает ключ как fallback если ключ отсутствует
        // ================================================================

        [Test]
        public void Get_ReturnsFallbackKey_WhenKeyMissing()
        {
            // Пустая таблица — ключ не найдётся
            var table = BuildTable();
            LocService.Initialize(table);

            string result = LocService.Get("missing.key");

            Assert.AreEqual("missing.key", result,
                "Если ключ не найден, должен возвращаться сам ключ как fallback.");
        }

        // ================================================================
        // Тест 6: полнота таблицы — все ключи глоссария имеют непустые ru и en
        // ================================================================

        [Test]
        public void LocTable_AllGlossaryKeys_HaveNonEmptyRuAndEn()
        {
            const string assetPath = "Assets/_Project/Data/Localization/LocTable.asset";
            var locTable = AssetDatabase.LoadAssetAtPath<LocTable>(assetPath);

            if (locTable == null)
            {
                Assert.Ignore($"LocTable.asset не найден по пути {assetPath} — запустите 'Create/Update LocTable (v10)'.");
                return;
            }

            // Список ключей, которые обязаны присутствовать в таблице
            var requiredKeys = new[]
            {
                "hud.crystals", "hud.selected_units", "hud.queue_count", "hud.hint_train",
                "tooltip.cost_label", "tooltip.time_label", "tooltip.supply_label",
                "tooltip.research_prefix", "tooltip.production_prefix", "tooltip.requires_label",
                "tooltip.crystals_title", "tooltip.crystals_desc",
                "tooltip.minimap_title", "tooltip.minimap_desc",
                "tooltip.ability_empty_title", "tooltip.ability_empty_desc",
                "tooltip.ability_cooldown", "tooltip.ability_distance",
                "tooltip.ability_damage", "tooltip.ability_radius",
                "tooltip.ability_heal", "tooltip.ability_duration", "tooltip.ability_dmg_mult",
                "pause.title", "pause.btn_continue", "pause.btn_settings",
                "pause.btn_exit_menu", "pause.btn_quit",
                "settings.title", "settings.btn_back",
                "menu.btn_play", "menu.btn_settings", "menu.btn_quit",
                "gameover.victory", "gameover.defeat",
                "gameover.btn_restart", "gameover.btn_main_menu",
                "stats.duration_format", "stats.duration_empty",
            };

            LocService.Initialize(locTable);

            foreach (var key in requiredKeys)
            {
                // Проверяем ru
                LocService.ResetForTests();
                LocService.Initialize(locTable);
                string ru = LocService.Get(key);
                Assert.AreNotEqual(key, ru,
                    $"Ключ '{key}': ru-строка отсутствует или пуста (fallback = ключ).");

                // Проверяем en
                LocService.CurrentLanguage = LocLanguage.En;
                string en = LocService.Get(key);
                Assert.AreNotEqual(key, en,
                    $"Ключ '{key}': en-строка отсутствует или пуста (fallback = ключ).");
            }
        }
    }
}
