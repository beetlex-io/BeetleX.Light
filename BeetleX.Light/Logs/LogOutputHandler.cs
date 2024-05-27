using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX.Light.Logs
{
    public interface ILogOutputHandler
    {
        void Write(LogRecord log);

        void Flush();
    }
}
