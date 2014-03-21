using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace TOPLib.Util.DotNet.Extensions
{
    public static class StringExtension
    {
        public static string FilePathToFileName(this string path)
        {
            string[] temp = path.Split('/', '\\');
            return temp[temp.Length - 1];
        }

        public static string Short(this string source)
        {
            source = source.Normalize().ToLower();
            switch (source)
            {
                case "northern territory":
                    return "NT";
                case "north territory":
                    return "NT";
                case "victoria":
                    return "VIC";
                case "west australia":
                    return "WA";
                case "south australia":
                    return "SA";
                case "tasmania":
                    return "TAS";
                case "queensland":
                    return "QLD";
                case "australian capital territory":
                    return "ACT";
                case "new south wales":
                    return "NSW";
            }
            return source.ToUpper();
        }

        public static string Simplify(this string source)
        {
            var result = source.Trim();
            result.Replace("\t", " ");
            result.Replace("\r", " ");
            result.Replace("\n", " ");

            while (result.Contains("  "))
            {
                result = result.Replace("  ", " ");
            }

            return result;
        }

        public static IEnumerable<string> FragmentByPairOf(this string source, string start, string end)
        {
            var result = new LinkedList<string>();
            for (int i = 0, n = 0, l = 0; i < source.Length; i++)
            {
                var rest = source.Substring(i);
                if (rest.StartsWith(start))
                {
                    if (l == 0) n = i;
                    l++;
                }
                if (rest.StartsWith(end))
                {
                    l--;
                    if (l == 0)
                        result.AddLast(source.Substring(n, i + end.Length - n));
                }
                if (l < 0) break;//prevent ****)(****** 
            }
            return result;
        }
        
        public static string AddParamToUrl(this string url, string param, string value)
        {
            string result = url;
            if (url.Contains("?"))
                result += "&";
            else
                result += "?";
            result += param + "=" + value;
            return result;
        }

        public static Expression<Func<T, TResult>> ToExpre<T, TResult>(this string exp, string paramName = "o")
        {
            var p = System.Linq.Expressions.Expression.Parameter(typeof(T), paramName);
            var e = System.Linq.Dynamic.DynamicExpression.ParseLambda(new[] { p }, typeof(TResult), exp);
            return (Expression<Func<T, TResult>>)e;
        }

    }
}
