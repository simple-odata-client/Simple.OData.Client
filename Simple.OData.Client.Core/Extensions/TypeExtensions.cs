using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Simple.OData.Client.Extensions
{
    static class TypeExtensions
    {
#if NET40 || SILVERLIGHT || PORTABLE_LEGACY
        public static IEnumerable<PropertyInfo> GetAllProperties(this Type type)
        {
            return type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(x => x.DeclaringType != typeof(object));
        }

        public static PropertyInfo GetAnyProperty(this Type type, string propertyName)
        {
            var property = type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public);
            return property == null || property.DeclaringType == typeof (object) ? null : property;
        }

        public static IEnumerable<PropertyInfo> GetDeclaredProperties(this Type type)
        {
            return type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        }

        public static PropertyInfo GetDeclaredProperty(this Type type, string propertyName)
        {
            return type.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        }

        public static IEnumerable<FieldInfo> GetAllFields(this Type type)
        {
            return type.GetFields(BindingFlags.Instance | BindingFlags.Public);
        }

        public static FieldInfo GetAnyField(this Type type, string fieldName, bool includeNonPublic = false)
        {
            var bindingFlags = BindingFlags.Instance | BindingFlags.Public;
            if (includeNonPublic)
                bindingFlags |= BindingFlags.NonPublic;
            var field = type.GetField(fieldName, bindingFlags);
            return field == null || field.DeclaringType == typeof (object) ? null : field;
        }

        public static IEnumerable<FieldInfo> GetDeclaredFields(this Type type)
        {
            return type.GetFields(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly);
        }

        public static FieldInfo GetDeclaredField(this Type type, string fieldName, bool includeNonPublic = false)
        {
            var bindingFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.DeclaredOnly;
            if (includeNonPublic)
                bindingFlags |= BindingFlags.NonPublic;
            return type.GetField(fieldName, bindingFlags);
        }

        public static MethodInfo GetDeclaredMethod(this Type type, string methodName)
        {
            return type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        public static IEnumerable<ConstructorInfo> GetDeclaredConstructors(this Type type)
        {
            return type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        public static ConstructorInfo GetDefaultConstructor(this Type type)
        {
            return type.GetConstructor(new Type[] {});
        }

        public static TypeAttributes GetTypeAttributes(this Type type)
        {
            return type.Attributes;
        }

        public static Type[] GetGenericTypeArguments(this Type type)
        {
            return type.GetGenericArguments();
        }

        public static bool IsTypeAssignableFrom(this Type type, Type otherType)
        {
            return type.IsAssignableFrom(otherType);
        }

        public static bool HasCustomAttribute(this Type type, Type attributeType, bool inherit)
        {
            return Attribute.IsDefined(type, attributeType, inherit);
        }

        public static bool IsGeneric(this Type type)
        {
            return type.IsGenericType;
        }

        public static bool IsValue(this Type type)
        {
            return type.IsValueType;
        }

        public static bool IsEnumType(this Type type)
        {
            return type.IsEnum;
        }

        public static Type GetBaseType(this Type type)
        {
            return type.BaseType;
        }
#else
        public static IEnumerable<PropertyInfo> GetAllProperties(this Type type)
        {
            var properties = GetDeclaredProperties(type).ToList();

            var baseType = type.GetTypeInfo().BaseType;
            if (baseType != null && baseType != typeof(object))
                properties.AddRange(baseType.GetAllProperties().Where(x => properties.All(y => y.Name != x.Name)));

            return properties.ToArray();
        }

        public static PropertyInfo GetAnyProperty(this Type type, string propertyName)
        {
            var currentType = type;
            while (currentType != null && currentType != typeof(object))
            {
                var property = currentType.GetTypeInfo().GetDeclaredProperty(propertyName);
                if (property != null)
                    return property;

                currentType = currentType.GetTypeInfo().BaseType;
            }
            return null;
        }

        private static bool IsInstanceProperty(PropertyInfo propertyInfo)
        {
            return (propertyInfo.CanRead && !propertyInfo.GetMethod.IsStatic)
                || (propertyInfo.CanWrite && !propertyInfo.SetMethod.IsStatic);
        }

        public static IEnumerable<PropertyInfo> GetDeclaredProperties(this Type type)
        {
            return type.GetTypeInfo().DeclaredProperties.Where(x => IsInstanceProperty(x));
        }

        public static PropertyInfo GetDeclaredProperty(this Type type, string propertyName)
        {
            return type.GetTypeInfo().GetDeclaredProperty(propertyName);
        }

        public static IEnumerable<FieldInfo> GetAllFields(this Type type)
        {
            var fields = GetDeclaredFields(type).ToList();

            var baseType = type.GetTypeInfo().BaseType;
            if (baseType != null && baseType != typeof(object))
                fields.AddRange(baseType.GetAllFields().Where(x => fields.All(y => y.Name != x.Name)));

            return fields.ToArray();
        }

        public static FieldInfo GetAnyField(this Type type, string fieldName, bool includeNonPublic = false)
        {
            var currentType = type;
            while (currentType != null && currentType != typeof(object))
            {
                var field = currentType.GetDeclaredField(fieldName);
                if (field != null)
                    return field;

                currentType = currentType.GetTypeInfo().BaseType;
            }
            return null;
        }

        public static IEnumerable<FieldInfo> GetDeclaredFields(this Type type)
        {
            return type.GetTypeInfo().DeclaredFields.Where(x => !x.IsStatic);
        }

        public static FieldInfo GetDeclaredField(this Type type, string fieldName)
        {
            return type.GetTypeInfo().GetDeclaredField(fieldName);
        }

        public static MethodInfo GetDeclaredMethod(this Type type, string methodName)
        {
            return type.GetTypeInfo().GetDeclaredMethod(methodName);
        }

        public static IEnumerable<ConstructorInfo> GetDeclaredConstructors(this Type type)
        {
            return type.GetTypeInfo().DeclaredConstructors.Where(x => !x.IsStatic);
        }

        public static ConstructorInfo GetDefaultConstructor(this Type type)
        {
            return GetDeclaredConstructors(type).SingleOrDefault(x => x.GetParameters().Length == 0);
        }

        public static TypeAttributes GetTypeAttributes(this Type type)
        {
            return type.GetTypeInfo().Attributes;
        }

        public static Type[] GetGenericTypeArguments(this Type type)
        {
            return type.GetTypeInfo().GenericTypeArguments;
        }

        public static bool IsTypeAssignableFrom(this Type type, Type otherType)
        {
            return type.GetTypeInfo().IsAssignableFrom(otherType.GetTypeInfo());
        }

        public static bool HasCustomAttribute(this Type type, Type attributeType, bool inherit)
        {
            return type.GetTypeInfo().GetCustomAttribute(attributeType, inherit) != null;
        }

        public static bool IsGeneric(this Type type)
        {
            return type.GetTypeInfo().IsGenericType;
        }

        public static bool IsValue(this Type type)
        {
            return type.GetTypeInfo().IsValueType;
        }

        public static bool IsEnumType(this Type type)
        {
            return type.GetTypeInfo().IsEnum;
        }

        public static Type GetBaseType(this Type type)
        {
            return type.GetTypeInfo().BaseType;
        }
#endif
    }
}
