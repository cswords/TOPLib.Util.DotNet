using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TOPLib.Util.DotNet.Persistence.Db;

namespace TOPLib.Util.DotNet.Persistence
{
    class Program
    {
        public static void Main(string[] args)
        {
            var connStr = "data source=10.1.9.24;initial catalog=FileDataImport;persist security info=True;user id=WIP_Admin;password=asdf1234;MultipleActiveResultSets=True;";
            var db = BAM.BOO<MsSQLDb>(connStr);
            var q = db["Fung_temp"].All
                .OrderBy["JobNo"]//.Desc
                .Select["Job No#"].As("JobNo")["Ledger"]
                .Fetch(10, 15)
                ;


            

            //var r = q.Extract().ToLocalData();


            //Console.WriteLine(r.Count());

            Console.WriteLine(q.ToSQL());
            Console.ReadKey();
        }
    }
}
