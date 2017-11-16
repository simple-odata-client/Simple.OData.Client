using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Simple.OData.Client
{
    class CommandDetails
    {
        public Session Session { get; private set; }
        public FluentCommand Parent { get; private set; }
        public string CollectionName { get; set; }
        public ODataExpression CollectionExpression { get; set; }
        public string DerivedCollectionName { get; set; }
        public ODataExpression DerivedCollectionExpression { get; set; }
        public string DynamicPropertiesContainerName { get; set; }
        public string FunctionName { get; set; }
        public string ActionName { get; set; }
        public IList<object> KeyValues { get; set; }
        public IDictionary<string, object> NamedKeyValues { get; set; }
        public IDictionary<string, object> EntryData { get; set; }
        public string Filter { get; set; }
        public ODataExpression FilterExpression { get; set; }
        public string Search { get; set; }
        public long SkipCount { get; set; }
        public long TopCount { get; set; }
        public List<KeyValuePair<string, ODataExpandOptions>> ExpandAssociations { get; private set; }
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

        public CommandDetails(Session session, FluentCommand parent, ConcurrentDictionary<object, IDictionary<string, object>> batchEntries)
        {
            this.Session = session;
            this.Parent = parent;
            this.SkipCount = -1;
            this.TopCount = -1;
            this.ExpandAssociations = new List<KeyValuePair<string, ODataExpandOptions>>();
            this.SelectColumns = new List<string>();
            this.OrderbyColumns = new List<KeyValuePair<string, bool>>();
            this.MediaProperties = new List<string>();
            this.BatchEntries = batchEntries;
        }

        public CommandDetails(CommandDetails details)
        {
            this.Session = details.Session;
            this.Parent = details.Parent;
            this.CollectionName = details.CollectionName;
            this.CollectionExpression = details.CollectionExpression;
            this.DerivedCollectionName = details.DerivedCollectionName;
            this.DerivedCollectionExpression = details.DerivedCollectionExpression;
            this.DynamicPropertiesContainerName = details.DynamicPropertiesContainerName;
            this.FunctionName = details.FunctionName;
            this.ActionName = details.ActionName;
            this.KeyValues = details.KeyValues;
            this.NamedKeyValues = details.NamedKeyValues;
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
        }
    }
}