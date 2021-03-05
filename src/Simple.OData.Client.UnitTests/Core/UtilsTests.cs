using Xunit;

namespace Simple.OData.Client.Tests.Core
{
    public class UtilsTests
    {
        [Theory]
        [InlineData("http://company.com", "", "http://company.com/")]
        [InlineData("http://company.com", null, "http://company.com/")]
        [InlineData("http://company.com", "$metadata", "http://company.com/$metadata")]
        [InlineData("http://company.com/", "$metadata", "http://company.com/$metadata")]
        [InlineData("https://company.com", "$metadata", "https://company.com/$metadata")]
        [InlineData("http://company.com?client=100", "$metadata", "http://company.com/$metadata?client=100")]
        [InlineData("http://company.com/feed?client=100", "$metadata", "http://company.com/feed/$metadata?client=100")]
        [InlineData("http://company.com", "Products", "http://company.com/Products")]
        [InlineData("http://company.com/", "Products", "http://company.com/Products")]
        [InlineData("http://company.com/feed", "Products", "http://company.com/feed/Products")]
        [InlineData("http://company.com", "Products?$filter=Id%20eq%201", "http://company.com/Products?$filter=Id%20eq%201")]
        [InlineData("http://company.com/feed", "Products?$filter=Id%20eq%201", "http://company.com/feed/Products?$filter=Id%20eq%201")]
        [InlineData("http://company.com?client=100", "Products?$filter=Id%20eq%201", "http://company.com/Products?$filter=Id%20eq%201&client=100")]
        [InlineData("http://company.com/feed?client=100", "Products?$filter=Id%20eq%201", "http://company.com/feed/Products?$filter=Id%20eq%201&client=100")]
        public void CreateAbsoluteUri(string baseUri, string relativePath, string expected)
        {
            var actual = Utils.CreateAbsoluteUri(baseUri, relativePath);

            Assert.Equal(expected, actual.AbsoluteUri);
        }
    }
}