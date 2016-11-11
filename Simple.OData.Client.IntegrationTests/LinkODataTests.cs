using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

using Entry = System.Collections.Generic.Dictionary<string, object>;

namespace Simple.OData.Client.Tests
{
    public class LinkODataTestsV4Json : LinkODataTests
    {
        public LinkODataTestsV4Json() : base(ODataV4ReadWriteUri, ODataPayloadFormat.Json, 4) { }
    }

    public abstract class LinkODataTests : ODataTestBase
    {
        protected LinkODataTests(string serviceUri, ODataPayloadFormat payloadFormat, int version)
            : base(serviceUri, payloadFormat, version)
        {
        }

        [Fact]
        public async Task LinkEntry()
        {
            var category = await _client
                .For("Categories")
                .Set(CreateCategory(4001, "Test4"))
                .InsertEntryAsync();
            var product = await _client
                .For("Products")
                .Set(CreateProduct(4002, "Test5"))
                .InsertEntryAsync();

            await _client
                .For("Products")
                .Key(product)
                .LinkEntryAsync(ProductCategoryName, category);

            product = await _client
                .For("Products")
                .Filter("Name eq 'Test5'")
                .Expand(ProductCategoryName)
                .FindEntryAsync();
            Assert.NotNull(product[ProductCategoryName]);
            Assert.Equal(category["ID"], ProductCategoryFunc(product)["ID"]);
        }

        [Fact]
        public async Task UnlinkEntry()
        {
            var category = await _client
                .For("Categories")
                .Set(CreateCategory(4003, "Test4"))
                .InsertEntryAsync();
            var product = await _client
                .For("Products")
                .Set(CreateProduct(4002, "Test5", category))
                .InsertEntryAsync();

            await _client
                .For("Products")
                .Key(product)
                .UnlinkEntryAsync(ProductCategoryName, ProductCategoryName == "Categories" ? category : null);

            product = await _client
                .For("Products")
                .Filter("Name eq 'Test5'")
                .Expand(ProductCategoryName)
                .FindEntryAsync();
            if (ProductCategoryName == "Categories")
                Assert.DoesNotContain(ProductCategoryName, product.Keys);
            else
                Assert.Null(product[ProductCategoryName]);
        }
    }
}
