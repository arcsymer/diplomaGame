using DiplomaGame.Runtime.Core.Localization;
using DiplomaGame.Runtime.Data;
using DiplomaGame.Runtime.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DiplomaGame.Tests.Editor
{
    /// <summary>
    /// EditMode-тесты для CommandCardButton (v6):
    /// • формат тултипа корректен;
    /// • Bind с null-иконкой не выбрасывает исключение;
    /// • Hide/Bind переключают gameObject.activeSelf.
    /// </summary>
    [TestFixture]
    public class CommandCardLogicTests
    {
        // Тултипы строятся через LocService — инициализируем реальной таблицей
        [OneTimeSetUp]
        public void LocSetUp()
        {
            var table = AssetDatabase.LoadAssetAtPath<LocTable>(
                "Assets/_Project/Data/Localization/LocTable.asset");
            Assert.IsNotNull(table,
                "LocTable.asset не найдена — прогоните Forge: Create/Update LocTable (v10).");
            LocService.ResetForTests();
            LocService.Initialize(table);
        }

        [OneTimeTearDown]
        public void LocTearDown() => LocService.ResetForTests();

        // ----------------------------------------------------------------
        // Вспомогательный метод — создаёт минимальный CommandCardButton
        // ----------------------------------------------------------------

        private GameObject CreateButtonGo(out CommandCardButton btn)
        {
            var go = new GameObject("TestCommandCardButton");

            // Добавляем необходимые компоненты для uGUI
            var image  = go.AddComponent<Image>();   // iconImage
            var button = go.AddComponent<Button>();  // button

            // TMP-тексты (в EditMode TextMeshProUGUI работает без canvas)
            var nameTextGo = new GameObject("NameText");
            nameTextGo.transform.SetParent(go.transform, false);
            var nameTmp = nameTextGo.AddComponent<TextMeshProUGUI>();

            var costTextGo = new GameObject("CostText");
            costTextGo.transform.SetParent(go.transform, false);
            var costTmp = costTextGo.AddComponent<TextMeshProUGUI>();

            var hotkeyTextGo = new GameObject("HotkeyText");
            hotkeyTextGo.transform.SetParent(go.transform, false);
            var hotkeyTmp = hotkeyTextGo.AddComponent<TextMeshProUGUI>();

            btn = go.AddComponent<CommandCardButton>();
            btn.InitForTest(index: 0);

            // Проставляем ссылки через SerializedObject (единственный способ в EditMode)
            var so = new UnityEditor.SerializedObject(btn);
            so.FindProperty("iconImage").objectReferenceValue    = image;
            so.FindProperty("button").objectReferenceValue       = button;
            so.FindProperty("unitNameText").objectReferenceValue = nameTmp;
            so.FindProperty("costText").objectReferenceValue     = costTmp;
            so.FindProperty("hotkeyText").objectReferenceValue   = hotkeyTmp;
            so.ApplyModifiedPropertiesWithoutUndo();

            return go;
        }

        private static ProductionEntry MakeEntry(
            string name        = "TestUnit",
            string desc        = "Описание",
            int    cost        = 50,
            float  time        = 5f,
            string hotkey      = "T",
            int    supplyCost  = 0,
            Sprite icon        = null)
        {
            var unitData = UnitData.CreateForTest(
                displayName: name,
                description: desc,
                supplyCost:  supplyCost);

            return new ProductionEntry
            {
                unitData       = unitData,
                cost           = cost,
                productionTime = time,
                hotkeyLabel    = hotkey,
                icon           = icon
            };
        }

        // ================================================================
        // Тест 1 — формат тултипа корректен
        // ================================================================

        [Test]
        public void Bind_TooltipFormat_IsCorrect()
        {
            var go  = CreateButtonGo(out var btn);
            var entry = MakeEntry(name: "Marine", desc: "Стандартный боец", cost: 50, time: 5f, hotkey: "T");

            btn.Bind(entry, building: null);

            var data = btn.GetTooltipData();

            Assert.AreEqual("Производство: Marine", data.Title,
                "Title тултипа должен быть 'Производство: <DisplayName>'.");

            Assert.AreEqual("Стандартный боец", data.Description,
                "Description должен совпадать с UnitData.Description.");

            StringAssert.Contains("Стоимость: 50", data.Stats,
                "Stats должен содержать 'Стоимость: <cost>'.");
            StringAssert.Contains("Время: 5с", data.Stats,
                "Stats должен содержать 'Время: <time>с'.");
            StringAssert.Contains("[T]", data.Stats,
                "Stats должен содержать хоткей в квадратных скобках.");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void Bind_TooltipFormat_WithSupply_ContainsSupplyLine()
        {
            var go    = CreateButtonGo(out var btn);
            var entry = MakeEntry(name: "HeavyMarine", cost: 90, time: 8f, hotkey: "Y", supplyCost: 2);

            btn.Bind(entry, building: null);

            var data = btn.GetTooltipData();

            StringAssert.Contains("Supply: 2", data.Stats,
                "Stats должен содержать 'Supply: <supplyCost>' когда supplyCost > 0.");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void Bind_TooltipFormat_NoSupply_DoesNotContainSupplyLine()
        {
            var go    = CreateButtonGo(out var btn);
            var entry = MakeEntry(name: "Marine", cost: 50, supplyCost: 0);

            btn.Bind(entry, building: null);

            var data = btn.GetTooltipData();

            StringAssert.DoesNotContain("Supply:", data.Stats,
                "Stats не должен содержать 'Supply:' когда supplyCost == 0.");

            Object.DestroyImmediate(go);
        }

        // ================================================================
        // Тест 2 — Bind с null-иконкой не выбрасывает исключение
        // ================================================================

        [Test]
        public void Bind_NullIcon_DoesNotThrow()
        {
            var go    = CreateButtonGo(out var btn);
            var entry = MakeEntry(icon: null);

            // Не должно бросить NullReferenceException или любое другое исключение
            Assert.DoesNotThrow(
                () => btn.Bind(entry, building: null),
                "Bind с null icon не должен выбрасывать исключение.");

            // GameObject должен стать активным после Bind
            Assert.IsTrue(go.activeSelf, "GO должен быть активен после Bind.");

            Object.DestroyImmediate(go);
        }

        // ================================================================
        // Тест 3 — Hide/Bind переключают gameObject.activeSelf
        // ================================================================

        [Test]
        public void Hide_DeactivatesGameObject()
        {
            var go    = CreateButtonGo(out var btn);
            var entry = MakeEntry();

            // Сначала показываем
            btn.Bind(entry, building: null);
            Assert.IsTrue(go.activeSelf, "GO должен быть активен после Bind.");

            // Скрываем
            btn.Hide();
            Assert.IsFalse(go.activeSelf, "GO должен быть неактивен после Hide.");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void Bind_AfterHide_ReactivatesGameObject()
        {
            var go    = CreateButtonGo(out var btn);
            var entry = MakeEntry();

            btn.Hide();
            Assert.IsFalse(go.activeSelf, "GO должен быть неактивен после Hide.");

            btn.Bind(entry, building: null);
            Assert.IsTrue(go.activeSelf, "GO должен снова стать активным после Bind.");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void HideBindCycle_MultipleRounds_TogglesProperly()
        {
            var go    = CreateButtonGo(out var btn);
            var entry = MakeEntry();

            for (int i = 0; i < 3; i++)
            {
                btn.Bind(entry, building: null);
                Assert.IsTrue(go.activeSelf, $"Итерация {i}: должен быть активен после Bind.");

                btn.Hide();
                Assert.IsFalse(go.activeSelf, $"Итерация {i}: должен быть неактивен после Hide.");
            }

            Object.DestroyImmediate(go);
        }
    }
}
