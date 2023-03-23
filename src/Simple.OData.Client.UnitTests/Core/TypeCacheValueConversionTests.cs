using System;
using Microsoft.Spatial;
using Simple.OData.Client.Tests.Entities;
using Xunit;

namespace Simple.OData.Client.Tests.Core;

public class TypeCacheValueConversionTests
{
	private static ITypeCache TypeCache => TypeCaches.TypeCache("test", null);

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
	[InlineData("2014-02-01T12:00:00.123", typeof(DateTime), typeof(DateTimeOffset))]
	[InlineData("2014-02-01T12:00:00.123", typeof(DateTime), typeof(DateTimeOffset?))]
	[InlineData("58D6C94D-B18A-43C9-AC1B-0B5A5BF10C35", typeof(string), typeof(Guid))]
	[InlineData("58D6C94D-B18A-43C9-AC1B-0B5A5BF10C35", typeof(string), typeof(Guid?))]
	[InlineData("0", typeof(string), typeof(TestEnum))]
	[InlineData("Something", typeof(string), typeof(TestEnum))]
	[InlineData("5", typeof(string), typeof(TestEnum?))]
	[InlineData("Nothing", typeof(string), typeof(TestEnum?))]
	public void TryConvert(object value, Type sourceType, Type targetType)
	{
		var sourceValue = ChangeType(value, sourceType);
		var result = TypeCache.TryConvert(sourceValue, targetType, out var targetValue);
		Assert.True(result);
		Assert.Equal(ChangeType(sourceValue, targetType), ChangeType(targetValue, targetType));

		sourceValue = ChangeType(value, targetType);
		result = TypeCache.TryConvert(sourceValue, sourceType, out targetValue);
		Assert.True(result);
		Assert.Equal(ChangeType(sourceValue, sourceType), ChangeType(targetValue, sourceType));
	}

	[Fact]
	public void TryConvert_Should_Use_DateTimeKind_Utc_For_Utc_DateTimeOffset()
	{
		var toConvert = DateTimeOffset.UtcNow;

		TypeCache.TryConvert(toConvert, typeof(DateTime), out var result);

		var resultTyped = (DateTime)result;

		Assert.Equal(DateTimeKind.Utc, resultTyped.Kind);

		TypeCache.TryConvert(toConvert, typeof(DateTime?), out var resultNullable);

		var resultNullableTyped = (DateTime?)result;

		Assert.Equal(DateTimeKind.Utc, resultNullableTyped.Value.Kind);
	}

	[Fact]
	public void TryConvert_Should_Use_DateTimeKind_Unspecified_For_Non_Utc_DateTimeOffset()
	{
		var toConvert = DateTimeOffset.Now;

		TypeCache.TryConvert(toConvert, typeof(DateTime), out var result);

		var resultTyped = (DateTime)result;

		Assert.Equal(DateTimeKind.Unspecified, resultTyped.Kind);

		TypeCache.TryConvert(toConvert, typeof(DateTime?), out var resultNullable);

		var resultNullableTyped = (DateTime?)result;

		Assert.Equal(DateTimeKind.Unspecified, resultNullableTyped.Value.Kind);
	}

	[Fact]
	public void TryConvert_GeographyPoint()
	{
		var source = GeographyPoint.Create(10, 10);
		var result = TypeCache.TryConvert(source, typeof(GeographyPoint), out _);
		Assert.True(result);
	}

	[Theory]
	[InlineData("2014-02-01", typeof(Microsoft.OData.Edm.Date), typeof(DateTime))]
	[InlineData("2014-02-01", typeof(Microsoft.OData.Edm.Date), typeof(DateTime?))]
	public void TryConvertODataEdmDate(object value, Type sourceType, Type targetType)
	{
		var sourceValue = ChangeType(value, sourceType);
		var result = TypeCache.TryConvert(sourceValue, targetType, out var targetValue);
		Assert.True(result);
		Assert.Equal(ChangeType(sourceValue, targetType), ChangeType(targetValue, targetType));
	}

	[Theory]
	[InlineData("58D6C94D-B18A-43C9-AC1B-0B5A5BF10C35", typeof(Guid?), typeof(DateTime))]
	[InlineData("58D6C94D-B18A-43C9-AC1B-0B5A5BF10C35", typeof(Guid), typeof(DateTime?))]
	public void TryConvertValueWithoutImplicitDateConversionFails(object value, Type sourceType, Type targetType)
	{
		var sourceValue = ChangeType(value, sourceType);
		var result = TypeCache.TryConvert(sourceValue, targetType, out _);
		Assert.False(result);
	}

	[Fact]
	public void TryConvert_CustomType_WithTypeConverterLambda()
	{
		TypeCache.Converter.RegisterTypeConverter<PrimitiveType>(
			c => new PrimitiveType(new Guid(c.ToString())));

		var source = Guid.NewGuid();
		var result = TypeCache.TryConvert(source, typeof(PrimitiveType), out var converted);
		Assert.True(result);
		Assert.Equal(source, ((PrimitiveType)converted).Value);
	}

	[Fact]
	public void TryConvert_CustomType_WithTypeConverterLambda_Nullable()
	{
		TypeCache.Converter.RegisterTypeConverter<PrimitiveType>(
			c => new PrimitiveType(new Guid(c.ToString())));

		var source = (Guid?)Guid.NewGuid();
		var result = TypeCache.TryConvert(source, typeof(PrimitiveType?), out var converted);
		Assert.True(result);
		Assert.Equal(source, ((PrimitiveType)converted).Value);
	}

	[Fact]
	public void TryConvert_CustomType_WithTypeConverterComponent()
	{
		var source = Guid.NewGuid();
		var result = TypeCache.TryConvert(source, typeof(PrimitiveType), out var converted);
		Assert.True(result);
		Assert.Equal(source, ((PrimitiveType)converted).Value);
	}

	[Fact]
	public void TryConvert_CustomType_WithTypeConverterComponent_Nullable()
	{
		var source = (Guid?)Guid.NewGuid();
		var result = TypeCache.TryConvert(source, typeof(PrimitiveType?), out var converted);
		Assert.True(result);
		Assert.Equal(source, ((PrimitiveType)converted).Value);
	}

	private object ChangeType(object value, Type targetType)
	{
		if (targetType == typeof(string))
		{
			return value.ToString();
		}

		if (targetType == typeof(DateTime))
		{
			return DateTime.Parse(value.ToString());
		}

		if (targetType == typeof(DateTimeOffset))
		{
			return DateTimeOffset.Parse(value.ToString());
		}

		if (targetType.IsEnum)
		{
			return Enum.Parse(targetType, value.ToString(), true);
		}

		if (targetType == typeof(Guid))
		{
			return new Guid(value.ToString());
		}

		if (targetType == typeof(PrimitiveType))
		{
			return new PrimitiveType(new Guid(value.ToString()));
		}

		if (Nullable.GetUnderlyingType(targetType) != null)
		{
			return ChangeType(value, Nullable.GetUnderlyingType(targetType));
		}

		if (targetType == typeof(Microsoft.OData.Edm.Date))
		{
			return Microsoft.OData.Edm.Date.Parse(value.ToString());
		}

		return Convert.ChangeType(value, targetType);
	}

	private enum TestEnum
	{
		Nothing = 0,
		Something = 5
	}
}
