using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;

namespace RSimpleJson.Reflection
{
    public static class ReflectionUtils
    {
        #region Public Methods
        public static Attribute GetAttribute(Type objectType, Type attributeType)
        {
            if (objectType == null || attributeType == null || !Attribute.IsDefined(objectType, attributeType))
            {
                return null;
            }

            return Attribute.GetCustomAttribute(objectType, attributeType);
        }

        public static Attribute GetAttribute(MemberInfo info, Type type)
        {
            if (info == null || type == null || !Attribute.IsDefined(info, type))
            {
                return null;
            }

            return Attribute.GetCustomAttribute(info, type);
        }

        public static bool IsNullableType(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        public static bool IsTypeDictionary(Type type)
        {
            if (typeof(IDictionary).IsAssignableFrom(type))
            {
                return true;
            }

            if (!type.IsGenericType)
            {
                return false;
            }

            Type genericTypeDefinition = type.GetGenericTypeDefinition();
            return genericTypeDefinition == typeof(IDictionary<,>);
        }

        public static bool IsTypeGenericeCollectionInterface(Type type)
        {
            if (!type.IsGenericType)
            {
                return false;
            }

            Type genericTypeDefinition = type.GetGenericTypeDefinition();
            return genericTypeDefinition == typeof(IList<>) || genericTypeDefinition == typeof(ICollection<>) || genericTypeDefinition == typeof(IEnumerable<>);
        }

        public static object ToNullableType(object obj, Type nullableType)
        {
            return (obj != null) ? Convert.ChangeType(obj, Nullable.GetUnderlyingType(nullableType), CultureInfo.InvariantCulture) : null;
        }
        #endregion
    }
}