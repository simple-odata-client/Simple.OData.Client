using System.Threading.Tasks;
using Xunit;
using Simple.OData.Client.Extensions;

namespace Simple.OData.Client.Tests
{
    public class PluralizerTests
    {
        private readonly SimplePluralizer _pluralizer = new SimplePluralizer();

        [Theory]
        [InlineData("Person", "Persons")]
        [InlineData("Day", "Days")]
        [InlineData("Dummy", "Dummies")]
        [InlineData("Access", "Accesses")]
        [InlineData("Life", "Lives")]
        [InlineData("Codex", "Codices")]
        public void PluralizeWord(string word, string expectedResult)
        {
            Assert.Equal(expectedResult, _pluralizer.Pluralize(word));
        }

        [Theory]
        [InlineData("Persons", "Person")]
        [InlineData("People", "Person")]
        [InlineData("Days", "Day")]
        [InlineData("Dummies", "Dummy")]
        [InlineData("Accesses", "Access")]
        [InlineData("Lives", "Life")]
        [InlineData("Codices", "Codex")]
        public void SingularizeWord(string word, string expectedResult)
        {
            Assert.Equal(expectedResult, _pluralizer.Singularize(word));
        }

        [Theory]
        [InlineData("Språk", "Språk")]
        public void PluralizeWordWithNonEnglishCharacters(string word, string expectedResult)
        {
            Assert.Equal(expectedResult, _pluralizer.Pluralize(word));
        }

        [Theory]
        [InlineData("Gårds", "Gårds")]
        public void SingularizeWordWithNonEnglishCharacters(string word, string expectedResult)
        {
            Assert.Equal(expectedResult, _pluralizer.Singularize(word));
        }

        [Theory]
        [InlineData("Catalog_Контрагенты", "Catalog_Контрагенты")]
        public void PluralizeWordWithNonLatinCharacters(string word, string expectedResult)
        {
            Assert.Equal(expectedResult, _pluralizer.Pluralize(word));
        }

        [Theory]
        [InlineData("Catalog_Контрагенты", "Catalog_Контрагенты")]
        public void SingularizeWordWithNonLatinCharacters(string word, string expectedResult)
        {
            Assert.Equal(expectedResult, _pluralizer.Singularize(word));
        }

        [Theory]
        [InlineData("Person", "person")]
        [InlineData("Day", "day")]
        [InlineData("Språk", "språk")]
        [InlineData("Person_123", "person123")]
        [InlineData("Språk_123", "språk123")]
        [InlineData("Catalog_Контрагенты_123", "catalogконтрагенты123")]
        public void HomogenizeWord(string word, string expectedResult)
        {
            Assert.Equal(expectedResult, word.Homogenize());
        }
    }

    public class NonLatinSchemaPluralizerTests : TestBase
    {
        public override string MetadataFile => "Russian.xml";
        public override IFormatSettings FormatSettings => new ODataV3Format();

        [Fact]
        public async Task TableWithNonLaticCharacters()
        {
            var client = CreateClient("Russian.xml");
            var commandText = await client
                .For("Catalog_Контрагенты")
                .Top(10)
                .GetCommandTextAsync();

            Assert.Equal("Catalog_Контрагенты?$top=10", commandText);
        }

        [Fact]
        public async Task TableWithNonLaticCharacters_NoPluralizer()
        {
            var client = CreateClient("Russian.xml", ODataNameMatchResolver.Strict);
            var commandText = await client
                .For("Catalog_Контрагенты")
                .Top(10)
                .GetCommandTextAsync();

            Assert.Equal("Catalog_Контрагенты?$top=10", commandText);
        }
    }
}