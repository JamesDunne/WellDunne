using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace System
{
    public static class ReflectionExtensions
    {
        public static string GetDeclarationDisplayString(this System.Reflection.MethodBase method)
        {
            if (method == null) return String.Empty;

            var parms = from m in method.GetParameters()
                        select m.ToString();

            string paramList = String.Join(", ", parms.ToArray());

            if (method.DeclaringType != null)
                return String.Format("{0}.{1}({2})", method.DeclaringType.FullName, method.Name, paramList);
            else
                return String.Format("{0}({1})", method.Name, paramList);
        }

        public static string GetMethodParameterDeclarationList(this System.Reflection.MethodBase method)
        {
            return method.GetMethodParameterDeclarationList(", ", ty => ty, ident => ident);
        }

        public static string GetMethodParameterDeclarationList(this System.Reflection.MethodBase method, string delim, Func<string, string> typeFormat, Func<string, string> identFormat)
        {
            StringBuilder sb = new StringBuilder();
            var prms = method.GetParameters();
            for (int i = 0; i < prms.Length; ++i)
            {
                Type pType = prms[i].ParameterType;
                string pTypeName = pType.GetCSharpDisplayName();
                sb.AppendFormat("{0} {1}", typeFormat(pTypeName), identFormat(prms[i].Name));
                if (i < prms.Length - 1) sb.Append(delim);
            }
            return sb.ToString();
        }

        public static string GetCSharpDisplayName(this Type pType)
        {
            string pTypeName;
            if (pType.IsGenericType && (pType.GetGenericTypeDefinition() == typeof(Nullable<>)))
            {
                pTypeName = pType.GetGenericArguments()[0].GetCSharpDisplayName() + "?";
            }
            else if (pType.IsGenericType)
            {
                // FIXME: generate a comma-delimited list of arguments
                pTypeName = pType.Name.Remove(pType.Name.Length - 2) + "<" + pType.GetGenericArguments()[0].GetCSharpDisplayName() + ">";
            }
            else if (pType == typeof(void))
            {
                pTypeName = "void";
            }
            else if (pType == typeof(int))
            {
                pTypeName = "int";
            }
            else if (pType == typeof(string))
            {
                pTypeName = "string";
            }
            else if (pType == typeof(bool))
            {
                pTypeName = "bool";
            }
            // TODO: other CLR to C# conversions here...
            else
            {
                pTypeName = pType.Name.ToString();
            }
            return pTypeName;
        }
    }
}
