using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX.Light
{
    public interface IContextClose
    {
        void Close(Exception e);
    }
}
