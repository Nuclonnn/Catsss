# Catsss — журнал разработки (DevLog)

Краткая хронология значимых изменений. Для текущего статуса систем смотри **`Development-Status.md`**.

---

## Июнь 2026 — архитектурная полировка menu flow ✅

**Цель:** убрать дублирование, углубить FSM, вынести настройки меню в SO, привести папки/namespace к единому виду.

**Ключевые изменения:**

| Область | Результат |
|---------|-----------|
| Menu flow | `ApplicationFlowController` + сервисы (`AppFlowSessionConnectService`, return/scene load) |
| Публичный API | `IAppFlowCommands`, `AppFlow.TryGet` — UI/network без прямого singleton |
| NGO connect | `NetworkSessionConnectRoutines` — общий host/client для flow и dev fallback |
| SessionStarter | `GameplayNetworkSessionStarter` — только dev Play / `-join`; не скрывает menu overlay |
| Return UX | `MenuReturnReason` → `IReturnFeedbackHandler` registry → `MenuReturnFeedback` |
| Настройки меню | `MenuConfig` SO: splash, таймауты, порт, fallback-сцена |
| Trials | `TrialSessionRegistry` — partial по доменам (progress, throws, modifiers, session-end) |
| LevelKit | `MagicSeal` / `AntiMagicZone` — только SO channels, без дублирующих `UnityEvent` |
| Mouse | states в `Gameplay/Mouse/States/`, signal driver base classes |
| Cleanup | Dev-скрипты в `Scripts/Dev/`, удалён мёртвый API |

**Документы:** `Architecture-Snapshot.md`, `MainMenu-And-Networking.md`, `Development-Status.md`.

---

## Июнь 2026 — Stage 6: закрыт MVP (мышь-саботажник) ✅

**Цель:** сценическая мышь вместо свободного NavMesh-AI — маршруты, триггеры, связка с Trials и Level Kit.

**Реализовано по шагам:**

| Шаг | Система | Ключевое |
|-----|---------|----------|
| 6.1 | Сетевой prefab | `MouseBrain`, `ServerNetworkTransform`, layers Spectral/Physical, `MouseConfig` |
| 6.2 | MouseRoute | `Path/Point_*`, end modes, waypoint events, gizmos |
| 6.3 | MouseCueTrigger | Co-op count, play once, route playback |
| 6.4 | Саботаж | waypoint → `EmptyEventChannel` → Stage 5 drivers |
| 6.5 | Trial reactions | `MouseTrialReaction` + `TrialProgressEventChannel` |
| 6.7 | Купол | `MouseDomeZone`, NavMesh flee, catch, `MouseCaughtChannel` |

**Polish:**
- `ReturnHidden` — скрытие на последней точке (не burrow teleport).
- Debug вынесен в `MouseDebugBootstrap`.
- `MouseVisualStub` fade, плавный поворот на маршруте.
- NavMesh manual sync для NGO + kinematic RB.
- `MouseCaughtListener` stub; `MouseRouteEditor` validate + direction arrows.

**Архитектурные решения:**
- Namespace `Catsss.Gameplay.Mouse` (конфликт с Input System).
- Shared path code: `WaypointPathFollower` / `WaypointPathSnapshot` в `Core/Path`.
- **6.6** (chase + rubberbanding) → **Stage 7.2**; параметры rubberband уже в `MouseConfig`.

**Документы:** `Stage 6.md`, `Development-Status.md`, `Architecture-Snapshot.md`, `Onboarding.md`.

---

## Май 2026 — Stage 5: закрыт MVP (Level Kit 5.1–5.4) ✅

**Цель:** универсальный конструктор уровня — печати, платформы, антимаг, ветер; связка через `EmptyEventChannel`.

**Реализовано по шагам:**

| Шаг | Система | Ключевое |
|-----|---------|----------|
| 5.1 | MagicSeal | E/trigger, Momentary/Toggle/OneShot, heavy/co-op requirements |
| 5.2 | KinematicPlatform | Waypoints snapshot, Cycle/Signal/Resonance, `MovingPlatformRider` |
| 5.3 | AntiMagicZone | Trigger/solid, штраф заряженного, `BlocksProjectiles`, signal driver |
| 5.4 | AeroZone | Target velocity, VerticalEquilibrium updraft, `IsAir`, signal driver |

**Архитектурные решения:**
- Server: печати, платформы, антимаг; Client (owner): ветер через `AeroZoneReceiver`.
- Визуал — stub-компоненты (MaterialPropertyBlock), логика без ссылок на particles/mesh.
- Выключенные зоны: коллайдер остаётся, alpha ~0.02.
- Исправление: один `EmptyEventChannel` на несколько слотов SignalDriver → triple-fire (раздельные channel asset'ы).

**Документы:** `Stage 5.md`, `Development-Status.md`, `Architecture-Snapshot.md`, `Onboarding.md`.

---

## Май 2026 — Stage 5.4: AeroZone (ветер)

**Реализовано:**

- `AeroZone` — trigger-box, target velocity, режим VerticalEquilibrium для updraft-колонн.
- `AeroZoneReceiver` на PlayerRoot — суммирование зон (owner-only).
- `NetworkPlayerController.ApplyWindInfluence()` — после FSM, включая Dash.
- `ChargeTypeDefinition`: `IsAir`, `AirWindSpeedMultiplier`; `IsHeavy` → ветер игнорируется.
- `AeroZoneSignalDriver`, `AeroZoneVisualStub`.

---

## Май 2026 — Stage 5.3: AntiMagicZone

**Реализовано:**

- `AntiMagicZone` — trigger (завеса) или solid BoxCollider; штраф только заряженного в активном trial.
- Телепорт на `RespawnPoint`, −1 попытка броска, **заряд остаётся**; 0 попыток → `TrialPenaltyReason.AntiMagicZone`.
- `BlocksProjectiles` + hook в `ChargeProjectile.TryHandleBarrierMissServer`.
- `AntiMagicZoneSignalDriver` — Enable/Disable/Toggle зоны и блока снарядов отдельно.
- `AntiMagicZoneVisualStub`.

---

## Май 2026 — Stage 5: KinematicPlatform (LevelKit 5.2)

**Цель:** универсальные движущиеся платформы для препятствий, дверей, гримуаров; связка с MagicSeal через EmptyEventChannel.

**Реализовано:**

- `KinematicPlatform` — snapshot waypoints из `Path/Point_*`, server FixedUpdate, Cycle Yoyo/Loop, Signal/Resonance.
- `KinematicPlatformSignalDriver` — Pressed/Released/OneShot → PlayForward/Reverse/Toggle/Stop + `EndPolicy`.
- `KinematicPlatformResonanceZone` — AnyPlayer / AnyActiveCharge / HeavyCharge.
- `ServerNetworkTransform`, `MovingPlatformRider` на игроке (owner-only delta).
- Editor gizmo для path.

**Документы:** `Stage 5.md`, `Development-Status.md`, `Architecture-Snapshot.md`.

---

## Май 2026 — Stage 5: MagicSeal (LevelKit 5.1)

**Цель:** первый универсальный lego-блок уровня — E/trigger-кнопки для будущих платформ, дверей, ветра и других пазлов.

**Реализовано:**

- `MagicSeal` — server-authoritative состояние `Idle/Pressed/Locked/Disabled`, политики `Momentary/Toggle/OneShot`.
- `MagicSealInteractable` — активация по E через существующие `IInteractable`, world prompt и URP outline.
- `MagicSealTriggerActivator` — trigger-плиты с требованиями `AnyPlayer` / `HeavyCharge`, минимумом игроков или `AllConnectedPlayers`.
- `MagicSealVisualStub` — клиентская заглушка цвета по реплицированному состоянию.
- SO-сигналы `EmptyEventChannel` для pressed/released/one-shot; локальные server `UnityEvent` оставлены для быстрых сценовых связок.

**Документы:** `Stage 5.md`, `Development-Status.md`, `Architecture-Snapshot.md`.

---

## Май 2026 — Stage 4: бросок заряда и передача (MVP) ✅

**Цель:** метание заряда между котами в co-op trial — Aim, homing-снаряд, catch/miss, лимит попыток.

**Реализовано:**

- `PlayerAimController` — ПКМ toggle Aim, ЛКМ throw (owner-only), `ThrowChargeServerRpc`.
- `PlayerThrowDirectionResolver` — луч из viewport (настройка `aimViewportY`), бросок в небо, синхрон с Cinemachine.
- `ChargeProjectile` — server kinematic homing, catch/miss, `TrialSessionRegistry` attempts.
- `PlayerAimVisualsPresenter` + `LineRendererAimTrajectory` — дуга предпросмотра от `ThrowOrigin`.
- `PlayerIncomingChargeIndicator` + visual stub — telegraph без пульсации; `ProjectileThrowSignals`.
- Edge cases 4.7: disconnect → restore snapshot кидавшему без декремента попыток; штраф bounds → `AbortAllForTrialServer`; auto-exit Aim; cooldown после catch.

**Решения:** без soft-lock; попытки и бонус таймера в `ChargeTypeDefinition`; **4.6 Aim-камера отложена** — текущая orbital.

**Документы:** `Stage 4.md`, `Development-Status.md`, `Architecture-Snapshot.md`, `Onboarding.md`.

**Git:** смержено в ветку **`Stage-5`** из `stage4/logic` (коммит «Этап 4 завершённый»).

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
