using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX.Light.Logs
{
    public enum LogLevel : int
    {
        All = 0,
        Trace = 1,
        Debug = 2,
        Info = 4,
        Warring = 8,
        Error = 16,
        Fatal = 32,
        Off = 64
    }
}
