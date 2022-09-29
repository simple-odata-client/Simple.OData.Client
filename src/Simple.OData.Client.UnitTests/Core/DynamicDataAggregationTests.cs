using System.Threading.Tasks;
using Simple.OData.Client.V4.Adapter.Extensions;
using Xunit;

namespace Simple.OData.Client.Tests.Core;

public class DynamicDataAggregationTests : CoreTestBase
{
	public override string MetadataFile => "Northwind4.xml";
	public override IFormatSettings FormatSettings => new ODataV4Format();

	[Fact]
	public async Task FilterWithStringsAsDictionary()
	{
		var b = ODataDynamicDataAggregation.Builder;
		var command = _client
			.WithExtensions()
			.For("Products")
			.Apply(b.Filter("contains(ProductName,'ai')").Filter("startswith(ProductName,'Ch')"));

		var commandText = await command.GetCommandTextAsync().ConfigureAwait(false);
		Assert.Equal("Products?$apply=filter%28contains%28ProductName%2C%27ai%27%29%20and%20startswith%28ProductName%2C%27Ch%27%29%29", commandText);
	}

	[Fact]
	public async Task FilterWithStringsAsODataEntries()
	{
		var x = ODataDynamic.Expression;
		var b = ODataDynamicDataAggregation.Builder;
		var command = _client
			.WithExtensions()
			.For(x.Product)
			.Apply(b.Filter("contains(ProductName,'ai')").Filter("startswith(ProductName,'Ch')"));

		var commandText = await command.GetCommandTextAsync();
		Assert.Equal("Products?$apply=filter%28contains%28ProductName%2C%27ai%27%29%20and%20startswith%28ProductName%2C%27Ch%27%29%29", commandText);
	}

	[Fact]
	public async Task FilterWithODataExpressionAsDictionary()
	{
		var x = ODataDynamic.Expression;
		var b = ODataDynamicDataAggregation.Builder;
		var command = _client
			.WithExtensions()
			.For("Products")
			.Apply(b.Filter(x.ProductName.Contains("ai")));

		var commandText = await command.GetCommandTextAsync();
		Assert.Equal("Products?$apply=filter%28contains%28ProductName%2C%27ai%27%29%29", commandText);
	}

	[Fact]
	public async Task FilterWithODataExpressionAsODataEntries()
	{
		var x = ODataDynamic.Expression;
		var b = ODataDynamicDataAggregation.Builder;
		var command = _client
			.WithExtensions()
			.For(x.Product)
			.Apply(b.Filter(x.ProductName.Contains("ai")));

		var commandText = await command.GetCommandTextAsync();
		Assert.Equal("Products?$apply=filter%28contains%28ProductName%2C%27ai%27%29%29", commandText);
	}

	[Fact]
	public async Task FilterWithMultipleClausesAsDictionary()
	{
		var x = ODataDynamic.Expression;
		var b = ODataDynamicDataAggregation.Builder;
		var command = _client
			.WithExtensions()
			.For("Products")
			.Apply(b.Filter(x.ProductName.Contains("ai")).Filter(x.ProductName.StartsWith("Ch")));

		var commandText = await command.GetCommandTextAsync();
		Assert.Equal("Products?$apply=filter%28contains%28ProductName%2C%27ai%27%29%20and%20startswith%28ProductName%2C%27Ch%27%29%29", commandText);
	}

	[Fact]
	public async Task FilterWithMultipleClausesAsODataEntries()
	{
		var x = ODataDynamic.Expression;
		var b = ODataDynamicDataAggregation.Builder;
		var command = _client
			.WithExtensions()
			.For(x.Product)
			.Apply(b.Filter(x.ProductName.Contains("ai")).Filter(x.ProductName.StartsWith("Ch")));

		var commandText = await command.GetCommandTextAsync();
		Assert.Equal("Products?$apply=filter%28contains%28ProductName%2C%27ai%27%29%20and%20startswith%28ProductName%2C%27Ch%27%29%29", commandText);
	}

	[Fact]
	public async Task AggregateWithAverageAsDictionary()
	{
		var x = ODataDynamic.Expression;
		var b = ODataDynamicDataAggregation.Builder;
		var a = ODataDynamicDataAggregation.AggregationFunction;
		var command = _client
			.WithExtensions()
			.For("Products")
			.Apply(b.Aggregate(new { AverageUnitPrice = a.Average(x.UnitPrice) }));

		var commandText = await command.GetCommandTextAsync().ConfigureAwait(false);
		Assert.Equal("Products?$apply=aggregate%28UnitPrice%20with%20average%20as%20AverageUnitPrice%29", commandText);
	}

	[Fact]
	public async Task AggregateWithAverageAsODataEntries()
	{
		var x = ODataDynamic.Expression;
		var b = ODataDynamicDataAggregation.Builder;
		var a = ODataDynamicDataAggregation.AggregationFunction;
		var command = _client
			.WithExtensions()
			.For(x.Product)
			.Apply(b.Aggregate(new { AverageUnitPrice = a.Average(x.UnitPrice) }));

		var commandText = await command.GetCommandTextAsync();
		Assert.Equal("Products?$apply=aggregate%28UnitPrice%20with%20average%20as%20AverageUnitPrice%29", commandText);
	}

	[Fact]
	public async Task AggregateWithSumAsDictionary()
	{
		var x = ODataDynamic.Expression;
		var b = ODataDynamicDataAggregation.Builder;
		var a = ODataDynamicDataAggregation.AggregationFunction;
		var command = _client
			.WithExtensions()
			.For("Products")
			.Apply(b.Aggregate(new { Total = a.Sum(x.UnitPrice) }));

		var commandText = await command.GetCommandTextAsync().ConfigureAwait(false);
		Assert.Equal("Products?$apply=aggregate%28UnitPrice%20with%20sum%20as%20Total%29", commandText);
	}

	[Fact]
	public async Task AggregateWithSumAsODataEntries()
	{
		var x = ODataDynamic.Expression;
		var b = ODataDynamicDataAggregation.Builder;
		var a = ODataDynamicDataAggregation.AggregationFunction;
		var command = _client
			.WithExtensions()
			.For(x.Product)
			.Apply(b.Aggregate(new { Total = a.Sum(x.UnitPrice) }));

		var commandText = await command.GetCommandTextAsync();
		Assert.Equal("Products?$apply=aggregate%28UnitPrice%20with%20sum%20as%20Total%29", commandText);
	}

	[Fact]
	public async Task AggregateWithMinAsDictionary()
	{
		var x = ODataDynamic.Expression;
		var b = ODataDynamicDataAggregation.Builder;
		var a = ODataDynamicDataAggregation.AggregationFunction;
		var command = _client
			.WithExtensions()
			.For("Products")
			.Apply(b.Aggregate(new { MinPrice = a.Min(x.UnitPrice) }));

		var commandText = await command.GetCommandTextAsync().ConfigureAwait(false);
		Assert.Equal("Products?$apply=aggregate%28UnitPrice%20with%20min%20as%20MinPrice%29", commandText);
	}

	[Fact]
	public async Task AggregateWithMinAsODataEntries()
	{
		var x = ODataDynamic.Expression;
		var b = ODataDynamicDataAggregation.Builder;
		var a = ODataDynamicDataAggregation.AggregationFunction;
		var command = _client
			.WithExtensions()
			.For(x.Product)
			.Apply(b.Aggregate(new { MinPrice = a.Min(x.UnitPrice) }));

		var commandText = await command.GetCommandTextAsync();
		Assert.Equal("Products?$apply=aggregate%28UnitPrice%20with%20min%20as%20MinPrice%29", commandText);
	}

	[Fact]
	public async Task AggregateWithMaxAsDictionary()
	{
		var x = ODataDynamic.Expression;
		var b = ODataDynamicDataAggregation.Builder;
		var a = ODataDynamicDataAggregation.AggregationFunction;
		var command = _client
			.WithExtensions()
			.For("Products")
			.Apply(b.Aggregate(new { MaxPrice = a.Max(x.UnitPrice) }));

		var commandText = await command.GetCommandTextAsync().ConfigureAwait(false);
		Assert.Equal("Products?$apply=aggregate%28UnitPrice%20with%20max%20as%20MaxPrice%29", commandText);
	}

	[Fact]
	public async Task AggregateWithMaxAsODateEntries()
	{
		var x = ODataDynamic.Expression;
		var b = ODataDynamicDataAggregation.Builder;
		var a = ODataDynamicDataAggregation.AggregationFunction;
		var command = _client
			.WithExtensions()
			.For(x.Product)
			.Apply(b.Aggregate(new { MaxPrice = a.Max(x.UnitPrice) }));

		var commandText = await command.GetCommandTextAsync();
		Assert.Equal("Products?$apply=aggregate%28UnitPrice%20with%20max%20as%20MaxPrice%29", commandText);
	}

	[Fact]
	public async Task AggregateWithDistinctCountAsDictionary()
	{
		var x = ODataDynamic.Expression;
		var b = ODataDynamicDataAggregation.Builder;
		var a = ODataDynamicDataAggregation.AggregationFunction;
		var command = _client
			.WithExtensions()
			.For("Products")
			.Apply(b.Aggregate(new { Count = a.CountDistinct(x.ProductName) }));

		var commandText = await command.GetCommandTextAsync().ConfigureAwait(false);
		Assert.Equal("Products?$apply=aggregate%28ProductName%20with%20countdistinct%20as%20Count%29", commandText);
	}

	[Fact]
	public async Task AggregateWithDistinctCountAsODataEntries()
	{
		var x = ODataDynamic.Expression;
		var b = ODataDynamicDataAggregation.Builder;
		var a = ODataDynamicDataAggregation.AggregationFunction;
		var command = _client
			.WithExtensions()
			.For(x.Product)
			.Apply(b.Aggregate(new { Count = a.CountDistinct(x.ProductName) }));

		var commandText = await command.GetCommandTextAsync();
		Assert.Equal("Products?$apply=aggregate%28ProductName%20with%20countdistinct%20as%20Count%29", commandText);
	}

	[Fact]
	public async Task AggregateWithCountAsDictionary()
	{
		_ = ODataDynamic.Expression;
		var b = ODataDynamicDataAggregation.Builder;
		var a = ODataDynamicDataAggregation.AggregationFunction;
		var command = _client
			.WithExtensions()
			.For("Products")
			.Apply(b.Aggregate(new { Count = a.Count() }));

		var commandText = await command.GetCommandTextAsync().ConfigureAwait(false);
		Assert.Equal("Products?$apply=aggregate%28%24count%20as%20Count%29", commandText);
	}

	[Fact]
	public async Task AggregateWithCountAsODataEntries()
	{
		var x = ODataDynamic.Expression;
		var b = ODataDynamicDataAggregation.Builder;
		var a = ODataDynamicDataAggregation.AggregationFunction;
		var command = _client
			.WithExtensions()
			.For(x.Product)
			.Apply(b.Aggregate(new { Count = a.Count() }));

		var commandText = await command.GetCommandTextAsync();
		Assert.Equal("Products?$apply=aggregate%28%24count%20as%20Count%29", commandText);
	}

	[Fact]
	public async Task SimpleGroupByAsDictionary()
	{
		var x = ODataDynamic.Expression;
		var b = ODataDynamicDataAggregation.Builder;
		var command = _client
			.WithExtensions()
			.For("Products")
			.Apply(b.GroupBy(x.Category.CategoryName));

		var commandText = await command.GetCommandTextAsync();
		Assert.Equal("Products?$apply=groupby%28%28Category%2FCategoryName%29%29", commandText);
	}

	[Fact]
	public async Task SimpleGroupByAsODataEntries()
	{
		var x = ODataDynamic.Expression;
		var b = ODataDynamicDataAggregation.Builder;
		var command = _client
			.WithExtensions()
			.For(x.Product)
			.Apply(b.GroupBy(x.Category.CategoryName));

		var commandText = await command.GetCommandTextAsync();
		Assert.Equal("Products?$apply=groupby%28%28Category%2FCategoryName%29%29", commandText);
	}

	[Fact]
	public async Task SimpleGroupByWithMultiplePropertiesAsDictionary()
	{
		var x = ODataDynamic.Expression;
		var b = ODataDynamicDataAggregation.Builder;
		var command = _client
			.WithExtensions()
			.For("Products")
			.Apply(b.GroupBy(new { x.Category.CategoryName, x.ProductName }));

		var commandText = await command.GetCommandTextAsync().ConfigureAwait(false);
		Assert.Equal("Products?$apply=groupby%28%28Category%2FCategoryName%2CProductName%29%29", commandText);
	}

	[Fact]
	public async Task SimpleGroupByWithMultiplePropertiesAsODataEntries()
	{
		var x = ODataDynamic.Expression;
		var b = ODataDynamicDataAggregation.Builder;
		var command = _client
			.WithExtensions()
			.For(x.Product)
			.Apply(b.GroupBy(new { x.Category.CategoryName, x.ProductName }));

		var commandText = await command.GetCommandTextAsync();
		Assert.Equal("Products?$apply=groupby%28%28Category%2FCategoryName%2CProductName%29%29", commandText);
	}

	[Fact]
	public async Task GroupByWithAggregationAsDictionary()
	{
		var x = ODataDynamic.Expression;
		var b = ODataDynamicDataAggregation.Builder;
		var a = ODataDynamicDataAggregation.AggregationFunction;
		var command = _client
			.WithExtensions()
			.For("Products")
			.Apply(b.GroupBy(new
			{
				x.Category.CategoryName,
				AverageUnitPrice = a.Average(x.UnitPrice)
			}));

		var commandText = await command.GetCommandTextAsync().ConfigureAwait(false);
		Assert.Equal("Products?$apply=groupby%28%28Category%2FCategoryName%29%2Caggregate%28UnitPrice%20with%20average%20as%20AverageUnitPrice%29%29", commandText);
	}

	[Fact]
	public async Task GroupByWithAggregationAsODataEntries()
	{
		var x = ODataDynamic.Expression;
		var b = ODataDynamicDataAggregation.Builder;
		var a = ODataDynamicDataAggregation.AggregationFunction;
		var command = _client
			.WithExtensions()
			.For(x.Product)
			.Apply(b.GroupBy(new
			{
				x.Category.CategoryName,
				AverageUnitPrice = a.Average(x.UnitPrice)
			}));

		var commandText = await command.GetCommandTextAsync();
		Assert.Equal("Products?$apply=groupby%28%28Category%2FCategoryName%29%2Caggregate%28UnitPrice%20with%20average%20as%20AverageUnitPrice%29%29", commandText);
	}

	[Fact]
	public async Task GroupByWithAggregationOfMultiplePropertiesAsDictionary()
	{
		var x = ODataDynamic.Expression;
		var b = ODataDynamicDataAggregation.Builder;
		var a = ODataDynamicDataAggregation.AggregationFunction;
		var command = _client
			.WithExtensions()
			.For("Products")
			.Apply(b.GroupBy(new
			{
				x.Category.CategoryName,
				AverageUnitPrice = a.Average(x.UnitPrice),
				Count = a.Count()
			}));

		var commandText = await command.GetCommandTextAsync().ConfigureAwait(false);
		Assert.Equal("Products?$apply=groupby%28%28Category%2FCategoryName%29%2Caggregate%28UnitPrice%20with%20average%20as%20AverageUnitPrice%2C%24count%20as%20Count%29%29", commandText);
	}

	[Fact]
	public async Task GroupByWithAggregationOfMultiplePropertiesAsODataEntries()
	{
		var x = ODataDynamic.Expression;
		var b = ODataDynamicDataAggregation.Builder;
		var a = ODataDynamicDataAggregation.AggregationFunction;
		var command = _client
			.WithExtensions()
			.For(x.Product)
			.Apply(b.GroupBy(new
			{
				x.Category.CategoryName,
				AverageUnitPrice = a.Average(x.UnitPrice),
				Count = a.Count()
			}));

		var commandText = await command.GetCommandTextAsync();
		Assert.Equal("Products?$apply=groupby%28%28Category%2FCategoryName%29%2Caggregate%28UnitPrice%20with%20average%20as%20AverageUnitPrice%2C%24count%20as%20Count%29%29", commandText);
	}

	[Fact]
	public async Task GroupByWithAggregationThenFilterAsDictionary()
	{
		var x = ODataDynamic.Expression;
		var b = ODataDynamicDataAggregation.Builder;
		var a = ODataDynamicDataAggregation.AggregationFunction;
		var command = _client
			.WithExtensions()
			.For("Products")
			.Apply(b
				.GroupBy(new
				{
					x.Category.CategoryName,
					MaxUnitPrice = a.Max(x.UnitPrice)
				})
				.Filter(x.MaxUnitPrice > 100));

		var commandText = await command.GetCommandTextAsync();
		Assert.Equal("Products?$apply=groupby%28%28Category%2FCategoryName%29%2Caggregate%28UnitPrice%20with%20max%20as%20MaxUnitPrice%29%29%2Ffilter%28MaxUnitPrice%20gt%20100%29", commandText);
	}

	[Fact]
	public async Task GroupByWithAggregationThenFilterAsODataEntries()
	{
		var x = ODataDynamic.Expression;
		var b = ODataDynamicDataAggregation.Builder;
		var a = ODataDynamicDataAggregation.AggregationFunction;
		var command = _client
			.WithExtensions()
			.For(x.Product)
			.Apply(b
				.GroupBy(new
				{
					x.Category.CategoryName,
					MaxUnitPrice = a.Max(x.UnitPrice)
				})
				.Filter(x.MaxUnitPrice > 100));

		var commandText = await command.GetCommandTextAsync();
		Assert.Equal("Products?$apply=groupby%28%28Category%2FCategoryName%29%2Caggregate%28UnitPrice%20with%20max%20as%20MaxUnitPrice%29%29%2Ffilter%28MaxUnitPrice%20gt%20100%29", commandText);
	}

	[Fact]
	public async Task GroupByWithAggregationThenAggregateAsDictionary()
	{
		var x = ODataDynamic.Expression;
		var b = ODataDynamicDataAggregation.Builder;
		var a = ODataDynamicDataAggregation.AggregationFunction;
		var command = _client
			.WithExtensions()
			.For("Products")
			.Apply(b
				.GroupBy(new
				{
					x.Category.CategoryName,
					AverageUnitPrice = a.Average(x.UnitPrice)
				})
				.Aggregate(new { MaxPrice = a.Max(x.AverageUnitPrice) }));

		var commandText = await command.GetCommandTextAsync().ConfigureAwait(false);
		Assert.Equal("Products?$apply=groupby%28%28Category%2FCategoryName%29%2Caggregate%28UnitPrice%20with%20average%20as%20AverageUnitPrice%29%29%2Faggregate%28AverageUnitPrice%20with%20max%20as%20MaxPrice%29", commandText);
	}

	[Fact]
	public async Task GroupByWithAggregationThenAggregateAsODataEntries()
	{
		var x = ODataDynamic.Expression;
		var b = ODataDynamicDataAggregation.Builder;
		var a = ODataDynamicDataAggregation.AggregationFunction;
		var command = _client
			.WithExtensions()
			.For(x.Product)
			.Apply(b
				.GroupBy(new
				{
					x.Category.CategoryName,
					AverageUnitPrice = a.Average(x.UnitPrice)
				})
				.Aggregate(new { MaxPrice = a.Max(x.AverageUnitPrice) }));

		var commandText = await command.GetCommandTextAsync();
		Assert.Equal("Products?$apply=groupby%28%28Category%2FCategoryName%29%2Caggregate%28UnitPrice%20with%20average%20as%20AverageUnitPrice%29%29%2Faggregate%28AverageUnitPrice%20with%20max%20as%20MaxPrice%29", commandText);
	}

	[Fact]
	public async Task GroupByWithAggregationThenGroupByWithAggregationAsDictionary()
	{
		var x = ODataDynamic.Expression;
		var b = ODataDynamicDataAggregation.Builder;
		var a = ODataDynamicDataAggregation.AggregationFunction;
		var command = _client
			.WithExtensions()
			.For("Products")
			.Apply(b
				.GroupBy(new
				{
					x.Category.CategoryName,
					AverageUnitPrice = a.Average(x.UnitPrice)
				})
				.GroupBy(new
				{
					x.AverageUnitPrice,
					CategoriesCount = a.Count()
				}));

		var commandText = await command.GetCommandTextAsync().ConfigureAwait(false);
		Assert.Equal("Products?$apply=groupby%28%28Category%2FCategoryName%29%2Caggregate%28UnitPrice%20with%20average%20as%20AverageUnitPrice%29%29%2Fgroupby%28%28AverageUnitPrice%29%2Caggregate%28%24count%20as%20CategoriesCount%29%29", commandText);
	}

	[Fact]
	public async Task GroupByWithAggregationThenGroupByWithAggregationAsODataEntries()
	{
		var x = ODataDynamic.Expression;
		var b = ODataDynamicDataAggregation.Builder;
		var a = ODataDynamicDataAggregation.AggregationFunction;
		var command = _client
			.WithExtensions()
			.For(x.Product)
			.Apply(b
				.GroupBy(new
				{
					x.Category.CategoryName,
					AverageUnitPrice = a.Average(x.UnitPrice)
				})
				.GroupBy(new
				{
					x.AverageUnitPrice,
					CategoriesCount = a.Count()
				}));

		var commandText = await command.GetCommandTextAsync();
		Assert.Equal("Products?$apply=groupby%28%28Category%2FCategoryName%29%2Caggregate%28UnitPrice%20with%20average%20as%20AverageUnitPrice%29%29%2Fgroupby%28%28AverageUnitPrice%29%2Caggregate%28%24count%20as%20CategoriesCount%29%29", commandText);
	}

	[Fact]
	public async Task GroupByWithAggregationThenOrderByAsDictionary()
	{
		var x = ODataDynamic.Expression;
		var b = ODataDynamicDataAggregation.Builder;
		var a = ODataDynamicDataAggregation.AggregationFunction;
		var command = _client
			.WithExtensions()
			.For("Products")
			.Apply(b.GroupBy(new
			{
				x.Category.CategoryName,
				AverageUnitPrice = a.Average(x.UnitPrice)
			}))
			.OrderBy(x.AverageUnitPrice);

		var commandText = await command.GetCommandTextAsync();
		Assert.Equal("Products?$apply=groupby%28%28Category%2FCategoryName%29%2Caggregate%28UnitPrice%20with%20average%20as%20AverageUnitPrice%29%29&$orderby=AverageUnitPrice", commandText);
	}

	[Fact]
	public async Task GroupByWithAggregationThenOrderByAsODataEntries()
	{
		var x = ODataDynamic.Expression;
		var b = ODataDynamicDataAggregation.Builder;
		var a = ODataDynamicDataAggregation.AggregationFunction;
		var command = _client
			.WithExtensions()
			.For(x.Product)
			.Apply(b.GroupBy(new
			{
				x.Category.CategoryName,
				AverageUnitPrice = a.Average(x.UnitPrice)
			}))
			.OrderBy(x.AverageUnitPrice);

		var commandText = await command.GetCommandTextAsync();
		Assert.Equal("Products?$apply=groupby%28%28Category%2FCategoryName%29%2Caggregate%28UnitPrice%20with%20average%20as%20AverageUnitPrice%29%29&$orderby=AverageUnitPrice", commandText);
	}

	[Fact]
	public async Task MultipleAppliesAsDictionary()
	{
		var x = ODataDynamic.Expression;
		var b = ODataDynamicDataAggregation.Builder;
		var a = ODataDynamicDataAggregation.AggregationFunction;
		var command = _client
			.WithExtensions()
			.For("Products")
			.Apply(b.Filter("contains(Category/CategoryName,'v')"))
			.Apply(b.GroupBy(new
			{
				x.Category.CategoryName,
				AverageUnitPrice = a.Average(x.UnitPrice)
			}));

		var commandText = await command.GetCommandTextAsync().ConfigureAwait(false);
		Assert.Equal("Products?$apply=filter%28contains%28Category%2FCategoryName%2C%27v%27%29%29%2Fgroupby%28%28Category%2FCategoryName%29%2Caggregate%28UnitPrice%20with%20average%20as%20AverageUnitPrice%29%29", commandText);
	}

	[Fact]
	public async Task MultipleAppliesAsODataEntries()
	{
		var x = ODataDynamic.Expression;
		var b = ODataDynamicDataAggregation.Builder;
		var a = ODataDynamicDataAggregation.AggregationFunction;
		var command = _client
			.WithExtensions()
			.For(x.Product)
			.Apply(b.Filter(x.Category.CategoryName.Contains("v")))
			.Apply(b.GroupBy(new
			{
				x.Category.CategoryName,
				AverageUnitPrice = a.Average(x.UnitPrice)
			}));

		var commandText = await command.GetCommandTextAsync();
		Assert.Equal("Products?$apply=filter%28contains%28Category%2FCategoryName%2C%27v%27%29%29%2Fgroupby%28%28Category%2FCategoryName%29%2Caggregate%28UnitPrice%20with%20average%20as%20AverageUnitPrice%29%29", commandText);
	}
}
