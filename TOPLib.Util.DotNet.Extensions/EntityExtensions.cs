using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Linq.Mapping;

namespace TOPLib.Util.DotNet.Extensions
{
    public static class EntityExtensions
    {

        public static IDictionary<string, object> ToDictionary(this object condition)
        {
            var props = condition.GetType().GetProperties();
            Dictionary<string, object> keys = new Dictionary<string, object>();
            foreach (var prop in props)
            {
                keys[prop.Name] = prop.GetValue(condition, null);
            }
            return keys;
        }

        public static IDictionary<string,object> GetLinqPrimaryKeyValues(this object condition)
        {
            if (Attribute.GetCustomAttribute(condition.GetType(), typeof(TableAttribute)) != null)
            {
                var props = condition.GetType().GetProperties();
                Dictionary<string, object> keys = new Dictionary<string, object>();
                foreach (var prop in props)
                {
                    var attr = (ColumnAttribute)Attribute.GetCustomAttribute(prop, typeof(ColumnAttribute));
                    if (attr != null)
                    {
                        if (attr.IsPrimaryKey)
                        {
                            keys[prop.Name] = prop.GetValue(condition, null);
                        }
                    }
                }
                return keys;
            }
            else
            {
                throw new Exception("This is not a valid entity!");
            }
        }
    }
}
