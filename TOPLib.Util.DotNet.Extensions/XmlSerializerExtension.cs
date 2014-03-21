using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using System.IO;
using System.Xml.Linq;
using System.Xml;

namespace TOPLib.Util.DotNet.Extensions
{
    public static class XmlSerializerExtension
    {

        public static void Serialize(this XmlSerializer x, TextWriter textWriter, object o, bool ignoreNullElements)
        {
            if (ignoreNullElements)
            {
                using (var memStream = new MemoryStream())
                {
                    var sw = new StreamWriter(memStream, Encoding.UTF8);
                    x.Serialize(sw, o);
                    memStream.Seek(0, SeekOrigin.Begin);

                    using (var t = new StreamReader(memStream))
                    {
                        var line = t.ReadLine();
                        while (line != null)
                        {
                            if (!line.Contains("xsi:nil=\"true\""))
                            {
                                textWriter.WriteLine(line);
                            }
                            line = t.ReadLine();
                        }
                    }
                }
            }
            else
            {
                x.Serialize(textWriter, o);
            }
        }

        public static TextReader SerializeToReader(this XmlSerializer x, object o)
        {
            var memStream = new MemoryStream();
            x.Serialize(memStream, o);
            memStream.Seek(0, SeekOrigin.Begin);

            var result = new StreamReader(memStream);
            return result;
        }

        public static XDocument SerializeToXDom<T>(this T obj, XmlSerializerNamespaces ns = null)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                XmlSerializer s = new XmlSerializer(typeof(T));
                s.Serialize(XmlWriter.Create(stream), obj, ns);
                stream.Flush();
                stream.Seek(0, SeekOrigin.Begin);

                return XDocument.Load(stream);
            }
        }

        public static string ToString(this XDocument xdom, Encoding encoding)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                var w = XmlWriter.Create(new StreamWriter(stream, encoding));
                xdom.WriteTo(w);
                w.Flush();
                var r = new StreamReader(stream);
                stream.Flush();
                stream.Seek(0, SeekOrigin.Begin);
                return r.ReadToEnd();
            }
        }
    }
}
