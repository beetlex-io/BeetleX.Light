using BeetleX.Light.Logs;
using BeetleX.Light.Memory;
using BeetleX.Light.Protocols;
using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.IO.Pipes;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX.Light.Clients
{
    public class NetClient : ILogHandler, ILocation, IGetLogHandler, IContextClose
    {

        public NetClient(string host, int port)
        {
            Host = host;
            Port = port;
            LineEof = Encoding.UTF8.GetBytes("\r\n");
        }

        public static implicit operator NetClient((string, int) info)
        {
            var NetClient = new NetClient(info.Item1, info.Item2);
            return NetClient;
        }

        public static implicit operator NetClient(string uri)
        {
            Uri uriInfo = new Uri(uri);
            var NetClient = new NetClient(uriInfo.Host, uriInfo.Port);
            return NetClient;
        }

        public bool Connected { get; private set; }

        public int ConnectTimeOut { get; set; } = 100000;

        public int ReceiveBufferSize { get; set; } = 1024 * 4;

        private SemaphoreSlim mConnectSemaphoreSlim = new SemaphoreSlim(1);

        private Dictionary<Type, Delegate> _messageReceiveHandlers = new Dictionary<Type, Delegate>();

        public int LineMaxLength { get; set; } = 1024 * 4;

        public byte[] LineEof { get; set; }

        public struct ConnectStatus
        {

            public bool Connected;

            public bool NewConnection;
        }

        public Func<NetClient, Task> Connecting { get; set; }
        private async Task<ConnectStatus> OnConnect()
        {
            ConnectStatus result = new ConnectStatus();
            result.Connected = false;
            result.NewConnection = false;
            if (Connected)
            {
                result.Connected = true;
                return result;
            }
            bool completed = false;
            try
            {
                completed = await mConnectSemaphoreSlim.WaitAsync(5000);
                if (!completed)
                {
                    if (Connected)
                    {
                        result.Connected = true;
                        return result;
                    }
                    throw new TimeoutException($"Wait tcp connecting timeout!");
                }
                if (!Connected)
                {
                    GetLoger(LogLevel.Debug)?.Write(this, "NetClient", "⏳ Connect", $"Connecting...");
                    NetStream?.Dispose();
                    NetSslStream?.Dispose();
                    _sendQueue.Clear();
                    if (ConnectTimeOut > 0)
                    {
                        TaskCompletionSource completionSource = new TaskCompletionSource();
                        Task.Run(async () =>
                        {
                            try
                            {
                                await CreateSocket();
                                completionSource.SetResult();
                            }
                            catch (Exception e_)
                            {
                                completionSource.TrySetException(e_);
                            }
                        });
                        Task.Run(async () =>
                        {
                            await Task.Delay(ConnectTimeOut);
                            completionSource.TrySetException(new TimeoutException($"Connect {Host}:{Port} timeout!"));
                        });
                        await completionSource.Task;
                    }
                    else
                    {
                        await CreateSocket();
                    }
                    GetLoger(LogLevel.Debug)?.Write(this, "NetClient", "✔ Connect", $"Connected");
                    NetStream = new PipeSpanSequenceNetStream(Socket);
                    NetStream.LogHandler = this;
                    NetStream.IsSsl = SSL;

                    if (ProtocolChannel != null)
                    {
                        ProtocolChannel = (IProtocolChannel<NetClient>)ProtocolChannel.Clone();
                        ProtocolChannel.Context = this;
                    }
                    List<IProtocolChannel<NetClient>> channels = new List<IProtocolChannel<NetClient>>();
                    if (SSL)
                    {
                        NetSslStream = new BXSslStream(NetStream, false, new RemoteCertificateValidationCallback(ValidateServerCertificate), null);
                        NetSslStream.LogHandler = this;
                        NetSslStream.OnlySequenceAdapterStream.LogHandler = this;
                        await OnSslAuthenticate(NetSslStream);
                        NetStream.SslCompleted = true;
                    }
                    result.NewConnection = true;
                    Connected = true;
                    if (Connecting != null)
                        await Connecting(this);
                    GetLoger(LogLevel.Debug)?.Write(this, "NetClient", "✔ Connect", $"Context created");

                }
            }
            catch (Exception e_)
            {
                GetLoger(LogLevel.Error)?.WriteException(this, "NetClient", "Connect", e_);
                Connected = false;
                throw new BXException($"Client connect to server error {e_.Message}!");
            }
            finally
            {
                if (completed)
                {
                    mDisconnectStatus = 0;
                    mConnectSemaphoreSlim.Release();
                }

            }
            result.Connected = Connected;
            return result;
        }
        protected async Task SocketSend()
        {
            var reader = NetStream.SendReader;
            ReadResult result;
            while (true)
            {
                try
                {
                    result = await reader.ReadAsync();
                    ReadOnlySequence<byte> buffer = result.Buffer;
                    if (buffer.Length == 0)
                        break;
                    while (buffer.Length > 0)
                    {
                        var len = await Socket.SendAsync(buffer.First);
                        GetLoger(LogLevel.Debug)?.Write(this, "NetClient", "SendData", $"Length {len}");
                        GetLoger(LogLevel.Trace)?.Write(this, "NetClient", "✉ SendData", $"{Convert.ToHexString(buffer.FirstSpan.Slice(0, len))}");
                        buffer = buffer.Slice(len);
                    }
                    reader.AdvanceTo(buffer.End);
                }

                catch (Exception e_)
                {
                    GetLoger(Logs.LogLevel.Error)?.WriteException(this, "NetClient", "SendData", e_);
                    break;
                }
                if (result.IsCompleted)
                {
                    break;
                }
            }
            //GetLoger(LogLevel.Debug)?.Write(this, "NetClient", "SocketSend", $"completed!");
            Disconnect(new BXException("Socket send completed!"));
        }
        protected async Task SocketReceive()
        {
            var writer = NetStream.ReceiveWriter;
            while (true)
            {
                Memory<byte> memory = writer.GetMemory(ReceiveBufferSize);
                try
                {
                    int bytesRead = await Socket.ReceiveAsync(memory, SocketFlags.None);
                    GetLoger(LogLevel.Debug)?.Write(this, "NetClient", "ReceiveData", $"Length {bytesRead}");
                    GetLoger(LogLevel.Trace)?.Write(this, "NetClient", "✉ ReceiveData", $"{Convert.ToHexString(memory.Slice(0, bytesRead).Span)}");
                    if (bytesRead == 0)
                    {

                        break;
                    }
                    writer.Advance(bytesRead);
                }
                catch (Exception ex)
                {
                    GetLoger(Logs.LogLevel.Error)?.WriteException(this, "NetClient", "ReceiveData", ex);
                    break;
                }
                FlushResult result = await writer.FlushAsync();
                if (result.IsCompleted)
                {
                    break;
                }

            }
            //GetLoger(LogLevel.Debug)?.Write(this, "NetClient", "SocketReceive", $"completed!");
            Disconnect(new BXException("Socket receive completed!"));
        }

        private int mDisconnectStatus = 0;

        public Action<NetClient> Disconnected { get; set; }

        protected virtual void OnDisconnect(Exception error)
        {
            Disconnected?.Invoke(this);
        }
        public void Disconnect(Exception exception = null)
        {
            if (System.Threading.Interlocked.CompareExchange(ref mDisconnectStatus, 1, 0) == 0)
            {
                mConnectSemaphoreSlim.Wait();
                try
                {
                    OnDisconnect(exception);
                    NetStream?.Dispose();
                    NetSslStream?.Dispose();
                    ProtocolChannel?.Dispose();
                    NetStream.SendReader.CancelPendingRead();
                    Connected = false;
                    GetLoger(LogLevel.Debug)?.Write(this, "NetClient", "\u2714 Disconnect", $"");

                }
                catch (Exception e_)
                {
                    GetLoger(LogLevel.Warring)?.WriteException(this, "NetClient", "Disconnect", e_);
                }
                finally
                {
                    _firstReceive = true;
                    NetStream = null;
                    NetSslStream = null;
                    ListenHandler.CloseSocket(Socket);
                    Socket = null;
                    _streamHandler = null;
                    mConnectSemaphoreSlim.Release();
                }
                try
                {
                    foreach (var item in LogOutputHandlers)
                    {
                        item.Flush();
                    }
                }
                catch { }
            }
        }

        public async ValueTask Connect()
        {
            var result = await OnConnect();
            if (result.NewConnection)
                OnStart();
        }

        public Action<NetClient, StreamHandler, object> Receive { get; set; }

        public NetClient RegisterHandler<T>(Action<NetClient, T> handler)
        {
            if (handler != null)
            {
                _messageReceiveHandlers.Add(typeof(T), handler);

            }
            return this;
        }

        private StreamHandler _streamHandler;

        protected StreamHandler StreamHandler
        {
            get
            {
                if (_streamHandler == null)
                {
                    if (NetSslStream != null)
                    {
                        _streamHandler = (NetSslStream, LittleEndian);
                        _streamHandler.ReadSequenceNetStream = NetSslStream.OnlySequenceAdapterStream;
                    }
                    else
                    {
                        _streamHandler = (NetStream, LittleEndian);
                        _streamHandler.ReadSequenceNetStream = NetStream;
                    }
                    _streamHandler.LineMaxLength = LineMaxLength;
                    _streamHandler.LineEof = LineEof;
                }
                return _streamHandler;
            }
        }

        private void OnReceive(NetClient client, object msg)
        {
            try
            {
                if (msg != null)
                {
                    if (_messageReceiveHandlers.TryGetValue(msg.GetType(), out var handler))
                    {
                        handler.DynamicInvoke(client, msg);
                        return;
                    }
                }
                if (this.Receive != null)
                    this.Receive(this, StreamHandler, msg);
            }
            catch (Exception e_)
            {
                GetLoger(Logs.LogLevel.Error)?.WriteException(this, "NetClient", "ClientReceive", e_);
            }
        }

        private void OnProtocolProcess(NetClient client)
        {
            if (ProtocolChannel != null)
            {
                try
                {
                    ProtocolChannel.Decoding(client.StreamHandler, OnReceive);
                }
                catch (Exception e_)
                {
                    GetLoger(Logs.LogLevel.Error)?.WriteException(this, "NetContext", $"{ProtocolChannel?.Name}ChannelDecoding", e_);
                    Disconnect(e_);
                    return;
                }
            }
            else
            {
                OnReceive(this, null);
            }
        }

        private bool _firstReceive = true;
        private void OnStart()
        {
            try
            {
                var sendTask = SocketSend();
                var receiveTask = SocketReceive();
                var read = NetStream.ReadSocketData<NetClient>(this, (c) =>
                {
                    try
                    {
                        if (!SSL)
                        {
                            OnProtocolProcess(c);
                            c.NetStream.ReaderAdvanceTo();
                        }
                        else
                        {
                            if (_firstReceive)
                            {
                                _firstReceive = false;
                                var syncTask = NetSslStream.SyncData<NetClient>(this, c =>
                                {
                                    OnProtocolProcess(c);
                                });
                            }
                        }

                    }
                    catch (Exception bxe)
                    {
                        GetLoger(Logs.LogLevel.Error)?.WriteException(this, "NetClient", "ClientReceive", bxe);
                        c.Disconnect(bxe);
                    }
                });
            }
            catch (Exception e_)
            {
                GetLoger(Logs.LogLevel.Error)?.WriteException(this, "NetClient", "ClientStart", e_);
            }
        }

        private async Task CreateSocket()
        {
            var unixSocket = UnixSocketUri.GetUnixSocketUrl(Host);
            if (unixSocket.IsUnixSocket)
            {
                Socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified);
                EndPoint = new UnixDomainSocketEndPoint(unixSocket.SockFile);
                await Socket.ConnectAsync(EndPoint);
                return;
            }
            IPHostEntry localhost = await Dns.GetHostEntryAsync(Host);
            // This is the IP address of the local machine
            IPAddress localIpAddress = localhost.AddressList[0];
            Socket = new Socket(localIpAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            if (LocalEndPoint != null)
                Socket.Bind(LocalEndPoint);
            EndPoint = new IPEndPoint(localIpAddress, Port);
            await Socket.ConnectAsync(EndPoint);

        }

        public SslProtocols? SslProtocols { get; set; } = System.Security.Authentication.SslProtocols.Tls11 | System.Security.Authentication.SslProtocols.Tls12;

        public EndPoint LocalEndPoint { get; set; }
        protected BXSslStream NetSslStream { get; private set; }
        protected PipeSpanSequenceNetStream NetStream { get; private set; }

        public bool LittleEndian { get; set; } = true;

        public RemoteCertificateValidationCallback CertificateValidationCallback { get; set; }

        public bool SSL { get; private set; }

        private string _sslServiceName;
        public string SslServiceName
        {
            get
            {
                return _sslServiceName;
            }
            set
            {
                _sslServiceName = value;
                SSL = true;
            }
        }

        public int TimeOut
        {
            get;
            set;
        }

        public Socket Socket { get; internal set; }

        public string Host { get; set; }

        public int Port { get; set; }

        public IProtocolChannel<NetClient> ProtocolChannel { get; set; }

        public void SetProtocolChannel<T>()
        where T : IProtocolChannel<NetClient>, new()
        {
            SetProtocolChannel(new T());
        }
        public void SetProtocolChannel(IProtocolChannel<NetClient> protocolChannel)
        {
            ProtocolChannel = protocolChannel;
        }

        public X509CertificateCollection CertificateCollection { get; private set; } = new X509CertificateCollection();

        public LogLevel LogLevel { get; set; } = LogLevel.Info;

        public LogWriter? GetLoger(LogLevel level)
        {
            if ((int)(LogLevel) <= (int)level)
            {
                LogWriter result = new LogWriter();
                result.Level = level;
                result.Loger = this;
                return result;

            }
            return null;
        }

        public List<Logs.ILogOutputHandler> LogOutputHandlers { get; set; } = new List<Logs.ILogOutputHandler>();
        public EndPoint EndPoint { get; set; }

        public NetClient AddLogOutputHandler(ILogOutputHandler logOutputHandler)
        {
            LogOutputHandlers.Add(logOutputHandler);
            return this;
        }

        public NetClient AddLogOutputHandler<T>()

        where T : ILogOutputHandler, new()
        {
            AddLogOutputHandler(new T());
            return this;
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

        protected virtual bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            if (CertificateValidationCallback != null)
                return CertificateValidationCallback(sender, certificate, chain, sslPolicyErrors);
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;
            return true;
        }
        protected virtual async Task OnSslAuthenticate(SslStream sslStream)
        {
            if (SslProtocols == null)
                SslProtocols = System.Security.Authentication.SslProtocols.Tls11 |
                     System.Security.Authentication.SslProtocols.Tls12;
            GetLoger(LogLevel.Debug)?.Write(this, "NetClient", "⏳ SslAuthenticate", $"Starting...");
            await sslStream.AuthenticateAsClientAsync(SslServiceName, CertificateCollection.Count > 0 ? CertificateCollection : null, SslProtocols.Value, false);
            //sslStream.AuthenticateAsClientAsync(SslServiceName);
            GetLoger(LogLevel.Debug)?.Write(this, "NetClient", "✔ SslAuthenticate", $"Completed");
        }

        private System.Collections.Concurrent.ConcurrentQueue<object> _sendQueue = new System.Collections.Concurrent.ConcurrentQueue<object>();

        private int _sendState = 0;
        public async Task Send(object message)
        {
            await Connect();
            if (message == null)
                return;
            _sendQueue.Enqueue(message);
            if (_sendQueue.Count > 0)
                OnSend(System.Threading.Interlocked.CompareExchange(ref _sendState, 1, 0) == 0);
        }
        private void OnSend(bool sending)
        {
            if (sending)
            {
                if (!Connected) return;
                bool haveData = false;
                while (_sendQueue.TryDequeue(out object msg))
                {
                    haveData = true;
                    if (msg is IProtocolData dataWriter)
                    {
                        dataWriter.Write(StreamHandler);
                    }
                    else
                    {
                        if (ProtocolChannel == null)
                        {
                            GetLoger(LogLevel.Error)?.WriteException(this, "NetClient", "SendData", new BXException("Write message error! the protocol channel does not exist"));
                            Disconnect();
                            return;
                        }
                        else
                        {

                            try
                            {

                                ProtocolChannel.Context = this;
                                ProtocolChannel.Encoding(StreamHandler, msg);

                                //this.ProtocolPacket.Encoding(msg, this, DataStream);
                            }
                            catch (Exception e_)
                            {
                                GetLoger(LogLevel.Error)?.WriteException(this, "NetContext", $"{ProtocolChannel?.Name}ChannelEncoding", e_);
                                Disconnect(e_);
                            }
                        }
                    }
                }
                if (haveData)
                    StreamHandler.Flush();
                _sendState = 0;
            }

        }

        void IContextClose.Close(Exception e)
        {
            Disconnect(e);
        }
    }
}
