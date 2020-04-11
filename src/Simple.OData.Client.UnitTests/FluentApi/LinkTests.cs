using System.Threading.Tasks;
using Xunit;

namespace Simple.OData.Client.Tests.FluentApi
{
    public class LinkTests : TestBase
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task LinkEntry(bool useAbsoluteReferenceUris)
        {
            var settings = CreateDefaultSettings().WithHttpMock();
            settings.UseAbsoluteReferenceUris = useAbsoluteReferenceUris;
            var client = new ODataClient(settings);
            var category = await client
                .For("Categories")
                .Set(new { CategoryName = "Test4" })
                .InsertEntryAsync();
            var product = await client
                .For("Products")
                .Set(new { ProductName = "Test5" })
                .InsertEntryAsync();

            await client
                .For("Products")
                .Key(product)
                .LinkEntryAsync("Category", category);

            product = await client
                .For("Products")
                .Filter("ProductName eq 'Test5'")
                .FindEntryAsync();
            Assert.NotNull(product["CategoryID"]);
            Assert.Equal(category["CategoryID"], product["CategoryID"]);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task UnlinkEntry(bool useAbsoluteReferenceUris)
        {
            var settings = CreateDefaultSettings().WithHttpMock();
            settings.UseAbsoluteReferenceUris = useAbsoluteReferenceUris;
            var client = new ODataClient(settings);
            var category = await client
                .For("Categories")
                .Set(new { CategoryName = "Test4" })
                .InsertEntryAsync();
            var product = await client
                .For("Products")
                .Set(new { ProductName = "Test5", CategoryID = category["CategoryID"] })
                .InsertEntryAsync();

            await client
                .For("Products")
                .Key(product)
                .UnlinkEntryAsync("Category");

            product = await client
                .For("Products")
                .Filter("ProductName eq 'Test5'")
                .FindEntryAsync();
            Assert.Null(product["CategoryID"]);
        }
    }
}
