using System;
using Simple.OData.Client.Extensions;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;
using System.Net.Http.Headers;
using Simple.OData.Client.Core.Adapter;

namespace Simple.OData.Client
{
    internal class AdapterFactory : IAdapterFactory
    {
        private const string AdapterV3AssemblyName = "Simple.OData.Client.V3.Adapter";
        private const string AdapterV3TypeName = "Simple.OData.Client.V3.Adapter.ODataAdapter";
        private const string AdapterV4AssemblyName = "Simple.OData.Client.V4.Adapter";
        private const string AdapterV4TypeName = "Simple.OData.Client.V4.Adapter.ODataAdapter";

        private readonly ISession _session;

        public AdapterFactory(ISession session)
        {
            _session = session;
        }

        public IODataAdapter CreateAdapter(string metadataString)
        {
            var protocolVersion = GetMetadataProtocolVersion(metadataString);
            return LoadAdapter(protocolVersion, _session, metadataString);            
        }

        public async Task<IODataAdapter> CreateAdapterAsync(HttpResponseMessage response)
        {
            string metadataDocument = await GetMetadataDocumentAsync(response);
            var protocolVersions = GetSupportedProtocolVersionsAsync(response.Headers, metadataDocument).ToArray();
            var loader = _session.Settings.AdapterFactory;
            if (loader != null)
            {
                foreach (var protocolVersion in protocolVersions)
                {
                    if (loader.CanLoadForVersion(protocolVersion))
                    {
                        return loader.LoadAdapter(protocolVersion, _session, metadataDocument);
                    }
                }
            }
            
            foreach (var protocolVersion in protocolVersions)
            {
                if (CanLoadForVersion(protocolVersion))
                {
                    return LoadInternalAdapter(protocolVersion, _session, metadataDocument);
                }
            }

            throw new NotSupportedException(string.Format("OData protocols {0} are not supported", string.Join(",", protocolVersions)));
        }

        public async Task<string> GetMetadataDocumentAsync(HttpResponseMessage response)
        {
            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
        }

        public IODataAdapter ParseMetadata(string metadataDocument)
        {
            var protocolVersion = GetMetadataProtocolVersion(metadataDocument);
            return LoadAdapter(protocolVersion, _session, metadataDocument);
        }

        public bool CanLoadForVersion(string protocolVersion)
        {
            return (protocolVersion == ODataProtocolVersion.V1 ||
                    protocolVersion == ODataProtocolVersion.V2 ||
                    protocolVersion == ODataProtocolVersion.V3 ||
                    protocolVersion == ODataProtocolVersion.V4);            
        }


        private string GetMetadataProtocolVersion(string metadataString)
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
            }

            return protocolVersion;
        }

        private IEnumerable<string> GetSupportedProtocolVersionsAsync(HttpResponseHeaders responseHeaders, string metaDataDocument)
        {
            IEnumerable<string> headerValues;
            if (responseHeaders.TryGetValues(HttpLiteral.DataServiceVersion, out headerValues) ||
                responseHeaders.TryGetValues(HttpLiteral.ODataVersion, out headerValues))
            {
                return headerValues.SelectMany(x => x.Split(';')).Where(x => x.Length > 0);
            }
            else
            {
                try
                {
                    var protocolVersion = GetMetadataProtocolVersion(metaDataDocument);
                    return new[] { protocolVersion };
                }
                catch (Exception)
                {
                    throw new InvalidOperationException("Unable to identify OData protocol version");
                }
            }
        }

        public IODataAdapter LoadAdapter(string protocolVersion, ISession session, string metadataDocument)
        {
            var loader = _session.Settings.AdapterFactory;
            if (loader != null && loader.CanLoadForVersion(protocolVersion))
            {

                return loader.LoadAdapter(protocolVersion, _session, metadataDocument);
                
            }
            else
            {
                return LoadInternalAdapter(protocolVersion, session, metadataDocument);
            }
        }

        private IODataAdapter LoadInternalAdapter(string protocolVersion, ISession session, string metadataDocument)
        {
            if (protocolVersion == ODataProtocolVersion.V1 ||
                protocolVersion == ODataProtocolVersion.V2 ||
                protocolVersion == ODataProtocolVersion.V3)
            {
                return LoadInternalAdapter(AdapterV3AssemblyName, AdapterV3TypeName, session, protocolVersion, metadataDocument);
            }
            else if (protocolVersion == ODataProtocolVersion.V4)
            {
                return LoadInternalAdapter(AdapterV4AssemblyName, AdapterV4TypeName, session, protocolVersion, metadataDocument);
            }

            throw new NotSupportedException(string.Format("OData protocol {0} is not supported", protocolVersion));
        }

        private IODataAdapter LoadInternalAdapter(string adapterAssemblyName, string adapterTypeName, params object[] ctorParams)
        {
            try
            {
                Assembly assembly = null;
#if PORTABLE
                var assemblyName = new AssemblyName(adapterAssemblyName);
                assembly = Assembly.Load(assemblyName);
#else
                assembly = this.GetType().Assembly;
#endif
                var constructors = assembly.GetType(adapterTypeName).GetDeclaredConstructors();
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
