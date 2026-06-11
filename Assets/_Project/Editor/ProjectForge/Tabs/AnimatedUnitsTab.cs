using System;
using System.Collections.Generic;
using DiplomaGame.Runtime.Combat;
using DiplomaGame.Runtime.Units;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;

namespace DiplomaGame.Editor
{
    /// <summary>
    /// v5 — Animated Units: заменяет статичные модели пехоты на анимированные мехи Quaternius.
    /// Marine → Mike.fbx (PlayerBlue), EnemyGrunt → George.fbx (EnemyRed).
    /// Создаёт AnimatorController в Assets/_Project/Art/Animations/.
    /// Добавляет Animator + UnitAnimator на префабы.
    /// Идемпотентно.
    /// </summary>
    internal static class AnimatedUnitsSetup
    {
        // ----------------------------------------------------------------
        // Пути
        // ----------------------------------------------------------------

        private const string MikeFbxPath    = "Assets/_Project/Art/Models/Units/Animated/Mike.fbx";
        private const string GeorgeFbxPath  = "Assets/_Project/Art/Models/Units/Animated/George.fbx";
        private const string AnimationsDir  = "Assets/_Project/Art/Animations";

        private const string MarineControllerPath = "Assets/_Project/Art/Animations/Marine_Controller.controller";
        private const string GruntControllerPath  = "Assets/_Project/Art/Animations/Grunt_Controller.controller";

        private const string MarinePrefabPath  = "Assets/_Project/Prefabs/Units/TestUnit.prefab";
        private const string GruntPrefabPath   = "Assets/_Project/Prefabs/Units/EnemyUnit.prefab";

        private const string PlayerBluePath    = "Assets/_Project/Art/Materials/PlayerBlue.mat";
        private const string EnemyRedPath      = "Assets/_Project/Art/Materials/EnemyRed.mat";

        // Целевая высота персонажа в метрах (по наибольшему измерению bbox — ADR-012)
        private const float TargetHeight = 1.2f;

        // ----------------------------------------------------------------
        // Точка входа
        // ----------------------------------------------------------------

        /// <summary>
        /// Основной идемпотентный метод. Вызывается кнопкой Forge и ForgeBatch.
        /// </summary>
        internal static void SetupAnimatedUnits()
        {
            EnsureFolder(AnimationsDir);

            // 1. Настроить импорт FBX
            ConfigureImporter(MikeFbxPath);
            ConfigureImporter(GeorgeFbxPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 2. Считать клипы
            var mikeClips   = LoadClips(MikeFbxPath);
            var georgeClips = LoadClips(GeorgeFbxPath);

            LogClipNames("Mike",   mikeClips);
            LogClipNames("George", georgeClips);

            // 3. Создать AnimatorController'ы
            var mikeController   = BuildController(MarineControllerPath, mikeClips);
            var georgeController = BuildController(GruntControllerPath, georgeClips);

            // 4. Применить к префабам
            var playerMat = AssetDatabase.LoadAssetAtPath<Material>(PlayerBluePath);
            var enemyMat  = AssetDatabase.LoadAssetAtPath<Material>(EnemyRedPath);

            ApplyToUnitPrefab(MarinePrefabPath, MikeFbxPath,   mikeController,   playerMat);
            ApplyToUnitPrefab(GruntPrefabPath,  GeorgeFbxPath, georgeController, enemyMat);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[Project Forge] Setup Animated Units (v5) завершён.");
        }

        // ----------------------------------------------------------------
        // 1. ModelImporter
        // ----------------------------------------------------------------

        private static void ConfigureImporter(string fbxPath)
        {
            var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (importer == null)
            {
                Debug.LogWarning($"[Project Forge] ModelImporter не найден для: {fbxPath}. " +
                                 "Убедитесь, что FBX скопированы и Unity их видит.");
                return;
            }

            bool changed = false;

            if (importer.animationType != ModelImporterAnimationType.Generic)
            {
                importer.animationType = ModelImporterAnimationType.Generic;
                changed = true;
            }

            if (!importer.importAnimation)
            {
                importer.importAnimation = true;
                changed = true;
            }

            // Отключаем ненужное
            if (importer.importCameras)  { importer.importCameras = false; changed = true; }
            if (importer.importLights)   { importer.importLights  = false; changed = true; }

            // Материалы: извлекаем в отдельные ассеты, чтобы потом заменить на team-color
            if (importer.materialImportMode != ModelImporterMaterialImportMode.ImportViaMaterialDescription)
            {
                importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
                Debug.Log($"[Project Forge] Настройки импорта обновлены: {fbxPath}");
            }
        }

        // ----------------------------------------------------------------
        // 2. Загрузка клипов
        // ----------------------------------------------------------------

        private static AnimationClip[] LoadClips(string fbxPath)
        {
            var allAssets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            var clips = new List<AnimationClip>();
            foreach (var asset in allAssets)
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                    clips.Add(clip);
            }
            return clips.ToArray();
        }

        private static void LogClipNames(string modelName, AnimationClip[] clips)
        {
            if (clips.Length == 0)
            {
                Debug.LogWarning($"[Project Forge] {modelName}: клипы не найдены. " +
                                 "Проверьте import settings (importAnimation=true).");
                return;
            }

            var sb = new System.Text.StringBuilder();
            sb.Append($"[Project Forge] {modelName} клипы ({clips.Length}): ");
            for (int i = 0; i < clips.Length; i++)
            {
                if (i > 0) sb.Append(", ");
                sb.Append(clips[i].name);
            }
            Debug.Log(sb.ToString());
        }

        // ----------------------------------------------------------------
        // 3. Поиск клипа по категории
        // ----------------------------------------------------------------

        private static AnimationClip FindClip(AnimationClip[] clips, params string[] keywords)
        {
            // case-insensitive contains поиск по ключевым словам
            foreach (var clip in clips)
            {
                string lower = clip.name.ToLowerInvariant();
                foreach (var kw in keywords)
                {
                    if (lower.Contains(kw.ToLowerInvariant()))
                        return clip;
                }
            }
            return null;
        }

        // ----------------------------------------------------------------
        // 4. AnimatorController
        // ----------------------------------------------------------------

        /// <summary>
        /// Создаёт или пересоздаёт AnimatorController с четырьмя состояниями:
        /// Idle (default, loop), Run (loop, условие IsMoving=true),
        /// Attack (trigger, exit → Idle), Death (trigger, без выхода).
        /// </summary>
        private static AnimatorController BuildController(string controllerPath, AnimationClip[] clips)
        {
            var idleClip   = FindClip(clips, "idle");
            var runClip    = FindClip(clips, "run", "walk");
            // Юниты стреляют: приоритет у Shoot; ближние (Punch/Kick) — только fallback.
            var attackClip = FindClip(clips, "shoot", "fire", "attack", "punch", "kick");
            var deathClip  = FindClip(clips, "death", "die", "dead");

            // Fallback: если Idle не нашли — берём первый клип
            if (idleClip == null && clips.Length > 0)
                idleClip = clips[0];

            // Удаляем старый контроллер если есть (идемпотентность)
            var existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (existing != null)
                AssetDatabase.DeleteAsset(controllerPath);

            var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);

            // Добавляем параметры
            controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Attack",   AnimatorControllerParameterType.Trigger);
            controller.AddParameter("Die",      AnimatorControllerParameterType.Trigger);

            var stateMachine = controller.layers[0].stateMachine;

            // --- Idle (default) ---
            var idleState = stateMachine.AddState("Idle");
            idleState.motion = idleClip;
            if (idleClip != null)
                SetLooping(idleClip, true);
            stateMachine.defaultState = idleState;

            // --- Run ---
            var runState = stateMachine.AddState("Run");
            runState.motion = runClip;
            if (runClip != null)
                SetLooping(runClip, true);

            // --- Attack ---
            var attackState = stateMachine.AddState("Attack");
            attackState.motion = attackClip;
            if (attackClip != null)
                SetLooping(attackClip, false);

            // --- Death ---
            var deathState = stateMachine.AddState("Death");
            deathState.motion = deathClip;
            if (deathClip != null)
                SetLooping(deathClip, false);

            // --- Переходы ---

            // Idle → Run (IsMoving = true)
            var idleToRun = idleState.AddTransition(runState);
            idleToRun.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");
            idleToRun.hasExitTime = false;
            idleToRun.duration    = 0.1f;

            // Run → Idle (IsMoving = false)
            var runToIdle = runState.AddTransition(idleState);
            runToIdle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");
            runToIdle.hasExitTime = false;
            runToIdle.duration    = 0.1f;

            // Any State → Attack (trigger)
            var anyToAttack = stateMachine.AddAnyStateTransition(attackState);
            anyToAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");
            anyToAttack.hasExitTime    = false;
            anyToAttack.duration       = 0.05f;
            anyToAttack.canTransitionToSelf = false;

            // Attack → Idle (exit time)
            var attackToIdle = attackState.AddTransition(idleState);
            attackToIdle.hasExitTime = true;
            attackToIdle.exitTime    = 0.9f;
            attackToIdle.duration    = 0.1f;

            // Any State → Death (trigger)
            var anyToDeath = stateMachine.AddAnyStateTransition(deathState);
            anyToDeath.AddCondition(AnimatorConditionMode.If, 0, "Die");
            anyToDeath.hasExitTime    = false;
            anyToDeath.duration       = 0.05f;
            anyToDeath.canTransitionToSelf = false;

            // Death не имеет выхода — юнит уничтожается после задержки 0.1 с (UnitCombat.OnDied)

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Project Forge] AnimatorController создан: {controllerPath} " +
                      $"[Idle={idleClip?.name ?? "null"}, " +
                      $"Run={runClip?.name ?? "null"}, " +
                      $"Attack={attackClip?.name ?? "null"} (компромисс: ближний жест), " +
                      $"Death={deathClip?.name ?? "null"}]");

            return controller;
        }

        /// <summary>
        /// Применяет loop-настройку к клипу через ModelImporter (если клип встроен в FBX).
        /// Если клип отдельный ассет — пытается через AnimationClipSettings.
        /// </summary>
        private static void SetLooping(AnimationClip clip, bool loop)
        {
            // Встроенные в FBX клипы — только через ModelImporter
            var assetPath = AssetDatabase.GetAssetPath(clip);
            if (string.IsNullOrEmpty(assetPath)) return;

            var importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null) return;

            var clipAnimations = importer.clipAnimations;
            if (clipAnimations == null || clipAnimations.Length == 0)
                clipAnimations = importer.defaultClipAnimations;

            bool changed = false;
            foreach (var ca in clipAnimations)
            {
                if (ca.name == clip.name)
                {
                    if (ca.loop != loop || ca.loopTime != loop)
                    {
                        ca.loop     = loop;
                        ca.loopTime = loop;
                        changed = true;
                    }
                    break;
                }
            }

            if (changed)
            {
                importer.clipAnimations = clipAnimations;
                importer.SaveAndReimport();
            }
        }

        // ----------------------------------------------------------------
        // 5. Применение к префабу
        // ----------------------------------------------------------------

        private static void ApplyToUnitPrefab(
            string             prefabPath,
            string             fbxPath,
            AnimatorController controller,
            Material           teamMat)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null)
            {
                Debug.LogWarning($"[Project Forge] Префаб не найден: {prefabPath}");
                return;
            }

            var fbxAsset = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
            if (fbxAsset == null)
            {
                Debug.LogWarning($"[Project Forge] FBX не найден: {fbxPath}. " +
                                 "Убедитесь что файл скопирован и проимпортирован.");
                return;
            }

            using (var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath))
            {
                var root = scope.prefabContentsRoot;

                // Скрыть MeshRenderer корня (капсула)
                var capsuleMr = root.GetComponent<MeshRenderer>();
                if (capsuleMr != null)
                    capsuleMr.enabled = false;

                // Удалить старый Visual (независимо от типа — старый character-X.fbx или анимированный)
                var oldVisualTf = root.transform.Find("Visual");
                if (oldVisualTf != null)
                    UnityEngine.Object.DestroyImmediate(oldVisualTf.gameObject);

                // Создать новый Visual из анимированного FBX
                var visual = (GameObject)PrefabUtility.InstantiatePrefab(fbxAsset, root.transform);
                visual.name = "Visual";
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                visual.transform.localScale    = Vector3.one;

                // Нормализация масштаба по баундам — ADR-012
                NormalizeVisualByBounds(visual, TargetHeight);

                // Team-color материал
                if (teamMat != null)
                {
                    foreach (var mr in visual.GetComponentsInChildren<MeshRenderer>(true))
                        mr.sharedMaterial = teamMat;
                    foreach (var smr in visual.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                        smr.sharedMaterial = teamMat;
                }

                // Animator на Visual (там риг)
                var animator = visual.GetComponent<Animator>();
                if (animator == null)
                    animator = visual.AddComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;

                // UnitAnimator на корне префаба (ищет Animator в children)
                var unitAnimator = root.GetComponent<UnitAnimator>();
                if (unitAnimator == null)
                    root.AddComponent<UnitAnimator>();
            }

            Debug.Log($"[Project Forge] Анимированный визуал применён: {prefabPath}");
        }

        // ----------------------------------------------------------------
        // Нормализация масштаба по баундам (ADR-012)
        // ----------------------------------------------------------------

        private static void NormalizeVisualByBounds(GameObject visual, float targetSize)
        {
            // Собираем bounds всех рендереров
            var renderers = visual.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0) return;

            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
                bounds.Encapsulate(renderers[i].bounds);

            // Берём наибольшее из трёх измерений
            float maxDim = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            if (maxDim < 0.0001f) return;

            float factor = targetSize / maxDim;
            visual.transform.localScale = Vector3.one * factor;
        }

        // ----------------------------------------------------------------
        // Утилиты
        // ----------------------------------------------------------------

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
