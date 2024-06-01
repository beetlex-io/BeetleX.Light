using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX.Light.Memory
{
    public class ObjectPoolFactory<T>
        where T : IResettable, new()
    {
        public static readonly ObjectPoolFactory<T> _default = new ObjectPoolFactory<T>();
        public static ObjectPoolFactory<T> Default => _default;
        public ObjectPoolFactory()
        {
            _partitions = new Partition[Environment.ProcessorCount];
            for (int i = 0; i < _partitions.Length; i++)
            {
                _partitions[i] = new Partition();
            }
        }

        private Partition[] _partitions;

        public ObjectPoolItem<T> Get()
        {
            int index = Thread.GetCurrentProcessorId() % _partitions.Length;
            var result = _partitions[index].Pop();
            if (result == null)
            {
                result = new ObjectPoolItem<T>();
                System.Threading.Interlocked.Increment(ref _allocatedQuantity);
            }
            result.Data.Reset();
            return result;
        }

        public void Return(ObjectPoolItem<T> item)
        {
            int index = Thread.GetCurrentProcessorId() % _partitions.Length;
            _partitions[index].Push(item);
        }

        class Partition
        {
            private Stack<ObjectPoolItem<T>> mData = new Stack<ObjectPoolItem<T>>(1024);

            public long Length => mData.Count;
            public ObjectPoolItem<T> Pop()
            {
                lock (this)
                {
                    if (mData.Count > 0)
                        return mData.Pop();
                    return default(ObjectPoolItem<T>);
                }

            }

            public void Push(ObjectPoolItem<T> data)
            {
                lock (this)
                {
                    mData.Push(data);
                }
            }

        }

        private long _allocatedQuantity = 0;

        public long AllocatedQuantity => _allocatedQuantity;

        public long Count
        {
            get
            {
                long result = 0;
                foreach (var item in _partitions)
                {
                    result += item.Length;
                }
                return result;
            }
        }

    }

    public class ObjectPoolItem<T> : IDisposable
        where T : IResettable, new()
    {
        public T Data { get; internal set; } = new T();

        public void Dispose()
        {
            ObjectPoolFactory<T>.Default.Return(this);
        }
    }

    public interface IResettable
    {
        void Reset();
    }

    public class PoolMemoryStream : MemoryStream, IResettable
    {
        public void Reset()
        {
            SetLength(0);
        }
    }

}
