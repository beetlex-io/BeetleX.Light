using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BeetleX.Light.Dispatchs
{
    public class DispatchCenter<T> : IDisposable
    {
        List<SingleThreadDispatcher<T>> mDispatchers = new List<SingleThreadDispatcher<T>>();

        long mIndex = 1;



        public DispatchCenter(Func<T, Task> process) : this(process, Math.Min(Environment.ProcessorCount, 16))
        {

        }

        public DispatchCenter(Func<T, Task> process, int count)
        {
            for (int i = 0; i < count; i++)
            {
                mDispatchers.Add(new SingleThreadDispatcher<T>(process));
            }
        }


        public void SetErrorHaneler(Func<T, Exception, Task> handler)
        {
            if (handler != null)
            {
                foreach (var item in mDispatchers)
                {
                    item.ProcessError = handler;
                }
            }
        }

        public void Enqueue(T data, int waitLength = 5)
        {
            if (waitLength < 2)
            {
                Next().Enqueue(data);
            }
            else
            {
                for (int i = 0; i < mDispatchers.Count; i++)
                {
                    var item = mDispatchers[i];
                    if (item.Count < waitLength)
                    {
                        item.Enqueue(data);
                        return;
                    }
                }
                Next().Enqueue(data);
            }
        }

        public int Count
        {
            get
            {
                int count = 0;
                foreach (var item in mDispatchers)
                    count += item.Count;
                return count;
            }
        }


        public SingleThreadDispatcher<T> Get(object data)
        {
            int id = Math.Abs(data.GetHashCode());
            return mDispatchers[id % mDispatchers.Count];
        }

        public SingleThreadDispatcher<T> Next()
        {
            return mDispatchers[(int)(System.Threading.Interlocked.Increment(ref mIndex) % mDispatchers.Count)];
        }

        public void Dispose()
        {
            foreach (SingleThreadDispatcher<T> item in mDispatchers)
            {
                item.Dispose();
            }
            mDispatchers.Clear();
        }
    }
}
