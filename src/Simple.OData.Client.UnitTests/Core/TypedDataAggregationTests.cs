using System.Collections.Generic;
using System.Threading.Tasks;
using Simple.OData.Client.Tests.Core;
using Simple.OData.Client.V4.Adapter.Extensions;
using Xunit;

namespace Simple.OData.Client.Tests.FluentApi;

public class TypedDataAggregationTests : CoreTestBase
{
	public override string MetadataFile => "Northwind4.xml";
	public override IFormatSettings FormatSettings => new ODataV4Format();

	[Fact]
	public async Task Filter()
	{
		var command = _client
			.WithExtensions()
			.For<Product>()
			.Apply(b => b.Filter(x => x.ProductName.Contains("ai")).Filter(x => x.ProductName.StartsWith("Ch")));

		var commandText = await command.GetCommandTextAsync().ConfigureAwait(false);
		Assert.Equal("Products?$apply=filter%28contains%28ProductName%2C%27ai%27%29%20and%20startswith%28ProductName%2C%27Ch%27%29%29", commandText);
	}

	[Fact]
	public async Task AggregateWithAverage()
	{
		var command = _client
			.WithExtensions()
			.For<Product>()
			.Apply(b => b.Aggregate((x, a) => new { AverageUnitPrice = a.Average(x.UnitPrice) }));

		var commandText = await command.GetCommandTextAsync().ConfigureAwait(false);
		Assert.Equal("Products?$apply=aggregate%28UnitPrice%20with%20average%20as%20AverageUnitPrice%29", commandText);
	}

	[Fact]
	public async Task AggregateWithAverageAsConcreteDestinationType()
	{
		var command = _client
			.WithExtensions()
			.For<Product>()
			.Apply(b => b.Aggregate((x, a) => new ProductGroupedByCategoryNameWithAggregatedProperties
			{
				AverageUnitPrice = a.Average(x.UnitPrice)
			}));

		var commandText = await command.GetCommandTextAsync().ConfigureAwait(false);
		Assert.Equal("Products?$apply=aggregate%28UnitPrice%20with%20average%20as%20AverageUnitPrice%29", commandText);
	}

	[Fact]
	public async Task AggregateWithSum()
	{
		var command = _client
			.WithExtensions()
			.For<Product>()
			.Apply(b => b.Aggregate((x, a) => new { Total = a.Sum(x.UnitPrice) }));

		var commandText = await command.GetCommandTextAsync().ConfigureAwait(false);
		Assert.Equal("Products?$apply=aggregate%28UnitPrice%20with%20sum%20as%20Total%29", commandText);
	}

	[Fact]
	public async Task AggregateWithMin()
	{
		var command = _client
			.WithExtensions()
			.For<Product>()
			.Apply(b => b.Aggregate((x, a) => new { MinPrice = a.Min(x.UnitPrice) }));

		var commandText = await command.GetCommandTextAsync().ConfigureAwait(false);
		Assert.Equal("Products?$apply=aggregate%28UnitPrice%20with%20min%20as%20MinPrice%29", commandText);
	}

	[Fact]
	public async Task AggregateWithMax()
	{
		var command = _client
			.WithExtensions()
			.For<Product>()
			.Apply(b => b.Aggregate((x, a) => new { MaxPrice = a.Max(x.UnitPrice) }));

		var commandText = await command.GetCommandTextAsync().ConfigureAwait(false);
		Assert.Equal("Products?$apply=aggregate%28UnitPrice%20with%20max%20as%20MaxPrice%29", commandText);
	}

	[Fact]
	public async Task AggregateWithDistinctCount()
	{
		var command = _client
			.WithExtensions()
			.For<Product>()
			.Apply(b => b.Aggregate((x, a) => new { Count = a.CountDistinct(x.ProductName) }));

		var commandText = await command.GetCommandTextAsync().ConfigureAwait(false);
		Assert.Equal("Products?$apply=aggregate%28ProductName%20with%20countdistinct%20as%20Count%29", commandText);
	}

	[Fact]
	public async Task AggregateWithCount()
	{
		var command = _client
			.WithExtensions()
			.For<Product>()
			.Apply(b => b.Aggregate((x, a) => new { Count = a.Count() }));

		var commandText = await command.GetCommandTextAsync().ConfigureAwait(false);
		Assert.Equal("Products?$apply=aggregate%28%24count%20as%20Count%29", commandText);
	}

	[Fact]
	public async Task SimpleGroupBy()
	{
		var command = _client
			.WithExtensions()
			.For<Product>()
			.Apply(b => b.GroupBy((x, _) => x.Category.CategoryName));

		var commandText = await command.GetCommandTextAsync().ConfigureAwait(false);
		Assert.Equal("Products?$apply=groupby%28%28Category%2FCategoryName%29%29", commandText);
	}

	[Fact]
	public async Task SimpleGroupByWithMultipleProperties()
	{
		var command = _client
			.WithExtensions()
			.For<Product>()
			.Apply(b => b.GroupBy((x, _) => new
			{
				x.Category.CategoryName,
				x.ProductName
			}));

		var commandText = await command.GetCommandTextAsync().ConfigureAwait(false);
		Assert.Equal("Products?$apply=groupby%28%28Category%2FCategoryName%2CProductName%29%29", commandText);
	}

	[Fact]
	public async Task GroupByWithAggregation()
	{
		var command = _client
			.WithExtensions()
			.For<Product>()
			.Apply(b => b.GroupBy((x, a) => new
			{
				x.Category.CategoryName,
				AverageUnitPrice = a.Average(x.UnitPrice)
			}));

		var commandText = await command.GetCommandTextAsync().ConfigureAwait(false);
		Assert.Equal("Products?$apply=groupby%28%28Category%2FCategoryName%29%2Caggregate%28UnitPrice%20with%20average%20as%20AverageUnitPrice%29%29", commandText);
	}

	[Fact]
	public async Task GroupByWithAggregationAndNestedAssignments()
	{
		var command = _client
			.WithExtensions()
			.For<Product>()
			.Apply(b => b.GroupBy((x, a) => new
			{
				x.ProductName,
				Category = new Category
				{
					CategoryName = x.Category.CategoryName
				},
				AverageUnitPrice = a.Average(x.UnitPrice)
			}));

		var commandText = await command.GetCommandTextAsync().ConfigureAwait(false);
		Assert.Equal("Products?$apply=groupby%28%28ProductName%2CCategory%2FCategoryName%29%2Caggregate%28UnitPrice%20with%20average%20as%20AverageUnitPrice%29%29", commandText);
	}

	[Fact]
	public async Task GroupByWithAggregationOfMultipleProperties()
	{
		var command = _client
			.WithExtensions()
			.For<Product>()
			.Apply(b => b.GroupBy((x, a) => new
			{
				x.Category.CategoryName,
				AverageUnitPrice = a.Average(x.UnitPrice),
				Count = a.Count()
			}));

		var commandText = await command.GetCommandTextAsync().ConfigureAwait(false);
		Assert.Equal("Products?$apply=groupby%28%28Category%2FCategoryName%29%2Caggregate%28UnitPrice%20with%20average%20as%20AverageUnitPrice%2C%24count%20as%20Count%29%29", commandText);
	}

	[Fact]
	public async Task FilterThenGroupByWithAggregationAsConcreteDestinationType()
	{
		var categories = new List<string> { "Beverage", "Food" };
		var command = _client
			.WithExtensions()
			.For<Product>()
			.Apply(b => b
				.Filter(x => categories.Contains(x.Category.CategoryName))
				.GroupBy((x, a) => new ProductGroupedByCategoryNameWithAggregatedProperties
				{
					Category = new Category { CategoryName = x.Category.CategoryName },
					AverageUnitPrice = a.Average(x.UnitPrice)
				}));

		var commandText = await command.GetCommandTextAsync().ConfigureAwait(false);
		Assert.Equal("Products?$apply=filter%28%28Category%2FCategoryName%20in%20%28%27Beverage%27%2C%27Food%27%29%29%29%2Fgroupby%28%28Category%2FCategoryName%29%2Caggregate%28UnitPrice%20with%20average%20as%20AverageUnitPrice%29%29", commandText);
	}

	[Fact]
	public async Task GroupByWithAggregationThenFilter()
	{
		var command = _client
			.WithExtensions()
			.For<Product>()
			.Apply(b => b
				.GroupBy((x, a) => new
				{
					x.Category.CategoryName,
					MaxUnitPrice = a.Max(x.UnitPrice)
				})
				.Filter(x => x.MaxUnitPrice > 100));

		var commandText = await command.GetCommandTextAsync().ConfigureAwait(false);
		Assert.Equal("Products?$apply=groupby%28%28Category%2FCategoryName%29%2Caggregate%28UnitPrice%20with%20max%20as%20MaxUnitPrice%29%29%2Ffilter%28MaxUnitPrice%20gt%20100%29", commandText);
	}

	[Fact]
	public async Task GroupByWithAggregationThenAggregate()
	{
		var command = _client
			.WithExtensions()
			.For<Product>()
			.Apply(b => b
				.GroupBy((x, a) => new
				{
					x.Category.CategoryName,
					AverageUnitPrice = a.Average(x.UnitPrice)
				})
				.Aggregate((x, a) => new { MaxPrice = a.Max(x.AverageUnitPrice) }));

		var commandText = await command.GetCommandTextAsync().ConfigureAwait(false);
		Assert.Equal("Products?$apply=groupby%28%28Category%2FCategoryName%29%2Caggregate%28UnitPrice%20with%20average%20as%20AverageUnitPrice%29%29%2Faggregate%28AverageUnitPrice%20with%20max%20as%20MaxPrice%29", commandText);
	}

	[Fact]
	public async Task GroupByWithAggregationThenGroupByWithAggregation()
	{
		var command = _client
			.WithExtensions()
			.For<Product>()
			.Apply(b => b
				.GroupBy((x, a) => new
				{
					x.Category.CategoryName,
					AverageUnitPrice = a.Average(x.UnitPrice)
				})
				.GroupBy((x, a) => new
				{
					x.AverageUnitPrice,
					CategoriesCount = a.Count()
				}));

		var commandText = await command.GetCommandTextAsync().ConfigureAwait(false);
		Assert.Equal("Products?$apply=groupby%28%28Category%2FCategoryName%29%2Caggregate%28UnitPrice%20with%20average%20as%20AverageUnitPrice%29%29%2Fgroupby%28%28AverageUnitPrice%29%2Caggregate%28%24count%20as%20CategoriesCount%29%29", commandText);
	}

	[Fact]
	public async Task GroupByWithAggregationThenOrderBy()
	{
		var command = _client
			.WithExtensions()
			.For<Product>()
			.Apply(b => b.GroupBy((x, a) => new
			{
				x.Category.CategoryName,
				AverageUnitPrice = a.Average(x.UnitPrice)
			}))
			.OrderBy(x => x.AverageUnitPrice);

		var commandText = await command.GetCommandTextAsync().ConfigureAwait(false);
		Assert.Equal("Products?$apply=groupby%28%28Category%2FCategoryName%29%2Caggregate%28UnitPrice%20with%20average%20as%20AverageUnitPrice%29%29&$orderby=AverageUnitPrice", commandText);
	}

	[Fact]
	public async Task MultipleApplies()
	{
		var command = _client
			.WithExtensions()
			.For<Product>()
			.Apply(b => b.Filter(x => x.Category.CategoryName.Contains("ab")))
			.Apply(b => b.GroupBy((x, a) => new
			{
				x.Category.CategoryName,
				AverageUnitPrice = a.Average(x.UnitPrice)
			}))
			.Apply(b => b.Filter(x => x.AverageUnitPrice > 100));

		var commandText = await command.GetCommandTextAsync().ConfigureAwait(false);
		Assert.Equal("Products?$apply=filter%28contains%28Category%2FCategoryName%2C%27ab%27%29%29%2Fgroupby%28%28Category%2FCategoryName%29%2Caggregate%28UnitPrice%20with%20average%20as%20AverageUnitPrice%29%29%2Ffilter%28AverageUnitPrice%20gt%20100%29", commandText);
	}
}

internal class ProductGroupedByCategoryNameWithAggregatedProperties : Product
{
	public decimal AverageUnitPrice { get; set; }
}
