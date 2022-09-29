using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Edm;
using Simple.OData.Client.V3.Adapter;
using Xunit;

namespace Simple.OData.Client.Tests.Core;

public class ResponseReaderV3Tests : CoreTestBase
{
	private const int productProperties = 10;
	private const int categoryProperties = 4;

	public override string MetadataFile => "Northwind3.xml";
	public override IFormatSettings FormatSettings => new ODataV3Format();

	[Fact]
	public async Task GetSingleProduct()
	{
		var response = SetUpResourceMock("SingleProduct.xml");
		var responseReader = new ResponseReader(_session, await _client.GetMetadataAsync<IEdmModel>().ConfigureAwait(false));
		var result = (await responseReader.GetResponseAsync(response).ConfigureAwait(false)).AsEntry(false);
		Assert.Equal(productProperties, result.Count);
	}

	[Fact]
	public async Task GetMultipleProducts()
	{
		var response = SetUpResourceMock("MultipleProducts.xml");
		var responseReader = new ResponseReader(_session, await _client.GetMetadataAsync<IEdmModel>().ConfigureAwait(false));
		var result = (await responseReader.GetResponseAsync(response).ConfigureAwait(false)).Feed.Entries;
		Assert.Equal(20, result.Count);
		Assert.Equal(productProperties, result.First().Data.Count);
	}

	[Fact]
	public async Task GetSingleProductWithCategory()
	{
		var response = SetUpResourceMock("SingleProductWithCategory.xml");
		var responseReader = new ResponseReader(_session, await _client.GetMetadataAsync<IEdmModel>().ConfigureAwait(false));
		var result = (await responseReader.GetResponseAsync(response).ConfigureAwait(false)).AsEntry(false);
		Assert.Equal(productProperties + 1, result.Count);
		Assert.Equal(categoryProperties, (result["Category"] as IDictionary<string, object>).Count);
	}

	[Fact]
	public async Task GetMultipleProductsWithCategory()
	{
		var response = SetUpResourceMock("MultipleProductsWithCategory.xml");
		var responseReader = new ResponseReader(_session, await _client.GetMetadataAsync<IEdmModel>().ConfigureAwait(false));
		var result = (await responseReader.GetResponseAsync(response).ConfigureAwait(false)).Feed.Entries;
		Assert.Equal(20, result.Count);
		Assert.Equal(productProperties + 1, result.First().Data.Count);
		Assert.Equal(categoryProperties, (result.First().Data["Category"] as IDictionary<string, object>).Count);
	}

	[Fact]
	public async Task GetSingleCategoryWithProducts()
	{
		var response = SetUpResourceMock("SingleCategoryWithProducts.xml");
		var responseReader = new ResponseReader(_session, await _client.GetMetadataAsync<IEdmModel>().ConfigureAwait(false));
		var result = (await responseReader.GetResponseAsync(response).ConfigureAwait(false)).AsEntry(false);
		Assert.Equal(categoryProperties + 1, result.Count);
		Assert.Equal(12, (result["Products"] as IEnumerable<IDictionary<string, object>>).Count());
		Assert.Equal(productProperties, (result["Products"] as IEnumerable<IDictionary<string, object>>).First().Count);
	}

	[Fact]
	public async Task GetMultipleCategoriesWithProducts()
	{
		var response = SetUpResourceMock("MultipleCategoriesWithProducts.xml");
		var responseReader = new ResponseReader(_session, await _client.GetMetadataAsync<IEdmModel>().ConfigureAwait(false));
		var result = (await responseReader.GetResponseAsync(response).ConfigureAwait(false)).Feed.Entries;
		Assert.Equal(8, result.Count);
		Assert.Equal(categoryProperties + 1, result.First().Data.Count);
		Assert.Equal(12, (result.First().Data["Products"] as IEnumerable<IDictionary<string, object>>).Count());
		Assert.Equal(productProperties, (result.First().Data["Products"] as IEnumerable<IDictionary<string, object>>).First().Count);
	}

	[Fact]
	public async Task GetSingleProductWithComplexProperty()
	{
		var response = SetUpResourceMock("SingleProductWithComplexProperty.xml");
		var responseReader = new ResponseReader(_session, null);
		var result = (await responseReader.GetResponseAsync(response).ConfigureAwait(false)).AsEntry(false);
		Assert.Equal(productProperties + 1, result.Count);
		var quantity = result["Quantity"] as IDictionary<string, object>;
		Assert.NotNull(quantity);
		Assert.Equal(10d, quantity["Value"]);
		Assert.Equal("bags", quantity["Units"]);
	}

	[Fact]
	public async Task GetSingleProductWithCollectionOfPrimitiveProperties()
	{
		var response = SetUpResourceMock("SingleProductWithCollectionOfPrimitiveProperties.xml");
		var responseReader = new ResponseReader(_session, null);
		var result = (await responseReader.GetResponseAsync(response).ConfigureAwait(false)).AsEntry(false);
		Assert.Equal(productProperties + 2, result.Count);
		var tags = result["Tags"] as IList<dynamic>;
		Assert.Equal(2, tags.Count);
		Assert.Equal("Bakery", tags[0]);
		Assert.Equal("Food", tags[1]);
		var ids = result["Ids"] as IList<dynamic>;
		Assert.Equal(2, ids.Count);
		Assert.Equal(1, ids[0]);
		Assert.Equal(2, ids[1]);
	}

	[Fact]
	public async Task GetSingleProductWithCollectionOfComplexProperties()
	{
		var response = SetUpResourceMock("SingleProductWithCollectionOfComplexProperties.xml");
		var responseReader = new ResponseReader(_session, null);
		var result = (await responseReader.GetResponseAsync(response).ConfigureAwait(false)).AsEntry(false);
		Assert.Equal(productProperties + 1, result.Count);
		var tags = result["Tags"] as IList<dynamic>;
		Assert.Equal(2, tags.Count);
		Assert.Equal("Food", tags[0]["group"]);
		Assert.Equal("Bakery", tags[0]["value"]);
		Assert.Equal("Food", tags[1]["group"]);
		Assert.Equal("Meat", tags[1]["value"]);
	}

	[Fact]
	public async Task GetSingleProductWithEmptyCollectionOfComplexProperties()
	{
		var response = SetUpResourceMock("SingleProductWithEmptyCollectionOfComplexProperties.xml");
		var responseReader = new ResponseReader(_session, null);
		var result = (await responseReader.GetResponseAsync(response).ConfigureAwait(false)).AsEntry(false);
		Assert.Equal(productProperties + 1, result.Count);
		var tags = result["Tags"] as IList<dynamic>;
		Assert.Equal(0, tags.Count);
	}

	[Fact]
	public Task GetColorsSchema()
	{
		return ParseSchema("Colors");
	}

	[Fact]
	public Task GetFacebookSchema()
	{
		return ParseSchema("Facebook");
	}

	[Fact]
	public Task GetFlickrSchema()
	{
		return ParseSchema("Flickr");
	}

	[Fact]
	public Task GetGoogleMapsSchema()
	{
		return ParseSchema("GoogleMaps");
	}

	[Fact]
	public Task GetiPhoneSchema()
	{
		return ParseSchema("iPhone");
	}

	[Fact]
	public Task GetTwitterSchema()
	{
		return ParseSchema("Twitter");
	}

	[Fact]
	public Task GetYouTubeSchema()
	{
		return ParseSchema("YouTube");
	}

	[Fact]
	public Task GetNestedSchema()
	{
		return ParseSchema("Nested");
	}

	[Fact]
	public Task GetArrayOfNestedSchema()
	{
		return ParseSchema("ArrayOfNested");
	}

	private static Task ParseSchema(string schemaName)
	{
		var document = GetResourceAsString(schemaName + ".edmx");
		var metadata = ODataClient.ParseMetadataString<IEdmModel>(document);
		var entityType = metadata.SchemaElements
			.Single(x => x.SchemaElementKind == EdmSchemaElementKind.TypeDefinition &&
				(x as IEdmType).TypeKind == EdmTypeKind.Entity);
		Assert.Equal(schemaName, entityType.Name);
		return Task.FromResult(0);
	}
}
