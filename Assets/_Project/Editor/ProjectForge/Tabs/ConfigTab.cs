using DiplomaGame.Runtime.Data;
using UnityEditor;
using UnityEngine;

namespace DiplomaGame.Editor
{
    /// <summary>
    /// Вкладка Config — создание и обновление ScriptableObject-ассетов данных юнитов.
    /// M4: Marine.asset и EnemyGrunt.asset.
    /// </summary>
    internal sealed class ConfigTab : IForgeTab
    {
        private const string UnitDataFolder = "Assets/_Project/Data/Units";

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
