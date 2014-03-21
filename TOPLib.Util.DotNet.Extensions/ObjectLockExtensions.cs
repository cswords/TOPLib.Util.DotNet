using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TOPLib.Util.DotNet.Extensions
{
    public static class ObjectLockExtensions
    {
        private static IDictionary<object, Lock> locks = new Dictionary<object, Lock>();

        private static IDictionary<object, object> LockRefs
        {
            get
            {
                var l = from kv in locks
                        select new KeyValuePair<object, object>(kv.Key, kv.Value.Reference);
                var result = new Dictionary<object, object>();
                foreach (var o in l)
                {
                    result.Add(o.Key, o.Value);
                }
                return result;
            }
        }

        public static Lock GetLock(this object o)
        {
            lock (locks)
            {
                if (!locks.Keys.Contains(o))
                {
                    locks[o] = new Lock();
                }
                return locks[o];
            }
        }

        public static void Lock(this object o, Action action, object reference = null)
        {
            if (o.GetType().IsValueType) throw new LockException(o);
            var lObj = o.GetLock();
            lock (lObj)
            {
                lObj.Reference = (reference == null) ? (new object()) : reference;
                action();
                lObj.Reference = null;
            }
        }

        public static R Lock<R>(this object o, Func<R> action, object reference = null)
        {
            if (o.GetType().IsValueType) throw new LockException(o);
            var lObj = o.GetLock();
            lock (lObj)
            {
                lObj.Reference = (reference == null) ? (new object()) : reference;
                var result = action();
                lObj.Reference = null;
                return result;
            }
        }

        public static bool CheckLocked(this object o)
        {
            return (o.GetLock().Reference == null);
        }
    }

    public class Lock
    {
        public object Reference { get; internal set; }
    }

    public class LockException : Exception
    {
        public LockException(object o)
        {
            this.Data.Add("Object", o);
        }

        public override string Message
        {
            get
            {
                return "Value type object cannot be locked.";
            }
        }
    }
}
