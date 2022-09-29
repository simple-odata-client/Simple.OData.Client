using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Simple.OData.Client;

/// <summary>
/// Provides access to OData operations in a fluent style.
/// </summary>
/// <typeparam name="T">The entry type.</typeparam>
public partial class UnboundClient<T> : FluentClientBase<T, IUnboundClient<T>>, IUnboundClient<T>
	where T : class
{
	internal UnboundClient(
		ODataClient client,
		Session session,
		FluentCommand? command = null,
		bool dynamicResults = false)
		: base(client, session, null, command, dynamicResults)
	{
	}

	public IUnboundClient<T> Set(object value)
	{
		Command.Set(value);
		return this;
	}

	public IUnboundClient<T> Set(IDictionary<string, object> value)
	{
		Command.Set(value);
		return this;
	}

	public IUnboundClient<T> Set(params ODataExpression[] value)
	{
		Command.Set(value);
		return this;
	}

	public IUnboundClient<T> Set(T entry)
	{
		Command.Set(entry);
		return this;
	}

	public IUnboundClient<T> Set(T entry, params ODataExpression[] associationsToSetByValue)
	{
		throw new NotImplementedException();
	}

	public IUnboundClient<T> Set(object value, IEnumerable<string> associationsToSetByValue)
	{
		throw new NotImplementedException();
	}

	public IUnboundClient<T> Set(object value, params string[] associationsToSetByValue)
	{
		throw new NotImplementedException();
	}

	public IUnboundClient<T> Set(object value, params ODataExpression[] associationsToSetByValue)
	{
		throw new NotImplementedException();
	}

	public IUnboundClient<T> Set(object value, Expression<Func<T, object>> associationsToSetByValue)
	{
		throw new NotImplementedException();
	}

	public IUnboundClient<T> Set(IDictionary<string, object> value, IEnumerable<string> associationsToSetByValue)
	{
		throw new NotImplementedException();
	}

	public IUnboundClient<T> Set(IDictionary<string, object> value, params string[] associationsToSetByValue)
	{
		throw new NotImplementedException();
	}

	public IUnboundClient<T> Set(T entry, Expression<Func<T, object>> associationsToSetByValue)
	{
		throw new NotImplementedException();
	}

	public IUnboundClient<IDictionary<string, object>> As(string derivedCollectionName)
	{
		Command.As(derivedCollectionName);
		return new UnboundClient<IDictionary<string, object>>(_client, _session, Command, _dynamicResults);
	}

	public IUnboundClient<U> As<U>(string? derivedCollectionName = null)
	where U : class
	{
		Command.As(derivedCollectionName ?? typeof(U).Name);
		return new UnboundClient<U>(_client, _session, Command, _dynamicResults);
	}

	public IUnboundClient<ODataEntry> As(ODataExpression expression)
	{
		Command.As(expression);
		return CreateClientForODataEntry();
	}

	private UnboundClient<ODataEntry> CreateClientForODataEntry()
	{
		return new UnboundClient<ODataEntry>(_client, _session, Command, true); ;
	}
}
