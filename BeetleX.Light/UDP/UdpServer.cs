using BeetleX.Light.Dispatchs;
using BeetleX.Light.Logs;
using BeetleX.Light.Memory;
using BeetleX.Light.Protocols;
using Microsoft.VisualBasic;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BeetleX.Light.UDP
{
    public class UdpServer : INetContext, ILogHandler, IDisposable
    {
        public UdpServer(string host, int port)
        {
            Host = host;
            Port = port;
            Process = Environment.ProcessorCount > 4 ? 2 : 1;
        }


        public static implicit operator UdpServer((string, int) info)
        {
            var NetClient = new UdpServer(info.Item1, info.Item2);
            return NetClient;
        }

        public static implicit operator UdpServer(string uri)
        {
            Uri uriInfo = new Uri(uri);
            var NetClient = new UdpServer(uriInfo.Host, uriInfo.Port);
            return NetClient;
        }
        public int Port { get; set; }

        public string Host { get; set; }

        public EndPoint EndPoint { get; set; }

        public int PacketBufferSize { get; set; } = 1024;

        public int? StartRegionPort { get; set; }

        public int? EndRegionPort { get; set; }

        public int Process { get; set; } = 1;

        public LogLevel LogLevel { get; set; }

        public bool ReuseAddress { get; set; } = false;

        public bool Pause { get; set; } = false;
        public bool UseIPv6 { get; set; } = true;

        public string ServerName { get; set; } = "BeetleX udp server";
        public List<ILogOutputHandler> LogOutputHandlers { get; private set; } = new List<ILogOutputHandler>();

        public UdpServer AddLogOutputHandler(ILogOutputHandler logOutputHandler)
        {
            LogOutputHandlers.Add(logOutputHandler);
            return this;
        }

        public UdpServer AddLogOutputHandler<T>()

        where T : ILogOutputHandler, new()
        {
            AddLogOutputHandler(new T());
            return this;
        }

        public bool LittleEndian { get; set; } = true;
        public IUdpProtocolChannel ProtocolChannel { get; set; }

        public void SetProtocolChannel<T>()
       where T : IUdpProtocolChannel, new()
        {
            SetProtocolChannel(new T());
        }
        public void SetProtocolChannel(IUdpProtocolChannel protocolChannel)
        {
            ProtocolChannel = protocolChannel;
        }
        public void Close(Exception e)
        {
            Dispose();
        }

        public LogWriter? GetLoger(LogLevel level)
        {
            if ((int)(this.LogLevel) <= (int)level)
            {
                LogWriter result = new LogWriter();
                result.Level = level;
                result.Loger = this;
                return result;

            }
            return null;
        }

        internal Socket Socket { get; set; }
        private IPAddress MatchIPAddress(string matchIP)
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    if (ip.ToString().IndexOf(matchIP) == 0)
                    {
                        return ip;
                    }
                }
            }
            throw new Exception($"No {matchIP} IPv4 address in the system!");
        }

        private DispatchCenter<UdpData> _dispatchCenter;

        public async Task Start()
        {
            _dispatchCenter = new DispatchCenter<UdpData>(OnReceiveProcess, Process);

            OnDisplayLogo();
            await Listen();
            SocketReceive();
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

                                            {ServerName}   

";
            logo += " -----------------------------------------------------------------------------------------\r\n";
            logo += $" Copyright © beetlex-io.com 2019-2024\r\n";
            logo += $" ServerGC [{GCSettings.IsServerGC}]\r\n";
            logo += $" Version  [{typeof(BXException).Assembly.GetName().Version}]\r\n";
            logo += $" Github   [https://github.com/beetlex-io]\r\n";
            logo += $" Email    [henryfan@msn.com]\r\n";
            logo += " -----------------------------------------------------------------------------------------\r\n";
            GetLoger(LogLevel.Off)?.Write(this, "NetServer", "Start", logo);
        }
        private Task Listen()
        {
            try
            {
                if (StartRegionPort != null)
                {
                    Port = StartRegionPort.Value;
                    if (EndRegionPort == null)
                        EndRegionPort = StartRegionPort + 100;
                }

                System.Net.IPAddress address;
                if (string.IsNullOrEmpty(Host))
                {
                    if (Socket.OSSupportsIPv6 && UseIPv6)
                    {
                        address = IPAddress.IPv6Any;
                    }
                    else
                    {
                        address = IPAddress.Any;
                    }
                }
                else
                {
                    if (Host.EndsWith("*"))
                    {
                        address = MatchIPAddress(Host.Replace("*", ""));
                        Host = address.ToString();
                    }
                    else
                    {
                        address = System.Net.IPAddress.Parse(Host);
                    }
                }
                NEXT_PORT:
                var ipaddress = new System.Net.IPEndPoint(address, Port);
                EndPoint = ipaddress;
                Socket = new Socket(EndPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
                if (ipaddress.Address == IPAddress.IPv6Any)
                {
                    Socket.DualMode = true;
                }
                if (this.ReuseAddress)
                {
                    Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                }
                try
                {
                    Socket.Bind(EndPoint);

                }
                catch (SocketException socket_err)
                {
                    if (socket_err.SocketErrorCode != SocketError.AddressAlreadyInUse)
                        throw socket_err;
                    if (StartRegionPort == null)
                        throw socket_err;
                    if (StartRegionPort < EndRegionPort)
                    {
                        GetLoger(LogLevel.Info)?.Write(this, "UdpServer", "Listen", $"{Host}@{Port} address already in use,continue listen ...");
                        StartRegionPort++;
                        Port = StartRegionPort.Value;
                        Socket?.Dispose();
                        goto NEXT_PORT;
                    }
                    else
                    {
                        throw socket_err;
                    }
                }
                GetLoger(LogLevel.Info)?.Write(this, "UdpServer", "Listen", $"Success {EndPoint.ToString()}");

            }
            catch (Exception e_)
            {
                GetLoger(LogLevel.Error)?.WriteException(this, "UdpServer", "Listen", e_);
            }
            return Task.CompletedTask;
        }

        private async Task SocketReceive()
        {
            while (true)
            {
                try
                {
                    EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                    IMemoryOwner<byte> buffer = MemoryPool<byte>.Shared.Rent(PacketBufferSize);
                    var result = await Socket.ReceiveFromAsync(buffer.Memory, remoteEndPoint);
                    UdpData data = new UdpData();
                    data.Memory = buffer.Memory.Slice(0, result.ReceivedBytes);
                    data.RemoteEndPoint = result.RemoteEndPoint;
                    data.MemoryOwner = buffer;
                    data.Server = this;
                    GetLoger(LogLevel.Debug)?.Write(result.RemoteEndPoint, "UdpServer", "ReceiveData", $"Length {result.ReceivedBytes}");
                    GetLoger(LogLevel.Trace)?.Write(result.RemoteEndPoint, "UdpServer", "✉ ReceiveData", $"{Convert.ToHexString(data.Memory.Span)}");
                    _dispatchCenter.Enqueue(data);
                    while (this.Pause)
                    {
                        await Task.Delay(1000);
                    }
                }
                catch (Exception e_)
                {
                    GetLoger(LogLevel.Error)?.WriteException(this, "UdpServer", "Receive", e_);
                }
            }
        }

        public Func<UdpServer, UdpData, Task> Receive { get; set; }

        private async Task OnReceiveProcess(UdpData data)
        {
            try
            {
                if (ProtocolChannel != null)
                {
                    try
                    {
                        data.Message = ProtocolChannel.Read(data.Memory, LittleEndian);
                        GetLoger(LogLevel.Debug)?.Write(data.RemoteEndPoint, "UdpServer", $"ChannelDecoding", data?.Message?.ToString());
                    }
                    catch (Exception e_)
                    {
                        GetLoger(LogLevel.Error)?.WriteException(data.RemoteEndPoint, "UdpServer", $"ChannelDecoding", e_);
                        return;
                    }
                }

                await Receive?.Invoke(this, data);
            }
            catch (Exception e_)
            {
                GetLoger(LogLevel.Error)?.WriteException(data.RemoteEndPoint, "UdpServer", "Receive", e_);
            }
            finally
            {
                data.MemoryOwner.Dispose();
                data.MemoryOwner = null;
            }
        }

        public void Write(LogLevel level, int threadid, string location, string model, string tag, string message, string stackTrace)
        {
            try
            {
                if (LogOutputHandlers.Count > 0)
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
                    foreach (var item in LogOutputHandlers)
                        item.Write(log);
                }
            }
            catch { }
        }

        public bool Disposed { get; set; } = false;
        public void Dispose()
        {
            Disposed = true;
            Pause = false;
            ListenHandler.CloseSocket(Socket);
        }


        public async Task Send(object message, EndPoint point)
        {
            try
            {
                if (message == null)
                    return;
                if (message is Memory<Byte> memory)
                {
                    await Socket.SendToAsync(memory, point);
                    GetLoger(LogLevel.Debug)?.Write(point, "UdpServer", "SendData", $"Length {memory.Length}");
                    GetLoger(LogLevel.Trace)?.Write(point, "UdpServer", "✉ SendData", $"{Convert.ToHexString(memory.Span)}");
                }
                else if (message is ArraySegment<byte> protocolData)
                {
                    await Socket.SendToAsync(protocolData, point);
                    GetLoger(LogLevel.Debug)?.Write(point, "UdpServer", "SendData", $"Length {protocolData.Count}");
                    GetLoger(LogLevel.Trace)?.Write(point, "UdpServer", "✉ SendData", $"{Convert.ToHexString(protocolData.Array, protocolData.Offset, protocolData.Count)}");
                }
                else if (message is byte[] data)
                {
                    await Socket.SendToAsync(data, point);
                    GetLoger(LogLevel.Debug)?.Write(point, "UdpServer", "SendData", $"Length {data.Length}");
                    GetLoger(LogLevel.Trace)?.Write(point, "UdpServer", "✉ SendData", $"{Convert.ToHexString(data)}");
                }
                else
                {
                    if (ProtocolChannel != null)
                    {
                        using (var poolStream = ObjectPoolFactory<PoolMemoryStream>.Default.Get())
                        {
                            var stream = poolStream.Data;
                            try
                            {
                                ProtocolChannel.Write(stream, message, LittleEndian);
                                GetLoger(LogLevel.Debug)?.Write(point, "UdpServer", $"ChannelEncoding", message.ToString());
                            }
                            catch (Exception e_)
                            {
                                GetLoger(LogLevel.Error)?.WriteException(point, "UdpServer", $"ChannelEncoding", e_);
                                return;
                            }

                            var buffer = stream.GetBuffer();
                            memory = new Memory<byte>(buffer, 0, (int)stream.Length);
                            await Socket.SendToAsync(memory, point);
                        }
                        GetLoger(LogLevel.Debug)?.Write(point, "UdpServer", "SendData", $"Length {memory.Length}");
                        GetLoger(LogLevel.Trace)?.Write(point, "UdpServer", "✉ SendData", $"{Convert.ToHexString(memory.Span)}");
                    }
                    else
                    {
                        GetLoger(LogLevel.Error)?.Write(point, "UdpServer", "Send", $"Write message error! the protocol channel does not exist");
                    }
                }
            }
            catch (Exception e_)
            {
                GetLoger(LogLevel.Error)?.WriteException(this, "UdpServer", "Send", e_);
            }
        }

        [ThreadStatic]
        static MemoryStream _memoryStream;

        public MemoryStream GetTempMemoryStream()
        {
            if (_memoryStream == null)
            {
                _memoryStream = new MemoryStream();
            }
            _memoryStream.SetLength(0);
            return _memoryStream;
        }
    }
}
