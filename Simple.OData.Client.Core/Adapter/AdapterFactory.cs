using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
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
        private const string ModelAdapterV3TypeName = "Simple.OData.Client.V3.Adapter.ODataModelAdapter";
        private const string ModelAdapterV4TypeName = "Simple.OData.Client.V4.Adapter.ODataModelAdapter";

        public async Task<IODataModelAdapter> CreateModelAdapterAsync(HttpResponseMessage response)
        {
            var protocolVersions = (await GetSupportedProtocolVersionsAsync(response).ConfigureAwait(false)).ToArray();

            foreach (var protocolVersion in protocolVersions)
            {
                var loadModelAdapter = GetModelAdapterLoader(protocolVersion, response);
                if (loadModelAdapter != null)
                    return loadModelAdapter();
            }
            throw new NotSupportedException(string.Format("OData protocols {0} are not supported", string.Join(",", protocolVersions)));
        }

        public IODataModelAdapter CreateModelAdapter(string metadataString)
        {
            var protocolVersion = GetMetadataProtocolVersion(metadataString);
            var loadModelAdapter = GetModelAdapterLoader(protocolVersion, metadataString);
            if (loadModelAdapter == null)
                throw new NotSupportedException(string.Format("OData protocol {0} is not supported", protocolVersion));

            return loadModelAdapter();
        }

        public Func<ISession, IODataAdapter> CreateAdapter(string metadataString)
        {
            var modelAdapter = CreateModelAdapter(metadataString);

            var loadAdapter = GetAdapterLoader(modelAdapter);
            if (loadAdapter == null)
                throw new NotSupportedException(string.Format("OData protocol {0} is not supported", modelAdapter.ProtocolVersion));

            return loadAdapter;
        }

        private static object CallConstructor(string assemblyName, string typeName, params object[] constructorParams)
        {
            Assembly assembly = null;
#if NETSTANDARD2_0
            assembly = Assembly.Load(new AssemblyName(assemblyName));
#else
            assembly = this.GetType().Assembly;
#endif

            var constructors = assembly.GetType(typeName).GetDeclaredConstructors();

#if NETSTANDARD2_0
            var ctor = constructors.Single(x =>
                x.GetParameters().Count() == constructorParams.Count() &&
                x.GetParameters().Last().ParameterType.GetTypeInfo().IsAssignableFrom(constructorParams.Last().GetType().GetTypeInfo()));
#else
            var ctor = constructors.Single(x =>
                x.GetParameters().Count() == ctorParams.Count() &&
                x.GetParameters().Last().ParameterType.IsAssignableFrom(ctorParams.Last().GetType()));
#endif
            return ctor.Invoke(constructorParams);
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
                    var metadataString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var protocolVersion = GetMetadataProtocolVersion(metadataString);
                    return new[] { protocolVersion };
                }
                catch (Exception)
                {
                    throw new InvalidOperationException("Unable to identify OData protocol version");
                }
            }
        }

        private Func<ISession, IODataAdapter> GetAdapterLoader(IODataModelAdapter modelAdapter)
        {
            if (modelAdapter.ProtocolVersion == ODataProtocolVersion.V1 ||
                modelAdapter.ProtocolVersion == ODataProtocolVersion.V2 ||
                modelAdapter.ProtocolVersion == ODataProtocolVersion.V3)
                return session => LoadAdapter(AdapterV3AssemblyName, AdapterV3TypeName, session, modelAdapter);
            if (modelAdapter.ProtocolVersion == ODataProtocolVersion.V4)
                return session => LoadAdapter(AdapterV4AssemblyName, AdapterV4TypeName, session, modelAdapter);

            return null;
        }

        private Func<IODataModelAdapter> GetModelAdapterLoader(string protocolVersion, object metadata)
        {
            if (protocolVersion == ODataProtocolVersion.V1 ||
                protocolVersion == ODataProtocolVersion.V2 ||
                protocolVersion == ODataProtocolVersion.V3)
                return () => LoadModelAdapter(AdapterV3AssemblyName, ModelAdapterV3TypeName, protocolVersion, metadata);
            if (protocolVersion == ODataProtocolVersion.V4)
                return () => LoadModelAdapter(AdapterV4AssemblyName, ModelAdapterV4TypeName, protocolVersion, metadata);

            return null;
        }

        private IODataModelAdapter LoadModelAdapter(string modelAdapterAssemblyName, string modelAdapterTypeName, string protocolVersion, object metadata)
        {
            try
            {
                return CallConstructor(modelAdapterAssemblyName, modelAdapterTypeName, protocolVersion, metadata) as IODataModelAdapter;
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(string.Format("Unable to load OData adapter from assembly {0}", modelAdapterAssemblyName), exception);
            }
        }

        private IODataAdapter LoadAdapter(string adapterAssemblyName, string adapterTypeName, ISession session, IODataModelAdapter modelAdapter)
        {
            // load adapter from user defined factory, if assigned and the factory can load for the target version
            if (session.Settings.AdapterFactory != null && session.Settings.AdapterFactory.CanLoadForModel(modelAdapter))
            {
                return session.Settings.AdapterFactory.LoadAdapter(session, modelAdapter);
            }
            try
            {
                return CallConstructor(adapterAssemblyName, adapterTypeName, session, modelAdapter) as IODataAdapter;
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(string.Format("Unable to load OData adapter from assembly {0}", adapterAssemblyName), exception);
            }
        }

        private string GetMetadataProtocolVersion(string metadataString)
        {
            using (var reader = XmlReader.Create(new StringReader(metadataString)))
            {
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
        }
    }
}