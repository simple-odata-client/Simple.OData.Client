using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

using Entry = System.Collections.Generic.Dictionary<string, object>;

namespace Simple.OData.Client.Tests
{
    public class FindTests : TestBase
    {
        public FindTests()
            : base(true)
        {
        }

        [Fact]
        public async Task Filter()
        {
            var products = await _client
                .For("Products")
                .Filter("ProductName eq 'Chai'")
                .FindEntriesAsync();
            Assert.Equal("Chai", products.Single()["ProductName"]);
        }

        [Fact]
        public async Task FilterStringExpression()
        {
            var products = await _client
                .For("Products")
                .Filter("substringof('ai',ProductName)")
                .FindEntriesAsync();
            Assert.Equal("Chai", products.Single()["ProductName"]);
        }

        [Fact]
        public async Task Get()
        {
            var category = await _client
                .For("Categories")
                .Key(1)
                .FindEntryAsync();
            Assert.Equal(1, category["CategoryID"]);
        }

        [Fact]
        public async Task GetNonExisting()
        {
            await AssertThrowsAsync<WebRequestException>(async () => await _client
                .For("Categories")
                .Key(-1)
                .FindEntryAsync());
        }

        [Fact]
        public async Task SkipOne()
        {
            var products = await _client
                .For("Products")
                .Skip(1)
                .FindEntriesAsync();
            Assert.Equal(ExpectedCountOfProducts-1, products.Count());
        }

        [Fact]
        public async Task TopOne()
        {
            var products = await _client
                .For("Products")
                .Top(1)
                .FindEntriesAsync();
            Assert.Equal(1, products.Count());
        }

        [Fact]
        public async Task SkipOneTopOne()
        {
            var products = await _client
                .For("Products")
                .Skip(1)
                .Top(1)
                .FindEntriesAsync();
            Assert.Equal(1, products.Count());
        }

        [Fact]
        public async Task OrderBy()
        {
            var product = (await _client
                .For("Products")
                .OrderBy("ProductName")
                .FindEntriesAsync()).First();
            Assert.Equal("Alice Mutton", product["ProductName"]);
        }

        [Fact]
        public async Task OrderByDescending()
        {
            var product = (await _client
                .For("Products")
                .OrderByDescending("ProductName")
                .FindEntriesAsync()).First();
            Assert.Equal("Zaanse koeken", product["ProductName"]);
        }

        [Fact]
        public async Task OrderByExpanded()
        {
            var product = (await _client
                .For("Products")
                .Expand("Category")
                .OrderBy("Category/CategoryName")
                .FindEntriesAsync()).Last();
            Assert.Equal("Seafood", (product["Category"] as IDictionary<string, object>)["CategoryName"]);
        }

        [Fact]
        public async Task SelectSingle()
        {
            var product = await _client
                .For("Products")
                .Select("ProductName")
                .FindEntryAsync();
            Assert.Contains("ProductName", product.Keys);
            Assert.DoesNotContain("ProductID", product.Keys);
        }

        [Fact]
        public async Task SelectSingleHomogenize()
        {
            var product = await _client
                .For("Products")
                .Select("Product_Name")
                .FindEntryAsync();
            Assert.Contains("ProductName", product.Keys);
            Assert.DoesNotContain("ProductID", product.Keys);
        }

        [Fact]
        public async Task SelectMultiple()
        {
            var product = await _client
                .For("Products")
                .Select("ProductID", "ProductName")
                .FindEntryAsync();
            Assert.Contains("ProductName", product.Keys);
            Assert.Contains("ProductID", product.Keys);
        }

        [Fact]
        public async Task SelectMultipleSingleString()
        {
            var product = await _client
                .For("Products")
                .Select("ProductID, ProductName")
                .FindEntryAsync();
            Assert.Equal(2, product.Count);
            Assert.Contains("ProductName", product.Keys);
            Assert.Contains("ProductID", product.Keys);
        }

        [Fact]
        public async Task ExpandOne()
        {
            var product = (await _client
                .For("Products")
                .OrderBy("ProductID")
                .Expand("Category")
                .FindEntriesAsync()).Last();
            Assert.Equal("Condiments", (product["Category"] as IDictionary<string, object>)["CategoryName"]);
        }

        [Fact]
        public async Task ExpandMany()
        {
            var category = await _client
                .For("Categories")
                .Expand("Products")
                .Filter("CategoryName eq 'Beverages'")
                .FindEntryAsync();
            Assert.Equal(ExpectedCountOfBeveragesProducts, (category["Products"] as IEnumerable<object>).Count());
        }

        [Fact]
        public async Task ExpandSecondLevel()
        {
            var product = (await _client
                .For("Products")
                .OrderBy("ProductID")
                .Expand("Category/Products")
                .FindEntriesAsync()).Last();
            Assert.Equal(ExpectedCountOfCondimentsProducts, ((product["Category"] as IDictionary<string, object>)["Products"] as IEnumerable<object>).Count());
        }

        [Fact]
        public async Task ExpandMultipleLevelsWithCollection()
        {
            var product = (await _client
                .For("Products")
                .OrderBy("ProductID")
                .Expand("Category/Products/Category")
                .FindEntriesAsync()).Last();
            Assert.Equal("Condiments", ((((product["Category"] as IDictionary<string, object>)["Products"] as IEnumerable<object>)
                                        .First() as IDictionary<string, object>)["Category"] as IDictionary<string, object>)["CategoryName"]);
        }

        [Fact]
        public async Task ExpandMultipleLevelsWithCollectionAndSelect()
        {
            var product = (await _client
                .For("Products")
                .OrderBy("ProductID")
                .Expand("Category/Products/Category")
                .Select(new[] { "Category/Products/Category/CategoryName" })
                .FindEntriesAsync()).Last();
            Assert.Equal(1, product.Count);
            Assert.Equal(1, (product["Category"] as IDictionary<string, object>).Count);
            Assert.Equal(1, (((product["Category"] as IDictionary<string, object>)["Products"] as IEnumerable<object>)
                                        .First() as IDictionary<string, object>).Count);
            Assert.Equal(1, ((((product["Category"] as IDictionary<string, object>)["Products"] as IEnumerable<object>)
                                        .First() as IDictionary<string, object>)["Category"] as IDictionary<string, object>).Count);
            Assert.Equal("Condiments", ((((product["Category"] as IDictionary<string, object>)["Products"] as IEnumerable<object>)
                                        .First() as IDictionary<string, object>)["Category"] as IDictionary<string, object>)["CategoryName"]);
        }

        [Fact]
        public async Task ExpandWithSelect()
        {
            var product = (await _client
                .For("Products")
                .OrderBy("ProductID")
                .Expand("Category/Products")
                .Select(new[] {"ProductName", "Category/CategoryName" })
                .FindEntriesAsync()).Last();
            Assert.Equal(2, product.Count);
            Assert.Equal(1, (product["Category"] as IDictionary<string, object>).Count);
            Assert.Equal("Condiments", (product["Category"] as IDictionary<string, object>)["CategoryName"]);
        }

        [Fact]
        public async Task Count()
        {
            var count = await _client
                .For("Products")
                .Count()
                .FindScalarAsync<int>();
            Assert.Equal(ExpectedCountOfProducts, count);
        }

        [Fact]
        public async Task FilterCount()
        {
            var count = await _client
                .For("Products")
                .Filter("ProductName eq 'Chai'")
                .Count()
                .FindScalarAsync<int>();
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task TotalCount()
        {
            var annotations = new ODataFeedAnnotations();
            var products = await _client
                .For("Products")
                .FindEntriesAsync(annotations);
            Assert.Equal(ExpectedCountOfProducts, annotations.Count);
            Assert.Equal(ExpectedCountOfProducts, products.Count());
        }

        [Fact]
        public async Task CombineAll()
        {
            var product = (await _client
                .For("Products")
                .OrderBy("ProductName")
                .Skip(2)
                .Top(1)
                .Expand("Category")
                .Select("Category")
                .FindEntriesAsync()).Single();
            Assert.Equal("Seafood", (product["Category"] as IDictionary<string, object>)["CategoryName"]);
        }

        [Fact]
        public async Task CombineAllReverse()
        {
            var product = (await _client
                .For("Products")
                .Select("Category")
                .Expand("Category")
                .Top(1)
                .Skip(2)
                .OrderBy("ProductName")
                .FindEntriesAsync()).Single();
            Assert.Equal("Seafood", (product["Category"] as IDictionary<string, object>)["CategoryName"]);
        }

        [Fact]
        public async Task NavigateToSingle()
        {
            var category = await _client
                .For("Products")
                .Key(new Entry() { { "ProductID", 2 } })
                .NavigateTo("Category")
                .FindEntryAsync();
            Assert.Equal("Beverages", category["CategoryName"]);
        }

        [Fact]
        public async Task NavigateToMultiple()
        {
            var products = await _client
                .For("Categories")
                .Key(2)
                .NavigateTo("Products")
                .FindEntriesAsync();
            Assert.Equal(ExpectedCountOfCondimentsProducts, products.Count());
        }

        [Fact]
        public async Task NavigateToRecursive()
        {
            var employee = await _client
                .For("Employees")
                .Key(14)
                .NavigateTo("Superior")
                .NavigateTo("Superior")
                .NavigateTo("Subordinates")
                .Key(3)
                .FindEntryAsync();
            Assert.Equal("Janet", employee["FirstName"]);
        }

        [Fact]
        public async Task NavigateToRecursiveSingleClause()
        {
            var employee = await _client
                .For("Employees")
                .Key(14)
                .NavigateTo("Superior/Superior/Subordinates")
                .Key(3)
                .FindEntryAsync();
            Assert.Equal("Janet", employee["FirstName"]);
        }

        [Fact]
        public async Task BaseClassEntries()
        {
            var transport = await _client
                .For("Transport")
                .FindEntriesAsync();
            Assert.Equal(2, transport.Count());
            Assert.False(transport.Any(x => x.ContainsKey(FluentCommand.AnnotationsLiteral)));
        }

        [Fact]
        public async Task BaseClassEntriesWithAnnotations()
        {
            var clientSettings = new ODataClientSettings
                                     {
                                         BaseUri = _serviceUri,
                                         IncludeAnnotationsInResults = true,
                                     };
            var client = new ODataClient(clientSettings);
            var transport = await client
                .For("Transport")
                .FindEntriesAsync();
            Assert.Equal(2, transport.Count());
            Assert.True(transport.All(x => x.ContainsKey(FluentCommand.AnnotationsLiteral)));
        }

        [Fact]
        public async Task AllDerivedClassEntries()
        {
            var transport = await _client
                .For("Transport")
                .As("Ships")
                .FindEntriesAsync();
            Assert.Equal("Titanic", transport.Single()["ShipName"]);
        }

        [Fact]
        public async Task AllDerivedClassEntriesWithAnnotations()
        {
            var clientSettings = new ODataClientSettings
            {
                BaseUri = _serviceUri,
                IncludeAnnotationsInResults = true,
            };
            var client = new ODataClient(clientSettings);
            var transport = await client
                .For("Transport")
                .As("Ships")
                .FindEntriesAsync();
            Assert.Equal("Titanic", transport.Single()["ShipName"]);
            Assert.Equal("NorthwindModel.Ship", (transport.Single()[FluentCommand.AnnotationsLiteral] as ODataEntryAnnotations).TypeName);
        }

        [Fact]
        public async Task DerivedClassEntry()
        {
            var transport = await _client
                .For("Transport")
                .As("Ships")
                .Filter("ShipName eq 'Titanic'")
                .FindEntryAsync();
            Assert.Equal("Titanic", transport["ShipName"]);
        }

        [Fact]
        public async Task BaseClassEntryByKey()
        {
            var transport = await _client
                .For("Transport")
                .Key(1)
                .FindEntryAsync();
            Assert.Equal("Titanic", transport["ShipName"]);
        }

        [Fact]
        public async Task DerivedClassEntryByKey()
        {
            var transport = await _client
                .For("Transport")
                .As("Ships")
                .Key(1)
                .FindEntryAsync();
            Assert.Equal("Titanic", transport["ShipName"]);
        }

        [Fact]
        public async Task DerivedClassEntryBaseAndDerivedFields()
        {
            var transport = await _client
                .For("Transport")
                .As("Ships")
                .Filter("TransportID eq 1 and ShipName eq 'Titanic'")
                .FindEntryAsync();
            Assert.Equal("Titanic", transport["ShipName"]);
        }

        [Fact]
        public async Task IsOfDerivedClassEntry()
        {
            var transport = await _client
                .For("Transport")
                .Filter("isof('NorthwindModel.Ship')")
                .FindEntryAsync();
            Assert.Equal("Titanic", transport["ShipName"]);
        }

        [Fact]
        public async Task IsOfAssociation()
        {
            var employee = await _client
                .For("Employees")
                .Filter("isof(Superior, 'NorthwindModel.Employee')")
                .FindEntryAsync();
            Assert.NotNull(employee);
        }

        [Fact]
        public async Task CastToPrimitiveType()
        {
            var product = await _client
                .For("Products")
                .Filter("ProductID eq cast(1L, 'Edm.Int32')")
                .FindEntryAsync();
            Assert.NotNull(product);
        }

        [Fact]
        public async Task CastInstanceToEntityType()
        {
            var employee = await _client
                .For("Employees")
                .Filter("cast('NorthwindModel.Employee') ne null")
                .FindEntryAsync();
            Assert.NotNull(employee);
        }

        [Fact]
        public async Task CastPropertyToEntityType()
        {
            var employee = await _client
                .For("Employees")
                .Filter("cast(Superior, 'NorthwindModel.Employee') ne null")
                .FindEntryAsync();
            Assert.NotNull(employee);
        }

        [Fact]
        public async Task FilterAny()
        {
            var products = await _client
                .For("Orders")
                .Filter("Order_Details/any(d:d/Quantity gt 50)")
                .FindEntriesAsync();
            Assert.Equal(ExpectedCountOfOrdersHavingAnyDetail, products.Count());
        }

        [Fact]
        public async Task FilterAll()
        {
            var products = await _client
                .For("Orders")
                .Filter("Order_Details/all(d:d/Quantity gt 50)")
                .FindEntriesAsync();
            Assert.Equal(ExpectedCountOfOrdersHavingAllDetails, products.Count());
        }
    }
}
