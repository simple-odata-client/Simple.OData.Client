using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Simple.OData.Client
{
    internal class FluentCommandDetails
    {
        public FluentCommandDetails Parent { get; private set; }
        public string CollectionName { get; set; }
        public ODataExpression CollectionExpression { get; set; }
        public string DerivedCollectionName { get; set; }
        public ODataExpression DerivedCollectionExpression { get; set; }
        public string DynamicPropertiesContainerName { get; set; }
        public string FunctionName { get; set; }
        public string ActionName { get; set; }
        public bool IsAlternateKey { get; set; }
        public IList<object> KeyValues { get; set; }
        public IDictionary<string, object> NamedKeyValues { get; set; }
        public object EntryValue { get; set; }
        public IDictionary<string, object> EntryData { get; set; }
        public string Filter { get; set; }
        public ODataExpression FilterExpression { get; set; }
        public string Search { get; set; }
        public long SkipCount { get; set; }
        public long TopCount { get; set; }
        public List<KeyValuePair<ODataExpandAssociation, ODataExpandOptions>> ExpandAssociations { get; private set; }
        public List<string> SelectColumns { get; private set; }
        public List<KeyValuePair<string, bool>> OrderbyColumns { get; private set; }
        public bool ComputeCount { get; set; }
        public bool IncludeCount { get; set; }
        public string LinkName { get; set; }
        public ODataExpression LinkExpression { get; set; }
        public string QueryOptions { get; set; }
        public ODataExpression QueryOptionsExpression { get; set; }
        public IDictionary<string, object> QueryOptionsKeyValues { get; set; }
        public string MediaName { get; set; }
        public IEnumerable<string> MediaProperties { get; set; }
        public ConcurrentDictionary<object, IDictionary<string, object>> BatchEntries { get; set; }
        public IDictionary<string, string> Headers { get; set; }
        public IDictionary<string, object> Extensions { get; set; }

        public FluentCommandDetails(FluentCommandDetails parent, ConcurrentDictionary<object, IDictionary<string, object>> batchEntries)
        {
            this.Parent = parent;
            this.SkipCount = -1;
            this.TopCount = -1;
            this.ExpandAssociations = new List<KeyValuePair<ODataExpandAssociation, ODataExpandOptions>>();
            this.SelectColumns = new List<string>();
            this.OrderbyColumns = new List<KeyValuePair<string, bool>>();
            this.MediaProperties = new List<string>();
            this.BatchEntries = batchEntries;
            this.Headers = new Dictionary<string, string>();
            this.Extensions = new Dictionary<string, object>();
        }

        public FluentCommandDetails(FluentCommandDetails details)
        {
            this.Parent = details.Parent;
            this.CollectionName = details.CollectionName;
            this.CollectionExpression = details.CollectionExpression;
            this.DerivedCollectionName = details.DerivedCollectionName;
            this.DerivedCollectionExpression = details.DerivedCollectionExpression;
            this.DynamicPropertiesContainerName = details.DynamicPropertiesContainerName;
            this.FunctionName = details.FunctionName;
            this.ActionName = details.ActionName;
            this.IsAlternateKey = details.IsAlternateKey;
            this.KeyValues = details.KeyValues;
            this.NamedKeyValues = details.NamedKeyValues;
            this.EntryValue = details.EntryValue;
            this.EntryData = details.EntryData;
            this.Filter = details.Filter;
            this.FilterExpression = details.FilterExpression;
            this.Search = details.Search;
            this.SkipCount = details.SkipCount;
            this.TopCount = details.TopCount;
            this.ExpandAssociations = details.ExpandAssociations;
            this.SelectColumns = details.SelectColumns;
            this.OrderbyColumns = details.OrderbyColumns;
            this.ComputeCount = details.ComputeCount;
            this.IncludeCount = details.IncludeCount;
            this.LinkName = details.LinkName;
            this.LinkExpression = details.LinkExpression;
            this.MediaName = details.MediaName;
            this.MediaProperties = details.MediaProperties;
            this.QueryOptions = details.QueryOptions;
            this.QueryOptionsKeyValues = details.QueryOptionsKeyValues;
            this.QueryOptionsExpression = details.QueryOptionsExpression;
            this.BatchEntries = details.BatchEntries;
            this.Headers = details.Headers;
            this.Extensions = details.Extensions;
        }

        public bool HasKey => this.KeyValues != null && this.KeyValues.Count > 0 || this.NamedKeyValues != null && this.NamedKeyValues.Count > 0;

        public bool HasFilter => !string.IsNullOrEmpty(this.Filter) || !ReferenceEquals(this.FilterExpression, null);

        public bool HasSearch => !string.IsNullOrEmpty(this.Search);

        public bool HasFunction => !string.IsNullOrEmpty(this.FunctionName);

        public bool HasAction => !string.IsNullOrEmpty(this.ActionName);

        public bool FilterIsKey => this.NamedKeyValues != null;

        public bool HasQueryStringOptions =>
            (this.HasFilter && !this.FilterIsKey) ||
            this.HasSearch ||
            this.HasFunction ||
            this.SkipCount > 0 ||
            this.TopCount > 0 ||
            this.ExpandAssociations?.Count > 0 ||
            this.SelectColumns?.Count > 0 ||
            this.OrderbyColumns.Count > 0 ||
            this.ComputeCount ||
            this.IncludeCount ||
            !string.IsNullOrEmpty(this.QueryOptions) ||
            this.QueryOptionsKeyValues?.Count > 0 ||
            !ReferenceEquals(this.QueryOptionsExpression, null) ||
            this.Extensions?.Count > 0;

        public IDictionary<string, object> FilterAsKey => this.NamedKeyValues;
    }
}