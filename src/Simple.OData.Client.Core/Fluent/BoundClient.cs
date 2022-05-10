using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Simple.OData.Client;

// ALthough BoundClient is never instantiated directly (only via IBoundClient interface)
// it's declared as public in order to resolve problem when it is used with dynamic C#
// For the same reason FluentCommand is also declared as public
// More: http://bloggingabout.net/blogs/vagif/archive/2013/08/05/we-need-better-interoperability-between-dynamic-and-statically-compiled-c.aspx

/// <summary>
/// Provides access to OData operations in a fluent style.
/// </summary>
/// <typeparam name="T">The entry type.</typeparam>
public partial class BoundClient<T> : FluentClientBase<T, IBoundClient<T>>, IBoundClient<T>
	where T : class
{
	internal BoundClient(
		ODataClient client,
		Session session,
		FluentCommand? parentCommand = null,
		FluentCommand? command = null,
		bool dynamicResults = false)
		: base(client, session, parentCommand, command, dynamicResults)
	{
	}

	public IBoundClient<T> For(string? collectionName = null)
	{
		Command.For(collectionName ?? _session.TypeCache.GetMappedName(typeof(T)));
		return this;
	}

	public IBoundClient<ODataEntry> For(ODataExpression expression)
	{
		Command.For(expression.Reference);
		return CreateClientForODataEntry();
	}

	public IBoundClient<IDictionary<string, object>> As(string derivedCollectionName)
	{
		Command.As(derivedCollectionName);
		return new BoundClient<IDictionary<string, object>>(_client, _session, _parentCommand, Command, _dynamicResults);
	}

	public IBoundClient<T> Set(object value)
	{
		Command.Set(value);
		return this;
	}

	public IBoundClient<T> Set(IDictionary<string, object> value)
	{
		Command.Set(value);
		return this;
	}

	public IBoundClient<T> Set(params ODataExpression[] value)
	{
		Command.Set(value);
		return this;
	}

	public IBoundClient<T> Set(T entry)
	{
		Command.Set(entry);
		return this;
	}

	public IBoundClient<T> Set(T entry, params ODataExpression[] associationsToSetByValue)
	{
		throw new NotImplementedException();
	}

	public IBoundClient<T> Set(object value, IEnumerable<string> associationsToSetByValue)
	{
		throw new NotImplementedException();
	}

	public IBoundClient<T> Set(object value, params string[] associationsToSetByValue)
	{
		throw new NotImplementedException();
	}

	public IBoundClient<T> Set(object value, params ODataExpression[] associationsToSetByValue)
	{
		throw new NotImplementedException();
	}

	public IBoundClient<T> Set(object value, Expression<Func<T, object>> associationsToSetByValue)
	{
		throw new NotImplementedException();
	}

	public IBoundClient<T> Set(IDictionary<string, object> value, IEnumerable<string> associationsToSetByValue)
	{
		throw new NotImplementedException();
	}

	public IBoundClient<T> Set(IDictionary<string, object> value, params string[] associationsToSetByValue)
	{
		throw new NotImplementedException();
	}

	public IBoundClient<T> Set(T entry, Expression<Func<T, object>> associationsToSetByValue)
	{
		throw new NotImplementedException();
	}

	public IBoundClient<U> As<U>(string? derivedCollectionName = null)
	where U : class
	{
		Command.As(derivedCollectionName ?? typeof(U).Name);
		return new BoundClient<U>(_client, _session, _parentCommand, Command, _dynamicResults);
	}

	public IBoundClient<ODataEntry> As(ODataExpression expression)
	{
		Command.As(expression);
		return CreateClientForODataEntry();
	}

	public bool FilterIsKey => Command.Details.FilterIsKey;

	public IDictionary<string, object> FilterAsKey => Command.Details.FilterAsKey;

	public IRequestBuilder<T> BuildRequestFor()
	{
		return new RequestBuilder<T>(Command, _session, _client.BatchWriter);
	}

	private BoundClient<ODataEntry> CreateClientForODataEntry()
	{
		return new BoundClient<ODataEntry>(_client, _session, _parentCommand, Command, true); ;
	}
}
