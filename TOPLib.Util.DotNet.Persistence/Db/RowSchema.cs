﻿using System;
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

        public object DatabaseType
        {
            get
            {
                var rto = schemaRow["ProviderSpecificDataType"];
                var rt = rto.GetType();
                var propInfo = rt.GetProperty("UnderlyingSystemType");
                var result = propInfo.GetValue(rto, null);
                return result;
            }
        }

        public string SqlType
        {
            get { return (string)schemaRow["DataTypeName"]; }
        }

        public bool IsKey
        {
            get { return (bool)schemaRow["IsKey"]; }
        }
    }

    public interface IRowSchema
    {
        string FieldName { get; }
        Type DataType { get; }
        object DatabaseType { get; }
        string SqlType { get; }
        bool IsKey { get; }
        //int? Precision { get; }
        //int? Scale { get; }
        //int? Length { get; }
    }
}
