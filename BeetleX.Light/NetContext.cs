using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;
using System.Text;
using System.Threading.Tasks;
using BeetleX.Light.Logs;
using BeetleX.Light.Memory;
using BeetleX.Light.Protocols;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BeetleX.Light
{
    public class NetContext : IDisposable, INetContext
    {
        public NetContext(Socket socket)
        {
            Socket = socket;
            NetStream = new PipeSpanSequenceNetStream(socket);
            LocalEndPoint = socket.LocalEndPoint;
            RemoteEndPoint = socket.RemoteEndPoint;
            NetStream.LogHandler = this;
        }

        public Socket Socket { get; internal set; }

        public long ID { get; internal set; }

        public IProtocolChannel<NetContext> ProtocolChannel { get; set; }

        public ISession Session { get; internal set; }

        public PipeSpanSequenceNetStream NetStream { get; internal set; }

        public BXSslStream NetSslStream { get; internal set; }

        public EndPoint LocalEndPoint { get; internal set; }

        public EndPoint RemoteEndPoint { get; internal set; }

        public INetServer Server { get; internal set; }

        public ListenHandler ListenHandler { get; set; }

        public bool TLS { get; internal set; } = false;

        private System.Collections.Concurrent.ConcurrentQueue<object> _sendQueue = new System.Collections.Concurrent.ConcurrentQueue<object>();

        private int _sendState = 0;
        public bool Send(object message)
        {
            if (message == null)
            {
                GetLoger(LogLevel.Warring)?.Write(this, "NetContext", $"Send", "Message is null");
                return false;
            }
            if (Disposed)
            {
                GetLoger(LogLevel.Warring)?.Write(this, "NetContext", $"Send", "Session disposed");
                return false;
            }
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
                _sendState = 0;
            }

        }

        internal async Task ReceiveToNetStream()
        {
            var writer = NetStream.ReceiveWriter;
            while (true)
            {
                Memory<byte> memory = writer.GetMemory(ListenHandler.ReceiveBufferSize);
                try
                {
                    int bytesRead = await Socket.ReceiveAsync(memory, SocketFlags.None);
                    Server.GetLoger(LogLevel.Debug)?.Write(this, "NetContext", "ReceiveData", $"Length {bytesRead}");
                    Server.GetLoger(LogLevel.Trace)?.Write(this, "NetContext", "✉ ReceiveData", $"{Convert.ToHexString(memory.Slice(0, bytesRead).Span)}");
                    if (bytesRead == 0)
                    {
                        GetLoger(LogLevel.Info)?.Write(this, "NetContext", "ReceiveData", $"receive data is 0");
                        await Task.Delay(Constants.ReceiveZeroDelayTime);
                        break;
                    }
                    writer.Advance(bytesRead);
                }
                catch (Exception ex)
                {
                    Server?.GetLoger(Logs.LogLevel.Error)?.WriteException(this, "NetContext", "ReceiveData", ex);
                    break;
                }
                FlushResult result = await writer.FlushAsync();
                if (result.IsCompleted)
                {
                    break;
                }

            }
            // Server.GetLoger(LogLevel.Debug)?.Write(this, "NetContext", "SocketReceive", $"Completed");
            Dispose();
        }

        internal async Task SendFromNetStream()
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
                        Server.GetLoger(LogLevel.Debug)?.Write(this, "NetContext", "SendData", $"Length {len}");
                        Server.GetLoger(LogLevel.Trace)?.Write(this, "NetContext", "✉ SendData", $"{Convert.ToHexString(buffer.FirstSpan.Slice(0, len))}");
                        buffer = buffer.Slice(len);
                    }
                    reader.AdvanceTo(buffer.End);
                }

                catch (Exception e_)
                {
                    Server.GetLoger(Logs.LogLevel.Error)?.WriteException(this, "NetContext", "ReceiveSend", e_);
                    break;
                }
                if (result.IsCompleted)
                {
                    break;
                }
            }
            // Server.GetLoger(LogLevel.Debug)?.Write(this, "NetContext", "SocketReceive", $"Completed");
            Dispose();
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
                        GetLoger(LogLevel.Debug)?.Write(this, "NetContext", "\u2714 Disposed", $"");
                    }
                    catch (Exception e_)
                    {
                        GetLoger(Logs.LogLevel.Warring)?.WriteException(this, "NetContext", "Disposed", e_);
                    }
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

        private StreamHandler _streamHandler;

        public StreamHandler NetStreamHandler
        {
            get
            {
                if (_streamHandler == null)
                {
                    if (NetSslStream != null)
                    {
                        _streamHandler = (NetSslStream, ListenHandler.LittleEndian);
                        _streamHandler.ReadSequenceNetStream = NetSslStream.OnlySequenceAdapterStream;
                    }
                    else
                    {
                        _streamHandler = (NetStream, ListenHandler.LittleEndian);
                        _streamHandler.ReadSequenceNetStream = NetStream;
                    }
                    _streamHandler.LineMaxLength = ListenHandler.LineMaxLength;
                    _streamHandler.LineEof = ListenHandler.LineEof;
                }
                return _streamHandler;
            }
        }
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
