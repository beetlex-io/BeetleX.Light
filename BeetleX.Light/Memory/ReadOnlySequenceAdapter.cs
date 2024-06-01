using BeetleX.Light.Logs;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace BeetleX.Light.Memory
{
    public class ReadOnlySequenceAdapter : IDisposable
    {

 

        public ReadOnlySequenceAdapter()
        {

        }

        public Span<byte> GetSpan(int length)
        {
            return GetMemory(length).Span;
        }

        public Memory<byte> GetMemory(int length)
        {

            if (_end == null)
            {
                CreateMemory(length);
            }
            int availableSize = _end.AvailableSize;
            if (availableSize < length && availableSize < 1024)
                CreateMemory(length);
            return _end.Allot(length);

        }

        private void CreateMemory(int length)
        {
            // MemoryBlock result = new MemoryBlock(length, SegmentMinSize);
            MemoryBlock result = MemoryBlockPool.Default.Get(length);
            if (_first == null)
                _first = result;
            if (_end == null)
            {
                _end = result;
            }
            else
            {
                _end.Next = result;
                _end = result;
            }
        }

        private MemoryBlock _first = null;

        private MemoryBlock _end = null;

        public void WriteAdvanceTo(int length)
        {
            _end.AdvanceTo(length);
        }

        internal MemoryBlock FlushReturn()
        {

            var result = _first;
            _first = null;
            _end = null;
            return result;
        }

        public void Flush()
        {

            var start = _first.GetUseMemory();
            if (_first == _end)
            {
                _readOnlySequence = new ReadOnlySequence<byte>(start);
            }
            else
            {
                MemorySegment first = new MemorySegment(start);
                MemoryBlock last = _first.Next;
                MemorySegment next = first;
                while (last != null)
                {
                    next = next.Append(last.GetUseMemory());
                    last = last.Next;

                }
                _readOnlySequence = new ReadOnlySequence<byte>(first, 0, next, next.Memory.Length);
            }

        }

        private ReadOnlySequence<byte> _readOnlySequence;

        public ReadOnlySequence<byte> ReadOnlySequence => _readOnlySequence;

        public void ReaderAdvanceTo(long length)
        {
            _readOnlySequence = _readOnlySequence.Slice(length);
            while (length > 0)
            {
                var memory = _first;
                var len = memory.Allocated - memory.Postion;
                if (length >= len)
                {
                    length -= len;
                    _first = memory.Next;
                    memory.Dispose();
                    if (_first == null)
                    {
                        _end = null;
                        return;
                    }
                }
                else
                {
                    memory.ReadAdvanceTo((int)length);
                    length = 0;
                }
            }
        }

        public void Dispose()
        {
            try
            {
                var segment = _first;
                _first = null;
                _end = null;
                while (segment != null)
                {
                    var next = segment.Next;
                    segment.Dispose();
                    segment = next;
                }
                _readOnlySequence = default;
                LogHandler?.GetLoger(LogLevel.Info)?.Write(LogHandler, "SequenceAdapter", "\u2714 Disposed", "");
            }
            catch (Exception e_)
            {
                LogHandler?.GetLoger(LogLevel.Warring)?.WriteException(LogHandler, "SequenceAdapter", "Disposed", e_);
            }
            LogHandler = null;
        }

        public IGetLogHandler LogHandler { get; set; }

        internal class MemorySegment : ReadOnlySequenceSegment<byte>
        {
            public MemorySegment(ReadOnlyMemory<byte> memory)
            {
                Memory = memory;
            }

            public MemorySegment Append(ReadOnlyMemory<byte> memory)
            {
                var segment = new MemorySegment(memory)
                {
                    RunningIndex = RunningIndex + Memory.Length
                };

                Next = segment;

                return segment;
            }
        }
    }
}
