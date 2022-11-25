using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;


namespace Simple.OData.Client;

/// <summary>
/// ODataAdapterFactory creates OData adapters for specific OData protocols.
/// Custom OData adapter classes can be derived from ODataAdapterFactory to provide custom implementation of its methods.
/// </summary>
public class ODataAdapterFactory : IODataAdapterFactory
{
	private const string AdapterV3AssemblyName = "Simple.OData.Client.V3.Adapter";
	private const string AdapterV4AssemblyName = "Simple.OData.Client.V4.Adapter";
	private const string AdapterV3TypeName = "Simple.OData.Client.V3.Adapter.ODataAdapter";
	private const string AdapterV4TypeName = "Simple.OData.Client.V4.Adapter.ODataAdapter";
	private const string ModelAdapterV3TypeName = "Simple.OData.Client.V3.Adapter.ODataModelAdapter";
	private const string ModelAdapterV4TypeName = "Simple.OData.Client.V4.Adapter.ODataModelAdapter";

	/// <inheritdoc />
	public async virtual Task<IODataModelAdapter> CreateModelAdapterAsync(HttpResponseMessage response, ITypeCache typeCache)
	{
		var protocolVersions = (await GetSupportedProtocolVersionsAsync(response)
			.ConfigureAwait(false)
		).ToArray();

		foreach (var protocolVersion in protocolVersions)
		{
			var loadModelAdapter = GetModelAdapterLoader(protocolVersion, response, typeCache);
			if (loadModelAdapter != null)
			{
				return loadModelAdapter();
			}
		}

		throw new NotSupportedException($"OData protocols {string.Join(",", protocolVersions)} are not supported");
	}

	/// <inheritdoc />
	public virtual IODataModelAdapter CreateModelAdapter(string metadataString, ITypeCache typeCache)
	{
		var protocolVersion = GetMetadataProtocolVersion(metadataString);
		var loadModelAdapter = GetModelAdapterLoader(protocolVersion, metadataString, typeCache);
		if (loadModelAdapter == null)
		{
			throw new NotSupportedException($"OData protocol {protocolVersion} is not supported");
		}

		return loadModelAdapter();
	}

	/// <inheritdoc />
	public virtual Func<ISession, IODataAdapter> CreateAdapterLoader(string metadataString, ITypeCache typeCache)
	{
		var modelAdapter = CreateModelAdapter(metadataString, typeCache);

		var loadAdapter = GetAdapterLoader(modelAdapter, typeCache);
		if (loadAdapter == null)
		{
			throw new NotSupportedException($"OData protocol {modelAdapter.ProtocolVersion} is not supported");
		}

		return loadAdapter;
	}

	/// <summary>
	/// Returns a collection of supported protocol versions.
	/// </summary>
	/// <param name="response">HTTP response message with schema information</param>
	/// <returns></returns>
	/// <exception cref="InvalidOperationException"></exception>
	protected static async Task<IEnumerable<string>> GetSupportedProtocolVersionsAsync(HttpResponseMessage response)
	{
		if (response.Headers.TryGetValues(HttpLiteral.DataServiceVersion, out var headerValues) ||
			response.Headers.TryGetValues(HttpLiteral.ODataVersion, out headerValues))
		{
			return headerValues.SelectMany(x => x.Split(';')).Where(x => x.Length > 0);
		}
		else
		{
			try
			{
				var metadataString = await response
					.Content
					.ReadAsStringAsync()
					.ConfigureAwait(false);
				var protocolVersion = GetMetadataProtocolVersion(metadataString);
				return new[] { protocolVersion };
			}
			catch (Exception)
			{
				throw new InvalidOperationException("Unable to identify OData protocol version.");
			}
		}
	}

	private Func<ISession, IODataAdapter>? GetAdapterLoader(IODataModelAdapter modelAdapter, ITypeCache typeCache)
	{
		if (modelAdapter.ProtocolVersion == ODataProtocolVersion.V1 ||
			modelAdapter.ProtocolVersion == ODataProtocolVersion.V2 ||
			modelAdapter.ProtocolVersion == ODataProtocolVersion.V3)
		{
			return session => LoadAdapter(AdapterV3AssemblyName, typeCache, AdapterV3TypeName, session, modelAdapter);
		}

		if (modelAdapter.ProtocolVersion == ODataProtocolVersion.V4)
		{
			return session => LoadAdapter(AdapterV4AssemblyName, typeCache, AdapterV4TypeName, session, modelAdapter);
		}

		return null;
	}

	private Func<IODataModelAdapter>? GetModelAdapterLoader(string protocolVersion, object extraInfo, ITypeCache typeCache)
	{
		if (protocolVersion == ODataProtocolVersion.V1 ||
			protocolVersion == ODataProtocolVersion.V2 ||
			protocolVersion == ODataProtocolVersion.V3)
		{
			return () => LoadModelAdapter(typeCache, AdapterV3AssemblyName, ModelAdapterV3TypeName, protocolVersion, extraInfo);
		}

		if (protocolVersion == ODataProtocolVersion.V4)
		{
			return () => LoadModelAdapter(typeCache, AdapterV4AssemblyName, ModelAdapterV4TypeName, protocolVersion, extraInfo);
		}

		return null;
	}

	private static IODataModelAdapter LoadModelAdapter(ITypeCache typeCache, string modelAdapterAssemblyName, string modelAdapterTypeName, params object[] ctorParams)
	{
		try
		{
			var type = LoadType(modelAdapterAssemblyName, modelAdapterTypeName);
			var ctor = FindAdapterConstructor(type, typeCache, ctorParams);
			return ctor.Invoke(ctorParams) as IODataModelAdapter;
		}
		catch (Exception exception)
		{
			throw new InvalidOperationException($"Unable to load OData adapter from assembly {modelAdapterAssemblyName}", exception);
		}
	}

	private static Type LoadType(string assemblyName, string typeName)
	{
		try
		{
			var assemblyNameObj = new AssemblyName(assemblyName);
			return Assembly.Load(assemblyNameObj).GetType(typeName);
		}
		catch (FileNotFoundException)
		{
			var type = Type.GetType(typeName);
			if (type == null)
			{
				throw;
			}

			return type;
		}
	}

	private static ConstructorInfo FindAdapterConstructor(Type type, ITypeCache typeCache, params object[] ctorParams)
	{
		var constructors = typeCache.GetDeclaredConstructors(type);
		return constructors.Single(x =>
			x.GetParameters().Length == ctorParams.Length &&
			x.GetParameters().Last().ParameterType.GetTypeInfo().IsAssignableFrom(ctorParams.Last().GetType().GetTypeInfo()));
	}

	private static IODataAdapter LoadAdapter(string adapterAssemblyName, ITypeCache typeCache, string adapterTypeName, params object[] ctorParams)
	{
		try
		{

			var type = LoadType(adapterAssemblyName, adapterTypeName);
			var constructors = typeCache.GetDeclaredConstructors(type);

			var ctor = constructors.Single(x =>
				x.GetParameters().Length == ctorParams.Length &&
				x.GetParameters().Last().ParameterType.IsInstanceOfType(ctorParams.Last()));

			return ctor.Invoke(ctorParams) as IODataAdapter;
		}
		catch (Exception exception)
		{
			throw new InvalidOperationException($"Unable to load OData adapter from assembly {adapterAssemblyName}", exception);
		}
	}

	/// <summary>
	/// Returns OData protocol version
	/// </summary>
	/// <param name="metadataString">Service metadata</param>
	/// <returns></returns>
	protected static string GetMetadataProtocolVersion(string metadataString)
	{
		using var reader = XmlReader.Create(new StringReader(metadataString));
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
					{
						version = reader.GetAttribute("m:" + HttpLiteral.DataServiceVersion);
					}

					if (!string.IsNullOrEmpty(version) && string.Compare(version, protocolVersion, StringComparison.Ordinal) > 0)
					{
						protocolVersion = version;
					}

					break;
				}
			}
		}

		return protocolVersion;
	}
}
