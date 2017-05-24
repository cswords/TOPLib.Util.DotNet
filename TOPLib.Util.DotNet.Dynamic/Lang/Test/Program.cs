using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TOPLib.Util.DotNet.Dynamic.Lang.Test
{
    public class Program
    {
        #region just hide
        //public static void Main2(string[] args)
        //{
        //    //I found ExpandoObject is implemented as Dynamic, but it is sealed and do not support Property setter and getter.
        //    dynamic employee, manager;

        //    employee = new ExpandoObject();
        //    employee.Name = "John Smith";
        //    employee.Age = 33;

        //    manager = new ExpandoObject();
        //    manager.Name = "Allison Brown";
        //    manager.Age = 42;
        //    manager.TeamSize = 10;

        //    Console.ReadKey();
        //}

        //public static void Main1(string[] args)
        //{
        //    dynamic test1 = new Dynamic(null);
        //    test1.Hello="World";
        //    test1.Print = new Func<string, string>((i) => { var a = i + " " + test1.Hello; Console.WriteLine(a); return a; });
        //    test1.Print("Hello");
        //    ((Dynamic)test1).SetProperty("IntValue", 34);
        //    var iTest1 = ((Dynamic)test1).AS<ITest>();
        //    test1.Hello = "Shit";
        //    iTest1.Print("Hello");

        //    var test2O = new TestImpl();
        //    dynamic test2 = new Dynamic(test2O);
        //    test2.Print("holoshit");
        //    test2.LongValue = (Int64)10;

        //    var iTest2=((Dynamic)test2).AS<ITest>();

        //    iTest2.Print("holoholoshit");

        //    test2.LongValue+=12;
        //    Console.WriteLine(test2O.LongValue);

        //    ((Dynamic)test1).SetProperty("IntValue", 34);
        //    var iTest3 = ((Dynamic)test1).AS<ITest>();
        //    iTest3.Print("mmmmm");


        //    Console.ReadKey();
        //}
        #endregion

        public static void Main(string[] args)
        {
            dynamic testD = new Dynamic(null);
            testD.Print = new Func<string, string>((o) =>
            {
                var result = "Hello " + o;
                Console.WriteLine(result);
                return result;
            });

            ((Dynamic)testD).SetProperty<object>("ObjValue", 1234);
            var testI = ((Dynamic)testD).As<ITest>();

            var a = testI.ObjValue;
            var b = testI.Print("dodo");

            ((Dynamic)testD).SetProperty<object>("ObjValue", null, () =>
            {
                return DateTime.Now;
            });

            a = testI.ObjValue;

            Console.WriteLine(int.MaxValue / 3650000);

            Console.ReadKey();
        }
    }

    public interface ITest
    {
        string Print(string what);

        object ObjValue { get; }// it makes suck when the property is int, probably for most value types.

    }

    public class TestImpl : ITest
    {
        public string Print(string what)
        {
            Console.WriteLine("Haha " + what);
            return "Hehe " + what;
        }

        public object ObjValue { get; set; }

        public long LongValue;
    }
}
