using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.OData.Edm;
using Xunit;
using Simple.OData.Client.V4.Adapter;

namespace Simple.OData.Client.Tests
{
    public class RequestWriterTests : TestBase
    {
        public override string MetadataFile { get { return "Northwind.xml"; } }
        public override IFormatSettings FormatSettings { get { return new ODataV4Format(); } }

        [Fact]
        public async Task CreateUpdateRequest_NoPreferredVerb_PartialProperties_Patch()
        {
            var requestWriter = new RequestWriter(_session, await _client.GetMetadataAsync<IEdmModel>(), null);
            var result = await requestWriter.CreateUpdateRequestAsync("Products", "",
                        new Dictionary<string, object>() { { "ProductID", 1 } },
                        new Dictionary<string, object>() { { "ProductName", "Chai" } }, false);
            Assert.Equal("PATCH", result.Method);
        }

        [Fact]
        public async Task CreateUpdateRequest_NoPreferredVerb_AllProperties_Patch()
        {
            var requestWriter = new RequestWriter(_session, await _client.GetMetadataAsync<IEdmModel>(), null);
            var result = await requestWriter.CreateUpdateRequestAsync("Products", "",
                        new Dictionary<string, object>() { { "ProductID", 1 } },
                        new Dictionary<string, object>()
                        {
                            { "ProductID", 1 },
                            { "SupplierID", 2 },
                            { "CategoryID", 3 },
                            { "ProductName", "Chai" },
                            { "EnglishName", "Tea" },
                            { "QuantityPerUnit", "10" },
                            { "UnitPrice", 20m },
                            { "UnitsInStock", 100 },
                            { "UnitsOnOrder", 1000 },
                            { "ReorderLevel", 500 },
                            { "Discontinued", false },
                        }, false);
            Assert.Equal("PATCH", result.Method);
        }

        [Fact]
        public async Task CreateUpdateRequest_PreferredVerbPut_AllProperties_Put()
        {
            var preferredUpdateMethod = _session.Settings.PreferredUpdateMethod;
            try
            {
                _session.Settings.PreferredUpdateMethod = ODataUpdateMethod.Put;
                var requestWriter = new RequestWriter(_session, await _client.GetMetadataAsync<IEdmModel>(), null);
                var result = await requestWriter.CreateUpdateRequestAsync("Products", "",
                            new Dictionary<string, object>() { { "ProductID", 1 } },
                            new Dictionary<string, object>()
                        {
                            { "ProductID", 1 },
                            { "SupplierID", 2 },
                            { "CategoryID", 3 },
                            { "ProductName", "Chai" },
                            { "EnglishName", "Tea" },
                            { "QuantityPerUnit", "10" },
                            { "UnitPrice", 20m },
                            { "UnitsInStock", 100 },
                            { "UnitsOnOrder", 1000 },
                            { "ReorderLevel", 500 },
                            { "Discontinued", false },
                        }, false);
                Assert.Equal("PUT", result.Method);
            }
            finally
            {
                _session.Settings.PreferredUpdateMethod = preferredUpdateMethod;
            }
        }

        [Fact]
        public async Task CreateUpdateRequest_PreferredVerbPatch_ChangedKey_Put()
        {
            var requestWriter = new RequestWriter(_session, await _client.GetMetadataAsync<IEdmModel>(), null);
            var result = await requestWriter.CreateUpdateRequestAsync("Products", "",
                        new Dictionary<string, object>() { { "ProductID", 1 } },
                        new Dictionary<string, object>()
                        {
                            { "ProductID", 10 },
                            { "SupplierID", 2 },
                            { "CategoryID", 3 },
                            { "ProductName", "Chai" },
                            { "EnglishName", "Tea" },
                            { "QuantityPerUnit", "10" },
                            { "UnitPrice", 20m },
                            { "UnitsInStock", 100 },
                            { "UnitsOnOrder", 1000 },
                            { "ReorderLevel", 500 },
                            { "Discontinued", false },
                        }, false);
            Assert.Equal("PUT", result.Method);
        }
    }
}
