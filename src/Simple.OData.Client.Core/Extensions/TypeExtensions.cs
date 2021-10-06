using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Simple.OData.Client.Extensions
{
    static class TypeExtensions
    {
        public static IEnumerable<Type> DerivedTypes(this Type type)
        {
            return type
                .Assembly.GetTypes()
                .Where(t => t.IsSubclassOf(type) && !t.IsAbstract);
        }

        public static IEnumerable<PropertyInfo> GetAllProperties(this Type type)
        {
            var properties = GetDeclaredProperties(type).ToList();

            var baseType = type.GetTypeInfo().BaseType;
            if (baseType != null && baseType != typeof(object))
                properties.AddRange(baseType.GetAllProperties().Where(x => properties.All(y => y.Name != x.Name)));

            return properties.ToArray();
        }

        /// <summary>
        /// Get a directly named property
        /// </summary>
        /// <param name="type"></param>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public static PropertyInfo GetNamedProperty(this Type type, string propertyName)
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

        public static IEnumerable<PropertyInfo> GetMappedProperties(this Type type)
        {
            return type.GetAllProperties().Where(x => !x.IsNotMapped());
        }

        public static IEnumerable<Tuple<PropertyInfo, string>> GetMappedPropertiesWithNames(this Type type)
        {
            return type.GetMappedProperties().Select(p => new Tuple<PropertyInfo, string>(p, p.GetMappedName()));
        }

        private static bool IsInstanceProperty(PropertyInfo propertyInfo)
        {
            return (propertyInfo.CanRead && !propertyInfo.GetMethod.IsStatic)
                || (propertyInfo.CanWrite && !propertyInfo.SetMethod.IsStatic);
        }

        private static bool IsExplicitInterfaceProperty(PropertyInfo propertyInfo)
        {
            return propertyInfo.Name.Contains(".");
        }

        private static bool IsIndexerProperty(PropertyInfo propertyInfo)
        {
            return propertyInfo.GetIndexParameters().Length > 0;
        }

        public static IEnumerable<PropertyInfo> GetDeclaredProperties(this Type type)
        {
            return type.GetTypeInfo().DeclaredProperties
                .Where(x => IsInstanceProperty(x) && !IsExplicitInterfaceProperty(x) && !IsIndexerProperty(x));
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
 
        public static bool IsAnonymousType(this Type type)
        {
            // HACK: The only way to detect anonymous types right now.
            return type.HasCustomAttribute(typeof(CompilerGeneratedAttribute), false)
                   && type.IsGeneric() && type.Name.Contains("AnonymousType")
                   && (type.Name.StartsWith("<>", StringComparison.OrdinalIgnoreCase) ||
                       type.Name.StartsWith("VB$", StringComparison.OrdinalIgnoreCase))
                   && (type.GetTypeAttributes() & TypeAttributes.NotPublic) == TypeAttributes.NotPublic;
        }

        public static string GetMappedName(this Type type)
        {
            var supportedAttributeNames = new[]
            {
                "DataContractAttribute",
                "TableAttribute",
            };

            var mappingAttribute = type.GetCustomAttributes()
                .FirstOrDefault(x => supportedAttributeNames.Any(y => x.GetType().Name == y));

            if (mappingAttribute != null)
            {
                var nameProperty = mappingAttribute.GetType().GetNamedProperty("Name");
                if (nameProperty != null)
                {
                    var propertyValue = nameProperty.GetValueEx(mappingAttribute);
                    if (propertyValue != null)
                        return propertyValue.ToString();
                }
            }

            return type.Name;
        }
    }
}
