# ЭТАП 3: Заряды, перманентные навыки, испытания (пилоны)

Документ объединяет **исходное ТЗ этапа**, **фактическую реализацию MVP** (пилон → заряд → финиш → бафф всей команде) и **инструкции по настройке в Unity**.

Сюжетные «ловушки» мыши из `Stage 7.md` (поглощение заряда, `GameFlowManager`, `PylonActivatedChannel`) — **отдельный будущий этап**. Текущие префабы `PylonStartRoot` / `PylonEndRoot` — **испытания (trials)** для альфа-петли прогрессии.

---

## Игровой цикл (проверено)

1. Игрок подходит к **стартовому пилону** (`TrialPylonStart`) в радиусе взаимодействия → подсветка контура + world-space промпт / hint на **PlayerRoot**.
2. **E** (только у владельца) → `PlayerTrialInteractor` → сервер: `TrialSessionRegistry.TryBeginTrial` → временный заряд **только инициатору** (`PlayerChargeController`).
3. Пока испытание **активно**, команда должна оставаться в **`TrialBoundsZone`** и успеть дойти до финиша.
4. Игрок с активным `trialId` и `ChargeId > 0` заходит в **финишную зону** (`TrialFinishZone` с тем же `TrialDefinition`).
5. Сервер: снимает заряд, выдаёт **перманентный модификатор всем подключённым игрокам** (`PlayerPermanentModifiers`), пилон помечается исчерпанным (`_isDepleted`).

**Командный штраф** (фаза B, проверено):

- **Выход из `TrialBoundsZone`** любого игрока, который был внутри зоны, пока trial активен (падение в пропасть = выход из объёма зоны).
- **Истечение таймера** заряда (`ChargeTypeDefinition.durationSeconds > 0`).
- Последствия для **всех** подключённых игроков: снятие заряда, телепорт на `penaltyRespawnPoint` пилона, отмена trial (`Cancelled`), пилон снова доступен для **E** (без кулдауна).
- Игрок **без активного испытания**, упавший вне trial — **без** телепорта (см. `GDD.md`).

**Принятые решения:**

| Вопрос | Решение |
|--------|---------|
| Кто получает заряд при E | Только инициатор |
| Источник заряда на Stage 3 | Только пилон; отдельный `ChargeSource` — **Stage 4** |
| Связь пилона и финиша | Один ассет `TrialDefinition` на оба объекта |
| Штраф | Командный: проиграл один — сброс у всех |
| Dash по умолчанию | `CanDash = false` до награды с пилона |
| Промпт | Один `InteractionPromptView` / hints на игроке, якорь — `PromptAnchor` на пилоне |

---

## Статус по исходным задачам 3.1–3.5

| Задача | Исходное ТЗ | Статус в репозитории |
|--------|-------------|----------------------|
| **3.1** Данные заряда | `SO_ChargeType` | ✅ `ChargeTypeDefinition` + каталоги в `Configs/Charge/` |
| **3.2** Перманентные навыки | `PlayerAbilityUnlocker` + Event Bus | ✅ `PlayerPermanentModifiers` + применение с сервера из `TrialSessionRegistry`; ❌ отдельный `PlayerAbilityUnlocker` и канал `AbilityUnlocked` |
| **3.3** `ChargedState` в FSM | Отдельное состояние | ⚠️ Упрощённо: множители из `ChargeTypeDefinition` читает `NetworkPlayerController` при `ChargeId > 0`; отдельного `ChargedState` нет |
| **3.4** Синхронизация VFX | `NetworkVariable` + `PlayerVisuals` | ⚠️ `PlayerChargeController` реплицирует `byte` chargeId; визуал — заглушка `PlayerChargeVisualStub`, без сетевого VFX |
| **3.5** Штрафы и телепорт | Телепорт на чекпоинт при таймауте / выходе из зоны | ✅ `TrialBoundsZone`, `ApplyTeamTrialPenalty`, `NetworkPlayerController.TeleportTo`; ❌ отдельный `ChargeSource`, глобальный kill plane `Y < -10` |

### Дополнительно реализовано (вне старого текста 3.1–3.5)

| Система | Скрипты / ассеты |
|---------|------------------|
| Испытания (trials) | `TrialDefinition`, `TrialPylonStart`, `TrialFinishZone`, `TrialBoundsZone`, `TrialSessionRegistry`, `PlayerTrialInteractor`, `TrialPenaltyReason` |
| Взаимодействие | `IInteractable`, `PlayerInteractionController`, `InteractionPromptView`, `InteractionPromptSettings` |
| Подсветка контура (URP) | `InteractableHighlightStub`, `InteractableOutlineRendererFeature`, шейдер `Catsss/InteractableOutlineFeature` |
| Контент | `GameplayContentCatalog`, `Assets/Configs/Trials/`, префабы `PylonStartRoot`, `PylonEndRoot` |
| События (опционально) | `TrialProgressEventChannel` — фазы Started / Completed / Cancelled |
| Локализация | `LocalizedTextReference`, `UI_Strings` (RU/EN), editor setup |
| World hints | `WorldTextHintDefinition`, `WorldTextHintTrigger`, `WorldTextHintPresenter` |
| Guest connect UX | `ClientConnectInputValidator`, `MenuConnectionFeedback`, локализованные ошибки |

---

## Расширение UX: локализация и world hints (фаза A+)

Документы: **`Localization-And-WorldHints.md`**, **`MainMenu-And-Networking.md`**.

### Локализация

- Все статические тексты MainMenu — String Table **`UI_Strings`**.
- Промпт E и trial hints — те же ключи через `LocalizedTextReference`.
- Bootstrap: **Catsss → Localization → Setup UI Strings** + **Setup MainMenu Scene Texts**.

### World hints

- **Одна** world-space подсказка на `PlayerRoot` (owner).
- `WorldTextHintPresenter` выбирает текст по **priority**: interaction (10) vs trigger (100+).
- Пример после unlock Dash: `Hint_PressShiftAfterTrial.asset` + prefab `WorldTextHintTriggerRoot`.
- Show rule **`PlayerCanDashUnlocked`** — client-side, следит за репликацией `CanDash`.

### Guest connect

- Валидация IP/порта **до** UTP (см. `MainMenu-And-Networking.md`).
- Ошибки на GuestPanel без загрузки Sandbox при неверном вводе.

---

## Архитектура кода

### Данные (`Assets/Scripts/Configs/Charge/`)

- **`ChargeTypeDefinition`** — `id`, множители прыжка/скорости, `durationSeconds` (0 = до финиша/сброса), `suppressJump`, `isHeavy`, заглушки VFX.
- **`PermanentModifierDefinition`** + **`PermanentModifierKind`** (например разблокировка Dash).
- **`ChargeTypeCatalog`**, **`PermanentModifierCatalog`**, **`GameplayContentCatalog`** — резолв по `byte` id.

### Игрок

- **`PlayerChargeController`** — серверный источник правды: `NetworkVariable<byte>` chargeId, `NetworkVariable<int>` activeTrialId, таймер `CooldownTimer`; при истечении таймера → `ApplyTeamTrialPenalty`.
- **`PlayerPermanentModifiers`** — `CanDash` и др., репликация с сервера.
- **`NetworkPlayerController`** — гейт Dash по `CanDash`, множители движения от активного заряда, **`TeleportTo`** / `TeleportFromServerClientRpc` (client-auth, только owner).
- **`PlayerInteractionController`** — только `IsOwner`: поиск ближайшего `IInteractable` в радиусе (~3 м), line-of-sight, E → `Interact`.
- **`PlayerTrialInteractor`** — `[Rpc(SendTo.Server)]` старт испытания с пилона.

### Сцена

- Объект с **`TrialSessionRegistry`** (`NetworkObject`, `ServiceLocator.Register` в `Awake`), ссылки на `GameplayContentCatalog` и опционально `TrialProgressEventChannel`.
- **`TrialBoundsZone`** — trigger-коллайдер вокруг геометрии испытания (не обязательно на префабе пилона; часто отдельный объект в Sandbox).

### Сеть

- Старт испытания, финиш и **штраф** — **только сервер**.
- Телепорт: сервер вызывает `ClientRpc` на каждом `NetworkPlayerController`; позицию задаёт **владелец** (совместимо с `ClientNetworkTransform`).

---

## Настройка в Unity: стартовый пилон

### Иерархия `PylonStartRoot`

```
PylonStartRoot     NetworkObject, BoxCollider, TrialPylonStart
├── PylonStartVisual   MeshRenderer (Lit), InteractableHighlightStub
├── PromptAnchor       пустой Transform (якорь текста)
├── PenaltyRespawnPoint   чекпоинт командного штрафа (Transform)
└── SpawnPoint         опционально
```

- **`InteractableHighlightStub`** — на объекте с **MeshRenderer** (`PylonStartVisual`), поле Source Renderers заполнится в `Reset`.
- **`TrialPylonStart`**: Trial Definition, Prompt Anchor, Highlight Stub, **Penalty Respawn Point** (обязательно для штрафа).

### Зона испытания `TrialBoundsZone`

- Отдельный GameObject с **Collider Is Trigger** (обычно Box), скрипт **`TrialBoundsZone`**.
- Поля: тот же **`TrialDefinition`**, опционально **`Linked Pylon`** (для валидации trialId в редакторе).
- Коллайдер обнимает **игровые платформы**, без «хвоста» в пустоту под пропастью — иначе падение не вызовет `OnTriggerExit`.
- Gizmo в Scene view при выделении объекта (жёлтый wire/box).

### Иерархия финиша `PylonEndRoot`

- **`TrialFinishZone`**, **Collider isTrigger**, тот же **`TrialDefinition`**, что у старта.
- **`NetworkObject`** на корне.

### `TrialDefinition` (Create → Catsss/Trials/Trial Definition)

- Уникальный **Trial Id**
- **Charge Type** — временный заряд при старте
- **Completion Reward** — перманент (например Dash)

Один ассет — на **оба** префаба пары.

### PlayerRoot

- **`PlayerInteractionController`**: Interaction Range ~3, World Prompt → дочерний `InteractionPrompt` (Canvas **World Space**, scale ~0.01).
- **`PlayerTrialInteractor`**, **`PlayerChargeController`** (каталог), **`PlayerPermanentModifiers`**.

### Sandbox

- Экземпляр **`TrialSessionRegistry`** в сцене, в NetworkManager зарегистрированы префабы пилонов.

---

## Контур взаимодействия (URP, Unity 6)

Подход: **Rendering Layer** + **Renderer Feature** (Render Graph), без дубликата меша.

### 1. Имена слоёв

**Edit → Project Settings → Tags and Layers → Rendering Layers**

- Index **0**: `Default`
- Index **1**: `InteractableOutline` (переименовать бывший Light Layer 1)

Маска в коде: **`InteractableOutlineRendering.LayerMask = 2`** (bit 1).

### 2. Материал контура

- Shader: **`Catsss/InteractableOutlineFeature`**
- Пример ассета: `Assets/Materials/Mat_PylonOutline.mat` (в Renderer Feature может быть `Mat_InteractableOutlineURP`)
- **Outline Width**: начать с **0.015–0.025** (радиальное раздувание)
- Центр раздувания задаётся из **`InteractableHighlightStub`** через `MaterialPropertyBlock` (`_ExtrusionCenterOS` = `renderer.localBounds.center`) — иначе на боксе рвутся углы и «плывёт» верхняя грань.

### 3. Renderer Feature на `PC_Renderer`

| Поле | Значение |
|------|----------|
| Render Pass Event | `Before Rendering Opaques` |
| Outline Material | материал с шейдером выше |
| Rendering Layer Mask | **2** |

Скрипт: `Assets/Scripts/Rendering/InteractableOutlineRendererFeature.cs` — URP **17.4**, `RecordRenderGraph`, отбор рендереров по тегам **UniversalForward** + `overrideMaterial`.

### 4. Поведение при фокусе

`InteractableHighlightStub.SetHighlighted(true)` → `renderingLayerMask |= 2`.  
`MaterialPropertyBlock` создаётся **лениво** при первом включении (не в поле-инициализаторе MonoBehaviour).

### Отладка

- В Play вручную включить на Mesh Renderer только **InteractableOutline** — контур должен быть виден без фокуса.
- Если контура нет: проверить Feature на том же **Renderer Data**, что у камеры; порт **7777** не занят (иначе хост не поднимается).

---

## Исходное ТЗ (справочно, 3.1–3.5)

<details>
<summary>Текст задач из первоначального плана этапа</summary>

**3.1** — SO заряда: duration, jump/speed multipliers, isHeavy, vfxPrefab, lightColor.

**3.2** — maxJumps, canDash в контроллере; FSM: Dash только при canDash; `PlayerAbilityUnlocker` + `AbilityUnlockedEvent`.

**3.3** — `ChargedState`, локальный таймер сброса.

**3.4** — `NetworkVariable<byte> CurrentChargeID`, спавн VFX в `PlayerVisuals`.

**3.5** — префаб `ChargeSource`, ServerRpc выдачи; телепорт на чекпоинт при таймауте / Y &lt; -10.

</details>

---

## Дорожная карта после MVP

| Фаза | Содержание | Статус |
|------|------------|--------|
| **A** | Пилон → заряд → финиш → перманент всем, UX (промпт, контур) | ✅ |
| **A+** | Локализация RU/EN, world hints, guest connect validation | ✅ |
| **B** | `TrialBoundsZone`, командный штраф, телепорт на `penaltyRespawnPoint`, отмена trial | ✅ |
| **C** | UI таймера заряда, сетевой VFX заряда | ❌ |
| **Stage 4** | Бросок заряда, Aim, homing | ✅ см. `Stage 4.md` |
| **Stage 5** | Level kit (печати, платформы, антимаг, ветер) | ✅ см. `Stage 5.md` |
| **Stage 7** | Сюжетные пилоны-ловушки, `GameFlowManager`, погоня мыши | ❌ |

См. также: `Stage 7.md` (задача 7.1), `GDD.md` (прогрессия через пилоны), `Development-Status.md`, `Architecture-Snapshot.md`, `Localization-And-WorldHints.md`, `MainMenu-And-Networking.md`.

---

*Последнее обновление: Stage 3–5 закрыты для альфы (MVP). Следующий фокус — **Stage 6 / Stage 7**.*
