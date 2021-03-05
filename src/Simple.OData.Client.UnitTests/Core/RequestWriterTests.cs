using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Simple.OData.Client.Tests.Core
{
    public class RequestWriterV3Tests : RequestWriterTests
    {
        public override string MetadataFile => "Northwind3.xml";
        public override IFormatSettings FormatSettings => new ODataV3Format();

        protected override async Task<IRequestWriter> CreateRequestWriter()
        {
            return new V3.Adapter.RequestWriter(_session, await _client.GetMetadataAsync<Microsoft.Data.Edm.IEdmModel>(), null);
        }
    }

    public class RequestWriterV4Tests : RequestWriterTests
    {
        public override string MetadataFile => "Northwind4.xml";
        public override IFormatSettings FormatSettings => new ODataV4Format();

        protected override async Task<IRequestWriter> CreateRequestWriter()
        {
            return new V4.Adapter.RequestWriter(_session, await _client.GetMetadataAsync<Microsoft.OData.Edm.IEdmModel>(), null);
        }
    }

    public abstract class RequestWriterTests : CoreTestBase
    {
        protected abstract Task<IRequestWriter> CreateRequestWriter();

        [Fact]
        public async Task CreateUpdateRequest_NoPreferredVerb_PartialProperties_Patch()
        {
            var requestWriter = await CreateRequestWriter();
            var result = await requestWriter.CreateUpdateRequestAsync("Products", "",
                        new Dictionary<string, object>() { { "ProductID", 1 } },
                        new Dictionary<string, object>() { { "ProductName", "Chai" } }, false);
            Assert.Equal("PATCH", result.Method);
        }

        [Fact]
        public async Task CreateUpdateRequest_NoPreferredVerb_AllProperties_Patch()
        {
            var requestWriter = await CreateRequestWriter();
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
                var requestWriter = await CreateRequestWriter();
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
            var requestWriter = await CreateRequestWriter();
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

        [Fact]
        public async Task CreateInsertRequest_Post()
        {
            var requestWriter = await CreateRequestWriter();
            var result = await requestWriter.CreateInsertRequestAsync("Employees", "",
                new Dictionary<string, object>()
                {
                    { "FirstName", "John" },
                    { "LastName", "Smith" },
                }, false);
            Assert.Equal("POST", result.Method);
        }

        [Fact]
        public async Task CreateInsertRequest_DateTime_Not_Null_Post()
        {
            var requestWriter = await CreateRequestWriter();
            var result = await requestWriter.CreateInsertRequestAsync("Employees", "",
                new Dictionary<string, object>()
                {
                    { "FirstName", "John" },
                    { "LastName", "Smith" },
                    { "BirthDate", DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Unspecified) },
                }, false);
            Assert.Equal("POST", result.Method);
        }

        [Fact]
        public async Task CreateInsertRequest_DateTime_Null_Post()
        {
            var requestWriter = await CreateRequestWriter();
            var result = await requestWriter.CreateInsertRequestAsync("Employees", "",
                new Dictionary<string, object>()
                {
                    { "FirstName", "John" },
                    { "LastName", "Smith" },
                    { "BirthDate", null },
                }, false);
            Assert.Equal("POST", result.Method);
        }

        [Fact]
        public async Task CreateInsertRequest_DateTimeOffset_Not_Null_Post()
        {
            var requestWriter = await CreateRequestWriter();
            var result = await requestWriter.CreateInsertRequestAsync("Employees", "",
                new Dictionary<string, object>()
                {
                    { "FirstName", "John" },
                    { "LastName", "Smith" },
                    { "BirthDate", DateTimeOffset.Now },
                }, false);
            Assert.Equal("POST", result.Method);
        }

        [Fact]
        public async Task CreateInsertRequest_DateTimeOffset_Null_Post()
        {
            var requestWriter = await CreateRequestWriter();
            var result = await requestWriter.CreateInsertRequestAsync("Employees", "",
                new Dictionary<string, object>()
                {
                    { "FirstName", "John" },
                    { "LastName", "Smith" },
                    { "BirthDate", null },
                }, false);
            Assert.Equal("POST", result.Method);
        }

        [Fact]
        public async Task CreateInsertRequest_Date_Not_Null_Post()
        {
            var requestWriter = await CreateRequestWriter();
            var result = await requestWriter.CreateInsertRequestAsync("Employees", "",
                new Dictionary<string, object>()
                {
                    { "FirstName", "John" },
                    { "LastName", "Smith" },
                    { "HireDate", DateTimeOffset.Now },
                }, false);
            Assert.Equal("POST", result.Method);
        }

        [Fact]
        public async Task CreateInsertRequest_Date_Null_Post()
        {
            var requestWriter = await CreateRequestWriter();
            var result = await requestWriter.CreateInsertRequestAsync("Employees", "",
                new Dictionary<string, object>()
                {
                    { "FirstName", "John" },
                    { "LastName", "Smith" },
                    { "HireDate", null },
                }, false);
            Assert.Equal("POST", result.Method);
        }

        [Fact]
        public async Task CreateInsertRequest_With_Headers()
        {
            var requestWriter = await CreateRequestWriter();
            var result = await requestWriter.CreateInsertRequestAsync("Employees", "",
                new Dictionary<string, object>()
                {
                    { "FirstName", "John" },
                }, false, 
                new Dictionary<string, string>
                {
                    { "header1" , "header1Value" },
                    { "header2" , "header2Value" }
                });

            Assert.Equal("header1Value", result.Headers["header1"]);
            Assert.Equal("header2Value", result.Headers["header2"]);
        }
    }
}
