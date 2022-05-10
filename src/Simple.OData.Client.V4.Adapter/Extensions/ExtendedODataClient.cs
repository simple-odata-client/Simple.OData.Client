using System.Collections.Generic;

namespace Simple.OData.Client.V4.Adapter.Extensions;

/// <summary>
/// <inheritdoc cref="IExtendedODataClient"/>
/// </summary>
public class ExtendedODataClient : IExtendedODataClient
{
	private readonly ODataClient _baseClient;

	public ExtendedODataClient(ODataClient baseClient)
	{
		_baseClient = baseClient;
	}

	public IExtendedBoundClient<IDictionary<string, object>> For(string collectionName)
	{
		var client = new ExtendedBoundClient<IDictionary<string, object>>(_baseClient, _baseClient.Session);
		client.For(collectionName);
		_baseClient.Session.Settings.IgnoreUnmappedProperties = true;
		_baseClient.Session.Settings.ReadUntypedAsString = false;
		return client;
	}

	public IExtendedBoundClient<ODataEntry> For(ODataExpression expression)
	{
		var client = new ExtendedBoundClient<ODataEntry>(_baseClient, _baseClient.Session, true);
		client.For(expression);
		_baseClient.Session.Settings.IgnoreUnmappedProperties = true;
		_baseClient.Session.Settings.ReadUntypedAsString = false;
		return client;
	}

	public IExtendedBoundClient<T> For<T>(string? collectionName = null) where T : class
	{
		var client = new ExtendedBoundClient<T>(_baseClient, _baseClient.Session);
		client.For(collectionName);
		return client;
	}
}
