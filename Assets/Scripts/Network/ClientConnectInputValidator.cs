using System;
using System.Linq;

namespace Catsss.Network
{
    public enum ClientConnectInputError : byte
    {
        None = 0,
        EmptyHost = 1,
        InvalidHost = 2,
        InvalidPort = 3,
    }

    /// <summary>
    /// Проверка IP/hostname/порта до вызова Unity Transport (UTP).
    /// </summary>
    public static class ClientConnectInputValidator
    {
        public static bool TryValidate(string hostRaw, ushort port, out ClientConnectInputError error)
        {
            error = ClientConnectInputError.None;
            string host = hostRaw?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(host))
            {
                error = ClientConnectInputError.EmptyHost;
                return false;
            }

            if (host.Length > 253)
            {
                error = ClientConnectInputError.InvalidHost;
                return false;
            }

            if (port == 0)
            {
                error = ClientConnectInputError.InvalidPort;
                return false;
            }

            if (IsLocalhostAlias(host) || IsValidIpv4(host) || IsValidHostname(host))
            {
                return true;
            }

            error = ClientConnectInputError.InvalidHost;
            return false;
        }

        public static bool TryParsePort(string portRaw, out ushort port, out ClientConnectInputError error)
        {
            error = ClientConnectInputError.None;
            port = 0;

            if (string.IsNullOrWhiteSpace(portRaw))
            {
                error = ClientConnectInputError.InvalidPort;
                return false;
            }

            if (!ushort.TryParse(portRaw.Trim(), out port) || port == 0)
            {
                error = ClientConnectInputError.InvalidPort;
                return false;
            }

            return true;
        }

        private static bool IsLocalhostAlias(string host)
        {
            return host.Equals("localhost", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsValidIpv4(string host)
        {
            string[] parts = host.Split('.');

            if (parts.Length != 4)
            {
                return false;
            }

            foreach (string part in parts)
            {
                if (part.Length == 0 || part.Length > 3)
                {
                    return false;
                }

                if (!int.TryParse(part, out int octet) || octet < 0 || octet > 255)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>Hostname с буквами; чисто числовые строки вроде «111» UTP не принимает.</summary>
        private static bool IsValidHostname(string host)
        {
            if (!host.Any(char.IsLetter))
            {
                return false;
            }

            if (host.StartsWith('.') || host.EndsWith('.') || host.Contains(".."))
            {
                return false;
            }

            string[] labels = host.Split('.');

            foreach (string label in labels)
            {
                if (label.Length == 0 || label.Length > 63)
                {
                    return false;
                }

                if (!char.IsLetterOrDigit(label[0]) || !char.IsLetterOrDigit(label[^1]))
                {
                    return false;
                }

                for (int i = 1; i < label.Length - 1; i++)
                {
                    char c = label[i];

                    if (!(char.IsLetterOrDigit(c) || c == '-'))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
