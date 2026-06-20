# ЭТАП 6: Мышь-саботажник и сценические маршруты

**Статус:** ✅ **MVP закрыт (6.1–6.5, 6.7 + polish).** **6.6** (финальная погоня + rubberbanding) → **Stage 7.2**. **Следующий этап:** Stage 7.

**Namespace скриптов:** `Catsss.Gameplay.Mouse` (не `Catsss.Mouse` — конфликт с `UnityEngine.InputSystem.Mouse`).

## Переосмысление этапа

Изначальная идея Stage 6 была построена вокруг свободного NavMesh-поведения. Для альфы это рискованно: игроки будут гоняться за мышью вместо платформинга, Trials останутся отдельно от сюжета, AI будет плохо контролироваться.

**Новая цель:** мышь — **сценический саботажник**, которого дизайнер настраивает через маршруты, триггеры и EventChannel. До финального купола мышь режиссируется вручную. NavMesh — только в куполе поимки.

## Дизайн-роль мыши

- напоминает игрокам цель уровня;
- появляется рядом с Trials и реагирует на прогресс;
- саботирует окружение без прямого урона (ветер, барьер, платформа);
- не ломает прохождение и не ловится до купола;
- создаёт ощущение «почти догнали», но поимка — только в куполе.

## Главный принцип настройки

```text
MouseCueTrigger / MouseTrialReaction
    -> MouseBrain.PlayRouteServer(MouseRoute)
        -> WaypointPathFollower двигает мышь по Path/Point_*
            -> MouseRouteWaypointEvent вызывает EmptyEventChannel
                -> LevelKit signal drivers реагируют
```

### Реализованные инструменты

| Компонент | Назначение |
|-----------|------------|
| `MouseBrain` | Серверное FSM: Hidden, RouteFollow, DomeFlee, Caught |
| `MouseRoute` | Waypoint-маршрут (`Path/Point_*`), скорость, end mode, события на точках |
| `MouseCueTrigger` | Trigger-зона → запуск маршрута (co-op count, play once) |
| `MouseTrialReaction` | Реакция на `TrialProgressEventChannel` (Started/Completed/Cancelled) |
| `MouseBurrowPoint` | Точка дома: **только стартовый spawn** в Hidden |
| `MouseDomeZone` | Trigger → `EnterDomeServer()` (запасной вход в купол) |
| `MouseVisualStub` | Клиент: spectral/physical tint + fade через MaterialPropertyBlock |
| `MouseCaughtListener` | Заглушка подписчика на `MouseCaughtChannel` (до Stage 7) |
| `MouseDebugBootstrap` | Dev-only: spectral на spawn, debug route, enter dome |
| `MouseConfig` | SO: слои, скорость маршрута, поворот, fade, dome flee, rubberband (7.2) |

**Общий path-код:** `WaypointPathSnapshot`, `WaypointPathFollower` в `Scripts/Core/Path/` (переиспользуется с KinematicPlatform).

---

## Задача 6.1: Сетевой префаб мыши ✅

**Префаб:** `Prefabs/Mouse/MouseRoot.prefab`

| Компонент | Назначение |
|-----------|------------|
| `NetworkObject` | NGO spawn |
| `ServerNetworkTransform` | Server-authoritative позиция |
| `Rigidbody` | Kinematic, без гравитации |
| `NavMeshAgent` | Disabled до купола |
| `MouseBrain` | FSM + presence replication |
| `MouseVisualStub` | На visual child |
| Collider (trigger) | Catch hitbox — только в `Physical` |

**Physics Layers** (TagManager): `Spectral` (8), `Physical` (9). Spectral ≠ Player; Physical = Player.

**Presence modes** (`MousePresenceMode`): `Hidden`, `Spectral`, `Physical` — реплицируются через `NetworkVariable`.

**Архитектура:** `MouseBrain` не ссылается на Animator/VFX/Audio. Наружу: `PresenceModeChanged`, `Materialized`, `Caught`, ClientRpc, SO channels.

---

## Задача 6.2: `MouseRoute` ✅

**Префаб-пример:** `Prefabs/Mouse/PathMouseRoot.prefab`

```text
MouseRoute_Trial1_Taunt
└── Path
    ├── Point_0
    ├── Point_1
    └── Point_2
```

| Поле | Описание |
|------|----------|
| `pathRoot` | Transform `Path` с дочерними `Point_*` |
| `speedOverride` | 0 = из `MouseConfig.RouteSpeed` |
| `endMode` | `StopAtEnd`, `ReturnHidden`, `EnterDome` |
| `waypointEvents` | Массив `MouseRouteWaypointEvent` (индекс точки + channel + wait) |

**ReturnHidden:** мышь **исчезает на последней точке** (без телепорта в burrow).

**Editor:** `MouseRouteEditor` — gizmo-стрелки направления, подпись `End: {EndMode}`, кнопка **Validate All Mouse Routes In Scene**.

**Движение:** плавный поворот (`MouseConfig.RouteTurnSpeedDegPerSec`, default 720°/с).

---

## Задача 6.3: `MouseCueTrigger` ✅

**Префаб:** `Prefabs/Mouse/MouseCueTrigger.prefab`

| Поле | Описание |
|------|----------|
| `mouse` | Ссылка на сценную `MouseBrain` |
| `route` | Какой `MouseRoute` проиграть |
| `playOnce` | Однократный запуск |
| `teleportToRouteStart` | Телепорт в Point_0 перед стартом |
| `blockWhileRouteActive` | Не перезапускать, пока маршь на маршруте |
| `countRequirement` | `AnyPlayerInZone`, `MinimumPlayers`, `AllConnectedPlayers` |
| `onStartedChannel` / `onFinishedChannel` | Опциональные EmptyEventChannel |

---

## Задача 6.4: Саботаж через EventChannel ✅

Мышь не знает про платформы/ветер/антимаг. На waypoint вызывается `EmptyEventChannel` → Stage 5 signal drivers.

```text
MouseRoute.waypointEvents[i]
    -> EmptyEventChannel.Invoke()
        -> KinematicPlatformSignalDriver / AeroZoneSignalDriver / AntiMagicZoneSignalDriver
```

Опционально: `waitSeconds` на точке — мышь ждёт перед следующим сегментом.

---

## Задача 6.5: Присутствие мыши рядом с Trials ✅

**Компонент:** `MouseTrialReaction` + `MouseTrialReactionBinding[]`

**Префаб:** `Prefabs/Mouse/MouseTrialReactions.prefab`

Слушает `TrialProgressEventChannel`, фильтрует по `trialId` + phase (`Started` / `Completed` / `Cancelled`), вызывает `MouseBrain.PlayRouteServer()`.

Bindings могут быть `playOnce` — не повторяются после срабатывания.

---

## Задача 6.6: Финальная погоня → Stage 7.2

**Не реализовано в Stage 6.** Перенесено в `GameFlowManager` (Stage 7.2):

- scripted chase route с `EndMode = EnterDome`;
- rubberbanding из `MouseConfig` (`RubberbandFarDistance`, `RubberbandSlowMultiplier`, `RubberbandNearDistance`);
- запуск после N завершённых Trials.

---

## Задача 6.7: Финальный купол и поимка ✅

**Вход:** `MouseRoute.EndMode = EnterDome` или trigger `MouseDomeZone`.

**DomeFlee state (сервер):**

1. `Physical` layer, catch collider enabled;
2. `MaterializeClientRpc` → визуал;
3. `NavMeshAgent` включается, warp на NavMesh (`MouseConfig.DomeNavMeshSampleRadius`, default 5 м);
4. Flee от ближайшего игрока (`DomeFleeDistance`, `DomeAgentSpeed` из config);
5. Manual sync: `updatePosition/Rotation = false` → `SyncNavMeshAgentTransformServer()` → `MoveServer` для NGO + kinematic RB.

**Поимка:** trigger игрока в dome → `CaughtMouseState` → `MouseCaughtChannel` + `CaughtClientRpc`.

**Победа/UI:** Stage 7.4 (`GameFlowManager`).

---

## Polish (закрыт)

| Пункт | Реализация |
|-------|------------|
| ReturnHidden на последней точке | `TransitionToHiddenAtCurrentPositionServer()` |
| Debug вынесен из MouseBrain | `MouseDebugBootstrap` |
| Заглушка поимки | `MouseCaughtListener` |
| Fade spectral/physical | `MouseVisualStub` + `MouseConfig.PresenceFadeDuration` |
| Плавный поворот на маршруте | `RouteTurnSpeedDegPerSec` |
| Gizmo/editor маршрута | стрелки, end label, validate button |
| NavMesh + NGO sync | manual agent sync в DomeFlee |

**Не делали (осознанно):** отдельный catch hitbox polish, чеклист автотестов.

---

## Инструкция по настройке в Unity

### 1. MouseRoot

```text
MouseRoot
├── Visual          (Renderer + MouseVisualStub)
├── CatchCollider   (trigger, layer Physical когда активен)
└── (root)          NetworkObject, ServerNetworkTransform, Rigidbody,
                    NavMeshAgent (off), MouseBrain
```

- Назначить `MouseConfig` asset (`Configs/Mouse/`).
- Назначить `homeBurrow` (`MouseBurrowPoint`) — только для **первого spawn**.
- Назначить `mouseCaughtChannel` SO.
- Добавить prefab в **Network Prefabs List**.
- Collision matrix: Spectral не коллидирует с Player; Physical — да.

### 2. Маршрут

1. Duplicate `PathMouseRoot` или создать GO + `MouseRoute`.
2. Под `Path` — `Point_0`, `Point_1`, …
3. Расставить точки, включить Gizmos.
4. Настроить `End Mode`, `waypointEvents`.

### 3. Cue / Trial reaction

- `MouseCueTrigger` в зоне старта сценки → route + mouse.
- `MouseTrialReaction` на сцене → channel + bindings per trial.

### 4. Купол

1. Запечь NavMesh под платформой купола.
2. `MouseDomeZone` trigger ИЛИ route с `EnterDome`.
3. Проверить `DomeNavMeshSampleRadius` в `MouseConfig`, если warp не находит mesh.

### 5. Dev-only (Sandbox)

- `MouseDebugBootstrap` на сцене → ссылка на MouseBrain, нужные флаги.
- `MouseCaughtListener` → `MouseCaughtChannel` для лога в консоль.

### 6. Связка с Level Kit

1. Создать `EmptyEventChannel` asset.
2. В `MouseRoute.waypointEvents` — channel + waypoint index.
3. Тот же asset на signal driver платформы/ветра/антимага.

---

## Проверка готовности этапа

**Код ✅** — все пункты ниже реализованы; **сцена** настраивается в Unity.

- [x] Мышь спавнится по сети, presence реплицируется.
- [x] Старт Hidden (teleport в home burrow), Spectral на маршруте, Physical в куполе.
- [x] `MouseCueTrigger` / `MouseTrialReaction` запускают маршрут на сервере.
- [x] Движение по waypoint-точкам, события на точках, wait.
- [x] `ReturnHidden` — fade и скрытие на последней точке.
- [x] `EnterDome` / `MouseDomeZone` → NavMesh flee + catch trigger.
- [x] `MouseCaughtChannel` при поимке.
- [ ] Финальная погоня + rubberbanding — **Stage 7.2**.
- [ ] Победа / экран — **Stage 7.4**.

---

## Связанные документы

| Документ | Содержание |
|----------|------------|
| `Stage 7.md` | GameFlowManager, chase, victory |
| `Stage 5.md` | Level kit, signal drivers |
| `Development-Status.md` | Общий статус проекта |
| `Architecture-Snapshot.md` | Mouse FSM, namespaces, path code |
