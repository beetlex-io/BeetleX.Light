using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX.Light.Dispatchs
{
    public interface IIOWork
    {
        void Execute();
    }
    public class IOQueue
    {
        private static readonly WaitCallback _doWorkCallback = s => ((IOQueue)s).DoWork();

        private readonly object _workSync = new object();
        private readonly ConcurrentQueue<IIOWork> _workItems = new ConcurrentQueue<IIOWork>();
        private bool _doingWork;

        public void Schedule(IIOWork work)
        {

            _workItems.Enqueue(work);

            lock (_workSync)
            {
                if (!_doingWork)
                {
                    System.Threading.ThreadPool.UnsafeQueueUserWorkItem(_doWorkCallback, this);
                    _doingWork = true;
                }
            }
        }



        private void DoWork()
        {
            while (true)
            {
                while (_workItems.TryDequeue(out IIOWork item))
                {
                    try
                    {
                        item.Execute();
                    }
                    catch { }
                }

                lock (_workSync)
                {
                    if (_workItems.IsEmpty)
                    {
                        _doingWork = false;
                        return;
                    }
                }
            }
        }
    }
}
