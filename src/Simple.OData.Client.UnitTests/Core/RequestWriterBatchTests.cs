using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Moq;

using Microsoft.Data.OData;
using Microsoft.Data.Edm;

using Xunit;

namespace Simple.OData.Client.Tests.Core
{
    public class TestableV3RequestWriter : V3.Adapter.RequestWriter
    {
        public TestableV3RequestWriter(ISession session, IEdmModel model, Lazy<IBatchWriter> deferredBatchWriter,
            Func<IODataRequestMessage, ODataMessageWriterSettings, IEdmModel, ODataMessageWriter> messageWriterInitializer)
            : base(session,model,deferredBatchWriter, messageWriterInitializer)
        {

        }
    }

    public class TestableV4RequestWriter : V4.Adapter.RequestWriter
    {
        public TestableV4RequestWriter(ISession session, Microsoft.OData.Edm.IEdmModel model, Lazy<IBatchWriter> deferredBatchWriter,
            Func<Microsoft.OData.IODataRequestMessage, 
                Microsoft.OData.ODataMessageWriterSettings,
                Microsoft.OData.Edm.IEdmModel, 
                Microsoft.OData.ODataMessageWriter> messageWriterInitializer)
            : base(session, model, deferredBatchWriter, messageWriterInitializer)
        {

        }
    }

    public class RequestWriterBatchV3Tests : RequestWriterBatchTests
    {
        public override string MetadataFile => "Northwind3.xml";
        public override IFormatSettings FormatSettings => new ODataV3Format();

        private StreamWriter batchStream = new StreamWriter(
            new MemoryStream(1024));

        protected override async Task<IRequestWriter> CreateBatchRequestWriter()
        {
            return new TestableV3RequestWriter(
                _session,
                await _client.GetMetadataAsync<Microsoft.Data.Edm.IEdmModel>(),
                new Lazy<IBatchWriter>(() => _session.Adapter.GetBatchWriter(
                    new Dictionary<object, IDictionary<string,object>>())),                
                (message, writerSettings, edmModel) => VerifyOperationMessageHeaders(message, writerSettings, edmModel));
        }

        protected ODataMessageWriter VerifyOperationMessageHeaders(
            IODataRequestMessage message, 
            ODataMessageWriterSettings writerSettings, 
            IEdmModel model)
        {
            Assert.Equal(3, message.Headers.Count());
            Assert.Equal("My-Custom-Http-Header", message.Headers.ElementAt(2).Key);
            Assert.Equal("Smile-and-shine", message.Headers.ElementAt(2).Value);

            return null;
        }

    }

    public class RequestWriterBatchV4Tests : RequestWriterBatchTests
    {
        public override string MetadataFile => "Northwind4.xml";
        public override IFormatSettings FormatSettings => new ODataV4Format();

        protected override async Task<IRequestWriter> CreateBatchRequestWriter()
        {
            return new TestableV4RequestWriter(
                _session,
                await _client.GetMetadataAsync<Microsoft.OData.Edm.IEdmModel>(),
                new Lazy<IBatchWriter>(() => base.BatchWriter),
                (message, writerSettings, edmModel) => VerifyOperationMessageHeaders(message, writerSettings, edmModel));
        }

        protected Microsoft.OData.ODataMessageWriter VerifyOperationMessageHeaders(
            Microsoft.OData.IODataRequestMessage message,
            Microsoft.OData.ODataMessageWriterSettings writerSettings, 
            Microsoft.OData.Edm.IEdmModel model)
        {
            Assert.Equal(3, message.Headers.Count());
            Assert.Equal("My-Custom-Http-Header", message.Headers.ElementAt(2).Key);
            Assert.Equal("Smile-and-shine", message.Headers.ElementAt(2).Value);

            return null;
        }
    }
    public abstract class RequestWriterBatchTests : CoreTestBase
    {
        private Dictionary<object, IDictionary<string, object>> _batchContent = 
            new Dictionary<object, IDictionary<string, object>>(3);

        protected Dictionary<object, IDictionary<string, object>> BatchContent => _batchContent;

        protected abstract Task<IRequestWriter> CreateBatchRequestWriter();

        protected IBatchWriter BatchWriter => _session.Adapter.GetBatchWriter(
                    _batchContent);

        [Fact]
        public async Task CreateUpdateRequest_NoPreferredVerb_AllProperties_OperationHeaders_Patch()
        {
            var requestWriter = await CreateBatchRequestWriter();
            
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
                        },
                        new Dictionary<string, string>()
                        {
                            { "My-Custom-Http-Header","Smile-and-shine"}
                        }, false);

            Assert.Equal("PATCH", result.Method);
            Assert.Equal(1, result.Headers.Count);
        }       
    }
}
