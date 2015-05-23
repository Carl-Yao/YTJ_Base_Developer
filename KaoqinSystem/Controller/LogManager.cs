using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Globalization;
using SwipCardSystem.Medol;

namespace SwipCardSystem.Controller
{
    /// <summary>
    /// Author: spring yang
    /// Create time:2012/3/30 
    /// Log Help class
    /// </summary>
    /// <remarks>High performance log class</remarks>
    public class Log : IDisposable
    {
        //Log Message queue
        private static Queue<LogMessage> _logMessages;

        //log save directory
        private static string _logDirectory;

        //log write file state
        private static bool _state;

        //log type
        private static LogType _logType;

        //log life time sign
        private static DateTime _timeSign;

        //log file stream writer
        private static StreamWriter _writer;

        /// <summary>
        /// Wait enqueue wirte log message semaphore will release
        /// </summary>
        private Semaphore _semaphore;

        /// <summary>
        /// Single instance
        /// </summary>
        private static Log _log;

        /// <summary>
        /// Gets a single instance
        /// </summary>
        public static Log LogInstance
        {
            get { return _log ?? (_log = new Log()); }
        }

        private object _lockObjeck = new object();

        /// <summary>
        /// Initialize Log instance
        /// </summary>
        private void Initialize()
        {
            if (_logMessages == null)
            {
                _state = true;
                string logPath = System.Configuration.ConfigurationManager.AppSettings["LogDirectory"];
                _logDirectory = string.IsNullOrEmpty(logPath) ? ".\\log\\" : logPath;
                if (!Directory.Exists(_logDirectory)) Directory.CreateDirectory(_logDirectory);
                _logType = LogType.Daily;
                _lockObjeck = new object();
                _semaphore = new Semaphore(0, int.MaxValue, Constants.LogSemaphoreName);
                _logMessages = new Queue<LogMessage>();
                var thread = new Thread(Work) { IsBackground = true };
                thread.Start();
            }
        }


        /// <summary>
        /// Create a log instance
        /// </summary>
        private Log()
        {
            Initialize();
        }

        /// <summary>
        /// Log save name type,default is daily
        /// </summary>
        public LogType LogType
        {
            get { return _logType; }
            set { _logType = value; }
        }

        /// <summary>
        /// Write Log file  work method
        /// </summary>
        private void Work()
        {
            while (true)
            {
                //Determine log queue have record need wirte
                if (_logMessages.Count > 0)
                {
                    FileWriteMessage();
                }
                else
                    if (WaitLogMessage()) break;
            }
        }

        /// <summary>
        /// Write message to log file
        /// </summary>
        private void FileWriteMessage()
        {
            LogMessage logMessage = null;
            lock (_lockObjeck)
            {
                if (_logMessages.Count > 0)
                    logMessage = _logMessages.Dequeue();
            }
            if (logMessage != null)
            {
                FileWrite(logMessage);
            }
        }


        /// <summary>
        /// The thread wait a log message
        /// </summary>
        /// <returns>is close or not</returns>
        private bool WaitLogMessage()
        {
            //determine log life time is true or false
            if (_state)
            {
                WaitHandle.WaitAny(new WaitHandle[] { _semaphore }, -1, false);
                return false;
            }
            FileClose();
            return true;
        }

        /// <summary>
        /// Gets file name by log type
        /// </summary>
        /// <returns>log file name</returns>
        private string GetFilename()
        {
            DateTime now = DateTime.Now;
            string format = "";
            switch (_logType)
            {
                case LogType.Daily:
                    _timeSign = new DateTime(now.Year, now.Month, now.Day);
                    _timeSign = _timeSign.AddDays(1);
                    format = "yyyyMMdd'.log'";
                    break;
                case LogType.Weekly:
                    _timeSign = new DateTime(now.Year, now.Month, now.Day);
                    _timeSign = _timeSign.AddDays(7);
                    format = "yyyyMMdd'.log'";
                    break;
                case LogType.Monthly:
                    _timeSign = new DateTime(now.Year, now.Month, 1);
                    _timeSign = _timeSign.AddMonths(1);
                    format = "yyyyMM'.log'";
                    break;
                case LogType.Annually:
                    _timeSign = new DateTime(now.Year, 1, 1);
                    _timeSign = _timeSign.AddYears(1);
                    format = "yyyy'.log'";
                    break;
            }
            return now.ToString(format);
        }

        /// <summary>
        /// Write log file message
        /// </summary>
        /// <param name="msg"></param>
        private void FileWrite(LogMessage msg)
        {
            try
            {
                if (_writer == null)
                {
                    FileOpen();
                }
                else
                {
                    //determine the log file is time sign
                    if (DateTime.Now >= _timeSign)
                    {
                        FileClose();
                        FileOpen();
                    }
                    _writer.WriteLine(Constants.LogMessageTime + msg.Datetime);
                    _writer.WriteLine(Constants.LogMessageType + msg.Type);
                    _writer.WriteLine(Constants.LogMessageContent + msg.Text);
                    _writer.Flush();
                }
            }
            catch (Exception e)
            {
                Console.Out.Write(e);
            }
        }

        /// <summary>
        /// Open log file write log message
        /// </summary>
        private void FileOpen()
        {
            _writer = new StreamWriter(Path.Combine(_logDirectory, GetFilename()), true, Encoding.UTF8);
        }

        /// <summary>
        /// Close log file 
        /// </summary>
        private void FileClose()
        {
            if (_writer != null)
            {
                _writer.Flush();
                _writer.Close();
                _writer.Dispose();
                _writer = null;
            }
        }

        /// <summary>
        /// Enqueue a new log message and release a semaphore
        /// </summary>
        /// <param name="msg">Log message</param>
        public void Write(LogMessage msg)
        {
            if (msg != null)
            {
                lock (_lockObjeck)
                {
                    _logMessages.Enqueue(msg);
                    _semaphore.Release();
                }
            }
        }

        /// <summary>
        /// Write message by message content and type
        /// </summary>
        /// <param name="text">log message</param>
        /// <param name="type">message type</param>
        public void Write(string text, MessageType type)
        {
            Write(new LogMessage(text, type));
        }

        /// <summary>
        /// Write Message by datetime and message content and type
        /// </summary>
        /// <param name="dateTime">datetime</param>
        /// <param name="text">message content</param>
        /// <param name="type">message type</param>
        public void Write(DateTime dateTime, string text, MessageType type)
        {
            Write(new LogMessage(dateTime, text, type));
        }

        /// <summary>
        /// Write message ty exception and message type 
        /// </summary>
        /// <param name="e">exception</param>
        /// <param name="type">message type</param>
        public void Write(Exception e, MessageType type)
        {
            Write(new LogMessage(e.Message, type));
        }

        #region IDisposable member

        /// <summary>
        /// Dispose log
        /// </summary>
        public void Dispose()
        {
            _state = false;
        }

        #endregion
    }




    /// <summary>
    /// Log Type
    /// </summary>
    /// <remarks>Create log by daily or weekly or monthly or annually</remarks>
    public enum LogType
    {
        /// <summary>
        /// Create log by daily
        /// </summary>
        Daily,

        /// <summary>
        /// Create log by weekly
        /// </summary>
        Weekly,

        /// <summary>
        /// Create log by monthly
        /// </summary>
        Monthly,

        /// <summary>
        /// Create log by annually
        /// </summary>
        Annually
    }





    /// <summary>
    /// Log Message Class
    /// </summary>
    public class LogMessage
    {

        /// <summary>
        /// Create Log message instance
        /// </summary>
        public LogMessage()
            : this("", MessageType.Unknown)
        {
        }

        /// <summary>
        /// Crete log message by message content and message type
        /// </summary>
        /// <param name="text">message content</param>
        /// <param name="messageType">message type</param>
        public LogMessage(string text, MessageType messageType)
            : this(DateTime.Now, text, messageType)
        {
        }

        /// <summary>
        /// Create log message by datetime and message content and message type
        /// </summary>
        /// <param name="dateTime">date time </param>
        /// <param name="text">message content</param>
        /// <param name="messageType">message type</param>
        public LogMessage(DateTime dateTime, string text, MessageType messageType)
        {
            Datetime = dateTime;
            Type = messageType;
            Text = text;
        }

        /// <summary>
        /// Gets or sets datetime
        /// </summary>
        public DateTime Datetime { get; set; }

        /// <summary>
        /// Gets or sets message content
        /// </summary>
        public string Text { get; set; }

        /// <summary>
        /// Gets or sets message type
        /// </summary>
        public MessageType Type { get; set; }

        /// <summary>
        /// Get Message to string
        /// </summary>
        /// <returns></returns>
        public new string ToString()
        {
            return Datetime.ToString(CultureInfo.InvariantCulture) + "\t" + Text + "\n";
        }
    }


    /// <summary>
    /// Log Message Type enum
    /// </summary>
    public enum MessageType
    {
        /// <summary>
        /// unknown type 
        /// </summary>
        Unknown,

        /// <summary>
        /// information type
        /// </summary>
        Information,

        /// <summary>
        /// warning type
        /// </summary>
        Warning,

        /// <summary>
        /// error type
        /// </summary>
        Error,

        /// <summary>
        /// success type
        /// </summary>
        Success
    }
}
