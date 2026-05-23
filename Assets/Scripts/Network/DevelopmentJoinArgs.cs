using System;
using UnityEngine;

namespace Catsss.Network
{
    /// <summary>
    /// Разовая проверка argv для dev-клиента (-join / -client).
    /// </summary>
    public static class DevelopmentJoinArgs
    {
        public static bool WantsClientFromCommandLine()
        {
            try
            {
                foreach (string arg in Environment.GetCommandLineArgs())
                {
                    if (arg.Equals("-join", StringComparison.OrdinalIgnoreCase) ||
                        arg.Equals("-client", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"DevelopmentJoinArgs: {ex.Message}");
            }

            return false;
        }
    }
}
