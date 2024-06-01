using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using BeetleX.Light.Dispatchs;
using BeetleX.Light.Logs;
using BeetleX.Light.Memory;
using BeetleX.Light.Protocols;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static BeetleX.Light.Memory.ReadOnlySequenceAdapter;

namespace BeetleX.Light
{
    public class NetContext : IDisposable, INetContext
    {
        public NetContext(Socket socket)
        {
            Socket = socket;
            NetStream = new PipeSpanSequenceNetStream(socket);
            NetStream.FlushCompleted = OnStreamFlushCompleted;
            LocalEndPoint = socket.LocalEndPoint;
            RemoteEndPoint = socket.RemoteEndPoint;
            NetStream.LogHandler = this;
            NetStream.ReadSoecketStream.LogHandler = this;
            NetStream.WriteSocketStream.LogHandler = this;
            NetStream.FlushReadSocketStreamCompleted = OnFlushReadSocketStream;
        }

        internal TaskCompletionSource ContextCompletionSource = new TaskCompletionSource();

        public Socket Socket { get; internal set; }

        public long ID { get; internal set; }

        public IProtocolChannel<NetContext> ProtocolChannel { get; set; }

        public ISession Session { get; internal set; }

        public PipeSpanSequenceNetStream NetStream { get; internal set; }

        public BXSslStream NetSslStream { get; internal set; }

        public EndPoint LocalEndPoint { get; internal set; }

        public EndPoint RemoteEndPoint { get; internal set; }

        public INetServer Server { get; internal set; }

        public SocketError SocketErrorCode { get; set; } = SocketError.Success;

        public IOQueue IOQueue { get; internal set; }

        public ListenHandler ListenHandler { get; set; }

        public bool TLS { get; internal set; } = false;

        private System.Collections.Concurrent.ConcurrentQueue<object> _sendQueue = new System.Collections.Concurrent.ConcurrentQueue<object>();

        private int _sendState = 0;
        public bool Send(object message)
        {

            if (Disposed)
            {
                GetLoger(LogLevel.Warring)?.Write(this, "NetContext", $"Send", "Session disposed");
                return false;
            }
            if (message != null)
                _sendQueue.Enqueue(message);
            if (_sendQueue.Count > 0)
                OnSend(System.Threading.Interlocked.CompareExchange(ref _sendState, 1, 0) == 0);
            return true;
        }
        private void OnSend(bool sending)
        {
            if (sending)
            {
                if (Disposed) return;
                bool haveData = false;
                while (_sendQueue.TryDequeue(out object msg) && !Disposed)
                {
                    haveData = true;
                    if (msg is IProtocolData dataWriter)
                    {
                        dataWriter.Write(NetStreamHandler);
                    }
                    else
                    {
                        if (ProtocolChannel == null)
                        {
                            Server.GetLoger(LogLevel.Error)?.WriteException(this, "NetContext", "SendData", new BXException("Write message error! the protocol channel does not exist"));
                            Dispose();
                            return;
                        }
                        else
                        {

                            try
                            {
                                ProtocolChannel.Encoding(NetStreamHandler, msg);
                                GetLoger(LogLevel.Debug)?.Write(this, "NetContext", $"{ProtocolChannel?.Name}Encoding", "");
                            }
                            catch (Exception e_)
                            {
                                GetLoger(LogLevel.Error)?.WriteException(this, "NetContext", $"{ProtocolChannel?.Name}Encoding", e_);
                                Dispose();
                            }
                        }
                    }
                }
                if (haveData)
                    NetStreamHandler.Flush();
            }

        }
        private void OnReceive(NetContext context, object messgae)
        {
            try
            {
                GetLoger(LogLevel.Debug)?.Write(context, "Session", "Receive", messgae == null ? context.NetStreamHandler.ReadSequenceNetStream.Length.ToString() : messgae.ToString());
                context.Session.Receive(context, messgae);

            }
            catch (Exception e_)
            {
                GetLoger(Logs.LogLevel.Error)?.WriteException(context, "NetContext", "SessionReceive", e_);
            }
        }

        private void OnProtocolProcess(INetContext obj)
        {
            NetContext context = (NetContext)obj;
            if (context.NetStream.Length > context.ListenHandler.MaxProtocolPacketSize)
            {
                GetLoger(Logs.LogLevel.Warring)?.WriteException(context, "NetContext", "SessionReceive",
                    new BXException($"Network data has overflowed the MaxProtocolPacketSize length"));
                context.Dispose();
                return;
            }

            if (context.ProtocolChannel != null)
            {
                try
                {
                    GetLoger(LogLevel.Debug)?.Write(context, "NetContext", $"{context.ProtocolChannel?.Name}Decoding", "");
                    context.ProtocolChannel.Decoding(context.NetStreamHandler, OnReceive);

                }
                catch (Exception e_)
                {
                    GetLoger(Logs.LogLevel.Error)?.WriteException(context, "NetContext", $"{context.ProtocolChannel?.Name}Decoding", e_);
                    context.Dispose();
                    return;
                }
            }
            else
            {
                OnReceive(context, null);
            }
        }

        private void OnFlushReadSocketStream(INetContext data)
        {
            NetContext context = (NetContext)data;
            try
            {

                if (!context.ListenHandler.SSL)
                {
                    OnProtocolProcess(context);
                }
                else
                {
                    if (context.FirstReceive)
                    {
                        context.FirstReceive = false;
                        context.NetSslStream.SyncDataCompleted = OnProtocolProcess;
                        var syncTask = context.NetSslStream.SyncData<NetContext>(context);
                    }
                }
            }
            catch (Exception bxe)
            {
                GetLoger(Logs.LogLevel.Debug)?.WriteException(context, "NetContext", "SessionReceive", bxe);
            }
        }

        internal async Task ReceiveToNetStream()
        {
            var writer = NetStream.ReadSoecketStream;
            while (true)
            {
                Memory<byte> memory = writer.GetWriteMemory(ListenHandler.ReceiveBufferSize);
                try
                {
                    int bytesRead = await Socket.ReceiveAsync(memory, SocketFlags.None);
                    Server?.GetLoger(LogLevel.Debug)?.Write(this, "NetContext", "ReceiveData", $"Length {bytesRead}");
                    Server?.GetLoger(LogLevel.Trace)?.Write(this, "NetContext", "✉ ReceiveData", $"{Convert.ToHexString(memory.Slice(0, bytesRead).Span)}");
                    if (bytesRead == 0)
                    {
                        GetLoger(LogLevel.Info)?.Write(this, "NetContext", "ReceiveData", $"receive data is 0");
                        await Task.Delay(Constants.ReceiveZeroDelayTime);
                        break;
                    }
                    writer.WriteAdvance(bytesRead);
                    NetStream.FlushReadSocketStream<NetContext>(this);
                }
                catch (SocketException sockErr)
                {
                    this.SocketErrorCode = sockErr.SocketErrorCode;
                    Server?.GetLoger(Logs.LogLevel.Warring)?.WriteException(this, "NetContext", "ReceiveData", sockErr);
                    Dispose();
                    break;
                }
                catch (Exception ex)
                {
                    Server?.GetLoger(Logs.LogLevel.Warring)?.WriteException(this, "NetContext", "ReceiveData", ex);
                    Dispose();
                    break;
                }

            }

        }

        internal void Init()
        {
            if (NetSslStream != null)
            {
                _streamHandler = (NetSslStream, ListenHandler.LittleEndian);
                _streamHandler.ReadSequenceNetStream = NetSslStream.OnlySequenceAdapterStream;
            }
            else
            {
                _streamHandler = (NetStream, ListenHandler.LittleEndian);
                _streamHandler.ReadSequenceNetStream = NetStream.ReadSoecketStream;
            }
            _streamHandler.LineMaxLength = ListenHandler.LineMaxLength;
            _streamHandler.LineEof = ListenHandler.LineEof;
        }

        class SendWork : IIOWork
        {
            public NetContext Context { get; set; }

            public MemoryBlock Data { get; set; }
            public void Execute()
            {
                Context.SendToSocket(Data, true);

            }
        }

        private SendWork _sendWork = new SendWork();
        private void OnStreamFlushCompleted(MemoryBlock data)
        {
            //_sendWork.Context = this;
            //_sendWork.Data = data;
            //IOQueue.Schedule(_sendWork);
            SendToSocket(data, true);
        }
        internal async Task SendToSocket(MemoryBlock segment, bool begin)
        {
            if (segment == null)
                return;
            try
            {
                var buffer = segment.GetUseMemory();
                if (buffer.Length != 0)
                {
                    var len = await Socket.SendAsync(buffer);
                    GetLoger(LogLevel.Debug)?.Write(this, "NetContext", "SendData", $"Length {len}");
                    GetLoger(LogLevel.Trace)?.Write(this, "NetContext", "✉ SendData", $"{Convert.ToHexString(buffer.Slice(0, len).Span)}");
                    if (len != buffer.Length)
                        GetLoger(LogLevel.Error)?.Write(this, "NetContext", "SendData", $"Buffer length {buffer.Length} completed {len}");
                    await SendToSocket(segment.Next, false);
                }
            }
            catch (SocketException sockErr)
            {
                this.SocketErrorCode = sockErr.SocketErrorCode;
                GetLoger(Logs.LogLevel.Warring)?.WriteException(this, "NetContext", "SendData", sockErr);
                Dispose();

            }
            catch (Exception e_)
            {
                GetLoger(Logs.LogLevel.Warring)?.WriteException(this, "NetContext", "SendData", e_);
                Dispose();

            }
            finally
            {
                if (begin)
                {
                    while (segment != null)
                    {
                        var next = segment.Next;
                        segment.Dispose();
                        segment = next;
                    }
                }

            }
            if (begin)
            {
                _sendState = 0;
                Send(null);
            }

        }

        private int mDisposed = 0;

        internal bool FirstReceive { get; set; } = true;
        public bool Disposed => mDisposed != 0;
        private void OnDisposed(bool disposing)
        {
            if (disposing)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        OnSessionDisposed();
                        await Task.Delay(Server.Options.SessionDisposeDelay);
                        _streamHandler?.Dispose();
                        NetSslStream?.Dispose();
                        NetStream?.Dispose();
                        ProtocolChannel?.Dispose();
                        ListenHandler.CloseSocket(Socket);
                        GetLoger(LogLevel.Info)?.Write(this, "NetContext", "\u2714 Disposed", $"");
                    }
                    catch (Exception e_)
                    {
                        GetLoger(Logs.LogLevel.Warring)?.WriteException(this, "NetContext", "Disposed", e_);
                    }
                    ContextCompletionSource.SetResult();
                    ProtocolChannel = null;
                    _streamHandler = null;
                    Server = null;
                    Session = null;
                    NetStream = null;
                    NetSslStream = null;
                });
            }

        }

        private void OnSessionDisposed()
        {
            try
            {
                Session.Dispose(this);
            }
            catch (Exception e_)
            {
                Server.GetLoger(Logs.LogLevel.Info)?.WriteException(this, "NetContext", "SessionDisposed", e_);
            }
        }
        public void Dispose()
        {
            OnDisposed(System.Threading.Interlocked.CompareExchange(ref mDisposed, 1, 0) == 0);
        }

        internal StreamHandler _streamHandler;

        public StreamHandler NetStreamHandler
        {
            get
            {
                return _streamHandler;
            }
        }

        public IStreamWriter Writer => _streamHandler;

        public IStreamReader Reader => _streamHandler;

        public LogWriter? GetLoger(LogLevel level)
        {
            return Server?.GetLoger(level);
        }

        void INetContext.Close(Exception e)
        {
            this.Dispose();
        }

        public EndPoint EndPoint
        {
            get
            {
                return RemoteEndPoint;
            }
            set
            {
            }
        }
    }
}
