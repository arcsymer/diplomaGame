using System.Reflection;
using System.Text;
using DiplomaGame.Runtime.Buildings;
using DiplomaGame.Runtime.Data;
using DiplomaGame.Runtime.Units;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace DiplomaGame.Tests.Editor
{
    /// <summary>
    /// EditMode-валидация проводки реальной сцены Sandbox (без PlayMode).
    /// Регрессионная защита от класса багов «производство списывает деньги,
    /// но юнит не спавнится» (потерянный/неверный префаб у инстанса здания):
    /// круг 11 — вражеский барак тратил кристаллы впустую, армия ИИ не росла,
    /// матч был невозможен. Симптом в PlayMode почти невидим (очередь тикает,
    /// денег нет, юнитов нет) — EditMode-инспекция ловит мгновенно.
    /// </summary>
    [TestFixture]
    public class SceneWiringTests
    {
        private const string SandboxPath = "Assets/_Project/Scenes/Sandbox.unity";

        [Test]
        public void Sandbox_AllProductionBuildings_ResolveSpawnablePrefab()
        {
            EditorSceneManager.OpenScene(SandboxPath, OpenSceneMode.Single);

            var prodBuildings = Object.FindObjectsByType<ProductionBuilding>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assert.Greater(prodBuildings.Length, 0, "В Sandbox нет ProductionBuilding.");

            var resolve = typeof(ProductionBuilding).GetMethod(
                "ResolvePrefab", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(resolve, "ProductionBuilding.ResolvePrefab не найден (рефактор?).");

            var report = new StringBuilder();
            bool anyBroken = false;

            foreach (var pb in prodBuildings)
            {
                var building = pb.GetComponent<Building>();
                var data     = building != null ? building.Data : null;
                string title = $"{pb.gameObject.name} (faction={(building != null ? building.Faction.ToString() : "?")}, data={(data != null ? data.name : "null")})";

                if (data == null)
                {
                    anyBroken = true;
                    report.AppendLine($"  ✗ {title}: BuildingData отсутствует.");
                    continue;
                }

                // Какие записи реально может заказать это здание
                if (data.HasMultiProduction)
                {
                    foreach (var entry in data.ProductionEntries)
                    {
                        if (entry == null || entry.unitData == null) continue;
                        var prefab = (GameObject)resolve.Invoke(pb, new object[] { entry });
                        bool ok = prefab != null && prefab.GetComponent<Unit>() != null;
                        if (!ok) anyBroken = true;
                        report.AppendLine($"  {(ok ? "✓" : "✗")} {title}: entry '{entry.unitData.name}' → prefab {(prefab != null ? prefab.name : "NULL")}");
                    }
                }
                else if (data.Produces != null)
                {
                    var synthetic = new ProductionEntry { unitData = data.Produces };
                    var prefab    = (GameObject)resolve.Invoke(pb, new object[] { synthetic });
                    bool ok = prefab != null && prefab.GetComponent<Unit>() != null;
                    if (!ok) anyBroken = true;
                    report.AppendLine($"  {(ok ? "✓" : "✗")} {title}: legacy '{data.Produces.name}' → prefab {(prefab != null ? prefab.name : "NULL")}");
                }
                else
                {
                    report.AppendLine($"  - {title}: не производит юнитов (ок).");
                }
            }

            Debug.Log("[SceneWiring] Производственные здания Sandbox:\n" + report);
            Assert.IsFalse(anyBroken,
                "Найдены производственные здания с неразрешимым префабом юнита:\n" + report);
        }

        [Test]
        public void Sandbox_EnemyFactionProduction_SpawnsEnemyFactionUnits()
        {
            EditorSceneManager.OpenScene(SandboxPath, OpenSceneMode.Single);

            var resolve = typeof(ProductionBuilding).GetMethod(
                "ResolvePrefab", BindingFlags.NonPublic | BindingFlags.Instance);

            var report = new StringBuilder();
            bool anyWrong = false;

            foreach (var pb in Object.FindObjectsByType<ProductionBuilding>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var building = pb.GetComponent<Building>();
                if (building == null || building.Data == null) continue;

                // Активная запись: первая мульти-entry либо legacy
                ProductionEntry entry = building.Data.HasMultiProduction && building.Data.ProductionEntries.Length > 0
                    ? building.Data.ProductionEntries[0]
                    : new ProductionEntry { unitData = building.Data.Produces };
                if (entry == null || entry.unitData == null) continue;

                var prefab = (GameObject)resolve.Invoke(pb, new object[] { entry });
                var unit   = prefab != null ? prefab.GetComponent<Unit>() : null;
                if (unit == null) continue; // покрыто первым тестом

                bool ok = unit.Faction == building.Faction;
                if (!ok) anyWrong = true;
                report.AppendLine($"  {(ok ? "✓" : "✗")} {pb.gameObject.name}: faction здания {building.Faction}, " +
                                  $"префаб '{prefab.name}' фракции {unit.Faction}");
            }

            Debug.Log("[SceneWiring] Фракции производства Sandbox:\n" + report);
            Assert.IsFalse(anyWrong,
                "Здание производит юнитов ЧУЖОЙ фракции:\n" + report);
        }
    }
}
