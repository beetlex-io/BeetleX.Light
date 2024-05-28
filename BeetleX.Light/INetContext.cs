using BeetleX.Light.Logs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX.Light
{
    public interface INetContext:IGetLogHandler
    {
        void Close(Exception e);
    }
}
