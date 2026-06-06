**Статус:** ✅ Закрыт (NGO, client-auth движение, RPC-визуал). Меню/гость — `MainMenu-And-Networking.md`.

ЭТАП 2: Базовый сетевой Игрок (Networked Locomotion)
Цель: Настроить Netcode for GameObjects (NGO), собрать префаб игрока и реализовать Client-Authoritative передвижение с ручной синхронизацией визуала. Работаем в сцене Scene_Gym.
Задача 2.1: Инициализация сети (Netcode + Relay)
•	Настрой компонент NetworkManager на сцене.
•	Напиши ConnectionManager (зарегистрируй в ServiceLocator). Скрипт должен уметь создавать Host-сессию через Unity Relay и подключаться к ней по генерируемому 6-значному Join Code.
Задача 2.2: Структура префаба игрока
Строго раздели компоненты по иерархии:
•	Player_Root: Здесь висят NetworkObject, скрипт из NGO Samples ClientNetworkTransform (для управления без задержек), PlayerController и PlayerNetwork.
•	Visuals_Container (Child): Здесь висят 3D-модель, Animator и скрипт PlayerVisuals.
Задача 2.3: Логика управления (FSM)
•	Напиши PlayerController, использующий машину состояний из Этапа 1.
•	Реализуй состояния LocomotionState, JumpState, DashState, который понадобится позже.
•	Все параметры физики (скорость, высота прыжка, кастомная гравитация, ускорение после рывка) читай из SO_GameConfig.
•	Ограничение: В Update() добавь проверку: если !IsOwner — инпуты и FSM не работают. Движением "чужого" игрока управляет только ClientNetworkTransform.
Задача 2.4: Архитектура Визуала и RPC-синхронизация
Мы не используем встроенный NetworkAnimator. Триггеры синхронизируем вручную:
•	Когда локальная физика совершает прыжок, PlayerController вызывает C# Action OnJumped.
•	Скрипт PlayerNetwork подписывается на OnJumped. При срабатывании он отправляет ServerRpc_SendJump. Сервер мгновенно рассылает ClientRpc_ReceiveJump всем клиентам (кроме отправителя).
•	Скрипт PlayerVisuals подписывается на локальный OnJumped (для себя) и на событие из PlayerNetwork (для клонов по сети). При получении сигнала — дергает Аниматор.
