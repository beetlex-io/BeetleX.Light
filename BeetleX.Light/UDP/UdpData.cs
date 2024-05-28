using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX.Light.UDP
{
    public class UdpData
    {
        public EndPoint RemoteEndPoint { get; set; }

        public Memory<byte> Memory { get; internal set; }

        internal IMemoryOwner<byte> MemoryOwner { get; set; }

        public object Message { get; set; }

        internal UdpServer Server { get; set; }

        public void Reply(object message)
        {
            Server.Send(message, RemoteEndPoint);
        }
    }
}
