using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.OData.Edm;
using Simple.OData.Client.V4.Adapter;
using Xunit;

namespace Simple.OData.Client.Tests.Core
{
    public class ActionWithEnumTests : TestBase
    {
        public override string MetadataFile { get { return "ActionWithEnum.xml"; } }
        public override IFormatSettings FormatSettings { get { return new ODataV4Format(); } }

        enum Rank
        {
            First,
            Second,
            Third,
        }

        [Fact]
        public async Task ActionWithEnum()
        {
            var requestWriter = new RequestWriter(_session, await _client.GetMetadataAsync<IEdmModel>(), null);
            var result = await requestWriter.CreateActionRequestAsync("Entity", "MakeFromParam", null,
                        new Dictionary<string, object>() { { "Name", "Entity Name" }, { "Rank", Rank.Second } }, false);
            Assert.Equal("POST", result.Method);
        }
    }
}
