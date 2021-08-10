using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Entry = System.Collections.Generic.Dictionary<string, object>;

namespace Simple.OData.Client.Tests.FluentApi
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
            var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
            var products = await client
                .For("Products")
                .Filter("ProductName eq 'Chai'")
                .FindEntriesAsync();
            Assert.Equal("Chai", products.Single()["ProductName"]);
        }

        [Fact]
        public async Task FilterStringExpression()
        {
            var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
            var products = await client
                .For("Products")
                .Filter("substringof('ai',ProductName)")
                .FindEntriesAsync();
            Assert.Equal("Chai", products.Single()["ProductName"]);
        }

        [Fact]
        public async Task Get()
        {
            var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
            var category = await client
                .For("Categories")
                .Key(1)
                .FindEntryAsync();
            Assert.Equal(1, category["CategoryID"]);
        }

        [Fact]
        public async Task GetNonExisting()
        {
            var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
            await AssertThrowsAsync<WebRequestException>(async () => await client
                .For("Categories")
                .Key(-1)
                .FindEntryAsync());
        }

        [Fact]
        public async Task SkipOne()
        {
            var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
            var products = await client
                .For("Products")
                .Skip(1)
                .FindEntriesAsync();
            Assert.Equal(ExpectedCountOfProducts - 1, products.Count());
        }

        [Fact]
        public async Task TopOne()
        {
            var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
            var products = await client
                .For("Products")
                .Top(1)
                .FindEntriesAsync();
            Assert.Single(products);
        }

        [Fact]
        public async Task SkipOneTopOne()
        {
            var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
            var products = await client
                .For("Products")
                .Skip(1)
                .Top(1)
                .FindEntriesAsync();
            Assert.Single(products);
        }

        [Fact]
        public async Task OrderBy()
        {
            var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
            var product = (await client
                .For("Products")
                .OrderBy("ProductName")
                .FindEntriesAsync()).First();
            Assert.Equal("Alice Mutton", product["ProductName"]);
        }

        [Fact]
        public async Task OrderByDescending()
        {
            var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
            var product = (await client
                .For("Products")
                .OrderByDescending("ProductName")
                .FindEntriesAsync()).First();
            Assert.Equal("Zaanse koeken", product["ProductName"]);
        }

        [Fact]
        public async Task OrderByExpanded()
        {
            var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
            var product = (await client
                .For("Products")
                .Expand("Category")
                .OrderBy("Category/CategoryName")
                .FindEntriesAsync()).Last();
            Assert.Equal("Seafood", (product["Category"] as IDictionary<string, object>)["CategoryName"]);
        }

        [Fact]
        public async Task SelectSingle()
        {
            var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
            var product = await client
                .For("Products")
                .Select("ProductName")
                .FindEntryAsync();
            Assert.Contains("ProductName", product.Keys);
            Assert.DoesNotContain("ProductID", product.Keys);
        }

        [Fact]
        public async Task SelectSingleHomogenize()
        {
            var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
            var product = await client
                .For("Products")
                .Select("Product_Name")
                .FindEntryAsync();
            Assert.Contains("ProductName", product.Keys);
            Assert.DoesNotContain("ProductID", product.Keys);
        }

        [Fact]
        public async Task SelectMultiple()
        {
            var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
            var product = await client
                .For("Products")
                .Select("ProductID", "ProductName")
                .FindEntryAsync();
            Assert.Contains("ProductName", product.Keys);
            Assert.Contains("ProductID", product.Keys);
        }

        [Fact]
        public async Task SelectMultipleSingleString()
        {
            var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
            var product = await client
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
            var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
            var product = (await client
                .For("Products")
                .OrderBy("ProductID")
                .Expand("Category")
                .FindEntriesAsync()).Last();
            Assert.Equal("Condiments", (product["Category"] as IDictionary<string, object>)["CategoryName"]);
        }

        [Fact]
        public async Task ExpandMany()
        {
            var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
            var category = await client
                .For("Categories")
                .Expand("Products")
                .Filter("CategoryName eq 'Beverages'")
                .FindEntryAsync();
            Assert.Equal(ExpectedCountOfBeveragesProducts, (category["Products"] as IEnumerable<object>).Count());
        }

        [Fact]
        public async Task ExpandSecondLevel()
        {
            var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
            var product = (await client
                .For("Products")
                .OrderBy("ProductID")
                .Expand("Category/Products")
                .FindEntriesAsync()).Last();
            Assert.Equal(ExpectedCountOfCondimentsProducts, ((product["Category"] as IDictionary<string, object>)["Products"] as IEnumerable<object>).Count());
        }

        [Fact]
        public async Task ExpandMultipleLevelsWithCollection()
        {
            var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
            var product = (await client
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
            var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
            var product = (await client
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
            var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
            var product = (await client
                .For("Products")
                .OrderBy("ProductID")
                .Expand("Category/Products")
                .Select(new[] { "ProductName", "Category/CategoryName" })
                .FindEntriesAsync()).Last();
            Assert.Equal(2, product.Count);
            Assert.Equal(1, (product["Category"] as IDictionary<string, object>).Count);
            Assert.Equal("Condiments", (product["Category"] as IDictionary<string, object>)["CategoryName"]);
        }

        [Fact]
        public async Task Count()
        {
            var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
            var count = await client
                .For("Products")
                .Count()
                .FindScalarAsync<int>();
            Assert.Equal(ExpectedCountOfProducts, count);
        }

        [Fact]
        public async Task FilterCount()
        {
            var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
            var count = await client
                .For("Products")
                .Filter("ProductName eq 'Chai'")
                .Count()
                .FindScalarAsync<int>();
            Assert.Equal(1, count);
        }

        [Fact]
        public async Task TotalCount()
        {
            var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
            var annotations = new ODataFeedAnnotations();
            var products = await client
                .For("Products")
                .FindEntriesAsync(annotations);
            Assert.Equal(ExpectedCountOfProducts, annotations.Count);
            Assert.Equal(ExpectedCountOfProducts, products.Count());
        }

        [Fact]
        public async Task CombineAll()
        {
            var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
            var product = (await client
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
            var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
            var product = (await client
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
            var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
            var category = await client
                .For("Products")
                .Key(new Entry() { { "ProductID", 2 } })
                .NavigateTo("Category")
                .FindEntryAsync();
            Assert.Equal("Beverages", category["CategoryName"]);
        }

        [Fact]
        public async Task NavigateToMultiple()
        {
            var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
            var products = await client
                .For("Categories")
                .Key(2)
                .NavigateTo("Products")
                .FindEntriesAsync();
            Assert.Equal(ExpectedCountOfCondimentsProducts, products.Count());
        }

        [Fact]
        public async Task NavigateToRecursive()
        {
            var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
            var employee = await client
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
            var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
            var employee = await client
                .For("Employees")
                .Key(14)
                .NavigateTo("Superior/Superior/Subordinates")
                .Key(3)
                .FindEntryAsync();
            Assert.Equal("Janet", employee["FirstName"]);
        }

        [Fact]
        public async Task NavigateToInvalid()
        {
            var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
            Func<IBoundClient<IDictionary<string,object>>> del = () => client
                .For("Employees")
                .Key(14)
                .Select("ProductName")
                .NavigateTo("Superior");
            Assert.Throws<InvalidOperationException>(del);
        }

        [Fact]
        public async Task BaseClassEntries()
        {
            var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
            var transport = await client
                .For("Transport")
                .FindEntriesAsync();
            Assert.Equal(2, transport.Count());
            Assert.DoesNotContain(transport, x => x.ContainsKey(FluentCommand.AnnotationsLiteral));
        }

        [Fact]
        public async Task BaseClassEntriesWithAnnotations()
        {
            var client = new ODataClient(CreateDefaultSettings().WithAnnotations().WithHttpMock());
            var transport = await client
                .For("Transport")
                .FindEntriesAsync();
            Assert.Equal(2, transport.Count());
            Assert.True(transport.All(x => x.ContainsKey(FluentCommand.AnnotationsLiteral)));
        }

        [Fact]
        public async Task AllDerivedClassEntries()
        {
            var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
            var transport = await client
                .For("Transport")
                .As("Ships")
                .FindEntriesAsync();
            Assert.Equal("Titanic", transport.Single()["ShipName"]);
        }

        [Fact]
        public async Task AllDerivedClassEntriesWithAnnotations()
        {
            var client = new ODataClient(CreateDefaultSettings().WithAnnotations().WithHttpMock());
            var transport = await client
                .For("Transport")
                .As("Ships")
                .FindEntriesAsync();

            var ship = transport.Single();
            Assert.Equal("Titanic", ship["ShipName"]);
            Assert.Equal("NorthwindModel.Ship", (ship[FluentCommand.AnnotationsLiteral] as ODataEntryAnnotations).TypeName);
        }

        [Fact]
        public async Task DerivedClassEntry()
        {
            var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
            var transport = await client
                .For("Transport")
                .As("Ships")
                .Filter("ShipName eq 'Titanic'")
                .FindEntryAsync();
            Assert.Equal("Titanic", transport["ShipName"]);
        }

        [Fact]
        public async Task BaseClassEntryByKey()
        {
            var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
            var transport = await client
                .For("Transport")
                .Key(1)
                .FindEntryAsync();
            Assert.Equal("Titanic", transport["ShipName"]);
        }

        [Fact]
        public async Task DerivedClassEntryByKey()
        {
            var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
            var transport = await client
                .For("Transport")
                .As("Ships")
                .Key(1)
                .FindEntryAsync();
            Assert.Equal("Titanic", transport["ShipName"]);
        }

        [Fact]
        public async Task DerivedClassEntryBaseAndDerivedFields()
        {
            var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
            var transport = await client
                .For("Transport")
                .As("Ships")
                .Filter("TransportID eq 1 and ShipName eq 'Titanic'")
                .FindEntryAsync();
            Assert.Equal("Titanic", transport["ShipName"]);
        }

        [Fact]
        public async Task IsOfDerivedClassEntry()
        {
            var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
            var transport = await client
                .For("Transport")
                .Filter("isof('NorthwindModel.Ship')")
                .FindEntryAsync();
            Assert.Equal("Titanic", transport["ShipName"]);
        }

        [Fact]
        public async Task IsOfAssociation()
        {
            var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
            var employee = await client
                .For("Employees")
                .Filter("isof(Superior, 'NorthwindModel.Employee')")
                .FindEntryAsync();
            Assert.NotNull(employee);
        }

        [Fact]
        public async Task CastToPrimitiveType()
        {
            var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
            var product = await client
                .For("Products")
                .Filter("ProductID eq cast(1L, 'Edm.Int32')")
                .FindEntryAsync();
            Assert.NotNull(product);
        }

        [Fact]
        public async Task CastInstanceToEntityType()
        {
            var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
            var employee = await client
                .For("Employees")
                .Filter("cast('NorthwindModel.Employee') ne null")
                .FindEntryAsync();
            Assert.NotNull(employee);
        }

        [Fact]
        public async Task CastPropertyToEntityType()
        {
            var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
            var employee = await client
                .For("Employees")
                .Filter("cast(Superior, 'NorthwindModel.Employee') ne null")
                .FindEntryAsync();
            Assert.NotNull(employee);
        }

        [Fact]
        public async Task FilterAny()
        {
            var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
            var products = await client
                .For("Orders")
                .Filter("Order_Details/any(d:d/Quantity gt 50)")
                .FindEntriesAsync();
            Assert.Equal(ExpectedCountOfOrdersHavingAnyDetail, products.Count());
        }

        [Fact]
        public async Task FilterAll()
        {
            var client = new ODataClient(CreateDefaultSettings().WithHttpMock());
            var products = await client
                .For("Orders")
                .Filter("Order_Details/all(d:d/Quantity gt 50)")
                .FindEntriesAsync();
            Assert.Equal(ExpectedCountOfOrdersHavingAllDetails, products.Count());
        }
    }
}
