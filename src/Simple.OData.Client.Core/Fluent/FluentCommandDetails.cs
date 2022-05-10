using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Simple.OData.Client;

internal class FluentCommandDetails
{
	public FluentCommandDetails? Parent { get; private set; }
	public string? CollectionName { get; set; }
	public ODataExpression CollectionExpression { get; set; }
	public string? DerivedCollectionName { get; set; }
	public ODataExpression DerivedCollectionExpression { get; set; }
	public string DynamicPropertiesContainerName { get; set; }
	public string FunctionName { get; set; }
	public string ActionName { get; set; }
	public bool IsAlternateKey { get; set; }
	public IList<object>? KeyValues { get; set; }
	public IDictionary<string, object>? NamedKeyValues { get; set; }
	public object EntryValue { get; set; }
	public IDictionary<string, object>? EntryData { get; set; }
	public string? Filter { get; set; }
	public ODataExpression FilterExpression { get; set; }
	public string Search { get; set; }
	public long SkipCount { get; set; }
	public long TopCount { get; set; }
	public List<KeyValuePair<ODataExpandAssociation, ODataExpandOptions>> ExpandAssociations { get; private set; }
	public List<string> SelectColumns { get; private set; }
	public List<KeyValuePair<string, bool>> OrderbyColumns { get; private set; }
	public bool ComputeCount { get; set; }
	public bool IncludeCount { get; set; }
	public string? LinkName { get; set; }
	public ODataExpression LinkExpression { get; set; }
	public string? QueryOptions { get; set; }
	public ODataExpression QueryOptionsExpression { get; set; }
	public IDictionary<string, object> QueryOptionsKeyValues { get; set; }
	public string MediaName { get; set; }
	public IEnumerable<string> MediaProperties { get; set; }
	public ConcurrentDictionary<object, IDictionary<string, object>>? BatchEntries { get; set; }
	public IDictionary<string, string> Headers { get; set; }
	public IDictionary<string, object> Extensions { get; set; }

	public FluentCommandDetails(
		FluentCommandDetails? parent,
		ConcurrentDictionary<object, IDictionary<string, object>>? batchEntries)
	{
		Parent = parent;
		SkipCount = -1;
		TopCount = -1;
		ExpandAssociations = new List<KeyValuePair<ODataExpandAssociation, ODataExpandOptions>>();
		SelectColumns = new List<string>();
		OrderbyColumns = new List<KeyValuePair<string, bool>>();
		MediaProperties = new List<string>();
		BatchEntries = batchEntries;
		Headers = new Dictionary<string, string>();
		Extensions = new Dictionary<string, object>();
	}

	public FluentCommandDetails(FluentCommandDetails details)
	{
		Parent = details.Parent;
		CollectionName = details.CollectionName;
		CollectionExpression = details.CollectionExpression;
		DerivedCollectionName = details.DerivedCollectionName;
		DerivedCollectionExpression = details.DerivedCollectionExpression;
		DynamicPropertiesContainerName = details.DynamicPropertiesContainerName;
		FunctionName = details.FunctionName;
		ActionName = details.ActionName;
		IsAlternateKey = details.IsAlternateKey;
		KeyValues = details.KeyValues;
		NamedKeyValues = details.NamedKeyValues;
		EntryValue = details.EntryValue;
		EntryData = details.EntryData;
		Filter = details.Filter;
		FilterExpression = details.FilterExpression;
		Search = details.Search;
		SkipCount = details.SkipCount;
		TopCount = details.TopCount;
		ExpandAssociations = details.ExpandAssociations;
		SelectColumns = details.SelectColumns;
		OrderbyColumns = details.OrderbyColumns;
		ComputeCount = details.ComputeCount;
		IncludeCount = details.IncludeCount;
		LinkName = details.LinkName;
		LinkExpression = details.LinkExpression;
		MediaName = details.MediaName;
		MediaProperties = details.MediaProperties;
		QueryOptions = details.QueryOptions;
		QueryOptionsKeyValues = details.QueryOptionsKeyValues;
		QueryOptionsExpression = details.QueryOptionsExpression;
		BatchEntries = details.BatchEntries;
		Headers = details.Headers;
		Extensions = details.Extensions;
	}

	public bool HasKey => KeyValues != null && KeyValues.Count > 0 || NamedKeyValues != null && NamedKeyValues.Count > 0;

	public bool HasFilter => !string.IsNullOrEmpty(Filter) || FilterExpression is not null;

	public bool HasSearch => !string.IsNullOrEmpty(Search);

	public bool HasFunction => !string.IsNullOrEmpty(FunctionName);

	public bool HasAction => !string.IsNullOrEmpty(ActionName);

	public bool FilterIsKey => NamedKeyValues != null;

	public IDictionary<string, object> FilterAsKey => NamedKeyValues;
}
