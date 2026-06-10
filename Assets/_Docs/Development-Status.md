# Catsss — текущее состояние разработки

**Живой документ** для отслеживания того, что уже в репозитории, что воспроизводимо в редакторе, и что из официальных этапов (`Stage 1–7`) ещё не закрыто.

| Поле | Значение |
|------|----------|
| **Текущий этап** | **Stage 6** ✅ MVP закрыт → следующий: **Stage 7** |
| **Закрыто недавно** | **Stage 6** ✅ — мышь-актёр: маршруты, cue, trial-реакции, купол, поимка, polish |
| **Цель альфы** | Vertical slice одного уровня, 2 игрока по сети |
| **Точка входа** | Сцена `MainMenu` → `Sandbox` |
| **Последнее обновление** | Июнь 2026 — Stage 6 MVP + polish |

**Ветки Git (рекомендация):** feature-ветка (`NewBranch` и т.п.) для Stage 7; merge в `main` после проверки сцены.

---

## Сводка: что работает сейчас

| Область | Статус | Документ |
|---------|--------|----------|
| Архитектурное ядро (FSM, Events, ServiceLocator, Timing) | ✅ Stage 1 | `Architecture-Snapshot.md`, `Stage 1.md` |
| NGO + UTP (IP/порт, без Relay) | ✅ | `MainMenu-And-Networking.md`, `Stage 2.md` |
| Движение игрока (ходьба, прыжок, dash, камера) | ✅ MVP | ниже |
| Испытания: пилон → заряд → финиш → перманент | ✅ Stage 3 | `Stage 3.md` |
| Штрафы испытания (bounds, таймер, командный телепорт) | ✅ | `Stage 3.md` |
| Взаимодействие E + URP-контур | ✅ MVP | `Stage 3.md` |
| Локализация RU/EN | ✅ | `Localization-And-WorldHints.md` |
| World-space подсказки | ✅ MVP | `Localization-And-WorldHints.md` |
| Guest connect с валидацией | ✅ | `MainMenu-And-Networking.md` |
| **Бросок заряда, Aim, homing, попытки** | ✅ Stage 4 | `Stage 4.md` |
| **Level kit: MagicSeal, KinematicPlatform, AntiMagic, AeroZone** | ✅ Stage 5 | `Stage 5.md` |
| **Мышь: маршруты, cue, trial-реакции, купол, поимка** | ✅ Stage 6 | `Stage 6.md` |
| **GameFlowManager, финальная погоня, победа** | ❌ Stage 7 | `Stage 7.md` |

---

## Этап 6 — мышь-саботажник (закрыт MVP)

Подробно: **`Stage 6.md`**.

**Цель:** сценическая мышь вместо свободного NavMesh-AI. Дизайнер собирает поведение из маршрутов, триггеров и EventChannel.

| Подсистема | Ключевые скрипты |
|------------|------------------|
| 6.1 Сетевой prefab | `MouseBrain`, `MouseVisualStub`, `MouseConfig`, `ServerNetworkTransform` |
| 6.2 Маршруты | `MouseRoute`, `MouseRouteWaypointEvent`, `WaypointPathFollower` |
| 6.3 Cue | `MouseCueTrigger`, `MouseCueCountRequirement` |
| 6.4 Саботаж | waypoint → `EmptyEventChannel` → Stage 5 drivers |
| 6.5 Trials | `MouseTrialReaction`, `MouseTrialReactionBinding` |
| 6.6 Погоня | **→ Stage 7.2** (rubberbanding в config, логика не подключена) |
| 6.7 Купол | `MouseDomeZone`, DomeFlee FSM, `MouseCaughtChannel` |
| Dev/Polish | `MouseDebugBootstrap`, `MouseCaughtListener`, fade, route gizmos |

**Связка:** `MouseCueTrigger` / `MouseTrialReaction` → `MouseBrain.PlayRouteServer` → LevelKit через channels.

**Prefabs:** `Prefabs/Mouse/MouseRoot`, `PathMouseRoot`, `MouseCueTrigger`, `MouseTrialReactions`, `MouseBurrows`.

---

## Этап 4 — бросок заряда (кратко, закрыт)

Подробно: **`Stage 4.md`**.

**Цикл:** ПКМ Aim → луч прицела (`PlayerThrowDirectionResolver`) → ЛКМ → `ChargeProjectile` (server homing) → catch / miss / лимит попыток → telegraph на ловце.

| Компонент | Назначение |
|-----------|------------|
| `PlayerAimController` | toggle Aim, throw RPC |
| `ChargeProjectile` | server flight, catch/miss |
| `TrialSessionRegistry` | попытки на trial |
| `PlayerIncomingChargeIndicator` | сетевой флаг «летит в меня» |

**Не сделано в Stage 4:** отдельная Aim-камера (4.6), полноценный VFX/AUDIO telegraph.

---

## Этап 5 — level kit (закрыт MVP)

Подробно: **`Stage 5.md`**.

| Подсистема | Ключевые скрипты |
|------------|------------------|
| 5.1 MagicSeal | `MagicSeal`, `MagicSealInteractable`, `MagicSealTriggerActivator` |
| 5.2 KinematicPlatform | `KinematicPlatform`, `KinematicPlatformSignalDriver`, `MovingPlatformRider` |
| 5.3 AntiMagicZone | `AntiMagicZone`, hook в `ChargeProjectile`, `TrialPenaltyReason.AntiMagicZone` |
| 5.4 AeroZone | `AeroZone`, `AeroZoneReceiver`, `IsAir` в `ChargeTypeDefinition` |

**Связка:** MagicSeal / Mouse waypoint → `EmptyEventChannel` → signal drivers.

---

## Главное меню и сеть

См. **`MainMenu-And-Networking.md`** — Host/Guest, UTP, валидация IP, `MenuConnectionFeedback`.

---

## Этап 3 — испытания (кратко)

Подробно: **`Stage 3.md`**.

**Цикл:** E у пилона → заряд инициатору → финиш → перманент **всем** → пилон depleted.

**Штраф:** выход из `TrialBoundsZone` или таймер заряда → сброс trial, телепорт **всех** на `PenaltyRespawnPoint`.

---

## Конфигурация

| Ассет | Содержимое |
|-------|------------|
| `Configs/GameConfig.asset` | Движение, физика, **Projectile** (Aim/полёт) |
| `Configs/Trials/`, `Configs/AllBaffs/` | Испытания, типы зарядов (`IsHeavy`, `IsAir`) |
| `Configs/WorldHints/` | World hints |
| `Configs/Mouse/` | `MouseConfig` — слои, route/dome/rubberband |

---

## Сцены и префабы

| Ассет | Назначение |
|-------|------------|
| `Scenes/MainMenu.unity` | Вход |
| `Scenes/Sandbox.unity` | Песочница NGO + trials + throw + level kit + mouse |
| `Prefabs/PlayerRoot.prefab` | Игрок (Aim, charge, telegraph, `AeroZoneReceiver`) |
| `Prefabs/ChargeProjectileRoot.prefab` | Снаряд (Network Prefabs) |
| `Prefabs/PylonStartRoot` / `PylonEndRoot` | Испытания |
| `Prefabs/LevelKit/*` | MagicSeal, KinematicPlatform, AntiMagic, AeroZone |
| `Prefabs/Mouse/*` | MouseRoot, PathMouseRoot, Cue, TrialReactions, Burrows |
| `Prefabs/ButtonInteractibleRoot` / `ButtonTriggerRoot` | Печати (примеры) |

---

## Технический долг

| Тема | Комментарий |
|------|-------------|
| Relay / UGS | Не интегрирован |
| Визуал игрока | `NetworkPlayerEventsRelay` — скелет без Animator |
| UI таймера заряда | Не сделан |
| Level kit / mouse VFX | Stub-материалы; particles/mesh — позже |
| Rubberbanding chase | Параметры в MouseConfig; логика — Stage 7.2 |
| Victory flow | `MouseCaughtChannel` есть; UI — Stage 7.4 |

---

## Карта документации `_Docs/`

| Файл | Назначение |
|------|------------|
| `Development-Status.md` | **Этот файл** |
| `Stage 4.md` / `Stage 5.md` / `Stage 6.md` | Закрытые этапы (MVP) |
| `Stage 7.md` | Следующий этап |
| `Onboarding.md` | Вход для нового участника |
| `Architecture-Snapshot.md` | Код и договоры |
| `DevLog.md` | Хронология |
| `Stage 1.md` … `Stage 7.md` | Задачи по этапам |

---

*Stage 6 MVP закрыт. Следующий фокус — **Stage 7** (GameFlowManager, chase, victory).*
