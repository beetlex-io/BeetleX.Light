using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX.Light.Args
{
    public class StartArgs
    {
        [Option("-host", Required = false)]
        public string Host { get; set; }

        [Option("-port", Required = false)]
        public int Port { get; set; }


        [Option("-sslfile", Required = false)]
        public string SSLFile { get; set; }


        [Option("-sslpwd", Required = false)]
        public string SSLPassWord { get; set; }

    }
}
