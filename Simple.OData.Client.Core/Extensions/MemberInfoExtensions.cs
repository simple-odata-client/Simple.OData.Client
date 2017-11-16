using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

#pragma warning disable 1591

namespace Simple.OData.Client.Extensions
{
    public static class MemberInfoExtensions
    {
        public static bool IsNotMapped(this MemberInfo member)
        {
            return member.GetCustomAttributes().Any(x => x.GetType().Name == "NotMappedAttribute");
        }

        public static string GetMappedName(this MemberInfo property)
        {
            var supportedAttributeNames = new[]
            {
                "DataAttribute",
                "DataMemberAttribute",
                "ColumnAttribute",
            };

            var propertyName = property.Name;
            string attributeProperty;
            var mappingAttribute = property.GetCustomAttributes()
                .FirstOrDefault(x => supportedAttributeNames.Any(y => x.GetType().Name == y));
            if (mappingAttribute != null)
            {
                attributeProperty = "Name";
            }
            else
            {
                attributeProperty = "PropertyName";
                mappingAttribute = property.GetCustomAttributes()
                    .FirstOrDefault(x => x.GetType().GetAnyProperty(attributeProperty) != null);
            }

            if (mappingAttribute != null)
            {
                var nameProperty = mappingAttribute.GetType().GetAnyProperty(attributeProperty);
                if (nameProperty != null)
                {
                    var propertyValue = nameProperty.GetValue(mappingAttribute, null);
                    if (propertyValue != null)
                        propertyName = propertyValue.ToString();
                }
            }

            return propertyName;
        }
    }
}