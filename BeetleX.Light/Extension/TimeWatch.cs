using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX.Light.Extension
{
    public class TimeWatch
    {
        static TimeWatch()
        {
            mWatch = new System.Diagnostics.Stopwatch();
            mWatch.Start();
        }

        protected static readonly System.Diagnostics.Stopwatch mWatch;

        public static double GetTotalMilliseconds()
        {
            return mWatch.Elapsed.TotalMilliseconds;
        }

        public static long GetElapsedMilliseconds()
        {
            return mWatch.ElapsedMilliseconds;
        }

        public static double GetTotalSeconds()
        {
            return mWatch.Elapsed.TotalSeconds;
        }

        public static double GetTotalMinutes()
        {
            return mWatch.Elapsed.TotalMinutes;
        }
        public static double GetTotalHours()
        {
            return mWatch.Elapsed.TotalHours;
        }

        public static double GetTotalDays()
        {
            return mWatch.Elapsed.TotalDays;
        }

    }
}
