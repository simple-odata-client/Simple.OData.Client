using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

using Entry = System.Collections.Generic.Dictionary<string, object>;

namespace Simple.OData.Client.Tests
{
    public class DeleteODataTestsV4Json : DeleteODataTests
    {
        public DeleteODataTestsV4Json() : base(ODataV4ReadWriteUri, ODataPayloadFormat.Json, 4) { }
    }

    public abstract class DeleteODataTests : ODataTestBase
    {
        protected DeleteODataTests(string serviceUri, ODataPayloadFormat payloadFormat, int version)
            : base(serviceUri, payloadFormat, version)
        {
        }

        [Fact]
        public async Task DeleteByKey()
        {
            var product = await _client
                .For("Products")
                .Set(CreateProduct(3001, "Test1"))
                .InsertEntryAsync();

            await _client
                .For("Products")
                .Key(product["ID"])
                .DeleteEntryAsync();

            product = await _client
                .For("Products")
                .Filter("Name eq 'Test1'")
                .FindEntryAsync();

            Assert.Null(product);
        }

        [Fact]
        public async Task DeleteByFilter()
        {
            var product = await _client
                .For("Products")
                .Set(CreateProduct(3002, "Test1"))
                .InsertEntryAsync();

            await _client
                .For("Products")
                .Filter("Name eq 'Test1'")
                .DeleteEntryAsync();

            product = await _client
                .For("Products")
                .Filter("Name eq 'Test1'")
                .FindEntryAsync();

            Assert.Null(product);
        }

        [Fact]
        public async Task DeleteByObjectAsKey()
        {
            var product = await _client
                .For("Products")
                .Set(CreateProduct(3003, "Test1"))
                .InsertEntryAsync();

            await _client
                .For("Products")
                .Key(product)
                .DeleteEntryAsync();

            product = await _client
                .For("Products")
                .Filter("Name eq 'Test1'")
                .FindEntryAsync();

            Assert.Null(product);
        }
    }
}
