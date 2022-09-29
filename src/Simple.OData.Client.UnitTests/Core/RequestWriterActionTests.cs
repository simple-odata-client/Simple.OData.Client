using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Simple.OData.Client.Tests.Core;

public class RequestWriterActionTests : CoreTestBase
{
	public override string MetadataFile => "ActionWithDerivedType.xml";
	public override IFormatSettings FormatSettings => new ODataV4Format();

	protected async Task<IRequestWriter> CreateRequestWriter()
	{
		return new V4.Adapter.RequestWriter(_session, await _client.GetMetadataAsync<Microsoft.OData.Edm.IEdmModel>().ConfigureAwait(false), null);
	}

	[Fact]
	public async Task CreateActionRequestWithDerivedType()
	{
		var requestWriter = await CreateRequestWriter().ConfigureAwait(false);
		var request = await requestWriter.CreateActionRequestAsync("CancelSalesOrder", "CancelSalesOrder", null,
			new Dictionary<string, object>
			{
				["Status"] = 936140011,
				["OrderClose"] = new Dictionary<string, object>
				{
					["salesorderid"] = new Dictionary<string, object> { { "salesorderid", new Guid("5AEEC23F-DB4F-E911-A978-000D3A3611E1") } },
					["subject"] = $"Close sales Order {DateTime.Now}",
					["@odata.type"] = "Microsoft.Dynamics.CRM.orderclose"
				}
			}, true).ConfigureAwait(false);
		var stringResult = await request.RequestMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
		var result = JsonConvert.DeserializeObject<JObject>(stringResult);

		Assert.True(result.ContainsKey("OrderClose"));

		var orderClose = result["OrderClose"];

		Assert.NotNull(orderClose);
		Assert.NotNull(orderClose["@odata.type"]);
		Assert.Equal("#Microsoft.Dynamics.CRM.orderclose", ((JValue)orderClose["@odata.type"]).Value as string);
	}
}
