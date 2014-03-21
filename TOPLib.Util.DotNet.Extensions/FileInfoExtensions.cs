using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace TOPLib.Util.DotNet.Extensions
{
    public static class FileInfoExtensions
    {
        public static FileInfo NumberIfFileNameExists(this FileInfo original)
        {
            if (original.Exists)
            {
                FileInfo result = null;
                var oname = original.Name;
                var match = Regex.Match(oname, @"\S\((<cur>\d+)\)" + Regex.Escape(original.Extension) + "$");
                if (match.Success)
                {
                    var cur = int.Parse(match.Groups["cur"].Value);
                    var name = oname.Substring(0, oname.Length - match.Value.Length) + " (" + (cur + 1).ToString() + ")" + original.Extension;
                    result = new FileInfo(Path.Combine(original.DirectoryName, name));
                }
                else
                {
                    var name = oname.Substring(0, oname.Length - original.Extension.Length) + " (1)" + original.Extension;
                    result = new FileInfo(Path.Combine(original.DirectoryName, name));
                }
                return result;
            }
            else
            {
                return original;
            }
        }
    }
}
