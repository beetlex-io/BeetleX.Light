using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX.Light.Memory
{
    public class LineBuffer : IDisposable
    {

        public LineBuffer(int maxSize, byte[] eof)
        {
            _maxSize = maxSize;
            _buffer = ArrayPool<byte>.Shared.Rent(_maxSize);
            _eof = eof;
        }

        private byte[] _eof;

        private byte[] _buffer;

        private int _maxSize;

        private int _postion;

        private int _length;

        private int _disposed = 0;

        public bool Import(byte value)
        {
            if (_length > _maxSize)
                throw new BXException("string line buffer overflow, change ListenHandler LineMaxLength value！");
            _buffer[_postion] = value;
            _length++;
            if (_length >= _eof.Length && value == _eof[^1])
            {
                for (int i = 0; i < _eof.Length; i++)
                {
                    if (_buffer[_postion - i] != _eof[^(1 + i)])
                        goto RETURN;
                }
                return true;
            }
        RETURN:
            _postion++;
            return false;
        }

        public string GetValue(Encoding coding)
        {
            string result;
            if (_length == 0)
                result = string.Empty;
            result = coding.GetString(_buffer, 0, _length - _eof.Length);
            _postion = 0;
            _length = 0;
            return result;
        }
        public void Dispose()
        {
            if (Interlocked.CompareExchange(ref _disposed, 1, 0) == 0)
            {
                if (_buffer != null)
                    ArrayPool<byte>.Shared.Return(_buffer);
            }
        }
    }
}
