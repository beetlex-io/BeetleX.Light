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
using System.Threading.Tasks.Dataflow;
using static BeetleX.Light.Memory.ReadOnlySequenceAdapter;

namespace BeetleX.Light.Memory
{
    public class PipeSpanSequenceNetStream : Stream, ISpanSequenceNetStream
    {

        public PipeSpanSequenceNetStream(Socket socket)
        {
            _Socket = socket;
            WriteSocketStream = new ReadOnlySequenceAdapterStream();
            ReadSoecketStream = new ReadOnlySequenceAdapterStream();
            _onlySequenceAdapter = ReadSoecketStream.ReadOnlySequenceAdapter;
        }


        private Socket _Socket;

        internal ReadOnlySequenceAdapterStream WriteSocketStream { get; set; }

        internal ReadOnlySequenceAdapterStream ReadSoecketStream { get; set; }

        private ReadOnlySequenceAdapter _onlySequenceAdapter;

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => ReadSoecketStream.Length;

        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public void TestLoad()
        {

        }

        public bool IsSsl { get; set; }

        public bool SslCompleted { get; set; }

        public IGetLogHandler LogHandler { get; set; }

        internal Action<INetContext> FlushReadSocketStreamCompleted { get; set; }

        public void FlushReadSocketStream<T>(T context)
             where T : INetContext
        {
            ReadSoecketStream.Flush();
            try
            {
                var reader = _onlySequenceAdapter.ReadOnlySequence;
                if (reader.Length > 0)
                {
                    if (_readCompletionSource != null)
                    {
                        var memory = _readCompletionSource.Buffer;
                        int len = 0;
                        if (reader.Length >= _readCompletionSource.Buffer.Length)
                        {
                            len = memory.Length;
                            reader.Slice(0, memory.Length).CopyTo(memory.Span);
                            ReadAdvance(memory.Length);
                        }
                        else
                        {
                            len = (int)reader.Length;
                            reader.Slice(0, reader.Length).CopyTo(memory.Span);
                            ReadAdvance(len);
                        }
                        var comp = _readCompletionSource;
                        _readCompletionSource = null;
                        comp.TrySetResult(len);
                    }
                    else
                        FlushReadSocketStreamCompleted(context);
                }
                else
                {
                    return;
                }
            }
            catch (Exception e_)
            {
                context.GetLoger(Logs.LogLevel.Debug)?.WriteException(context, "PipSequenceStream", "FlushReadSocketData", e_);
                context.Close(e_);
            }
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
                    var reader = _onlySequenceAdapter.ReadOnlySequence;
                    if (reader.Length >= buffer.Length)
                    {
                        result = buffer.Length;
                        reader.Slice(0, buffer.Length).CopyTo(buffer.Span);
                        ReadAdvance(buffer.Length);

                    }
                    else
                    {
                        result = (int)reader.Length;
                        reader.Slice(0, reader.Length).CopyTo(buffer.Span);
                        ReadAdvance(reader.Length);
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

        internal Action<MemoryBlock> FlushCompleted { get; set; }

        public override void Flush()
        {
            //WriteSocketStream.Flush();
            var result = WriteSocketStream.FlushReturn();
            FlushCompleted(result);

        }



        public override int ReadByte()
        {
            var reader = _onlySequenceAdapter.ReadOnlySequence;
            if (reader.Length == 0)
                return -1;
            int result = reader.FirstSpan[0];
            ReadAdvance(1);
            return result;
        }

        public bool TryRead(int count, out ReadOnlySequence<byte> data)
        {
            data = default;
            if (Length > count)
            {
                var reader = _onlySequenceAdapter.ReadOnlySequence;
                data = reader.Slice(0, count);
                return true;
            }
            return false;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (Length == 0)
                return 0;
            if (buffer.Length == 0)
                return 0;
            var reader = _onlySequenceAdapter.ReadOnlySequence;
            if (Length > count)
            {
                reader.Slice(0, count).CopyTo(new Span<byte>(buffer, offset, count));
                ReadAdvance(count);
                return count;
            }
            else
            {
                var len = (int)reader.Length;
                reader.CopyTo(new Span<byte>(buffer, offset, len));
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
            Span<byte> span = new Span<byte>(buffer, offset, count);
            while (count > 0)
            {
                var memory = WriteSocketStream.GetWriteSpan(count);
                span.Slice(0, memory.Length).CopyTo(memory);
                WriteAdvance(memory.Length);
                span = span.Slice(memory.Length);
                count -= memory.Length;
            }
        }

        public override void WriteByte(byte value)
        {
            var memory = WriteSocketStream.GetWriteMemory(1);
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
                    WriteSocketStream.Dispose();
                    ReadSoecketStream.Dispose();
                    LogHandler?.GetLoger(LogLevel.Info)?.Write(LogHandler, "PipStream", "\u2714 Disposed", "");
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
            return _onlySequenceAdapter.ReadOnlySequence;
        }

        public void ReadAdvance(long count)
        {
            ReadSoecketStream.ReadAdvance(count);
        }

        public Span<byte> GetWriteSpan(int count)
        {
            return WriteSocketStream.GetWriteSpan(count);
        }

        public Memory<byte> GetWriteMemory(int count)
        {
            return WriteSocketStream.GetWriteMemory(count);
        }

        public void WriteAdvance(int count)
        {
            _catchWriteLength += count;
            WriteSocketStream.WriteAdvance(count);
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
