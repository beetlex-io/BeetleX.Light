using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX.Light.Logs
{
    public interface ILocation
    {
        EndPoint EndPoint { get; set; }
    }
}
