using System.Collections;
using Catsss.Network;
using UnityEngine;

namespace Catsss.Menu.Flow.Services
{
    /// <summary>Контракт для coroutine-сервисов flow: FSM-переходы и session flags.</summary>
    internal interface IAppFlowRoutineHost
    {
        void EnterInGameState();

        void EnterReturningState();

        void EnterMainMenuState();

        void SetSessionStartupHandled(bool handled);
    }
}
