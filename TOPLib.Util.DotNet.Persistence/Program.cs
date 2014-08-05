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
            var connStr = "data source=10.1.9.24;initial catalog=FLUX_CS;persist security info=True;user id=WIP_Admin;password=asdf1234;MultipleActiveResultSets=True;";
            var db = BAM.BOO<MsSQLDb>(connStr);

            var filter=new Dictionary<string, object>();
            filter["h.OrderNo"] = "SHIT-SO1408040004";

            var q=db["DOC_Order_Header"].As("h").InnerJoin("DOC_Order_Details").As("d")
                .On(BOO.L("h.OrderNo=d.OrderNo"))
                .FilterBy(filter)
                .Select["SKU"]["QtyOrdered_Each"].As("Qty");

            Console.WriteLine(q.ToSQL());

            var result = q.Extract();
            Console.WriteLine(result.Rows[0]["SKU"].ToString());
            Console.ReadKey();
        }
    }
}
