using System;
using System.Collections.Generic;
using System.Linq;

using Simple.OData.Client.Extensions;

namespace Simple.OData.Client;

public class ResolvedCommand
{
	private readonly ISession _sesson;

	internal ResolvedCommand(FluentCommand command, ISession session)
	{
		_sesson = session;
		Details = new FluentCommandDetails(command.Details);

		ResolveCollectionName(command.Details);
		ResolveDerivedCollectionName(command.Details);
		ResolveLinkName(command.Details);
		ResolveEntityCollection(command.Details);
		ResolveKeys(command.Details);
		ResolveFilter(command.Details);
		ResolveEntryData(command.Details);
	}

	internal FluentCommandDetails Details { get; private set; }

	internal EntityCollection? EntityCollection { get; private set; }

	public string QualifiedEntityCollectionName
	{
		get
		{
			var entityCollection = EntityCollection;
			return entityCollection.BaseEntityCollection == null
				? entityCollection.Name
				: $"{entityCollection.BaseEntityCollection.Name}/{_sesson.Metadata.GetQualifiedTypeName(entityCollection.Name)}";
		}
	}

	public string DynamicPropertiesContainerName => Details.DynamicPropertiesContainerName;

	private void ResolveCollectionName(FluentCommandDetails details)
	{
		if (Details.CollectionName == null && details.CollectionExpression is not null)
		{
			var collectionName = details.CollectionExpression.AsString(_sesson);
			var items = collectionName.Split('/');
			if (items.Length > 1)
			{
				Details.CollectionName = items[0];
				Details.DerivedCollectionName = items[1];
			}
			else
			{
				Details.CollectionName = collectionName;
			}
		}
	}

	private void ResolveDerivedCollectionName(FluentCommandDetails details)
	{
		if (Details.DerivedCollectionName == null && details.DerivedCollectionExpression is not null)
		{
			var derivedCollectionName = details.DerivedCollectionExpression.AsString(_sesson);
			Details.DerivedCollectionName = derivedCollectionName;
		}
	}

	private void ResolveLinkName(FluentCommandDetails details)
	{
		if (Details.LinkName == null && details.LinkExpression is not null)
		{
			Details.LinkName = details.LinkExpression.AsString(_sesson);
		}
	}

	private void ResolveEntityCollection(FluentCommandDetails details)
	{
		if (string.IsNullOrEmpty(Details.CollectionName) && string.IsNullOrEmpty(Details.LinkName))
		{
			EntityCollection = null;
		}
		else
		{
			EntityCollection entityCollection;
			if (!string.IsNullOrEmpty(Details.LinkName))
			{
				var parent = new FluentCommand(Details.Parent).Resolve(_sesson);
				var collectionName = _sesson.Metadata.GetNavigationPropertyPartnerTypeName(
					parent.EntityCollection.Name, Details.LinkName);
				entityCollection = _sesson.Metadata.GetEntityCollection(collectionName);
			}
			else
			{
				entityCollection = _sesson.Metadata.GetEntityCollection(Details.CollectionName);
			}

			EntityCollection =
				string.IsNullOrEmpty(Details.DerivedCollectionName)
				? entityCollection
				: _sesson.Metadata.GetDerivedEntityCollection(entityCollection, Details.DerivedCollectionName);
		}
	}

	private void ResolveKeys(FluentCommandDetails details)
	{
		var namedKeyValues =
			details.KeyValues != null && details.KeyValues.Count == 1 &&
			_sesson.TypeCache.IsAnonymousType(details.KeyValues.First().GetType())
				? _sesson.TypeCache.ToDictionary(details.KeyValues.First())
				: details.NamedKeyValues;

		if (namedKeyValues != null)
		{
			if (NamedKeyValuesMatchAnyKey(namedKeyValues, out var matchingKey, out var isAlternateKey))
			{
				Details.NamedKeyValues = matchingKey.ToDictionary();
				Details.IsAlternateKey = isAlternateKey;
			}
			else
			{
				Details.NamedKeyValues = TryExtractKeyFromNamedValues(namedKeyValues, out var containedKey)
					? containedKey.ToDictionary()
					: null;
			}

			Details.KeyValues = null;
		}
	}

	private void ResolveFilter(FluentCommandDetails details)
	{
		if (Details.Filter == null && details.FilterExpression is not null)
		{
			Details.NamedKeyValues = TryInterpretFilterExpressionAsKey(details.FilterExpression, out var isAlternateKey);
			Details.IsAlternateKey = isAlternateKey;

			if (Details.NamedKeyValues == null)
			{
				var entityCollection = EntityCollection;
				if (details.HasFunction)
				{
					var collection = _sesson.Metadata.GetFunctionReturnCollection(details.FunctionName);
					if (collection != null)
					{
						entityCollection = collection;
					}
				}

				Details.Filter = details.FilterExpression.Format(
					new ExpressionContext(_sesson, entityCollection, null, DynamicPropertiesContainerName));
			}
			else
			{
				Details.KeyValues = null;
				Details.TopCount = -1;
			}

			if (details.FilterExpression.HasTypeConstraint(details.DerivedCollectionName))
			{
				Details.DerivedCollectionName = null;
			}
		}
	}

	private void ResolveEntryData(FluentCommandDetails details)
	{
		if (details.EntryValue != null)
		{
			Details.EntryData = _sesson.TypeCache.ToDictionary(details.EntryValue);
			Details.BatchEntries?.GetOrAdd(details.EntryValue, Details.EntryData);
		}
	}

	public IDictionary<string, object> FilterAsKey => Details.NamedKeyValues;

	public ResolvedCommand WithCount()
	{
		Details.IncludeCount = true;
		return this;
	}

	public override string ToString()
	{
		return Format();
	}

	public string Format()
	{
		return _sesson.Adapter.GetCommandFormatter().FormatCommand(this);
	}

	internal IDictionary<string, object> KeyValues
	{
		get
		{
			if (!Details.HasKey)
			{
				return null;
			}

			var keyNames = _sesson.Metadata.GetDeclaredKeyPropertyNames(EntityCollection.Name).ToList();
			var namedKeyValues = Details.KeyValues?.Zip(
				 keyNames,
				 (keyValue, keyName) => new KeyValuePair<string, object>(keyName, keyValue))
				??
				Details.NamedKeyValues;
			return namedKeyValues?.ToDictionary(
				x => x.Key,
				x => x.Value is ODataExpression oDataExpression ? oDataExpression.Value : x.Value);
		}
	}

	internal IDictionary<string, object> CommandData
	{
		get
		{
			if (Details.EntryData == null)
			{
				return new Dictionary<string, object>();
			}

			if (string.IsNullOrEmpty(Details.DynamicPropertiesContainerName))
			{
				return Details.EntryData;
			}

			var entryData = new Dictionary<string, object>();
			foreach (var key in Details.EntryData.Keys.Where(x =>
				!string.Equals(x, Details.DynamicPropertiesContainerName, StringComparison.OrdinalIgnoreCase)))
			{
				entryData.Add(key, Details.EntryData[key]);
			}

			if (Details.EntryData.TryGetValue(Details.DynamicPropertiesContainerName, out var dynamicProperties) && dynamicProperties != null)
			{
				if (dynamicProperties is IDictionary<string, object> kv)
				{
					foreach (var key in kv.Keys)
					{
						entryData.Add(key, kv[key]);
					}
				}
				else
				{
					throw new InvalidOperationException($"Property {Details.DynamicPropertiesContainerName} must implement IDictionary<string,object> interface");
				}
			}

			return entryData;
		}
	}

	private IDictionary<string, object> TryInterpretFilterExpressionAsKey(ODataExpression expression, out bool isAlternateKey)
	{
		isAlternateKey = false;
		var ok = false;

		IDictionary<string, object> namedKeyValues = new Dictionary<string, object>();
		if (expression is not null)
		{
			ok = expression.ExtractLookupColumns(namedKeyValues);
		}

		if (!ok)
		{
			return null;
		}

		if (NamedKeyValuesMatchAnyKey(namedKeyValues, out var matchingNamedKeyValues, out isAlternateKey))
		{
			return matchingNamedKeyValues.ToIDictionary();
		}

		return null;
	}

	private bool NamedKeyValuesMatchAnyKey(IDictionary<string, object> namedKeyValues, out IEnumerable<KeyValuePair<string, object>> matchingNamedKeyValues, out bool isAlternateKey)
	{
		isAlternateKey = false;

		if (NamedKeyValuesMatchPrimaryKey(namedKeyValues, out matchingNamedKeyValues))
		{
			return true;
		}

		if (NamedKeyValuesMatchAlternateKey(namedKeyValues, out matchingNamedKeyValues))
		{
			isAlternateKey = true;
			return true;
		}

		return false;
	}

	private bool NamedKeyValuesMatchPrimaryKey(IDictionary<string, object> namedKeyValues, out IEnumerable<KeyValuePair<string, object>> matchingNamedKeyValues)
	{
		var keyNames = _sesson.Metadata.GetDeclaredKeyPropertyNames(EntityCollection.Name).ToList();

		return Utils.NamedKeyValuesMatchKeyNames(namedKeyValues, _sesson.Settings.NameMatchResolver, keyNames, out matchingNamedKeyValues);
	}

	private bool NamedKeyValuesMatchAlternateKey(IDictionary<string, object> namedKeyValues, out IEnumerable<KeyValuePair<string, object>>? alternateKeyNamedValues)
	{
		alternateKeyNamedValues = null;

		var alternateKeys = _sesson.Metadata.GetAlternateKeyPropertyNames(EntityCollection.Name).ToList();

		foreach (var alternateKey in alternateKeys)
		{
			if (Utils.NamedKeyValuesMatchKeyNames(namedKeyValues, _sesson.Settings.NameMatchResolver, alternateKey, out alternateKeyNamedValues))
			{
				return true;
			}
		}

		return false;
	}

	private bool TryExtractKeyFromNamedValues(IDictionary<string, object> namedValues, out IEnumerable<KeyValuePair<string, object>> matchingNamedKeyValues)
	{
		return Utils.NamedKeyValuesContainKeyNames(namedValues,
			_sesson.Settings.NameMatchResolver,
			_sesson.Metadata.GetDeclaredKeyPropertyNames(EntityCollection.Name),
			out matchingNamedKeyValues);
	}
}
