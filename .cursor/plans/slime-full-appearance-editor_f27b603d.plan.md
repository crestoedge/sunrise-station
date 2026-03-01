---
name: slime-full-appearance-editor
overview: Расширить текущий UI изменения внешности слаймов, чтобы во время раунда можно было редактировать почти всю вкладку внешности HumanoidProfileEditor, при этом не давая доступ к работам, лодаутам и остальному профилю.
todos:
  - id: design-fields
    content: Зафиксировать список полей внешности, которые можно менять in-round у слаймов, и то, что остаётся запрещённым (species, jobs, traits и т.д.).
    status: pending
  - id: extend-shared-protocol
    content: Расширить Shared DynamicAppearance протокол (state + messages) под полный срез внешности, совместимый с HumanoidAppearanceComponent.
    status: pending
  - id: implement-client-window
    content: Реализовать клиентское окно SlimeAppearanceEditorWindow на основе appearance-вкладки HumanoidProfileEditor, без вкладок jobs/antags/traits и без сохранения профиля.
    status: pending
  - id: wire-bui-to-window
    content: Подключить SlimeAppearanceEditorWindow к DynamicAppearanceBoundUserInterface и обеспечить обмен расширенным UI state и сообщениями.
    status: pending
  - id: implement-server-handlers
    content: Добавить обработку нового full-appearance сообщения в DynamicAppearanceSystem с валидацией для SlimePerson и обновлением HumanoidAppearanceComponent.
    status: pending
  - id: deduplicate-logic
    content: По возможности вынести общие helper-методы из HumanoidProfileEditor, чтобы их использовали оба редактора внешности.
    status: pending
  - id: add-guards-and-tests
    content: Добавить ограничения доступа (только владелец, только SlimePerson), затем протестировать изменения на тестовом сервере и убедиться, что они не затрагивают глобальные профили и профессии.
    status: pending
isProject: false
---

## Цели

- **Сделать отдельный, урезанный in‑round редактор внешности для слаймов**, который работает поверх текущей `DynamicAppearance` BUI.
- **Разрешать менять только внешность текущего моба** (Sex, Gender, Age, BodyType, рост/ширину, цвета кожи/глаз, волосы/markings и базовые слои), **не трогая профессии, слоты, антагов, трейт‑систему и species/profiles**.

## Общая архитектура

```mermaid
flowchart TD
    clientPlayer[ClientPlayer]
    verb["AlternativeVerb: slime-appearance"]
    serverSystem[DynamicAppearanceSystem]
    uiState[DynamicAppearanceUIState (расширено)]
    bui[DynamicAppearanceBoundUserInterface]
    slimeWindow[SlimeAppearanceEditorWindow]
    msgs[DynamicAppearance* UI messages]
    humanoidComp[HumanoidAppearanceComponent]

    clientPlayer -->|использует верб| verb --> serverSystem
    serverSystem -->|OpenUi + SetUiState| bui
    serverSystem <-->|UIState/messages| bui
    bui --> slimeWindow
    slimeWindow -->|правки внешности| msgs --> serverSystem
    serverSystem -->|валидирует + применяет| humanoidComp
    serverSystem -->|Dirty + SetUiState| bui
```



## План по шагам

- **1. Определить точный набор изменяемых полей**
  - Базироваться на вкладке "Appearance" из `[Content.Client/Lobby/UI/HumanoidProfileEditor.xaml.cs](Content.Client/Lobby/UI/HumanoidProfileEditor.xaml.cs)`.
  - Включить: `Name`, `Sex`, `Gender`, `Age`, `BodyType`, рост/ширину, `SkinColor`, `EyeColor`, hair/facial hair (стиль, цвет + эффекты), произвольные markings и `CustomBaseLayers`.
  - Исключить: выбор `Species`, spawn‑priority, TTS/Voice, flavor text, jobs, antags, traits, loadouts и любые настройки, которые влияют на профиль за пределами текущего раунда.
  - Зафиксировать, что меняется **только** `HumanoidAppearanceComponent` живого слайма (`Species == "SlimePerson"`).
- **2. Расширить общий протокол DynamicAppearance (shared)**
  - В `[Content.Shared/_Sunrise/DynamicAppearance/SharedDynamicAppearanceSystem.cs](Content.Shared/_Sunrise/DynamicAppearance/SharedDynamicAppearanceSystem.cs)`:
    - Либо **расширить** существующий `DynamicAppearanceUIState`, добавив необходимые поля (например, возраст, имя, пол, gender, eye color, height/width и т.д.),
    - Либо ввести **новый state‑класс** `DynamicAppearanceFullUIState`, который наследуется от `BoundUserInterfaceState` и содержит полный срез внешности (можно близко к `HumanoidCharacterAppearance`).
  - Добавить 1–2 сообщения от клиента к серверу:
    - Например, `DynamicAppearanceUIFullAppearanceSetMessage(HumanoidCharacterAppearance appearance, bool resendState)` — передаёт все изменённые поля разом.
    - (Опционально) оставить текущие сообщения для markings/base layers как есть, чтобы не ломать уже существующий функционал и окно `HumanoidMarkingModifierWindow`.
- **3. Новый клиентский UI‑класс под слаймов**
  - Создать новое окно, например `SlimeAppearanceEditorWindow` в клиентском sunrise‑неймспейсе, рядом с `DynamicAppearanceBoundUserInterface` — файл вида `[Content.Client/_Sunrise/DynamicAppearance/SlimeAppearanceEditorWindow.xaml(.cs)](Content.Client/_Sunrise/DynamicAppearance/SlimeAppearanceEditorWindow.xaml.cs)`.
  - Вынести из `HumanoidProfileEditor` (appearance‑часть) нужные контролы и логику в этот новый класс:
    - Контролы для `Name`, `Sex`, `Gender`, `Age`, `BodyType`, height/width с отображением рост/вес, skin‑/eye‑color, hair/facial hair, markings, предпросмотр dummy и вращение спрайта.
    - Создать локальное поле текущей редактируемой внешности (например, `HumanoidCharacterAppearance` или эквивалент), которое будет инициализироваться из `DynamicAppearanceUIState` при открытии.
  - Адаптировать работу с `MarkingManager`, `SpeciesPrototype`, `SkinColorationStrategy`, `HumanoidAppearanceSystem` по аналогии с тем, как это сделано в `HumanoidProfileEditor`, но без доступа к профилям, слоту и jobs.
- **4. Интеграция нового окна в BUI**
  - В `[Content.Client/_Sunrise/DynamicAppearance/DynamicAppearanceBoundUserInterface.cs](Content.Client/_Sunrise/DynamicAppearance/DynamicAppearanceBoundUserInterface.cs)`:
    - Вместо создания `HumanoidMarkingModifierWindow` создавать `SlimeAppearanceEditorWindow` (либо выбирать окно по какому‑то флагу, если нужно сохранить старый вариант для других видов).
    - Пробросить делегаты/ивенты окна:
      - При любых изменениях внешности окно формирует либо:
        - полный `HumanoidCharacterAppearance` и отправляет `DynamicAppearanceUIFullAppearanceSetMessage`, либо
        - цельный набор более мелких сообщений (но лучше один агрегированный пакет, чтобы минимизировать сетевой шум).
    - В `UpdateState` принимать расширенный `DynamicAppearanceUIState`/`DynamicAppearanceFullUIState` и обновлять состояние окна (в т.ч. предпросмотр dummy).
- **5. Серверная логика применения правок**
  - В `[Content.Server/_Sunrise/SlimeAppearance/DynamicAppearanceSystem.cs](Content.Server/_Sunrise/SlimeAppearance/DynamicAppearanceSystem.cs)`:
    - Подписаться на новое сообщение `DynamicAppearanceUIFullAppearanceSetMessage` аналогично уже существующим `DynamicAppearanceUIMarkingSetMessage` и `DynamicAppearanceUIBaseLayersSetMessage`.
    - В обработчике:
      - Проверить наличие `HumanoidAppearanceComponent` и что `Species == "SlimePerson"`.
      - Применить поля из сообщения к компоненту: имя, возраст, sex/gender, body type, height/width, skin/eye colors, hair/facial hair, markings, custom base layers.
      - Использовать `_prototypeManager` и правила из `HumanoidAppearanceSystem`/`SpeciesPrototype` и `MarkingPrototype` для валидации (фильтрация недопустимых markings, ограничение размеров по min/max, невозможность смены species и т.п.).
      - Вызвать `Dirty(uid, humanoidAppearance)`.
      - При необходимости (если флаг `ResendState == true`) переслать на клиент актуальное состояние через `SetUiState`, чтобы окно синхронизировалось.
- **6. Повторное использование логики HumanoidProfileEditor**
  - Чтобы не дублировать сложные куски (skin coloration, hair/markings построение, size‑контролы и т.п.), выделить из `[HumanoidProfileEditor.xaml.cs](Content.Client/Lobby/UI/HumanoidProfileEditor.xaml.cs)` небольшие helper‑методы/классы, которые можно переиспользовать и в `SlimeAppearanceEditorWindow`.
  - Вынести общее в:
    - либо отдельный утилитарный класс/partial‑файл под humanoid‑appearance UI,
    - либо общий базовый класс для редакторов внешности (pre‑round и in‑round), от которого будут наследоваться `HumanoidProfileEditor` и `SlimeAppearanceEditorWindow`.
- **7. Ограничения и защита от злоупотреблений**
  - Убедиться, что verb открытия окна (`OnVerbsRequest` в `DynamicAppearanceSystem`) доступен только самому владельцу сущности и, при необходимости, только в определённых состояниях (например, живой слайм, не в критическом состоянии и т.п.).
  - Гарантировать, что изменения затрагивают только in‑round сущность, а не глобальные `HumanoidCharacterProfile`/preferences:
    - Не трогать `IClientPreferencesManager` и слоты.
    - Не вызывать код сохранения профиля; вся логика ограничена `HumanoidAppearanceComponent` текущего EntityUid.
  - При желании добавить cvar/настройку, позволяющую вообще отключить этот verb или ограничить по ролям/whitelist.
- **8. Тестирование**
  - На test‑сервере:
    - Создать слайма, открыть новый редактор, проверить изменение каждого поля (sex, gender, возраст, размеры, волосы/markings, цвет кожи/глаз).
    - Убедиться, что:
      - species не меняется;
      - профессии, приоритеты работ, антаги, traits и loadouts не отображаются и не меняются;
      - после закрытия/открытия окна состояние синхронизируется с сервером;
      - правки не переживают round‑restart (нет утечки в persistent‑профиль).
  - Прогнать базовые linters/тесты проекта, убедиться в отсутствии регрессий.

