# Локализация и world hints

RU/EN через Unity Localization Package + единая система текстовых подсказок в world space.

См. также: `Stage 3.md`, `Stage 4.md`, `Stage 5.md`, `Development-Status.md`, `Architecture-Snapshot.md`.

---

## Зачем

| Система | Проблема без неё | Решение |
|---------|------------------|---------|
| **Localization** | Хардкод строк в Canvas и коде | String Table `UI_Strings`, смена языка без правки сцен |
| **World hints** | Отдельный UI на каждом триггере, конфликты текстов | Один `WorldTextHintPresenter` на игроке, подсказка «переезжает» между якорями |

Обе системы используют общий тип **`LocalizedTextReference`**.

---

## Локализация

### Структура ассетов

```
Assets/Localization/
├── Localization Settings.asset
├── Locales/
│   ├── English.asset
│   └── Russian_(ru).asset
└── StringTables/
    └── UI_Strings (+ _en, _ru, Shared Data)
```

### Ключевые скрипты

| Скрипт | Назначение |
|--------|------------|
| `LocalizedTextReference` | SO-поле: LocalizedString + editor fallback, Bind/Unbind |
| `LocalizedUiText` | Статический текст на Canvas (TMP / Legacy Text) |
| `MainMenuLocalizedText` | Динамика меню: IP-хинт, ошибки guest panel |

### Editor bootstrap

1. **Catsss → Localization → Setup UI Strings (RU + EN)**  
   Создаёт/обновляет ключи в `UI_Strings`.

2. **Catsss → Localization → Setup MainMenu Scene Texts**  
   Вешает `LocalizedUiText` на кнопки/плейсхолдеры, связывает `MainMenuLocalizedText`.

### Основные ключи меню

| Ключ | EN (пример) |
|------|-------------|
| `menu.title` | Catsss |
| `menu.button.start` | Start game |
| `menu.button.connect` | Connect |
| `menu.guest.ip_hint` | Host IP for guest (LAN / Hamachi): {0} |
| `menu.error.connection_failed` | Could not connect to the host… |
| `menu.error.empty_host` | Enter the host IP or hostname. |
| `menu.error.invalid_address` | Invalid IP or hostname… |
| `menu.error.invalid_port` | Invalid port… |

### Ключи подсказок

| Ключ | Назначение |
|------|------------|
| `hint.interact.press_e` | Промпт взаимодействия |
| `hint.trial.press_shift_dash` | Подсказка после unlock Dash |

### Как добавить новую строку

1. Добавить tuple в `LocalizationSetupMenu.DefaultEntries` (EN + RU).
2. Запустить **Setup UI Strings**.
3. В инспекторе назначить ключ через `LocalizedTextReference` или прогнать scene setup.

### Как использовать в коде

```csharp
// Одноразово
string text = myLocalizedReference.ResolveDisplayText();

// С авто-обновлением при смене локали
myLocalizedReference.Bind(value => tmpText.text = value);
// ...
myLocalizedReference.Unbind();
```

---

## World hints

### Идея

- **Один** world-space TMP на **PlayerRoot** (owner-only).
- Источники (interaction E, триггеры в мире) регистрируют «слот» с якорем, текстом и **приоритетом**.
- Presenter выбирает winner и двигает view к якорю; view смотрит на камеру игрока (billboard).

### Приоритеты по умолчанию

| Источник | Priority |
|----------|----------|
| Interaction (E) | 10 (`WorldTextHintPresenter.InteractionPriority`) |
| Trigger hints | 100+ (в Definition) |

Interaction проигрывает trigger с меньшим priority — игрок видит «Нажмите E» у пилона, а не фоновую подсказку.

### Ключевые типы

| Тип | Роль |
|-----|------|
| `WorldTextHintDefinition` | SO-профиль: текст, offset, priority, show/hide rules |
| `WorldTextHintTrigger` | На объекте в мире; слушает правила show/hide |
| `WorldTextHintPresenter` | На PlayerRoot; owner-only; merge слотов |
| `WorldTextHintView` | Billboard TMP, offset от якоря |
| `InteractionPromptView` | Наследник View для промпта E (совместимость prefab) |

### Show kinds (когда показать)

| Kind | Описание |
|------|----------|
| Manual | Только через API |
| OnEnable | При включении триггера |
| TrialProgress | SO event `TrialProgressEventChannel` (server-side phases) |
| EmptyEvent | Любой `EmptyEventChannel` |
| PlayerCanDashUnlocked | Когда `PlayerPermanentModifiers.CanDash` реплицировался true (client-side) |

### Hide kinds (когда скрыть)

| Kind | Описание |
|------|----------|
| InputAction | Dash / Interact / Jump (Peek, не Consume) |
| Timeout | Через N секунд |
| TrialProgress | Фаза из SO event |
| EmptyEvent | Внешний сигнал |
| OnDisable | При выключении триггера |

### Пример: подсказка «Нажмите Shift» после trial

**Ассет:** `Configs/WorldHints/Hint_PressShiftAfterTrial.asset`

- Text: `hint.trial.press_shift_dash`
- Show: `PlayerCanDashUnlocked`
- Hide: `InputAction` → Dash

**Prefab:** `WorldTextHintTriggerRoot` — разместить на Sandbox (например под TrialSystems).

**Editor:** **Catsss → World Hints → Create Dash Hint Definition** (если ассета ещё нет).

---

## Настройка в Unity

### PlayerRoot

На префабе должны быть:

- `WorldTextHintPresenter` — ссылки на `WorldTextHintView`, `PlayerCameraController`, head anchor.
- `PlayerInteractionController` — вызывает `SetInteractionFocus` / `ClearInteractionFocus` на presenter.
- Дочерний Canvas **World Space** с `WorldTextHintView` или `InteractionPromptView`.

### Новый trigger в мире

1. Duplicate `WorldTextHintTriggerRoot` или Create Empty + компоненты.
2. Create → **Catsss/World Hints/Hint Definition** — настроить текст и rules.
3. На trigger: assign Definition, optional custom anchor Transform.
4. Добавить ключ локализации если новый текст.

### Billboard и камера

View использует **`PlayerCameraController.UnityCamera`**, не `Camera.main` — важно для split/co-op когда main camera не та.

Rotation: `LookRotation(viewPos - cameraPos)` — текст лицом к игроку.

---

## Связь с Interaction (E)

```
PlayerInteractionController (owner)
    → focus IInteractable
    → WorldTextHintPresenter.SetInteractionFocus(anchor, InteractionPromptSettings)
    → priority 10

WorldTextHintTrigger
    → WorldTextHintPresenter (через trigger slot)
    → priority из Definition (обычно 100)
```

`InteractionPromptSettings` содержит `LocalizedTextReference` для текста «Нажмите E».

---

## Отладка

| Симптом | Проверка |
|---------|----------|
| Текст не меняется при смене локали | Bind/Unbind на `LocalizedTextReference` |
| Всегда один и тот же fallback | Прогнать Setup UI Strings; проверить KeyId в сцене |
| Подсказка не видна | Presenter только на owner; view active? |
| Подсказка смотрит не туда | `PlayerCameraController` назначен в Presenter |
| NullReference в Presenter | `HintSlot` — struct (исправлено); проверить view ref |

---

## Архитектурные правила

- **Логика не тянет TMP напрямую** — trials/charges шлют events или реплицируют state; triggers слушают SO или NetworkVariables.
- **Owner-only UI** — чужие игроки не видят ваши подсказки ввода.
- **Один visible hint** — не плодить Canvas на каждом пилоне.

---

## Добавление языка (будущее)

1. Create Locale в `Assets/Localization/Locales/`.
2. Добавить колонку в `UI_Strings` через Localization Tables window.
3. Upsert переводы в `LocalizationSetupMenu` или вручную в таблице.
4. Unity Localization Settings → добавить locale в Available Locales.
