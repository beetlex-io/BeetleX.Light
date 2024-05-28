using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX.Light.Protocols
{
    public interface IUdpProtocolChannel
    {
        string Name { get; }
        object Read(ReadOnlyMemory<byte> buffer, bool littleEndian);

        void Write(Stream stream, object data, bool littleEndian);
    }
}
