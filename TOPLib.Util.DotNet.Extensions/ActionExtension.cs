using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace TOPLib.Util.DotNet.Extensions
{
    public static class ActionExtension
    {
        public static AutoResetEvent ThreadPoolInvoke(this Action action)
        {
            AutoResetEvent autoEvent = new AutoResetEvent(false);
            ThreadPool.QueueUserWorkItem(new WaitCallback((s) => { action.Invoke(); ((AutoResetEvent)s).Set(); }), autoEvent);

            return autoEvent;
        }
    }
}
