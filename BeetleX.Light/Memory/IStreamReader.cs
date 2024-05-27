using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX.Light.Memory
{
    public interface IStreamReader : IDisposable
    {
        ISpanSequenceNetStream ReadSequenceNetStream { get; }
        
        bool LittleEndian { get; }

        string ReadString(Encoding coding = null, int length = 0);

        bool TryReadLine(out string result, Encoding coding = null);

        string ReadUTF();

        int? ReadInt7bit();

        int ReadInt();

        uint ReadUInt();

        short ReadInt16();

        ushort ReadUInt16();

        long ReadInt64();

        ulong ReadUInt64();

        float ReadFloat();

        double ReadDouble();

        DateTime ReadDateTime();

        bool TryReadBinaryObject(HeaderSizeType type, out object result, Func<ReadOnlyMemory<byte>, object> handler);

        object ReadBinaryObject(HeaderSizeType type, Func<ReadOnlyMemory<byte>, object> handler);


    }
}
