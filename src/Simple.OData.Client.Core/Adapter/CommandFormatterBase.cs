using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Simple.OData.Client;

public abstract class CommandFormatterBase : ICommandFormatter
{
	protected readonly ISession _session;

	protected CommandFormatterBase(ISession session)
	{
		_session = session;
	}

	public abstract string ConvertValueToUriLiteral(object value, bool escapeString);

	public abstract FunctionFormat FunctionFormat { get; }

	protected ITypeCache TypeCache => _session.TypeCache;

	public string FormatCommand(ResolvedCommand command)
	{
		if (command.Details.HasFunction && command.Details.HasAction)
		{
			throw new InvalidOperationException("OData function and action may not be combined.");
		}

		var commandText = string.Empty;
		if (!string.IsNullOrEmpty(command.Details.CollectionName))
		{
			commandText += _session.Metadata.GetEntityCollectionExactName(command.Details.CollectionName);
		}
		else if (!string.IsNullOrEmpty(command.Details.LinkName))
		{
			var parent = new FluentCommand(command.Details.Parent).Resolve(_session);
			commandText += $"{FormatCommand(parent)}/{_session.Metadata.GetNavigationPropertyExactName(parent.EntityCollection.Name, command.Details.LinkName)}";
		}

		if (command.Details.HasKey)
		{
			commandText += ConvertKeyValuesToUriLiteral(command.KeyValues, !command.Details.IsAlternateKey);
		}

		var collectionValues = new List<string>();
		if (!string.IsNullOrEmpty(command.Details.MediaName))
		{
			commandText += "/" + (command.Details.MediaName == FluentCommand.MediaEntityLiteral
				? ODataLiteral.Value
				: command.Details.MediaName);
		}
		else
		{
			if (!string.IsNullOrEmpty(command.Details.FunctionName) || !string.IsNullOrEmpty(command.Details.ActionName))
			{
				if (!string.IsNullOrEmpty(command.Details.CollectionName) || !string.IsNullOrEmpty(command.Details.LinkName))
				{
					commandText += "/";
				}

				if (!string.IsNullOrEmpty(command.Details.FunctionName))
				{
					commandText += _session.Metadata.GetFunctionFullName(command.Details.FunctionName);
				}
				else
				{
					commandText += _session.Metadata.GetActionFullName(command.Details.ActionName);
				}
			}

			if (!string.IsNullOrEmpty(command.Details.FunctionName) && FunctionFormat == FunctionFormat.Key)
			{
				commandText += ConvertKeyValuesToUriLiteralExtractCollections(command.CommandData, collectionValues, false);
			}

			if (!string.IsNullOrEmpty(command.Details.DerivedCollectionName))
			{
				commandText += "/" + _session.Metadata.GetQualifiedTypeName(command.Details.DerivedCollectionName);
			}
		}

		commandText += FormatClauses(command, collectionValues);
		return commandText;
	}

	public string FormatNavigationPath(EntityCollection entityCollection, string path)
	{
		var items = path.Split('/');
		var associationName = _session.Metadata.HasNavigationProperty(entityCollection.Name, items.First())
			? _session.Metadata.GetNavigationPropertyExactName(entityCollection.Name, items.First())
			: items.First();
		var text = associationName;
		if (items.Length == 1)
		{
			return text;
		}
		else
		{
			path = path.Substring(items.First().Length + 1);

			entityCollection = _session.Metadata.GetEntityCollection(
				_session.Metadata.GetNavigationPropertyPartnerTypeName(entityCollection.Name, associationName));

			return $"{text}/{FormatNavigationPath(entityCollection, path)}";
		}
	}

	public string ConvertKeyValuesToUriLiteral(IDictionary<string, object> key, bool skipKeyNameForSingleValue)
	{
		var formattedKeyValues = key.Count == 1 && skipKeyNameForSingleValue
			? string.Join(",", key.Select(x => ConvertValueToUriLiteral(x.Value, true)))
			: string.Join(",",
				key.Select(x => $"{x.Key}={ConvertValueToUriLiteral(x.Value, true)}"));
		return "(" + formattedKeyValues + ")";
	}

	public string ConvertKeyValuesToUriLiteralExtractCollections(IDictionary<string, object> data, IList<string> collectionValues, bool skipKeyNameForSingleValue)
	{
		var escapedData = new Dictionary<string, object>();
		var colIndex = 0;
		foreach (var item in data)
		{
			object itemValue;
			if (item.Value != null)
			{
				var itemType = item.Value.GetType();
				if (itemType.IsArray || TypeCache.IsGeneric(itemType) && TypeCache.IsTypeAssignableFrom(typeof(System.Collections.IEnumerable), itemType))
				{
					var itemAlias = $"@p{++colIndex}";
					var collection = item.Value as System.Collections.IEnumerable;
					var escapedCollection = new List<object>();
					foreach (var o in collection)
					{
						escapedCollection.Add(ConvertValueToUriLiteral(o, true));
					}

					collectionValues.Add(string.Format(CultureInfo.InvariantCulture, "{0}=[" + string.Join(",", escapedCollection) + "]", itemAlias));
					itemValue = itemAlias;
				}
				else
				{
					itemValue = ConvertValueToUriLiteral(item.Value, true);
				}
			}
			else
			{
				itemValue = ConvertValueToUriLiteral(item.Value, true);
			}

			escapedData.Add(item.Key, itemValue);
		}

		var formattedKeyValues = escapedData.Count == 1 && skipKeyNameForSingleValue
			? string.Join(",", escapedData)
			: string.Join(",",
				escapedData.Select(x => $"{x.Key}={x.Value}"));
		return "(" + formattedKeyValues + ")";
	}

	protected abstract void FormatExpandSelectOrderby(IList<string> commandClauses, EntityCollection resultCollection, ResolvedCommand command);

	protected abstract void FormatInlineCount(IList<string> commandClauses);

	protected abstract void FormatExtensions(IList<string> commandClauses, ResolvedCommand command);

	private const string ReservedUriCharacters = @"!*'();:@&=+$,/?#[] ";

	protected static string EscapeUnescapedString(string text)
	{
		return text.ToCharArray().Intersect(ReservedUriCharacters.ToCharArray()).Any()
			? Uri.EscapeDataString(text)
			: text;
	}

	private string FormatClauses(ResolvedCommand command, IList<string>? queryClauses = null)
	{
		var text = string.Empty;
		queryClauses ??= new List<string>();
		var aggregateClauses = new List<string>();

		if (command.CommandData.Any() && !string.IsNullOrEmpty(command.Details.FunctionName) &&
			FunctionFormat == FunctionFormat.Query)
		{
			queryClauses.Add(string.Join("&", command.CommandData.Select(x => $"{x.Key}={ConvertValueToUriLiteral(x.Value, true)}")));
		}

		if (command.Details.Filter != null)
		{
			queryClauses.Add($"{ODataLiteral.Filter}={EscapeUnescapedString(command.Details.Filter)}");
		}

		if (command.Details.Search != null)
		{
			queryClauses.Add($"{ODataLiteral.Search}={EscapeUnescapedString(command.Details.Search)}");
		}

		if (command.Details.QueryOptions != null)
		{
			queryClauses.Add(command.Details.QueryOptions);
		}

		var details = command.Details;
		if (details.QueryOptionsExpression is not null)
		{
			queryClauses.Add(details.QueryOptionsExpression.Format(new ExpressionContext(_session, true)));
		}

		if (command.Details.QueryOptionsKeyValues != null)
		{
			foreach (var kv in command.Details.QueryOptionsKeyValues)
			{
				queryClauses.Add($"{kv.Key}={ODataExpression.FromValue(kv.Value).Format(new ExpressionContext(_session))}");
			}
		}

		if (command.Details.SkipCount >= 0)
		{
			queryClauses.Add($"{ODataLiteral.Skip}={command.Details.SkipCount}");
		}

		if (command.Details.TopCount >= 0)
		{
			queryClauses.Add($"{ODataLiteral.Top}={command.Details.TopCount}");
		}

		if (command.Details.Extensions.Any())
		{
			FormatExtensions(queryClauses, command);
		}

		EntityCollection resultCollection;
		if (command.Details.HasFunction)
		{
			resultCollection = _session.Adapter.GetMetadata().GetFunctionReturnCollection(command.Details.FunctionName);
		}
		else
		{
			resultCollection = command.Details.HasAction
				? _session.Adapter.GetMetadata().GetActionReturnCollection(command.Details.ActionName)
				: command.EntityCollection;
		}

		if (resultCollection != null)
		{
			FormatExpandSelectOrderby(queryClauses, resultCollection, command);
		}

		if (command.Details.IncludeCount)
		{
			FormatInlineCount(queryClauses);
		}

		if (command.Details.ComputeCount)
		{
			aggregateClauses.Add(ODataLiteral.Count);
		}

		if (aggregateClauses.Any())
		{
			text += "/" + string.Join("/", aggregateClauses);
		}

		if (queryClauses.Any())
		{
			text += "?" + string.Join("&", queryClauses);
		}

		return text;
	}

	protected string FormatExpandItem(KeyValuePair<string, ODataExpandOptions> pathWithOptions, EntityCollection entityCollection)
	{
		return FormatNavigationPath(entityCollection, pathWithOptions.Key);
	}

	protected string FormatSelectItem(string path, EntityCollection entityCollection)
	{
		var items = path.Split('/');
		if (items.Length == 1)
		{
			return _session.Metadata.HasStructuralProperty(entityCollection.Name, path)
				? _session.Metadata.GetStructuralPropertyExactName(entityCollection.Name, path)
				: _session.Metadata.HasNavigationProperty(entityCollection.Name, path)
				? _session.Metadata.GetNavigationPropertyExactName(entityCollection.Name, path)
				: path;
		}
		else
		{
			var associationName = _session.Metadata.GetNavigationPropertyExactName(entityCollection.Name, items.First());
			var text = associationName;
			path = path.Substring(items.First().Length + 1);
			entityCollection = _session.Metadata.GetEntityCollection(
				_session.Metadata.GetNavigationPropertyPartnerTypeName(entityCollection.Name, associationName));
			return $"{text}/{FormatSelectItem(path, entityCollection)}";
		}
	}

	protected string FormatOrderByItem(KeyValuePair<string, bool> pathWithOrder, EntityCollection entityCollection)
	{
		var items = pathWithOrder.Key.Split('/');
		if (items.Length == 1)
		{
			var clause = _session.Metadata.HasStructuralProperty(entityCollection.Name, pathWithOrder.Key)
				? _session.Metadata.GetStructuralPropertyExactName(entityCollection.Name, pathWithOrder.Key)
				: _session.Metadata.HasNavigationProperty(entityCollection.Name, pathWithOrder.Key)
				? _session.Metadata.GetNavigationPropertyExactName(entityCollection.Name, pathWithOrder.Key)
				: pathWithOrder.Key;
			if (pathWithOrder.Value)
			{
				clause += " desc";
			}

			return clause;
		}
		else
		{
			if (_session.Metadata.HasNavigationProperty(entityCollection.Name, items[0]))
			{
				var associationName = _session.Metadata.GetNavigationPropertyExactName(entityCollection.Name, items[0]);
				var text = associationName;
				var item = pathWithOrder.Key.Substring(items.First().Length + 1);
				entityCollection = _session.Metadata.GetEntityCollection(
					_session.Metadata.GetNavigationPropertyPartnerTypeName(entityCollection.Name, associationName));
				return $"{text}/{FormatOrderByItem(new KeyValuePair<string, bool>(item, pathWithOrder.Value), entityCollection)}";
			}
			else if (_session.Metadata.HasStructuralProperty(entityCollection.Name, items[0]))
			{
				var clause = _session.Metadata.GetStructuralPropertyPath(entityCollection.Name, items);
				if (pathWithOrder.Value)
				{
					clause += " desc";
				}

				return clause;
			}
			else
			{
				throw new UnresolvableObjectException(items[0], $"Property path [{items[0]}] not found");
			}
		}
	}

	protected static void FormatClause<T>(IList<string> commandClauses, EntityCollection entityCollection,
		IList<T> clauses, string clauseLiteral, Func<T, EntityCollection, string> formatItem)
	{
		if (clauses.Any())
		{
			commandClauses.Add($"{clauseLiteral}={string.Join(",", clauses.Select(x => formatItem(x, entityCollection)))}");
		}
	}

	protected static IEnumerable<KeyValuePair<string, ODataExpandOptions>> FlatExpandAssociations(
		IEnumerable<KeyValuePair<ODataExpandAssociation, ODataExpandOptions>> associations)
	{
		return associations
			.SelectMany(a => a.Key.ExpandAssociations.Any()
				? FlatExpandAssociations(a.Key.ExpandAssociations.Select(x =>
						new KeyValuePair<ODataExpandAssociation, ODataExpandOptions>(x,
							ODataExpandOptions.ByValue())))
					.Select(x => new KeyValuePair<string, ODataExpandOptions>(a.Key.Name + "/" + x.Key, x.Value))
				: new[] { new KeyValuePair<string, ODataExpandOptions>(a.Key.Name, a.Value) })
			.ToList();
	}
}
