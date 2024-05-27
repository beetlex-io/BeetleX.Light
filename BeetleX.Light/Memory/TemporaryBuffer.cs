using System;
using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX.Light.Memory
{
    public struct TemporaryBuffer<T> : IDisposable
    {

        public Span<T> GetSpan()
        {
            return Owner.Memory.Span;
        }
        public IMemoryOwner<T> Owner { get; private set; }

        public void Dispose()
        {
            Owner.Dispose();
        }

        public static implicit operator TemporaryBuffer<T>(int length)
        {
            TemporaryBuffer<T> result = new TemporaryBuffer<T>();
            result.Owner = MemoryPool<T>.Shared.Rent(length);
            return result;
        }
        public static implicit operator TemporaryBuffer<T>(uint length)
        {
            TemporaryBuffer<T> result = new TemporaryBuffer<T>();
            result.Owner = MemoryPool<T>.Shared.Rent((int)length);
            return result;
        }
    }



    public struct EncodingBuffer : IDisposable
    {
        public byte[] Data { get; set; }

        public int Length { get; set; }
        public void Dispose()
        {
            ArrayPool<byte>.Shared.Return(Data);
        }

        public static implicit operator EncodingBuffer(string value)
        {
            EncodingBuffer result = new EncodingBuffer();
            if (string.IsNullOrEmpty(value))
                return result;
            result.Data = ArrayPool<byte>.Shared.Rent(value.Length * 6);
            result.Length = Encoding.UTF8.GetBytes(value, 0, value.Length, result.Data, 0);
            return result;
        }

        public static implicit operator EncodingBuffer((string, Encoding) value)
        {
            EncodingBuffer result = new EncodingBuffer();
            if (string.IsNullOrEmpty(value.Item1))
                return result;
            result.Data = ArrayPool<byte>.Shared.Rent(value.Item1.Length * 6);
            result.Length = value.Item2.GetBytes(value.Item1, 0, value.Item1.Length, result.Data, 0);
            return result;
        }

        public void Write(Stream stream)
        {
            stream.Write(Data, 0, Length);
        }
    }
}
