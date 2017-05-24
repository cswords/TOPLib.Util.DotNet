using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace TOPLib.Util.DotNet.Debug
{
    public class Logger
    {
        private LogFile _LoggerFile;

        internal LogFile LoggerFile
        {
            get
            {
                return _LoggerFile;
            }
        }

        public Logger(string logName = null)
        {
            this._LoggerFile = new LogFile(LogFolder, (logName ?? AppName) + ".log");
        }

        private static string AppName
        {
            get
            {
                var mainModule = Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName);
                var lastDot = mainModule.LastIndexOf('.');
                return mainModule.Substring(0, lastDot);
            }
        }

        #region Global loggers: Default(Today), Today, Single
        private static string _logFolder;

        public static string LogFolder
        {
            set
            {
                if (!Directory.Exists(value))
                {
                    if (!File.Exists(value))
                        Directory.CreateDirectory(value);
                    else
                        throw new Exception("There is a file at this folder position");
                }
                _logFolder = value;
            }
            get
            {
                if (_logFolder == null)
                    _logFolder = Directory.GetCurrentDirectory();
                return _logFolder;
            }
        }

        private static Logger _Default;

        public static Logger Default
        {
            get
            {
                if (_Default == null)
                    _Default = Today;
                return _Default;
            }
            set
            {
                _Default = value;
            }
        }

        private static Logger _Today;

        public static Logger Today
        {
            get
            {
                if (_Today == null)
                {
                    _Today = new Logger(AppName + DateTime.Today.ToString("-yyyy-MM-dd"));
                }
                return _Today;
            }
        }

        private static Logger _Single;

        public static Logger Single
        {
            get
            {
                if (_Single == null)
                    _Single = new Logger();
                return _Single;
            }
        }
        #endregion
    }
}