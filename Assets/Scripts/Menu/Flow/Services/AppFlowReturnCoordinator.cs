using Catsss.Core.Services;

using Catsss.Menu.Flow.ReturnFeedback;

using Catsss.Trials;

using Unity.Netcode;

using UnityEngine;



namespace Catsss.Menu.Flow.Services

{

    /// <summary>Feedback при возврате в меню и уведомление remote-клиентов перед shutdown хоста.</summary>

    internal static class AppFlowReturnCoordinator

    {

        public static void ApplyReturnFeedback(MenuReturnReason reason, in MenuReturnFeedbackContext context = default)

        {

            AppFlowReturnFeedbackRegistry.Apply(reason, in context);

        }



        public static void TryNotifyRemoteClientsBeforeLocalShutdown(MenuReturnReason localReason)

        {

            NetworkManager networkManager = NetworkManager.Singleton;



            if (networkManager == null || !networkManager.IsHost)

            {

                return;

            }



            MenuReturnReason remoteReason = localReason == MenuReturnReason.UserLeftSession

                ? MenuReturnReason.SessionEnded

                : localReason;



            if (ServiceLocator.TryGet(out TrialSessionRegistry registry))

            {

                registry.NotifyRemoteClientsSessionEndingServer(remoteReason);

                return;

            }



            TrialSessionRegistry found = Object.FindAnyObjectByType<TrialSessionRegistry>();



            if (found != null)

            {

                found.NotifyRemoteClientsSessionEndingServer(remoteReason);

            }

        }

    }

}


