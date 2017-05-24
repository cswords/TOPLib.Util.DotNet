using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading;

namespace TOPLib.Util.DotNet.Dynamic.Lang
{
    public static class DynamicCaster
    {
        public static string AssemblyNameString { get; set; }

        private static ModuleBuilder mb = null;

        internal static Action SaveAssembly { get; private set; }

        internal static ModuleBuilder Mb
        {
            get
            {
                if (mb == null)
                {
                    if (AssemblyNameString == null)
                        AssemblyNameString = "MyDynamicAssembly";

                    AppDomain currentDom = Thread.GetDomain();

                    AssemblyName myAsmName = new AssemblyName();
                    myAsmName.Name = AssemblyNameString;

                    AssemblyBuilder myAsmBldr = currentDom.DefineDynamicAssembly(
                                       myAsmName,
                                       AssemblyBuilderAccess.RunAndSave);

                    // We've created a dynamic assembly space - now, we need to create a module 
                    // within it to reflect the type Point into.

                    mb = myAsmBldr.DefineDynamicModule(myAsmName.Name, myAsmName.Name + ".dll");

                    SaveAssembly = () => { myAsmBldr.Save(myAsmName + ".dll"); };
                }
                return mb;
            }
        }

        private static IDictionary<Type, Type> cache = new Dictionary<Type, Type>();

        /// <summary>
        /// Ases the specified d.
        /// </summary>
        /// <typeparam name="K">The target interface type.</typeparam>
        /// <param name="d">The Dynamic object which will be cast to the target interface type.</param>
        /// <returns></returns>
        /// <exception cref="Exception">
        /// Property type is not implemented!!!
        /// or
        /// Property is not implemented!!!
        /// or
        /// The target type must be an Interface!!!
        /// </exception>
        public static K As<K>(this Dynamic d)
        {
            if (typeof(K).IsInterface)
            {
                if (d.GetType().IsGenericType)
                {
                    if (typeof(K).IsAssignableFrom(d.GetType().GetGenericArguments()[0]))
                    {
                        dynamic o = d;
                        return (K)(o.ObjectReference);
                    }
                }
                Type type;
                if (cache.ContainsKey(typeof(K)))
                {
                    type = cache[typeof(K)];
                }
                else
                {
                    var myModuleBldr = Mb;

                    var interfaceType = typeof(K);

                    var temporaryTypeName = interfaceType.Name + "DynamicImpl_" + Guid.NewGuid();

                    TypeBuilder tb = myModuleBldr.DefineType(temporaryTypeName, TypeAttributes.Public | TypeAttributes.Class, typeof(CastedBase), new Type[] { interfaceType });
                    tb.AddInterfaceImplementation(interfaceType);

                    ConstructorBuilder cb = tb.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, new Type[] { typeof(DynamicBase) });
                    BuildConstructor(cb.GetILGenerator());

                    foreach (var methodInfo in interfaceType.GetMethods())
                    {
                        var mb = tb.DefineMethod(methodInfo.Name, methodInfo.Attributes & (~MethodAttributes.Abstract), CallingConventions.Standard,
                            methodInfo.ReturnType, methodInfo.GetParameters().Select(i => i.ParameterType).ToArray());

                        for (int i = 0; i < methodInfo.GetParameters().Length; i++)
                        {
                            var para = methodInfo.GetParameters()[i];
                            mb.DefineParameter(i + 1, para.Attributes, para.Name);
                        }
                        BuildMethod(mb.GetILGenerator(), methodInfo);
                        tb.DefineMethodOverride(mb, methodInfo);
                    }

                    foreach (var propertyInfo in interfaceType.GetProperties())
                    {
                        if (d.MemberStore[propertyInfo.Name] != null)
                        {
                            var property = d.MemberStore[propertyInfo.Name].FindField();
                            if (property != null)
                            {
                                if (property is PropertyStore)
                                {
                                    if (property.GetType().GetGenericArguments().Length == 1)
                                    {
                                        if (property.GetType().GetGenericArguments()[0] == propertyInfo.PropertyType)
                                        {
                                            var pb = tb.DefineProperty(propertyInfo.Name, propertyInfo.Attributes, propertyInfo.PropertyType, Type.EmptyTypes);

                                            if (propertyInfo.GetGetMethod() != null)
                                            {
                                                var methodInfo = propertyInfo.GetGetMethod();
                                                var mb = tb.DefineMethod(methodInfo.Name, methodInfo.Attributes & (~MethodAttributes.Abstract), CallingConventions.Standard,
                                                    methodInfo.ReturnType, methodInfo.GetParameters().Select(i => i.ParameterType).ToArray());

                                                for (int i = 0; i < methodInfo.GetParameters().Length; i++)
                                                {
                                                    var para = methodInfo.GetParameters()[i];
                                                    mb.DefineParameter(i + 1, para.Attributes, para.Name);
                                                }
                                                BuildMethod(mb.GetILGenerator(), methodInfo);

                                                pb.SetGetMethod(mb);
                                            }

                                            if (propertyInfo.GetSetMethod() != null)
                                            {
                                                var methodInfo = propertyInfo.GetSetMethod();
                                                var mb = tb.DefineMethod(methodInfo.Name, methodInfo.Attributes & (~MethodAttributes.Abstract), CallingConventions.Standard,
                                                    methodInfo.ReturnType, methodInfo.GetParameters().Select(i => i.ParameterType).ToArray());

                                                for (int i = 0; i < methodInfo.GetParameters().Length; i++)
                                                {
                                                    var para = methodInfo.GetParameters()[i];
                                                    mb.DefineParameter(i + 1, para.Attributes, para.Name);
                                                }
                                                BuildMethod(mb.GetILGenerator(), methodInfo);

                                                pb.SetSetMethod(mb);
                                            }
                                            continue;
                                        }
                                        else
                                        {
                                            throw new Exception("Property type is not implemented!!!");
                                        }
                                    }
                                }
                            }
                        }
                        throw new Exception("Property is not implemented!!!");
                    }

                    type = tb.CreateType();
                    cache[typeof(K)] = type;
                }

                var result = (K)type.GetConstructor(new Type[] { typeof(DynamicBase) }).Invoke(new object[] { d });

                return result;
            }
            throw new Exception("The target type must be an Interface!!!");
        }

        public static Delegate GetFunc(this CastedBase casted, MethodBase methodBase)
        {
            var memberStore = casted.DynamicObj.MemberStore[methodBase.Name];
            if (memberStore != null)
            {
                return memberStore.FindMethod(methodBase.GetParameters().Select(p => p.ParameterType).ToArray());
            }
            else if (methodBase.Name.StartsWith("get_"))
            {
                memberStore = casted.DynamicObj.MemberStore[methodBase.Name.Substring(4)];
                var property = memberStore.FindField();
                if (property is PropertyStore)
                {
                    var propertyStore = (PropertyStore)property;
                    return propertyStore.GetGetter();
                }
            }
            else if (methodBase.Name.StartsWith("set_"))
            {
                memberStore = casted.DynamicObj.MemberStore[methodBase.Name.Substring(4)];
                var property = memberStore.FindField();
                if (property is PropertyStore)
                {
                    var propertyStore = (PropertyStore)property;
                    return propertyStore.GetSetter();
                }
            }
            return null;
        }

        //I used ILSpy and a demo class TestDynamic to make these
        private static void BuildConstructor(ILGenerator il)
        {
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, typeof(CastedBase).GetConstructor(new Type[] { typeof(Dynamic) }));
            il.Emit(OpCodes.Ret);
        }

        private static void BuildMethod(ILGenerator il, MethodInfo method)
        {
            var done = il.DefineLabel();

            il.DeclareLocal(typeof(MethodBase));
            il.DeclareLocal(typeof(System.Delegate));
            il.DeclareLocal(typeof(object));
            il.DeclareLocal(typeof(object[]));

            il.Emit(OpCodes.Nop);
            il.Emit(OpCodes.Call, typeof(MethodBase).GetMethod("GetCurrentMethod"));
            il.Emit(OpCodes.Stloc_0);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Call, typeof(DynamicCaster).GetMethod("GetFunc", new Type[] { typeof(CastedBase), typeof(MethodBase) }));
            il.Emit(OpCodes.Stloc_1);
            il.Emit(OpCodes.Ldloc_1);

            il.Emit(OpCodes.Ldc_I4, method.GetParameters().Length);
            il.Emit(OpCodes.Newarr, typeof(object));
            il.Emit(OpCodes.Stloc_3);

            var paraCount = method.GetParameters().Length;
            for (int i = 0; i < paraCount; i++)
            {
                il.Emit(OpCodes.Ldloc_3);
                il.Emit(OpCodes.Ldc_I4, i);

                int _i = i + 1;
                switch (_i)
                {
                    case 0:
                        il.Emit(OpCodes.Ldarg_0);
                        break;
                    case 1:
                        il.Emit(OpCodes.Ldarg_1);
                        break;
                    case 2:
                        il.Emit(OpCodes.Ldarg_2);
                        break;
                    case 3:
                        il.Emit(OpCodes.Ldarg_3);
                        break;
                    default:
                        if (_i < 128)
                        {
                            il.Emit(OpCodes.Ldarg_S, (byte)_i);
                        }
                        else
                        {
                            il.Emit(OpCodes.Ldarg, _i);//not known
                        }
                        break;
                }

                il.Emit(OpCodes.Stelem_Ref);
            }

            il.Emit(OpCodes.Ldloc_3);
            il.Emit(OpCodes.Callvirt, typeof(Delegate).GetMethod("DynamicInvoke"));
            il.Emit(OpCodes.Stloc_2);
            il.Emit(OpCodes.Br_S, done);

            il.MarkLabel(done);
            il.Emit(OpCodes.Ldloc_2);
            il.Emit(OpCodes.Ret);
        }
    }
}
