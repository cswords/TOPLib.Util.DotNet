using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace TOPLib.Util.DotNet.Dynamic.Lang
{
    public class MemberStore : IEnumerable<object>
    {
        private IList<object> collection;

        private MemberStore() { collection = new List<object>(); }

        public static MemberStore Create(object obj)
        {
            var result = new MemberStore();
            return result.Attach(obj);
        }

        public MemberStore Attach(object obj)
        {
            if (obj is Delegate)
            {
                var func = (Delegate)obj;
                var current = FindMethod(func.Method.GetParameters().Select(i => i.ParameterType).ToArray());
                if (current != null)
                    collection.Remove(current);
            }
            else
            {
                var current = FindField();
                if (current != null)
                    collection.Remove(current);
            }
            collection.Add(obj);
            return this;
        }

        public Delegate FindMethod(Type[] argumentTypes)
        {
            var typesr = argumentTypes;
            foreach (var obj in this)
            {
                if (obj is Delegate)
                {
                    var func = (Delegate)obj;
                    var match = true;
                    var typesl = func.Method.GetParameters().Select(i => i.ParameterType).ToArray();
                    if (typesl.Length != typesr.Length)
                    {
                        match = false;
                    }
                    else
                    {
                        for (int i = 0; i < typesr.Length; i++)
                        {
                            if (!typesl[i].IsAssignableFrom(typesr[i]))
                            {
                                match = false;
                                break;
                            }
                        }
                    }
                    if (match)
                    {
                        return func;
                    }
                }
            }
            return null;
        }

        public object FindField()
        {
            foreach (var obj in this)
            {
                if (!(obj is Delegate))
                    return obj;
            }
            return null;
        }

        public IEnumerator<object> GetEnumerator()
        {
            return collection.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return collection.GetEnumerator();
        }
    }

    public class MemberDictionary : IEnumerable<KeyValuePair<string, MemberStore>>
    {
        private IDictionary<string, MemberStore> dict;
        public MemberDictionary()
        {
            dict = new Dictionary<string, MemberStore>();
        }

        public MemberStore this[string key]
        {
            get
            {
                if (dict.ContainsKey(key))
                    return dict[key];
                else
                    return null;
            }
        }

        public void Attach(string name, object obj)
        {
            if (dict.ContainsKey(name))
                dict[name].Attach(obj);
            else
                dict[name] = MemberStore.Create(obj);
        }

        public IEnumerator<KeyValuePair<string, MemberStore>> GetEnumerator()
        {
            return dict.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return dict.GetEnumerator();
        }
    }

    internal abstract class PropertyStore
    {
        public abstract Type PropertyType { get; }

        public abstract object Get();
        public abstract void Set(object obj);

        public abstract Delegate GetGetter();
        public abstract Delegate GetSetter();

        public static PropertyStore Create<T>(Action<T> setter, Func<T> getter)
        {
            return new PropertyStore<T>(setter, getter);
        }

        internal abstract void Update(Delegate setter, Delegate getter);
    }

    internal class PropertyStore<T> : PropertyStore
    {
        public override Type PropertyType
        {
            get
            {
                return typeof(T);
            }
        }

        internal Action<T> Setter { get; private set; }

        public override Delegate GetSetter() { return Setter; }

        internal Func<T> Getter { get; private set; }

        public override Delegate GetGetter() { return Getter; }

        internal PropertyStore(Action<T> setter, Func<T> getter)
        {
            this.Setter = setter;
            this.Getter = getter;
        }

        public PropertyStore(FieldInfo field, object obj)
        {
            this.Setter = (v) => field.SetValue(obj, v);
            this.Getter = () => (T)field.GetValue(obj);
        }

        public override object Get()
        {
            return Getter();
        }

        public override void Set(object obj)
        {
            if (obj.GetType().IsAssignableFrom(typeof(T)))
                Setter((T)obj);
            else
                throw new Exception("Wrong type to set!!!");
        }

        internal override void Update(Delegate setter, Delegate getter)
        {
            if (setter != null)
                this.Setter = (Action<T>)setter;
            if (getter != null)
                this.Getter = (Func<T>)getter;
        }
    }
}
