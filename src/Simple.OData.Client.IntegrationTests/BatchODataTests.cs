using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Simple.OData.Client.Tests
{
    using Entry = System.Collections.Generic.Dictionary<string, object>;

    public class BatchODataTestsV2Atom : BatchODataTests
    {
        public BatchODataTestsV2Atom() : base(ODataV2ReadWriteUri, ODataPayloadFormat.Atom, 2) { }
    }

    public class BatchODataTestsV2Json : BatchODataTests
    {
        public BatchODataTestsV2Json() : base(ODataV2ReadWriteUri, ODataPayloadFormat.Json, 2) { }
    }

    public class BatchODataTestsV3Atom : BatchODataTests
    {
        public BatchODataTestsV3Atom() : base(ODataV3ReadWriteUri, ODataPayloadFormat.Atom, 3) { }
    }

    public class BatchODataTestsV3Json : BatchODataTests
    {
        public BatchODataTestsV3Json() : base(ODataV3ReadWriteUri, ODataPayloadFormat.Json, 3) { }
    }

    public class BatchODataTestsV4Json : BatchODataTests
    {
        public BatchODataTestsV4Json() : base(ODataV4ReadWriteUri, ODataPayloadFormat.Json, 4) { }
    }

    public abstract class BatchODataTests : ODataTestBase
    {
        protected BatchODataTests(string serviceUri, ODataPayloadFormat payloadFormat, int version)
            : base(serviceUri, payloadFormat, version)
        {
        }

        [Fact]
        public async Task Success()
        {
            var batch = new ODataBatch(_serviceUri);
            batch += x => x.InsertEntryAsync("Products", CreateProduct(5001, "Test1"), false);
            batch += x => x.InsertEntryAsync("Products", CreateProduct(5002, "Test2"), false);
            await batch.ExecuteAsync();

            var product = await _client.FindEntryAsync("Products?$filter=Name eq 'Test1'");
            Assert.NotNull(product);
            product = await _client.FindEntryAsync("Products?$filter=Name eq 'Test2'");
            Assert.NotNull(product);
        }

        // OData.org sample service doesn't fail on this request
        //[Fact]
        //public async Task PartialFailures()
        //{
        //    var batch = new ODataBatch(_serviceUri);
        //    batch += x => x.InsertEntryAsync("Products", CreateProduct(5003, "Test3"), false);
        //    batch += x => x.InsertEntryAsync("Products", CreateProduct(0, "Test4"), false);
        //    await AssertThrowsAsync<AggregateException>(async () => await batch.ExecuteAsync());
        //}

        // OData.org sample service doesn't fail on this request
        //[Fact]
        //public async Task AllFailures()
        //{
        //    var batch = new ODataBatch(_serviceUri);
        //    batch += x => x.InsertEntryAsync("Products", CreateProduct(0, "Test5"), false);
        //    batch += x => x.InsertEntryAsync("Products", CreateProduct(0, "Test6"), false);
        //    await AssertThrowsAsync<AggregateException>(async () => await batch.ExecuteAsync());
        //}

        [Fact]
        public async Task InsertUpdateDeleteSingleBatch()
        {
            var key = new Entry() { { "Name", "Test11" } };

            var batch = new ODataBatch(_serviceUri);
            batch += x => x.InsertEntryAsync("Products", CreateProduct(5011, "Test11"), false);
            batch += x => x.UpdateEntriesAsync("Products", "Products?$filter=Name eq 'Test11'", new Entry() { { "Price", 22m } });
            batch += x => x.DeleteEntriesAsync("Products", "Products?$filter=Name eq 'Test11'");
            await batch.ExecuteAsync();

            var product = await _client.FindEntryAsync("Products?$filter=Name eq 'Test11'");
            Assert.Equal(18d, Convert.ToDouble(product["Price"]));
        }

        // Not properly handled by OData.org sample service
        //[Fact]
        //public async Task InsertUpdateDeleteSeparateBatches()
        //{
        //    var batch = new ODataBatch(_serviceUri);
        //    batch += x => x.InsertEntryAsync("Products", CreateProduct(5012, "Test12"), false);
        //    await batch.ExecuteAsync();

        //    var product = await _client.FindEntryAsync("Products?$filter=Name eq 'Test12'");
        //    Assert.Equal(18d, Convert.ToDouble(product["Price"]));
        //    var key = new Entry() { { "ID", product["ID"] } };

        //    batch = new ODataBatch(_serviceUri);
        //    batch += x => x.UpdateEntryAsync("Products", key, new Entry() { { "Price", 22m } });
        //    await batch.ExecuteAsync();
        //    product = await _client.FindEntryAsync("Products?$filter=Name eq 'Test12'");
        //    Assert.Equal(22d, Convert.ToDouble(product["Price"]));

        //    batch = new ODataBatch(_serviceUri);
        //    batch += x => x.DeleteEntryAsync("Products", key);
        //    await batch.ExecuteAsync();
        //    product = await _client.FindEntryAsync("Products?$filter=Name eq 'Test12'");
        //    Assert.Null(product);
        //}

        [Fact]
        public async Task InsertSingleEntityWithSingleAssociationSingleBatch()
        {
            IDictionary<string, object> category = null;
            var batch = new ODataBatch(_serviceUri);
            batch += async x => category = await x.InsertEntryAsync("Categories", CreateCategory(5013, "Test13"));
            batch += x => x.InsertEntryAsync("Products", CreateProduct(5014, "Test14", category), false);
            await batch.ExecuteAsync();

            var product = await _client
                .For("Products")
                .Expand(ProductCategoryName)
                .Filter("Name eq 'Test14'")
                .FindEntryAsync();
            Assert.Equal(5013, ProductCategoryFunc(product)["ID"]);
        }

        [Fact]
        public async Task InsertSingleEntityWithMultipleAssociationsSingleBatch()
        {
            IDictionary<string, object> product1 = null;
            IDictionary<string, object> product2 = null;

            var batch = new ODataBatch(_serviceUri);
            batch += async x => product1 = await x.InsertEntryAsync("Products", CreateProduct(5015, "Test15"));
            batch += async x => product2 = await x.InsertEntryAsync("Products", CreateProduct(5016, "Test16"));
            batch += async x => await x.InsertEntryAsync("Categories", CreateCategory(5017, "Test17", new[] { product1, product2 }), false);
            await batch.ExecuteAsync();

            var category = await _client
                .For("Categories")
                .Key(5017)
                .Expand("Products")
                .FindEntryAsync();
            Assert.Equal(2, (category["Products"] as IEnumerable<object>).Count());
        }

        [Fact]
        public async Task ExecuteXCsrfFetchPriorToBatchExecution()
        {
            IDictionary<string, object> product1 = null;
            IDictionary<string, object> product2 = null;

            // None of the existing sample service endpoints actually provide an xcsrf token, 
            // but in scenarios where a developer may need to use a csrf token, this is an 
            // example of how to acquire one and send it in on subsequent batch requests.
            var token = "";
            var settings = new ODataClientSettings(_serviceUri);
            settings.BeforeRequest += (request) =>
            {
                request.Headers.Add("x-csrf-token", "fetch");
            };
            settings.AfterResponse += (response) =>
            {
                // Assuming that because the service end points don't return tokens at this time
                // that we won't be setting the value of the token here.
                token = response.Headers.TryGetValues("x-csrf-token", out var values) ? values.First() : "myToken";
            };

            // Execute an arbitrary request to retrieve the csrf token
            var client = new ODataClient(settings);
            await client.GetMetadataDocumentAsync();

            // Since the token was never updated it should still be an empty string.
            Assert.NotNull(token);

            // Change the settings for the client so we can re-use the session and create a new request with different headers 
            var newHeaders = new Dictionary<string, IEnumerable<string>>
            {
                {"x-csrf-token", new List<string> {token}}
            };
            client.UpdateRequestHeaders(newHeaders);

            var batch = new ODataBatch(client, reuseSession: true);
            batch += async x => product1 = await x.InsertEntryAsync("Products", CreateProduct(5015, "Test15"));
            batch += async x => product2 = await x.InsertEntryAsync("Products", CreateProduct(5016, "Test16"));
            batch += async x => await x.InsertEntryAsync("Categories", CreateCategory(5017, "Test17", new[] { product1, product2 }), false);
            await batch.ExecuteAsync();

            var category = await _client
                .For("Categories")
                .Key(5017)
                .Expand("Products")
                .FindEntryAsync();
            Assert.Equal(2, (category["Products"] as IEnumerable<object>).Count());
        }

        [Fact]
        public async Task IgnoreResourceNotFoundExceptionInBatchRequest()
        {
            var settings = new ODataClientSettings(_serviceUri)
            {
                IgnoreResourceNotFoundException = true
            };

            var batch = new ODataBatch(settings);

            IDictionary<string, object> bread = new Dictionary<string, object>();

            IDictionary<string, object> milk = new Dictionary<string, object>();

            batch += async x => bread = await x.For("Products").Key(0).FindEntryAsync();
            batch += async x => await x.For("Products").Key(-1).FindEntryAsync();
            batch += async x => milk = await x.For("Products").Key(1).FindEntryAsync();

            await batch.ExecuteAsync();

            Assert.True(bread.ContainsKey("ID"));
            Assert.True(milk.ContainsKey("ID"));
        }

        [Fact]
        public async Task InsertWithoutResultsReadingLocationHeader()
        {
            const int id = 5897;
            ODataResponse response = null;
            var batch = new ODataBatch(_serviceUri);
            batch += async x =>
            {
                var request = x.For("Products")
                    .Set(CreateProduct(id, "Test"))
                    .BuildRequestFor()
                    .InsertEntryAsync(false)
                    .Result
                    .GetRequest();
                response = await x.GetResponseAsync(request);
            };
            await batch.ExecuteAsync();
            Assert.NotNull(response);
            Assert.Equal($"{_serviceUri}Products({id})", response.Location);
        }

        [Fact]
        public async Task InsertReadingLocationHeader()
        {
            const int id = 5898;
            ODataResponse response = null;
            var batch = new ODataBatch(_serviceUri);
            batch += async x =>
            {
                var request = x.For("Products")
                    .Set(CreateProduct(id, "Test"))
                    .BuildRequestFor()
                    .InsertEntryAsync(true)
                    .Result
                    .GetRequest();
                response = await x.GetResponseAsync(request);
            };
            await batch.ExecuteAsync();
            Assert.NotNull(response);
            Assert.Equal($"{_serviceUri}Products({id})", response.Location);
        }
    }
}
