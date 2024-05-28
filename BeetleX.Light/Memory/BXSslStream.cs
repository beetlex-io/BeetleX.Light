using BeetleX.Light.Clients;
using BeetleX.Light.Logs;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX.Light.Memory
{
    public class BXSslStream : SslStream
    {
        public BXSslStream(Stream innerStream, bool leaveInnerStreamOpen) : base(innerStream, leaveInnerStreamOpen)
        {
            this.OnlySequenceAdapterStream = new ReadOnlySequenceAdapterStream();
        }
        public BXSslStream(Stream innerStream, bool leaveInnerStreamOpen,
            RemoteCertificateValidationCallback? userCertificateValidationCallback,
            LocalCertificateSelectionCallback? userCertificateSelectionCallback)
        : base(innerStream, leaveInnerStreamOpen, userCertificateValidationCallback, userCertificateSelectionCallback)
        {
            this.OnlySequenceAdapterStream = new ReadOnlySequenceAdapterStream();

        }


        public IGetLogHandler LogHandler { get; set; }
        public ReadOnlySequenceAdapterStream OnlySequenceAdapterStream { get; set; }

        public override void Write(byte[] buffer, int offset, int count)
        {
            base.Write(buffer, offset, count);
            LogHandler?.GetLoger(LogLevel.Debug)?.Write(LogHandler, "BXSslStream", "SyncData", $"Write length {count}");
            LogHandler?.GetLoger(Logs.LogLevel.Trace)?.Write(LogHandler, "BXSslStream", "✉ SyncData", $"Write {Convert.ToHexString(buffer, offset, count)}");
        }

        private bool _disposed = false;

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                try
                {
                    _disposed = true;
                    OnlySequenceAdapterStream?.Dispose();
                    LogHandler?.GetLoger(LogLevel.Debug)?.Write(LogHandler, "BXSslStream", "\u2714 Disposed", "");
                }
                catch (Exception e_)
                {
                    LogHandler?.GetLoger(LogLevel.Warring)?.WriteException(LogHandler, "BXSslStream", "Disposed", e_);
                }
                finally
                {
                    LogHandler = null;
                }
            }
        }
        public async Task SyncData<T>(T context, Action<T> completed)
            where T : IGetLogHandler, ILocation, INetContext
        {
            while (true)
            {
                try
                {
                    var memory = OnlySequenceAdapterStream.GetWriteMemory(1024 * 4);
                    int len = await ReadAsync(memory);
                    if (len > 0)
                    {
                        context.GetLoger(Logs.LogLevel.Debug)?.Write(context, "BXSslStream", "SyncData", $"Read length {len}");
                        context.GetLoger(Logs.LogLevel.Trace)?.Write(context, "BXSslStream", "✉ SyncData", $"Read {Convert.ToHexString(memory.Slice(0, len).Span)}");
                        OnlySequenceAdapterStream.WriteAdvance(len);
                        OnlySequenceAdapterStream.Flush();
                        ((PipeSpanSequenceNetStream)InnerStream).ReaderAdvanceTo();

                        completed(context);
                    }
                    else
                    {
                        ((PipeSpanSequenceNetStream)InnerStream).ReaderAdvanceTo();
                        await Task.Delay(Constants.ReceiveZeroDelayTime);
                        if (!_disposed)
                        {
                            throw new BXException("sync data to OnlySequence error, receive data is 0!");
                        }
                        break;
                    }

                }
                catch (Exception e_)
                {
                    context.GetLoger(Logs.LogLevel.Debug)?.WriteException(context, "BXSslStream", "SyncData", e_);
                    context.Close(e_);
                    break;
                }
            }

        }
    }



}
