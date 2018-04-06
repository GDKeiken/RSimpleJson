using System;
using System.Reflection;
using System.Collections.Generic;

namespace RSimpleJson.Reflection
{
    public class CacheResolver
    {
        #region Inner Classes
        public sealed class MemberMap
        {
            public readonly MemberInfo MemberInfo;
            public readonly Type Type;
            public readonly GetHandler Getter;
            public readonly SetHandler Setter;

            public MemberMap(PropertyInfo propertyInfo)
            {
                MemberInfo = propertyInfo;
                Type = propertyInfo.PropertyType;
                Getter = CreateGetHandler(propertyInfo);
                Setter = CreateSetHandler(propertyInfo);
            }

            public MemberMap(FieldInfo fieldInfo)
            {
                MemberInfo = fieldInfo;
                Type = fieldInfo.FieldType;
                Getter = CreateGetHandler(fieldInfo);
                Setter = CreateSetHandler(fieldInfo);
            }
        }
        #endregion

        #region Delegates
        private delegate object CtorDelegate();
        public delegate void MemberMapLoader(Type type, Dictionary<string, MemberMap> memberMaps);
        public delegate object GetHandler(object source);
        public delegate void SetHandler(object source, object value);
        #endregion

        #region Variables & Properties
        private static readonly Dictionary<Type, CtorDelegate> _constructorCache = new Dictionary<Type, CtorDelegate>();
        private readonly Dictionary<Type, Dictionary<string, MemberMap>> _memberMapsCache = new Dictionary<Type, Dictionary<string, MemberMap>>();
        private readonly MemberMapLoader _memberMapLoader;
        #endregion

        #region Public Methods
        public CacheResolver(MemberMapLoader memberMapLoader)
        {
            _memberMapLoader = memberMapLoader;
        }

        public Dictionary<string, MemberMap> LoadMaps(Type type)
        {
            if (type == null || type == typeof(object))
            {
                return null;
            }

			Dictionary<string, MemberMap> memberDict;
            if (_memberMapsCache.TryGetValue(type, out memberDict))
            {
                return memberDict;
            }

            memberDict = new Dictionary<string, MemberMap>();
            _memberMapLoader(type, memberDict);
            _memberMapsCache.Add(type, memberDict);
            return memberDict;
        }

        #region Static Methods
        public static object GetNewInstance(Type type)
        {
			return Activator.CreateInstance(type);
			/*CtorDelegate ctorDelegate;
            if (_constructorCache.TryGetValue(type, out ctorDelegate))
            {
                return ctorDelegate();
            }

            ConstructorInfo constructorInfo = type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
            ctorDelegate = (() => constructorInfo.Invoke(null));
            _constructorCache.Add(type, ctorDelegate);
            return ctorDelegate();*/
		}

		public static void ClearCache()
		{
			_constructorCache.Clear();
        }
        #endregion

        #endregion

        #region Private Methods

        #region Static Methods
        private static GetHandler CreateGetHandler(PropertyInfo propertyInfo)
        {
            MethodInfo getMethodInfo = propertyInfo.GetGetMethod(true);
            if (getMethodInfo == null)
            {
                return null;
            }
            return (object instance) => getMethodInfo.Invoke(instance, Type.EmptyTypes);
        }

        private static GetHandler CreateGetHandler(FieldInfo fieldInfo)
        {
            return (object instance) => fieldInfo.GetValue(instance);
        }

        private static SetHandler CreateSetHandler(FieldInfo fieldInfo)
        {
            if (fieldInfo.IsInitOnly || fieldInfo.IsLiteral)
            {
                return null;
            }

            return delegate (object instance, object value)
            {
                fieldInfo.SetValue(instance, value);
            };
        }

        private static SetHandler CreateSetHandler(PropertyInfo propertyInfo)
        {
            MethodInfo setMethodInfo = propertyInfo.GetSetMethod(true);
            if (setMethodInfo == null)
            {
                return null;
            }

            return delegate (object instance, object value)
            {
                setMethodInfo.Invoke(instance, new object[] { value });
            };
        }
        #endregion

        #endregion
    }
}