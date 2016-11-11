using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

using Entry = System.Collections.Generic.Dictionary<string, object>;

namespace Simple.OData.Client.Tests
{
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
    }
}
