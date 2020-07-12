using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.UI.Builder
{
    internal static class ReflectionExtensions
    {
        internal static readonly string s_PropertyNotFoundMessage = "Property not found from Reflection";

        public static bool HasValueByReflection(this object obj, string propertyName)
        {
            var propertyInfo = obj.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);

            return propertyInfo != null;
        }

        public static object GetValueByReflection(this object obj, string propertyName)
        {
            var propertyInfo = obj.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.IgnoreCase);

            if (propertyInfo == null)
                throw new ArgumentException(s_PropertyNotFoundMessage);

            return propertyInfo.GetValue(obj);
        }

        public static void SetValueByReflection(this object obj, string propertyName, object value)
        {
            var propertyInfo = obj.GetType().GetProperty(propertyName);

            if (propertyInfo == null)
                throw new ArgumentException(s_PropertyNotFoundMessage);

            propertyInfo?.SetValue(obj, value);
        }
    }
}
