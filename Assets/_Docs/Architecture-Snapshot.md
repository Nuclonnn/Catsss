# Catsss — снимок архитектуры и структуры

**Живой документ:** структура кода и архитектурные договоры. Дополняет `GDD.md` и этапы `Stage 1–7`.

| Последнее обновление | Июнь 2026 |
|---------------------|----------|
| Текущий фокус | **Stage 6** ✅ MVP; следующий — **Stage 7** |

---

## Высокоуровневая структура `Assets`

| Область | Назначение |
|--------|-------------|
| `Scripts/Core/` | FSM, Events, Services, Timing, **Localization**, **WorldHints** |
| `Scripts/Network/` | NGO, UTP, session starter, **ServerNetworkTransform**, **ClientConnectInputValidator** |
| `Scripts/Menu/` | MainMenu UI, intent, overlay, **MenuConnectionFeedback** |
| `Scripts/Player/` | Контроллер, FSM, ввод, камера, interaction, **`AeroZoneReceiver`** |
| `Scripts/Player/Aim/` | Aim mode, траектория, `PlayerThrowDirectionResolver` |
| `Scripts/Charges/` | Заряды, **`Charges/Projectile/`**, перманентные модификаторы |
| `Scripts/Trials/` | Пилоны, зона bounds, реестр, штрафы |
| `Scripts/LevelKit/` | **Stage 5 ✅:** MagicSeal, KinematicPlatform, AntiMagicZone, AeroZone |
| `Scripts/Mouse/` | **Stage 6 ✅:** MouseBrain FSM, routes, cue, trial reactions, dome |
| `Scripts/Core/Path/` | `WaypointPathSnapshot`, `WaypointPathFollower` (platforms + mouse) |
| `Scripts/Interaction/` | `IInteractable`, промпт (через world hints) |
| `Scripts/Rendering/` | URP outline для фокуса взаимодействия |
| `Scripts/Configs/` | `GameConfig`, каталоги зарядов/перманентов |
| `Scripts/Editor/` | Bootstrap локализации, world hints, KinematicPlatform gizmo, **MouseRouteEditor** |
| `Localization/` | Unity Localization: locales, `UI_Strings` |
| `Configs/` | SO: GameConfig, Trials, AllBaffs, **WorldHints** |
| `Prefabs/` | PlayerRoot, PylonStart/End, **LevelKit/**, **Mouse/**, **WorldTextHintTriggerRoot** |
| `Scenes/` | **MainMenu** (вход), **Sandbox** (геймплей) |
| `_Docs/` | Документация проекта |

---

## Столпы архитектуры

1. **Логика ↔ визуал** — физика/сеть не ссылаются на Animator/VFX/Audio; наружу `event Action` или SO channels.
2. **FSM** — `IState`, `ITransition`, `IPredicate`; без enum-switch в геймплей-логике.
3. **SO Event Bus** — `EventChannel<T>`; при invoke — итерация по **копии** списка слушателей.
4. **Service Locator** — регистрация в `Awake`, `Get<T>()` throws if missing; без `Singleton.Instance`.
5. **Data-driven** — баланс в ScriptableObject configs, не размазан по prefab fields.
6. **NGO авторитет** — движение client-auth (`ClientNetworkTransform`); interaction/trials server-auth; визуал/RPC вручную.

---

## Диаграмма слоёв (упрощённо)

```
┌─────────────────────────────────────────────────────────┐
│  UI Layer: MainMenu, LocalizedUiText, WorldTextHintView │
├─────────────────────────────────────────────────────────┤
│  Gameplay: Player FSM, Trials, Charges, Interaction     │
├─────────────────────────────────────────────────────────┤
│  Core: FSM, Events, Services, Timing, Localization      │
├─────────────────────────────────────────────────────────┤
│  Network: ConnectionManager, NGO, SessionStarter        │
├─────────────────────────────────────────────────────────┤
│  Data: GameConfig, TrialDefinition, HintDefinition, …   │
└─────────────────────────────────────────────────────────┘
         ▲                              ▲
         │ SO EventChannels             │ ServiceLocator
         └──────── cross-system ────────┘
```

---

## Сетевой слой

| Компонент | Ответственность |
|-----------|-----------------|
| `ConnectionManager` | UTP `SetConnectionData`, host listen `0.0.0.0`, Start/Stop, transport role для сообщений об ошибках |
| `NetworkSessionIntent` | Статическая передача host/client между сценами |
| `GameplayNetworkSessionStarter` | Intent → NGO; client timeout; return to menu |
| `ClientConnectInputValidator` | IPv4 / localhost / hostname / port до UTP |
| `MenuConnectionFeedback` | Обратная связь MainMenu после failed connect |
| `ClientNetworkTransform` | Client-authoritative transform |
| `NetworkPlayerEventsRelay` | RPC-скелет для будущего визуала |

**Не реализовано:** Unity Relay, UGS, join-код.

Подробнее: **`MainMenu-And-Networking.md`**.

---

## Испытания и заряды (Stage 3)

```
PlayerInteractionController (owner)
    → focus + WorldTextHintPresenter.SetInteractionFocus
    → E → PlayerTrialInteractor → Server

TrialPylonStart → TrialSessionRegistry.TryBeginTrial
    → PlayerChargeController (initiator)

TrialBoundsZone (server trigger)
    → OnTriggerExit + active trial → ApplyTeamTrialPenalty (all players)

PlayerChargeController (timer expired)
    → ApplyTeamTrialPenalty

ApplyTeamTrialPenalty
    → ForceClearCharge (all) + TeleportFromServerClientRpc (all)
    → ResetTrialActive on pylons + TrialProgress Cancelled

TrialFinishZone → CompleteTrial
    → ClearCharge + PlayerPermanentModifiers (all players)
```

- `TrialSessionRegistry` в ServiceLocator; `TrialPenaltyReason` — расширяемый enum для будущего VFX/звука.
- Один `TrialDefinition` на пару start/finish; чекпоинт штрафа — `TrialPylonStart.PenaltyRespawnPoint`.
- URP outline: Rendering Layer bit 1, `InteractableOutlineRendererFeature`.

Подробнее: **`Stage 3.md`**.

---

## Снаряд и Aim Mode (Stage 4) ✅

```
PlayerAimController (owner, IsAiming)
    → PlayerThrowDirectionResolver (viewport ray, cached after Cinemachine)
    → LMB → ThrowChargeServerRpc → ChargeProjectile.InitializeServer + Spawn

ChargeProjectile (server, FixedUpdate)
    → homing → catch: catcher.TryApplyChargeWithRemainingServer (full duration)
    → miss: TrialSessionRegistry.TryRegisterThrowMissServer
    → PlayerIncomingChargeIndicator на цели (NetworkVariable thrower id)

ApplyTeamTrialPenalty / CancelActiveTrial / disconnect
    → ChargeProjectile.AbortAll* (без miss или restore snapshot на disconnect)
```

- Направление броска **не** `camera.forward` — `PlayerThrowDirectionResolver`.
- Визуал прицела: `PlayerAimVisualsPresenter` после `CinemachineCore.CameraUpdatedEvent`.
- `ProjectileThrowSignals` — Aim, incoming telegraph, attempts (для UI/VFX/Audio).
- Prefab: `ChargeProjectileRoot` в Network Prefabs.
- **4.6** отдельная Aim-камера — не реализована.

Подробнее: **`Stage 4.md`**.

---

## Level kit (Stage 5) ✅

### MagicSeal (5.1) — server

```
MagicSeal (NetworkObject)
    ← MagicSealInteractable (E) / MagicSealTriggerActivator (trigger)
    → EmptyEventChannel (Pressed / Released / OneShot)
    → MagicSealVisualStub (client)
```

Политики: Momentary, Toggle, OneShot. Требования: AnyPlayer, HeavyCharge, min players, AllConnectedPlayers.

### KinematicPlatform (5.2) — server

```
EmptyEventChannel → KinematicPlatformSignalDriver → KinematicPlatform
    → ServerNetworkTransform
    ← MovingPlatformRider (owner on PlayerRoot)
```

Режимы: Cycle (Yoyo/Loop), SignalDriven, Resonance. Waypoints snapshot из `Path/Point_*` на spawn.

### AntiMagicZone (5.3) — server

```
AntiMagicZone (trigger или solid collider)
    → заряженный игрок в trial: TeleportFromServerClientRpc + TryRegisterThrowMissServer
    → ChargeProjectile: TryHandleBarrierMissServer если BlocksProjectiles
    ← AntiMagicZoneSignalDriver + ClientRpc (визуал)
```

`TrialPenaltyReason.AntiMagicZone` при исчерпании попыток.

### AeroZone (5.4) — client (owner)

```
AeroZone (trigger) → AeroZoneReceiver (PlayerRoot) → NetworkPlayerController.ApplyWindInfluence()
    ← AeroZoneSignalDriver + ClientRpc (визуал, IsZoneActive)
```

Модель: **target velocity**; updraft — **VerticalEquilibrium** (пружина). `IsHeavy` → 0; `IsAir` → multiplier.

Подробнее: **`Stage 5.md`**.

---

## Мышь-саботажник (Stage 6) ✅

### MouseBrain FSM (server)

```
Hidden          — spawn в home burrow, presence Hidden
RouteFollow     — WaypointPathFollower, Spectral, waypoint events
DomeFlee        — Physical, NavMeshAgent flee, manual RB sync
Caught          — MouseCaughtChannel + ClientRpc
```

```
MouseCueTrigger / MouseTrialReaction
    → MouseBrain.PlayRouteServer(MouseRoute)
        → WaypointPathFollower (Core/Path)
            → MouseRouteWaypointEvent → EmptyEventChannel
                → LevelKit signal drivers

MouseRoute EndMode EnterDome / MouseDomeZone
    → DomeFlee → catch trigger + player → Caught
```

- **Transform sync:** `ServerNetworkTransform` на MouseRoot.
- **ReturnHidden:** скрытие на последней точке (без burrow teleport).
- **Rubberbanding:** параметры в `MouseConfig`; применение — **Stage 7.2**.
- **Визуал:** `MouseVisualStub` (client) — fade + MaterialPropertyBlock; не в FSM.
- **Dev:** `MouseDebugBootstrap`, `MouseCaughtListener` (stub до GameFlowManager).
- **Namespace:** `Catsss.Gameplay.Mouse` (избегает конфликта с Input System `Mouse`).

Подробнее: **`Stage 6.md`**.

---

## Локализация (`Scripts/Core/Localization/`)

| Тип | Роль |
|-----|------|
| `LocalizedTextReference` | Serializable ref + fallback + Bind |
| `LocalizedUiText` | Canvas static text |

**MainMenu:** `MainMenuLocalizedText` — IP hint, guest errors.

**World hints:** текст в `WorldTextHintDefinition` через тот же `LocalizedTextReference`.

Таблица: **`UI_Strings`** (en, ru).

Подробнее: **`Localization-And-WorldHints.md`**.

---

## World hints (`Scripts/Core/WorldHints/`)

| Тип | Роль |
|-----|------|
| `WorldTextHintDefinition` | SO: text, priority, show/hide rules |
| `WorldTextHintTrigger` | World object driver |
| `WorldTextHintPresenter` | На PlayerRoot, owner-only, merge по priority |
| `WorldTextHintView` | Billboard TMP |

Interaction reuse: `InteractionPromptView` : `WorldTextHintView`.

Подробнее: **`Localization-And-WorldHints.md`**.

---

## Модуль времени (`Catsss.Core.Timing`)

- `Timer`, `CooldownTimer`, `StopwatchTimer`, `GracePeriodTimer` (coyote / buffers).

---

## Namespaces

| Namespace | Содержимое |
|-----------|------------|
| `Catsss.Core.*` | FSM, Events, Services, Timing, Localization, WorldHints |
| `Catsss.Network` | Connection, validation, session |
| `Catsss.Menu` | MainMenu flow |
| `Catsss.Player` | Player controller, states, camera |
| `Catsss.Charges` | Charge + permanent modifiers |
| `Catsss.Charges.Projectile` | Снаряд, telegraph, throw signals |
| `Catsss.Player.Aim` | Aim controller, trajectory, throw direction |
| `Catsss.Trials` | Trial pylons, bounds, registry, penalty reasons |
| `Catsss.LevelKit` | Stage 5 puzzle blocks: MagicSeal, KinematicPlatform, AntiMagicZone, AeroZone |
| `Catsss.Gameplay.Mouse` | Stage 6: MouseBrain, routes, cue, dome |
| `Catsss.Core.Path` | Waypoint path snapshot + follower |
| `Catsss.Interaction` | Interactable contract, prompt settings |
| `Catsss.Rendering` | URP features |
| `Catsss.Configs` | GameConfig, charge/trial data |

---

## Карта документации

| Документ | Когда читать |
|----------|--------------|
| `Development-Status.md` | Что сделано / текущий этап |
| `DevLog.md` | Недавние изменения |
| `MainMenu-And-Networking.md` | Сеть, меню, тесты |
| `Localization-And-WorldHints.md` | RU/EN, подсказки |
| `Stage 3.md` | Trials MVP + настройка Unity |
| `Stage 4.md` | Aim + бросок (закрыт MVP) |
| `Stage 5.md` | Level kit (закрыт MVP) |
| `Stage 6.md` | Мышь-саботажник (закрыт MVP) |
| `Stage 7.md` | Следующий этап |
| `Onboarding.md` | Первый день в проекте |
