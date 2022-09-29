using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Simple.OData.Client.Tests.FluentApi;

public class UpdateTests : TestBase
{
	[Fact]
	public async Task UpdateByKey()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var product = await client
			.For("Products")
			.Set(new { ProductName = "Test1", UnitPrice = 18m })
			.InsertEntryAsync().ConfigureAwait(false);

		await client
			.For("Products")
			.Key(product["ProductID"])
			.Set(new { UnitPrice = 123m })
			.UpdateEntryAsync().ConfigureAwait(false);

		product = await client
			.For("Products")
			.Filter("ProductName eq 'Test1'")
			.FindEntryAsync().ConfigureAwait(false);

		Assert.Equal(123m, product["UnitPrice"]);
	}

	[Fact(Skip = "Cannot mock")]
	public async Task UpdateByKeyClearMetadataCache()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var product = await client
			.For("Products")
			.Set(new { ProductName = "Test1", UnitPrice = 18m })
			.InsertEntryAsync().ConfigureAwait(false);

		client.Session.ClearMetadataCache();
		await client
			.For("Products")
			.Key(product["ProductID"])
			.Set(new { UnitPrice = 123m })
			.UpdateEntryAsync().ConfigureAwait(false);

		product = await client
			.For("Products")
			.Filter("ProductName eq 'Test1'")
			.FindEntryAsync().ConfigureAwait(false);

		Assert.Equal(123m, product["UnitPrice"]);
	}

	[Fact]
	public async Task UpdateByFilter()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		_ = await client
			.For("Products")
			.Set(new { ProductName = "Test1", UnitPrice = 18m })
			.InsertEntryAsync().ConfigureAwait(false);

		await client
			.For("Products")
			.Filter("ProductName eq 'Test1'")
			.Set(new { UnitPrice = 123m })
			.UpdateEntryAsync().ConfigureAwait(false);

		var product = await client
			.For("Products")
			.Filter("ProductName eq 'Test1'")
			.FindEntryAsync().ConfigureAwait(false);

		Assert.Equal(123m, product["UnitPrice"]);
	}

	[Fact]
	public async Task UpdateMultipleWithResult()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		_ = await client
			.For("Products")
			.Set(new { ProductName = "Test1", UnitPrice = 18m })
			.InsertEntryAsync().ConfigureAwait(false);

		var product = (await client
			.For("Products")
			.Filter("ProductName eq 'Test1'")
			.Set(new { UnitPrice = 123m })
			.UpdateEntriesAsync().ConfigureAwait(false)).Single();

		Assert.Equal(123m, product["UnitPrice"]);
	}

	[Fact]
	public async Task UpdateMultipleNoResult()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		_ = await client
			.For("Products")
			.Set(new { ProductName = "Test1", UnitPrice = 18m })
			.InsertEntryAsync().ConfigureAwait(false);

		var product = (await client
			.For("Products")
			.Filter("ProductName eq 'Test1'")
			.Set(new { UnitPrice = 123m })
			.UpdateEntriesAsync(false).ConfigureAwait(false)).Single();
		Assert.Null(product);

		product = await client
			.For("Products")
			.Filter("ProductName eq 'Test1'")
			.FindEntryAsync().ConfigureAwait(false);

		Assert.Equal(123m, product["UnitPrice"]);
	}

	[Fact]
	public async Task UpdateByObjectAsKey()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var product = await client
			.For("Products")
			.Set(new { ProductName = "Test1", UnitPrice = 18m })
			.InsertEntryAsync().ConfigureAwait(false);

		await client
			.For("Products")
			.Key(product)
			.Set(new { UnitPrice = 456m })
			.UpdateEntryAsync().ConfigureAwait(false);

		product = await client
			.For("Products")
			.Filter("ProductName eq 'Test1'")
			.FindEntryAsync().ConfigureAwait(false);

		Assert.Equal(456m, product["UnitPrice"]);
	}

	[Fact]
	public async Task UpdateDate()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var today = DateTime.Parse("2018-05-20T20:30:40.6770000");
		var tomorrow = today.AddDays(1);

		var employee = await client
			.For("Employees")
			.Set(new { FirstName = "Test1", LastName = "Test1", HireDate = today })
			.InsertEntryAsync().ConfigureAwait(false);

		await client
			.For("Employees")
			.Key(employee["EmployeeID"])
			.Set(new { HireDate = tomorrow })
			.UpdateEntryAsync().ConfigureAwait(false);

		employee = await client
			.For("Employees")
			.Key(employee["EmployeeID"])
			.FindEntryAsync().ConfigureAwait(false);

		Assert.Equal(tomorrow, employee["HireDate"]);
	}

	[Fact]
	public async Task AddSingleAssociation()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var category = await client
			.For("Categories")
			.Set(new { CategoryName = "Test1" })
			.InsertEntryAsync().ConfigureAwait(false);
		var product = await client
			.For("Products")
			.Set(new { ProductName = "Test2", UnitPrice = 18m })
			.InsertEntryAsync().ConfigureAwait(false);

		await client
			.For("Products")
			.Key(product["ProductID"])
			.Set(new { Category = category })
			.UpdateEntryAsync().ConfigureAwait(false);

		product = await client
			.For("Products")
			.Filter("ProductID eq " + product["ProductID"])
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Equal(category["CategoryID"], product["CategoryID"]);
		category = await client
			.For("Categories")
			.Filter("CategoryID eq " + category["CategoryID"])
			.Expand("Products")
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Single((category["Products"] as IEnumerable<object>));
	}

	[Fact]
	public async Task UpdateSingleAssociation()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var category = await client
			.For("Categories")
			.Set(new { CategoryName = "Test1" })
			.InsertEntryAsync().ConfigureAwait(false);
		var product = await client
			.For("Products")
			.Set(new { ProductName = "Test2", UnitPrice = 18m, CategoryID = 1 })
			.InsertEntryAsync().ConfigureAwait(false);

		await client
			.For("Products")
			.Key(product["ProductID"])
			.Set(new { Category = category })
			.UpdateEntryAsync().ConfigureAwait(false);

		product = await client
			.For("Products")
			.Filter("ProductID eq " + product["ProductID"])
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Equal(category["CategoryID"], product["CategoryID"]);
		category = await client
			.For("Categories")
			.Filter("CategoryID eq " + category["CategoryID"])
			.Expand("Products")
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Single((category["Products"] as IEnumerable<object>));
	}

	[Fact]
	public async Task RemoveSingleAssociation()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var category = await client
			.For("Categories")
			.Set(new { CategoryName = "Test6" })
			.InsertEntryAsync().ConfigureAwait(false);
		var product = await client
			.For("Products")
			.Set(new { ProductName = "Test7", UnitPrice = 18m, Category = category })
			.InsertEntryAsync().ConfigureAwait(false);

		await client
			.For("Products")
			.Key(product["ProductID"])
			.Set(new { Category = (int?)null })
			.UpdateEntryAsync().ConfigureAwait(false);

		product = await client
			.For("Products")
			.Filter("ProductID eq " + product["ProductID"])
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Null(product["CategoryID"]);
	}

	[Fact]
	public async Task UpdateMultipleAssociations()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var category = await client
			.For("Categories")
			.Set(new { CategoryName = "Test3" })
			.InsertEntryAsync().ConfigureAwait(false);
		var product1 = await client
			.For("Products")
			.Set(new { ProductName = "Test4", UnitPrice = 18m, CategoryID = 1 })
			.InsertEntryAsync().ConfigureAwait(false);
		var product2 = await client
			.For("Products")
			.Set(new { ProductName = "Test5", UnitPrice = 18m, CategoryID = 1 })
			.InsertEntryAsync().ConfigureAwait(false);

		await client
			.For("Categories")
			.Key(category["CategoryID"])
			.Set(new { Products = new[] { product1, product2 } })
			.UpdateEntryAsync().ConfigureAwait(false);

		category = await client
			.For("Categories")
			.Filter("CategoryID eq " + category["CategoryID"])
			.Expand("Products")
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Equal(2, (category["Products"] as IEnumerable<object>).Count());
	}

	[Fact]
	public async Task UpdateDerived()
	{
		var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
		var ship = await client
			.For("Transport")
			.As("Ship")
			.Set(new { ShipName = "Test1" })
			.InsertEntryAsync().ConfigureAwait(false);

		ship = await client
			.For("Transport")
			.As("Ship")
			.Key(ship["TransportID"])
			.Set(new { ShipName = "Test2" })
			.UpdateEntryAsync().ConfigureAwait(false);

		Assert.Equal("Test2", ship["ShipName"]);
	}
}
