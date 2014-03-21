using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace TOPLib.Util.DotNet.Persistence.Db
{
    public static class QueryExtensions
    {
        internal static string GetSelectClause<T>(this T query, string[] resultFields, params string[] additionalFields)
            where T : QueryBase<T>, ISelectable
        {
            LinkedList<QueryField> fields = new LinkedList<QueryField>();
            foreach (var fieldStr in resultFields)
            {
                fields.AddLast(QueryField.Parse(fieldStr));
            }
            var result = string.Empty;
            if (fields.Count > 0)
                result+= "SELECT " + string.Join(", ", fields.Select(f => f.ToString(query.From.Db.DbType)));
            else
                result += "SELECT *";
            foreach (var fieldStr in additionalFields)
            {
                result += ", " + fieldStr.Trim();
            }
            return result;
        }

        internal static string GetFromClause<T>(this T query)
            where T : QueryBase<T>
        {
            return "FROM " + query.From.TextClause;
        }

        internal static string GetWhereClause<T>(this T query, Constraint additionalConstraint = null)
            where T : QueryBase<T>
        {
            if (query.WhereClause != null & additionalConstraint != null)
            {
                var c = query.WhereClause & additionalConstraint;
                return "WHERE " + c.ToString();
            }
            else if (query.WhereClause != null)
            {
                return "WHERE " + query.WhereClause.ToString();
            }
            else if (additionalConstraint != null)
            {
                return "WHERE " + additionalConstraint.ToString();
            }
            else return string.Empty;
        }

        internal static string GetOrderByClause(this SortedQuery query)
        {
            string sql = string.Empty;
            if (query.OrderByClause != null)
            {
                if (query.OrderByClause.Count > 0)
                {
                    sql += "ORDER BY";
                    foreach (var pair in query.OrderByClause)
                    {
                        if (query.From.Db.DbType == BAM.DataBaseType.MsSql)
                        {
                            if (!pair.Key.StartsWith("[") & !pair.Key.StartsWith("]"))
                                sql += " [" + pair.Key + "]" + (pair.Value ? string.Empty : " desc");
                            else
                                sql += " " + pair.Key + (pair.Value ? string.Empty : " desc");
                        }
                        if (query.From.Db.DbType == BAM.DataBaseType.MySql)
                        {
                            if (!pair.Key.StartsWith("`") & !pair.Key.StartsWith("`"))
                                sql += " `" + pair.Key + "`" + (pair.Value ? string.Empty : " desc");
                            else
                                sql += " " + pair.Key + (pair.Value ? string.Empty : " desc");
                        }
                    }
                }
            }
            return sql;
        }

        internal static string GetOrderByClause(this FetchedQuery query)
        {
            if (query.BaseQuery is SortedQuery)
            {
                return (query.BaseQuery as SortedQuery).GetOrderByClause();
            }
            else
            {
                return null;
            }
        }

        public static string ToSelectQuery<T>(this T query, params string[] resultFields)
            where T : QueryBase<T>, ISelectable
        {
            string sql = string.Empty;
            if (query is FetchedQuery)
            {
                if (query.From.Db.DbType == BAM.DataBaseType.MySql)
                {
                    sql = sql.TrimEnd() + query.GetSelectClause(resultFields);

                    sql = sql.TrimEnd() + " " + query.GetFromClause();
                    sql = sql.TrimEnd() + " " + query.GetWhereClause();

                    if (query is SortedQuery)
                        sql = sql.TrimEnd() + " " + (query as SortedQuery).GetOrderByClause();

                    sql = sql.TrimEnd() + " LIMIT " + (query as FetchedQuery).Take + " OFFSET " + (query as FetchedQuery).Skip;
                }
                if (query.From.Db.DbType == BAM.DataBaseType.MsSql)
                {

                    if (((query as FetchedQuery).BaseQuery is SortedQuery))
                    {
                        sql = sql.TrimEnd() + query.GetSelectClause(resultFields,
                            "ROW_NUMBER() OVER (" + ((query as FetchedQuery).BaseQuery as SortedQuery).GetOrderByClause() + ") AS __ROWID__");
                    }
                    else
                    {
                        sql = sql.TrimEnd() + query.GetSelectClause(resultFields,
                            "ROW_NUMBER() OVER (ORDER BY (SELECT 1)) AS __ROWID__");
                    }

                    sql = sql.TrimEnd() + " " + query.GetFromClause();
                    sql = sql.TrimEnd() + " " + query.GetWhereClause();

                    sql = query.GetSelectClause(resultFields)
                        + " FROM (" + sql.TrimEnd() + ") __T__ WHERE "
                        + "__ROWID__>" + (query as FetchedQuery).Skip + " AND __ROWID__<=" + ((query as FetchedQuery).Skip + (query as FetchedQuery).Take);
                }
            }
            else
            {
                sql += query.GetSelectClause(resultFields);
                sql += " " + query.GetFromClause();

                sql += " " + query.GetWhereClause();

                if (query is SortedQuery)
                    sql += " " + (query as SortedQuery).GetOrderByClause();


            }
            sql = sql.TrimEnd() + ";";
            return sql;
        }
    }

    public static class SelectableExtension
    {

        public static IEnumerable<IDictionary<string, object>> Select<T>(this QueryBase<T> query, params string[] resultFields)
            where T : QueryBase<T>,ISelectable
        {
            var to = query.From.Db.Extract(((T)query).ToSelectQuery(resultFields));
            return BAM.ToLocalData(to);
        }

        public static IEnumerable<IRowSchema> GetSchema<T>(this T query, params string[] resultFields)
            where T : QueryBase<T>, ISelectable
        {
            var schemaTable = query.From.Db.GetSchemaTable(query.ToSelectQuery(resultFields));
            var result = new LinkedList<IRowSchema>();
            foreach (DataRow schemaRow in schemaTable.Rows)
            {
                var row = new RowSchema(schemaRow);
                if (!row.FieldName.Equals("__ROWID__"))
                    result.AddLast(row);
            }
            return result;
        }

        public static IEnumerable<object> List<T>(this T query, string valueField)
            where T : QueryBase<T>, ISelectable
        {
            var table = query.Select(new string[] { valueField });
            var result = table.Select(d => d.First().Value);
            return result;
        }

        public static IEnumerable<IDictionary<string, object>> List<T>(this T query, params string[] valueFields)
            where T : QueryBase<T>, ISelectable
        {
            var result = query.Select(valueFields);
            return result;
        }

        public static void ForEachRow<T>(this T query, Action<object> action, string valueField)
            where T : QueryBase<T>, ISelectable
        {
            var table = query.From.Db.Extract(query.ToSelectQuery(new string[] { valueField }));
            foreach (var row in table.Rows)
            {
                var queryField = QueryField.Parse(valueField);

                action(table.Rows[0][queryField.Alias == null ? queryField.Field : queryField.Alias]);
            }
        }

        public static void ForEachRow<T>(this T query, Action<IDictionary<string, object>> action, params string[] valueFields)
            where T : QueryBase<T>, ISelectable
        {
            var table = query.From.Db.Extract(query.ToSelectQuery(valueFields));
            foreach (DataRow row in table.Rows)
            {
                var dict = new Dictionary<string, object>();
                foreach (var valueField in valueFields)
                {
                    var queryField = QueryField.Parse(valueField);

                    dict[queryField.Alias == null ? queryField.Field : queryField.Alias] = row[queryField.Alias == null ? queryField.Field : queryField.Alias];
                }
                action(dict);
            }
        }
    }

    public static class SortableExtension
    {
        public static SortedQuery OrderBy<T>(this T query, string field)
            where T : QueryBase<T>, ISortable
        {
            var result = new SortedQuery
            {
                BaseQuery = query,
                From = query.From,
                WhereClause = query.WhereClause
            };
            result.OrderByClause.Add(field, true);
            return result;
        }

        public static SortedQuery OrderByDesc<T>(this T query, string field)
            where T : QueryBase<T>, ISortable
        {
            var result = new SortedQuery
            {
                BaseQuery = query,
                From = query.From,
                WhereClause = query.WhereClause
            };
            result.OrderByClause.Add(field, false);
            return result;
        }

    }

    public static class UpdatableExtension
    {
        public static bool Update<T>(this T query, string valueField, object value)
            where T:QueryBase<T>,IUpdatable
        {

            string sql = "UPDATE " + query.From.TextClause + " SET ";
            sql += valueField + "='" + value + "' ";
            if (query.WhereClause != null) sql += " WHERE " + query.WhereClause;
            sql += ";";

#if DEBUG
            Console.WriteLine(sql);
#endif

            return query.From.Db.Execute(sql);
        }


        public static bool Update<T>(this T query, IDictionary<string, object> values)
            where T : QueryBase<T>, IUpdatable
        {
            if (values.Count > 0)
            {
                string sql = "UPDATE " + query.From.TextClause + " SET ";
                var sets = new LinkedList<string>();
                foreach (var pair in values)
                {
                    var field = pair.Key.Replace("[", "").Replace("]", "").Replace("`", "");

                    var value = pair.Value;

                    if (value is string)
                    {
                        value = ((string)value)
                            .Replace("'", "\\'")
                            .Replace("\"", "\\\"");
                    }

                    if (query.From.Db.DbType == TOPLib.Util.DotNet.Persistence.Db.BAM.DataBaseType.MsSql)
                    {
                        sets.AddLast("[" + field + "] = '" + value + "'");
                    }
                    else
                    {
                        sets.AddLast("`" + field + "` = '" + value + "'");
                    }
                }
                sql += string.Join(", ", sets);
                if (query.WhereClause != null) sql += " WHERE " + query.WhereClause;
                sql += ";";

#if DEBUG
                Console.WriteLine(sql);
#endif

                return query.From.Db.Execute(sql);
            }
            else return false;
        }

    }

    public static class FetchableExtension
    {
        public static FetchedQuery Fetch<T>(this T query, long skip, long take)
            where T : QueryBase<T>, IFetchable
        {
            if (query is SortedQuery)
            {
                return new FetchedQuery
                {
                    BaseQuery = query,
                    From = query.From,
                    Skip = skip,
                    Take = take,
                    WhereClause = query.WhereClause
                };
            }
            else
            {
                return new FetchedQuery
                {
                    BaseQuery = query,
                    From = query.From,
                    Skip = skip,
                    Take = take,
                    WhereClause = query.WhereClause
                };
            }
        }
    }
    
}
