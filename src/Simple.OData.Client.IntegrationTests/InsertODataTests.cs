using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

using Entry = System.Collections.Generic.Dictionary<string, object>;

namespace Simple.OData.Client.Tests
{
    public class InsertODataTestsV2Atom : InsertODataTests
    {
        public InsertODataTestsV2Atom() : base(ODataV2ReadWriteUri, ODataPayloadFormat.Atom, 2) { }
    }

    public class InsertODataTestsV2Json : InsertODataTests
    {
        public InsertODataTestsV2Json() : base(ODataV2ReadWriteUri, ODataPayloadFormat.Json, 2) { }
    }

    public class InsertODataTestsV3Atom : InsertODataTests
    {
        public InsertODataTestsV3Atom() : base(ODataV3ReadWriteUri, ODataPayloadFormat.Atom, 3) { }
    }

    public class InsertODataTestsV3Json : InsertODataTests
    {
        public InsertODataTestsV3Json() : base(ODataV3ReadWriteUri, ODataPayloadFormat.Json, 3) { }
    }

    public class InsertODataTestsV4Json : InsertODataTests
    {
        public InsertODataTestsV4Json() : base(ODataV4ReadWriteUri, ODataPayloadFormat.Json, 4) { }
    }

    public abstract class InsertODataTests : ODataTestBase
    {
        protected InsertODataTests(string serviceUri, ODataPayloadFormat payloadFormat, int version)
            : base(serviceUri, payloadFormat, version)
        {
        }

        [Fact]
        public async Task Insert()
        {
            var product = await _client
                .For("Products")
                .Set(CreateProduct(1001, "Test1"))
                .InsertEntryAsync();

            Assert.Equal("Test1", product["Name"]);
        }

        [Fact]
        public async Task InsertLookupByID()
        {
            var product = await _client
                .For("Products")
                .Set(CreateProduct(1002, "Test1"))
                .InsertEntryAsync();

            Assert.True((int)product["ID"] > 0);
            Assert.Equal("Test1", product["Name"]);
        }

        [Fact]
        public async Task InsertExpando()
        {
            dynamic expando = new ExpandoObject();
            expando.ID = 1003;
            expando.Name = "Test9";
            expando.Description = "Test9";
            expando.Price = 18m;
            expando.Rating = 1;
            expando.ReleaseDate = DateTime.Now;

            var product = await (Task<IDictionary<string, object>>)_client
                .For("Products")
                .Set(expando)
                .InsertEntryAsync();

            Assert.True((int)product["ID"] > 0);
        }

        [Fact]
        public async Task InsertProductWithCategory()
        {
            var category = await _client
                .For("Categories")
                .Set(CreateCategory(1005, "Test5"))
                .InsertEntryAsync();
            var product = await _client
                .For("Products")
                .Set(CreateProduct(1007, "Test6", category))
                .InsertEntryAsync();

            Assert.Equal("Test6", product["Name"]);
            product = await _client
                .For("Products")
                .Filter("Name eq 'Test6'")
                .Expand(ProductCategoryName)
                .FindEntryAsync();
            Assert.NotNull(product[ProductCategoryName]);
            Assert.Equal(category["ID"], ProductCategoryFunc(product)["ID"]);
        }

        [Fact]
        public async Task InsertReadingLocationHeader()
        {
            const int id = 5899;
            var withRequest = await _client
                .For("Products")
                .Set(CreateProduct(id, "Test"))
                .BuildRequestFor()
                .InsertEntryAsync();
            var response = await _client.GetResponseAsync(withRequest.GetRequest());
            Assert.NotNull(response);
            Assert.Equal($"{_serviceUri}Products({id})", response.Location);
        }

        [Fact]
        public async Task InsertWithoutResultsReadingLocationHeader()
        {
            const int id = 5900;
            var withRequest = await _client
                .For("Products")
                .Set(CreateProduct(id, "Test"))
                .BuildRequestFor()
                .InsertEntryAsync(false);
            var response = await _client.GetResponseAsync(withRequest.GetRequest());
            Assert.NotNull(response);
            Assert.Equal($"{_serviceUri}Products({id})", response.Location);
        }
    }
}
