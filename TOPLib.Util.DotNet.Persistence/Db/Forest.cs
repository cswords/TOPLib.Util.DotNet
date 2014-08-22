using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using TOPLib.Util.DotNet.Persistence.Util;

namespace TOPLib.Util.DotNet.Persistence.Db
{
    public interface ISql
    {
        string ToSQL();
    }

    public interface INoAlias<T> : ISql
        where T : IAliased<T>
    {
        T As(string alias);
    }

    public interface IAliased : ISql
    {
        string Alias { get; set; }
    }

    public interface IAliased<T> : IAliased
    {
    }

    public interface IParameterable
    {
        ReadOnlyDictionary<string, object> Parameters { get; }
        void SetParameter(string name, object value);
    }

    public interface IExecutable : ISql
    {
        void Execute(int timeout = 30);
    }

    public interface IExtractable : ISql, IRight
    {
        DataTable Extract(int timeout=30);
        IFillingQuery Fill(string tableName);
    }

    public interface IDatabase
    {
        string DbConnStr { get; }
        ISqlContext CreateContext();
        bool DetectTable(string objectName);        
    }

    public interface ISqlContext : IDisposable, IParameterable
    {
        IDatabase Db { get; }
        ITable this[string name] { get; }
        DataTable Extract(string sql, int timeout = 30);
        DataTable GetSchemaTable(string sql, int timeout = 30);
        bool Execute(string sql, int timeout = 30);
    }

    public interface IRight : ISql { }

    public interface ITable : INoAlias<IAliasedTable>, IQueryBase, IRight
    {
        string Name { get; }

        IWriteOnly ToInsert { get; }

        IExecutable Persist(IDictionary<string, object> data);

        IEnumerable<IRowSchema> Schema { get; }
    }

    public interface IAliasedTable : IAliased<IAliasedTable>, IJoinable { }

    public interface IJoinable : IQueryBase
    {
        IJoinning<IAliasedLeftJoinning> LeftJoin(string tableName);
        IJoinning<IAliasedInnerJoinning> InnerJoin(string tableName);
        IJoinning<IAliasedCrossJoinning> CrossJoin(string tableName);

        IJoinning<IAliasedLeftJoinning> LeftJoin(IRight query);
        IJoinning<IAliasedInnerJoinning> InnerJoin(IRight query);
        IJoinning<IAliasedCrossJoinning> CrossJoin(IRight query);
    }

    public interface IJoinning<T> : ISql, INoAlias<T>
        where T : IAliased<T>
    {
        IRight Right { get; }
    }

    public interface IAliasedJoinning<T> : ISql, IAliased<T>
    {
    }

    public interface IJoinQueryBase<T> : IAliasedJoinning<T>
    {
        IJoinQuery On(Constraint constraint);
    }

    public interface IJoinQuery : IQueryBase
    {
        Constraint OnClause { get; }
    }

    public interface IAliasedLeftJoinning : IAliasedJoinning<IAliasedLeftJoinning>, IJoinQueryBase<IAliasedLeftJoinning> { }
    public interface IAliasedInnerJoinning : IAliasedJoinning<IAliasedInnerJoinning>, IJoinQueryBase<IAliasedInnerJoinning> { }
    public interface IAliasedCrossJoinning : IAliasedJoinning<IAliasedCrossJoinning>, IQueryBase { }

    public interface IQueryBase : ISql
    {
        IQuery Where(Constraint constraint);
        IQuery FilterBy(IDictionary<string, object> mapping);
        IQuery All { get; }
    }

    public interface IQuery
    {
        IGroupable GroupBy { get; }
        ISortable OrderBy { get; }
        ISelectable Select { get; }


        Constraint WhereClaus { get; }


        IWriteOnly ToUpdate(string name);
        IExecutable ToDelete(string name);
    }

    public interface IWriteOnly : IExecutable
    {
        object this[string field] { set; }
    }

    public interface IGroupable
    {
        IGrouped this[string field] { get; }
    }

    public interface IGrouped : IGroupable
    {
        ISortable OrderBy { get; }
        ISelectable Select { get; }
    }

    public interface ISortable
    {
        IAscSorted this[string field] { get; }
        IAscSorted Exp(string exp);
    }

    public interface ISorted : ISortable
    {
        ISelectable Select { get; }
    }

    public interface IAscSorted : ISorted
    {
        IDescSorted Desc { get; }
    }
    public interface IDescSorted : ISorted { }

    public interface ISingleSelectable
    {
        INoAliaseExtractable this[string field] { get; }
        INoAliaseExtractable Exp(string exp);
    }

    public interface ISelectable : ISingleSelectable
    {
        IFetchable this[IDictionary<string, string> fields] { get; }
    }

    public interface IFillingQuery:ISql//select into, insert into select (fill null to not selected fields)
    {

    }

    public interface IFetchable : IExtractable
    {
        IFetchedExtractable Fetch(long skip, long take);
    }

    public interface IFetchedExtractable : IExtractable
    {
        long Skip { get; }
        long Take { get; }
    }

    public interface INoAliaseExtractable : IFetchable, ISingleSelectable
    {
        IAliasedExtractable As(string alias);
    }

    public interface IAliasedExtractable : IFetchable, ISingleSelectable
    {
    }
}
