using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Spatial;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Data.Edm;
using Microsoft.Data.Edm.Csdl;
using Microsoft.Data.OData;

#pragma warning disable 1591

namespace Simple.OData.Client
{
    public static class V3ModelAdapter
    {
        public static void Reference() { }
    }
}

#pragma warning disable 1591

namespace Simple.OData.Client.V3.Adapter
{
    public class ODataModelAdapter : ODataModelAdapterBase
    {
        public override AdapterVersion AdapterVersion { get { return AdapterVersion.V3; } }

        public new IEdmModel Model
        {
            get { return base.Model as IEdmModel; }
            set { base.Model = value; }
        }

        private ODataModelAdapter(string protocolVersion)
        {
            ProtocolVersion = protocolVersion;
        }

        public ODataModelAdapter(string protocolVersion, HttpResponseMessage response)
            : this(protocolVersion)
        {
            var readerSettings = new ODataMessageReaderSettings
            {
                MessageQuotas = { MaxReceivedMessageSize = Int32.MaxValue }
            };
            using (var messageReader = new ODataMessageReader(new ODataResponseMessage(response), readerSettings))
            {
                Model = messageReader.ReadMetadataDocument();
            }
        }

        public ODataModelAdapter(string protocolVersion, string metadataString)
            : this(protocolVersion)
        {
            using (var reader = XmlReader.Create(new StringReader(metadataString)))
            {
                reader.MoveToContent();
                Model = EdmxReader.Parse(reader);
            }
        }
    }
}
