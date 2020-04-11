using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Simple.OData.Client;
using Simple.OData.Client.Tests;
using Xunit;
using WebApiOData.V4.Samples.Models;
#if NET452 && !MOCK_HTTP
using Microsoft.Owin.Testing;
using WebApiOData.V4.Samples.Startups;
#endif

namespace WebApiOData.V4.Samples.Tests
{
    public class FunctionV4Tests : IDisposable
    {
#if NET452 && !MOCK_HTTP
        private readonly TestServer _server;

        public FunctionV4Tests()
        {
            _server = TestServer.Create<FunctionStartup>();
        }

        public void Dispose()
        {
            _server.Dispose();
        }
#else
        public void Dispose()
        {
        }
#endif

        private ODataClientSettings CreateDefaultSettings()
        {
            return new ODataClientSettings()
            {
                BaseUri = new Uri("http://localhost/functions"),
                MetadataDocument = GetMetadataDocument(),
                PayloadFormat = ODataPayloadFormat.Json,
#if NET452 && !MOCK_HTTP
                OnCreateMessageHandler = () => _server.Handler,
#endif
                OnTrace = (x, y) => Console.WriteLine(string.Format(x, y)),
            };
        }

        private string GetMetadataDocument()
        {
#if MOCK_HTTP
            return MetadataResolver.GetMetadataDocument("Metadata.xml");
#else
            return null;
#endif

        }

        [Fact]
        public async Task Get_the_most_expensive_product_untyped()
        {
            var settings = CreateDefaultSettings().WithHttpMock();
            var client = new ODataClient(settings);
            var result = (double)await client
                .FindScalarAsync("Products/Default.MostExpensive()");

            Assert.InRange(result, 500, 1000);
        }

        [Fact]
        public async Task Get_the_most_expensive_product_untyped_batch()
        {
            var settings = CreateDefaultSettings().WithHttpMock();
            var client = new ODataClient(settings);
            object result = 0;
            var batch = new ODataBatch(settings);
            batch += async c => result = await c
                .FindScalarAsync("Products/Default.MostExpensive()");
            await batch.ExecuteAsync();

            Assert.InRange((double)result, 500, 1000);
        }

        [Fact]
        public async Task Get_the_most_expensive_product_typed()
        {
            var settings = CreateDefaultSettings().WithHttpMock();
            var client = new ODataClient(settings);
            var result = await client
                .For<Product>()
                .Function("MostExpensive")
                .ExecuteAsScalarAsync<double>();

            Assert.InRange(result, 500, 1000);
        }

        [Fact]
        public async Task Get_the_most_expensive_product_dynamic()
        {
            var settings = CreateDefaultSettings().WithHttpMock();
            var client = new ODataClient(settings);
            var x = ODataDynamic.Expression;

            var result = await client
                .For(x.Products)
                .Function("MostExpensive")
                .ExecuteAsScalarAsync<double>();

            Assert.InRange(result, 500, 1000);
        }

        [Fact]
        public async Task Get_the_most_expensives_products_untyped()
        {
            var settings = CreateDefaultSettings().WithHttpMock();
            var client = new ODataClient(settings);
            var result = await client
                .FindEntriesAsync("Products/Default.MostExpensives()");

            Assert.Equal(3, result.Count());
        }

        [Fact]
        public async Task Get_the_most_expensives_products_typed()
        {
            var settings = CreateDefaultSettings().WithHttpMock();
            var client = new ODataClient(settings);
            var result = await client
                .For<Product>()
                .Function("MostExpensives")
                .ExecuteAsEnumerableAsync();

            Assert.Equal(3, result.Count());
        }

        [Fact]
        public async Task Get_the_most_expensives_products_typed_array()
        {
            var settings = CreateDefaultSettings().WithHttpMock();
            var client = new ODataClient(settings);
            var result = await client
                .For<Product>()
                .Function("MostExpensives")
                .ExecuteAsArrayAsync<Product>();

            Assert.Equal(3, result.Count());
        }

        [Fact]
        public async Task Get_the_most_expensives_products_dynamic()
        {
            var settings = CreateDefaultSettings().WithHttpMock();
            var client = new ODataClient(settings);
            var x = ODataDynamic.Expression;

            IEnumerable<dynamic> result = await client
                .For(x.Products)
                .Function("MostExpensives")
                .ExecuteAsEnumerableAsync();

            Assert.Equal(3, result.Count());
        }

        [Fact]
        public async Task Get_the_top_10_expensive_products_untyped()
        {
            var settings = CreateDefaultSettings().WithHttpMock();
            var client = new ODataClient(settings);
            var result = await client
                .FindEntriesAsync("Products/Default.Top10()");

            Assert.Equal(10, result.Count());
        }

        [Fact]
        public async Task Get_the_top_10_expensive_products_typed()
        {
            var settings = CreateDefaultSettings().WithHttpMock();
            var client = new ODataClient(settings);
            var result = await client
                .For<Product>()
                .Function("Top10")
                .ExecuteAsEnumerableAsync();

            Assert.Equal(10, result.Count());
        }

        [Fact]
        public async Task Get_the_top_10_expensive_products_typed_array()
        {
            var settings = CreateDefaultSettings().WithHttpMock();
            var client = new ODataClient(settings);
            var result = await client
                .For<Product>()
                .Function("Top10")
                .ExecuteAsArrayAsync<Product>();

            Assert.Equal(10, result.Count());
        }

        [Fact]
        public async Task Get_the_top_10_expensive_products_dynamic()
        {
            var settings = CreateDefaultSettings().WithHttpMock();
            var client = new ODataClient(settings);
            var x = ODataDynamic.Expression;

            IEnumerable<dynamic> result = await client
                .For(x.Products)
                .Function("Top10")
                .ExecuteAsEnumerableAsync();

            Assert.Equal(10, result.Count());
        }

        [Fact]
        public async Task Get_the_rank_of_the_product_price_untyped()
        {
            var settings = CreateDefaultSettings().WithHttpMock();
            var client = new ODataClient(settings);
            var result = (int)await client
                .FindScalarAsync("Products(33)/Default.GetPriceRank()");

            Assert.InRange(result, 0, 100);
        }

        [Fact]
        public async Task Get_the_rank_of_the_product_price_typed()
        {
            var settings = CreateDefaultSettings().WithHttpMock();
            var client = new ODataClient(settings);
            var result = await client
                .For<Product>()
                .Key(33)
                .Function("GetPriceRank")
                .ExecuteAsScalarAsync<int>();

            Assert.InRange(result, 0, 100);
        }

        [Fact]
        public async Task Get_the_rank_of_the_product_price_dynamic()
        {
            var settings = CreateDefaultSettings().WithHttpMock();
            var client = new ODataClient(settings);
            var x = ODataDynamic.Expression;

            var result = await client
                .For(x.Products)
                .Key(33)
                .Function("GetPriceRank")
                .ExecuteAsScalarAsync<int>();

            Assert.InRange(result, 0, 100);
        }

        [Fact]
        public async Task Get_the_sales_tax_untyped()
        {
            var settings = CreateDefaultSettings().WithHttpMock();
            var client = new ODataClient(settings);
            var result = (double)await client
                .FindScalarAsync("Products(33)/Default.CalculateGeneralSalesTax(state='WA')");

            Assert.InRange(result, 1, 200);
        }

        [Fact]
        public async Task Get_the_sales_tax_typed()
        {
            var settings = CreateDefaultSettings().WithHttpMock();
            var client = new ODataClient(settings);
            var result = await client
                .For<Product>()
                .Key(33)
                .Function("CalculateGeneralSalesTax")
                .Set(new { state = "WA" })
                .ExecuteAsScalarAsync<double>();

            Assert.InRange(result, 1, 200);
        }

        [Fact]
        public async Task Get_the_sales_tax_dynamic()
        {
            var settings = CreateDefaultSettings().WithHttpMock();
            var client = new ODataClient(settings);
            var x = ODataDynamic.Expression;

            var result = await client
                .For(x.Products)
                .Key(33)
                .Function("CalculateGeneralSalesTax")
                .Set(x.state = "WA")
                .ExecuteAsScalarAsync<double>();

            Assert.InRange(result, 1, 200);
        }

        [Fact]
        public async Task Get_the_sales_tax_rate_untyped()
        {
            var settings = CreateDefaultSettings().WithHttpMock();
            var client = new ODataClient(settings);
            var result = await client
                .ExecuteFunctionAsScalarAsync<double>("GetSalesTaxRate", 
                new Dictionary<string, object>() { { "state", "CA" } });

            Assert.InRange(result, 5, 20);
        }

        [Fact]
        public async Task Get_the_sales_tax_rate_typed()
        {
            var settings = CreateDefaultSettings().WithHttpMock();
            var client = new ODataClient(settings);
            var result = await client
                .Unbound()
                .Function("GetSalesTaxRate")
                .Set(new { state = "CA" })
                .ExecuteAsScalarAsync<double>();

            Assert.InRange(result, 5, 20);
        }

        [Fact]
        public async Task Get_the_sales_tax_rate_dynamic()
        {
            var settings = CreateDefaultSettings().WithHttpMock();
            var client = new ODataClient(settings);
            var x = ODataDynamic.Expression;

            var result = await client
                .Unbound()
                .Function("GetSalesTaxRate")
                .Set(x.state = "CA")
                .ExecuteAsScalarAsync<double>();

            Assert.InRange(result, 5, 20);
        }
        
        [Fact]
        public async Task Get_product_placements_untyped()
        {
            var settings = CreateDefaultSettings().WithHttpMock();
            var client = new ODataClient(settings);
            var result = await client.FindEntriesAsync("Products(4)/Default.Placements()", null);
            Assert.Equal(3, result.Count());
            result = await client.FindEntriesAsync("Products(5)/Default.Placements()?$top=1&$orderby=ID desc&$skip=1", null);
            Assert.Single(result);
            Assert.Equal("Fatal Vengeance 2", result.First()["Title"]);
            result = await client.FindEntriesAsync("Products(5)/Default.Placements()?$top=1&$orderby=ID desc&$skip=1&$filter=ID gt 5", null);
            Assert.Empty(result);
        }

        [Fact]
        public async Task Get_product_placements_typed()
        {
            var settings = CreateDefaultSettings().WithHttpMock();
            var client = new ODataClient(settings);
            var result = await client
                .For<Product>()
                .Key(4)
                .Function<Movie>("Placements")
                .FindEntriesAsync();
            Assert.Equal(3, result.Count());
            result = await client
                .For<Product>()
                .Key(5)
                .Function<Movie>("Placements")
                .Top(1)
                .OrderByDescending(x => x.ID)
                .Skip(1)
                .FindEntriesAsync();
            Assert.Equal(1, result.Count());
            Assert.Equal("Fatal Vengeance 2", result.First().Title);
            result = await client
                .For<Product>()
                .Key(5)
                .Function<Movie>("Placements")
                .Top(1)
                .OrderByDescending(x => x.ID)
                .Skip(1)
                .Filter(x => x.ID > 5)
                .FindEntriesAsync();
            Assert.Equal(0, result.Count());
        }

        [Fact]
        public async Task Get_product_placements_dynamic()
        {
            var settings = CreateDefaultSettings().WithHttpMock();
            var client = new ODataClient(settings);
            var x = ODataDynamic.Expression;
            
            IEnumerable<dynamic> result = await client
                .For(x.Product)
                .Key(4)
                .Function("Placements")
                .FindEntriesAsync();
            Assert.Equal(3, result.Count());
            result = await client
                .For(x.Product)
                .Key(5)
                .Function("Placements")
                .Top(1)
                .OrderByDescending(x.ID)
                .Skip(1)
                .FindEntriesAsync();
            Assert.Single(result);
            Assert.Equal("Fatal Vengeance 2", result.First().Title);
            result = await client
                .For(x.Product)
                .Key(5)
                .Function("Placements")
                .Top(1)
                .OrderByDescending(x.ID)
                .Skip(1)
                .Filter(x.ID > 5)
                .FindEntriesAsync();
            Assert.Empty(result);
        }

        //[Fact]
        //public async Task Call_function_with_parameter_alias_untyped()
        //{
        //    var result = await _client
        //        .ExecuteFunctionAsScalarAsync("GetSalesTaxRate(state=@p1)?@p1='ND'");

        //    Assert.InRange(result, 5, 20);
        //}
    }
}
