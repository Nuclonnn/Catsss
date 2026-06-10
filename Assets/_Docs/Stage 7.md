# ЭТАП 7: GameFlowManager и финальный act уровня

**Статус:** ❌ Не начат. **Предусловие:** Stage 6 ✅ (мышь-актёр: маршруты, cue, trial-реакции, купол, поимка, polish).

**Цель:** Связать Trials, мышь и кульминацию уровня в один сценарий через `GameFlowManager`. Провести сквозной vertical slice альфа-уровня.

**Документы:** `Stage 3.md` (Trials), `Stage 6.md` (мышь), `GDD.md`.

---

## Связь со Stage 6

| Было в Stage 6 | Куда переехало |
|----------------|----------------|
| **6.6** Финальная погоня + rubberbanding | **Stage 7.2** — запуск через `GameFlowManager` |
| **6.7** Купол + поимка | **Stage 6** ✅ — механика мыши (`MouseDomeZone`, flee, `MouseCaughtChannel`) |
| Победа / slow-mo / экран | **Stage 7.4** — реакция `GameFlowManager` на `MouseCaughtChannel` |

Stage 6 закрывает **инструменты и финальную поимку мыши**. Stage 7 закрывает **режissуру уровня**: когда начинается погоня, музыка, победа.

---

## Фазы альфа-уровня (целевой flow)

```text
1. Exploration     — мышь Hidden / короткие сценки (MouseCueTrigger, MouseTrialReaction)
2. Trial 1         — Started/Completed реакции мыши (6.5), саботаж (6.4)
3. Trial 2         — то же
4. Chase (6.6→7.2) — GameFlowManager запускает финальный MouseRoute + rubberbanding
5. Dome (6.7)      — EnterDome → flee → catch → MouseCaughtChannel
6. Victory (7.4)   — GameFlowManager → экран победы
```

---

## Задача 7.1: Trials и прогресс (уточнение)

**Не дублировать Stage 3.** Trial-пилоны, заряд, финиш и перманент уже работают.

Для Stage 7 достаточно:
- слушать **`TrialProgressEventChannel`** (Completed) или считать завершённые trials на сервере;
- опционально: кооп-действие «сыр» (`MagicSeal` + E оба игрока) как триггер погони — настраивается в сцене.

Старые «сюжетные пилоны-ловушки» из раннего ТЗ **не используются** в альфе — прогресс = **2 Trial Completed**.

---

## Задача 7.2: GameFlowManager + финальная погоня (бывшая 6.6)

Создать **`GameFlowManager`** (`NetworkBehaviour`, server-only, `ServiceLocator`).

### Фазы менеджера

| Фаза | Условие входа |
|------|----------------|
| `Exploration` | старт сцены |
| `Chase` | оба Trial Completed (+ опционально кооп-сыр) |
| `Victory` | `MouseCaughtChannel` |

### Обязанности

- Подписка на **`TrialProgressEventChannel`** — считать `completedTrials`; при `>= trialsRequiredForChase` (2) → `StartChasePhaseServer()`.
- **`StartChasePhaseServer()`:**
  - `mouse.PlayRouteServer(finalChaseRoute, teleportToRouteStart: true)`;
  - маршрут с **`End Mode = EnterDome`**;
  - **rubberbanding** (перенос из отложенной 6.6): в `RouteFollowMouseState` или флаг `MouseRoute.enableRubberbanding` — замедление, если игрок далеко;
  - `ClientRpc` / event для смены музыки (заглушка OK).
- Не запускать погоню повторно (`playOnce`).

### Rubberbanding (из 6.6)

- параметры из `MouseConfig`: `rubberbandFarDistance`, `rubberbandSlowMultiplier`, `rubberbandNearDistance`;
- применять только на финальном chase-маршруте (флаг на route или вызов из GameFlowManager).

---

## Задача 7.3: Перманентные навыки

**Уже в Stage 3:** `TrialSessionRegistry` → `PlayerPermanentModifiers` при CompleteTrial.

Stage 7 **не дублирует** `AbilityUnlockedChannel`, если Trials уже выдают Dash / DoubleJump.

Опционально: world hint или сценка мыши после Completed — через `MouseTrialReaction` (6.5).

---

## Задача 7.4: Победа

- `GameFlowManager` подписывается на **`MouseCaughtChannel`** (Stage 6.7).
- Сервер:
  - фаза → `Victory`;
  - остановка chase-таймеров;
  - `ClientRpc_ShowVictoryScreen()` (заглушка UI OK);
  - опционально slow-mo на клиентах (не `timeScale` на server).

---

## Задача 7.5: Сквозной тест альфы

Чеклист:
- [ ] Host + Guest: Trial 1 → Trial 2 → погоня → купол → поимка → победа
- [ ] Мышь не ловится до купola (Spectral на chase)
- [ ] Rubberbanding не теряет мышь из гонки
- [ ] `MouseCaughtChannel` → GameFlowManager

---

## Настройка в Unity (после реализации 7.2–7.4)

1. Объект **`GameFlowManager`** в Sandbox, `NetworkObject`, ServiceLocator.
2. Ссылки: `MouseBrain`, `MouseRoute_FinalChase`, `TrialProgressEventChannel`, `MouseCaughtChannel`.
3. **`trialsRequiredForChase`** = 2.
4. Финальный маршрут: верхний ярус, **End Mode = EnterDome**, rubberbanding включён.
5. **MouseDomeZone** + NavMesh bake в куполе (см. Stage 6.7).

---

*Stage 7 стартует после закрытия Stage 6.7.*
