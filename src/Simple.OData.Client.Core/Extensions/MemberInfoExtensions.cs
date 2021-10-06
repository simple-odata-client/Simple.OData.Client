using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

#pragma warning disable 1591

namespace Simple.OData.Client.Extensions
{
    public static class MemberInfoExtensions
    {
        private static readonly ConcurrentDictionary<MemberInfo, MappingInfo> cache = new ConcurrentDictionary<MemberInfo, MappingInfo>();

        public static bool IsNotMapped(this MemberInfo memberInfo)
        {
            return MappingInfo(memberInfo).IsNotMapped;
        }

        /// <summary>
        /// Extract a column name from the member's attributes
        /// </summary>
        /// <param name="memberInfo"></param>
        /// <returns></returns>
        public static string GetMappedName(this MemberInfo memberInfo)
        {
            return MappingInfo(memberInfo).MappedName;
        }

        private static MappingInfo MappingInfo(MemberInfo memberInfo)
        {
            var info = cache.GetOrAdd(memberInfo, MappingInfoFactory);

            return info;
        }

        private static MappingInfo MappingInfoFactory(MemberInfo memberInfo)
        {
            var attributes = memberInfo.GetCustomAttributes().ToList();

            return new MappingInfo
            {
                IsNotMapped = attributes.IsNotMapped(),
                MappedName = memberInfo.Name.GetMappedName(attributes)
            };
        }

        private static bool IsNotMapped(this IList<Attribute> attributes)
        {
            var supportedAttributeNames = new[]
            {
                "NotMappedAttribute",
                "JsonIgnoreAttribute",
            };

            return attributes.Any(x => supportedAttributeNames.Contains(x.GetType().Name));
        }

        private static string GetMappedName(this string name, IList<Attribute> attributes)
        {
            var supportedAttributeNames = new[]
            {
                "DataAttribute",
                "DataMemberAttribute",
                "ColumnAttribute",
                "JsonPropertyAttribute",
                "JsonPropertyNameAttribute",
            };

            var mappingAttribute = attributes.FirstOrDefault(x => supportedAttributeNames.Any(y => x.GetType().Name == y));
            if (mappingAttribute == null)
            {
                mappingAttribute = attributes.FirstOrDefault(x => x.GetType().GetNamedProperty("PropertyName") != null);
            }

            var attributePropertyNames = new[]
            {
                "Name",
                "PropertyName",
            };
            if (mappingAttribute != null)
            {
                foreach (var attributeProperty in attributePropertyNames)
                {
                    var nameProperty = mappingAttribute.GetType().GetNamedProperty(attributeProperty);
                    if (nameProperty != null)
                    {
                        var propertyValue = nameProperty.GetValueEx(mappingAttribute);
                        if (propertyValue != null)
                            return propertyValue.ToString();
                    }
                }
            }

            return name;
        }

        public static object GetValueEx(this MemberInfo memberInfo, object instance)
        {
            return MemberAccessor.GetValue(instance, memberInfo);
        }

        public static void SetValueEx(this MemberInfo memberInfo, object instance, object value)
        {
            MemberAccessor.SetValue(instance, memberInfo, value);
        }
    }

    public class MappingInfo
    {
        public bool IsNotMapped { get; set; }

        public string MappedName { get; set; }
    }
}