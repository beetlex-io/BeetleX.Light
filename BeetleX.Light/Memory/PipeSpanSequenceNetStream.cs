using BeetleX.Light.Clients;
using BeetleX.Light.Logs;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Reflection.PortableExecutable;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX.Light.Memory
{
    public class PipeSpanSequenceNetStream : Stream, ISpanSequenceNetStream
    {

        private static PipeOptions _pipeOptions = null;
        public static PipeOptions PipeOptions
        {
            get
            {
                return _pipeOptions ?? PipeOptions.Default;
            }
            set
            {
                _pipeOptions = value;
            }
        }
        public PipeSpanSequenceNetStream(Socket socket)
        {
            _Socket = socket;
            _socketReadPipe = new Pipe(PipeOptions);
            _socketWritePipe = new Pipe(PipeOptions);
            _PipeReader = _socketReadPipe.Reader;
            _PipeWriter = _socketWritePipe.Writer;
        }

        private Pipe _socketReadPipe;

        private Pipe _socketWritePipe;

        private Socket _Socket;

        private PipeReader _PipeReader;

        private PipeWriter _PipeWriter;

        private ReadOnlySequence<byte> _ReadBuffer;

        public PipeWriter ReceiveWriter => _socketReadPipe.Writer;

        public PipeReader SendReader => _socketWritePipe.Reader;

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => _ReadBuffer.Length;

        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public void ReaderAdvanceTo()
        {
            _PipeReader.AdvanceTo(_ReadBuffer.Start, _ReadBuffer.End);
        }

        public void TestLoad()
        {
            var result = _PipeReader.ReadAsync().AsTask();
            result.Wait();
            _ReadBuffer = result.Result.Buffer;
        }

        public bool IsSsl { get; set; }

        public bool SslCompleted { get; set; }

        public IGetLogHandler LogHandler { get; set; }

        public async ValueTask ReadSocketData<T>(T context, Action<T> callBack)
            where T : ILocation, IGetLogHandler, INetContext
        {
            while (true)
            {
                try
                {
                    ReadResult result = await _PipeReader.ReadAsync();
                    _ReadBuffer = result.Buffer;
                    if (_ReadBuffer.Length > 0)
                    {
                        if (_readCompletionSource != null)
                        {
                            var memory = _readCompletionSource.Buffer;
                            int len = 0;
                            if (_ReadBuffer.Length >= _readCompletionSource.Buffer.Length)
                            {
                                len = memory.Length;
                                _ReadBuffer.Slice(0, memory.Length).CopyTo(memory.Span);
                                ReadAdvance(memory.Length);
                            }
                            else
                            {
                                len = (int)_ReadBuffer.Length;
                                _ReadBuffer.Slice(0, _ReadBuffer.Length).CopyTo(memory.Span);
                                ReadAdvance(_ReadBuffer.Length);
                            }
                            var comp = _readCompletionSource;
                            _readCompletionSource = null;
                            comp.TrySetResult(len);
                        }
                        else
                            callBack(context);
                    }
                    else
                        break;
                    if (result.IsCompleted)
                    {
                        break;
                    }
                }
                catch (Exception e_)
                {
                    context.GetLoger(Logs.LogLevel.Error)?.WriteException(context, "PipSequenceStream", "ReadData", e_);
                    context.Close(e_);
                    break;
                }
            }
//            LogHandler?.GetLoger(LogLevel.Debug)?.Write(LogHandler, "PipSequenceStream", "ReadData", "completed");
        }

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default(CancellationToken))
        {
            if (!SslCompleted && IsSsl)
            {
                return base.WriteAsync(buffer, cancellationToken);
            }
            else
            {
                Write(buffer.Span);
                return new ValueTask();
            }
        }

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (!SslCompleted && IsSsl)
            {
                return await base.ReadAsync(buffer, cancellationToken);
            }
            else
            {
                if (buffer.Length == 0)
                    return 0;
                if (Length > 0)
                {
                    int result = 0;
                    _readCompletionSource = null;
                    if (_ReadBuffer.Length >= buffer.Length)
                    {
                        result = buffer.Length;
                        _ReadBuffer.Slice(0, buffer.Length).CopyTo(buffer.Span);
                        ReadAdvance(buffer.Length);

                    }
                    else
                    {
                        result = (int)_ReadBuffer.Length;
                        _ReadBuffer.Slice(0, _ReadBuffer.Length).CopyTo(buffer.Span);
                        ReadAdvance(_ReadBuffer.Length);
                    }
                    return result;
                }
                else
                {
                    _readCompletionSource = new ReadTaskCompletionSource();
                    _readCompletionSource.Buffer = buffer;
                    return await _readCompletionSource.Task;
                }
            }
        }


        private ReadTaskCompletionSource _readCompletionSource;

        class ReadTaskCompletionSource : TaskCompletionSource<int>
        {
            public Memory<byte> Buffer { get; set; }

            public int Count { get; set; }
        }

        public override int Read(Span<byte> buffer)
        {
            return base.Read(buffer);
        }

        public override void Flush()
        {
            _PipeWriter.FlushAsync();

        }

        public override int ReadByte()
        {
            if (_ReadBuffer.Length == 0)
                return -1;
            int result = _ReadBuffer.FirstSpan[0];
            ReadAdvance(1);
            return result;
        }

        public bool TryRead(int count, out ReadOnlySequence<byte> data)
        {
            data = default;
            if (Length > count)
            {
                data = _ReadBuffer.Slice(0, count);
                return true;
            }
            return false;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer.Length == 0)
                return 0;
            if (Length > count)
            {
                _ReadBuffer.CopyTo(new Span<byte>(buffer, offset, count));
                ReadAdvance(count);
                return count;
            }
            else
            {
                var len = (int)_ReadBuffer.Length;
                _ReadBuffer.CopyTo(new Span<byte>(buffer, offset, len));
                ReadAdvance(len);
                return len;
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            var memory = _PipeWriter.GetSpan(count);
            Span<byte> span = new Span<byte>(buffer, offset, count);
            span.CopyTo(memory);
            WriteAdvance(count);
        }

        public override void WriteByte(byte value)
        {
            var memory = _PipeWriter.GetMemory(1);
            memory.Span[0] = value;
            WriteAdvance(1);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                try
                {
                    _readCompletionSource?.TrySetResult(0);
                    _socketReadPipe.Reader.CancelPendingRead();
                    _socketReadPipe.Reader.Complete();
                    _socketReadPipe.Writer.Complete();

                    _socketWritePipe.Reader.CancelPendingRead();
                    _socketWritePipe.Reader.Complete();
                    _socketWritePipe.Writer.Complete();
                    LogHandler?.GetLoger(LogLevel.Debug)?.Write(LogHandler, "PipStream", "\u2714 Disposed", "");
                }
                catch (Exception e_)
                {
                    LogHandler?.GetLoger(LogLevel.Warring)?.WriteException(LogHandler, "PipStream", "Disposed", e_);
                }
                finally
                {
                    _readCompletionSource = null;
                    LogHandler = null;
                }
            }
        }


        public ReadOnlySequence<byte> GetReadOnlySequence()
        {
            return _ReadBuffer;
        }

        public void ReadAdvance(long count)
        {
            _ReadBuffer = _ReadBuffer.Slice(count);
        }

        public Span<byte> GetWriteSpan(int count)
        {
            return _PipeWriter.GetSpan(count);
        }

        public Memory<byte> GetWriteMemory(int count)
        {
            return _PipeWriter.GetMemory(count);
        }

        public void WriteAdvance(int count)
        {
            _catchWriteLength += count;
            _PipeWriter.Advance(count);
        }

        public Span<byte> Allot(int count)
        {
            var result = GetWriteSpan(count);
            WriteAdvance(count);
            return result;

        }

        private long _catchWriteLength = 0;

        public void StartWriteLength()
        {
            _catchWriteLength = 0;
        }

        public int EndWriteLength()
        {
            return (int)_catchWriteLength;
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int size, AsyncCallback callback, object state)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            if (offset < 0 || offset > buffer.Length)
            {
                throw new ArgumentOutOfRangeException("offset");
            }
            if (size < 0 || size > buffer.Length - offset)
            {
                throw new ArgumentOutOfRangeException("size");
            }
            Socket streamSocket = _Socket;
            if (streamSocket == null)
            {
                throw new BXException("Socket is null!");
            }
            IAsyncResult asyncResult = streamSocket.BeginReceive(buffer, offset, size, SocketFlags.None, callback, state);
            return asyncResult;
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
            {
                throw new ArgumentNullException("asyncResult");
            }
            Socket streamSocket = _Socket;
            if (streamSocket == null)
            {
                throw new BXException("Socket is null!");
            }

            int num = streamSocket.EndReceive(asyncResult);
            return num;
        }

        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int size, AsyncCallback callback, object state)
        {

            if (buffer == null)
            {
                throw new ArgumentNullException("buffer");
            }
            if (offset < 0 || offset > buffer.Length)
            {
                throw new ArgumentOutOfRangeException("offset");
            }
            if (size < 0 || size > buffer.Length - offset)
            {
                throw new ArgumentOutOfRangeException("size");
            }
            Socket streamSocket = _Socket;
            if (streamSocket == null)
            {
                throw new BXException("Socket is null!");
            }
            IAsyncResult asyncResult = streamSocket.BeginSend(buffer, offset, size, SocketFlags.None, callback, state);
            return asyncResult;

        }

        public override void EndWrite(IAsyncResult asyncResult)
        {

            if (asyncResult == null)
            {
                throw new ArgumentNullException("asyncResult");
            }
            Socket streamSocket = _Socket;
            if (streamSocket == null)
            {
                throw new BXException("Socket is null!");
            }
            streamSocket.EndSend(asyncResult);
        }
    }
}
