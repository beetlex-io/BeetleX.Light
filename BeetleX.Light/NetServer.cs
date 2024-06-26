﻿using System;
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
using System.Transactions;
using BeetleX.Light.Args;
using BeetleX.Light.Dispatchs;
using BeetleX.Light.Extension;
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
            = new DispatchCenter<(Socket, ListenHandler)>(OnConnecting, Environment.ProcessorCount > 16 ? 16 : Environment.ProcessorCount);
            CommandLineParser = CommandLineParser.GetCommandLineParser();
            _netServerStatus = new NetServerStatus(this);
        }

        private long mID;

        private NetServerStatus _netServerStatus;

        private System.Collections.Concurrent.ConcurrentDictionary<long, NetContext> _userContexts = new System.Collections.Concurrent.ConcurrentDictionary<long, NetContext>();

        private DispatchCenter<(Socket, ListenHandler)> _acceptthreadDispatcher;

        private IOQueue[] _IOScheduler;

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

        public NetworkStatistics NetworkStatistics { get; private set; } = new NetworkStatistics();

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
                    netContext.IOQueue = GetOQueue();
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
                    netContext.Init();
                    _userContexts[netContext.ID] = netContext;
                    OnApplicationConnected(netContext);
                    GetLoger(LogLevel.Info)?.Write(netContext, "NetServer", "✔ NetContext", $"Connected");
                    OnSessionConnect(netContext);
                    var task = StartNetContext(netContext);
                    NetworkStatistics.Connections.Add(1);
                }
                else
                {
                    GetLoger(LogLevel.Info)?.Write(item.Item1, "NetServer", "NetContext", $"Cancel");
                    ListenHandler.CloseSocket(item.Item1);
                }
            }
            catch (Exception e_)
            {
                GetLoger(LogLevel.Warring)?.Write(item.Item1, "NetServer", "Connect", $"Error {e_.Message}");
            }
        }


        private async Task StartNetContext(NetContext context)
        {
            var reviceTask = context.ReceiveFromSocket();
            await reviceTask;
            _userContexts.TryRemove(context.ID, out context);
            OnApplicationDisconnect(context);
            context.Dispose();
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

        public void WriteLog(LogLevel level, int threadid, string location, string model, string tag, string message, string stackTrace)
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

        private long _ioqueueInex = 0;
        internal IOQueue GetOQueue()
        {
            return _IOScheduler[System.Threading.Interlocked.Increment(ref _ioqueueInex) % _IOScheduler.Length];
        }

        public virtual void Start()
        {
            TryUnhandledException();
            if (Options.IOQueues < 1)
                Options.IOQueues = 1;
            _IOScheduler = new IOQueue[Options.IOQueues];
            for (int i = 0; i < Options.IOQueues; i++)
            {
                _IOScheduler[i] = new IOQueue();
            }
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

        public void SencCompleted(int bytes)
        {
            if (Options.LogLevel <= LogLevel.Info)
            {
                NetworkStatistics.SendIO.Add(1);
                NetworkStatistics.SendBytes.Add(bytes);
                NetworkStatistics.NetWorkIO.Add(1);
                NetworkStatistics.NetWorkBytes.Add(bytes);
            }
        }

        public void ReceiveCompleted(int bytes)
        {
            if (Options.LogLevel <= LogLevel.Info)
            {
                NetworkStatistics.ReceiveIO.Add(1);
                NetworkStatistics.Receiveytes.Add(bytes);
                NetworkStatistics.NetWorkIO.Add(1);
                NetworkStatistics.NetWorkBytes.Add(bytes);
            }
        }


    }
}
