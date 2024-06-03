using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX.Light.Extension
{
    public class NetworkStatistics
    {
        public SecondsCounter SendIO { get; private set; } = new SecondsCounter("SendIO");

        public SecondsCounter ReceiveIO { get; private set; } = new SecondsCounter("ReceiveIO");

        public SecondsCounter SendBytes { get; private set; } = new SecondsCounter("SendBytes", 1024 * 1024, "MB");

        public SecondsCounter Receiveytes { get; private set; } = new SecondsCounter("ReceiveBytes", 1024 * 1024, "MB");

        public SecondsCounter Connections { get; private set; } = new SecondsCounter("Connections");

        public SecondsCounter NetWorkIO { get; private set; } = new SecondsCounter("NetWorkIO");

        public SecondsCounter NetWorkBytes { get; private set; } = new SecondsCounter("NetWorkBytes", 1024 * 1024, "MB");

        public SecondsCounter.Value[] GetValues()
        {
            return new SecondsCounter.Value[]
            {
                Connections.GetValue(),
                SendIO.GetValue(),
                SendBytes.GetValue(),
                ReceiveIO.GetValue(),
                Receiveytes.GetValue(),
                NetWorkIO.GetValue(),
                NetWorkBytes.GetValue()
            };
        }

        public void OutputToSB(StringBuilder sb)
        {
            _sb.AppendLine("");
            _sb.AppendLine("".PadLeft(29, '-') + "NetworkStatistics" + "".PadLeft(30, '-'));
            var values = GetValues();
            foreach (var item in values)
            {
                _sb.AppendLine("-" + item.ToString() + " -");

            }
            _sb.AppendLine("".PadLeft(76, '-'));
        }


        private StringBuilder _sb = new StringBuilder();
        public override string ToString()
        {
            _sb.Clear();
            OutputToSB(_sb);
            return _sb.ToString();
        }
    }
}
