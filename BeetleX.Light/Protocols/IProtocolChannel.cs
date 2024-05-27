using BeetleX.Light.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX.Light.Protocols
{
    public interface IProtocolChannel<T> : ICloneable, IDisposable
    {
        string Name { get; }

        T Context { get; set; }

        void Encoding(IStreamWriter writer, object message);

        void Decoding(IStreamReader reader, Action<T, object> completed);
    }
}
