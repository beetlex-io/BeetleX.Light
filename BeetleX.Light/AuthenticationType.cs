using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX.Light
{
    public enum AuthenticationType : int
    {
        None = 1,

        Connected = 2,

        User = 4,

        Admin = 8,

        Security = 16

    }
}
