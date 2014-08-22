using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TOPLib.Util.DotNet.Persistence.Util
{

    public class ContextItem : IDisposable
    {
        internal ContextItem(string name, IDisposable obj)
        {
            this.Name = name; this.Obj = obj;
        }

        public string Name { get; private set; }

        public IDisposable Obj { get; private set; }

        public static ContextCollection operator +(ContextItem a, ContextItem b)
        {
            return new ContextCollection(a, b);
        }

        public void Dispose()
        {
            if (Obj != null)
                this.Obj.Dispose();
        }

        public IDisposable this[string name]
        {
            get
            {
                if (this.Name == name)
                    return this.Obj;
                else
                    return null;
            }
        }
    }

    public class ContextCollection : IDisposable, IEnumerable<ContextItem>
    {

        internal IList<ContextItem> contextItemList = new List<ContextItem>();

        internal ContextCollection(params ContextItem[] items)
        {
            foreach (var item in items)
            {
                var l = contextItemList.Where(i => i.Name == item.Name);
                while (l.Count() > 0)
                {
                    contextItemList.Remove(l.First());
                }
                contextItemList.Add(item);
            }
        }

        internal ContextCollection(ContextCollection left, ContextItem right)
        {
            foreach (var item in left.contextItemList)
            {
                contextItemList.Add(item);
            }
            var l = contextItemList.Where(i => i.Name == right.Name);
            while (l.Count() > 0)
            {
                contextItemList.Remove(l.First());
            }
            contextItemList.Add(right);
        }

        public static ContextCollection operator +(ContextCollection a, ContextItem b)
        {
            return new ContextCollection(a, b);
        }

        public void Dispose()
        {
            foreach (var item in contextItemList)
            {
                item.Dispose();
            }
        }

        public IEnumerator<ContextItem> GetEnumerator()
        {
            return contextItemList.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return contextItemList.GetEnumerator();
        }

        public IDisposable this[string key]
        {
            get
            {
                var l = contextItemList.Where(i => i.Name == key);
                if (l.Count() > 0)
                {
                    return l.First();
                }
                else
                {
                    return null;
                }
            }
        }
    }

    public static class IDisposableExtension
    {
        public static ContextItem As(this IDisposable obj, string name)
        {
            return new ContextItem(name, obj);
        }

        public static ContextItem<T> As<T>(this T obj, string name)
            where T : IDisposable
        {
            return new ContextItem<T>(name, obj);
        }
    }

#region generic

    public class ContextItem<T> : IDisposable
        where T : IDisposable
    {
        internal ContextItem(string name, T obj)
        {
            this.Name = name; this.Obj = obj;
        }

        public string Name { get; private set; }

        public T Obj { get; private set; }

        public static ContextCollection<T> operator +(ContextItem<T> a, ContextItem<T> b)
        {
            return new ContextCollection<T>(a, b);
        }

        public void Dispose()
        {
            if (Obj != null)
                this.Obj.Dispose();
        }
        public T this[string name]
        {
            get
            {
                if (this.Name == name)
                    return this.Obj;
                else
                    return default(T);
            }
        }
    }

    public class ContextCollection<T> : IDisposable, IEnumerable<ContextItem<T>>
        where T : IDisposable
    {

        internal IList<ContextItem<T>> contextItemList = new List<ContextItem<T>>();

        internal ContextCollection(params ContextItem<T>[] items)
        {
            foreach (var item in items)
            {
                var l = contextItemList.Where(i => i.Name == item.Name);
                while (l.Count() > 0)
                {
                    contextItemList.Remove(l.First());
                }
                contextItemList.Add(item);
            }
        }

        internal ContextCollection(ContextCollection<T> left, ContextItem<T> right)
        {
            foreach (var item in left.contextItemList)
            {
                contextItemList.Add(item);
            }
            var l = contextItemList.Where(i => i.Name == right.Name);
            while (l.Count() > 0)
            {
                contextItemList.Remove(l.First());
            }
            contextItemList.Add(right);
        }

        public static ContextCollection<T> operator +(ContextCollection<T> a, ContextItem<T> b)
        {
            return new ContextCollection<T>(a, b);
        }

        public void Dispose()
        {
            foreach (var item in contextItemList)
            {
                item.Dispose();
            }
        }

        public IEnumerator<ContextItem<T>> GetEnumerator()
        {
            return contextItemList.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return contextItemList.GetEnumerator();
        }

        public T this[string key]
        {
            get
            {
                var l = contextItemList.Where(i => i.Name == key);
                if (l.Count() > 0)
                {
                    return l.First().Obj;
                }
                else
                {
                    return default(T);
                }
            }
        }
    }
#endregion
}
