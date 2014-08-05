using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace TOPLib.Util.DotNet.Persistence.Db
{
    internal class RowSchema : IRowSchema
    {
        private DataRow schemaRow;

        public RowSchema(DataRow schemaRow)
        {
            this.schemaRow = schemaRow;
        }

        public string FieldName
        {
            get { return (string)schemaRow["ColumnName"]; }
        }

        public Type DataType
        {
            get { return (Type)schemaRow["DataType"]; }
        }

        public string SqlType
        {
            get
            {
                var result = (string)schemaRow["DataTypeName"];
                if (result == "binary"
                   || result == "char"
                   || result == "nchar"
                   || result == "nvarchar"
                   || result == "varchar"
                   || result == "varbinary"
                  )
                    result += "(" + (schemaRow["ColumnSize"].ToString() == "2147483647" ? "max" : schemaRow["ColumnSize"].ToString()) + ")";
                else if (result == "datetime2"
                   || result == "datetimeoffset"
                   || result == "time"
                  )
                    result += "(" + schemaRow["NumericScale"].ToString() + ")";
                else if (result == "decimal"
                   || result == "numeric"
                  )
                    result += "(" + schemaRow["NumericPrecision"].ToString() + "," + schemaRow["NumericScale"].ToString() + ")";

                result = result.ToUpper();
                return result;
            }
        }

        public bool IsKey
        {
            get { return (bool)schemaRow["IsKey"]; }
        }


        public bool IsHidden
        {
            get { return (bool)schemaRow["IsHidden"]; }
        }
    }

    public interface IRowSchema
    {
        string FieldName { get; }
        Type DataType { get; }
        string SqlType { get; }
        bool IsKey { get; }
        bool IsHidden { get; }
        //int? Precision { get; }
        //int? Scale { get; }
        //int? Length { get; }
    }
}
