using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX.Light
{
    public class UnixSocketUri
    {
        public bool IsUnixSocket { get; set; }
        public string SockFile { get; set; }
        public string PathAndQuery { get; set; }
        public string Protocol { get; set; }

        public static UnixSocketUri GetUnixSocketUrl(string host)
        {
            UnixSocketUri result = new UnixSocketUri();
            result.IsUnixSocket = false;
            if (string.IsNullOrEmpty(host))
                return result;
            var index = host.IndexOf(".sock", StringComparison.OrdinalIgnoreCase);
            if (index > 0)
            {
                var values = host.Split(':');
                if (values.Length > 1)
                {
                    result.Protocol = values[0].ToLower();
                    host = values[1];
                    index = host.IndexOf(".sock", StringComparison.OrdinalIgnoreCase);
                }
                result.IsUnixSocket = true;
                if (index + 5 == host.Length)
                {
                    result.SockFile = host;
                }
                else
                {
                    result.SockFile = host.Substring(0, index + 5);
                    result.PathAndQuery = host.Substring(index + 5);
                }
            }
            return result;
        }
    }
}
