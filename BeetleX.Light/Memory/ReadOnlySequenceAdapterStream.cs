using BeetleX.Light.Logs;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX.Light.Memory
{
    public class ReadOnlySequenceAdapterStream : Stream, ISpanSequenceNetStream, IGetLogHandler
    {

        public ReadOnlySequenceAdapterStream()
        {

            this.ReadOnlySequenceAdapter = new ReadOnlySequenceAdapter();
            this.ReadOnlySequenceAdapter.SegmentMinSize = 1024 * 4;
            this.ReadOnlySequenceAdapter.LogHandler = this;
        }


        public ReadOnlySequenceAdapter ReadOnlySequenceAdapter { get; private set; }

        public IGetLogHandler LogHandler { get; set; }
        public override bool CanRead => true;

        public override bool CanSeek => throw new NotImplementedException();

        public override bool CanWrite => throw new NotImplementedException();

        public override long Length => ReadOnlySequenceAdapter.ReadOnlySequence.Length;

        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public Span<byte> Allot(int count)
        {
            var result = ReadOnlySequenceAdapter.GetSpan(count);
            WriteAdvance(4);
            return result;
        }

        public override void Flush()
        {
            this.ReadOnlySequenceAdapter.Flush();
        }

        public ReadOnlySequence<byte> GetReadOnlySequence()
        {
            return ReadOnlySequenceAdapter.ReadOnlySequence;
        }

        public Memory<byte> GetWriteMemory(int count)
        {
            return ReadOnlySequenceAdapter.GetMemory(count);
        }

        public override int ReadByte()
        {
            if (Length == 0)
                return -1;
            int result = ReadOnlySequenceAdapter.ReadOnlySequence.FirstSpan[0];
            ReadAdvance(1);
            return result;
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer.Length == 0)
                return 0;
            if (Length > count)
            {
                ReadOnlySequenceAdapter.ReadOnlySequence.Slice(0, count).CopyTo(new Span<byte>(buffer, offset, count));
                ReadAdvance(count);
                return count;
            }
            else
            {
                var len = (int)Length;
                ReadOnlySequenceAdapter.ReadOnlySequence.CopyTo(new Span<byte>(buffer, offset, len));
                ReadAdvance(len);
                return len;
            }
        }

        public void ReadAdvance(long count)
        {
            this.ReadOnlySequenceAdapter.ReaderAdvanceTo(count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void WriteByte(byte value)
        {
            var memory = ReadOnlySequenceAdapter.GetMemory(1);
            memory.Span[0] = value;
            WriteAdvance(1);
        }

        public bool TryRead(int count, out ReadOnlySequence<byte> data)
        {
            data = default;
            if (Length > count)
            {
                data = ReadOnlySequenceAdapter.ReadOnlySequence.Slice(0, count);
                return true;
            }
            return false;
        }


        public override void Write(byte[] buffer, int offset, int count)
        {
            var memory = ReadOnlySequenceAdapter.GetSpan(count);
            Span<byte> span = new Span<byte>(buffer, offset, count);
            span.CopyTo(memory);
            WriteAdvance(count);
        }



        public Span<byte> GetWriteSpan(int count)
        {
            return ReadOnlySequenceAdapter.GetSpan(count);
        }


        private long _catchWriteLength = 0;

        public void StartWriteLength()
        {
            _catchWriteLength = 0; ;
        }

        public int EndWriteLength()
        {
            return (int)_catchWriteLength;
        }

        public void WriteAdvance(int count)
        {
            _catchWriteLength += count;
            ReadOnlySequenceAdapter.WriteAdvanceTo(count);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                try
                {
                    ReadOnlySequenceAdapter.Dispose();
                    LogHandler?.GetLoger(LogLevel.Info)?.Write(LogHandler, "SequenceStream", "\u2714 Disposed", "");
                }
                catch (Exception e_)
                {
                    LogHandler?.GetLoger(LogLevel.Warring)?.WriteException(LogHandler, "SequenceStream", "Disposed", e_);
                }
                finally
                {
                    LogHandler = null;
                    ReadOnlySequenceAdapter = null;
                }
            }
        }

        public Socket Socket { get; set; }
        public EndPoint EndPoint { get => LogHandler?.EndPoint; set => throw new NotImplementedException(); }

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
            Socket streamSocket = Socket;
            if (streamSocket == null)
            {
                throw new BXException("SpanSequenceNetStream Socket is null!");
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
            Socket streamSocket = Socket;
            if (streamSocket == null)
            {
                throw new BXException("SpanSequenceNetStream Socket is null!");
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
            Socket streamSocket = Socket;
            if (streamSocket == null)
            {
                throw new BXException("SpanSequenceNetStream Socket is null!");
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
            Socket streamSocket = Socket;
            if (streamSocket == null)
            {
                throw new BXException("SpanSequenceNetStream Socket is null!");
            }
            streamSocket.EndSend(asyncResult);
        }

        public LogWriter? GetLoger(LogLevel type)
        {
            return LogHandler?.GetLoger(type);
        }
    }
}
