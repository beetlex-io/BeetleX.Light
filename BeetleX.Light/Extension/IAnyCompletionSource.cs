using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX.Light.Extension
{
    public interface IAnyCompletionSource
    {
        void Success(object data);
        void Error(Exception error);
        void Wait(Task task, Action<Task, IAnyCompletionSource> handler);

        Task GetTask();

        Action<IAnyCompletionSource> CompletedHandler { get; set; }

        object Token { get; set; }

    }

    public class AnyCompletionSource<T> : TaskCompletionSource<T>, IAnyCompletionSource
    {
        public void Success(object data)
        {

            CompletedHandler?.Invoke(this);
            TrySetResult((T)data);

        }

        public void Error(Exception error)
        {
            CompletedHandler?.Invoke(this);
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
                CompletedHandler?.Invoke(this);
                if (!this.Task.IsCompleted)
                    TrySetException(new TimeoutException(message));

            }
            catch { }
            finally
            {

            }
        }

        public Task GetTask()
        {
            return this.Task;
        }

        public Action<IAnyCompletionSource> CompletedHandler { get; set; }
        public object Token { get; set; }
    }
}
