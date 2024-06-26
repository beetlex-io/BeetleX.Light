﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX.Light.Logs
{
    public interface IGetLogHandler : ILocation
    {
        LogWriter? GetLoger(LogLevel type);

    }
    public interface ILogHandler
    {
        void WriteLog(LogLevel level, int threadid, string location, string model, string tag, string message, string stackTrace);
    }
    public struct LogWriter
    {
        public LogLevel Level { get; set; }

        public ILogHandler Loger { get; set; }


        public void Write(EndPoint location, string model, string tag, string message)
        {
            Write(location?.ToString(), model, tag, message, null);
        }

        public void WriteException(EndPoint location, string model, string tag, Exception e_)
        {
            Write(location?.ToString(), model, tag, e_.Message, e_.StackTrace);
        }

        public void Write(Socket location, string model, string tag, string message)
        {
            Write(location?.RemoteEndPoint?.ToString(), model, tag, message, null);
        }

        public void WriteException(Socket location, string model, string tag, Exception e_)
        {
            Write(location?.RemoteEndPoint?.ToString(), model, tag, e_.Message, e_.StackTrace);
        }
        public void Write(ILocation context, string model, string tag, string message)
        {
            Write(context?.EndPoint?.ToString(), model, tag, message, null);
        }

        public void WriteException(ILocation context, string model, string tag, Exception e_)
        {
            Write(context?.EndPoint?.ToString(), model, tag, e_.Message, e_.StackTrace);
        }

        public void Write(string location, string model, string tag, string message, string stackTrace)
        {
            if (string.IsNullOrEmpty(location))
                location = "BeetleX";
            Loger?.WriteLog(Level, Thread.CurrentThread.ManagedThreadId, location, model, tag, message, stackTrace);
        }
    }


    public class DefaultLoger : IGetLogHandler, ILogHandler
    {
        public EndPoint EndPoint { get; set; }

        public LogLevel LogLevel { get; set; } = LogLevel.Info;

        public LogWriter? GetLoger(LogLevel level)
        {
            if ((int)(LogLevel) <= (int)level)
            {
                LogWriter result = new LogWriter();
                result.Level = level;
                result.Loger = this;
                return result;

            }
            return null;
        }
        public List<ILogOutputHandler> LogOutputHandlers { get; private set; } = new List<ILogOutputHandler>();

        public void WriteLog(LogLevel level, int threadid, string location, string model, string tag, string message, string stackTrace)
        {
            try
            {
                if (LogOutputHandlers.Count > 0)
                {
                    LogRecord log = new LogRecord();
                    log.ThreadID = threadid;
                    log.Model = model;
                    log.Location = location;
                    log.Message = message;
                    log.Level = level;
                    log.Tag = tag;
                    log.Message = message;
                    log.StackTrace = stackTrace;
                    log.DateTime = DateTime.Now;
                    foreach (var item in LogOutputHandlers)
                        item.Write(log);
                }
            }
            catch { }
        }
    }

}
