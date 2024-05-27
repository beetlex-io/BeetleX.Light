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
}
