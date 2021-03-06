﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;

namespace TOPLib.Util.DotNet.Persistence.Db
{
    public static class BOO
    {
        public static Constraint L(string t)
        {
            return new SingleConstraint(t);
        }

        public static IDictionary<string, string> M(params string[] fields)
        {
            var result = new Dictionary<string, string>();
            foreach (var field in fields)
            {
                if (result.ContainsKey(field))
                    result[field] = field;
                else
                    result.Add(field, field);
            }
            return result;
        }

        public static string Indentation(this string source, string space = "\t")
        {
            return space + source.Replace("\n", "\n" + space);
        }

        public static IEnumerable<IDictionary<string, object>> ToLocalData(this DataTable dataTable)
        {
            var result = new LinkedList<IDictionary<string, object>>();
            if (dataTable != null)
            {
                foreach (DataRow row in dataTable.Rows)
                {
                    var rowDict = new Dictionary<string, object>();
                    foreach (DataColumn column in dataTable.Columns)
                    {
                        rowDict[column.ColumnName] = row[column.ColumnName];
                    }
                    result.AddLast(rowDict);
                }
            }
            return result;
        }

        public static IEnumerable<IRowSchema> GetSchema(this IExtractable query, int timeout = 30)
        {
            var schemaTable = ((JointBase)query).Context.GetSchemaTable(query.ToSQL(), timeout);
            var result = new LinkedList<IRowSchema>();
            foreach (DataRow schemaRow in schemaTable.Rows)
            {
                var row = new RowSchema(schemaRow);
                if (!row.FieldName.Equals("__ROWID__") & !row.IsHidden)
                    result.AddLast(row);
            }
            return result;
        }

        public static bool ImportCSV(this IDatabase db, FileInfo file, string table, string timeStampField, string rowIdField, string filenameField)
        {
            if (db is MsSQLDb)
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
                return ((Bamboo)db).Execute(sql);
            }
            else throw new NotSupportedException("MySQL is not supported yet.");
        }

        public static string ToCSV(this DataTable table, string separator = ",", string lineSeparator = "\r\n")
        {
            string result = string.Empty;
            //header
            foreach (DataColumn col in table.Columns)
            {
                result += (result.Length == 0 ? "" : separator) + col.ColumnName;
            }
            //rows
            foreach (DataRow row in table.Rows)
            {
                result += lineSeparator;
                foreach (DataColumn col in table.Columns)
                {
                    result += (result.EndsWith(lineSeparator) ? "" : separator)
                        + (row[col] == null ? "" : row[col].ToString().Replace(separator, " "));
                }
            }
            return result;
        }

        public static IDictionary<IDictionary<string, object>, Exception> Fill(this DataTable data, ITable targetTable)
        {
            var result = new Dictionary<IDictionary<string, object>, Exception>();
            var schema = targetTable.Schema;

            var dl = data.ToLocalData();
            IExecutable i = null;
            foreach (var d in dl)
            {
                try
                {
                    i = targetTable.Persist(d);
                    i.Execute();
                    result.Add(d, new Exception("Success with: \n" + i.ToSQL()));
                }
                catch (Exception e)
                {
                    result.Add(d, new Exception(i != null ? ("Error with: \n" + i.ToSQL()) : "Please check internal error.", e));
                }
            }
            return result;
        }
    }
}
