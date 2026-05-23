using System.Collections.Generic;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace Catsss.Menu
{
    /// <summary>
    /// Список возможных IPv4 машины-хоста, чтобы второй игрок ввёл тот же адрес в режиме гостя (Hamachi добавит свой интерфейс).
    /// </summary>
    public static class NetworkLocalAddressHints
    {
        public static string SummarizeLanIpv4()
        {
            string csv = BuildLanIpv4Csv();
            return string.IsNullOrEmpty(csv) ? string.Empty : csv;
        }

        /// <summary>Список IPv4 через запятую или пустая строка.</summary>
        public static string BuildLanIpv4Csv()
        {
            var seen = new HashSet<string>();
            var sb = new StringBuilder();

            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                {
                    continue;
                }

                if (ni.OperationalStatus != OperationalStatus.Up)
                {
                    continue;
                }

                IPInterfaceProperties ipProps = ni.GetIPProperties();
                foreach (UnicastIPAddressInformation ip in ipProps.UnicastAddresses)
                {
                    if (ip.Address.AddressFamily != AddressFamily.InterNetwork)
                    {
                        continue;
                    }

                    string s = ip.Address.ToString();
                    if (s.StartsWith("169."))
                    {
                        continue;
                    }

                    if (!seen.Add(s))
                    {
                        continue;
                    }

                    if (sb.Length > 0)
                    {
                        sb.Append(", ");
                    }

                    sb.Append(s);

                    if (seen.Count >= 6)
                    {
                        return sb.ToString();
                    }
                }
            }

            return sb.ToString();
        }
    }
}
