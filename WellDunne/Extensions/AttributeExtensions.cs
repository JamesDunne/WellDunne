using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace System
{
    public static class AttributeExtensions
    {
        public static IEnumerable<T> GetCustomAttributes<T>(this MemberInfo type) where T : Attribute
        {
            return type.GetCustomAttributes(typeof(T), false).OfType<T>();
        }

        public static IEnumerable<TAtt> GetAttributesForNonEmptyProperties<TParam, TAtt>(TParam input) where TAtt : Attribute
        {
            return input.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(sel =>
                {
                    object value = sel.GetGetMethod().Invoke(input, null);
                    return value != null;
                })
                .SelectMany(sel => sel.GetCustomAttributes<TAtt>());
        }
    }
}
