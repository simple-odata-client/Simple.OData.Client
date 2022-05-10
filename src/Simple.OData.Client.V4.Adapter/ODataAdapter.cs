using System;
using System.Collections.Generic;
using Microsoft.OData.Edm;
using Microsoft.Spatial;

using Simple.OData.Client.Adapter;

namespace Simple.OData.Client.V4.Adapter;

public class ODataAdapter : ODataAdapterBase
{
	private readonly ISession _session;
	private IMetadata? _metadata;

	public override AdapterVersion AdapterVersion => AdapterVersion.V4;

	public override ODataPayloadFormat DefaultPayloadFormat => ODataPayloadFormat.Json;

	public ODataAdapter(ISession session, IODataModelAdapter modelAdapter)
	{
		_session = session;
		ProtocolVersion = modelAdapter.ProtocolVersion;
		Model = modelAdapter.Model as IEdmModel;

		session.TypeCache.Converter.RegisterTypeConverter(typeof(GeographyPoint), TypeConverters.CreateGeographyPoint);
		session.TypeCache.Converter.RegisterTypeConverter(typeof(GeometryPoint), TypeConverters.CreateGeometryPoint);
	}

	public new IEdmModel Model
	{
		get => base.Model as IEdmModel;
		set
		{
			base.Model = value;
			_metadata = null;
		}
	}

	public override string GetODataVersionString()
	{
		return ProtocolVersion switch
		{
			ODataProtocolVersion.V4 => "V4",
			_ => throw new InvalidOperationException($"Unsupported OData protocol version: \"{ProtocolVersion}\""),
		};
	}

	public override IMetadata GetMetadata()
	{
		// TODO: Should use a MetadataFactory here 
		return _metadata ??= new MetadataCache(new Metadata(Model, _session.Settings.NameMatchResolver, _session.Settings.IgnoreUnmappedProperties, _session.Settings.UnqualifiedNameCall));
	}

	public override ICommandFormatter GetCommandFormatter()
	{
		return new CommandFormatter(_session);
	}

	public override IResponseReader GetResponseReader()
	{
		return new ResponseReader(_session, Model);
	}

	public override IRequestWriter GetRequestWriter(Lazy<IBatchWriter> deferredBatchWriter)
	{
		return new RequestWriter(_session, Model, deferredBatchWriter);
	}

	public override IBatchWriter GetBatchWriter(IDictionary<object, IDictionary<string, object>> batchEntries)
	{
		return new BatchWriter(_session, batchEntries);
	}
}
