# Catsss — текущее состояние разработки

**Живой документ** для отслеживания того, что уже в репозитории, что воспроизводимо в редакторе, и что из официальных этапов (`Stage 1–7`) ещё не закрыто.

| Поле | Значение |
|------|----------|
| **Текущий этап** | **Stage 3** — испытания + штрафы + UX (локализация, world hints, guest connect) |
| **Цель альфы** | Vertical slice одного уровня, 2 игрока по сети |
| **Точка входа** | Сцена `MainMenu` → `Sandbox` |
| **Последнее обновление** | Май 2026 — фаза B: `TrialBoundsZone`, командный штраф, телепорт |

---

## Сводка: что работает сейчас

| Область | Статус | Документ |
|---------|--------|----------|
| Архитектурное ядро (FSM, Events, ServiceLocator, Timing) | ✅ Stage 1 | `Architecture-Snapshot.md`, `Stage 1.md` |
| NGO + UTP (IP/порт, без Relay) | ✅ Упрощённо | `MainMenu-And-Networking.md`, `Stage 2.md` |
| Движение игрока (ходьба, прыжок, dash, камера) | ✅ MVP | `Development-Status.md` (ниже) |
| Испытания: пилон → заряд → финиш → перманент | ✅ | `Stage 3.md` |
| Штрафы испытания (bounds, таймер, командный телепорт) | ✅ | `Stage 3.md` |
| Взаимодействие E + URP-контур | ✅ MVP | `Stage 3.md` |
| Локализация RU/EN (String Table) | ✅ MainMenu + hints | `Localization-And-WorldHints.md` |
| World-space подсказки (billboard, приоритеты) | ✅ MVP | `Localization-And-WorldHints.md` |
| Guest connect с валидацией и возвратом в меню | ✅ | `MainMenu-And-Networking.md` |
| Заряд-снаряд, level kit, мышь, сюжетные пилоны | ❌ Stages 4–7 | `Stage 4.md` … `Stage 7.md` |

---

## Главное меню и вход в сессию

- Сцена **`MainMenu`**: **Хост** / **Гость** (IP + порт) / **Выход** — `MainMenuController`.
- Локализованные тексты кнопок, плейсхолдеров, ошибок — `MainMenuLocalizedText` + String Table **`UI_Strings`**.
- Перед загрузкой Sandbox — **`MenuLoadingOverlay`** (`DontDestroyOnLoad`).
- Параметры сессии — статический **`NetworkSessionIntent`** (хост/клиент, порт, задержка оверлея).
- На **`Sandbox`**: **`GameplayNetworkSessionStarter`** читает intent и вызывает `ConnectionManager.StartHost()` / `StartClient()`.
- Dev-пути без меню: fallback-хост в Sandbox, CLI **`-join` / `-client`** (`DevelopmentJoinArgs`).
- **`DevNetworkBootstrap`** не дублирует старт, если на сцене уже есть `GameplayNetworkSessionStarter`.
- Транспорт: **Unity Transport (UTP)**, прямое **IP + порт**. Хост слушает **`0.0.0.0`** по умолчанию. **Unity Relay / join-код отложены** — для разных сетей: VPN (Hamachi) или проброс порта.

### Защита guest connect (новое)

Трёхслойная валидация **до** вызова UTP:

1. **`MainMenuController`** — проверка IP/порта на GuestPanel; ошибка показывается **без загрузки Sandbox**.
2. **`GameplayNetworkSessionStarter`** — повторная проверка intent; при ошибке — возврат в MainMenu через **`MenuConnectionFeedback`**.
3. **`ConnectionManager.StartClient()`** — последний барьер; при невалидном адресе только `LogWarning`, без transport failure.

Ключевые типы: **`ClientConnectInputValidator`**, **`ClientConnectInputError`**, **`MenuConnectionFeedback`**.

Подробности: **`MainMenu-And-Networking.md`**.

---

## Локализация и world hints (расширение Stage 3)

- **Unity Localization Package**: таблица `UI_Strings`, локали **English** и **Russian (ru)**.
- **`LocalizedTextReference`** — ссылка на строку + editor fallback; используется в UI и подсказках.
- **`LocalizedUiText`** — статический текст на Canvas (кнопки, заголовки).
- **`MainMenuLocalizedText`** — динамика: LAN IP-хинт для хоста, ошибки GuestPanel.
- **World hints**: одна подсказка на игрока, billboard к камере, приоритет interaction (10) vs trigger (100+).
- Editor bootstrap: **Catsss → Localization → …**, **Catsss → World Hints → …**.

Подробности: **`Localization-And-WorldHints.md`**.

---

## Движение игрока (проверенный MVP)

Сценарий: владелец `PlayerRoot` после входа в сессию:

- WASD относительно камеры, прыжок (coyote + input buffer), кастомная гравитация из `GameConfig`.
- Dash (Shift) с кулдауном; один воздушный dash до приземления.
- После dash при удержании Shift — post-dash sprint multiplier.
- Мультиплеер локально: MainMenu → Host + второй процесс Client (`127.0.0.1` или LAN IP).
- Спавн со смещением по stride для нескольких игроков; ввод только у `IsOwner`.

## Камера

- `PlayerCameraController` + Cinemachine 3, орбита через Look.
- Зона полировки: коллизии камеры, damping, Script Execution Order.

## Конфигурация

- **`Assets/Configs/GameConfig.asset`** — движение, физика.
- **`Configs/Trials/`**, **`Configs/AllBaffs/`**, **`Configs/WorldHints/`** — контент испытаний и подсказок.

---

## Этап 3 — испытания (кратко)

Подробно: **`Stage 3.md`**.

**Цикл:** E у пилона → заряд инициатору → финиш → перманент **всем** → пилон depleted.  
**Штраф:** выход из `TrialBoundsZone` или таймер заряда → сброс trial, телепорт **всех** на `PenaltyRespawnPoint`, повтор без кулдауна.

| Область | Ключевые типы |
|--------|----------------|
| Данные | `TrialDefinition`, `ChargeTypeDefinition`, `PermanentModifierDefinition`, каталоги |
| Игрок | `PlayerChargeController`, `PlayerPermanentModifiers`, `PlayerInteractionController`, `PlayerTrialInteractor`, `NetworkPlayerController.TeleportTo` |
| Мир | `TrialPylonStart`, `TrialFinishZone`, `TrialBoundsZone`, `TrialSessionRegistry`, `TrialPenaltyReason` |
| UX | `InteractableOutlineRendererFeature`, `WorldTextHintPresenter`, `InteractionPromptView` |

**Stage 3 закрыт для альфы** (gameplay loop). **Не сделано (фаза C / другие этапы):** `ChargedState` в FSM, UI таймера заряда, сетевой VFX, `ChargeSource` (Stage 4), антимаг (Stage 5), сюжетные пилоны Stage 7.

---

## Сцены и префабы

| Ассет | Назначение |
|-------|------------|
| `Scenes/MainMenu.unity` | Точка входа билда, сеть UI |
| `Scenes/Sandbox.unity` | Песочница: NGO, trials, hints |
| `Prefabs/PlayerRoot.prefab` | Сетевой игрок + hints presenter |
| `Prefabs/PylonStartRoot.prefab` | Старт испытания |
| `Prefabs/PylonEndRoot.prefab` | Финиш испытания |
| `Prefabs/WorldTextHintTriggerRoot.prefab` | Триггер world hint |

---

## Технический долг и риски

| Тема | Комментарий |
|------|-------------|
| Relay / UGS | Не интегрирован; расхождение с текстом `Stage 2.1` — осознанное |
| Визуал игрока | `NetworkPlayerEventsRelay` — RPC-скелет без Animator |
| Event Bus в меню | Пока прямые вызовы; каналы на успешный connect — опционально |
| Порт | Держать одинаковым: MainMenu host port = `ConnectionManager.port` на Sandbox |
| `NetworkRigidbody` | Учитывать при будущих физических добавках |
| Локализация | После добавления ключей — прогон editor setup (см. гайд) |

---

## Карта документации `_Docs/`

| Файл | Назначение |
|------|------------|
| `Onboarding.md` | Вход в проект, 6 столпов |
| `Architecture-Snapshot.md` | Структура кода и договоры |
| `Development-Status.md` | **Этот файл** — текущий статус |
| `DevLog.md` | Хронология недавних изменений |
| `MainMenu-And-Networking.md` | Меню, NGO, guest validation |
| `Localization-And-WorldHints.md` | RU/EN, подсказки в мире |
| `Stage 1.md` … `Stage 7.md` | Задачи по этапам |
| `GDD.md`, `ConceptDocument.md` | Дизайн и концепт |

---

*Следующий логический шаг: **Stage 4** (метание заряда, `ChargeProjectile`) или полировка фазы C Stage 3 (UI таймера, VFX заряда).*
