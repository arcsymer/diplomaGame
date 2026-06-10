using DiplomaGame.Runtime.Data;
using UnityEditor;
using UnityEngine;

namespace DiplomaGame.Editor
{
    /// <summary>
    /// Вкладка Config — создание и обновление ScriptableObject-ассетов данных юнитов и зданий.
    /// M4: Marine.asset и EnemyGrunt.asset.
    /// M5: HQ.asset, Barracks.asset, Extractor.asset.
    /// </summary>
    internal sealed class ConfigTab : IForgeTab
    {
        private const string UnitDataFolder     = "Assets/_Project/Data/Units";
        private const string BuildingDataFolder = "Assets/_Project/Data/Buildings";

        public string Title => "Config";

        public void OnGUI()
        {
            GUILayout.Space(8);
            GUILayout.Label("Конфигурация данных", EditorStyles.boldLabel);
            GUILayout.Space(4);

            EditorGUILayout.HelpBox(
                "Создаёт (идемпотентно) UnitData ScriptableObject-ассеты:\n" +
                "• Marine.asset — боец игрока\n" +
                "• EnemyGrunt.asset — базовый враг",
                MessageType.Info);

            GUILayout.Space(4);

            if (GUILayout.Button("Create/Update Unit Data Assets (M4)", GUILayout.Height(32)))
                CreateOrUpdateUnitDataAssets();

            GUILayout.Space(8);

            EditorGUILayout.HelpBox(
                "Создаёт (идемпотентно) BuildingData ScriptableObject-ассеты:\n" +
                "• HQ.asset — штаб (hp 1000, доход 5/2с)\n" +
                "• Barracks.asset — казарма (стоимость 100, hp 500, производит Marine)\n" +
                "• Extractor.asset — экстрактор (стоимость 75, hp 300, доход 8/2с)",
                MessageType.Info);

            GUILayout.Space(4);

            if (GUILayout.Button("Create/Update Building Data (M5)", GUILayout.Height(32)))
                CreateOrUpdateBuildingDataAssets();
        }

        // ----------------------------------------------------------------
        // Основная операция
        // ----------------------------------------------------------------

        /// <summary>
        /// Идемпотентно создаёт или обновляет UnitData-ассеты для M4.
        /// </summary>
        internal static void CreateOrUpdateUnitDataAssets()
        {
            EnsureFolder(UnitDataFolder);

            CreateOrUpdateUnitData(
                assetName:        "Marine",
                displayName:      "Marine",
                maxHp:            100f,
                damage:           10f,
                attackRange:      8f,
                attackCooldown:   1f,
                aggroRadius:      12f,
                moveSpeed:        5f,
                retreatFraction:  0.25f,
                retreatDisabled:  false);

            CreateOrUpdateUnitData(
                assetName:        "EnemyGrunt",
                displayName:      "Enemy Grunt",
                maxHp:            80f,
                damage:           8f,
                attackRange:      7f,
                attackCooldown:   1.2f,
                aggroRadius:      12f,
                moveSpeed:        4.5f,
                retreatFraction:  0.25f,
                retreatDisabled:  false);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Project Forge] UnitData ассеты (M4) созданы/обновлены.");
        }

        // ----------------------------------------------------------------
        // Вспомогательные методы
        // ----------------------------------------------------------------

        private static UnitData CreateOrUpdateUnitData(
            string assetName,
            string displayName,
            float  maxHp,
            float  damage,
            float  attackRange,
            float  attackCooldown,
            float  aggroRadius,
            float  moveSpeed,
            float  retreatFraction,
            bool   retreatDisabled)
        {
            string path     = $"{UnitDataFolder}/{assetName}.asset";
            var    existing = AssetDatabase.LoadAssetAtPath<UnitData>(path);

            UnitData data;
            if (existing != null)
            {
                data = existing;
            }
            else
            {
                data = ScriptableObject.CreateInstance<UnitData>();
                AssetDatabase.CreateAsset(data, path);
            }

            var so = new SerializedObject(data);
            so.FindProperty("_displayName").stringValue        = displayName;
            so.FindProperty("_maxHp").floatValue               = maxHp;
            so.FindProperty("_damage").floatValue              = damage;
            so.FindProperty("_attackRange").floatValue         = attackRange;
            so.FindProperty("_attackCooldown").floatValue      = attackCooldown;
            so.FindProperty("_aggroRadius").floatValue         = aggroRadius;
            so.FindProperty("_moveSpeed").floatValue           = moveSpeed;
            so.FindProperty("_retreatHpFraction").floatValue   = retreatFraction;
            so.FindProperty("_retreatDisabled").boolValue      = retreatDisabled;
            so.ApplyModifiedPropertiesWithoutUndo();

            return data;
        }

        // ----------------------------------------------------------------
        // M5: BuildingData
        // ----------------------------------------------------------------

        /// <summary>
        /// Идемпотентно создаёт или обновляет BuildingData-ассеты для M5.
        /// </summary>
        internal static void CreateOrUpdateBuildingDataAssets()
        {
            EnsureFolder(BuildingDataFolder);

            var marineData = AssetDatabase.LoadAssetAtPath<DiplomaGame.Runtime.Data.UnitData>(
                "Assets/_Project/Data/Units/Marine.asset");

            if (marineData == null)
                Debug.LogWarning("[Project Forge] Marine.asset не найден — у Barracks поле produces будет пустым. " +
                                 "Сначала запустите 'Create/Update Unit Data Assets (M4)'.");

            // HQ
            CreateOrUpdateBuildingData(
                assetName:         "HQ",
                displayName:       "Headquarters",
                cost:              0,
                maxHp:             1000f,
                buildingType:      DiplomaGame.Runtime.Data.BuildingType.Headquarters,
                incomePerTick:     5,
                incomeTickInterval: 2f,
                produces:          null,
                productionTime:    0f,
                productionCost:    0);

            // Barracks
            CreateOrUpdateBuildingData(
                assetName:         "Barracks",
                displayName:       "Barracks",
                cost:              100,
                maxHp:             500f,
                buildingType:      DiplomaGame.Runtime.Data.BuildingType.Barracks,
                incomePerTick:     0,
                incomeTickInterval: 2f,
                produces:          marineData,
                productionTime:    5f,
                productionCost:    50);

            // Extractor
            CreateOrUpdateBuildingData(
                assetName:         "Extractor",
                displayName:       "Extractor",
                cost:              75,
                maxHp:             300f,
                buildingType:      DiplomaGame.Runtime.Data.BuildingType.Extractor,
                incomePerTick:     8,
                incomeTickInterval: 2f,
                produces:          null,
                productionTime:    0f,
                productionCost:    0);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Project Forge] BuildingData ассеты (M5) созданы/обновлены.");
        }

        private static DiplomaGame.Runtime.Data.BuildingData CreateOrUpdateBuildingData(
            string       assetName,
            string       displayName,
            int          cost,
            float        maxHp,
            DiplomaGame.Runtime.Data.BuildingType buildingType,
            int          incomePerTick,
            float        incomeTickInterval,
            DiplomaGame.Runtime.Data.UnitData produces,
            float        productionTime,
            int          productionCost)
        {
            string path     = $"{BuildingDataFolder}/{assetName}.asset";
            var    existing = AssetDatabase.LoadAssetAtPath<DiplomaGame.Runtime.Data.BuildingData>(path);

            DiplomaGame.Runtime.Data.BuildingData data;
            if (existing != null)
            {
                data = existing;
            }
            else
            {
                data = ScriptableObject.CreateInstance<DiplomaGame.Runtime.Data.BuildingData>();
                AssetDatabase.CreateAsset(data, path);
            }

            var so = new SerializedObject(data);
            so.FindProperty("_displayName").stringValue               = displayName;
            so.FindProperty("_cost").intValue                         = cost;
            so.FindProperty("_maxHp").floatValue                      = maxHp;
            so.FindProperty("_buildingType").enumValueIndex           = (int)buildingType;
            so.FindProperty("_incomePerTick").intValue                = incomePerTick;
            so.FindProperty("_incomeTickInterval").floatValue         = incomeTickInterval;
            so.FindProperty("_produces").objectReferenceValue         = produces;
            so.FindProperty("_productionTime").floatValue             = productionTime;
            so.FindProperty("_productionCost").intValue               = productionCost;
            so.ApplyModifiedPropertiesWithoutUndo();

            return data;
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath)) return;

            var parts   = folderPath.Split('/');
            var current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
