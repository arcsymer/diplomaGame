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
        private const string AbilityDataFolder  = "Assets/_Project/Data/Abilities";
        private const string TechDataFolder     = "Assets/_Project/Data/Tech";

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

            GUILayout.Space(8);

            EditorGUILayout.HelpBox(
                "Tooltip Descriptions: проставляет _description (и _supplyCost для юнитов)\n" +
                "существующим ассетам Unit/Building/Ability Data для системы тултипов.",
                MessageType.Info);

            GUILayout.Space(4);

            if (GUILayout.Button("Update Tooltip Descriptions", GUILayout.Height(32)))
                UpdateTooltipDescriptions();

            GUILayout.Space(8);

            EditorGUILayout.HelpBox(
                "v3 Tank:\n" +
                "• Tank.asset      — HP 280, Damage 25, AoE 3.0, AttackRange 5, TargetPriority Buildings\n" +
                "• EnemyTank.asset — то же (вражеская версия)\n" +
                "• WarFactory.asset — HP 600, productionCost 150, productionTime 12",
                MessageType.Info);

            GUILayout.Space(4);

            if (GUILayout.Button("Create/Update Tank Data (v3)", GUILayout.Height(32)))
                CreateOrUpdateTankDataAssets();

            GUILayout.Space(8);

            EditorGUILayout.HelpBox(
                "Migrate Production Entries (v6):\n" +
                "• Создаёт HeavyMarine.asset (HP 150, Damage 14, AttackRange 8, Cooldown 1.1, Supply 2)\n" +
                "• Barracks._productionEntries: [T] Marine cost 50 time 5 · [Y] HeavyMarine cost 90 time 8\n" +
                "• WarFactory._productionEntries: [T] Tank cost 150 time 12\n" +
                "• Legacy-поля зданий не изменяются. Иконки = null (заглушки).",
                MessageType.Info);

            GUILayout.Space(4);

            if (GUILayout.Button("Migrate Production Entries (v6)", GUILayout.Height(32)))
                MigrateProductionEntriesV6();

            GUILayout.Space(8);

            EditorGUILayout.HelpBox(
                "Create/Update Tech Assets (v7):\n" +
                "• Tech_Armoring.asset — Бронирование, +20% MaxHp, InfantryOnly, 150/40с, hotkey R\n" +
                "• Tech_Weapons.asset  — Усиленные стволы, +15% Damage, все, 175/45с, hotkey G\n" +
                "• Tech_RapidFire.asset — Расширенные обоймы, −15% AttackCooldown, все, 200/50с, hotkey X (требует Weapons)\n" +
                "• Barracks._techEntries заполняется тремя технологиями.\n" +
                "Идемпотентно.",
                MessageType.Info);

            GUILayout.Space(4);

            if (GUILayout.Button("Create/Update Tech Assets (v7)", GUILayout.Height(32)))
                CreateOrUpdateTechAssetsV7();

            GUILayout.Space(8);

            EditorGUILayout.HelpBox(
                "Migrate Localization EN Fields (v10):\n" +
                "Проставляет _displayNameEn / _descriptionEn на все Unit/Building/Ability/Tech ассеты.",
                MessageType.Info);

            GUILayout.Space(4);

            if (GUILayout.Button("Migrate Localization EN Fields (v10)", GUILayout.Height(32)))
                MigrateLocalizationEnFieldsV10();

            GUILayout.Space(8);

            EditorGUILayout.HelpBox(
                "Create/Update LocTable (v10):\n" +
                "Создаёт Assets/_Project/Data/Localization/LocTable.asset со всеми ключами глоссария (50+ записей).",
                MessageType.Info);

            GUILayout.Space(4);

            if (GUILayout.Button("Create/Update LocTable (v10)", GUILayout.Height(32)))
                CreateOrUpdateLocTableV10();
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
                description:      "Универсальный боец. Атакует наземные цели.",
                supplyCost:       1,
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
                description:      "Базовый враг. Атакует всё.",
                supplyCost:       1,
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
            string         assetName,
            string         displayName,
            float          maxHp,
            float          damage,
            float          attackRange,
            float          attackCooldown,
            float          aggroRadius,
            float          moveSpeed,
            float          retreatFraction,
            bool           retreatDisabled,
            string         description     = "",
            int            supplyCost      = 0,
            float          aoeRadius       = 0f,
            DiplomaGame.Runtime.Data.TargetPriority targetPriority = DiplomaGame.Runtime.Data.TargetPriority.Units)
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
            so.FindProperty("_description").stringValue        = description;
            so.FindProperty("_supplyCost").intValue            = supplyCost;
            so.FindProperty("_maxHp").floatValue               = maxHp;
            so.FindProperty("_damage").floatValue              = damage;
            so.FindProperty("_attackRange").floatValue         = attackRange;
            so.FindProperty("_attackCooldown").floatValue      = attackCooldown;
            so.FindProperty("_aggroRadius").floatValue         = aggroRadius;
            so.FindProperty("_moveSpeed").floatValue           = moveSpeed;
            so.FindProperty("_retreatHpFraction").floatValue   = retreatFraction;
            so.FindProperty("_retreatDisabled").boolValue      = retreatDisabled;
            so.FindProperty("_aoeRadius").floatValue           = aoeRadius;
            so.FindProperty("_targetPriority").enumValueIndex  = (int)targetPriority;
            so.ApplyModifiedPropertiesWithoutUndo();

            return data;
        }

        // ----------------------------------------------------------------
        // v3: Tank UnitData + WarFactory BuildingData
        // ----------------------------------------------------------------

        /// <summary>
        /// Идемпотентно создаёт/обновляет Tank.asset, EnemyTank.asset, WarFactory.asset.
        /// </summary>
        internal static void CreateOrUpdateTankDataAssets()
        {
            EnsureFolder(UnitDataFolder);
            EnsureFolder(BuildingDataFolder);

            const string tankDesc = "Тяжёлый танк. Урон по площади, ломает здания.";

            // Tank (Player)
            CreateOrUpdateUnitData(
                assetName:       "Tank",
                displayName:     "Tank",
                description:     tankDesc,
                supplyCost:      3,
                maxHp:           280f,
                damage:          25f,
                attackRange:     5f,
                attackCooldown:  2.0f,
                aggroRadius:     12f,
                moveSpeed:       3.0f,
                retreatFraction: 0f,
                retreatDisabled: true,
                aoeRadius:       3.0f,
                targetPriority:  DiplomaGame.Runtime.Data.TargetPriority.Buildings);

            // EnemyTank
            CreateOrUpdateUnitData(
                assetName:       "EnemyTank",
                displayName:     "Enemy Tank",
                description:     tankDesc,
                supplyCost:      3,
                maxHp:           280f,
                damage:          25f,
                attackRange:     5f,
                attackCooldown:  2.0f,
                aggroRadius:     12f,
                moveSpeed:       3.0f,
                retreatFraction: 0f,
                retreatDisabled: true,
                aoeRadius:       3.0f,
                targetPriority:  DiplomaGame.Runtime.Data.TargetPriority.Buildings);

            // WarFactory BuildingData
            var tankData = AssetDatabase.LoadAssetAtPath<DiplomaGame.Runtime.Data.UnitData>(
                $"{UnitDataFolder}/Tank.asset");

            if (tankData == null)
                Debug.LogWarning("[Project Forge] Tank.asset не найден при создании WarFactory — поле produces будет пустым.");

            CreateOrUpdateBuildingData(
                assetName:          "WarFactory",
                displayName:        "War Factory",
                description:        "Военный завод. Производит танки.",
                cost:               200,
                maxHp:              600f,
                buildingType:       DiplomaGame.Runtime.Data.BuildingType.WarFactory,
                incomePerTick:      0,
                incomeTickInterval: 2f,
                produces:           tankData,
                productionTime:     12f,
                productionCost:     150);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Project Forge] Tank Data ассеты (v3) созданы/обновлены.");
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
                description:       "Штаб. Генерирует доход кристаллов.",
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
                description:       "Казарма. Производит боевых юнитов.",
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
                description:       "Экстрактор. Добывает кристаллы из месторождения.",
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
            int          productionCost,
            string       description  = "")
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
            so.FindProperty("_description").stringValue               = description;
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

        // ----------------------------------------------------------------
        // Tooltip Descriptions — обновить существующие ассеты
        // ----------------------------------------------------------------

        /// <summary>
        /// Проставляет _description и _supplyCost существующим ассетам Unit/Building/Ability Data.
        /// Идемпотентно: безопасно вызывать повторно.
        /// </summary>
        internal static void UpdateTooltipDescriptions()
        {
            // ---- Units ----
            SetUnitDescription("Marine",     "Универсальный боец. Атакует наземные цели.", 1);
            SetUnitDescription("EnemyGrunt", "Базовый враг. Атакует всё.", 1);
            SetUnitDescription("Tank",       "Тяжёлый танк. Урон по площади, ломает здания.", 3);
            SetUnitDescription("EnemyTank",  "Тяжёлый танк. Урон по площади, ломает здания.", 3);

            // ---- Buildings ----
            SetBuildingDescription("HQ",         "Штаб. Генерирует доход кристаллов.");
            SetBuildingDescription("Barracks",   "Казарма. Производит боевых юнитов.");
            SetBuildingDescription("Extractor",  "Экстрактор. Добывает кристаллы из месторождения.");
            SetBuildingDescription("WarFactory", "Военный завод. Производит танки.");

            // ---- Abilities ----
            SetAbilityDescription("Dash",      "Рывок вперёд. Позволяет уйти из-под огня.");
            SetAbilityDescription("Ability2",  "Ударная волна. Наносит урон врагам вокруг героя.");
            SetAbilityDescription("Ability3",  "Ремонтное поле. Восстанавливает HP союзников вокруг.");
            SetAbilityDescription("Ability4",  "Перегрузка. Временно повышает скорострельность и урон.");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Project Forge] Tooltip Descriptions обновлены.");
        }

        private static void SetUnitDescription(string assetName, string description, int supplyCost = 0)
        {
            string path = $"{UnitDataFolder}/{assetName}.asset";
            var    data = AssetDatabase.LoadAssetAtPath<UnitData>(path);
            if (data == null)
            {
                Debug.LogWarning($"[Project Forge] UnitData не найден: {path}");
                return;
            }

            var so = new SerializedObject(data);
            so.FindProperty("_description").stringValue = description;
            so.FindProperty("_supplyCost").intValue     = supplyCost;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetBuildingDescription(string assetName, string description)
        {
            string path = $"{BuildingDataFolder}/{assetName}.asset";
            var    data = AssetDatabase.LoadAssetAtPath<DiplomaGame.Runtime.Data.BuildingData>(path);
            if (data == null)
            {
                Debug.LogWarning($"[Project Forge] BuildingData не найден: {path}");
                return;
            }

            var so = new SerializedObject(data);
            so.FindProperty("_description").stringValue = description;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetAbilityDescription(string assetName, string description)
        {
            string path = $"{AbilityDataFolder}/{assetName}.asset";
            var    data = AssetDatabase.LoadAssetAtPath<AbilityData>(path);
            if (data == null)
            {
                Debug.LogWarning($"[Project Forge] AbilityData не найден: {path}");
                return;
            }

            var so = new SerializedObject(data);
            so.FindProperty("_description").stringValue = description;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ----------------------------------------------------------------
        // v6: Migrate Production Entries
        // ----------------------------------------------------------------

        /// <summary>
        /// Идемпотентно:
        ///   (а) создаёт HeavyMarine.asset;
        ///   (б) заполняет _productionEntries для Barracks и WarFactory;
        ///   (в) не трогает legacy-поля.
        /// </summary>
        internal static void MigrateProductionEntriesV6()
        {
            EnsureFolder(UnitDataFolder);
            EnsureFolder(BuildingDataFolder);

            // ----- (а) Создаём HeavyMarine.asset -----
            var heavyMarine = CreateOrUpdateUnitData(
                assetName:          "HeavyMarine",
                displayName:        "Heavy Marine",
                description:        "Тяжёлый пехотинец. Медленнее, но крепче и больнее.",
                supplyCost:         2,
                maxHp:              150f,
                damage:             14f,
                attackRange:        8f,
                attackCooldown:     1.1f,
                aggroRadius:        12f,
                moveSpeed:          4.2f,
                retreatFraction:    0.25f,
                retreatDisabled:    false);

            // Проставляем fireWhileRetreating = true (SerializedObject — поле уже есть в UnitData)
            {
                string hmPath = $"{UnitDataFolder}/HeavyMarine.asset";
                var    hmData = AssetDatabase.LoadAssetAtPath<UnitData>(hmPath);
                if (hmData != null)
                {
                    var so = new SerializedObject(hmData);
                    var prop = so.FindProperty("_fireWhileRetreating");
                    if (prop != null)
                    {
                        prop.boolValue = true;
                        so.ApplyModifiedPropertiesWithoutUndo();
                    }
                }
            }

            // ----- (б) Загружаем ассеты -----
            var marineData = AssetDatabase.LoadAssetAtPath<UnitData>($"{UnitDataFolder}/Marine.asset");
            var tankData   = AssetDatabase.LoadAssetAtPath<UnitData>($"{UnitDataFolder}/Tank.asset");

            if (marineData == null)
                Debug.LogWarning("[Project Forge v6] Marine.asset не найден — запустите Create/Update Unit Data Assets (M4) сначала.");
            if (heavyMarine == null)
                Debug.LogWarning("[Project Forge v6] HeavyMarine.asset не был создан.");
            if (tankData == null)
                Debug.LogWarning("[Project Forge v6] Tank.asset не найден — запустите Create/Update Tank Data (v3) сначала.");

            // ----- Barracks._productionEntries -----
            {
                string path = $"{BuildingDataFolder}/Barracks.asset";
                var    data = AssetDatabase.LoadAssetAtPath<BuildingData>(path);
                if (data == null)
                {
                    Debug.LogWarning("[Project Forge v6] Barracks.asset не найден.");
                }
                else
                {
                    var so      = new SerializedObject(data);
                    var entries = so.FindProperty("_productionEntries");
                    entries.arraySize = 2;

                    // entries[0] — Marine
                    SetProductionEntry(entries.GetArrayElementAtIndex(0), marineData, cost: 50, productionTime: 5f, hotkeyLabel: "T");
                    // entries[1] — HeavyMarine
                    SetProductionEntry(entries.GetArrayElementAtIndex(1), heavyMarine, cost: 90, productionTime: 8f, hotkeyLabel: "Y");

                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            // ----- WarFactory._productionEntries -----
            {
                string path = $"{BuildingDataFolder}/WarFactory.asset";
                var    data = AssetDatabase.LoadAssetAtPath<BuildingData>(path);
                if (data == null)
                {
                    Debug.LogWarning("[Project Forge v6] WarFactory.asset не найден. Запустите Create/Update Tank Data (v3) сначала.");
                }
                else
                {
                    var so      = new SerializedObject(data);
                    var entries = so.FindProperty("_productionEntries");
                    entries.arraySize = 1;

                    // entries[0] — Tank
                    SetProductionEntry(entries.GetArrayElementAtIndex(0), tankData, cost: 150, productionTime: 12f, hotkeyLabel: "T");

                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Project Forge] Production Entries (v6) перенесены успешно.");
        }

        /// <summary>Заполняет элемент SerializedProperty ProductionEntry через SerializedProperty.</summary>
        private static void SetProductionEntry(
            SerializedProperty entryProp,
            UnitData           unitData,
            int                cost,
            float              productionTime,
            string             hotkeyLabel)
        {
            entryProp.FindPropertyRelative("unitData").objectReferenceValue = unitData;
            entryProp.FindPropertyRelative("cost").intValue                 = cost;
            entryProp.FindPropertyRelative("productionTime").floatValue     = productionTime;
            entryProp.FindPropertyRelative("icon").objectReferenceValue     = null;
            entryProp.FindPropertyRelative("hotkeyLabel").stringValue       = hotkeyLabel;
        }

        // ----------------------------------------------------------------
        // v7: Tech Assets
        // ----------------------------------------------------------------

        /// <summary>
        /// Идемпотентно создаёт/обновляет TechData-ассеты и заполняет Barracks._techEntries.
        /// </summary>
        internal static void CreateOrUpdateTechAssetsV7()
        {
            EnsureFolder(TechDataFolder);
            EnsureFolder(BuildingDataFolder);

            // ----- Tech_Armoring -----
            var armoring = CreateOrUpdateTechData(
                assetName:       "Tech_Armoring",
                displayName:     "Бронирование",
                description:     "+20% здоровья пехоте.",
                cost:            150,
                researchTime:    40f,
                hotkeyLabel:     "R",
                effectType:      DiplomaGame.Runtime.Data.TechEffect.MaxHpMultiplier,
                effectMagnitude: 0.20f,
                prerequisites:   null,
                infantryOnly:    true);

            // ----- Tech_Weapons -----
            var weapons = CreateOrUpdateTechData(
                assetName:       "Tech_Weapons",
                displayName:     "Усиленные стволы",
                description:     "+15% урона всем юнитам.",
                cost:            175,
                researchTime:    45f,
                hotkeyLabel:     "G",
                effectType:      DiplomaGame.Runtime.Data.TechEffect.DamageMultiplier,
                effectMagnitude: 0.15f,
                prerequisites:   null,
                infantryOnly:    false);

            // ----- Tech_RapidFire (prerequisite: Tech_Weapons) -----
            CreateOrUpdateTechData(
                assetName:       "Tech_RapidFire",
                displayName:     "Расширенные обоймы",
                description:     "−15% перезарядки. Требует Усиленные стволы.",
                cost:            200,
                researchTime:    50f,
                hotkeyLabel:     "X",
                effectType:      DiplomaGame.Runtime.Data.TechEffect.AttackCooldownMultiplier,
                effectMagnitude: -0.15f,
                prerequisites:   weapons != null ? new[] { weapons } : null,
                infantryOnly:    false);

            // ----- Загружаем три ассета для заполнения Barracks -----
            var armoringAsset  = AssetDatabase.LoadAssetAtPath<DiplomaGame.Runtime.Data.TechData>(
                $"{TechDataFolder}/Tech_Armoring.asset");
            var weaponsAsset   = AssetDatabase.LoadAssetAtPath<DiplomaGame.Runtime.Data.TechData>(
                $"{TechDataFolder}/Tech_Weapons.asset");
            var rapidFireAsset = AssetDatabase.LoadAssetAtPath<DiplomaGame.Runtime.Data.TechData>(
                $"{TechDataFolder}/Tech_RapidFire.asset");

            // ----- Barracks._techEntries -----
            {
                string path = $"{BuildingDataFolder}/Barracks.asset";
                var    data = AssetDatabase.LoadAssetAtPath<DiplomaGame.Runtime.Data.BuildingData>(path);
                if (data == null)
                {
                    Debug.LogWarning("[Project Forge v7] Barracks.asset не найден. Запустите Create/Update Building Data (M5) сначала.");
                }
                else
                {
                    var so      = new SerializedObject(data);
                    var entries = so.FindProperty("_techEntries");
                    entries.arraySize = 3;

                    SetTechEntry(entries.GetArrayElementAtIndex(0), armoringAsset);
                    SetTechEntry(entries.GetArrayElementAtIndex(1), weaponsAsset);
                    SetTechEntry(entries.GetArrayElementAtIndex(2), rapidFireAsset);

                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Project Forge] Tech Assets (v7) созданы/обновлены.");
        }

        private static DiplomaGame.Runtime.Data.TechData CreateOrUpdateTechData(
            string   assetName,
            string   displayName,
            string   description,
            int      cost,
            float    researchTime,
            string   hotkeyLabel,
            DiplomaGame.Runtime.Data.TechEffect effectType,
            float    effectMagnitude,
            DiplomaGame.Runtime.Data.TechData[] prerequisites,
            bool     infantryOnly)
        {
            string path     = $"{TechDataFolder}/{assetName}.asset";
            var    existing = AssetDatabase.LoadAssetAtPath<DiplomaGame.Runtime.Data.TechData>(path);

            DiplomaGame.Runtime.Data.TechData data;
            if (existing != null)
            {
                data = existing;
            }
            else
            {
                data = ScriptableObject.CreateInstance<DiplomaGame.Runtime.Data.TechData>();
                AssetDatabase.CreateAsset(data, path);
            }

            var so = new SerializedObject(data);
            so.FindProperty("_displayName").stringValue         = displayName;
            so.FindProperty("_description").stringValue         = description;
            so.FindProperty("_cost").intValue                   = cost;
            so.FindProperty("_researchTime").floatValue         = researchTime;
            so.FindProperty("_hotkeyLabel").stringValue         = hotkeyLabel;
            so.FindProperty("_effectType").enumValueIndex       = (int)effectType;
            so.FindProperty("_effectMagnitude").floatValue      = effectMagnitude;
            so.FindProperty("_infantryOnly").boolValue          = infantryOnly;

            // Prerequisites
            var prereqProp = so.FindProperty("_prerequisites");
            if (prerequisites != null && prerequisites.Length > 0)
            {
                prereqProp.arraySize = prerequisites.Length;
                for (int i = 0; i < prerequisites.Length; i++)
                    prereqProp.GetArrayElementAtIndex(i).objectReferenceValue = prerequisites[i];
            }
            else
            {
                prereqProp.arraySize = 0;
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            return data;
        }

        private static void SetTechEntry(SerializedProperty entryProp, DiplomaGame.Runtime.Data.TechData techData)
        {
            entryProp.FindPropertyRelative("techData").objectReferenceValue = techData;
        }

        // ----------------------------------------------------------------
        // v10: Migrate Localization EN Fields
        // ----------------------------------------------------------------

        private const string LocalizationDataFolder = "Assets/_Project/Data/Localization";

        /// <summary>
        /// Проставляет _displayNameEn / _descriptionEn на все Unit/Building/Ability/Tech ассеты.
        /// Идемпотентно: безопасно запускать повторно.
        /// </summary>
        internal static void MigrateLocalizationEnFieldsV10()
        {
            // Units
            SetEnFields<UnitData>(UnitDataFolder, "Marine",      "Marine",       "Universal infantry. Attacks ground targets.");
            SetEnFields<UnitData>(UnitDataFolder, "EnemyGrunt",  "Enemy Grunt",  "Basic enemy. Attacks everything.");
            SetEnFields<UnitData>(UnitDataFolder, "Tank",        "Tank",         "Heavy tank. AoE damage, good against buildings.");
            SetEnFields<UnitData>(UnitDataFolder, "EnemyTank",   "Enemy Tank",   "Heavy tank. AoE damage, good against buildings.");
            SetEnFields<UnitData>(UnitDataFolder, "HeavyMarine", "Heavy Marine", "Heavier infantry. Slower but tougher.");

            // Buildings
            SetEnFields<BuildingData>(BuildingDataFolder, "HQ",         "Headquarters", "Command center. Generates crystal income.");
            SetEnFields<BuildingData>(BuildingDataFolder, "Barracks",   "Barracks",     "Trains combat units.");
            SetEnFields<BuildingData>(BuildingDataFolder, "Extractor",  "Extractor",    "Mines crystals from deposits.");
            SetEnFields<BuildingData>(BuildingDataFolder, "WarFactory", "War Factory",  "Heavy vehicle plant. Produces tanks.");

            // Abilities
            SetEnFields<AbilityData>(AbilityDataFolder, "Dash",     "Dash",           "Dash forward. Escape from enemy fire.");
            SetEnFields<AbilityData>(AbilityDataFolder, "Ability2", "Shockwave",       "Shockwave. Damages enemies around the hero.");
            SetEnFields<AbilityData>(AbilityDataFolder, "Ability3", "Repair Field",    "Repair field. Restores HP of nearby allies.");
            SetEnFields<AbilityData>(AbilityDataFolder, "Ability4", "Overcharge",      "Overcharge. Temporarily boosts fire rate and damage.");

            // Tech
            SetEnFields<TechData>(TechDataFolder, "Tech_Armoring",  "Armoring",        "+20% HP for infantry.");
            SetEnFields<TechData>(TechDataFolder, "Tech_Weapons",   "Enhanced Weapons", "+15% damage for all units.");
            SetEnFields<TechData>(TechDataFolder, "Tech_RapidFire", "Rapid Fire",       "-15% attack cooldown. Requires Enhanced Weapons.");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Project Forge] Localization EN fields (v10) migrated.");
        }

        private static void SetEnFields<T>(string folder, string assetName, string displayNameEn, string descriptionEn)
            where T : ScriptableObject
        {
            string path = $"{folder}/{assetName}.asset";
            var    data = AssetDatabase.LoadAssetAtPath<T>(path);
            if (data == null)
            {
                Debug.LogWarning($"[Project Forge v10] {typeof(T).Name} не найден: {path}");
                return;
            }

            var so = new SerializedObject(data);
            var nameProp = so.FindProperty("_displayNameEn");
            var descProp = so.FindProperty("_descriptionEn");

            if (nameProp != null) nameProp.stringValue = displayNameEn;
            if (descProp != null) descProp.stringValue = descriptionEn;

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ----------------------------------------------------------------
        // v10: Create/Update LocTable
        // ----------------------------------------------------------------

        /// <summary>
        /// Создаёт или обновляет Assets/_Project/Data/Localization/LocTable.asset
        /// со всеми ключами глоссария (50+ записей).
        /// </summary>
        internal static void CreateOrUpdateLocTableV10()
        {
            EnsureFolder(LocalizationDataFolder);

            const string path = "Assets/_Project/Data/Localization/LocTable.asset";
            var existing = AssetDatabase.LoadAssetAtPath<LocTable>(path);

            LocTable table;
            if (existing != null)
            {
                table = existing;
            }
            else
            {
                table = ScriptableObject.CreateInstance<LocTable>();
                AssetDatabase.CreateAsset(table, path);
            }

            // Полный глоссарий: (key, ru, en)
            var entries = new (string key, string ru, string en)[]
            {
                // HUD
                ("hud.crystals",          "Кристаллы: {0}",                    "Crystals: {0}"),
                ("hud.selected_units",    "Выбрано: {0} юнитов",               "Selected: {0} units"),
                ("hud.queue_count",       "Очередь: {0}",                      "Queue: {0}"),
                ("hud.hint_train",        "Нажмите кнопку для производства",   "Press button to train"),

                // Tooltip — общие метки
                ("tooltip.cost_label",          "Стоимость: ",                 "Cost: "),
                ("tooltip.time_label",          "Время: ",                     "Time: "),
                ("tooltip.supply_label",        "Supply: ",                    "Supply: "),
                ("tooltip.research_prefix",     "Исследование: ",              "Research: "),
                ("tooltip.production_prefix",   "Производство: ",              "Train: "),
                ("tooltip.requires_label",      "Требует: ",                   "Requires: "),

                // Tooltip — ресурсы и карта
                ("tooltip.crystals_title",  "Кристаллы",                                                    "Crystals"),
                ("tooltip.crystals_desc",   "Основная валюта. Добываются экстракторами и поступают от штаба.", "Primary currency. Mined by extractors and generated by HQ."),
                ("tooltip.minimap_title",   "Миникарта",                                                    "Minimap"),
                ("tooltip.minimap_desc",    "ПКМ — задать точку сбора. Показывает союзников (синие) и врагов (красные).", "RMB — set rally point. Shows allies (blue) and enemies (red)."),

                // Tooltip — способности
                ("tooltip.ability_empty_title",  "Способность",                    "Ability"),
                ("tooltip.ability_empty_desc",   "Слот способности пуст.",         "Ability slot is empty."),
                ("tooltip.ability_cooldown",     "Кулдаун: {0}с",                  "Cooldown: {0}s"),
                ("tooltip.ability_distance",     "Дистанция: {0}",                 "Distance: {0}"),
                ("tooltip.ability_damage",       "Урон: {0}",                      "Damage: {0}"),
                ("tooltip.ability_radius",       "Радиус: {0}",                    "Radius: {0}"),
                ("tooltip.ability_heal",         "Лечение: {0}",                   "Heal: {0}"),
                ("tooltip.ability_duration",     "Длительность: {0}с",             "Duration: {0}s"),
                ("tooltip.ability_dmg_mult",     "Урон ×{0}",                      "Damage ×{0}"),

                // Пауза
                ("pause.title",         "Пауза",          "Pause"),
                ("pause.btn_continue",  "Продолжить",     "Continue"),
                ("pause.btn_settings",  "Настройки",      "Settings"),
                ("pause.btn_exit_menu", "Выйти в меню",   "Exit to Menu"),
                ("pause.btn_quit",      "Выйти из игры",  "Quit"),

                // Настройки
                ("settings.title",          "Настройки",          "Settings"),
                ("settings.quality_label",  "Качество",           "Quality"),
                ("settings.fullscreen",     "Полный экран",       "Fullscreen"),
                ("settings.master_volume",  "Громкость",          "Master Volume"),
                ("settings.music_volume",   "Музыка",             "Music"),
                ("settings.sfx_volume",     "Звуки",              "SFX"),
                ("settings.sensitivity",    "Чувствительность",   "Sensitivity"),
                ("settings.language",       "Язык",               "Language"),
                ("settings.btn_back",       "Назад",              "Back"),

                // Главное меню
                ("menu.btn_play",     "Играть",         "Play"),
                ("menu.btn_settings", "Настройки",      "Settings"),
                ("menu.btn_quit",     "Выйти",          "Quit"),
                ("menu.title",        "Диплом: RTS+TPS", "Diploma: RTS+TPS"),

                // GameOver
                ("gameover.victory",        "Победа!",          "Victory!"),
                ("gameover.defeat",         "Поражение",        "Defeat"),
                ("gameover.btn_restart",    "Заново",           "Restart"),
                ("gameover.btn_main_menu",  "Главное меню",     "Main Menu"),

                // Статистика матча
                ("stats.duration_format",    "Длительность: {0:00}:{1:00}", "Duration: {0:00}:{1:00}"),
                ("stats.duration_empty",     "Длительность: --:--",         "Duration: --:--"),
                ("stats.header_player",      "Игрок",                       "Player"),
                ("stats.header_enemy",       "Враг",                        "Enemy"),
                ("stats.kills",              "Убито",                       "Kills"),
                ("stats.losses",             "Потери",                      "Losses"),
                ("stats.damage_dealt",       "Нанесено урона",              "Damage Dealt"),
                ("stats.damage_taken",       "Получено урона",              "Damage Taken"),
                ("stats.crystals",           "Кристаллы",                   "Crystals"),
                ("stats.produced",           "Произведено",                 "Produced"),
                ("stats.army_peak",          "Пик армии",                   "Army Peak"),
            };

            var so = new SerializedObject(table);
            var entriesProp = so.FindProperty("_entries");
            entriesProp.arraySize = entries.Length;

            for (int i = 0; i < entries.Length; i++)
            {
                var elem = entriesProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("key").stringValue = entries[i].key;
                elem.FindPropertyRelative("ru").stringValue  = entries[i].ru;
                elem.FindPropertyRelative("en").stringValue  = entries[i].en;
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Project Forge] LocTable (v10) создана/обновлена: {entries.Length} записей → {path}");
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
