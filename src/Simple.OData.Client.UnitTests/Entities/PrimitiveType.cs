using System;
using System.ComponentModel;
using System.Globalization;

namespace Simple.OData.Client.Tests.Entities
{
    [TypeConverter(typeof(PrimitiveTypeConverter))]
    internal readonly struct PrimitiveType
    {
        public PrimitiveType(Guid value)
        {
            Value = value;
        }

        public Guid Value { get; }
    }
    
    internal class PrimitiveTypeConverter : System.ComponentModel.TypeConverter
    {
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            => destinationType == typeof(PrimitiveType) ||
               destinationType == typeof(PrimitiveType?) ||
               base.CanConvertTo(context, destinationType);

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(PrimitiveType))
            {
                return new PrimitiveType(Guid.Parse(value.ToString()));
            }
            if (destinationType == typeof(PrimitiveType))
            {
                return (PrimitiveType?)new PrimitiveType(Guid.Parse(value.ToString()));
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}