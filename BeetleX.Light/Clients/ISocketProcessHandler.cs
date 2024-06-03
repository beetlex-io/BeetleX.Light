using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX.Light.Clients
{
    public interface ISocketProcessHandler
    {
        void SendCompeted(NetClient client,Memory<byte> data,int completed);

        void ReceiveCompeted(NetClient client, Memory<byte> data, int completed);
    }
}
