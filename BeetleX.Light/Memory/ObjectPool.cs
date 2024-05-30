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
        private System.Collections.Concurrent.ConcurrentQueue<ObjectPoolItem<T>> _objectPoolItems = new System.Collections.Concurrent.ConcurrentQueue<ObjectPoolItem<T>>();


        public ObjectPoolItem<T> Get()
        {
            ObjectPoolItem<T> result = default;
            if (!_objectPoolItems.TryDequeue(out result))
            {
                result = new ObjectPoolItem<T>();
                result.Factory = this;
            }
            result.Data.Reset();
            return result;
        }

        private static ObjectPoolFactory<T> _default = null;
        public static ObjectPoolFactory<T> Default
        {
            get
            {
                if (_default == null)
                {
                    _default = new ObjectPoolFactory<T>();
                }
                return _default;
            }
        }

        internal void Return(ObjectPoolItem<T> item)
        {
            _objectPoolItems.Enqueue(item);
        }

    }

    public interface IResettable
    {
        void Reset();
    }

    public class ObjectPoolItem<T> : IDisposable
        where T : IResettable, new()
    {
        public ObjectPoolItem()
        {
            Data = new T();
        }

        internal ObjectPoolFactory<T> Factory { get; set; }
        public T Data { get; internal set; }
        public void Dispose()
        {
            Factory.Return(this);
        }
    }

    public class PoolMemoryStream : MemoryStream, IResettable
    {
        public void Reset()
        {
            SetLength(0);
        }
    }

}
