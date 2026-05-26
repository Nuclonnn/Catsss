# ЭТАП 4: Бросок заряда и передача между котами

**Статус:** План зафиксирован, реализация ещё не начата.

**Цель:** Заряженный игрок прицеливается и кидает физический снаряд. Снаряд закручивается к напарнику. Если напарник поймал — заряд (с обновлённым таймером) переходит к нему вместе с активным `trialId`. Если промах — снаряд исчезает, заряд возвращается кидавшему с бонусом к таймеру; после исчерпания попыток — командный штраф (как в Stage 3 phase B).

Этот документ — **дизайн + roadmap**. Исходное ТЗ задач 4.1–4.4 — внизу в свёрнутом блоке (для исторической справки). По ходу реализации сюда добавятся «Фактическая реализация» и «Настройка в Unity» по аналогии со `Stage 3.md`.

---

## 1. Архитектурные решения (зафиксированы)

### A. Передача активного `trialId`

- При **успешной поимке** активный `trialId` **переходит** с кидавшего на ловящего. Ловец продолжает испытание (может донести заряд до финиша).
- Во время **полёта снаряда** trial остаётся активным; снаряд **не считается носителем** в смысле `TrialBoundsZone` — зона смотрит только на игроков.
- Если кидавший в это время выходит из `TrialBoundsZone` или один из игроков падает — отрабатывает существующий `Stage 3` штраф (`ApplyTeamTrialPenalty`), снаряд при этом despawn’ится сервером.

### B. Таймер заряда при передаче

- Во время полёта снаряда **таймер не тикает** (заряд физически «не у кого»).
- При **поимке партнёром** таймер **сбрасывается на 100%** (refresh = `ChargeTypeDefinition.DurationSeconds`).
- При **промахе и возврате** кидавшему таймер восстанавливается из снимка `remainingSnapshot + ProjectileSettings.bonusReturnSeconds` (компенсация времени прицеливания и полёта).

### C. Система попыток (вместо «промах = штраф»)

- Лимит попыток на trial — `GameConfig.Projectile.maxThrowAttemptsPerTrial` (дефолт **3**).
- Попытки 1 и 2 — промах: снаряд исчезает в точке удара (звук+VFX «пшик», заглушка), **заряд мгновенно возвращается** кидавшему с бонусом к таймеру.
- Попытка 3 — промах: командный штраф (телепорт всех на `PenaltyRespawnPoint`, причина `TrialPenaltyReason.MissedTooManyTimes`).
- Счётчик хранится в `TrialSessionRegistry: Dictionary<int trialId, int attemptsLeft>`. Инициализация на `TryBeginTrial`, очистка на `CompleteTrial` / `CancelActiveTrial` / `ApplyTeamTrialPenalty` / disconnect.

### D. Источник заряда

- Отдельный `ChargeSource` **не делаем** в рамках Stage 4. Система попыток заменяет «возврат к источнику».
- Заряд по-прежнему выдаётся только через `TrialPylonStart` → `TrialSessionRegistry.TryBeginTrial`.

### E. Прицеливание: тип

- **Без классического soft-lock** (нет автоснэпа направления при выпуске).
- Есть **доводка** (homing): снаряд после выпуска плавно поворачивает к выбранной цели.
- Бросок всегда происходит **вперёд по направлению камеры**, потом самонаведение «дотягивает» к напарнику.

### F. Выбор цели

- В 2-coop цель = **второй игрок** (всегда). Если игроков больше — ближайший по дистанции (направление не учитывается).
- Если в сцене только 1 игрок — вход в Aim **заблокирован** (нет цели).
- В будущем (>2 игроков) можно ввести выбор по фокусу камеры — но пока YAGNI.

### G. Инпут

- **ПКМ — toggle Aim Mode** (вход/выход из режима прицеливания). Повторное нажатие ПКМ — выход без выстрела.
- **ЛКМ в режиме Aim — выстрел** (используем существующий action `Attack`).
- Action `Aim` добавляется новым в `Player`-карту `InputSystem_Actions.inputactions` с биндингом `<Mouse>/rightButton`.
- Вход в Aim блокируется если: нет заряда / не вышел `ProjectileSettings.throwCooldownAfterCatch` / нет цели.
- При потере заряда (любая причина) Aim авто-выходит.

### H. Архитектура Aim — `PlayerAimController`, не FSM-стейт

- `PlayerAimController` — отдельный `NetworkBehaviour` (owner-only) на `PlayerRoot`.
- **`AimingState` в `NetworkPlayerController.StateMachine` не создаём**: движение, прыжок, дэш в Aim не ограничены — стейт ничего бы не делал и нарушил бы single responsibility FSM.
- Aim — это **слой ввода + визуала**, поэтому отдельный компонент чище.
- Наружу — `event Action AimEntered`, `AimExited`, `event Action<NetworkPlayerController> TargetChanged`.

### I. Soft-lock UX

- **Линия/дуга прицеливания** через интерфейс `IAimTrajectoryRenderer` — основная реализация `LineRendererAimTrajectory`, симулирует движение снаряда (тот же `RotateTowards` цикл) и пишет точки в `LineRenderer`. Симуляция отделена в `static ProjectileTrajectorySimulator.Simulate(...)` — переиспользуется и снарядом, и предпросмотром.
- **Маркер цели** — `PlayerTargetMarker` (World-space Canvas над напарником в Aim Mode). Виден только владельцу-кидающему.
- **Outline напарника** в Aim Mode — переиспользуем существующий URP-контур (Rendering Layer 1).
- **Камера в Aim Mode** — переключение на отдельную `CinemachineCamera` (выше угол, чуть ближе радиус) через `PlayerAimCameraSwitcher` (приоритет Cinemachine). Cinemachine Brain делает плавный blend.

### J. Telegraph эффект на ловящем

- Пока снаряд летит, у потенциального ловца включается «приёмный» визуал — пульсация цвета заряда (через `MaterialPropertyBlock`, как `PlayerChargeVisualStub`).
- Реализация: `PlayerIncomingChargeIndicator` (`NetworkBehaviour` на `PlayerRoot`) с `NetworkVariable<ulong> IncomingFromClientId` (0 = нет). Сервер выставляет при спавне снаряда, сбрасывает на despawn. Визуал-стаб `PlayerIncomingChargeVisualStub` подписывается у себя.

### K. Балансовые параметры (`ProjectileSettings` в `GameConfig`)

| Поле | Дефолт | Назначение |
|------|--------|-----------|
| `throwSpeed` | 12 | скорость снаряда (м/с) |
| `homingTurnSpeedDegPerSec` | 110 | сила доводки (град/с). Закручивает в **любую** сторону без ограничения по «дуге сверху». |
| `maxLifetime` | 4 | сек, despawn по таймауту (засчитывается как промах) |
| `bonusReturnSeconds` | 1.5 | прибавка к `remaining` при возврате заряда после промаха |
| `maxThrowAttemptsPerTrial` | 3 | попыток на испытание |
| `throwCooldownAfterCatch` | 0.4 | сек, блокирует вход в Aim сразу после поимки |
| `catchTriggerRadius` | 0.9 | радиус дочернего «увеличенного» trigger-коллайдера ловли на игроке |
| `trajectorySimulationSteps` | 40 | шагов симуляции для дуги предпросмотра |
| `trajectorySimulationStep` | 0.05 | dt одного шага симуляции (с) |

### L. Сетевой авторитет

- Snapshot всех решений — **сервер**. `ChargeProjectile` живёт и считает физику только на сервере, клиенты видят интерполированный `NetworkTransform`.
- Спавн снаряда — `ServerRpc` от owner кидавшего.
- Гравитация на снаряд **не действует** (kinematic Rigidbody). Дуга — чисто homing.

### M. Что не делаем в Stage 4 (фиксируем как scope)

- Pass-through через антимаг-барьер с флагом `BlocksProjectiles` — Stage 5.
- Charge-up бросок (удержание ЛКМ для усиления) — отложено до полировки.
- Аркадная дуга (баллистика, gravity на снаряд) — отложено: пользователь предпочёл homing-only.
- Replace charge на лету у заряженного ловца (edge case) — обрабатываем как «обычная замена», без специальной логики.

---

## 2. Итеративный план (7 шагов)

Каждый шаг — самостоятельный коммит и проверяемое поведение. Между шагами — фронт «скрипты от меня» / фронт «сборка от тебя».

### Шаг 4.0 — Фундамент: `ProjectileSettings` + новый Input action

**Скрипты:**
- `Scripts/Configs/ProjectileSettings.cs` (`Serializable`) с полями из секции K.
- В `GameConfig` добавить `[field: SerializeField] public ProjectileSettings Projectile { get; private set; }`.
- В `InputSystem_Actions.inputactions` добавить action `Aim` (Button), биндинг `<Mouse>/rightButton`. Регенерация `InputSystem_Actions.cs`.
- В `PlayerInputReader` добавить `bool AimToggledThisFrame { get; }` + consume API, переименовать существующее на `ThrowPressedThisFrame` (или использовать как есть).

**Unity:**
- `Assets/Configs/GameConfig.asset` — заполнить дефолты `Projectile`.
- Открыть `InputSystem_Actions.inputactions`, добавить action, нажать Apply, убедиться, что код перегенерирован.

**Проверка:** `Debug.Log` ПКМ/ЛКМ у владельца показывает оба события.

---

### Шаг 4.1 — `PlayerAimController` + выбор цели + auto-exit

**Скрипты:**
- `Scripts/Player/Aim/PlayerAimController.cs` (`NetworkBehaviour`, owner-only):
  - `bool IsAiming`, `NetworkPlayerController CurrentTarget`.
  - События `AimEntered`, `AimExited`, `TargetChanged`.
  - Toggle на `AimToggledThisFrame`. Вход блокируется если нет заряда / cooldown / нет цели.
  - Подписка на `PlayerChargeController.ChargeChanged` — авто-выход при потере заряда.
- В `PlayerChargeController` отдать публично `float RemainingDuration { get; }`.

**Unity:**
- На `PlayerRoot.prefab` добавить `PlayerAimController` (без визуала пока).

**Проверка:** ПКМ → лог `AimEntered`; повторный ПКМ или потеря заряда → `AimExited`.

---

### Шаг 4.2 — `ChargeProjectile` (прямой полёт, без homing)

**Скрипты:**
- `Scripts/Charges/Projectile/ChargeProjectile.cs` (`NetworkBehaviour`):
  - Поля сервера: `_throwerClientId`, `_chargeId`, `_trialId`, `_direction`, `_targetTransform`, `_remainingSnapshot`, `_despawnAt`.
  - `InitializeServer(...)` — вызывается до `Spawn()`.
  - `FixedUpdate` (только сервер): прямое движение `position += dir * speed * dt` + lifetime check.
  - `OnTriggerEnter(Collider)` (только сервер): резолвит `PlayerChargeController` у `other` — это catch (если не thrower) или miss (если environment). Пока — `Debug.Log` + despawn.
- В `PlayerAimController` — `ThrowChargeServerRpc(Vector3 origin, Vector3 direction, ulong targetClientId)`. На сервере спавнит снаряд, передаёт параметры в `InitializeServer`.

**Unity:**
- Создать префаб `ChargeProjectileRoot.prefab`: `NetworkObject`, `NetworkTransform` (Interpolate), `Rigidbody` (Kinematic, useGravity off), `SphereCollider` (isTrigger), заглушка-меш.
- Зарегистрировать в `NetworkManager` prefabs list.
- Назначить ссылку на префаб в `PlayerAimController`.

**Проверка:** ПКМ → ЛКМ → снаряд летит прямо, через 4 сек despawn, лог Catch/Miss.

---

### Шаг 4.3 — Homing + поимка/промах + попытки + восстановление таймера

**Скрипты:**
- `ChargeProjectile.FixedUpdate`: добавить homing через `Vector3.RotateTowards`.
- `PlayerChargeController.TryApplyChargeWithRemainingServer(byte chargeId, int trialId, float remainingDuration)` — единая точка выдачи заряда с произвольным таймером.
- `TrialSessionRegistry`:
  - `Dictionary<int, int> _attemptsLeftByTrial`.
  - Поле `[SerializeField] GameConfig gameConfig` (для чтения `Projectile.maxThrowAttemptsPerTrial`).
  - `bool TryRegisterThrowMissServer(int trialId)` → возвращает `true` если попытки исчерпаны (нужен team penalty).
  - Cleanup в `CompleteTrial` / `CancelActiveTrial` / `ApplyTeamTrialPenalty` / `HandleClientDisconnected`.
- В enum `TrialPenaltyReason` добавить `MissedTooManyTimes`.
- `ChargeProjectile.HandleCatchServer`: ловцу `TryApplyChargeWithRemainingServer(chargeId, trialId, fullDuration)` (refresh 100%).
- `ChargeProjectile.HandleMissServer`: декремент попыток; если штраф — `ApplyTeamTrialPenalty`; иначе — возврат заряда кидавшему `TryApplyChargeWithRemainingServer(chargeId, trialId, _remainingSnapshot + bonusReturnSeconds)`.

**Unity:**
- В сцене `Sandbox` у `TrialSessionRegistry` пробросить ссылку на `GameConfig`.

**Проверка:** Полный цикл: бросок → поймал → передал на финиш / промахнулся 3 раза → team penalty.

---

### Шаг 4.4 — Визуал прицеливания: дуга + маркер цели

**Скрипты:**
- `Scripts/Charges/Projectile/ProjectileTrajectorySimulator.cs` — `static IReadOnlyList<Vector3> Simulate(...)` с пулом, чтобы переиспользоваться и снарядом (после рефакторинга `FixedUpdate` через тот же путь) и предпросмотром.
- `Scripts/Player/Aim/IAimTrajectoryRenderer.cs` — интерфейс: `void Show()`, `void Hide()`, `void UpdateTrajectory(Vector3 origin, Vector3 direction, Transform target)`.
- `Scripts/Player/Aim/LineRendererAimTrajectory.cs` — реализация через `LineRenderer`.
- `Scripts/Player/Aim/PlayerTargetMarker.cs` — worldspace canvas-маркер, виден только в Aim Mode у владельца.

**Unity:**
- На `PlayerRoot.prefab` добавить `AimVisualsRoot` с `LineRenderer` (URP unlit материал, толщина ~0.05).
- Создать дочерний префаб маркера (Canvas WorldSpace + иконка-заглушка).
- Связать всё в `PlayerAimController`.

**Проверка:** ПКМ → видна дуга (с homing’ом), над напарником иконка. Выстрел летит примерно по дуге.

---

### Шаг 4.5 — Telegraph эффект на ловящем

**Скрипты:**
- `Scripts/Charges/Projectile/PlayerIncomingChargeIndicator.cs` (`NetworkBehaviour`):
  - `NetworkVariable<ulong> IncomingFromClientId` (0 = нет).
  - `event Action<bool> IsIncomingTargetChanged`.
- В `ChargeProjectile.OnNetworkSpawn` (сервер) — выставить у ловца. На `OnNetworkDespawn` — сброс.
- `Scripts/Charges/Projectile/PlayerIncomingChargeVisualStub.cs` — пульсация emission/цвета через `MaterialPropertyBlock`, по аналогии с `PlayerChargeVisualStub`.

**Unity:**
- На `PlayerRoot.prefab` — `PlayerIncomingChargeIndicator` + `PlayerIncomingChargeVisualStub` со ссылками на рендерерами кота.

**Проверка:** Кидавший бросил → у ловца пульсирующий эффект, пока снаряд в воздухе. Поймал/промазал → эффект гаснет.

---

### Шаг 4.6 — Свич камеры в Aim Mode (Cinemachine)

**Скрипты:**
- `Scripts/Player/Aim/PlayerAimCameraSwitcher.cs` (owner-only): подписан на `PlayerAimController`, меняет `Priority` второй вирт.камере.

**Unity:**
- Добавить вторую `CinemachineCamera` в иерархию CameraRig игрока (`Cinemachine 3`).
- Настроить: vertical axis ≈ 55–65°, чуть меньший радиус.
- `CinemachineBrain.DefaultBlend` ≈ 0.3 с.
- Связать в `PlayerAimCameraSwitcher`.

**Проверка:** ПКМ → камера плавно «поднимается над игроком», ЛКМ-выстрел или повторный ПКМ → возвращается.

---

### Шаг 4.7 — Polish + edge cases + документация

**Скрипты:**
- `throwCooldownAfterCatch` в `PlayerAimController` — блокирует вход в Aim N секунд после `ChargeChanged(charged)`.
- Disconnect одного из игроков пока летит снаряд → сервер despawn’ит снаряд, возврат заряда кидавшему **без декремента попыток**.
- `TrialBoundsZone` штраф при полёте снаряда → сервер despawn’ит снаряд тихо (попытка не тратится, штраф уже сработал по другой причине).
- Auto-exit Aim при потере заряда (резерв на любой случай).

**Документация:**
- Обновить `Stage 4.md` (этот файл): добавить раздел «Фактическая реализация» и «Настройка в Unity» по шаблону `Stage 3.md`.
- `DevLog.md`: запись о завершении Stage 4 с ключевыми решениями (попытки, refresh таймера, без soft-lock).
- `Development-Status.md`: статус Stage 4 = ✅, перевод фокуса на Stage 5.
- `Architecture-Snapshot.md`: новая секция «Снаряд и Aim Mode» с диаграммой потока catch/miss.

---

## 3. Сводная диаграмма потока (Stage 4)

```
PlayerAimController (owner, IsAiming = true)
    │  LMB → ThrowChargeServerRpc(origin, direction, targetClientId)
    ▼
TrialSessionRegistry / NetworkPlayerController (server)
    │  спавн ChargeProjectile, charge = 0 у thrower, _remainingSnapshot
    ▼
ChargeProjectile (server, FixedUpdate)
    │  RotateTowards(direction, toTarget, turnSpeed*dt)
    │  position += direction * speed * dt
    │  PlayerIncomingChargeIndicator.IncomingFromClientId = throwerClientId
    │
    ├── OnTriggerEnter: PlayerChargeController other != thrower
    │     → catcher.TryApplyChargeWithRemainingServer(chargeId, trialId, fullDuration)
    │     → catcher активный носитель trial; despawn снаряда
    │
    ├── OnTriggerEnter: environment
    │     → TrialSessionRegistry.TryRegisterThrowMissServer(trialId)
    │     │   ├── попыток ещё > 0:
    │     │   │     thrower.TryApplyChargeWithRemainingServer(chargeId, trialId, _remainingSnapshot + bonus)
    │     │   │     despawn снаряда, лёгкий VFX «пшик»
    │     │   └── попытки = 0:
    │     │         TrialSessionRegistry.ApplyTeamTrialPenalty(trialId, MissedTooManyTimes)
    │     │         despawn снаряда
    │
    └── lifetime expired → как miss (засчитывается попытка)
```

---

## 4. Открытые риски

| Риск | Митигейт |
|------|----------|
| Регенерация `InputSystem_Actions.cs` после добавления action — ручной шаг в Unity | Чёткая инструкция в Шаге 4.0, дополнительный smoke-test через лог |
| LineRenderer для дуги может «прыгать» при быстром повороте камеры | Симуляцию делаем в `LateUpdate` после CinemachineBrain (как уже сделано для `MoveDirection` в `NetworkPlayerController`) |
| Дочерний catch-trigger (`catchTriggerRadius = 0.9`) может ловить чужие OverlapSphere в Trial bounds | Резолв через `GetComponentInParent<PlayerChargeController>()`, фильтр по NetworkObject |
| Снаряд в одном физическом тике может «прошить» партнёра при высокой скорости | Если будет видно — добавить `SphereCast` от прошлой позиции до новой в FixedUpdate |
| Возврат заряда после промаха в момент, когда thrower вне TrialBoundsZone (упал) | Существующий `TrialBoundsZone` уже сработает раньше; despawn снаряда без декремента попыток |

---

<details>
<summary>Исходное ТЗ задач 4.1–4.4 (справочно, до уточнений)</summary>

**4.1.** Префаб `ChargeProjectile` с `NetworkObject`, `NetworkTransform`, kinematic `Rigidbody`, `SphereCollider` (trigger). Скрипт `HomingProjectile` (только сервер) — `speed`, `homingTurnSpeed`, `Vector3.RotateTowards` к `targetTransform`.

**4.2.** Прицеливание soft-lock: зажатие ПКМ у локального заряженного, SphereCast/конусная проверка, ближайший напарник, `currentTargetID`.

**4.3.** Бросок: отпускание ПКМ. Клиент: снимает с себя `ChargedState` (ID=0). `ServerRpc_ThrowCharge(targetPlayerID, chargeID, direction)`. Сервер: спавнит префаб, передаёт thrower/target/chargeId, `NetworkObject.Spawn()`.

**4.4.** Поимка/промах (сервер): тег `Player` (не thrower) → выдать `chargeId` + despawn. Environment / `AntiMagicZone` → `ClientRpc_PlayFizzleVFX` + despawn; штраф — возврат к Источнику.

**Расхождения с финальным планом:**

- ПКМ — **toggle Aim Mode**, не «удержание». Выстрел — ЛКМ (не отпускание ПКМ).
- **Без классического soft-lock**: бросок строго вперёд по камере, доводка догоняет.
- Промах ≠ командный штраф сразу. Действует **система попыток** (3) с возвратом заряда + бонус таймеру.
- **`ChargeSource`** не делаем — система попыток заменяет необходимость возврата к источнику.
- Антимаг-барьер (`BlocksProjectiles`) — Stage 5.
- Камера в Aim переключается на отдельную CinemachineCamera (выше угол) — не было в ТЗ.
- Telegraph эффект на ловящем — не было в ТЗ.

</details>

---

*После начала реализации — этот документ обрастёт разделами «Фактическая реализация» и «Настройка в Unity» по образцу `Stage 3.md`.*
