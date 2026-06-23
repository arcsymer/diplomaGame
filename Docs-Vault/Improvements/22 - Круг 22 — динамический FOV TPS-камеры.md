# Круг 22 — динамический FOV TPS-камеры (kick на dash/абилку)

Дата: 2026-06-23. Ветка `improve/circle-22` → main, тег `v1.22.0`.

## Improvement: кинетический FOV героя
TPS-камера имела фиксированный FOV. Динамический FOV добавляет кинетику: при касте Dash и Overcharge — быстрый kick (+9° → плавный возврат за ~0.25 с). Тезис: прямое управление героем должно ощущаться кинетично.
- **Что:** `DynamicFovController` (на GameManagers) ловит `AbilitySystem.AbilityCast`, на Dash/Overcharge даёт FOV-kick; lerp обратно к базе. Только TPS-режим (гард `GameModeController.ModeChanged`); RTS-камеру не трогает. LateUpdate, ноль аллокаций, OnDisable восстанавливает FOV.
- **CM 3.x API:** `CinemachineCamera.Lens` (struct) — read → mutate FieldOfView → write back.
- **Честное ограничение:** sprint-widen ПРОПУЩЕН — у `HeroController` нет состояния спринта (фикс. moveSpeed); добавление требует API-изменения HeroController (бэклог).
- **Код:** чистая логика `DynamicFovLogic` (TriggerKick / GetTargetFov / StepFov / TickKick / Tick) — 23 EditMode-теста. 3 поля GameFeelSettings (kickAmount 9, kickDuration 0.08, returnSpeed 12). Forge `SetupDynamicFov` (+ ForceReserializeAssets — урок круга 20/21 применён превентивно).

## Верификация
- Компиляция чистая, Forge без исключений (6 ссылок прошиты, GameFeelSettings.asset 9/0.08/12 персистнуты). **EditMode 464/464** (441 + 23), 0 регрессий.
- PlayMode (FOV в сцене) — на CI.
- Заметка: субагенты ловили транзиентные 529 Overloaded в этот период (серверная нагрузка); круг 21 верифицирован оркестратором inline, круги 22 — субагентами после прояснения.

## Бэклог (HERO/TPS-сфера в основном отполирована)
- Sprint-widen FOV (нужен sprint-state в HeroController — backend).
- Направленный индикатор урона (Health API с позицией источника — backend ripple).
- Aim-spread/bloom (геймплей-риск для power-fantasy).
- Адаптивное аудио (CC0-сорсинг + эстетика — нужен юзер).
