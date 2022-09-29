using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Simple.OData.Client;

/// <summary>
/// Provides access to unbound OData operations in a fluent style.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public interface IUnboundClient<T> : IFluentClient<T, IUnboundClient<T>>
	where T : class
{
	/// <summary>
	/// Casts the collection of base entities as the collection of derived ones.
	/// </summary>
	/// <param name="derivedCollectionName">Name of the derived collection.</param>
	/// <returns>Self.</returns>
	IUnboundClient<IDictionary<string, object>> As(string derivedCollectionName);

	/// <summary>
	/// Casts the collection of base entities as the collection of derived ones.
	/// </summary>
	/// <param name="derivedCollectionName">Name of the derived collection.</param>
	/// <returns>Self.</returns>
	IUnboundClient<U> As<U>(string? derivedCollectionName = null) where U : class;

	/// <summary>
	/// Casts the collection of base entities as the collection of derived ones.
	/// </summary>
	/// <param name="expression">The expression for the derived collection.</param>
	/// <returns>Self.</returns>
	IUnboundClient<ODataEntry> As(ODataExpression expression);

	/// <summary>
	/// Sets the specified entry value for update.
	/// </summary>
	/// <param name="value">The value to update the entry with.</param>
	/// <returns>Self.</returns>
	IUnboundClient<T> Set(object value);

	/// <summary>
	/// Sets the specified entry value for update.
	/// </summary>
	/// <param name="value">The value to update the entry with.</param>
	/// <returns>Self.</returns>
	IUnboundClient<T> Set(IDictionary<string, object> value);

	/// <summary>
	/// Sets the specified entry value for update.
	/// </summary>
	/// <param name="entry">The entry with the updated value.</param>
	/// <returns>Self.</returns>
	IUnboundClient<T> Set(T entry);

	/// <summary>
	/// Sets the specified entry value for update.
	/// </summary>
	/// <param name="value">The value to update the entry with.</param>
	/// <returns>Self.</returns>
	IUnboundClient<T> Set(params ODataExpression[] value);

	/// <summary>
	/// Sets the specified entry value for update.
	/// </summary>
	/// <param name="value">The value to update the entry with.</param>
	/// <param name="associationsToSetByValue">The list of associations to be passed by value for deep insert/update.</param>
	/// <returns>Self.</returns>
	IUnboundClient<T> Set(object value, IEnumerable<string> associationsToSetByValue);

	/// <summary>
	/// Sets the specified entry value for update.
	/// </summary>
	/// <param name="value">The value to update the entry with.</param>
	/// <param name="associationsToSetByValue">The list of associations to be passed by value for deep insert/update.</param>
	/// <returns>Self.</returns>
	IUnboundClient<T> Set(object value, params string[] associationsToSetByValue);

	/// <summary>
	/// Sets the specified entry value for update.
	/// </summary>
	/// <param name="value">The value to update the entry with.</param>
	/// <param name="associationsToSetByValue">The list of associations to be passed by value for deep insert/update.</param>
	/// <returns>Self.</returns>
	IUnboundClient<T> Set(object value, params ODataExpression[] associationsToSetByValue);

	/// <summary>
	/// Sets the specified entry value for update.
	/// </summary>
	/// <param name="value">The value to update the entry with.</param>
	/// <param name="associationsToSetByValue">The list of associations to be passed by value for deep insert/update.</param>
	/// <returns>Self.</returns>
	IUnboundClient<T> Set(object value, Expression<Func<T, object>> associationsToSetByValue);

	/// <summary>
	/// Sets the specified entry value for update.
	/// </summary>
	/// <param name="value">The value to update the entry with.</param>
	/// <param name="associationsToSetByValue">The list of associations to be passed by value for deep insert/update.</param>
	/// <returns>Self.</returns>
	IUnboundClient<T> Set(IDictionary<string, object> value, IEnumerable<string> associationsToSetByValue);
	/// <summary>
	/// Sets the specified entry value for update.
	/// </summary>
	/// <param name="value">The value to update the entry with.</param>
	/// <param name="associationsToSetByValue">The list of associations to be passed by value for deep insert/update.</param>
	/// <returns>Self.</returns>
	IUnboundClient<T> Set(IDictionary<string, object> value, params string[] associationsToSetByValue);

	/// <summary>
	/// Sets the specified entry value for update.
	/// </summary>
	/// <param name="entry">The entry with the updated value.</param>
	/// <param name="associationsToSetByValue">The list of associations to be passed by value for deep insert/update.</param>
	/// <returns>Self.</returns>
	IUnboundClient<T> Set(T entry, params ODataExpression[] associationsToSetByValue);

	/// <summary>
	/// Sets the specified entry value for update.
	/// </summary>
	/// <param name="entry">The entry with the updated value.</param>
	/// <param name="associationsToSetByValue">The list of associations to be passed by value for deep insert/update.</param>
	/// <returns>Self.</returns>
	IUnboundClient<T> Set(T entry, Expression<Func<T, object>> associationsToSetByValue);
}
