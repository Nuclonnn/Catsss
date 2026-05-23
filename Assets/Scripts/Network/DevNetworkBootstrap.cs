using System;
using System.Collections;
using Catsss.Network;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Дев-запуск NGO: хост или клиент. В билде режим <see cref="LaunchRole.Automatic"/> даёт клиента при аргументах -join / -client.
/// Один ПК: редактор = хост, второй экземпляр (exe) с флагом -join = второй игрок.
/// </summary>
public sealed class DevNetworkBootstrap : MonoBehaviour
{
    public enum LaunchRole
    {
        Host,
        Client,

        /// <summary>
        /// Редактор — как <see cref="editorRoleWhenAutomatic"/>. Standalone: клиент только если в командной строке есть -join или -client, иначе хост.
        /// </summary>
        Automatic
    }

    [FormerlySerializedAs("startHostOnPlay")]
    [SerializeField]
    private bool autoStartOnPlay = true;

    [SerializeField]
    private LaunchRole launchRole = LaunchRole.Host;

    [Tooltip("Используется только при LaunchRole = Automatic во время Play в редакторе.")]
    [SerializeField]
    private LaunchRole editorRoleWhenAutomatic = LaunchRole.Host;

    [SerializeField, Min(0f)]
    private float clientStartDelaySeconds = 0.35f;

    private void Start()
    {
        if (!autoStartOnPlay)
        {
            return;
        }

        if (FindAnyObjectByType<GameplayNetworkSessionStarter>() != null)
        {
            return;
        }

        ConnectionManager connection = FindAnyObjectByType<ConnectionManager>();
        if (connection == null)
        {
            Debug.LogError("DevNetworkBootstrap: ConnectionManager не найден на сцене.");
            return;
        }

        LaunchRole resolved = ResolveLaunchRole();
        switch (resolved)
        {
            case LaunchRole.Host:
                connection.StartHost();
                Debug.Log("DevNetworkBootstrap: StartHost().");
                break;
            case LaunchRole.Client:
                StartCoroutine(StartClientDelayed(connection));
                break;
            default:
                Debug.LogError($"DevNetworkBootstrap: неожиданная роль {resolved}.");
                break;
        }
    }

    private LaunchRole ResolveLaunchRole()
    {
        if (launchRole == LaunchRole.Host || launchRole == LaunchRole.Client)
        {
            return launchRole;
        }

        if (Application.isEditor)
        {
            return editorRoleWhenAutomatic;
        }

        return HasJoinCommandLineArg() ? LaunchRole.Client : LaunchRole.Host;
    }

    private static bool HasJoinCommandLineArg()
    {
        return DevelopmentJoinArgs.WantsClientFromCommandLine();
    }

    private IEnumerator StartClientDelayed(ConnectionManager connection)
    {
        if (clientStartDelaySeconds > 0f)
        {
            yield return new WaitForSeconds(clientStartDelaySeconds);
        }

        connection.StartClient();
        Debug.Log("DevNetworkBootstrap: StartClient().");
    }
}
