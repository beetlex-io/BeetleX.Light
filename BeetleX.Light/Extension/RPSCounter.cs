using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX.Light.Extension
{
    public class RPSCounter
    {
        public RPSCounter(int max)
        {
            mLastTime = TimeWatch.GetTotalSeconds();
            mMax = max;
        }

        private long mLastCount;

        private double mLastTime;

        public Value Next(long count)
        {
            double now = TimeWatch.GetTotalSeconds();
            double time = now - mLastTime;
            Value result = new Value();
            result.RPS = (long)((count - mLastCount) / time);
            result.Count = count;
            mLastCount = count;
            mLastTime = now;
            return result;
        }

        public struct Value
        {
            public long Count { get; set; }

            public long RPS { get; set; }
        }

        private int mMax;

        private long mRPSCount;

        private long mLastLimitRpsTime;

        public void SetMaxRpx(int value)
        {
            this.mMax = value;
        }

        public int LimitTime { get; set; } = 1000;

        public bool Limit(int max = 0)
        {
            if (max > 0)
                mMax = max;
            if (mMax <= 0)
                return false;
            else
            {
                mRPSCount = Interlocked.Increment(ref mRPSCount);
                long now = TimeWatch.GetElapsedMilliseconds();
                long time = now - mLastLimitRpsTime;
                if (time >= LimitTime)
                {
                    Interlocked.Exchange(ref mRPSCount, 0);
                    Interlocked.Exchange(ref mLastLimitRpsTime, now);
                }
                else
                {
                    if (mRPSCount > mMax)
                        return true;
                }
            }
            return false;
        }

    }

    public class SecondsCounter
    {
        public SecondsCounter(string name, int cardina = 1)
        {
            _name = name;
            _cardina = cardina;
        }
        public SecondsCounter(string name, int cardina, string unit)
        {
            _name = name;
            _cardina = cardina;
            _unit = unit;
        }

        public int _cardina = 1;

        private long _lastCount;

        private long _count;

        private double _lastTime;

        private string _name;

        private string _unit;

        public void Add(int data)
        {
            System.Threading.Interlocked.Add(ref _count, data);
        }

        public Value GetValue()
        {
            double now = TimeWatch.GetTotalSeconds();
            double time = now - _lastTime;
            Value result = new Value();
            result.Name = _name;
            result.SecondsCount = (long)((_count - _lastCount) / time) / _cardina;
            result.TotalCount = _count / _cardina;
            result.Unit = _unit;
            if (result.Unit == null)
                result.Unit = "";
            _lastCount = _count;
            _lastTime = now;
            return result;
        }

        public struct Value
        {
            public long TotalCount { get; set; }

            public long SecondsCount { get; set; }

            public string Name { get; set; }

            public string Unit { get; set; }

            public override string ToString()
            {
                return $"  {Name.PadRight(20)} {(SecondsCount.ToString("###,###,##0 ")).PadLeft(20)}/s [{(TotalCount.ToString("###,###,##0 ")).PadLeft(20)} {Unit.PadRight(4, ' ')}]";
            }
        }


    }
}
