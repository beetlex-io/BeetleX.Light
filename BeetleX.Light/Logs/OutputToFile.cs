using BeetleX.Light.Dispatchs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX.Light.Logs
{
    public class LogOutputToFile : ILogOutputHandler, IDisposable
    {
        public LogOutputToFile()
        {
            Type = "beetlex";
            onInit();
        }
        public LogOutputToFile(string type)
        {
            Type = type;
            onInit();
        }

        private void onInit()
        {
            mLogPath = Directory.GetCurrentDirectory() +
                   Path.DirectorySeparatorChar + "logs" + Path.DirectorySeparatorChar;
            if (!Directory.Exists(mLogPath))
            {
                Directory.CreateDirectory(mLogPath);
            }
            mDispatcher = new SingleThreadDispatcher<LogRecord>(OnWriteLog);
        }
        public string Type { get; private set; }

        private string mLogPath;

        private int FileIndex = 0;

        private SingleThreadDispatcher<LogRecord> mDispatcher;

        private StreamWriter mWriter;

        private int mWriteCount;

        protected StreamWriter GetWriter()
        {
            if (mWriter == null || mWriter.BaseStream.Length > 1024 * 1024 * 20)
            {
                if (mWriter != null)
                {
                    mWriter.Flush();
                    mWriter.Close();
                }
                string filename;
                do
                {
                    filename = mLogPath + Type + "_" + DateTime.Now.ToString("yyyyMMdd") + "_" + ++FileIndex + ".txt";
                } while (File.Exists(filename));
                mWriter = new StreamWriter(filename, false, Encoding.UTF8);

            }
            return mWriter;

        }

        private Task OnWriteLog(LogRecord e)
        {
            mWriteCount++;
            StreamWriter writer = GetWriter();
            writer.Write($"[{e.Level.ToString().PadRight(6)}] [{e.DateTime:yyyy-MM-dd HH:mmm:ss}] {e.ThreadID.ToString("000")} [{e.Location.PadRight(16)} {e.Model} {e.Tag.PadLeft(30 - e.Model.Length)}] {e.Message}");
            if (!string.IsNullOrEmpty(e.StackTrace))
                writer.WriteLine($"\t\t{e.StackTrace}");
            writer.WriteLine("");
            if (mWriteCount > 200 || mDispatcher.Count == 0)
            {
                writer.Flush();
                mWriteCount = 0;
            }
            return Task.CompletedTask;
        }

        public void Write(LogRecord log)
        {
            mDispatcher.Enqueue(log);
        }

        public void Dispose()
        {
            if (mWriter != null)
            {
                mWriter.Flush();
                mWriter.Close();
            }
        }

        public void Flush()
        {
            mWriter?.Flush();
        }
    }
}
