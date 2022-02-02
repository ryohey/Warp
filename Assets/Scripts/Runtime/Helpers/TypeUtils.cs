using System;
using System.Text.RegularExpressions;

namespace Warp
{
    public static class TypeUtils
    {
        public static Type GetUnityType(string className)
        {
            return Type.GetType($"UnityEngine.{className}, UnityEngine.dll");
        }

        public static string FixPropName(string name)
        {
            return Regex.Replace(name, @"^m_", string.Empty)
                .FirstCharToLowerCase();
        }
    }
}
