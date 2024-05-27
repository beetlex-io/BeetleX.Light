using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX.Light.Extension
{
    interface IAnyCompletionSource
    {
        void Success(object data);
        void Error(Exception error);
        void Wait(Task task, Action<Task, IAnyCompletionSource> handler);

        Task GetTask();
    }

    class AnyCompletionSource<T> : TaskCompletionSource<T>, IAnyCompletionSource
    {
        public void Success(object data)
        {
            TrySetResult((T)data);
        }

        public void Error(Exception error)
        {
            TrySetException(error);
        }


        public async void Wait(Task task, Action<Task, IAnyCompletionSource> handler)
        {
            try
            {
                await task;
                handler?.Invoke(task, this);
            }
            catch (Exception e_)
            {
                Error(e_);
            }
        }

        public async void SetTimeOut(int timeout, string message)
        {
            try
            {
                await System.Threading.Tasks.Task.Delay(timeout);
                if (!this.Task.IsCompleted)
                    TrySetException(new TimeoutException(message));
            }
            catch { }
        }

        public Task GetTask()
        {
            return this.Task;
        }


    }
}
