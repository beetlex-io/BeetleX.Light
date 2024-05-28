using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX.Light.Memory
{
    public interface IStreamWriter : IDisposable
    {
        ISpanSequenceNetStream WriteSequenceNetStream { get; }
        Stream Stream { get; }

        bool LittleEndian { get; }

        int WriteString(string value, Encoding coding = null);

        void WriteLine(string value, Encoding coding = null);

        void WriteUTF(string value);

        void WriteInt7bit(int value);

        void WriteInt(int value);

        void WriteUInt(uint value);

        void WriteInt16(short value);

        void WriteUInt16(ushort value);

        void WriteInt64(long value);

        void WriteUInt64(ulong value);

        void WriteFloat(float value);

        void WriteDouble(double value);

        void WriteDateTime(DateTime value);

        void Flush();

        MemoryStream GetTempMemoryStream();

        void WriteBinaryObject(HeaderSizeType type, object msg, Action<Stream, object> handler);

        void Write(byte[] buffer, int offset, int count);
    }
}
