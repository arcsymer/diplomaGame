# diplomaGame — гибрид RTS + TPS (дипломный проект)

3D-игра на Unity: игрок одновременно командует армией в RTS-режиме (вид сверху) и управляет героем в TPS-режиме (от третьего лица). Академическая новизна — сознательно сниженный микроменеджмент. Рабочее название `diplomaGame`, финальное появится позже.

## Жёсткие ограничения
- **Нулевой бюджет** (`zero-budget-constraint.md`, ADR-009): никаких покупок, подписок, платных API, триалов с картой. Только FOSS, локальная генерация, CC0/CC-BY-ассеты.
- Любой внешний ассет — только с проверенной лицензией, заносится в `Docs-Vault/Licenses & Attribution.md`.
- Всё, что требует ручной работы в Unity-редакторе, оформляется кнопкой в **Project Forge** (`Tools/Project Forge`) — единственный Editor-тул, одноразовые скрипты запрещены.

## Стек
- **Unity 6000.4.9f1** (6.4), URP 17.4, Windows-таргет.
- Пакеты: Input System 1.19 (раздельные Action Maps RTS/TPS), Cinemachine **3.1** (⚠️ API 3.x: `Unity.Cinemachine`, единый `CinemachineCamera` — не использовать рецепты 2.x), AI Navigation 2.0, Test Framework 1.6, uGUI 2.0 (TextMeshPro внутри, отдельного пакета нет).
- UI — **uGUI** (не UI Toolkit), твининг — PrimeTween (добавим в M6), ИИ юнитов — собственная FSM (пакет Behavior не используем, ADR-005).

## Структура
- `Assets/_Project/` — весь контент игры: `Scripts/` (runtime), `Editor/ProjectForge/`, `Scenes/`, `Prefabs/`, `Data/` (ScriptableObject-ассеты), `Art/`, `Audio/`, `UI/`, `VFX/`, `Tests/`.
- `Docs-Vault/` — Obsidian-хранилище: GDD, архитектура, роадмап, **Decision Log (ADR)**, ресёрч, отчёты сессий, статистика. Отчёт после каждой значимой сессии; статистика накапливается, не переписывается.
- `Builds/` — локальные сборки (в .gitignore).

## Конвенции C#
- Данные (статы юнитов/зданий/способностей/звуки) — ScriptableObject, не хардкод.
- `[SerializeField] private`, кэширование компонентов в `Awake()`, без аллокаций в Update-путях, object pooling для пуль/эффектов, события вместо жёсткой связности.
- Editor-код: `DestroyImmediate`, идемпотентные операции, `SerializedObject` для приватных полей.
- Логика отделяется от MonoBehaviour для тестируемости; EditMode/PlayMode тесты на ключевую логику.

## Git
- Ветка `main` стабильная, conventional commits (`feat:`, `fix:`, `docs:`, `chore:`), теги по майлстоунам (semver).
- LFS — выборочно (fbx/blend/psd/tga/wav и т.п.); мелкие png/ogg — обычный git. Решение в ADR-008.
- `git push` — только после явного подтверждения пользователя (кроме случаев, когда он уже дал добро на сессию).
- Релизы: `gh release create vX.Y.Z <zip>` — билд прикладывается к GitHub Release.

## Текущее состояние
- **M0–M12 завершены**, релиз v1.0.0 с Windows-билдом опубликован (GitHub Releases).
- **Фаза улучшения (v3)**: бесконечный цикл Measure → Benchmark (vs SC2) → … → Commit по сферам, до команды «СТОП». Гэп-анализ и приоритеты — `Docs-Vault/Improvements/`.
- Измерительные харнессы: PlayMode-тесты категории `Balance` (армия vs армия, стресс+перф) пишут JSON в `Docs-Vault/Stats/`; вкладка **Improve** в Forge (`ForgeBatch.ExportImprovementMetrics`) дописывает строку в `Improvements/Metrics.md`.
- Тесты локально: `Unity.exe -batchmode -runTests -testPlatform EditMode|PlayMode -testResults <xml> -logFile <log>` (Unity 6000.4.9f1).
- CI: GitHub Actions + GameCI v4 (образ запинен на 6000.4.10f1, ADR-008); секреты настроены.
