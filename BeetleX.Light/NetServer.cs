using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using BeetleX.Light.Args;
using BeetleX.Light.Dispatchs;
using BeetleX.Light.Logs;
using BeetleX.Light.Memory;
using BeetleX.Light.Protocols;
using static System.Collections.Specialized.BitVector32;

namespace BeetleX.Light
{
    public class NetServer<APPLICATION, SESSION> : INetServer, ILocation
        where APPLICATION : IApplication, new()
        where SESSION : ISession, new()
    {

        public NetServer()
        {
            _acceptthreadDispatcher
            = new SingleThreadDispatcher<(Socket, ListenHandler)>(OnConnecting);
            CommandLineParser = CommandLineParser.GetCommandLineParser();
        }

        private long mID;

        private System.Collections.Concurrent.ConcurrentDictionary<long, NetContext> _userContexts = new System.Collections.Concurrent.ConcurrentDictionary<long, NetContext>();

        private SingleThreadDispatcher<(Socket, ListenHandler)> _acceptthreadDispatcher;

        public CommandLineParser CommandLineParser { get; set; }

        public NetServerOptions Options { get; internal set; } = new NetServerOptions();

        public IApplication Application { get; internal set; } = new APPLICATION();

        public ConcurrentDictionary<long, NetContext> UserContexts => _userContexts;

        public LogWriter? GetLoger(LogLevel level)
        {
            if ((int)(this.Options.LogLevel) <= (int)level)
            {
                LogWriter result = new LogWriter();
                result.Level = level;
                result.Loger = this;
                return result;

            }
            return null;
        }

        public StartArgs StartArgs { get; set; }

        public EndPoint EndPoint { get; set; }

        private long GetID()
        {
            return System.Threading.Interlocked.Increment(ref mID);
        }


        private void OnApplicationConnected(NetContext context)
        {
            try
            {
                Application.Connected(context);
                GetLoger(LogLevel.Debug)?.Write(context, "Application", "✔ Connected", $"");
            }
            catch (Exception e_)
            {
                GetLoger(LogLevel.Warring)?.WriteException(context, "Application", "Connected", e_);
            }
        }

        private void OnApplicationDisconnect(NetContext context)
        {
            try
            {
                Application.Disconnect(context);
                GetLoger(LogLevel.Debug)?.Write(context, "Application", "✔ Disconnect", $"");
            }
            catch (Exception e_)
            {
                GetLoger(LogLevel.Warring)?.WriteException(context, "Application", "Disconnect", e_);
            }
        }

        private void OnSessionConnect(NetContext context)
        {
            try
            {
                context.Session.Connected(context);
                GetLoger(LogLevel.Debug)?.Write(context, "Session", "✔ Connected", $"");
            }
            catch (Exception e_)
            {
                GetLoger(LogLevel.Warring)?.WriteException(context, "Session", "Connected", e_);
            }
        }

        private async Task OnConnecting((Socket, ListenHandler) item)
        {
            NetContext netContext = null;
            try
            {
                if (Application.Connecting(item.Item1, item.Item2))
                {
                    GetLoger(LogLevel.Debug)?.Write(item.Item1, "NetServer", "⏳ NetContext", $"Connecting");
                    netContext = new NetContext(item.Item1);
                    netContext.ListenHandler = item.Item2;
                    netContext.Session = new SESSION();
                    netContext.Server = this;
                    if (item.Item2.ProtocolChannel != null)
                    {
                        netContext.ProtocolChannel = (IProtocolChannel<NetContext>)item.Item2.ProtocolChannel.Clone();
                        netContext.ProtocolChannel.Context = netContext;
                    }
                    if (item.Item2.SSL)
                    {
                        netContext.TLS = true;
                        await StartSSLAuth(netContext, item.Item2);
                        netContext.NetStream.SslCompleted = true;
                    }
                    netContext.ID = GetID();
                    _userContexts[netContext.ID] = netContext;
                    OnApplicationConnected(netContext);
                    GetLoger(LogLevel.Debug)?.Write(netContext, "NetServer", "✔ NetContext", $"Connected");
                    OnSessionConnect(netContext);
                    await StartNetContext(netContext);
                }
                else
                {
                    GetLoger(LogLevel.Debug)?.Write(item.Item1, "NetServer", "NetContext", $"Cancel");
                    ListenHandler.CloseSocket(item.Item1);
                }
            }
            catch (Exception e_)
            {
                GetLoger(LogLevel.Info)?.Write(item.Item1, "NetServer", "Connect", $"Error {e_.Message}");
            }
            finally
            {
                if (netContext != null)
                {
                    _userContexts.TryRemove(netContext.ID, out netContext);
                    OnApplicationDisconnect(netContext);
                    netContext.Dispose();
                }
            }
        }

        private void OnReceive(NetContext context, object messgae)
        {
            try
            {
                context.Session.Receive(context, context.DataStream, messgae);
                GetLoger(LogLevel.Debug)?.Write(context, "Session", "Receive", messgae == null ? context.DataStream.ReadSequenceNetStream.Length.ToString() : messgae.ToString());
            }
            catch (Exception e_)
            {
                GetLoger(Logs.LogLevel.Error)?.WriteException(context, "NetContext", "SessionReceive", e_);
            }
        }

        private void OnProtocolProcess(NetContext context)
        {
            if (context.NetStream.Length > context.ListenHandler.MaxProtocolPacketSize)
            {
                GetLoger(Logs.LogLevel.Error)?.WriteException(context, "NetContext", "SessionReceive",
                    new BXException($"Network data has overflowed the MaxProtocolPacketSize length"));
                context.Dispose();
                return;
            }
            IProtocolChannel<NetContext> channel = null;
            if (context.ProtocolChannel != null)
            {
                try
                {
                    context.ProtocolChannel.Decoding(context.DataStream, OnReceive);
                }
                catch (Exception e_)
                {
                    GetLoger(Logs.LogLevel.Error)?.WriteException(context, "NetContext", $"{channel?.Name}ChannelDecoding", e_);
                    context.Dispose();
                    return;
                }
            }
            else
            {
                OnReceive(context, null);
            }
        }


        private async Task StartNetContext(NetContext context)
        {
            var reviceTask = context.ReceiveToNetStream();
            var sendTask = context.SendFromNetStream();
            var readsocket = context.NetStream.ReadSocketData<NetContext>(context, async c =>
            {
                try
                {
                    if (!context.ListenHandler.SSL)
                    {
                        OnProtocolProcess(context);
                        c.NetStream.ReaderAdvanceTo();
                    }
                    else
                    {
                        if (context.FirstReceive)
                        {
                            context.FirstReceive = false;
                            var syncTask = context.NetSslStream.SyncData<NetContext>(context, c =>
                            {
                                OnProtocolProcess(c);
                            });
                        }
                    }
                }
                catch (BXException bxe)
                {
                    GetLoger(Logs.LogLevel.Error)?.WriteException(c, "NetContext", "SessionReceive", bxe);
                    c.Dispose();
                }
            });
            await Task.WhenAll(reviceTask, sendTask);
        }

        private void SslAuthenticateAsyncCallback(IAsyncResult ar)
        {
            (NetContext, TaskCompletionSource<object>) state = ((NetContext, TaskCompletionSource<object>))ar.AsyncState;
            try
            {
                GetLoger(LogLevel.Debug)?.Write(state.Item1, "NetServer", "✔ SslAuthenticate", $"Completed");
                SslStream sslStream = state.Item1.NetSslStream;
                sslStream.EndAuthenticateAsServer(ar);
                state.Item2.TrySetResult(new object());

            }
            catch (Exception e_)
            {
                GetLoger(LogLevel.Info)?.WriteException(state.Item1, "NetServer", "SslAuthenticate", e_);
                state.Item2.TrySetException(e_);
            }
        }

        private Task StartSSLAuth(NetContext context, ListenHandler listen)
        {
            TaskCompletionSource<object> completed = new TaskCompletionSource<object>();
            try
            {
                GetLoger(LogLevel.Debug)?.Write(context, "NetServer", "⏳ SslAuthenticate", $"Starting...");
                context.NetStream.IsSsl = true;
                context.NetSslStream = new BXSslStream(context.NetStream, false);
                context.NetSslStream.OnlySequenceAdapterStream.LogHandler = context;
                context.NetSslStream.LogHandler = context;
                context.NetSslStream.BeginAuthenticateAsServer(listen.Certificate, false, listen.SslProtocols, true, SslAuthenticateAsyncCallback,
                    (context, completed));
            }
            catch (Exception e_)
            {
                GetLoger(LogLevel.Info)?.WriteException(context, "NetServer", "SslAuthenticate", e_);
                context.Dispose();
            }
            return completed.Task;
        }

        public void SocketConnecting(Socket socket, ListenHandler listen)
        {
            _acceptthreadDispatcher.Enqueue((socket, listen));
        }

        public void Write(LogLevel level, int threadid, string location, string model, string tag, string message, string stackTrace)
        {
            try
            {
                if (Options.LogOutputHandlers.Count > 0)
                {
                    LogRecord log = new LogRecord();
                    log.ThreadID = threadid;
                    log.Model = model;
                    log.Location = location;
                    log.Message = message;
                    log.Level = level;
                    log.Tag = tag;
                    log.Message = message;
                    log.StackTrace = stackTrace;
                    log.DateTime = DateTime.Now;
                    foreach (var item in Options.LogOutputHandlers)
                        item.Write(log);
                }
            }
            catch { }
        }

        public void Start()
        {
            TryUnhandledException();
            StartArgs = CommandLineParser.GetOption<StartArgs>();
            Options.SetDefaultListen(o =>
            {
                if (StartArgs.Port > 0)
                    o.Port = StartArgs.Port;
                if (!string.IsNullOrEmpty(StartArgs.Host))
                    o.Host = StartArgs.Host;
                if (!string.IsNullOrEmpty(StartArgs.SSLFile))
                {
                    o.EnabledSSL(StartArgs.SSLFile, StartArgs.SSLPassWord);
                }
            });
            foreach (var item in Options.ListenHandlers)
                item.Run(this);
            OnDisplayLogo();
            Application.Started(this);
        }
        private void OnDisplayLogo()
        {
            AssemblyCopyrightAttribute productAttr = typeof(BeetleX.Light.BXException).Assembly.GetCustomAttribute<AssemblyCopyrightAttribute>();
            var logo = "\r\n";
            logo += " -----------------------------------------------------------------------------------------\r\n";
            logo += $@"
          ____                  _     _         __   __
         |  _ \                | |   | |        \ \ / /
         | |_) |   ___    ___  | |_  | |   ___   \ V / 
         |  _ <   / _ \  / _ \ | __| | |  / _ \   > <  
         | |_) | |  __/ |  __/ | |_  | | |  __/  / . \ 
         |____/   \___|  \___|  \__| |_|  \___| /_/ \_\ 

                                            {Options.ServerName}   

";
            logo += " -----------------------------------------------------------------------------------------\r\n";
            logo += $" Copyright © beetlex-io.com 2019-2024\r\n";
            logo += $" ServerGC [{GCSettings.IsServerGC}]\r\n";
            logo += $" Version  [{typeof(BXException).Assembly.GetName().Version}]\r\n";
            logo += $" Github   [https://github.com/beetlex-io]\r\n";
            logo += $" Email    [henryfan@msn.com]\r\n";
            logo += " -----------------------------------------------------------------------------------------\r\n";
            foreach (var item in this.Options.ListenHandlers)
            {
                logo += $" {item}\r\n";
            }
            logo += " -----------------------------------------------------------------------------------------\r\n";
            GetLoger(LogLevel.Off)?.Write(this, "NetServer", "Start", logo);
        }
        private void TryUnhandledException()
        {
            GetLoger(LogLevel.Off)?.Write(this, "NetServer", "Start", "Try unhandled exception...");
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                using (System.IO.StreamWriter writer = new StreamWriter("__UnhandledException.txt"))
                {
                    Exception error = e.ExceptionObject as Exception;
                    writer.WriteLine(DateTime.Now);
                    if (error != null)
                    {
                        writer.WriteLine(error.Message);
                        writer.WriteLine(error.StackTrace);
                        if (error.InnerException != null)
                        {
                            writer.WriteLine(error.InnerException.Message);
                            writer.WriteLine(error.InnerException.StackTrace);
                        }
                    }
                    else
                    {
                        writer.WriteLine("Unhandled Exception:" + e.ExceptionObject.ToString());

                    }
                    writer.Flush();
                }
            };
        }

        public NetContext GetUserContext(long id)
        {
            _userContexts.TryGetValue(id, out NetContext result);
            return result;
        }
    }
}
