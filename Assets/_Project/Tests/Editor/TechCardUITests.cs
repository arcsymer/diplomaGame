using DiplomaGame.Runtime.Core.Localization;
using DiplomaGame.Runtime.Data;
using DiplomaGame.Runtime.Tech;
using UnityEditor;
using DiplomaGame.Runtime.UI;
using DiplomaGame.Runtime.Units;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace DiplomaGame.Tests.Editor
{
    /// <summary>
    /// EditMode-тесты для CommandCardButton.BindTech (v7):
    /// • BindTech заполняет поля из TechData;
    /// • исследована → interactable false + overlay активен;
    /// • prerequisites не выполнены → interactable false;
    /// • доступна → interactable true.
    /// Следуют паттерну CommandCardLogicTests.
    /// </summary>
    [TestFixture]
    public class TechCardUITests
    {
        // ----------------------------------------------------------------
        // SetUp / TearDown — сброс TechRegistry + LocService (тултипы)
        // ----------------------------------------------------------------

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

        [SetUp]
        public void SetUp()
        {
            ResetRegistry();
        }

        [TearDown]
        public void TearDown()
        {
            ResetRegistry();
        }

        private static void ResetRegistry()
        {
            var field = typeof(TechRegistry).GetField(
                "_instance",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            field?.SetValue(null, null);
        }

        // ----------------------------------------------------------------
        // Вспомогательный метод — создаёт минимальный CommandCardButton с researchedOverlay
        // ----------------------------------------------------------------

        private GameObject CreateButtonGo(out CommandCardButton btn, out GameObject overlay)
        {
            var go = new GameObject("TestTechCardButton");

            var image  = go.AddComponent<Image>();  // iconImage
            var button = go.AddComponent<Button>(); // button

            var nameTextGo = new GameObject("NameText");
            nameTextGo.transform.SetParent(go.transform, false);
            var nameTmp = nameTextGo.AddComponent<TextMeshProUGUI>();

            var costTextGo = new GameObject("CostText");
            costTextGo.transform.SetParent(go.transform, false);
            var costTmp = costTextGo.AddComponent<TextMeshProUGUI>();

            var hotkeyTextGo = new GameObject("HotkeyText");
            hotkeyTextGo.transform.SetParent(go.transform, false);
            var hotkeyTmp = hotkeyTextGo.AddComponent<TextMeshProUGUI>();

            // ResearchedOverlay
            var overlayGo = new GameObject("ResearchedOverlay");
            overlayGo.transform.SetParent(go.transform, false);
            overlayGo.AddComponent<Image>().color = new Color(0.2f, 0.8f, 0.3f, 0.6f);
            overlayGo.SetActive(false);

            btn = go.AddComponent<CommandCardButton>();
            btn.InitForTest(index: 0);

            var so = new UnityEditor.SerializedObject(btn);
            so.FindProperty("iconImage").objectReferenceValue         = image;
            so.FindProperty("button").objectReferenceValue            = button;
            so.FindProperty("unitNameText").objectReferenceValue      = nameTmp;
            so.FindProperty("costText").objectReferenceValue          = costTmp;
            so.FindProperty("hotkeyText").objectReferenceValue        = hotkeyTmp;
            so.FindProperty("researchedOverlay").objectReferenceValue = overlayGo;
            so.ApplyModifiedPropertiesWithoutUndo();

            overlay = overlayGo;
            return go;
        }

        private static TechEntry MakeTechEntry(
            string     displayName  = "TestTech",
            string     description  = "Описание",
            int        cost         = 100,
            float      researchTime = 30f,
            string     hotkey       = "Q",
            TechData[] prerequisites = null)
        {
            var tech = TechData.CreateForTest(
                displayName:  displayName,
                description:  description,
                cost:         cost,
                researchTime: researchTime,
                hotkeyLabel:  hotkey,
                prerequisites: prerequisites);

            return new TechEntry { techData = tech };
        }

        // ================================================================
        // Тест 1 — BindTech заполняет поля из TechData корректно
        // ================================================================

        [Test]
        public void BindTech_FillsFieldsFromTechData()
        {
            var go  = CreateButtonGo(out var btn, out _);
            var entry = MakeTechEntry(
                displayName:  "Armoring",
                description:  "Броня",
                cost:         150,
                researchTime: 45f,
                hotkey:       "Q");

            btn.BindTech(entry, building: null, faction: Faction.Player);

            var data = btn.GetTooltipData();

            Assert.IsTrue(go.activeSelf, "GO должен быть активен после BindTech.");
            StringAssert.Contains("Armoring", data.Title, "Title должен содержать DisplayName технологии.");
            StringAssert.Contains("Броня",    data.Description, "Description должен содержать описание.");
            StringAssert.Contains("150",      data.Stats, "Stats должен содержать стоимость 150.");
            StringAssert.Contains("[Q]",      data.Stats, "Stats должен содержать хоткей [Q].");

            Object.DestroyImmediate(go);
        }

        // ================================================================
        // Тест 2 — исследована: interactable false + overlay активен
        // ================================================================

        [Test]
        public void BindTech_Researched_InteractableFalseAndOverlayActive()
        {
            var go  = CreateButtonGo(out var btn, out var overlay);
            var entry = MakeTechEntry(displayName: "WeaponsTech");

            // Предварительно исследуем
            TechRegistry.Instance.MarkResearched(Faction.Player, entry.techData);

            btn.BindTech(entry, building: null, faction: Faction.Player);

            var button = go.GetComponent<Button>();

            Assert.IsFalse(button.interactable,  "Исследованная технология: кнопка не должна быть интерактивна.");
            Assert.IsTrue(overlay.activeSelf,    "Исследованная технология: overlay должен быть активен.");

            Object.DestroyImmediate(go);
        }

        // ================================================================
        // Тест 3 — prerequisites не выполнены: interactable false
        // ================================================================

        [Test]
        public void BindTech_PrerequisitesUnmet_InteractableFalse()
        {
            var go  = CreateButtonGo(out var btn, out var overlay);

            var prereq = TechData.CreateForTest(displayName: "Prereq");
            var entry  = MakeTechEntry(displayName: "AdvancedTech", prerequisites: new[] { prereq });

            // prereq НЕ исследован
            btn.BindTech(entry, building: null, faction: Faction.Player);

            var button = go.GetComponent<Button>();

            Assert.IsFalse(button.interactable, "Без prerequisites: кнопка не должна быть интерактивна.");
            Assert.IsFalse(overlay.activeSelf,  "Без prerequisites: overlay не должен быть активен (не исследована).");

            Object.DestroyImmediate(go);
        }

        // ================================================================
        // Тест 4 — prerequisites выполнены и не исследована: interactable true
        // ================================================================

        [Test]
        public void BindTech_Available_InteractableTrue()
        {
            var go  = CreateButtonGo(out var btn, out var overlay);

            var prereq = TechData.CreateForTest(displayName: "PrereqTech");
            var entry  = MakeTechEntry(displayName: "TargetTech", prerequisites: new[] { prereq });

            // Исследуем prerequisite
            TechRegistry.Instance.MarkResearched(Faction.Player, prereq);

            btn.BindTech(entry, building: null, faction: Faction.Player);

            var button = go.GetComponent<Button>();

            Assert.IsTrue(button.interactable,  "Доступная технология: кнопка должна быть интерактивна.");
            Assert.IsFalse(overlay.activeSelf, "Доступная технология: overlay не должен быть активен (ещё не исследована).");

            Object.DestroyImmediate(go);
        }

        // ================================================================
        // Тест 5 — Prerequisites в тултипе: перечисляет неисследованные
        // ================================================================

        [Test]
        public void BindTech_PrerequisitesUnmet_TooltipContainsPrereqNames()
        {
            var go = CreateButtonGo(out var btn, out _);

            var prereq = TechData.CreateForTest(displayName: "BasicResearch");
            var entry  = MakeTechEntry(displayName: "Advanced", prerequisites: new[] { prereq });

            btn.BindTech(entry, building: null, faction: Faction.Player);

            var data = btn.GetTooltipData();

            StringAssert.Contains("BasicResearch", data.Description,
                "Description тултипа должен содержать имя неисследованного prerequisite.");
            StringAssert.Contains("Требует:", data.Description,
                "Description должен содержать метку 'Требует:'.");

            Object.DestroyImmediate(go);
        }
    }
}
