# Catsss — текущее состояние разработки

**Живой документ** для отслеживания того, что уже в репозитории, что воспроизводимо в редакторе, и что из официальных этапов (`Stage 1–7`) ещё не закрыто.

| Поле | Значение |
|------|----------|
| **Текущий этап** | **Stage 5** ✅ MVP закрыт → следующий: **Stage 6 / Stage 7** |
| **Закрыто недавно** | **Stage 5** ✅ — level kit (печати, платформы, антимаг, ветер) |
| **Цель альфы** | Vertical slice одного уровня, 2 игрока по сети |
| **Точка входа** | Сцена `MainMenu` → `Sandbox` |
| **Последнее обновление** | Май 2026 — Stage 5 MVP |

**Ветки Git (рекомендация):** **`Stage-5`** — level kit; после merge в `main` — ветка для Stage 6/7.

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
| **Level kit: MagicSeal** | ✅ Stage 5 | `Stage 5.md` |
| **Level kit: KinematicPlatform + rider** | ✅ Stage 5 | `Stage 5.md` |
| **Level kit: AntiMagicZone** | ✅ Stage 5 | `Stage 5.md` |
| **Level kit: AeroZone (ветер)** | ✅ Stage 5 | `Stage 5.md` |
| Сюжетные пилоны-ловушки (мышь) | ❌ Stage 7 | `Stage 7.md` |

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

**Цель:** универсальные префабы уровня — печати, кинематические платформы, антимаг-зоны, аэрозоны.

| Подсистема | Ключевые скрипты |
|------------|------------------|
| 5.1 MagicSeal | `MagicSeal`, `MagicSealInteractable`, `MagicSealTriggerActivator` |
| 5.2 KinematicPlatform | `KinematicPlatform`, `KinematicPlatformSignalDriver`, `MovingPlatformRider` |
| 5.3 AntiMagicZone | `AntiMagicZone`, hook в `ChargeProjectile`, `TrialPenaltyReason.AntiMagicZone` |
| 5.4 AeroZone | `AeroZone`, `AeroZoneReceiver`, `IsAir` в `ChargeTypeDefinition` |

**Связка:** MagicSeal → `EmptyEventChannel` → signal drivers платформ / зон.

**Опционально (5.5):** отдельная sandbox-комната level kit, polish префабов.

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

---

## Сцены и префабы

| Ассет | Назначение |
|-------|------------|
| `Scenes/MainMenu.unity` | Вход |
| `Scenes/Sandbox.unity` | Песочница NGO + trials + throw + level kit |
| `Prefabs/PlayerRoot.prefab` | Игрок (Aim, charge, telegraph, `AeroZoneReceiver`) |
| `Prefabs/ChargeProjectileRoot.prefab` | Снаряд (Network Prefabs) |
| `Prefabs/PylonStartRoot` / `PylonEndRoot` | Испытания |
| `Prefabs/LevelKit/*` | MagicSeal, KinematicPlatform, AntiMagic, AeroZone — настраиваются в Unity |
| `Prefabs/ButtonInteractibleRoot` / `ButtonTriggerRoot` | Печати (примеры) |

---

## Технический долг

| Тема | Комментарий |
|------|-------------|
| Relay / UGS | Не интегрирован |
| Визуал игрока | `NetworkPlayerEventsRelay` — скелет без Animator |
| UI таймера заряда | Не сделан |
| Level kit VFX | Stub-материалы; particles/mesh — позже |

---

## Карта документации `_Docs/`

| Файл | Назначение |
|------|------------|
| `Development-Status.md` | **Этот файл** |
| `Stage 4.md` / `Stage 5.md` | Закрытые этапы (MVP) |
| `Stage 6.md` / `Stage 7.md` | Следующие этапы |
| `Onboarding.md` | Вход для нового участника |
| `Architecture-Snapshot.md` | Код и договоры |
| `DevLog.md` | Хронология |
| `Stage 1.md` … `Stage 7.md` | Задачи по этапам |

---

*Stage 5 MVP закрыт. Следующий фокус — **Stage 6** или **Stage 7** по roadmap.*
