using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using BeetleX.Light.Args;
using BeetleX.Light.Logs;

namespace BeetleX.Light
{
    public interface INetServer : ILogHandler
    {
        NetServerOptions Options { get; }

        LogWriter? GetLoger(LogLevel type);

        void SocketConnecting(Socket socket, ListenHandler listen);

        CommandLineParser CommandLineParser { get; }

        IApplication Application { get; }

        void Start();

        System.Collections.Concurrent.ConcurrentDictionary<long, NetContext> UserContexts { get; }

        NetContext GetUserContext(long id);
    }
}
