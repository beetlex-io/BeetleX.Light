using BeetleX.Light.Dispatchs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX.Light.Logs
{
    public class LogOutputToConsole : ILogOutputHandler
    {
        public LogOutputToConsole()
        {
            Console.OutputEncoding = Encoding.UTF8;
            mDispatcher = new SingleThreadDispatcher<LogRecord>(OnWriteLog);
        }

        private SingleThreadDispatcher<LogRecord> mDispatcher;

        private object mLockConsole = new object();
        private Task OnWriteLog(LogRecord e)
        {
            Console.Write($"[{DateTime.Now.ToString("HH:mmm:ss")}] ");
            switch (e.Level)
            {
                case LogLevel.Error:
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    break;
                case LogLevel.Warring:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case LogLevel.Fatal:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case LogLevel.Info:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;
                case LogLevel.Debug:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
            }
            Console.Write($"[{e.Level.ToString().PadRight(6)}]");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write($" {e.ThreadID.ToString().PadLeft(3)} [{e.Location.PadRight(16)} {e.Model} {e.Tag.PadLeft(26 - e.Model.Length)}] {e.Message}");
            if (!string.IsNullOrEmpty(e.StackTrace))
                Console.Write($"\r\n\t\t{e.StackTrace}\r\n");
            else
                Console.Write("\r\n");
            return Task.CompletedTask;
        }

        public void Write(LogRecord log)
        {
            mDispatcher.Enqueue(log);
        }

        public void Flush()
        {

        }
    }
}
