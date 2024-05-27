using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX.Light.Memory
{
    public class BitHelper
    {


        public const int BIT_1 = 0b0000_0000_0000_0001;

        public const int BIT_2 = 0b0000_0000_0000_0010;

        public const int BIT_3 = 0b0000_0000_0000_0100;

        public const int BIT_4 = 0b0000_0000_0000_1000;

        public const int BIT_5 = 0b0000_0000_0001_0000;

        public const int BIT_6 = 0b0000_0000_0010_0000;

        public const int BIT_7 = 0b0000_0000_0100_0000;

        public const int BIT_8 = 0b0000_0000_1000_0000;

        public const int BIT_9 = 0b0000_0001_0000_0000;

        public const int BIT_10 = 0b0000_0010_0000_0000;

        public const int BIT_11 = 0b0000_0100_0000_0000;

        public const int BIT_12 = 0b0000_1000_0000_0000;

        public const int BIT_13 = 0b0001_0000_0000_0000;

        public const int BIT_14 = 0b0010_0000_0000_0000;

        public const int BIT_15 = 0b0100_0000_0000_0000;

        public const int BIT_16 = 0b1000_0000_0000_0000;

        [ThreadStatic]
        static byte[] _bytes2;

        [ThreadStatic]
        static byte[] _bytes4;

        [ThreadStatic]
        static byte[] _bytes8;

        public static byte[] Get2Bytes()
        {
            if (_bytes2 == null)
                _bytes2 = new byte[2];
            return _bytes2;
        }
        public static byte[] Get4Bytes()
        {
            if (_bytes4 == null)
                _bytes4 = new byte[4];
            return _bytes4;
        }
        public static byte[] Get8Bytes()
        {
            if (_bytes8 == null)
                _bytes8 = new byte[8];
            return _bytes8;
        }


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


        public static void Write(Span<byte> _buffer, short value)
        {
            _buffer[0] = (byte)value;
            _buffer[1] = (byte)(value >> 8);
        }

        public static void Write(Span<byte> _buffer, ushort value)
        {
            _buffer[0] = (byte)value;
            _buffer[1] = (byte)(value >> 8);

        }


        public static void Write(Span<byte> _buffer, int value)
        {
            _buffer[0] = (byte)value;
            _buffer[1] = (byte)(value >> 8);
            _buffer[2] = (byte)(value >> 16);
            _buffer[3] = (byte)(value >> 24);

        }

        public static void Write(Span<byte> _buffer, uint value)
        {
            _buffer[0] = (byte)value;
            _buffer[1] = (byte)(value >> 8);
            _buffer[2] = (byte)(value >> 16);
            _buffer[3] = (byte)(value >> 24);

        }

        public static void Write(Span<byte> _buffer, long value)
        {
            _buffer[0] = (byte)value;
            _buffer[1] = (byte)(value >> 8);
            _buffer[2] = (byte)(value >> 16);
            _buffer[3] = (byte)(value >> 24);
            _buffer[4] = (byte)(value >> 32);
            _buffer[5] = (byte)(value >> 40);
            _buffer[6] = (byte)(value >> 48);
            _buffer[7] = (byte)(value >> 56);

        }

        public static void Write(Span<byte> _buffer, ulong value)
        {
            _buffer[0] = (byte)value;
            _buffer[1] = (byte)(value >> 8);
            _buffer[2] = (byte)(value >> 16);
            _buffer[3] = (byte)(value >> 24);
            _buffer[4] = (byte)(value >> 32);
            _buffer[5] = (byte)(value >> 40);
            _buffer[6] = (byte)(value >> 48);
            _buffer[7] = (byte)(value >> 56);

        }





        public static void Write(byte[] _buffer, int postion, short value)
        {
            _buffer[postion + 0] = (byte)value;
            _buffer[postion + 1] = (byte)(value >> 8);
        }

        public static void Write(byte[] _buffer, int postion, ushort value)
        {
            _buffer[postion + 0] = (byte)value;
            _buffer[postion + 1] = (byte)(value >> 8);

        }


        public static void Write(byte[] _buffer, int postion, int value)
        {
            _buffer[postion + 0] = (byte)value;
            _buffer[postion + 1] = (byte)(value >> 8);
            _buffer[postion + 2] = (byte)(value >> 16);
            _buffer[postion + 3] = (byte)(value >> 24);

        }

        public static void Write(byte[] _buffer, int postion, uint value)
        {
            _buffer[postion + 0] = (byte)value;
            _buffer[postion + 1] = (byte)(value >> 8);
            _buffer[postion + 2] = (byte)(value >> 16);
            _buffer[postion + 3] = (byte)(value >> 24);

        }

        public static void Write(byte[] _buffer, int postion, long value)
        {
            _buffer[postion + 0] = (byte)value;
            _buffer[postion + 1] = (byte)(value >> 8);
            _buffer[postion + 2] = (byte)(value >> 16);
            _buffer[postion + 3] = (byte)(value >> 24);
            _buffer[postion + 4] = (byte)(value >> 32);
            _buffer[postion + 5] = (byte)(value >> 40);
            _buffer[postion + 6] = (byte)(value >> 48);
            _buffer[postion + 7] = (byte)(value >> 56);

        }

        public static void Write(byte[] _buffer, int postion, ulong value)
        {
            _buffer[postion + 0] = (byte)value;
            _buffer[postion + 1] = (byte)(value >> 8);
            _buffer[postion + 2] = (byte)(value >> 16);
            _buffer[postion + 3] = (byte)(value >> 24);
            _buffer[postion + 4] = (byte)(value >> 32);
            _buffer[postion + 5] = (byte)(value >> 40);
            _buffer[postion + 6] = (byte)(value >> 48);
            _buffer[postion + 7] = (byte)(value >> 56);

        }



        public static short ReadInt16(byte[] m_buffer, int postion)
        {

            return (short)(m_buffer[postion + 0] | m_buffer[postion + 1] << 8);
        }

        public static ushort ReadUInt16(byte[] m_buffer, int postion)
        {

            return (ushort)(m_buffer[postion + 0] | m_buffer[postion + 1] << 8);
        }

        public static int ReadInt32(byte[] m_buffer, int postion)
        {

            return m_buffer[postion + 0] | m_buffer[postion + 1] << 8 | m_buffer[postion + 2] << 16 | m_buffer[postion + 3] << 24;
        }

        public static uint ReadUInt32(byte[] m_buffer, int postion)
        {

            return (uint)(m_buffer[postion + 0] | m_buffer[postion + 1] << 8 | m_buffer[postion + 2] << 16 | m_buffer[postion + 3] << 24);
        }

        public static long ReadInt64(byte[] m_buffer, int postion)
        {

            uint num = (uint)(m_buffer[postion + 0] | m_buffer[postion + 1] << 8 | m_buffer[postion + 2] << 16 | m_buffer[postion + 3] << 24);
            uint num2 = (uint)(m_buffer[postion + 4] | m_buffer[postion + 5] << 8 | m_buffer[postion + 6] << 16 | m_buffer[postion + 7] << 24);
            return (long)((ulong)num2 << 32 | num);
        }

        public static ulong ReadUInt64(byte[] m_buffer, int postion)
        {

            uint num = (uint)(m_buffer[postion + 0] | m_buffer[postion + 1] << 8 | m_buffer[postion + 2] << 16 | m_buffer[postion + 3] << 24);
            uint num2 = (uint)(m_buffer[postion + 4] | m_buffer[postion + 5] << 8 | m_buffer[postion + 6] << 16 | m_buffer[postion + 7] << 24);
            return (ulong)num2 << 32 | num;
        }

        public static short ReadInt16(ReadOnlySequence<byte> buffer)
        {
            if (buffer.FirstSpan.Length >= 2)
                return ReadInt16(buffer.FirstSpan);
            return (short)(
                buffer.FirstSpan[0] |
                buffer.Slice(1).FirstSpan[0] << 8
                );
        }
        public static short ReadInt16(ReadOnlySpan<byte> m_buffer)
        {
            return (short)(m_buffer[0] | m_buffer[1] << 8);
        }

        public static ushort ReadUInt16(ReadOnlySequence<byte> m_buffer)
        {
            if (m_buffer.FirstSpan.Length >= 2)
                return ReadUInt16(m_buffer.FirstSpan);
            else
                return (ushort)(
                    m_buffer.FirstSpan[0] |
                    m_buffer.Slice(1).FirstSpan[0] << 8);
        }
        public static ushort ReadUInt16(ReadOnlySpan<byte> m_buffer)
        {

            return (ushort)(m_buffer[0] | m_buffer[1] << 8);
        }


        public static int ReadInt32(ReadOnlySequence<byte> m_buffer)
        {
            if (m_buffer.FirstSpan.Length >= 4)
                return ReadInt32(m_buffer.FirstSpan);
            var bytes = Get4Bytes();
            m_buffer.CopyTo(bytes);
            return ReadInt32(bytes);
        }
        public static int ReadInt32(ReadOnlySpan<byte> m_buffer)
        {
            return m_buffer[0] | m_buffer[1] << 8 | m_buffer[2] << 16 | m_buffer[3] << 24;
        }

        public static uint ReadUInt32(ReadOnlySequence<byte> m_buffer)
        {
            if (m_buffer.FirstSpan.Length >= 4)
                return ReadUInt32(m_buffer.FirstSpan);
            var bytes = Get4Bytes();
            m_buffer.CopyTo(bytes);
            return ReadUInt32(bytes);
        }

        public static uint ReadUInt32(ReadOnlySpan<byte> m_buffer)
        {
            return (uint)(m_buffer[0] | m_buffer[1] << 8 | m_buffer[2] << 16 | m_buffer[3] << 24);
        }

        public static long ReadInt64(ReadOnlySequence<byte> m_buffer)
        {
            if (m_buffer.FirstSpan.Length >= 8)
                return ReadInt64(m_buffer.FirstSpan);
            var bytes = Get8Bytes();
            m_buffer.CopyTo(bytes);
            return ReadInt64(bytes);
        }

        public static long ReadInt64(ReadOnlySpan<byte> m_buffer)
        {
            uint num = (uint)(m_buffer[0] | m_buffer[1] << 8 | m_buffer[2] << 16 | m_buffer[3] << 24);
            uint num2 = (uint)(m_buffer[4] | m_buffer[5] << 8 | m_buffer[6] << 16 | m_buffer[7] << 24);
            return (long)((ulong)num2 << 32 | num);
        }

        public static ulong ReadUInt64(ReadOnlySequence<byte> m_buffer)
        {
            if (m_buffer.FirstSpan.Length >= 8)
                return ReadUInt64(m_buffer.FirstSpan);
            var bytes = Get8Bytes();
            m_buffer.CopyTo(bytes);
            return ReadUInt64(bytes);
        }

        public static ulong ReadUInt64(ReadOnlySpan<byte> m_buffer)
        {

            uint num = (uint)(m_buffer[0] | m_buffer[1] << 8 | m_buffer[2] << 16 | m_buffer[3] << 24);
            uint num2 = (uint)(m_buffer[4] | m_buffer[5] << 8 | m_buffer[6] << 16 | m_buffer[7] << 24);
            return (ulong)num2 << 32 | num;
        }

    }
}
