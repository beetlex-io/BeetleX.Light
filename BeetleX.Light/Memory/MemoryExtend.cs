using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX.Light.Memory
{
    public static class BeetlexMemoryExtend
    {

        public static short SwapInt16(short v)
        {
            return (short)((v & 0xff) << 8 | v >> 8 & 0xff);
        }

        public static ushort SwapUInt16(ushort v)
        {
            return (ushort)((v & 0xff) << 8 | v >> 8 & 0xff);
        }

        public static int SwapInt32(int v)
        {
            return (SwapInt16((short)v) & 0xffff) << 0x10 |
                          SwapInt16((short)(v >> 0x10)) & 0xffff;
        }

        public static uint SwapUInt32(uint v)
        {
            return (uint)((SwapUInt16((ushort)v) & 0xffff) << 0x10 |
                           SwapUInt16((ushort)(v >> 0x10)) & 0xffff);
        }

        public static long SwapInt64(long v)
        {
            return (SwapInt32((int)v) & 0xffffffffL) << 0x20 |
                           SwapInt32((int)(v >> 0x20)) & 0xffffffffL;
        }

        public static ulong SwapUInt64(ulong v)
        {
            return (ulong)((SwapUInt32((uint)v) & 0xffffffffL) << 0x20 |
                            SwapUInt32((uint)(v >> 0x20)) & 0xffffffffL);
        }


        public static void Write(this Span<byte> _buffer, short value, bool littleEndian = true)
        {
            if (!littleEndian)
                value = SwapInt16(value);
            _buffer[0] = (byte)value;
            _buffer[1] = (byte)(value >> 8);
        }

        public static void Write(this Stream _buffer, short value, bool littleEndian = true)
        {
            Span<byte> bytes = stackalloc byte[2];
            Write(bytes, value, littleEndian);
            _buffer.Write(bytes);

        }

        public static void Write(this Span<byte> _buffer, ushort value, bool littleEndian = true)
        {
            if (!littleEndian)
                value = SwapUInt16(value);
            _buffer[0] = (byte)value;
            _buffer[1] = (byte)(value >> 8);

        }

        public static void Write(this Stream _buffer, ushort value, bool littleEndian = true)
        {
            Span<byte> bytes = stackalloc byte[2];
            Write(bytes, value, littleEndian);
            _buffer.Write(bytes);

        }

        public static void Write(this Span<byte> _buffer, int value, bool littleEndian = true)
        {
            if (!littleEndian)
                value = SwapInt32(value);
            _buffer[0] = (byte)value;
            _buffer[1] = (byte)(value >> 8);
            _buffer[2] = (byte)(value >> 16);
            _buffer[3] = (byte)(value >> 24);

        }

        public static void Write(this Stream _buffer, int value, bool littleEndian = true)
        {
            Span<byte> bytes = stackalloc byte[4];
            Write(bytes, value, littleEndian);
            _buffer.Write(bytes);
        }

        public static void Write(this Span<byte> _buffer, uint value, bool littleEndian = true)
        {
            if (!littleEndian)
                value = SwapUInt32(value);
            _buffer[0] = (byte)value;
            _buffer[1] = (byte)(value >> 8);
            _buffer[2] = (byte)(value >> 16);
            _buffer[3] = (byte)(value >> 24);

        }

        public static void Write(this Stream _buffer, uint value, bool littleEndian = true)
        {
            Span<byte> bytes = stackalloc byte[4];
            Write(bytes, value, littleEndian);
            _buffer.Write(bytes);
        }

        public static void Write(this Span<byte> _buffer, long value, bool littleEndian = true)
        {
            if (!littleEndian)
                value = SwapInt64(value);
            _buffer[0] = (byte)value;
            _buffer[1] = (byte)(value >> 8);
            _buffer[2] = (byte)(value >> 16);
            _buffer[3] = (byte)(value >> 24);
            _buffer[4] = (byte)(value >> 32);
            _buffer[5] = (byte)(value >> 40);
            _buffer[6] = (byte)(value >> 48);
            _buffer[7] = (byte)(value >> 56);

        }

        public static void Write(this Stream _buffer, long value, bool littleEndian = true)
        {
            Span<byte> bytes = stackalloc byte[8];
            Write(bytes, value, littleEndian);
            _buffer.Write(bytes);
        }

        public static void Write(this Span<byte> _buffer, ulong value, bool littleEndian = true)
        {
            if (!littleEndian)
                value = SwapUInt64(value);
            _buffer[0] = (byte)value;
            _buffer[1] = (byte)(value >> 8);
            _buffer[2] = (byte)(value >> 16);
            _buffer[3] = (byte)(value >> 24);
            _buffer[4] = (byte)(value >> 32);
            _buffer[5] = (byte)(value >> 40);
            _buffer[6] = (byte)(value >> 48);
            _buffer[7] = (byte)(value >> 56);

        }

        public static void Write(this Stream _buffer, ulong value, bool littleEndian = true)
        {
            Span<byte> bytes = stackalloc byte[8];
            Write(bytes, value, littleEndian);
            _buffer.Write(bytes);
        }

        public static void Write(this byte[] _buffer, int postion, short value, bool littleEndian = true)
        {
            if (!littleEndian)
                value = SwapInt16(value);
            _buffer[postion + 0] = (byte)value;
            _buffer[postion + 1] = (byte)(value >> 8);
        }

        public static void Write(this byte[] _buffer, int postion, ushort value, bool littleEndian = true)
        {
            if (!littleEndian)
                value = SwapUInt16(value);
            _buffer[postion + 0] = (byte)value;
            _buffer[postion + 1] = (byte)(value >> 8);

        }

        public static void Write(this byte[] _buffer, int postion, int value, bool littleEndian = true)
        {
            if (!littleEndian)
                value = SwapInt32(value);
            _buffer[postion + 0] = (byte)value;
            _buffer[postion + 1] = (byte)(value >> 8);
            _buffer[postion + 2] = (byte)(value >> 16);
            _buffer[postion + 3] = (byte)(value >> 24);

        }

        public static void Write(this byte[] _buffer, int postion, uint value, bool littleEndian = true)
        {
            if (!littleEndian)
                value = SwapUInt32(value);
            _buffer[postion + 0] = (byte)value;
            _buffer[postion + 1] = (byte)(value >> 8);
            _buffer[postion + 2] = (byte)(value >> 16);
            _buffer[postion + 3] = (byte)(value >> 24);

        }

        public static void Write(this byte[] _buffer, int postion, long value, bool littleEndian = true)
        {
            if (!littleEndian)
                value = SwapInt64(value);
            _buffer[postion + 0] = (byte)value;
            _buffer[postion + 1] = (byte)(value >> 8);
            _buffer[postion + 2] = (byte)(value >> 16);
            _buffer[postion + 3] = (byte)(value >> 24);
            _buffer[postion + 4] = (byte)(value >> 32);
            _buffer[postion + 5] = (byte)(value >> 40);
            _buffer[postion + 6] = (byte)(value >> 48);
            _buffer[postion + 7] = (byte)(value >> 56);

        }

        public static void Write(this byte[] _buffer, int postion, ulong value, bool littleEndian = true)
        {
            if (!littleEndian)
                value = SwapUInt64(value);
            _buffer[postion + 0] = (byte)value;
            _buffer[postion + 1] = (byte)(value >> 8);
            _buffer[postion + 2] = (byte)(value >> 16);
            _buffer[postion + 3] = (byte)(value >> 24);
            _buffer[postion + 4] = (byte)(value >> 32);
            _buffer[postion + 5] = (byte)(value >> 40);
            _buffer[postion + 6] = (byte)(value >> 48);
            _buffer[postion + 7] = (byte)(value >> 56);

        }

        public static int Write(this Stream _buffer, string value, Encoding encoding)
        {
            if (string.IsNullOrEmpty(value))
                return 0;
            int result = 0;
            using (TemporaryBuffer<byte> bytes = value.Length * 6)
            {
                Span<byte> span = bytes.GetSpan();
                result = encoding.GetBytes(value, span);
                _buffer.Write(span.Slice(0, result));
                return result;
            }
        }

        public static int Write(this Span<byte> _buffer, string value, Encoding encoding)
        {
            if (string.IsNullOrEmpty(value))
                return 0;
            return encoding.GetBytes(value, _buffer);
        }

        public static int Write(this byte[] _buffer, int postion, string value, Encoding encoding)
        {
            if (string.IsNullOrEmpty(value))
                return 0;
            return encoding.GetBytes(value, 0, value.Length, _buffer, 0);
        }

        public static short ReadInt16(this byte[] m_buffer, int postion, bool littleEndian = true)
        {

            var result = (short)(m_buffer[postion + 0] | m_buffer[postion + 1] << 8);
            if (!littleEndian)
                result = SwapInt16(result);
            return result;
        }

        public static ushort ReadUInt16(this byte[] m_buffer, int postion, bool littleEndian = true)
        {

            var result = (ushort)(m_buffer[postion + 0] | m_buffer[postion + 1] << 8);
            if (!littleEndian)
                result = SwapUInt16(result);
            return result;
        }

        public static int ReadInt32(this byte[] m_buffer, int postion, bool littleEndian = true)
        {

            var result = m_buffer[postion + 0] | m_buffer[postion + 1] << 8 | m_buffer[postion + 2] << 16 | m_buffer[postion + 3] << 24;
            if (!littleEndian)
                result = SwapInt32(result);
            return result;
        }

        public static uint ReadUInt32(this byte[] m_buffer, int postion, bool littleEndian = true)
        {

            var result = (uint)(m_buffer[postion + 0] | m_buffer[postion + 1] << 8 | m_buffer[postion + 2] << 16 | m_buffer[postion + 3] << 24);
            if (!littleEndian)
                result = SwapUInt32(result);
            return result;
        }
        public static long ReadInt64(this byte[] m_buffer, int postion, bool littleEndian = true)
        {

            uint num = (uint)(m_buffer[postion + 0] | m_buffer[postion + 1] << 8 | m_buffer[postion + 2] << 16 | m_buffer[postion + 3] << 24);
            uint num2 = (uint)(m_buffer[postion + 4] | m_buffer[postion + 5] << 8 | m_buffer[postion + 6] << 16 | m_buffer[postion + 7] << 24);
            var result = (long)((ulong)num2 << 32 | num);
            if (!littleEndian)
                result = SwapInt64(result);
            return result;
        }

        public static ulong ReadUInt64(this byte[] m_buffer, int postion, bool littleEndian = true)
        {

            uint num = (uint)(m_buffer[postion + 0] | m_buffer[postion + 1] << 8 | m_buffer[postion + 2] << 16 | m_buffer[postion + 3] << 24);
            uint num2 = (uint)(m_buffer[postion + 4] | m_buffer[postion + 5] << 8 | m_buffer[postion + 6] << 16 | m_buffer[postion + 7] << 24);
            var result = (ulong)num2 << 32 | num;
            if (!littleEndian)
                result = SwapUInt64(result);
            return result;
        }

        public static short ReadInt16(this ReadOnlySequence<byte> buffer, bool littleEndian = true)
        {
            short result = 0;
            if (buffer.FirstSpan.Length >= 2)
                result = buffer.FirstSpan.ReadInt16();
            else
            {
                result = (short)(
                    buffer.FirstSpan[0] |
                    buffer.Slice(1).FirstSpan[0] << 8
                    );
            }
            if (!littleEndian)
                result = SwapInt16(result);
            return result;
        }
        public static short ReadInt16(this Stream m_buffer, bool littleEndian = true)
        {
            Span<byte> bytes = stackalloc byte[2];
            m_buffer.Read(bytes);
            return ReadInt16(bytes, littleEndian);
        }
        public static short ReadInt16(this ReadOnlySpan<byte> m_buffer, bool littleEndian = true)
        {
            var result = (short)(m_buffer[0] | m_buffer[1] << 8);
            if (!littleEndian)
                result = SwapInt16(result);
            return result;
        }

        public static ushort ReadUInt16(this ReadOnlySequence<byte> m_buffer, bool littleEndian = true)
        {
            ushort result = 0;
            if (m_buffer.FirstSpan.Length >= 2)
                result = m_buffer.FirstSpan.ReadUInt16();
            else
                result = (ushort)(
                    m_buffer.FirstSpan[0] |
                    m_buffer.Slice(1).FirstSpan[0] << 8);
            if (!littleEndian)
                result = SwapUInt16(result);
            return result;
        }
        public static ushort ReadUInt16(this Stream m_buffer, bool littleEndian = true)
        {
            Span<byte> bytes = stackalloc byte[2];
            m_buffer.Read(bytes);
            return ReadUInt16(bytes, littleEndian);
        }
        public static ushort ReadUInt16(this ReadOnlySpan<byte> m_buffer, bool littleEndian = true)
        {

            var result = (ushort)(m_buffer[0] | m_buffer[1] << 8);
            if (!littleEndian)
                result = SwapUInt16(result);
            return result;
        }



        public static int ReadInt32(this Stream m_buffer, bool littleEndian = true)
        {
            Span<byte> bytes = stackalloc byte[4];
            m_buffer.Read(bytes);
            return ReadInt32(bytes, littleEndian);
        }
        public static int ReadInt32(this ReadOnlySequence<byte> m_buffer, bool littleEndian = true)
        {
            int result = 0;
            if (m_buffer.FirstSpan.Length >= 4)
                result = m_buffer.FirstSpan.ReadInt32();
            else
            {
                Span<byte> bytes = stackalloc byte[4];
                m_buffer.CopyTo(bytes);
                result = ReadInt32(bytes);
            }
            if (!littleEndian)
                result = SwapInt32(result);
            return result;
        }
        public static int ReadInt32(this ReadOnlySpan<byte> m_buffer, bool littleEndian = true)
        {
            var result = m_buffer[0] | m_buffer[1] << 8 | m_buffer[2] << 16 | m_buffer[3] << 24;
            if (!littleEndian)
                result = SwapInt32(result);
            return result;
        }

        public static uint ReadUInt32(this Stream m_buffer, bool littleEndian = true)
        {
            Span<byte> bytes = stackalloc byte[4];
            m_buffer.Read(bytes);
            return ReadUInt32(bytes, littleEndian);
        }
        public static uint ReadUInt32(this ReadOnlySequence<byte> m_buffer, bool littleEndian = true)
        {
            uint result = 0;
            if (m_buffer.FirstSpan.Length >= 4)
                result = m_buffer.FirstSpan.ReadUInt32();
            else
            {
                Span<byte> bytes = stackalloc byte[4];
                m_buffer.CopyTo(bytes);
                result = ReadUInt32(bytes);
            }
            if (!littleEndian)
                result = SwapUInt32(result);
            return result;
        }
        public static uint ReadUInt32(this ReadOnlySpan<byte> m_buffer, bool littleEndian = true)
        {
            var result = (uint)(m_buffer[0] | m_buffer[1] << 8 | m_buffer[2] << 16 | m_buffer[3] << 24);
            if (!littleEndian)
                result = SwapUInt32(result);
            return result;
        }

        public static long ReadInt64(this Stream m_buffer, bool littleEndian = true)
        {
            Span<byte> bytes = stackalloc byte[8];
            m_buffer.Read(bytes);
            return ReadInt64(bytes, littleEndian);
        }
        public static long ReadInt64(this ReadOnlySequence<byte> m_buffer, bool littleEndian = true)
        {
            long result = 0;
            if (m_buffer.FirstSpan.Length >= 8)
                result = m_buffer.FirstSpan.ReadInt64();
            else
            {
                Span<byte> bytes = stackalloc byte[8];
                m_buffer.CopyTo(bytes);
                result = ReadInt64(bytes);
            }
            if (!littleEndian)
                result = SwapInt64(result);
            return result;
        }
        public static long ReadInt64(this ReadOnlySpan<byte> m_buffer, bool littleEndian = true)
        {
            uint num = (uint)(m_buffer[0] | m_buffer[1] << 8 | m_buffer[2] << 16 | m_buffer[3] << 24);
            uint num2 = (uint)(m_buffer[4] | m_buffer[5] << 8 | m_buffer[6] << 16 | m_buffer[7] << 24);
            var result = (long)((ulong)num2 << 32 | num);
            if (!littleEndian)
                result = SwapInt64(result);
            return result;
        }


        public static ulong ReadUInt64(this Stream m_buffer, bool littleEndian = true)
        {
            Span<byte> bytes = stackalloc byte[8];
            m_buffer.Read(bytes);
            return ReadUInt64(bytes, littleEndian);
        }
        public static ulong ReadUInt64(this ReadOnlySequence<byte> m_buffer, bool littleEndian = true)
        {
            ulong result = 0;
            if (m_buffer.FirstSpan.Length >= 8)
                result = m_buffer.FirstSpan.ReadUInt64();
            else
            {
                Span<byte> bytes = stackalloc byte[8];
                m_buffer.CopyTo(bytes);
                result = ReadUInt64(bytes);
            }
            if (!littleEndian)
                result = SwapUInt64(result);
            return result;
        }
        public static ulong ReadUInt64(this ReadOnlySpan<byte> m_buffer, bool littleEndian = true)
        {

            uint num = (uint)(m_buffer[0] | m_buffer[1] << 8 | m_buffer[2] << 16 | m_buffer[3] << 24);
            uint num2 = (uint)(m_buffer[4] | m_buffer[5] << 8 | m_buffer[6] << 16 | m_buffer[7] << 24);
            var result = (ulong)num2 << 32 | num;
            if (!littleEndian)
                result = SwapUInt64(result);
            return result;
        }

        public static string ReadString(this byte[] _buffer, int postion, int count, Encoding coding)
        {

            return coding.GetString(_buffer, postion, count);
        }
        public static string ReadString(this ReadOnlySpan<byte> _buffer, Encoding coding)
        {
            return coding.GetString(_buffer);
        }
        public static string ReadString(this Stream _buffer, int count, Encoding coding)
        {
            using (TemporaryBuffer<byte> bytes = count)
            {
                Span<byte> span = bytes.GetSpan().Slice(0, count);
                _buffer.Read(span);
                return coding.GetString(span);
            }
        }
        public static string ReadString(this ReadOnlySequence<byte> _buffer, Encoding coding)
        {
            return coding.GetString(_buffer);
        }

        public static ReadOnlySequence<byte>? IndexOf(this ReadOnlySequence<byte> _buffer, string eof, Encoding coding = null)
        {
            coding = coding ?? Encoding.UTF8;
            var bytes = coding.GetBytes(eof);
            return _buffer.IndexOf(bytes);
        }

        public static ReadOnlySequence<byte>? IndexOf(this ReadOnlySequence<byte> _buffer, byte[] eof)
        {

            ReadOnlySequence<byte> _source = _buffer;
            START:
            var point = _buffer.PositionOf(eof[^1]);
            if (point != null)
            {
                var endpoint = _buffer.GetPosition(1, point.Value);
                var result = _source.Slice(0, endpoint);
                if (eof.Length == 1)
                    return result;
                if (result.Length >= eof.Length)
                {
                    var eofbuff = result.Slice(result.Length - eof.Length);
                    var data = eofbuff.ToArray();
                    for (int i = 0; i < eof.Length - 1; i++)
                    {
                        if (data[i] != eof[i])
                        {
                            _buffer = _buffer.Slice(endpoint);
                            goto START;
                        }
                    }
                }
                return result;
            }
            return null;
        }
    }
}
