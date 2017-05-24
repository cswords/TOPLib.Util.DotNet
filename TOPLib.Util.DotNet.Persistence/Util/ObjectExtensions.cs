using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TOPLib.Util.DotNet.Persistence.Util
{
    public static class ObjectExtensions
    {
        public static Boolean IsConvertableTo(this Type sourceType, Type targetType)
        {
            if (sourceType.IsValueType)
                return Check(sourceType, targetType);
            else
                return targetType.IsAssignableFrom(sourceType);
        }

        internal static bool Check(Type fromType, Type toType)
        {
            Type converterType = typeof(TypeConverterChecker<,>).MakeGenericType(fromType, toType);
            object instance = Activator.CreateInstance(converterType);
            return (bool)converterType.GetProperty("CanConvert").GetGetMethod().Invoke(instance, null);
        }
    }

    internal class TypeConverterChecker<TFrom, TTo>
    {
        public bool CanConvert { get; private set; }

        public TypeConverterChecker()
        {
            TFrom from = default(TFrom);
            if (from == null)
                if (typeof(TFrom).Equals(typeof(String)))
                    from = (TFrom)(dynamic)"";
                else
                    from = (TFrom)Activator.CreateInstance(typeof(TFrom));
            try
            {
                TTo to = (dynamic)from;
                CanConvert = true;
            }
            catch
            {
                CanConvert = false;
            }
        }
    }
}
