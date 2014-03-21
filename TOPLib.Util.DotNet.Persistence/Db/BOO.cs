using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace TOPLib.Util.DotNet.Persistence.Db
{
    public static class BOO
    {
        public static Constraint L(string exp)
        {
            var result = new SingleConstraint(exp);
            return result;
        }

        public static Constraint L(this object o, string exp)
        {
            return L(exp);
        }

        public static Constraint M(string left, object right)
        {
            return L(left + " = '" + right.ToString() + "'");
        }

        public static Constraint M(this object o, string left, object right)
        {
            return L(left + " = '" + right.ToString() + "'");
        }

        public static bool ImportCSV(this BAM db, FileInfo file, string table, string timeStampField, string rowIdField, string filenameField)
        {
            if (db.DbType == TOPLib.Util.DotNet.Persistence.Db.BAM.DataBaseType.MsSql)
            {
                var exist = db.DetectTable(table);
                var sql = string.Empty;
                //sql += "EXEC master.dbo.sp_MSset_oledb_prop N'Microsoft.ACE.OLEDB.12.0' , N'DynamicParameters' , 1;";
                if (exist)
                {
                    sql += "INSERT INTO " + table;
                    sql += " SELECT ROW_NUMBER() OVER(ORDER BY GETDATE()) AS [" + rowIdField + "],GETDATE() AS [" + timeStampField + "],'" + file.FullName + "' AS [" + filenameField + "], *";
                }
                else
                {
                    sql += "SELECT ROW_NUMBER() OVER(ORDER BY GETDATE()) AS RowID,GETDATE() AS DateImport, * INTO " + table;
                }
                sql += " FROM OPENDATASOURCE ('Microsoft.ACE.OLEDB.12.0', 'Data Source=";
                sql += file.DirectoryName;
                sql += ";Extended Properties=\"Text;HDR=Yes;FMT=Delimited\"' )...[" + file.Name.Replace(".", "#") + "];";
                return db.Execute(sql);
            }
            else throw new NotSupportedException("MySQL is not supported yet.");
        }
    }
}
