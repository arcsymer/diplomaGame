# 00 — Prerequisites (Фаза 0)

> Чек-лист всего, что нужно подготовить до старта M0. Собрано по результатам веб-ресёрча 2026-06-10. Источники — внизу каждого раздела. Решения зафиксированы в [[04 - Decision Log]].

> ⛔ **ЖЁСТКОЕ ОГРАНИЧЕНИЕ — НУЛЕВОЙ БЮДЖЕТ** (`zero-budget-constraint.md`, приоритет над всем): никаких покупок, подписок, платных API, платных ассетов, триалов и привязки карт. Только FOSS, локальная генерация и CC0/свободные лицензии. Чек-лист ниже составлен с учётом этого ограничения; см. [[04 - Decision Log#ADR-009]].

**Статус: ⏳ ожидает подтверждения пользователя («ОК, готово»)**
**Бюджет: $0 — ни один пункт не требует оплаты или карты.**

---

## A. Что ставит/заводит пользователь (требуется действие)

- [ ] **GitHub-аккаунт** + **gh CLI** (актуальная v2.93.0) — `gh auth login`
- [ ] **Git** + **Git LFS** — `git lfs install` (один раз на машину)
- [ ] **Unity Hub 3.15.x** + **Unity 6.3 LTS (6000.3.17f1** или свежее в ветке 6000.3**)**
  - Модули при установке: **Windows Build Support (IL2CPP)** + Microsoft Visual Studio Community 2022 (если ещё не стоит; нужны C++ Tools и Windows SDK 10.0.19041+)
  - Лицензия **Personal** — бесплатна (доход < $200k/год), активируется в Hub: Preferences → Licenses → «Get a free personal license»
- [ ] **Node.js** — уже стоит (нужен для Claude Code)
- [ ] **Obsidian** (desktop) — открыть хранилище `Docs-Vault/` этого репозитория
- [ ] (опц.) IDE: Rider / VS 2022 / VS Code с C# Dev Kit

### Почему Unity 6.3 LTS, а не 6.0 / 6.4
- 6.3 LTS — текущая рекомендация Unity для новых проектов, поддержка до **декабря 2027** (переживёт защиту диплома).
- 6.0 LTS — поддержка кончается уже в октябре 2026.
- 6.4 — «Supported»-релиз, умрёт с выходом 6.5; для проекта с дедлайном стабильность важнее новинок.

Источники: [Unity 6 support schedule](https://unity.com/releases/unity-6/support) · [Unity 6.3 LTS announcement](https://unity.com/blog/unity-6-3-lts-is-now-available) · [System requirements 6000.3](https://docs.unity3d.com/6000.3/Documentation/Manual/system-requirements.html) · [Unity Personal](https://unity.com/products/unity-personal)

---

## B. Аудио-пайплайн — полностью бесплатный (zero-budget)

### Главные выводы ресёрча (июнь 2026)
- Облачные сервисы (Suno, Udio, ElevenLabs, Stable Audio API, Azure TTS) **исключены ограничением нулевого бюджета** (подписки/триалы/карты). К счастью, ресёрч показал, что они и так проблемны: у Udio отключено скачивание (сделка с UMG), у Suno нет API и free-тир без скачивания (сделка с Warner).
- Бесплатная CC0/CC-BY база покрывает 100% потребностей проекта: Kenney (CC0, включая готовый **Voiceover Pack** с ack-репликами!), Sonniss GDC (royalty-free, 7.5 GB проф. SFX), incompetech, Pixabay, OpenGameArt.
- Локальная генерация (опционально): Stable Audio Open (открытые веса, Community License — свободно при выручке < $1M), Piper TTS (FOSS, CPU-only).

### Итоговый набор — всё $0, без аккаунтов и ключей
| Назначение | Инструмент | Лицензия | Примечание |
|---|---|---|---|
| Музыка (меню/эмбиент/бой) | incompetech/Kevin MacLeod + Pixabay Music + OpenGameArt (фильтр CC0/CC-BY) | CC-BY 4.0 (кредит в титрах) / Pixabay License / CC0 | основной путь; много военного/эпического |
| Музыка — локальная генерация (опц.) | **Stable Audio Open** (открытые веса, локально, нужна GPU) | Stability Community License (< $1M — свободно) | MusicGen — запасной (веса **CC-BY-NC** — пометить в Decision Log) |
| SFX (выстрелы, UI, взрывы, импакты) | **Kenney** Sci-fi Sounds / Interface / Impact (CC0) + **Sonniss GDC 2026** | CC0 / royalty-free без атрибуции | покрывает ~90% |
| SFX поиск точечный | **Freesound** (бесплатный API-токен, без карты) — фильтровать CC0/CC-BY | по-звуково CC0/CC-BY | избегать CC-BY-NC при наличии аналогов |
| SFX ретро/UI-генерация | sfxr / jsfxr / Bfxr / ChipTone (локально, без аккаунта) | свободные | пиу-пиу, клики, пикапы |
| Голоса юнитов (ack-реплики) | **Kenney Voiceover Pack + Voiceover Pack (Fighter)** (CC0, готовые реплики) | CC0 | путь №1 — ноль усилий |
| Голоса — генерация своих реплик | **Piper TTS** (локально, CPU, активный форк piper1-gpl) | код GPL; **лицензию конкретного голоса проверить** на HF rhasspy/piper-voices | путь №2; eSpeak NG — крайний случай (роботизирован) |

### Действие пользователя
- **Ничего.** Ни одного аккаунта, ключа или карты не требуется. (Опционально и только по желанию: бесплатный токен Freesound API — регистрация без карты.)

Источники: [Kenney Audio](https://kenney.nl/assets/category:Audio) · [Sonniss GDC](https://gdc.sonniss.com/) · [incompetech FAQ](https://incompetech.com/music/royalty-free/faq.html) · [Pixabay License](https://pixabay.com/service/license-summary/) · [OpenGameArt FAQ](https://opengameart.org/content/faq) · [Stable Audio Open / Community License](https://stability.ai/license) · [Piper (piper1-gpl)](https://github.com/OHF-Voice/piper1-gpl) · [Piper voices + лицензии](https://huggingface.co/rhasspy/piper-voices) · [Freesound API](https://freesound.org/docs/api/overview.html) · [Udio downloads disabled](https://routenote.com/blog/udio-stops-user-downloads-after-umg-deal-heres-why/)

---

## C. Арт / визуал

### Рекомендация: дефолт «всё бесплатно и CC0» — можно коммитить в открытый репозиторий
| Категория | Выбор | Лицензия |
|---|---|---|
| Юниты (мехи/танки) | **Quaternius** Animated Mech Pack + Animated Tanks Pack | CC0 |
| Солдаты/герой TPS | Quaternius Ultimate Space Kit / Sci-Fi Essentials | CC0 |
| Здания базы | Quaternius Ultimate Modular Sci-Fi Pack | CC0 |
| Оружие/пропсы | **Kenney** Blaster Kit + Space Kit | CC0 |
| UI-кит (панели, кнопки) | Kenney UI Pack — Sci-Fi (130 файлов) | CC0 |
| RTS-иконки (ресурсы, способности) | game-icons.net (4000+ SVG) | **CC-BY 3.0 — атрибуция в титрах** |
| Шрифты | **Russo One** (заголовки) + **Exo 2** (текст) — оба с кириллицей | SIL OFL 1.1 (файл лицензии в репо) |
| VFX-текстуры | Kenney Particle Pack (CC0) + Unity Particle Pack (с URP-конвертацией) | CC0 / Asset Store EULA |

⚠️ Orbitron (классика сай-фай) — **без кириллицы**, отброшен.

Дополнительно (всё бесплатно): **Poly Pizza** и **Sketchfab с фильтром CC0** — точечный добор моделей; **ProBuilder** (бесплатный пакет Unity) — прототип-геометрия уровня.

> Платные паки (Synty POLYGON и т.п.) исключены ограничением нулевого бюджета. CC0-набор Kenney + Quaternius полностью покрывает потребности проекта и, в отличие от Synty, легально живёт в открытом репозитории.

### Действие пользователя
- **Ничего** — весь арт бесплатный (CC0/OFL/CC-BY), скачаю и интегрирую сам.

Источники: [Kenney 3D](https://kenney.nl/assets/category:3D) · [Quaternius](https://quaternius.com/) · [Synty EULA](https://syntystore.com/pages/one-time-purchase-licence) · [game-icons.net license](https://game-icons.net/about.html) · [Russo One](https://fonts.google.com/specimen/Russo+One) · [Exo 2](https://fonts.google.com/specimen/Exo+2) · [Unity Particle Pack + URP](https://forum.unity.com/threads/fixing-the-parts-of-the-particle-pack-that-dont-work-under-urp.839212/)

---

## D. Unity-пакеты (ставлю сам через manifest — подтверждённый список)

Версии сверены с официальным списком released-пакетов Unity 6.3 (6000.3):

| Пакет | Версия | Примечание |
|---|---|---|
| Input System (com.unity.inputsystem) | **1.19.x** | Project-Wide Input Actions; раздельные Action Maps RTS/TPS |
| AI Navigation (com.unity.ai.navigation) | **2.0.x** | NavMeshSurface/Agent для юнитов |
| URP | **17.3.x** (core, привязан к редактору) | + Volume для пост-обработки |
| Cinemachine | **3.1.x** | ⚠️ API 3.x ≠ 2.x: `Unity.Cinemachine`, единый `CinemachineCamera`; старые туториалы 2.x не применять |
| Test Framework | **1.6.x** | EditMode/PlayMode |
| TextMeshPro | — | **отдельного пакета больше нет** — слит в com.unity.ugui 2.0 |
| Твининг | **PrimeTween** (MIT, zero-alloc) | вместо DOTween: open-source, без лицензионных вопросов в дипломе |
| ~~Behavior (com.unity.behavior)~~ | не берём | с мая 2026 maintenance-only; ИИ юнитов — своя FSM/utility (плюс для диплома: своя архитектура) |

UI-решение: **uGUI (Canvas + TMP) для игрового HUD, health bars, миникарты** (RawImage + RenderTexture) — проверенные рецепты, твинится, дружит с ассетами. UI Toolkit world-space слишком свежий — лишний риск. Обоснование в [[04 - Decision Log]].

Источники: [Released packages 6000.3](https://docs.unity3d.com/6000.3/Documentation/Manual/pack-safe.html) · [Cinemachine 3.x upgrade guide](https://docs.unity3d.com/Packages/com.unity.cinemachine@3.1/manual/CinemachineUpgradeFrom2.html) · [Behavior maintenance](https://discussions.unity.com/t/update-on-behavior-package-support-and-team-presence/1718517) · [PrimeTween](https://github.com/KyryloKuzyk/PrimeTween) · [UI systems comparison](https://docs.unity3d.com/Manual/UI-system-compare.html)

---

## E. GitHub / VCS (настраиваю сам — план на подтверждение)

- **LFS с первого коммита, но выборочно**: крупные бинарники (fbx, blend, psd, длинные wav, большие текстуры, terrain/lighting data) — в LFS; мелкие png/ogg (< ~5 МБ) — обычный git. Причина: бесплатный тир GitHub LFS = **10 GiB хранилища + 10 GiB трафика/мес**; data packs отменены (теперь metered billing $0.07/GiB·мес); без привязанной карты при превышении LFS просто блокируется до конца месяца. Решение нужно ДО первого пуша (миграция задним числом = переписывание истории).
- `.gitignore` Unity (Library/, Temp/, Obj/, Build/, Logs/, UserSettings/) + `.gitattributes` (LFS-паттерны + `text eol=lf` для Unity-YAML + unityyamlmerge).
- Ветка `main` стабильная, conventional commits, теги по майлстоунам (semver).
- **Релиз игры — через GitHub Releases** (`gh release create vX.Y.Z build.zip`): лимит 2 GiB/файл, трафик скачивания бесплатный и безлимитный — идеально для билда.
- **CI (GitHub Actions + GameCI)**: проект GameCI жив (`unity-builder@v4`, `unity-test-runner@v4`), Actions для публичных репо **бесплатны безлимитно**. Но: нужен секрет `UNITY_LICENSE` (содержимое `C:\ProgramData\Unity\Unity_lic.ulf` — **действие пользователя**), холодные сборки медленные, LFS-трафик расходуется. **Рекомендация: CI — опционально и минимально** (тесты + сборка по тегу); основной путь — локальная сборка через Project Forge.

### Действие пользователя
- [ ] Решить по CI: (а) без CI — только локальные сборки (рекомендую для старта, добавить можно позже), (б) минимальный CI — тогда после M0 передать содержимое `Unity_lic.ulf` в секрет репо.

Источники: [LFS billing](https://docs.github.com/billing/managing-billing-for-git-large-file-storage/about-billing-for-git-large-file-storage) · [Metered billing FAQ](https://github.com/orgs/community/discussions/61362) · [GameCI activation](https://game.ci/docs/github/activation/) · [Actions billing](https://docs.github.com/billing/managing-billing-for-github-actions/about-billing-for-github-actions) · [Release limits](https://docs.github.com/en/repositories/releasing-projects-on-github/about-releases) · [Unity .gitattributes (Takken)](https://gist.github.com/webbertakken/ff250a0d5e59a8aae961c2e509c07fbc)

---

## Итоговый минимум действий пользователя

1. **Установить** (всё бесплатно, без карт): Git + Git LFS, gh CLI (+ `gh auth login`), Unity Hub + **Unity 6.3 LTS** с модулем Windows IL2CPP (+ VS 2022 Community), Obsidian.
2. **Активировать** лицензию Unity Personal в Hub (бесплатно, без карты).
3. **Ответить на 2 развилки**: CI — нужен или нет (рекомендую старт без CI)? Название игры/репозитория?
4. Сказать **«ОК, готово»** → стартую M0.

**Суммарный бюджет: $0.** Ни ключей, ни подписок, ни карт не требуется.
