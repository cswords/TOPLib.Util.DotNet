using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace TOPLib.Util.DotNet.Dynamic.Test
{
    [TestClass]
    public class TestLang
    {
        [TestMethod]
        public void TestNullReplaceMent()
        {
            Exception ex = null;

            ITest resultObj = null;

            dynamic testObj = new Lang.Dynamic();

            try
            {
                testObj.Hello = new Func<string, string>((o) =>
                {
                    var result = "Hello " + o;
                    return result;
                });
            }
            catch (Exception e)
            {
                ex = e;
            }
            Assert.IsNull(ex);
            ex = null;

            try
            {
                testObj.Target = "world";
                resultObj = Lang.DynamicCaster.As<ITest>(testObj);
            }
            catch (Exception e)
            {
                ex = e;
            }
            Assert.IsNotNull(ex);
            ex = null;

            try
            {
                ((Lang.Dynamic)testObj).SetProperty("Target", "world");
                resultObj = Lang.DynamicCaster.As<ITest>(testObj);
            }
            catch (Exception e)
            {
                ex = e;
            }
            Assert.IsNotNull(ex);
            ex = null;

            try
            {
                ((Lang.Dynamic)testObj).SetProperty<object>("Target", "world");
                resultObj = Lang.DynamicCaster.As<ITest>(testObj);
                Assert.AreEqual("Hello world", resultObj.Hello((string)resultObj.Target));
            }
            catch (Exception e)
            {
                ex = e;
            }
            Assert.IsNull(ex);
            ex = null;

            try
            {
                ((Lang.Dynamic)testObj).SetProperty<object>("Target", 3);
                resultObj = Lang.DynamicCaster.As<ITest>(testObj);
                Assert.AreEqual("Hello 3", resultObj.Hello(resultObj.Target.ToString()));
            }
            catch (Exception e)
            {
                ex = e;
            }
            Assert.IsNull(ex);
            ex = null;

            try
            {
                ((Lang.Dynamic)testObj).SetProperty<object>("Target",
                    null,
                    () =>
                    {
                        return "world";
                    });
                resultObj = Lang.DynamicCaster.As<ITest>(testObj);
                Assert.AreEqual("Hello world", resultObj.Hello((string)resultObj.Target));
            }
            catch (Exception e)
            {
                ex = e;
            }
            Assert.IsNull(ex);
            ex = null;
        }
    }

    public interface ITest
    {
        string Hello(string what);

        object Target { get; }// it makes suck when the property is int, probably for most value types.

    }

    public class TestImpl : ITest
    {
        public string Hello(string what)
        {
            return "Hehe " + what;
        }

        public object Target { get; set; }

        public long LongValue;
    }
}
