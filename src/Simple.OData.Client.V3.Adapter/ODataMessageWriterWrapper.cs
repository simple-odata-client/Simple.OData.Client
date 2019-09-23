using System;
using Microsoft.Data.OData;
using Microsoft.Data.Edm;

namespace Simple.OData.Client.V3.Adapter
{
    public abstract class ODataMessageWriterWrapper : IDisposable
    {
        protected ODataMessageWriter _messageWriter;

        protected ODataMessageWriterWrapper(IODataRequestMessage message, ODataMessageWriterSettings writerSettings, IEdmModel model)
        {
            _messageWriter = new ODataMessageWriter(
                message, writerSettings, model);
        }

        public virtual ODataWriter CreateODataEntryWriter()
        {
            if(_messageWriter != null)
            {
                return _messageWriter.CreateODataEntryWriter();
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