using System;
using System.Threading.Tasks;

using Microsoft.OData;
using Microsoft.OData.Edm;

namespace Simple.OData.Client.V4.Adapter
{
    public abstract class ODataMessageWriterWrapper : IDisposable
    {
        protected ODataMessageWriter _messageWriter;

        protected ODataMessageWriterWrapper(IODataRequestMessage message, ODataMessageWriterSettings writerSettings, IEdmModel model)
        {
            _messageWriter = new ODataMessageWriter(
                message, writerSettings, model);
        }

        public async virtual Task<ODataWriter> CreateODataResourceWriterAsync()
        {
            if(_messageWriter != null)
            {
                return await _messageWriter.CreateODataResourceWriterAsync();
            }

            return null;
        }

        public void Dispose()
        {
            if (_messageWriter != null)
                _messageWriter.Dispose();
        }
    }

    public class ConcreteODataMessageWriter : ODataMessageWriterWrapper
    {
        public ConcreteODataMessageWriter(IODataRequestMessage message, ODataMessageWriterSettings writerSettings, IEdmModel model)
            : base(message, writerSettings, model)
        {

        }
    }
}