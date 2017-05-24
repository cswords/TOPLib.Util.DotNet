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
            var msg = text;
            var ie = e;
            while (ie != null)
            {
                msg += "\r\n " + (ie == e ? "E" : "Inner e") + "xception: " + ie.GetType().FullName;
                msg += "\r\n " + ie.Message;
                if (ie.StackTrace != null)
                    msg += "\r\n " + ie.StackTrace;
                ie = ie.InnerException;
            }
            logger.LoggerFile.WriteLine(msg);
        }
    }
}
