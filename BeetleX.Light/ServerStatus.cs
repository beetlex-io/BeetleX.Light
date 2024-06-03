using BeetleX.Light.Extension;
using BeetleX.Light.Memory;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX.Light
{
    public class NetServerStatus
    {
        public NetServerStatus(INetServer server)
        {
            _netServer = server;
            _lastNextTime = TimeWatch.GetElapsedMilliseconds();
            _startTime = DateTime.Now;
            _processorCount = Environment.ProcessorCount;
            _totalMemory = Environment.WorkingSet;
            _cpuMaxTime = _processorCount * 1000;
            _process = System.Diagnostics.Process.GetCurrentProcess();
            _lastTotalProcessorTime = _process.TotalProcessorTime.Milliseconds;
            _version = typeof(BXException).Assembly.GetName().Version.ToString();
            _system = Environment.OSVersion.Platform.ToString();
            _systemVersion = Environment.OSVersion.VersionString;
        }

        private int _processorCount;

        private long _totalMemory;

        private long _cpuMaxTime;

        private Process _process;

        private double _lastTotalProcessorTime;

        private INetServer _netServer;

        private string _version;

        private long _lastNextTime;

        private int _nextStatu = 0;

        private DateTime _startTime;

        private long _lastTime;

        private string _system;

        private string _systemVersion;

        private NetServerStatusValue _statusValue = new NetServerStatusValue();
        public NetServerStatusValue Next()
        {
            if (TimeWatch.GetElapsedMilliseconds() - _lastNextTime >= 1000)
            {
                if (System.Threading.Interlocked.CompareExchange(ref _nextStatu, 1, 0) == 0)
                {
                    NetServerStatusValue value = new NetServerStatusValue();

                    TimeSpan ts = (DateTime.Now - _startTime);
                    value.RunTime = $"{(long)ts.Days}:{(long)ts.Hours}:{(long)ts.Minutes}:{(long)ts.Seconds}";
                    value.Version = _version;
                    long time = TimeWatch.GetElapsedMilliseconds();
                    double second = (double)(time - _lastTime) / 1000d;
                    _lastTime = time;
                    double cputime = _process.TotalProcessorTime.TotalMilliseconds;
                    long cpufulltime = (long)(second * _cpuMaxTime);
                    double usetime = cputime - _lastTotalProcessorTime;
                    _lastTotalProcessorTime = cputime;
                    value.Cpu = (int)(((double)usetime / (double)cpufulltime) * 10000) / 100d;
                    value.Memory = Environment.WorkingSet / 1024;
                    value.Onlines = _netServer.UserContexts.Count;
                    value.System = _system;
                    value.SystemVersion = _systemVersion;
                    value.BufferAllocatedQuantity = MemoryBlockPool.Default.AllocatedQuantity;
                    value.BufferInPoolQuantity = MemoryBlockPool.Default.Count;
                    value.Counters = _netServer.NetworkStatistics.GetValues();
                    _statusValue = value;
                    _nextStatu = 0;
                }
            }
            return _statusValue;
        }

        public void Print()
        {

        }

        public class NetServerStatusValue
        {
            public string System { get; set; }

            public long BufferAllocatedQuantity { get; set; }

            public long BufferInPoolQuantity { get; set; }

            public string SystemVersion { get; set; }
            public string RunTime { get; set; }
            public long Memory { get; set; }

            public double Cpu { get; set; }

            public string Version { get; set; }

            public long Onlines { get; set; }

            public SecondsCounter.Value[] Counters { get; set; }
        }
    }
}
