using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX.Light.Memory
{
    public interface ISpanSequenceNetStream
    {
        ReadOnlySequence<byte> GetReadOnlySequence();

        void ReadAdvance(long count);

        Span<byte> GetWriteSpan(int count);

        Memory<byte> GetWriteMemory(int count);

        void WriteAdvance(int count);

        bool TryRead(int count, out ReadOnlySequence<byte> data);

        Span<byte> Allot(int count);

        void StartWriteLength();

        int EndWriteLength();

        long Length { get; }
    }
}
