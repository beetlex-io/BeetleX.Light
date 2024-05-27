using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX.Light.Memory
{
    public class Int7bit
    {

        public void Write(Stream stream, int value)
        {
            Span<byte> buffer = stackalloc byte[6];
            var count = 0;
            var num = (ulong)value;
            while (num >= 0x80)
            {
                buffer[count++] = (byte)(num | 0x80);
                num >>= 7;
            }
            buffer[count++] = (byte)num;
            stream.Write(buffer.Slice(0, count));
        }

        private uint mResult = 0;

        private byte mBits = 0;
        public int? Read(Stream stream)
        {

            byte b;
            while (true)
            {
                if (stream.Length < 1)
                    return null;
                var bt = stream.ReadByte();
                if (bt < 0)
                {
                    mBits = 0;
                    mResult = 0;
                    throw new BXException("Read 7bit int error:byte value cannot be less than zero!");
                }
                b = (byte)bt;

                mResult |= (uint)((b & 0x7f) << mBits);
                if ((b & 0x80) == 0) break;
                mBits += 7;
                if (mBits >= 32)
                {
                    mBits = 0;
                    mResult = 0;
                    throw new BXException("Read 7bit int error:out of maximum value!");
                }
            }
            mBits = 0;
            var result = mResult;
            mResult = 0;
            return (int)result;
        }
    }
}
