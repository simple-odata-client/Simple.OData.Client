using System.Collections.Generic;

namespace Simple.OData.Client.V4.Adapter.Extensions;

public interface IExtendedODataClient
{
	/// <summary>
	/// Returns an instance of a fluent extended OData client for the specified collection.
	/// </summary>
	/// <param name="collectionName">Name of the collection.</param>
	/// <returns>The fluent extended OData client instance.</returns>
	IExtendedBoundClient<IDictionary<string, object>> For(string collectionName);
	/// <summary>
	/// Returns an instance of a fluent extended OData client for the specified collection.
	/// </summary>
	/// <param name="expression">Collection expression.</param>
	/// <returns>The fluent extended OData client instance.</returns>
	IExtendedBoundClient<ODataEntry> For(ODataExpression expression);
	/// <summary>
	/// Returns an instance of a fluent extended OData client for the specified collection.
	/// </summary>
	/// <param name="collectionName">Name of the collection.</param>
	/// <returns>The fluent extended OData client instance.</returns>
	IExtendedBoundClient<T> For<T>(string? collectionName = null) where T : class;
}
