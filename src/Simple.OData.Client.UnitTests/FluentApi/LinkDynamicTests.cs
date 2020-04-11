using System.Threading.Tasks;
using Xunit;

namespace Simple.OData.Client.Tests.FluentApi
{
    public class LinkDynamicTests : TestBase
    {
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task LinkEntry(bool useAbsoluteReferenceUris)
        {
                var settings = CreateDefaultSettings().WithHttpMock();
                settings.UseAbsoluteReferenceUris = useAbsoluteReferenceUris;
                var client = new ODataClient(settings);
                var x = ODataDynamic.Expression;
            var category = await client
                .For(x.Categories)
                .Set(x.CategoryName = "Test4")
                .InsertEntryAsync();
            var product = await client
                .For(x.Products)
                .Set(x.ProductName = "Test5")
                .InsertEntryAsync();

            await client
                .For(x.Products)
                .Key(product)
                .LinkEntryAsync(x.Category, category);

            product = await client
                .For(x.Products)
                .Filter(x.ProductName == "Test5")
                .FindEntryAsync();
            Assert.NotNull(product.CategoryID);
            Assert.Equal(category.CategoryID, product.CategoryID);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task UnlinkEntry(bool useAbsoluteReferenceUris)
        {
            var settings = CreateDefaultSettings().WithHttpMock();
            settings.UseAbsoluteReferenceUris = useAbsoluteReferenceUris;
            var client = new ODataClient(settings);
            var x = ODataDynamic.Expression;
            var category = await client
                .For(x.Categories)
                .Set(x.CategoryName = "Test4")
                .InsertEntryAsync();
            var product = await client
                .For(x.Products)
                .Set(x.ProductName = "Test5", x.CategoryID = category.CategoryID)
                .InsertEntryAsync();

            await client
                .For(x.Products)
                .Key(product)
                .UnlinkEntryAsync(x.Category);

            product = await client
                .For(x.Products)
                .Filter(x.ProductName == "Test5")
                .FindEntryAsync();
            Assert.Null(product.CategoryID);
        }
    }
}
