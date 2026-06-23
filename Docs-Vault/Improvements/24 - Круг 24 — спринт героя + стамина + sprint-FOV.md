# Круг 24 — спринт героя + стамина + sprint-widen FOV

Дата: 2026-06-23. Ветка `improve/circle-24` → main, тег `v1.24.0`.

## Improvement: мобильность героя (геймплей-механика, не только полиш)
Герой получил СПРИНТ — реальная мобильность (репозиция/кайт/побег), активное кинетичное управление (тезис reduced-micro). Завершает отложенный sprint-widen FOV круга 22.
- **Backend (sprint + stamina):** `SprintStaminaLogic` (чистая статика, `Tick` → next stamina + isSprinting; 12 тестов). `HeroController`: спринт ×1.6 скорости при зажатом Sprint + движении; стамина max 100, drain 25/с, regen 15/с, minToStart 10, авто-стоп на 0; БЕЗ траты при стоянии (сознательно). Экспонирует `IsSprinting` + `StaminaNormalized`. Input — action `Sprint` (Hold, Left Shift / геймпад LB) добавлен в TPS-map `GameControls.inputactions`.
- **Frontend (UI + feel):** `StaminaBarLogic` (fill / цвет yellow→red ниже 0.35 / видимость; 20 тестов) + `HeroStaminaBar` (TPS HUD, показ при спринте или stamina < full). Sprint-widen FOV — расширил `DynamicFovLogic`/`DynamicFovController` круга 22: target FOV = base + (спринт? +4° : 0) + (kick); overloads backward-compatible (тесты круга 22 целы). `fovSprintWiden=4` в GameFeelSettings.
- **Forge:** `SetupDynamicFov` расширен (прошивает HeroController для sprint-FOV + пишет fovSprintWiden), новый `SetupStaminaBar`, общий `SetupCircle24`.

## Верификация
- Компиляция чистая, Forge без исключений (HeroController прошит в DynamicFov + stamina bar создан; fovSprintWiden=4 персистнут). **EditMode 519/519** (476 + 43 новых: SprintStamina 12 / StaminaBar 20 / DynamicFov +11). 0 регрессий (HeroMovementLogic, circle-22 FOV-overloads, SceneWiring — зелёные). Ревью поймало баг ТЕСТА (Color.yellow.b = 4/255 ≠ 0, НЕ продакшена) — исправлен оркестратором (b < yellow.b).
- PlayMode (спринт / стамина / FOV в сцене) — на CI.

## Бэклог
- Аудио (CC0-сорсинг); aim-spread (геймплей-риск power-fantasy); шеврон-спрайт стрелки урона (CC0); контент карт/юнитов.
