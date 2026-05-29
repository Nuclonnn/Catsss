# Catsss — снимок архитектуры и структуры

**Живой документ:** структура кода и архитектурные договоры. Дополняет `GDD.md` и этапы `Stage 1–7`.

| Последнее обновление | Май 2026 |
|---------------------|----------|
| Текущий фокус | Stage 4 MVP (Aim + снаряд); далее Stage 5 |

---

## Высокоуровневая структура `Assets`

| Область | Назначение |
|--------|-------------|
| `Scripts/Core/` | FSM, Events, Services, Timing, **Localization**, **WorldHints** |
| `Scripts/Network/` | NGO, UTP, session starter, **ClientConnectInputValidator** |
| `Scripts/Menu/` | MainMenu UI, intent, overlay, **MenuConnectionFeedback** |
| `Scripts/Player/` | Контроллер, FSM, ввод, камера, interaction, trial interactor |
| `Scripts/Charges/` | Временные заряды, снаряд (`Projectile/`), перманентные модификаторы |
| `Scripts/Player/Aim/` | Aim mode, траектория, направление броска |
| `Scripts/Trials/` | Пилоны, зона bounds, реестр, штрафы |
| `Scripts/Interaction/` | `IInteractable`, промпт (через world hints) |
| `Scripts/Rendering/` | URP outline для фокуса взаимодействия |
| `Scripts/Configs/` | `GameConfig`, каталоги зарядов/перманентов |
| `Scripts/Editor/` | Bootstrap локализации, world hints, scene wiring |
| `Localization/` | Unity Localization: locales, `UI_Strings` |
| `Configs/` | SO: GameConfig, Trials, AllBaffs, **WorldHints** |
| `Prefabs/` | PlayerRoot, PylonStart/End, **WorldTextHintTriggerRoot** |
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

## Снаряд и Aim Mode (Stage 4)

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
- **4.6** отдельная Aim-камера — не реализована.

Подробнее: **`Stage 4.md`**.

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
| `Stage 4.md` | Aim + бросок + настройка Unity |
| `Onboarding.md` | Первый день в проекте |
