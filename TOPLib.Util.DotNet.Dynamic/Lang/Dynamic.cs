using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;

namespace TOPLib.Util.DotNet.Dynamic.Lang
{
    public abstract class DynamicBase : DynamicObject
    {
        internal MemberDictionary MemberStore { get; private set; }

        protected DynamicBase()
        {
            this.MemberStore = new MemberDictionary();
        }

        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            if (MemberStore[binder.Name] != null)
            {
                var field = MemberStore[binder.Name].FindField();
                if (field is PropertyStore)
                {
                    var property = (PropertyStore)field;
                    property.Set(value);
                    return true;
                }
            }
            MemberStore.Attach(binder.Name, value);
            return true;
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (MemberStore[binder.Name] != null)
            {
                var field = MemberStore[binder.Name].FindField();
                if (field is PropertyStore)
                {
                    var property = (PropertyStore)field;
                    result = property.Get();
                }
                else
                {
                    result = MemberStore[binder.Name].FindField();
                }
                return true;
            }
            result = null;
            return false;
        }

        public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
        {
            if (indexes.Length == 1)
            {
                if (indexes[0] is string)
                {
                    if (MemberStore[(string)indexes[0]] != null)
                    {
                        var field = MemberStore[(string)indexes[0]].FindField();
                        if (field is PropertyStore)
                        {
                            ((PropertyStore)field).Set(value);
                        }
                    }
                    MemberStore.Attach((string)indexes[0], value);
                    return true;
                }
            }
            return false;
        }

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            result = null;
            if (indexes.Length == 1)
            {
                if (indexes[0] is string)
                {
                    if (MemberStore[(string)indexes[0]] != null)
                    {
                        var field = MemberStore[(string)indexes[0]].FindField();
                        if (field is PropertyStore)
                        {
                            result = ((PropertyStore)field).Get();
                        }
                        else
                        {
                            result = field;
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            if (MemberStore[binder.Name] == null)
            {
                result = null;
                return false;
            }
            else
            {
                var argTypes = args.Select(a => a.GetType()).ToArray();
                var func = MemberStore[binder.Name].FindMethod(argTypes);
                if (func != null)
                {
                    result = func.DynamicInvoke(args);
                    return true;
                }
                else
                {
                    result = null;
                    return false;
                }
            }
        }
    }

    /// <summary>
    /// This class is supposed to use like ExpandoObject.
    /// A Dynamic object can have dynamic method and property implementation.
    /// It can also be casted to a interface if the required members of the interface were implemented.
    /// </summary>
    /// <seealso cref="TOPLib.Util.DotNet.Dynamic.Lang.DynamicBase" />
    public class Dynamic : DynamicBase
    {
        private static readonly object simpleObj = new object();

        internal object ObjectReference { get; private set; }

        public Dynamic(object objRef = null)
            : base()
        {
            if (objRef == null) objRef = simpleObj;

            this.ObjectReference = objRef;

            foreach (var member in objRef.GetType().GetMembers())
            {
                if (member is FieldInfo)
                {
                    var field = (FieldInfo)member;
                    MemberStore.Attach(member.Name, this.ToStore(field));
                }
                else if (member is PropertyInfo)
                {
                    var property = (PropertyInfo)member;

                    MemberStore.Attach(member.Name, this.ToStore(property));
                }
                else if (member is MethodInfo)
                {
                    var method = (MethodInfo)member;

                    var illegalParams = method.GetParameters().Where(p => p.IsOut);
                    if (illegalParams.Count() > 0)
                    {
                        continue;
                    }
                    var delegateType = typeof(Action);

                    var paraCount = method.GetParameters().Count() + (method.ReturnType == typeof(void) ? 0 : 1);

                    string typeName = "System."
                        + (method.ReturnType == typeof(void) ? "Action" : "Func")
                        + (paraCount == 0 ? "" : "`" + paraCount);

                    delegateType = Assembly.GetAssembly(delegateType).GetType(typeName);

                    if (delegateType == null)
                        throw new Exception("Wth of the method do you have???");

                    if (delegateType.IsGenericType)
                    {
                        var types = new List<Type>(method.GetParameters().Select(p => p.ParameterType));
                        if (method.ReturnType != typeof(void))
                            types.Add(method.ReturnType);
                        delegateType = delegateType.MakeGenericType(types.ToArray());
                    }

                    Delegate func;

                    if (method.IsStatic)
                        func = Delegate.CreateDelegate(delegateType, method);
                    else
                        func = Delegate.CreateDelegate(delegateType, objRef, method);

                    MemberStore.Attach(method.Name, func);
                }

            }
        }

        private PropertyStore ToStore(PropertyInfo propertyInfo)
        {
            var setterType = typeof(Action<>).MakeGenericType(propertyInfo.PropertyType);
            var getterType = typeof(Func<>).MakeGenericType(propertyInfo.PropertyType);

            var setter = Delegate.CreateDelegate(setterType, ObjectReference, propertyInfo.GetSetMethod());
            var getter = Delegate.CreateDelegate(getterType, ObjectReference, propertyInfo.GetGetMethod());

            return (PropertyStore)(typeof(PropertyStore).GetMethod("Create").MakeGenericMethod(propertyInfo.PropertyType).Invoke(null, new object[] { setter, getter }));
        }

        private PropertyStore ToStore(FieldInfo fieldInfo)
        {
            var returnType = typeof(PropertyStore<>).MakeGenericType(fieldInfo.FieldType);
            var constructor = returnType.GetConstructor(new Type[] { typeof(FieldInfo), typeof(object) });
            return (PropertyStore)constructor.Invoke(new object[] { fieldInfo, ObjectReference });
        }

        /// <summary>
        /// Set up a property.
        /// </summary>
        /// <typeparam name="T">The property type</typeparam>
        /// <param name="name">The property name.</param>
        /// <param name="setter">The setter of property, which can be null for readonly property</param>
        /// <param name="getter">The getter of property, which can be null for writeonly property</param>
        public void SetProperty<T>(
            string name,
            Action<T> setter,
            Func<T> getter
            )
        {
            object toSet = null;
            if (MemberStore[name] != null)
            {
                var field = MemberStore[name].FindField();
                if (field != null)
                {
                    if (field is PropertyStore)
                    {
                        var prop = (PropertyStore)field;
                        prop.Update(setter, getter);
                        return;
                    }
                    if (typeof(T).IsAssignableFrom(field.GetType()))
                        toSet = field;
                }
            }
            var property = PropertyStore.Create<T>(setter, getter);
            MemberStore.Attach(name, property);
            if (toSet != null)
                property.Set(toSet);
        }


        /// <summary>
        /// Set up a property.
        /// </summary>
        /// <typeparam name="T">The property type</typeparam>
        /// <param name="name">The property name</param>
        /// <param name="constantValue">The constant property value.</param>
        public void SetProperty<T>(
            string name,
            T constantValue
            )
        {
            object toSet = null;
            if (MemberStore[name] != null)
            {
                MemberStore[name].ClearProperty();
            }
            var property = PropertyStore.Create<T>(null, () =>
            {
                return constantValue;
            });
            MemberStore.Attach(name, property);
            if (toSet != null)
                property.Set(toSet);
        }
    }
}
