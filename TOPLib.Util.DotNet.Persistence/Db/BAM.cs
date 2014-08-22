using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TOPLib.Util.DotNet.Persistence.Db
{
    public static class BAM
    {
        public static IDatabase BOO<T>(string conn)
            where T:Bamboo, new()
        {
            return new Database<T>(conn);
        }

        public static IDatabase BOO(string type, string conn)
        {
            switch (type)
            {
                case "MySQL":
                    return new Database<MySQLDb>(conn);
                case "PgSQL":
                    return new Database<PgSQLDb>(conn);
                default:
                    return new Database<MsSQLDb>(conn);
            }
        }
    }
}
