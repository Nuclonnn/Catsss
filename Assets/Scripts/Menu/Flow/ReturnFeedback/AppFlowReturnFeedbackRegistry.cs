using System.Collections.Generic;

namespace Catsss.Menu.Flow.ReturnFeedback
{
    /// <summary>Реестр handler'ов return-feedback. Итерация по копии — как EventChannel.</summary>
    internal static class AppFlowReturnFeedbackRegistry
    {
        private static readonly IReturnFeedbackHandler[] Handlers =
        {
            new GuestConnectionFailedReturnFeedbackHandler(),
            new GuestInputErrorReturnFeedbackHandler(),
            new HostDisconnectedReturnFeedbackHandler(),
            new SessionEndedReturnFeedbackHandler(),
            new SceneLoadFailedReturnFeedbackHandler(),
        };

        private static readonly Dictionary<MenuReturnReason, IReturnFeedbackHandler> HandlerByReason = BuildLookup();

        public static void Apply(MenuReturnReason reason, in MenuReturnFeedbackContext context = default)
        {
            if (reason == MenuReturnReason.None || reason == MenuReturnReason.UserLeftSession)
            {
                return;
            }

            if (HandlerByReason.TryGetValue(reason, out IReturnFeedbackHandler handler))
            {
                handler.Apply(in context);
            }
        }

        private static Dictionary<MenuReturnReason, IReturnFeedbackHandler> BuildLookup()
        {
            var lookup = new Dictionary<MenuReturnReason, IReturnFeedbackHandler>(Handlers.Length);

            foreach (IReturnFeedbackHandler handler in Handlers)
            {
                lookup[handler.Reason] = handler;
            }

            return lookup;
        }
    }
}
