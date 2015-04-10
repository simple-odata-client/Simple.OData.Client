using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Simple.OData.Client.Extensions;

namespace Simple.OData.Client
{
    class AdapterFactory
    {
        private const string AdapterV3AssemblyName = "Simple.OData.Client.V3.Adapter";
        private const string AdapterV4AssemblyName = "Simple.OData.Client.V4.Adapter";
        private const string AdapterV3TypeName = "Simple.OData.Client.V3.Adapter.ODataAdapter";
        private const string AdapterV4TypeName = "Simple.OData.Client.V4.Adapter.ODataAdapter";

        private readonly ISession _session;

        public AdapterFactory(ISession session)
        {
            _session = session;
        }

        public async Task<IODataAdapter> CreateAdapterAsync(HttpResponseMessage response)
        {
            var protocolVersions = (await GetSupportedProtocolVersionsAsync(response)).ToArray();

            IODataAdapter adapter;
            if (protocolVersions.Any(x => x == ODataProtocolVersion.V1 || x == ODataProtocolVersion.V2 || x == ODataProtocolVersion.V3))
                adapter = LoadAdapter(AdapterV3AssemblyName, AdapterV3TypeName, _session, protocolVersions.First(), response);
            else if (protocolVersions.Any(x => x == ODataProtocolVersion.V4))
                adapter = LoadAdapter(AdapterV4AssemblyName, AdapterV4TypeName, _session, protocolVersions.First(), response);
            else
                throw new NotSupportedException(string.Format("OData protocol {0} is not supported", protocolVersions));

            return adapter;
        }

        public async Task<string> GetMetadataAsStringAsync(HttpResponseMessage response)
        {
            return await response.Content.ReadAsStringAsync();
        }

        public IODataAdapter ParseMetadata(string metadataString)
        {
            var reader = XmlReader.Create(new StringReader(metadataString));
            reader.MoveToContent();
            var protocolVersion = reader.GetAttribute("Version");

            if (protocolVersion == ODataProtocolVersion.V1 ||
                protocolVersion == ODataProtocolVersion.V2 ||
                protocolVersion == ODataProtocolVersion.V3)
            {
                while (reader.Read())
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        var version = reader.GetAttribute("m:" + HttpLiteral.MaxDataServiceVersion);
                        if (string.IsNullOrEmpty(version))
                            version = reader.GetAttribute("m:" + HttpLiteral.DataServiceVersion);
                        if (!string.IsNullOrEmpty(version) && string.Compare(version, protocolVersion, StringComparison.Ordinal) > 0)
                            protocolVersion = version;

                        break;
                    }
                }

                return LoadAdapter(AdapterV3AssemblyName, AdapterV3TypeName, _session, protocolVersion, metadataString);
            }
            else if (protocolVersion == ODataProtocolVersion.V4)
            {
                return LoadAdapter(AdapterV4AssemblyName, AdapterV4TypeName, _session, protocolVersion, metadataString);
            }

            throw new NotSupportedException(string.Format("OData protocol {0} is not supported", protocolVersion));
        }

        internal async Task<HttpResponseMessage> SendMetadataRequestAsync(CancellationToken cancellationToken)
        {
            var request = new ODataRequest(RestVerbs.Get, _session, ODataLiteral.Metadata);

            using (var requestRunner = new RequestRunner(_session))
            {
                return await requestRunner.ExecuteRequestAsync(request, cancellationToken);    
            }
        }

        private async Task<IEnumerable<string>> GetSupportedProtocolVersionsAsync(HttpResponseMessage response)
        {
            IEnumerable<string> headerValues;
            if (response.Headers.TryGetValues(HttpLiteral.DataServiceVersion, out headerValues) ||
                response.Headers.TryGetValues(HttpLiteral.ODataVersion, out headerValues))
            {
                return headerValues.SelectMany(x => x.Split(';')).Where(x => x.Length > 0);                
            }
            else
            {
                try
                {
                    var metadataString = await GetMetadataAsStringAsync(response);
                    var reader = XmlReader.Create(new StringReader(metadataString));
                    reader.MoveToContent();
                    return new [] {reader.GetAttribute("Version")};
                }
                catch (Exception)
                {
                    throw new InvalidOperationException("Unable to identify OData protocol version");
                }
            }
        }

        private IODataAdapter LoadAdapter(string adapterAssemblyName, string adapterTypeName, params object[] ctorParams)
        {
            try
            {
#if PORTABLE
                var assemblyName = new AssemblyName(adapterAssemblyName);
                var assembly = Assembly.Load(assemblyName);
                var constructors = assembly.GetType(adapterTypeName).GetDeclaredConstructors();
#else
                var constructors = this.GetType().Assembly.GetType(adapterTypeName).GetDeclaredConstructors();
#endif
                var ctor = constructors.Single(x => 
                    x.GetParameters().Count() == ctorParams.Count() &&
                    x.GetParameters().Last().ParameterType == ctorParams.Last().GetType());
                return ctor.Invoke(ctorParams) as IODataAdapter;
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(string.Format("Unable to load OData adapter from assembly {0}", adapterAssemblyName), exception);
            }
        }
    }
}