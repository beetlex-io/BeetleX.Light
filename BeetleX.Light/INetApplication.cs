using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX.Light
{
    public interface IApplication
    {
        void Started(INetServer server);

        bool Connecting(Socket socket, ListenHandler handler);

        void Connected(NetContext context);

        void Disconnect(NetContext context);
    }

    public class ApplicationBase : IApplication
    {

        public INetServer Server { get; private set; }

        public virtual void Connected(NetContext context)
        {

        }

        public virtual bool Connecting(Socket socket, ListenHandler handler)
        {
            return true;
        }

        public virtual void Disconnect(NetContext context)
        {

        }

        public virtual void Started(INetServer server)
        {
            Server = server;
        }
    }
}
