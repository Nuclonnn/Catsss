# Catsss — журнал разработки (DevLog)

Краткая хронология значимых изменений. Для текущего статуса систем смотри **`Development-Status.md`**.

---

## Май 2026 — Stage 3 фаза B: штрафы испытания

**Цель:** закрыть gameplay loop испытаний по `GDD.md` — зона, командный сброс, телепорт на чекпоинт.

**Реализовано:**

- `TrialBoundsZone` — server-only trigger; выход игрока из зоны при активном trial → штраф.
- `TrialSessionRegistry.ApplyTeamTrialPenalty` — снятие заряда у **всех** клиентов, телепорт на `PenaltyRespawnPoint`, сброс `_isTrialActive`, событие `Cancelled`, лог с `TrialPenaltyReason`.
- `TrialPenaltyReason`: `LeftTrialBounds`, `ChargeTimerExpired`.
- `PlayerChargeController` — истечение таймера → командный штраф (не «тихий» clear).
- `NetworkPlayerController.TeleportTo` + `TeleportFromServerClientRpc` (client-auth, owner-only).

**Решения:** штраф командный; падение = выход из bounds; без kill plane `Y < -10`; без кулдауна на повтор; `ChargeSource` — Stage 4.

**Файлы:** `Scripts/Trials/TrialBoundsZone.cs`, `TrialPenaltyReason.cs`, `TrialSessionRegistry.cs`, `Scripts/Charges/PlayerChargeController.cs`, `Scripts/Player/NetworkPlayerController.cs`.

**Документы:** обновлены `Stage 3.md`, `Development-Status.md`, `Architecture-Snapshot.md`.

---

## Май 2026 — Guest connect: защита от неверного ввода

**Проблема:** IP вроде `111` вызывал ошибки Unity Transport, misleading лог «порт занят», shutdown NGO.

**Решение:**

- `ClientConnectInputValidator` — проверка IPv4 (4 октета), `localhost`, hostname (с буквами; чисто числовые строки отклоняются), порт 1–65535.
- Валидация в `MainMenuController` **до** загрузки Sandbox.
- Повтор в `GameplayNetworkSessionStarter` и `ConnectionManager.StartClient()`.
- `MenuConnectionFeedback` — возврат в MainMenu с открытой GuestPanel и типом ошибки.
- Локализованные тексты: `menu.error.empty_host`, `invalid_address`, `invalid_port`, `connection_failed`.
- Исправлен баг в `MainMenuLocalizedText.BindActiveGuestError()` — текст ошибки не обновлялся (всегда «Connection error»).

**Файлы:** `Scripts/Network/ClientConnectInputValidator.cs`, `Scripts/Menu/MainMenuController.cs`, `MainMenuLocalizedText.cs`, `MenuConnectionFeedback.cs`, `Scripts/Network/ConnectionManager.cs`, `GameplayNetworkSessionStarter.cs`, editor setup локализации.

---

## Май 2026 — Локализация MainMenu (RU + EN)

- Unity Localization Package, String Table **`UI_Strings`**.
- `LocalizedTextReference`, `LocalizedUiText`, `MainMenuLocalizedText`.
- Editor: **Catsss → Localization → Setup UI Strings (RU + EN)** и **Setup MainMenu Scene Texts**.
- LAN IP-хинт для хоста через `NetworkLocalAddressHints` + `menu.guest.ip_hint`.

---

## Май 2026 — World-space подсказки (World Hints)

**Зачем:** единая система текстовых подсказок в мире (billboard к камере), вместо разрозненных UI на каждом объекте.

**Реализовано:**

- `WorldTextHintDefinition` (SO) — текст, якорь, приоритет, правила show/hide.
- `WorldTextHintTrigger` — показ/скрытие по правилам (OnEnable, TrialProgress, PlayerCanDashUnlocked, InputAction, Timeout и др.).
- `WorldTextHintPresenter` на **PlayerRoot** (owner-only) — одна подсказка, winner по приоритету.
- `WorldTextHintView` — billboard через `PlayerCameraController.UnityCamera`.
- `InteractionPromptView` — thin wrapper для совместимости с промптом E.
- Пример: `Configs/WorldHints/Hint_PressShiftAfterTrial.asset`, prefab `WorldTextHintTriggerRoot`.

**Исправления:** `HintSlot` переведён в struct (был class → NullReference в `ChooseWinner`).

---

## Ранее (до DevLog) — Stage 3 MVP

- Испытания: `TrialPylonStart`, `TrialFinishZone`, `TrialSessionRegistry`.
- Заряды и перманенты: `PlayerChargeController`, `PlayerPermanentModifiers`.
- URP контур: `InteractableOutlineRendererFeature`, `InteractableHighlightStub`.
- MainMenu → Sandbox flow: `NetworkSessionIntent`, `GameplayNetworkSessionStarter`, `MenuLoadingOverlay`.

---

## Как вести этот файл

После каждого завершённого спринта / этапа добавлять секцию:

1. **Дата + тема**
2. **Проблема / цель** (если была)
3. **Что сделано** (списком)
4. **Ключевые файлы**
5. Ссылка на обновлённый `Development-Status.md` при смене статуса этапа
