# Onboarding — Catsss

**Роль:** Gameplay / Network Programmer  
**Жанр:** 3D Co-op Puzzle-Platformer (сетевой, 2 игрока)  
**Цель:** отполированная **Alpha vertical slice** одного уровня.

---

## ЧАСТЬ 1: Архитектурные правила (The Pillars)

Прежде чем писать код — ознакомься. Отклонения не проходят ревью.

### Правило 1: Разделение логики и визуала

Скрипты физики, сети и управления **не знают** про Animator, Particles, Audio.

- `NetworkPlayerController` — Rigidbody и математика; наружу `event Action Jumped`, `Dashed`.
- Визуал на дочернем объекте подписывается на события и играет анимации/VFX.

### Правило 2: State Machine

Кастомная OOP FSM: `IState`, `ITransition`, `IPredicate`. **Без** больших enum-switch.

- Состояния: `NetworkPlayerLocomotionState`, `NetworkPlayerJumpState`, `NetworkPlayerDashState`.
- Внутри состояний — **нет** ссылок на Unity-визуал.

### Правило 3: SO Event Bus

Системы общаются через ScriptableObject **`EventChannel<T>`**.

- При invoke — итерация по **копии** списка подписчиков.
- Примеры ассетов: `Assets/Events/TrialProgressChannel.asset`.

### Правило 4: Service Locator

Без `Something.Instance`. Регистрация в `Awake`, получение через `ServiceLocator.Get<T>()`.

- Примеры: `ConnectionManager`, `TrialSessionRegistry`.

### Правило 5: Данные в Scriptable Objects

Баланс и контент — в `Assets/Configs/`, не размазан по prefab.

- `GameConfig`, `TrialDefinition`, `ChargeTypeDefinition`, `WorldTextHintDefinition`.

### Правило 6: Сеть (NGO)

- **Движение** — client-authoritative (`ClientNetworkTransform`).
- **Взаимодействие / trials** — server-authoritative (`Rpc(SendTo.Server)`).
- **Анимации** — без `NetworkAnimator`; ручные RPC (`NetworkPlayerEventsRelay`).
- **Транспорт сейчас:** UTP, IP + port (Relay — в планах, см. `Stage 2.md`).

Подробная карта: **`Architecture-Snapshot.md`**.

---

## ЧАСТЬ 2: С чего начать в репозитории

### 1. Прочитать статус

| Порядок | Файл |
|---------|------|
| 1 | `Development-Status.md` — что работает **сейчас** |
| 2 | `Architecture-Snapshot.md` — структура папок и namespaces |
| 3 | `Stage 4.md` — бросок заряда (закрыт MVP) |
| 4 | `Stage 5.md` — level kit (закрыт MVP) |
| 5 | `Stage 6.md` — мышь-саботажник (закрыт MVP) |
| 6 | `Stage 7.md` — **следующий этап** |
| 7 | `DevLog.md` — что менялось недавно |

### 2. Запустить проект

1. Открыть сцену **`MainMenu`**.
2. Play → **Host** → попадаешь в **Sandbox** с сетевым игроком.
2. Второй клиент: Guest → `127.0.0.1` + порт 7777.

Подробнее: **`MainMenu-And-Networking.md`**.

### 3. Этапы: что закрыто / что сейчас

**Stage 3 ✅:** пилон → заряд → финиш → перманент; штрафы bounds/таймер; E + outline; локализация; guest connect.

**Stage 4 ✅ (MVP):** ПКМ Aim, ЛКМ throw, `ChargeProjectile`, homing, попытки, telegraph.

**Stage 5 ✅ (MVP):** level kit — MagicSeal, KinematicPlatform, AntiMagicZone, AeroZone.

**Stage 6 ✅ (MVP):** мышь-актёр — `MouseRoute`, `MouseCueTrigger`, `MouseTrialReaction`, купол, поимка. Prefabs в `Prefabs/Mouse/`.

**Следующий:** Stage 7 — `GameFlowManager`, финальная погоня, победа.

### 4. Специализированные гайды

| Тема | Документ |
|------|----------|
| Меню, NGO, тесты мультиплеера | `MainMenu-And-Networking.md` |
| RU/EN, world hints | `Localization-And-WorldHints.md` |
| Дизайн игры | `GDD.md`, `ConceptDocument.md` |
| Задачи по этапам | `Stage 1.md` … `Stage 7.md` |

### 5. Editor-меню (bootstrap)

- **Catsss → Localization → Setup UI Strings (RU + EN)**
- **Catsss → Localization → Setup MainMenu Scene Texts**
- **Catsss → World Hints → Create Dash Hint Definition**

Запускать после pull, если добавлялись новые ключи локализации.

---

## ЧАСТЬ 3: Где лежит код

```
Assets/Scripts/
├── Core/          FSM, Events, Services, Timing, Localization, WorldHints
├── Network/       ConnectionManager, SessionStarter, Validator
├── Menu/          MainMenuController, Intent, Overlay
├── Player/        Controller, FSM states, Camera, Interaction
├── Player/Aim/    Aim, trajectory, throw direction
├── Charges/       ChargeController, Projectile/, PermanentModifiers
├── Trials/        Pylons, Registry, TrialDefinition
├── LevelKit/      MagicSeal, KinematicPlatform, AntiMagicZone, AeroZone
├── Interaction/   IInteractable, PromptSettings
├── Rendering/     URP outline feature
└── Configs/       GameConfig, charge catalogs
```

---

## ЧАСТЬ 4: Workflow команды

- **Программист (Cursor):** скрипты, архитектура, документация `_Docs/`.
- **Unity (ты):** сцены, prefab wiring, Build Settings, URP, тест в Play Mode.
- **Итерации:** маленький кусок → сборка → правка → следующий этап.
- **Git:** feature-ветки (`stage4/logic`, `Stage-5`, не только `main`); коммиты — по запросу.

После значимых изменений обновляй **`Development-Status.md`** и добавляй запись в **`DevLog.md`**.
