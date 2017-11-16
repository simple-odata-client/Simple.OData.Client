using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Simple.OData.Client.Extensions;

#pragma warning disable 1591

namespace Simple.OData.Client
{
    // ALthough FluentCommand is never instantiated directly (only via ICommand interface)
    // it's declared as public in order to resolve problem when it is used with dynamic C#
    // For the same reason FluentClient is also declared as public
    // More: http://bloggingabout.net/blogs/vagif/archive/2013/08/05/we-need-better-interoperability-between-dynamic-and-statically-compiled-c.aspx

    public partial class FluentCommand
    {
        private readonly CommandDetails _details;

        internal static readonly string ResultLiteral = "__result";
        internal static readonly string AnnotationsLiteral = "__annotations";
        internal static readonly string MediaEntityLiteral = "__entity";

        internal FluentCommand(Session session, FluentCommand parent, ConcurrentDictionary<object, IDictionary<string, object>> batchEntries)
        {
            _details = new CommandDetails(session, parent, batchEntries);
        }

        internal FluentCommand(FluentCommand ancestor)
        {
            _details = new CommandDetails(ancestor._details);
        }

        private bool IsBatchResponse { get { return _details.Session == null; } }

        internal CommandDetails Details { get { return _details; } }

        internal FluentCommand Resolve()
        {
            if (!ReferenceEquals(_details.CollectionExpression, null))
            {
                For(_details.CollectionExpression.AsString(_details.Session));
                _details.CollectionExpression = null;
            }

            if (!ReferenceEquals(_details.DerivedCollectionExpression, null))
            {
                As(_details.DerivedCollectionExpression.AsString(_details.Session));
                _details.DerivedCollectionExpression = null;
            }

            if (!ReferenceEquals(_details.FilterExpression, null))
            {
                _details.NamedKeyValues = TryInterpretFilterExpressionAsKey(_details.FilterExpression);
                if (_details.NamedKeyValues == null)
                {
                    _details.Filter = _details.FilterExpression.Format(
                        new ExpressionContext(_details.Session, this.EntityCollection, null, this.DynamicPropertiesContainerName));
                }
                else
                {
                    _details.KeyValues = null;
                    _details.TopCount = -1;
                }
                if (_details.FilterExpression.HasTypeConstraint(_details.DerivedCollectionName))
                {
                    _details.DerivedCollectionName = null;
                }
                _details.FilterExpression = null;
            }

            if (!ReferenceEquals(_details.LinkExpression, null))
            {
                Link(_details.LinkExpression.AsString(_details.Session));
                _details.LinkExpression = null;
            }

            return this;
        }

        internal EntityCollection EntityCollection
        {
            get
            {
                if (string.IsNullOrEmpty(_details.CollectionName) && string.IsNullOrEmpty(_details.LinkName))
                    return null;

                EntityCollection entityCollection;
                if (!string.IsNullOrEmpty(_details.LinkName))
                {
                    var parent = new FluentCommand(_details.Parent).Resolve();
                    var collectionName = _details.Session.Metadata.GetNavigationPropertyPartnerTypeName(
                        parent.EntityCollection.Name, _details.LinkName);
                    entityCollection = _details.Session.Metadata.GetEntityCollection(collectionName);
                }
                else
                {
                    entityCollection = _details.Session.Metadata.GetEntityCollection(_details.CollectionName);
                }

                return string.IsNullOrEmpty(_details.DerivedCollectionName)
                    ? entityCollection
                    : _details.Session.Metadata.GetDerivedEntityCollection(entityCollection, _details.DerivedCollectionName);
            }
        }

        public string QualifiedEntityCollectionName
        {
            get
            {
                var entityCollection = new FluentCommand(this).Resolve().EntityCollection;
                return entityCollection.BaseEntityCollection == null
                    ? entityCollection.Name
                    : string.Format("{0}/{1}",
                        entityCollection.BaseEntityCollection.Name,
                        _details.Session.Metadata.GetQualifiedTypeName(entityCollection.Name));
            }
        }

        public string DynamicPropertiesContainerName
        {
            get { return _details.DynamicPropertiesContainerName; }
        }

        public Task<string> GetCommandTextAsync()
        {
            return GetCommandTextAsync(CancellationToken.None);
        }

        public async Task<string> GetCommandTextAsync(CancellationToken cancellationToken)
        {
            await _details.Session.ResolveAdapterAsync(cancellationToken).ConfigureAwait(false);
            return new FluentCommand(this).Resolve().Format();
        }

        public FluentCommand For(string collectionName)
        {
            if (IsBatchResponse) return this;

            var items = collectionName.Split('/');
            if (items.Count() > 1)
            {
                _details.CollectionName = items[0];
                _details.DerivedCollectionName = items[1];
            }
            else
            {
                _details.CollectionName = collectionName;
            }
            return this;
        }

        public FluentCommand WithProperties(string propertyName)
        {
            if (IsBatchResponse) return this;

            _details.DynamicPropertiesContainerName = propertyName;
            return this;
        }

        public FluentCommand WithMedia(IEnumerable<string> properties)
        {
            if (IsBatchResponse) return this;

            _details.MediaProperties = properties;
            return this;
        }

        public FluentCommand WithMedia(params string[] properties)
        {
            if (IsBatchResponse) return this;

            _details.MediaProperties = SplitItems(properties).ToList();
            return this;
        }

        public FluentCommand WithMedia(params ODataExpression[] properties)
        {
            return WithMedia(properties.Select(x => x.Reference));
        }

        public FluentCommand For(ODataExpression expression)
        {
            if (IsBatchResponse) return this;

            _details.CollectionExpression = expression;
            return this;
        }

        public FluentCommand As(string derivedCollectionName)
        {
            if (IsBatchResponse) return this;

            _details.DerivedCollectionName = derivedCollectionName;
            return this;
        }

        public FluentCommand As(ODataExpression expression)
        {
            if (IsBatchResponse) return this;

            _details.DerivedCollectionExpression = expression;
            return this;
        }

        public FluentCommand Link(string linkName)
        {
            if (IsBatchResponse) return this;

            _details.LinkName = linkName;
            return this;
        }

        public FluentCommand Link(ODataExpression expression)
        {
            if (IsBatchResponse) return this;

            _details.LinkExpression = expression;
            return this;
        }

        public FluentCommand Key(params object[] key)
        {
            if (IsBatchResponse) return this;

            if (key != null && key.Length == 1 && IsAnonymousType(key.First().GetType()))
            {
                var namedKeyValues = key.First();
                _details.NamedKeyValues = Utils.GetMappedProperties(namedKeyValues.GetType())
                    .Select(x => new KeyValuePair<string, object>(x.Name, x.GetValue(namedKeyValues, null))).ToIDictionary();
            }
            else
            {
                _details.KeyValues = key.ToList();
            }
            return this;
        }

        public FluentCommand Key(IEnumerable<object> key)
        {
            if (IsBatchResponse) return this;

            _details.KeyValues = key.ToList();
            _details.NamedKeyValues = null;
            return this;
        }

        public FluentCommand Key(IDictionary<string, object> key)
        {
            if (IsBatchResponse) return this;

            _details.NamedKeyValues = key;
            _details.KeyValues = null;
            return this;
        }

        public FluentCommand Filter(string filter)
        {
            if (IsBatchResponse) return this;

            if (string.IsNullOrEmpty(_details.Filter))
                _details.Filter = filter;
            else
                _details.Filter = string.Format("({0}) and ({1})", _details.Filter, filter);
            return this;
        }

        public FluentCommand Filter(ODataExpression expression)
        {
            if (IsBatchResponse) return this;

            if (ReferenceEquals(_details.FilterExpression, null))
                _details.FilterExpression = expression;
            else
                _details.FilterExpression = _details.FilterExpression && expression;
            return this;
        }

        public FluentCommand Search(string search)
        {
            if (IsBatchResponse) return this;

            _details.Search = search;
            return this;
        }

        public FluentCommand Skip(long count)
        {
            if (IsBatchResponse) return this;

            _details.SkipCount = count;
            return this;
        }

        public FluentCommand Top(long count)
        {
            if (IsBatchResponse) return this;

            if (!HasKey)
            {
                _details.TopCount = count;
            }
            else if (count != 1)
            {
                throw new InvalidOperationException("Top count may only be assigned to 1 when key is assigned");
            }
            return this;
        }

        public FluentCommand Expand(ODataExpandOptions expandOptions)
        {
            if (IsBatchResponse) return this;

            _details.ExpandAssociations.AddRange(new[] { new KeyValuePair<string, ODataExpandOptions>("*", ODataExpandOptions.ByValue()) });
            return this;
        }

        public FluentCommand Expand(IEnumerable<string> associations)
        {
            if (IsBatchResponse) return this;

            _details.ExpandAssociations.AddRange(SplitItems(associations).Select(x => new KeyValuePair<string, ODataExpandOptions>(x, ODataExpandOptions.ByValue())));
            return this;
        }

        public FluentCommand Expand(ODataExpandOptions expandOptions, IEnumerable<string> associations)
        {
            if (IsBatchResponse) return this;

            _details.ExpandAssociations.AddRange(SplitItems(associations).Select(x => new KeyValuePair<string, ODataExpandOptions>(x, expandOptions)));
            return this;
        }

        public FluentCommand Expand(params string[] associations)
        {
            if (IsBatchResponse) return this;

            _details.ExpandAssociations.AddRange(SplitItems(associations).Select(x => new KeyValuePair<string, ODataExpandOptions>(x, ODataExpandOptions.ByValue())));
            return this;
        }

        public FluentCommand Expand(ODataExpandOptions expandOptions, params string[] associations)
        {
            if (IsBatchResponse) return this;

            _details.ExpandAssociations.AddRange(SplitItems(associations).Select(x => new KeyValuePair<string, ODataExpandOptions>(x, expandOptions)));
            return this;
        }

        public FluentCommand Expand(params ODataExpression[] columns)
        {
            return Expand(columns.Select(x => x.Reference));
        }

        public FluentCommand Expand(ODataExpandOptions expandOptions, params ODataExpression[] columns)
        {
            return Expand(expandOptions, columns.Select(x => x.Reference));
        }

        public FluentCommand Select(IEnumerable<string> columns)
        {
            if (IsBatchResponse) return this;

            _details.SelectColumns.AddRange(SplitItems(columns).ToList());
            return this;
        }

        public FluentCommand Select(params string[] columns)
        {
            if (IsBatchResponse) return this;

            _details.SelectColumns.AddRange(SplitItems(columns).ToList());
            return this;
        }

        public FluentCommand Select(params ODataExpression[] columns)
        {
            return Select(columns.Select(x => x.Reference));
        }

        public FluentCommand OrderBy(IEnumerable<KeyValuePair<string, bool>> columns)
        {
            if (IsBatchResponse) return this;

            _details.OrderbyColumns.AddRange(SplitItems(columns));
            return this;
        }

        public FluentCommand OrderBy(params string[] columns)
        {
            return OrderBy(SplitItems(columns).Select(x => new KeyValuePair<string, bool>(x, false)));
        }

        public FluentCommand OrderBy(params ODataExpression[] columns)
        {
            return OrderBy(columns.Select(x => new KeyValuePair<string, bool>(x.Reference, false)));
        }

        public FluentCommand ThenBy(params string[] columns)
        {
            if (IsBatchResponse) return this;

            _details.OrderbyColumns.AddRange(SplitItems(columns).Select(x => new KeyValuePair<string, bool>(x, false)));
            return this;
        }

        public FluentCommand ThenBy(params ODataExpression[] columns)
        {
            return ThenBy(columns.Select(x => x.Reference).ToArray());
        }

        public FluentCommand OrderByDescending(params string[] columns)
        {
            return OrderBy(SplitItems(columns).Select(x => new KeyValuePair<string, bool>(x, true)));
        }

        public FluentCommand OrderByDescending(params ODataExpression[] columns)
        {
            return OrderBy(columns.Select(x => new KeyValuePair<string, bool>(x.Reference, true)));
        }

        public FluentCommand ThenByDescending(params string[] columns)
        {
            if (IsBatchResponse) return this;

            _details.OrderbyColumns.AddRange(SplitItems(columns).Select(x => new KeyValuePair<string, bool>(x, true)));
            return this;
        }

        public FluentCommand ThenByDescending(params ODataExpression[] columns)
        {
            return ThenByDescending(columns.Select(x => x.Reference).ToArray());
        }

        public FluentCommand QueryOptions(string queryOptions)
        {
            if (IsBatchResponse) return this;

            if (_details.QueryOptions == null)
                _details.QueryOptions = queryOptions;
            else
                _details.QueryOptions = string.Format("{0}&{1}", _details.QueryOptions, queryOptions);
            return this;
        }

        public FluentCommand QueryOptions(IDictionary<string, object> queryOptions)
        {
            if (IsBatchResponse) return this;

            _details.QueryOptionsKeyValues = queryOptions;
            return this;
        }

        public FluentCommand QueryOptions(ODataExpression expression)
        {
            if (IsBatchResponse) return this;

            if (ReferenceEquals(_details.QueryOptionsExpression, null))
                _details.QueryOptionsExpression = expression;
            else
                _details.QueryOptionsExpression = _details.QueryOptionsExpression && expression;
            return this;
        }

        public FluentCommand Media()
        {
            return Media(FluentCommand.MediaEntityLiteral);
        }

        public FluentCommand Media(string streamName)
        {
            if (IsBatchResponse) return this;

            _details.MediaName = streamName;
            return this;
        }

        public FluentCommand Media(ODataExpression expression)
        {
            return Media(expression.Reference);
        }

        public FluentCommand Count()
        {
            if (IsBatchResponse) return this;

            _details.ComputeCount = true;
            return this;
        }

        public FluentCommand Set(object value)
        {
            if (IsBatchResponse) return this;

            _details.EntryData = Utils.GetMappedProperties(value.GetType())
                .Select(x => new KeyValuePair<string, object>(x.GetMappedName(), x.GetValue(value, null)))
                .ToDictionary();
            if (_details.BatchEntries != null)
                _details.BatchEntries.GetOrAdd(value, _details.EntryData);
            return this;
        }

        public FluentCommand Set(IDictionary<string, object> value)
        {
            if (IsBatchResponse) return this;

            _details.EntryData = value;
            return this;
        }

        public FluentCommand Set(params ODataExpression[] value)
        {
            if (IsBatchResponse) return this;

            _details.EntryData = value.Select(x => new KeyValuePair<string, object>(x.Reference, x.Value)).ToIDictionary();
            if (_details.BatchEntries != null)
                _details.BatchEntries.GetOrAdd(value, _details.EntryData);
            return this;
        }

        public FluentCommand Function(string functionName)
        {
            if (IsBatchResponse) return this;

            _details.FunctionName = functionName;
            return this;
        }

        public FluentCommand Action(string actionName)
        {
            if (IsBatchResponse) return this;

            _details.ActionName = actionName;
            return this;
        }

        public bool FilterIsKey
        {
            get
            {
                return _details.NamedKeyValues != null;
            }
        }

        public IDictionary<string, object> FilterAsKey
        {
            get
            {
                return _details.NamedKeyValues;
            }
        }

        public FluentCommand WithCount()
        {
            if (IsBatchResponse) return this;

            _details.IncludeCount = true;
            return this;
        }

        public override string ToString()
        {
            return Format();
        }

        private string Format()
        {
            return _details.Session.Adapter.GetCommandFormatter().FormatCommand(this);
        }

        internal bool HasKey
        {
            get { return _details.KeyValues != null && _details.KeyValues.Count > 0 || _details.NamedKeyValues != null && _details.NamedKeyValues.Count > 0; }
        }

        internal bool HasFilter
        {
            get { return !string.IsNullOrEmpty(_details.Filter) || !ReferenceEquals(_details.FilterExpression, null); }
        }

        internal bool HasSearch
        {
            get { return !string.IsNullOrEmpty(_details.Search); }
        }

        public bool HasFunction
        {
            get
            {
                return !string.IsNullOrEmpty(_details.FunctionName);
            }
        }

        public bool HasAction
        {
            get
            {
                return !string.IsNullOrEmpty(_details.ActionName);
            }
        }

        internal IDictionary<string, object> KeyValues
        {
            get
            {
                if (!HasKey)
                    return null;

                var keyNames = _details.Session.Metadata.GetDeclaredKeyPropertyNames(this.EntityCollection.Name).ToList();
                var namedKeyValues = new Dictionary<string, object>();
                for (int index = 0; index < keyNames.Count; index++)
                {
                    bool found = false;
                    object keyValue = null;
                    if (_details.NamedKeyValues != null && _details.NamedKeyValues.Count > 0)
                    {
                        keyValue = _details.NamedKeyValues.FirstOrDefault(x => _details.Session.Settings.NameMatchResolver.IsMatch(x.Key, keyNames[index])).Value;
                        found = keyValue != null;
                    }
                    else if (_details.KeyValues != null && _details.KeyValues.Count >= index)
                    {
                        keyValue = _details.KeyValues[index];
                        found = true;
                    }
                    if (found)
                    {
                        var value = keyValue is ODataExpression ?
                            (keyValue as ODataExpression).Value :
                            keyValue;
                        namedKeyValues.Add(keyNames[index], value);
                    }
                }
                return namedKeyValues;
            }
        }

        internal IDictionary<string, object> CommandData
        {
            get
            {
                if (_details.EntryData == null)
                    return new Dictionary<string, object>();
                if (string.IsNullOrEmpty(_details.DynamicPropertiesContainerName))
                    return _details.EntryData;

                var entryData = new Dictionary<string, object>();
                foreach (var key in _details.EntryData.Keys.Where(x => 
                    !string.Equals(x, _details.DynamicPropertiesContainerName, StringComparison.OrdinalIgnoreCase)))
                {
                    entryData.Add(key, _details.EntryData[key]);
                }
                object dynamicProperties;
                if (_details.EntryData.TryGetValue(_details.DynamicPropertiesContainerName, out dynamicProperties) && dynamicProperties != null)
                {
                    if (dynamicProperties is IDictionary<string, object>)
                    {
                        var kv = dynamicProperties as IDictionary<string, object>;
                        foreach (var key in kv.Keys)
                        {
                            entryData.Add(key, kv[key]);
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            string.Format("Property {0} must implement IDictionary<string,object> interface", 
                            _details.DynamicPropertiesContainerName));
                    }
                }

                return entryData;
            }
        }

        internal IList<string> SelectedColumns
        {
            get { return _details.SelectColumns; }
        }

        internal string FunctionName
        {
            get { return _details.FunctionName; }
        }

        internal string ActionName
        {
            get { return _details.ActionName; }
        }

        private IEnumerable<string> SplitItems(IEnumerable<string> columns)
        {
            return columns.SelectMany(x => x.Split(',').Select(y => y.Trim()));
        }

        private IEnumerable<KeyValuePair<string, bool>> SplitItems(IEnumerable<KeyValuePair<string, bool>> columns)
        {
            return columns.SelectMany(x => x.Key.Split(',').Select(y => new KeyValuePair<string, bool>(y.Trim(), x.Value)));
        }

        private IDictionary<string, object> TryInterpretFilterExpressionAsKey(ODataExpression expression)
        {
            bool ok = false;
            IDictionary<string, object> namedKeyValues = new Dictionary<string, object>();
            if (!ReferenceEquals(expression, null))
            {
                ok = expression.ExtractLookupColumns(namedKeyValues);
            }
            if (!ok)
                return null;

            var keyNames = _details.Session.Metadata.GetDeclaredKeyPropertyNames(this.EntityCollection.Name).ToList();
            return keyNames.Count == namedKeyValues.Count && Utils.AllMatch(keyNames, namedKeyValues.Keys, _details.Session.Settings.NameMatchResolver)
                ? namedKeyValues
                : null;
        }

        private static bool IsAnonymousType(Type type)
        {
            // HACK: The only way to detect anonymous types right now.
            return type.HasCustomAttribute(typeof(CompilerGeneratedAttribute), false)
                       && type.IsGeneric() && type.Name.Contains("AnonymousType")
                       && (type.Name.StartsWith("<>", StringComparison.OrdinalIgnoreCase) ||
                           type.Name.StartsWith("VB$", StringComparison.OrdinalIgnoreCase))
                       && (type.GetTypeAttributes() & TypeAttributes.NotPublic) == TypeAttributes.NotPublic;
        }
    }
}
