using BeetleX.Light;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Net.Http.Headers;
using System.Numerics;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BeetleX.Light.Memory
{



    public class StreamHandler : IStreamReader, IStreamWriter
    {

        public StreamHandler()
        {
            LineEof = Encoding.UTF8.GetBytes("\r\n");
        }

        public byte[] LineEof { get; set; }
        public Stream Stream { get; set; }

        public int LineMaxLength { get; set; } = 1024 * 4;

        public bool LittleEndian { get; set; } = true;

        public ISpanSequenceNetStream ReadSequenceNetStream { get; internal set; }

        public ISpanSequenceNetStream WriteSequenceNetStream { get; internal set; }

        public long Length => ReadSequenceNetStream.Length;

        public static implicit operator StreamHandler(Stream stream)
        {
            StreamHandler handler = new StreamHandler();
            handler.Stream = stream;
            handler.WriteSequenceNetStream = stream as ISpanSequenceNetStream;
            return handler;
        }

        public static implicit operator StreamHandler((Stream, bool) stream)
        {
            StreamHandler handler = new StreamHandler();
            handler.Stream = stream.Item1;
            handler.WriteSequenceNetStream = stream.Item1 as ISpanSequenceNetStream;
            handler.LittleEndian = stream.Item2;
            return handler;
        }

        #region string
        public int WriteString(string value, Encoding coding = null)
        {

            if (!string.IsNullOrEmpty(value))
            {
                coding = coding ?? Encoding.UTF8;
                if (WriteSequenceNetStream != null)
                {
                    var span = WriteSequenceNetStream.GetWriteSpan(value.Length * 6);
                    var len = span.Write(value, coding);
                    WriteSequenceNetStream.WriteAdvance(len);
                    return len;
                }
                else
                {
                    var len = Stream.Write(value, coding);
                    return len;
                }
            }
            return 0;
        }

        public string ReadString(Encoding coding = null, int length = 0)
        {
            coding = coding ?? Encoding.UTF8;
            string result;
            if (length == 0)
            {
                if (ReadSequenceNetStream != null)
                {
                    var buffer = ReadSequenceNetStream.GetReadOnlySequence();
                    result = buffer.ReadString(coding);
                    ReadSequenceNetStream.ReadAdvance(buffer.Length);
                }
                else
                {
                    result = Stream.ReadString((int)Stream.Length, coding);
                }
            }
            else
            {
                if (ReadSequenceNetStream != null)
                {
                    var buffer = ReadSequenceNetStream.GetReadOnlySequence().Slice(0, length);
                    result = buffer.ReadString(coding);
                    ReadSequenceNetStream.ReadAdvance(length);
                }
                else
                {
                    result = Stream.ReadString(length, coding);
                }
            }
            return result;
        }
        #endregion

        #region string-line
        public void WriteLine(string value, Encoding coding = null)
        {
            if (!string.IsNullOrEmpty(value))
            {
                WriteString(value, coding);
            }
            Stream.Write(LineEof);
        }

        private LineBuffer _lineBuffer;

        public bool TryReadLine(out string result, Encoding coding = null)
        {
            result = default;
            coding = coding ?? Encoding.UTF8;
            if (ReadSequenceNetStream != null)
            {
                var buffer = ReadSequenceNetStream.GetReadOnlySequence();
                var match = buffer.IndexOf(LineEof);
                if (match != null)
                {
                    result = match.Value.Slice(0, match.Value.Length - LineEof.Length).ReadString(coding);
                    ReadSequenceNetStream.ReadAdvance(match.Value.Length);
                    return true;
                }
            }
            else
            {
                if (_lineBuffer == null)
                {
                    _lineBuffer = new LineBuffer(LineMaxLength, LineEof);
                }
                while (Stream.Length > 0)
                {
                    int b = Stream.ReadByte();
                    if (b == -1)
                        return false;
                    if (_lineBuffer.Import((byte)b))
                    {
                        result = _lineBuffer.GetValue(coding);
                        return true;
                    }
                }
            }
            return false;
        }
        #endregion

        #region utfstring
        public void WriteUTF(string value)
        {
            if (WriteSequenceNetStream != null)
            {
                var sizespan = WriteSequenceNetStream.GetWriteSpan(2);
                WriteSequenceNetStream.WriteAdvance(2);
                ushort len = (ushort)WriteString(value, Encoding.UTF8);
                sizespan.Write(len, LittleEndian);
            }
            else
            {
                ushort len = 0;
                if (string.IsNullOrEmpty(value))
                {
                    Stream.Write(len, LittleEndian);
                }
                else
                {
                    using (TemporaryBuffer<byte> utfBuffer = value.Length * 6)
                    {
                        var span = utfBuffer.GetSpan();
                        len = (ushort)Encoding.UTF8.GetBytes(value, span);
                        Stream.Write(len, LittleEndian);
                        span = span.Slice(0, len);
                        Stream.Write(span);
                    }
                }
            }
        }

        public string ReadUTF()
        {
            string result = null;
            ushort len = 0;
            len = ReadUInt16();
            if (len > 0)
                result = ReadString(Encoding.UTF8, len);
            return result;
        }
        #endregion

        #region int7bit
        private Int7bit _int7Bit = new Int7bit();
        public void WriteInt7bit(int value)
        {
            _int7Bit.Write(Stream, value);
        }
        public int? ReadInt7bit()
        {
            return _int7Bit.Read(Stream);
        }
        #endregion

        public void Write(byte[] buffer, int offset, int count)
        {
            Stream.Write(buffer, offset, count);
        }

        #region int
        public void WriteInt(int value)
        {
            if (WriteSequenceNetStream != null)
            {
                WriteSequenceNetStream.GetWriteSpan(4).Write(value, LittleEndian);
                WriteSequenceNetStream.WriteAdvance(4);
            }
            else
            {
                Stream.Write(value, LittleEndian);
            }
        }

        public int ReadInt()
        {
            int result;
            if (ReadSequenceNetStream != null)
            {
                result = ReadSequenceNetStream.GetReadOnlySequence().ReadInt32(LittleEndian);
                ReadSequenceNetStream.ReadAdvance(4);
            }
            else
            {
                result = Stream.ReadInt32(LittleEndian);
            }
            return result;
        }
        #endregion

        #region uint
        public void WriteUInt(uint value)
        {
            if (WriteSequenceNetStream != null)
            {
                WriteSequenceNetStream.GetWriteSpan(4).Write(value, LittleEndian);
                WriteSequenceNetStream.WriteAdvance(4);
            }
            else
            {
                Stream.Write(value, LittleEndian);
            }
        }

        public uint ReadUInt()
        {
            uint result;
            if (ReadSequenceNetStream != null)
            {
                result = ReadSequenceNetStream.GetReadOnlySequence().ReadUInt32(LittleEndian);
                ReadSequenceNetStream.ReadAdvance(4);
            }
            else
            {
                result = Stream.ReadUInt32(LittleEndian);
            }
            return result;
        }
        #endregion

        public int Read(byte[] buffer, int offset, int count)
        {
            return Read(buffer, offset, count);
        }

        #region int16
        public void WriteInt16(short value)
        {
            if (WriteSequenceNetStream != null)
            {
                WriteSequenceNetStream.GetWriteSpan(2).Write(value, LittleEndian);
                WriteSequenceNetStream.WriteAdvance(2);
            }
            else
            {
                Stream.Write(value, LittleEndian);
            }
        }

        public short ReadInt16()
        {
            short result = 0;
            if (ReadSequenceNetStream != null)
            {
                result = ReadSequenceNetStream.GetReadOnlySequence().ReadInt16(LittleEndian);
                ReadSequenceNetStream.ReadAdvance(2);
            }
            else
            {
                result = Stream.ReadInt16(LittleEndian);

            }
            return result;
        }
        #endregion

        #region Uint16
        public void WriteUInt16(ushort value)
        {
            if (WriteSequenceNetStream != null)
            {
                WriteSequenceNetStream.GetWriteSpan(2).Write(value, LittleEndian);
                WriteSequenceNetStream.WriteAdvance(2);
            }
            else
            {
                Stream.Write(value, LittleEndian);
            }
        }

        public ushort ReadUInt16()
        {
            ushort result = 0;
            if (ReadSequenceNetStream != null)
            {
                result = ReadSequenceNetStream.GetReadOnlySequence().ReadUInt16(LittleEndian);
                ReadSequenceNetStream.ReadAdvance(2);
            }
            else
            {
                result = Stream.ReadUInt16(LittleEndian);
            }
            return result;
        }
        #endregion

        #region int64
        public void WriteInt64(long value)
        {
            if (WriteSequenceNetStream != null)
            {
                WriteSequenceNetStream.GetWriteSpan(8).Write(value, LittleEndian);
                WriteSequenceNetStream.WriteAdvance(8);
            }
            else
            {
                Stream.Write(value, LittleEndian);
            }
        }

        public long ReadInt64()
        {
            long result;
            if (ReadSequenceNetStream != null)
            {
                result = ReadSequenceNetStream.GetReadOnlySequence().ReadInt64(LittleEndian);
                ReadSequenceNetStream.ReadAdvance(8);
            }
            else
            {
                result = Stream.ReadInt64(LittleEndian);
            }
            return result;
        }
        #endregion

        #region uint64
        public void WriteUInt64(ulong value)
        {
            if (WriteSequenceNetStream != null)
            {
                WriteSequenceNetStream.GetWriteSpan(8).Write(value, LittleEndian);
                WriteSequenceNetStream.WriteAdvance(8);
            }
            else
            {
                Stream.Write(value, LittleEndian);
            }
        }

        public ulong ReadUInt64()
        {
            ulong result;
            if (ReadSequenceNetStream != null)
            {
                result = ReadSequenceNetStream.GetReadOnlySequence().ReadUInt64(LittleEndian);
                ReadSequenceNetStream.ReadAdvance(8);
            }
            else
            {
                result = Stream.ReadUInt64(LittleEndian);
            }

            return result;
        }
        #endregion

        #region float
        public unsafe void WriteFloat(float value)
        {
            int num = *(int*)&value;
            WriteInt(num);
        }

        public unsafe float ReadFloat()
        {
            int num = ReadInt();
            return *(float*)&num;
        }
        #endregion

        #region double
        public unsafe void WriteDouble(double value)
        {
            long num = *(long*)&value;
            WriteInt64(num);
        }

        public unsafe double ReadDouble()
        {
            long num = ReadInt64();
            return *(double*)&num;
        }
        #endregion

        #region DateTime
        public unsafe void WriteDateTime(DateTime value)
        {
            WriteInt64(value.ToBinary());
        }

        public unsafe DateTime ReadDateTime()
        {
            long num = ReadInt64();
            return DateTime.FromBinary(num);
        }
        #endregion

        public void Flush()
        {
            Stream.Flush();
        }

        public void Dispose()
        {
            _lineBuffer?.Dispose();
        }

        class InnerStream : MemoryStream
        {
            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
            }
        }

        [ThreadStatic]
        static MemoryStream _memoryStream;

        public MemoryStream GetTempMemoryStream()
        {
            if (_memoryStream == null)
            {
                _memoryStream = new InnerStream();
            }
            _memoryStream.SetLength(0);
            return _memoryStream;
        }

        public bool TryReadBinaryObject(HeaderSizeType type, out object result, Func<ReadOnlyMemory<byte>, object> handler)
        {
            ReadOnlySequence<byte> span;
            result = default;
            uint len;
            if (type == HeaderSizeType.Short)
            {
                if (!ReadSequenceNetStream.TryRead(2, out span))
                    return false;
                len = span.ReadUInt16() + (uint)2;
            }
            else
            {
                if (!ReadSequenceNetStream.TryRead(4, out span))
                    return false;
                len = span.ReadUInt32() + 4;
            }
            if (ReadSequenceNetStream.Length >= len)
            {
                result = ReadBinaryObject(type, handler);
                return true;
            }
            else
            {
                return false;
            }

        }

        public object ReadBinaryObject(HeaderSizeType type, Func<ReadOnlyMemory<byte>, object> handler)
        {
            uint len = 0;
            if (type == HeaderSizeType.Short)
            {
                len = ReadUInt16();
            }
            else
            {
                len = ReadUInt();
            }
            if (len == 0)
                return null;
            using (TemporaryBuffer<byte> buffer = len)
            {
                Span<byte> span = buffer.GetSpan().Slice(0, (int)len);
                ReadSequenceNetStream.GetReadOnlySequence().Slice(0, len).CopyTo(span);
                var memory = buffer.Owner.Memory.Slice(0, (int)len);
                ReadSequenceNetStream.ReadAdvance(len);
                return handler?.Invoke(memory);
            }

        }

        public void WriteBinaryObject(HeaderSizeType type, object msg, Action<Stream, object> handler)
        {
            Span<byte> size;
            if (msg == null)
            {
                if (type == HeaderSizeType.Short)
                {
                    WriteUInt16((UInt16)0);
                }
                else
                {
                    WriteUInt((UInt32)0);
                }
                return;
            }
            if (WriteSequenceNetStream != null)
            {
                if (type == HeaderSizeType.Short)
                    size = WriteSequenceNetStream.Allot(2);
                else
                    size = WriteSequenceNetStream.Allot(4);
                WriteSequenceNetStream.StartWriteLength();
                handler?.Invoke(Stream, msg);
                var len = WriteSequenceNetStream.EndWriteLength();
                if (type == HeaderSizeType.Short)
                    size.Write((ushort)len);
                else
                    size.Write((uint)len);
            }
            else
            {
                Stream stream = GetTempMemoryStream();
                handler?.Invoke(stream, msg);
                if (type == HeaderSizeType.Short)
                {
                    WriteUInt16((UInt16)stream.Length);
                }
                else
                {
                    WriteUInt((UInt32)stream.Length);
                }
                stream.Position = 0;
                stream.CopyTo(Stream);


            }

        }
    }

}
