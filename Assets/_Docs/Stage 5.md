# ЭТАП 5: Level Kit (конструктор уровня)

**Статус:** ✅ **Завершён (MVP)** — 5.1–5.4 реализованы и проверены в Sandbox. Опционально: **5.5** — отдельная тестовая комната / polish префабов.

**Цель:** Универсальные сетевые компоненты для пазлов уровня. Уровень собирается из ограниченного набора префабов с настройкой в Inspector.

**Предусловие:** **Stage 4** ✅ — `ChargeProjectile`, заряды, trials, штрафы.

**Следующий этап:** **Stage 6** (см. `Stage 6.md`) или **Stage 7** (сюжетные пилоны) — по roadmap.

**Документы:** `Development-Status.md`, `Architecture-Snapshot.md`, `DevLog.md`, `Stage 4.md`.

---

## Итоги этапа (кратко)

| Шаг | Система | Авторитет | Статус |
|-----|---------|-----------|--------|
| 5.1 | **MagicSeal** — E/trigger-кнопки, Momentary/Toggle/OneShot | Server | ✅ |
| 5.2 | **KinematicPlatform** — waypoints, Cycle/Signal/Resonance, rider | Server | ✅ |
| 5.3 | **AntiMagicZone** — штраф заряженного, блок снаряда | Server | ✅ |
| 5.4 | **AeroZone** — ветер (target velocity + equilibrium updraft) | Client (owner) | ✅ |
| 5.5 | Sandbox-комната level kit, polish префабов | — | ⏳ опционально |

**Связка систем:** MagicSeal → `EmptyEventChannel` → SignalDriver платформы / антимага / ветра. Визуал везде через stub-компоненты (MaterialPropertyBlock), логика не зависит от мешей.

**Ключевые решения:**
- Печати и платформы — **server-authoritative**; ветер — **client-auth** на owner (совместимо с `ClientNetworkTransform`).
- Антимаг: заряд **остаётся** после штрафа; −1 попытка броска; 0 попыток → `TrialPenaltyReason.AntiMagicZone`.
- Ветер: не «бесконечное ускорение», а **target velocity**; для updraft-колонн — режим **VerticalEquilibrium** (пружина к линии внутри box).
- `ChargeTypeDefinition`: **`IsHeavy`** → ветер игнорируется; **`IsAir`** → `AirWindSpeedMultiplier`.
- Выключенные зоны (антимаг/ветер): коллайдер **остаётся**, визуал alpha ~0.02.

---

## 1. Архитектурные договоры (как в проекте)

| Система | Авторитет | Коммуникация наружу |
|---------|-----------|---------------------|
| Печать, платформа, барьер (логика) | **Server** | `EventChannel` / `UnityEvent` / `Action` |
| Визуал (материал, particles) | Client / local stub | подписка на события логики |
| Аэрозона (ветер) | **Client** на owner | сила на `NetworkPlayerController` (client-auth движение) |

Без Singleton; конфиги в SO где есть баланс.

---

## 2. Задачи (исходное ТЗ)

### 5.1 — MagicSeal (универсальные кнопки)

- Скрипт `MagicSeal` — server-authoritative ядро состояния.
- Источники активации: `MagicSealInteractable` (E) и `MagicSealTriggerActivator` (trigger/collider).
- Режимы поведения: `Momentary`, `Toggle`, `OneShot`.
- Trigger-требования: любой игрок, `HeavyCharge`, минимум игроков, все подключённые игроки.
- Сигналы наружу: `EmptyEventChannel` на pressed/released/one-shot + локальные server `UnityEvent`.
- Визуал-заглушка: `MagicSealVisualStub` меняет цвет по реплицированному состоянию.

### 5.2 — Кинематические платформы (Grimoire)

- `KinematicPlatform` — server path mover, snapshot waypoints на spawn.
- `ServerNetworkTransform` — server-authoritative синхронизация.
- `KinematicPlatformSignalDriver` — `EmptyEventChannel` → команды платформы.
- `KinematicPlatformResonanceZone` — trigger-child, heavy/any charge.
- `MovingPlatformRider` на `PlayerRoot` — owner переносится вместе с платформой.
- Cycle: **Yoyo** и **Loop**; Signal: Momentary/Toggle/OneShot mapping; easing Linear/SmoothStep.

### 5.3 — Антимагические зоны

- `AntiMagicZone` — один скрипт: **trigger** (завеса) или **solid** BoxCollider (поверхность).
- Заряженный игрок в активном trial: телепорт на `RespawnPoint`, −1 попытка броска, **заряд остаётся**; 0 попыток → командный штраф.
- `BlocksProjectiles`: снаряд = обычный промах; иначе пролетает сквозь trigger-завесу.
- `AntiMagicZoneVisualStub` — полупрозрачная заглушка (завеса); на твёрдые объекты — опционально.

### 5.4 — AeroZone (ветер)

- **Client-only** на owner: `AeroZone` + `AeroZoneReceiver` на `PlayerRoot`.
- Модель: **target velocity** (скорость тянется к `direction * strength`, без бесконечного разгона).
- **VerticalEquilibrium** — опционально для updraft-колонн: пружина к линии равновесия внутри box.
- `Heavy` → игнор; `IsAir` → `AirWindSpeedMultiplier`; обычное состояние → 100%.
- Ветер работает **во время Dash**.
- `AeroZoneSignalDriver` — только вкл/выкл зоны (Enable/Disable/Toggle).

---

## 3. План реализации (предлагаемый порядок)

| Шаг | Содержание | Зависимости |
|-----|------------|-------------|
| 5.0 | Папка `Scripts/LevelKit/`, пустые prefab-заглушки, EventChannel ассеты | — |
| 5.1 | `MagicSeal` + визуал-stub | `PlayerInteractionController`, E |
| 5.2 | `KinematicPlatform` + сигнал от печати | NGO server transform |
| 5.3 | `AntiMagicZone` + hook в `ChargeProjectile` | Stage 4 |
| 5.4 | `AeroZone` + чтение charge state на клиенте | FSM / `PlayerChargeController` |
| 5.5 | Sandbox-тестовая комната, документация «Настройка в Unity» | все выше |

---

## 4. Интеграция со Stage 4

- `ChargeProjectile` при контакте с `AntiMagicZone` (`IsZoneActive && BlocksProjectiles`) → промах через `TryHandleBarrierMissServer`.
- Логика попыток броска — только через `TrialSessionRegistry`; барьер не дублирует счётчик.
- Печати вызывают платформы/зоны через **`EmptyEventChannel`** без прямой ссылки на визуал.

---

## 4.1. Фактическая реализация 5.1 — MagicSeal

| Скрипт | Роль |
|--------|------|
| `Scripts/LevelKit/MagicSeal.cs` | Серверное состояние печати, SO-сигналы, server `UnityEvent` |
| `MagicSealInteractable.cs` | Активация по E через текущий `IInteractable`, prompt и outline |
| `MagicSealTriggerActivator.cs` | Trigger-активация от игроков, heavy-проверка, минимум/все игроки |
| `MagicSealVisualStub.cs` | Клиентская визуальная заглушка по состоянию печати |

Состояния: `Idle`, `Pressed`, `Locked`, `Disabled`.

Политики:

- `Momentary` — удержание trigger; E даёт короткий импulse (Pressed→Released), для платформ рекомендуется trigger-плита.
- `Toggle` — переключает `Idle`/`Pressed`; Pressed Channel на первом нажатии, Released — на втором.
- `OneShot` — только **OneShot Channel** (без Pressed), печать в `Locked`.

---

## 4.2. Фактическая реализация 5.2 — KinematicPlatform

| Скрипт | Роль |
|--------|------|
| `KinematicPlatform.cs` | Server path mover, snapshot waypoints, Cycle/Signal/Resonance |
| `KinematicPlatformSignalDriver.cs` | Подписка на `EmptyEventChannel`, mapping действий, пресеты |
| `KinematicPlatformResonanceZone.cs` | Trigger-child для resonance |
| `KinematicPlatformSignalTravelMode.cs` | OneWay / Yoyo для signal-driven движения |
| `ServerNetworkTransform.cs` | Server-authoritative NetworkTransform |
| `MovingPlatformRider.cs` | Owner-only перенос игрока с платформой |
| `Editor/KinematicPlatformEditor.cs` | Gizmo линий между Point_* |

Режимы платформы: **Cycle** (Yoyo/Loop), **SignalDriven**, **Resonance**. SignalDriver: пресеты ToggleBridge, OneShotGate, HoldElevator и др.

---

## 4.3. Фактическая реализация 5.3 — AntiMagicZone

| Скрипт | Роль |
|--------|------|
| `AntiMagicZone.cs` | Trigger/solid антимаг, штраф заряженного игрока, блок снаряда |
| `AntiMagicZoneSignalDriver.cs` | EmptyEventChannel → вкл/выкл зоны и BlocksProjectiles |
| `AntiMagicZoneVisualStub.cs` | Полупрозрачная заглушка завесы |

Hook в `ChargeProjectile`: учитывает `IsZoneActive && BlocksProjectiles`. Штраф: `TrialPenaltyReason.AntiMagicZone`.

---

## 4.4. Фактическая реализация 5.4 — AeroZone

| Скрипт | Роль |
|--------|------|
| `AeroZone.cs` | Trigger-box ветра, target velocity / vertical equilibrium |
| `AeroZoneReceiver.cs` | Owner-only суммирование зон на PlayerRoot |
| `AeroZoneSignalDriver.cs` | EmptyEventChannel → вкл/выкл зоны |
| `AeroZoneVisualStub.cs` | Полупрозрачная заглушка потока |
| `AeroZoneWindMode.cs` | TargetVelocity / VerticalEquilibrium |

Hook в `NetworkPlayerController.ApplyWindInfluence()` — после FSM, включая Dash.

Расширение `ChargeTypeDefinition`: **`IsAir`**, **`AirWindSpeedMultiplier`**.

---

## 4.5. Общий список скриптов LevelKit

Папка: `Assets/Scripts/LevelKit/` (~31 файл). Включая enum/helper-типы для signal binding, activation policy, wind mode.

Player-side: `AeroZoneReceiver` в `Assets/Scripts/Player/`. Network transform: `ServerNetworkTransform` в `Assets/Scripts/Network/`.

---

### E-печать

```
MagicSealRoot          NetworkObject, MagicSeal, MagicSealInteractable, Collider
├── Visual             MeshRenderer, InteractableHighlightStub, MagicSealVisualStub
└── PromptAnchor       Transform
```

- `MagicSeal`: выбрать `Activation Policy`, назначить `Pressed/Released/OneShot` `EmptyEventChannel` при необходимости.
- `MagicSealInteractable`: назначить `Prompt Anchor`, `Prompt Settings`, `Highlight Stub`.
- Collider на объекте должен попадать в `PlayerInteractionController.interactableMask`.

### Trigger-печать

```
MagicSealRoot          NetworkObject, MagicSeal
├── Visual             MeshRenderer, MagicSealVisualStub
└── Trigger            Collider isTrigger, MagicSealTriggerActivator
```

- `Player Requirement = AnyPlayer` для обычной нажимной плиты.
- `Player Requirement = HeavyCharge` для тяжёлой плиты.
- `Count Requirement = AllConnectedPlayers` для кооп-кнопки «оба игрока стоят».
- `MinimumQualifiedPlayers` использовать для будущих пазлов на 2+ игроков.

### KinematicPlatform prefab

```
KinematicPlatformRoot     NetworkObject, Rigidbody, Collider, ServerNetworkTransform,
                          KinematicPlatform, [KinematicPlatformSignalDriver]
├── Path                  (якоря waypoints — snapshot на spawn, не двигаются логикой)
│   ├── Point_0
│   ├── Point_1
│   └── Point_2
├── Visual                (меш, опционально)
└── ResonanceTrigger      (опционально: trigger + KinematicPlatformResonanceZone)
```

- Prefab добавить в **Network Prefabs List**.
- `Path/Point_*` — расставить в Scene; на Play позиции кэшируются, root с collider едет по кэшу.
- **Cycle:** `Activation Mode = Cycle`, выбрать `Loop Mode` Yoyo или Loop.
- **Signal + кнопка:** `Activation Mode = SignalDriven`, создать `EmptyEventChannel`, назначить на MagicSeal и `KinematicPlatformSignalDriver`.
  - **ToggleBridge** (пресет): Pressed→PlayForward, Released→PlayReverse, Travel=OneWay.
  - **OneShotGate**: OneShot→PlayForward, Travel=OneWay.
  - **OneShotStartYoyo**: OneShot→PlayForward, Travel=Yoyo (остановка — через Toggle или Stop).
  - **HoldElevator** (trigger Momentary): Pressed→Forward, Released→Reverse.
  - `ReleaseEndPolicy` только на **KinematicPlatformResonanceZone**, не на SignalDriver.
- **Resonance:** `Activation Mode = Resonance`, trigger-child с `KinematicPlatformResonanceZone`.
- **PlayerRoot / MovingPlatformRider:**
  - Компонент уже на `PlayerRoot`; работает **только у owner** (локальный игрок).
  - **Feet Probe** — тот же `GroundProbe`, что у `NetworkPlayerController` (дочерний Transform у ног).
  - **Probe Distance** — ~0.55–0.75 (должен доставать до верха платформы под ногами).
  - **Platform Mask** — слои с collider платформы (если платформа на Default — оставить Everything).
  - **Draw Debug** — включить на время теста: зелёный луч = платформа найдена, красный = нет.
  - Платформа: `KinematicPlatform` + `BoxCollider` (не trigger) + kinematic `Rigidbody` на **одном root** с collider.
  - Платформа должна быть в **Network Prefabs List** и засpawnена (не просто scene object без NetworkObject spawn).

### AntiMagicZone

**Завеса (trigger):**
```
AntiMagicVeilRoot
├── AntiMagicZone
├── BoxCollider (isTrigger = true)
├── RespawnPoint
└── Visual → MeshRenderer + AntiMagicZoneVisualStub
```

**Твёрдая поверхность:** `AntiMagicZone` + `BoxCollider` (не trigger) + `RespawnPoint` на любом объекте.

- **Blocks Projectiles:** true = промах снаряда; false = пролёт сквозь завесу.
- Заряженный в trial: −1 попытка, телепорт на RespawnPoint, заряд остаётся; 0 попыток → `TrialPenaltyReason.AntiMagicZone`.
- UnityEvent `chargedPlayerPenalized` / `projectileBlocked` — заготовка под звук/VFX.

**Signal + кнопка (управляемая завеса):**
- Root: `NetworkObject` + `AntiMagicZone` + `AntiMagicZoneSignalDriver`
- Те же `EmptyEventChannel`, что на MagicSeal (Pressed/Released/OneShot — **разные** asset'ы)
- На каждое событие два поля: **Zone Action** (Enable/Disable/Toggle/None) и **Projectile Block Action**
- Пример Toggle: Pressed → Zone Enable + Block Enable; Released → Zone Disable + Block Disable
- `Zone Starts Enabled` — начальное состояние до первого сигнала
- Визуал гаснет (alpha ~0.02), коллайдер **остаётся**

### AeroZone (ветер)

**Базовый поток:**
```
AeroZoneRoot           NetworkObject (опционально, если signal), AeroZone
├── BoxCollider        isTrigger = true
└── Visual             MeshRenderer + AeroZoneVisualStub (URP transparent)
```

- **Wind Direction** + **Use Local Direction** — куда дует (стрелка в gizmo).
- **Target Speed** — целевая скорость потока (м/с), не ускорение.
- **Approach Acceleration** — как быстро скорость игрока сходится к target (м/с²).
- **Wind Mode:**
  - `TargetVelocity` — боковой/наклонный ветер, снос траектории.
  - `VerticalEquilibrium` — updraft: горизонталь как target velocity; вертикаль — пружина к **Equilibrium Normalized Height** (0=низ box, 1=верх). Жёлтая линия в gizmo.
- **Equilibrium Spring / Damping** — подбираются на updraft-колоннах (игрок «левитирует» на линии).

**Модификаторы заряда** (`ChargeTypeDefinition`):
- `IsHeavy` — ветер не действует.
- `IsAir` + `AirWindSpeedMultiplier` — усиленный поток (например ×2.5).

**PlayerRoot:**
- Добавить `AeroZoneReceiver` (или он подтянется через `RequireComponent` на `NetworkPlayerController`).
- **Draw Wind Debug** — луч целевой скорости в Scene.

**Signal + кнопка (управляемый поток):**
```
AeroZoneRoot           NetworkObject + AeroZone + AeroZoneSignalDriver
├── BoxCollider        isTrigger
└── Visual             AeroZoneVisualStub
```
- Те же `EmptyEventChannel`, что на MagicSeal.
- На каждое событие одно поле **Zone Action**: Enable / Disable / Toggle / None.
- `Zone Starts Enabled` — начальное состояние.
- Выключенная зона: визуал alpha ~0.02, коллайдер **остаётся**, ветер не действует.

---

## 6. Проверка готовности этапа

- [x] Печать Standard/E отправляет сигнал (2 игрока, host + client).
- [x] Trigger-печать удерживается, пока подходящий игрок внутри зоны.
- [x] Heavy-печать отсекает игрока без `ChargeTypeDefinition.IsHeavy`.
- [x] Кооп-печать с `AllConnectedPlayers` срабатывает только когда оба игрока в trigger.
- [x] Барьер с зарядом — штраф по ТЗ; снаряд гасится при `BlocksProjectiles`.
- [x] Ветер не ломает сетевую синхронизацию позиции (client-auth).
- [x] KinematicPlatform + MovingPlatformRider — игрок едет с платформой.
- [x] AeroZone: target velocity и equilibrium updraft; Heavy/Air-модификаторы.

---

*Stage 5 MVP закрыт (май 2026). Следующий фокус — Stage 6 / Stage 7 по roadmap.*
