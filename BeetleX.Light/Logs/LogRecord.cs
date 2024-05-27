using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX.Light.Logs
{
    public class LogRecord
    {
        public LogLevel Level { get; set; }

        public int ThreadID { get; set; }

        public DateTime DateTime { get; set; }

        public string Location { get; set; }

        public string Model { get; set; }

        public string Tag { get; set; }

        public string Message { get; set; }

        public string StackTrace { get; set; }

        public LogRecord Reset()
        {
            this.StackTrace = null;
            return this;
        }
    }
}
