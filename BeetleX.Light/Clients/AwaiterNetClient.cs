using BeetleX.Light.Extension;
using BeetleX.Light.Memory;
using BeetleX.Light.Protocols;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX.Light.Clients
{
    public class AwaiterNetClient<T> : NetClient
        where T : IProtocolChannel<NetClient>
    {

        private System.Collections.Concurrent.ConcurrentDictionary<Type, IAnyCompletionSource> _completionSources = new System.Collections.Concurrent.ConcurrentDictionary<Type, IAnyCompletionSource>();

        public AwaiterNetClient(string host, int port) : base(host, port)
        {

        }


        protected override void OnReceive(NetClient client, object msg)
        {
            if (msg != null)
            {
                if (_completionSources.TryGetValue(msg.GetType(), out var completionSource))
                {
                    Task.Run(() => { completionSource.Success(msg); });

                }
            }
            base.OnReceive(client, msg);
        }

        public Task<T> Request<T>(object message)
        {
            AnyCompletionSource<T> anyCompletionSource = new AnyCompletionSource<T>();
            anyCompletionSource.Token = typeof(T);
            anyCompletionSource.CompletedHandler = (o) =>
            {
                _completionSources.TryRemove((Type)o.Token, out var result);
            };
            _completionSources[typeof(T)] = anyCompletionSource;
            anyCompletionSource.SetTimeOut(TimeOut, $"{message} request timeout");
            Send(message);
            return anyCompletionSource.Task;
        }

        public static implicit operator AwaiterNetClient<T>((string, int) info)
        {
            var NetClient = new AwaiterNetClient<T>(info.Item1, info.Item2);
            return NetClient;
        }

        public static implicit operator AwaiterNetClient<T>(string uri)
        {
            Uri uriInfo = new Uri(uri);
            var NetClient = new AwaiterNetClient<T>(uriInfo.Host, uriInfo.Port);
            return NetClient;
        }
        protected override void OnDisconnect(Exception error)
        {
            base.OnDisconnect(error);
            try
            {
                foreach (var item in _completionSources.Values)
                {
                    Task.Run(() =>
                    {
                        try
                        {
                            item.Error(error);
                        }
                        catch { }
                    });

                }
            }
            catch
            {

            }
            _completionSources.Clear();
        }
    }
}
