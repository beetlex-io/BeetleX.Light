using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BeetleX.Light.Logs;

namespace BeetleX.Light
{
    public class NetServerOptions
    {

        public NetServerOptions()
        {
            SetDefaultListen(listen =>
            {
                listen.Port = 8090;
            });
            IOQueues = Environment.ProcessorCount > 8 ? 8 : Environment.ProcessorCount;
        }

        public int IOQueues { get; set; }
        public int SessionDisposeDelay { get; set; } = 2000;
        public LogLevel LogLevel { get; set; } = LogLevel.Info;

        public string ServerName { get; set; } = "BeetleX tcp server";

        public string ServerID { get; set; } = Guid.NewGuid().ToString();

        public List<ILogOutputHandler> LogOutputHandlers { get; private set; } = new List<ILogOutputHandler>();

        public NetServerOptions AddLogOutputHandler(ILogOutputHandler logOutputHandler)
        {
            LogOutputHandlers.Add(logOutputHandler);
            return this;
        }

        public NetServerOptions AddLogOutputHandler<T>()

        where T : ILogOutputHandler, new()
        {
            AddLogOutputHandler(new T());
            return this;
        }

        public List<ListenHandler> ListenHandlers { get; private set; } = new List<ListenHandler>();

        public NetServerOptions SetDefaultListen(Action<ListenHandler> setting)
        {
            return SetListen("Default", setting);
        }

        public NetServerOptions SetListen(string name, Action<ListenHandler> setting)
        {
            var item = ListenHandlers.LastOrDefault(i => i.Name == name);
            if (item == null)
            {
                item = new ListenHandler(name);
                ListenHandlers.Add(item);
            }
            setting?.Invoke(item);
            return this;
        }
    }
}
