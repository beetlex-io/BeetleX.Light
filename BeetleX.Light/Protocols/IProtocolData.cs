using BeetleX.Light.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX.Light
{
    public interface IProtocolData
    {
        void Write(StreamHandler stream);
    }

    public class ProtocolData : IProtocolData
    {

        public ProtocolData()
        {

        }

        public ProtocolData(byte[] data)
        {
            _data = new ArraySegment<byte>(data);
        }

        public ProtocolData(ArraySegment<byte> data)
        {
            _data = data;
        }

        private ArraySegment<Byte> _data;

        public static implicit operator ProtocolData(string value)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(value);
            return new ProtocolData(buffer);
        }
        public static implicit operator ProtocolData((string, Encoding) value)
        {
            byte[] buffer = value.Item2.GetBytes(value.Item1);
            return new ProtocolData(buffer);
        }
        public void Write(StreamHandler stream)
        {
            if (_data.Array != null)
            {
                stream.Stream.Write(_data.Array, _data.Offset, _data.Count);
                stream.Flush();
            }
        }
    }
}
