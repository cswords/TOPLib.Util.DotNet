using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TOPLib.Util.DotNet.Debug
{
    public static class LoggerExtensions
    {
        public static void WriteLine(this Logger logger, string text)
        {
            logger.LoggerFile.WriteLine(text);
        }

        public static void Write(this Logger logger, string text, Exception e)
        {
            const string newLine = "\r\n ";
            var msg = text;
            var ie = e; // iterate the inner exceptions
            while (ie != null)
            {
                msg += newLine + (ie == e ? "E" : "Inner e") + "xception: " + ie.GetType().FullName;
                msg += newLine + ie.Message;
                if (ie.StackTrace != null)
                    msg += newLine + ie.StackTrace;
                ie = ie.InnerException;
            }
            logger.LoggerFile.WriteLine(msg);
        }
    }
}
