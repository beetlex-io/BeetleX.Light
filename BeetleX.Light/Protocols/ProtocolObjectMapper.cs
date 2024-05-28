using BeetleX.Light.Memory;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX.Light.Protocols
{

    public class ProtocolMessageMapperFactory
    {
        public static ProtocolMessageMapper<String> StringMapper { get; set; } = new ProtocolStringHeader();

        public static ProtocolMessageMapper<uint> UintMapper { get; set; } = new ProtocolUIntHeader();

        public static ProtocolMessageMapper<UInt16> UInt16Mapper { get; set; } = new ProtocolUInt16Header();
    }
    public abstract class ProtocolMessageMapper<T>
    {
        protected Dictionary<T, Type> _valueToType = new Dictionary<T, Type>();

        protected Dictionary<Type, T> _typeToValue = new Dictionary<Type, T>();

        public bool WriteType(Stream writer, object obj, bool littleEndian)
        {
            if (_typeToValue.TryGetValue(obj.GetType(), out T value))
            {
                OnWriteType(writer, value, littleEndian);
                return true;
            }
            return false;
        }

        protected abstract void OnWriteType(Stream writer, T value, bool littleEndian);

        public ObjectMapperInfo<T> ReadType(ReadOnlyMemory<byte> reader, bool littleEndian)
        {
            ObjectMapperInfo<T> result = new ObjectMapperInfo<T>();
            var value = OnReadType(reader, littleEndian);
            if (!_valueToType.TryGetValue(value.Item1, out Type type))
            {
                throw new BXException($"{value} not exist mapper type!");
            }
            result.Value = value.Item1;
            result.MessageType = type;
            result.BuffersLength = value.Item2;
            return result;
        }

        public abstract (T, int) OnReadType(ReadOnlyMemory<byte> reader, bool littleEndian);


        public ObjectMapperInfo<T> ReadType(Stream reader, bool littleEndian)
        {
            ObjectMapperInfo<T> result = new ObjectMapperInfo<T>();
            var value = OnReadType(reader, littleEndian);
            _valueToType.TryGetValue(value, out Type type);
            result.Value = value;
            result.MessageType = type;
            return result;
        }

        protected abstract T OnReadType(Stream reader, bool littleEndian);

        public void RegisterAssembly<MSG>()
        {
            foreach (Type type in typeof(MSG).Assembly.GetTypes())
            {
                ProtocolObjectAttribute value = GetObjectTypeValue(type);
                if (value != null)
                {
                    _valueToType[(T)value.Value] = value.MessageType;
                    _typeToValue[value.MessageType] = (T)value.Value;
                }
            }
        }

        protected abstract ProtocolObjectAttribute GetObjectTypeValue(Type type);

    }

    class ProtocolStringHeader : ProtocolMessageMapper<string>
    {
        public override (string, int) OnReadType(ReadOnlyMemory<byte> reader, bool littleEndian)
        {
            var result = reader.ReadUTF(out int length, littleEndian);
            return (result, length);

        }

        protected override ProtocolObjectAttribute GetObjectTypeValue(Type type)
        {
            ProtocolObjectAttribute otv = type.GetCustomAttribute<ProtocolObjectAttribute>(false);
            if (otv != null && otv.MapperValueType == ProtocolObjectAttribute.ValueType.String)
            {
                if (string.IsNullOrEmpty((string)otv.Value))
                {
                    otv.Value = type.Name;
                }
                otv.MessageType = type;
                return otv;
            }
            return null;
        }

        protected override string OnReadType(Stream reader, bool littleEndian)
        {
            return reader.ReadUTF(littleEndian);
        }

        protected override void OnWriteType(Stream writer, string value, bool littleEndian)
        {
            writer.WriteUTF(value, littleEndian);
        }
    }

    class ProtocolUInt16Header : ProtocolMessageMapper<UInt16>
    {
        public override (ushort, int) OnReadType(ReadOnlyMemory<byte> reader, bool littleEndian)
        {
            var result = reader.Span.ReadUInt16(littleEndian);
            return (result, 2);
        }

        protected override ProtocolObjectAttribute GetObjectTypeValue(Type type)
        {
            ProtocolObjectAttribute otv = type.GetCustomAttribute<ProtocolObjectAttribute>(false);
            if (otv != null && otv.MapperValueType == ProtocolObjectAttribute.ValueType.Short)
            {
                otv.MessageType = type;
                return otv;
            }
            return null;
        }

        protected override ushort OnReadType(Stream reader, bool littleEndian)
        {
            return reader.ReadUInt16(littleEndian);
        }

        protected override void OnWriteType(Stream writer, ushort value, bool littleEndian)
        {
            writer.Write(value, littleEndian);
        }
    }

    class ProtocolUIntHeader : ProtocolMessageMapper<uint>
    {
        public override (uint, int) OnReadType(ReadOnlyMemory<byte> reader, bool littleEndian)
        {
            var result = reader.Span.ReadUInt32(littleEndian);
            return (result, 4);
        }

        protected override ProtocolObjectAttribute GetObjectTypeValue(Type type)
        {
            ProtocolObjectAttribute otv = type.GetCustomAttribute<ProtocolObjectAttribute>(false);
            if (otv != null && otv.MapperValueType == ProtocolObjectAttribute.ValueType.Int)
            {
                otv.MessageType = type;
                return otv;
            }
            return null;
        }

        protected override uint OnReadType(Stream reader, bool littleEndian)
        {
            return reader.ReadUInt32(littleEndian);
        }

        protected override void OnWriteType(Stream writer, uint value, bool littleEndian)
        {
            writer.Write(value, littleEndian);
        }
    }

    public struct ObjectMapperInfo<T>
    {
        public T Value { get; set; }

        public Type MessageType { get; set; }

        public int BuffersLength { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class ProtocolObjectAttribute : Attribute
    {
        public ProtocolObjectAttribute(string value = null)
        {
            MapperValueType = ValueType.String;
            Value = value;
        }

        public ProtocolObjectAttribute(uint value)
        {
            MapperValueType = ValueType.Int;
            Value = value;
        }

        public ProtocolObjectAttribute(ushort value)
        {
            MapperValueType = ValueType.Short;
            Value = value;
        }

        public Type MessageType { get; set; }

        public ValueType MapperValueType { get; set; }

        public object Value { get; set; }


        public enum ValueType
        {
            String,
            Int,
            Short
        }
    }
}
