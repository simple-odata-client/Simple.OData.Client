using System;
using System.CodeDom;
using Microsoft.Spatial;
using Xunit;
using Xunit.Extensions;
using Xunit.Sdk;

namespace Simple.OData.Client.Tests
{
    public class ValueConversionTests
    {
        [Theory]
        [InlineData(1, typeof(int), typeof(byte))]
        [InlineData(1, typeof(int), typeof(byte?))]
        [InlineData(1, typeof(int), typeof(short))]
        [InlineData(1, typeof(int), typeof(short?))]
        [InlineData(1, typeof(int), typeof(long))]
        [InlineData(1, typeof(int), typeof(long?))]
        [InlineData(1, typeof(int), typeof(decimal))]
        [InlineData(1, typeof(int), typeof(decimal?))]
        [InlineData(1, typeof(int), typeof(float))]
        [InlineData(1, typeof(int), typeof(float?))]
        [InlineData(1, typeof(int), typeof(double))]
        [InlineData(1, typeof(int), typeof(double?))]
        [InlineData(1, typeof(double), typeof(byte))]
        [InlineData(1, typeof(double), typeof(byte?))]
        [InlineData(1, typeof(double), typeof(short))]
        [InlineData(1, typeof(double), typeof(short?))]
        [InlineData(1, typeof(double), typeof(long))]
        [InlineData(1, typeof(double), typeof(long?))]
        [InlineData(1, typeof(double), typeof(decimal))]
        [InlineData(1, typeof(double), typeof(decimal?))]
        [InlineData(1, typeof(double), typeof(float))]
        [InlineData(1, typeof(double), typeof(float?))]
        [InlineData("Utc", typeof(string), typeof(DateTimeKind))]
        [InlineData("Utc", typeof(string), typeof(DateTimeKind?))]
        [InlineData("2014-02-01T12:00:00.123", typeof(DateTimeOffset), typeof(DateTime))]
        [InlineData("2014-02-01T12:00:00.123", typeof(DateTimeOffset), typeof(DateTime?))]
        [InlineData("58D6C94D-B18A-43C9-AC1B-0B5A5BF10C35", typeof(string), typeof(Guid))]
        [InlineData("58D6C94D-B18A-43C9-AC1B-0B5A5BF10C35", typeof(string), typeof(Guid?))]
        public void TryConvert(object value, Type sourceType, Type targetType)
        {
            var sourceValue = ChangeType(value, sourceType);
            object targetValue;
            var result = Utils.TryConvert(sourceValue, targetType, out targetValue);
            Assert.Equal(true, result);
            Assert.Equal(ChangeType(sourceValue, targetType), ChangeType(targetValue, targetType));

            sourceValue = ChangeType(value, targetType);
            result = Utils.TryConvert(sourceValue, sourceType, out targetValue);
            Assert.Equal(true, result);
            Assert.Equal(ChangeType(sourceValue, sourceType), ChangeType(targetValue, sourceType));
        }

        [Fact]
        public void TryConvertGeographyPoint()
        {
            var source = GeographyPoint.Create(10, 10);
            object targetValue;
            var result = Utils.TryConvert(source, typeof(GeographyPoint), out targetValue);
            Assert.Equal(true, result);
        }

        private object ChangeType(object value, Type targetType)
        {
            if (targetType == typeof(string))
                return value.ToString();
            if (targetType == typeof(DateTime))
                return DateTime.Parse(value.ToString());
            if (targetType == typeof(DateTimeOffset))
                return DateTimeOffset.Parse(value.ToString());
            if (targetType.IsEnum)
                return Enum.Parse(targetType, value.ToString(), true);
            if (targetType == typeof(Guid))
                return new Guid(value.ToString());
            if (Nullable.GetUnderlyingType(targetType) != null)
                return ChangeType(value, Nullable.GetUnderlyingType(targetType));

            return Convert.ChangeType(value, targetType);
        }
    }
}