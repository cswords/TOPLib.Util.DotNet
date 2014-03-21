using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TOPLib.Util.DotNet.Persistence.Db
{
    public class QueryField
    {
        public string Table { get; set; }
        public string Field { get; set; }
        public string Alias { get; set; }
        public string ToString(BAM.DataBaseType dbType)
        {
            string result = null;
            if (dbType == TOPLib.Util.DotNet.Persistence.Db.BAM.DataBaseType.MsSql)
            {
                result = "[" + this.Field + "]";
                if (!string.IsNullOrWhiteSpace(this.Table))
                    result = "[" + Table + "]." + result;
                if (!string.IsNullOrWhiteSpace(this.Alias))
                    result += " AS [" + this.Alias + "]";
            }
            else
            {
                result = "`" + this.Field + "`";
                if (!string.IsNullOrWhiteSpace(this.Table))
                    result = "`" + Table + "`." + result;
                if (!string.IsNullOrWhiteSpace(this.Alias))
                    result += " AS `" + this.Alias + "`";
            }
            return result;
        }
        //(([^.^\s]+)\.)?([^.^\s]+)(\s+as\s+([^.^\s]+))?
        private static string QueryFieldReg = @"((?<table>[^.^\s]+)\.)?(?<field>[^.^\s]+)(\s+as\s+(?<alias>[^.^\s]+))?";

        public static QueryField Parse(string fieldStr)
        {
            var field = new QueryField();
            var match = Regex.Match(fieldStr, QueryFieldReg, RegexOptions.IgnoreCase);
            if (match.Success)
            {
                if (match.Groups["table"].Success)
                    field.Table = match.Groups["table"].Value.Replace("[", "").Replace("]", "").Replace("`", "");


                if (match.Groups["alias"].Success)
                    field.Alias = match.Groups["alias"].Value.Replace("[", "").Replace("]", "").Replace("`", "");

                field.Field = match.Groups["field"].Value.Replace("[", "").Replace("]", "").Replace("`", "");

                if (field.Alias == null) field.Alias = field.Field;
            }
            return field;
        }
    }

}
