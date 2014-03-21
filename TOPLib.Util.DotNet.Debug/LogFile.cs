using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TOPLib.Util.DotNet.Debug
{
    internal class LogFile
    {
        private string logFilePath;

        internal LogFile(string folder, string fileName)
        {
            this.logFilePath = Path.Combine(folder, fileName);
            if (!File.Exists(this.logFilePath))
            {
                try
                {
                    if (!Directory.Exists(folder))
                    {
                        if (File.Exists(folder))
                            throw new Exception("There is a file taking the place of given folder.");
                        else
                            Directory.CreateDirectory(folder);
                    }
                    using (var wr = File.CreateText(this.logFilePath))
                    {
                        wr.Close();
                    }
                }
                catch (Exception e)
                {
                    throw new Exception("Log file cannot be created. Please check the inner exception.", e);
                }
            }
        }

        internal void WriteLine(string text)
        {
            using (var wr = File.AppendText(this.logFilePath))
            {
                var msg = "[" + DateTime.Now + "] " + text;
                wr.WriteLine(msg);
                Console.WriteLine(msg);
                wr.Close();
            }
        }
    }
}