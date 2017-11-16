using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.OData;
using Simple.OData.Client.Extensions;

namespace Simple.OData.Client.V4.Adapter
{
    public class CommandFormatter : CommandFormatterBase
    {
        public CommandFormatter(ISession session)
            : base(session)
        {
        }

        public override FunctionFormat FunctionFormat
        {
            get { return FunctionFormat.Key; }
        }

        public override string ConvertValueToUriLiteral(object value, bool escapeDataString)
        {
            if (value != null && value.GetType().IsEnumType())
                value = new ODataEnumValue(value.ToString(), _session.Metadata.GetQualifiedTypeName(value.GetType().Name));
            if (value is ODataExpression)
                return (value as ODataExpression).AsString(_session);

            var odataVersion = (ODataVersion)Enum.Parse(typeof(ODataVersion), _session.Adapter.GetODataVersionString(), false);
            Func<object, string> convertValue = x => ODataUriUtils.ConvertToUriLiteral(x, odataVersion, (_session.Adapter as ODataAdapter).Model);

            if (value is ODataEnumValue && _session.Settings.EnumPrefixFree)
                value = (value as ODataEnumValue).Value;
            else if (value is DateTime)
                value = new DateTimeOffset((DateTime)value);

            return escapeDataString
                ? Uri.EscapeDataString(convertValue(value))
                : convertValue(value);
        }

        protected override void FormatExpandSelectOrderby(IList<string> commandClauses, EntityCollection resultCollection, FluentCommand command)
        {
            if (command.Details.ExpandAssociations.Any())
            {
                commandClauses.Add(string.Format("{0}={1}", ODataLiteral.Expand,
                    string.Join(",", command.Details.ExpandAssociations.Select(x =>
                        FormatExpansionSegment(x.Key, resultCollection,
                        x.Value,
                        SelectPathSegmentColumns(command.Details.SelectColumns, x.Key),
                        SelectPathSegmentColumns(command.Details.OrderbyColumns, x.Key))))));
            }

            FormatClause(commandClauses, resultCollection,
                SelectPathSegmentColumns(command.Details.SelectColumns, null,
                    command.Details.ExpandAssociations.Select(FormatFirstSegment).ToList()),
                ODataLiteral.Select, FormatSelectItem);

            /*FormatClause(commandClauses, resultCollection,
                SelectPathSegmentColumns(command.Details.OrderbyColumns, null,
                    command.Details.ExpandAssociations.Select(FormatFirstSegment).ToList()),
                ODataLiteral.OrderBy, FormatOrderByItem);

            foreach (var x in command.Details.ExpandAssociations)
            {
                var segmentOrderByColumns = SelectPathSegmentColumns(command.Details.OrderbyColumns, x.Key);
                if (segmentOrderByColumns.Any() && !IsInnerCollectionOrderBy(x.Key, resultCollection, segmentOrderByColumns))
                {
                    FormatClause(commandClauses, resultCollection, command.Details.OrderbyColumns, ODataLiteral.OrderBy, FormatOrderByItem);
                }
            }*/

            var l = command.Details.OrderbyColumns
                 .Where(o => !command.Details.ExpandAssociations.Select(ea => ea.Key).Any(ea => IsInnerCollectionOrderBy(ea, resultCollection, o.Key))).ToList();

            FormatClause(commandClauses, resultCollection,
                command.Details.OrderbyColumns
                    .Where(o => !command.Details.ExpandAssociations.Select(ea => ea.Key)
                                .Any(ea => IsInnerCollectionOrderBy(ea, resultCollection, o.Key))).ToList(),
                ODataLiteral.OrderBy, FormatOrderByItem);
        }

        protected override void FormatInlineCount(IList<string> commandClauses)
        {
            commandClauses.Add(string.Format("{0}={1}", ODataLiteral.Count, ODataLiteral.True));
        }

        private string FormatExpansionSegment(string path, EntityCollection entityCollection,
            ODataExpandOptions expandOptions, IList<string> selectColumns, IList<KeyValuePair<string, bool>> orderbyColumns)
        {
            var items = path.Split('/');
            var associationName = _session.Metadata.GetNavigationPropertyExactName(entityCollection.Name, items.First());
            bool expandsToCollection = _session.Metadata.IsNavigationPropertyCollection(entityCollection.Name, associationName);

            var clauses = new List<string>();
            var text = associationName;
            if (expandOptions.ExpandMode == ODataExpandMode.ByReference)
                text += "/" + ODataLiteral.Ref;

            if (items.Count() > 1)
            {
                path = path.Substring(items.First().Length + 1);
                entityCollection = _session.Metadata.GetEntityCollection(
                    _session.Metadata.GetNavigationPropertyPartnerTypeName(entityCollection.Name, associationName));

                clauses.Add(string.Format("{0}={1}", ODataLiteral.Expand,
                    FormatExpansionSegment(path, entityCollection, expandOptions,
                        SelectPathSegmentColumns(selectColumns, path),
                        SelectPathSegmentColumns(orderbyColumns, path))));
            }

            if (expandOptions.Levels > 1)
            {
                clauses.Add(string.Format("{0}={1}", ODataLiteral.Levels, expandOptions.Levels));
            }
            else if (expandOptions.Levels == 0)
            {
                clauses.Add(string.Format("{0}={1}", ODataLiteral.Levels, ODataLiteral.Max));
            }

            if (selectColumns.Any())
            {
                var columns = string.Join(",", SelectPathSegmentColumns(selectColumns, null));
                if (!string.IsNullOrEmpty(columns))
                    clauses.Add(string.Format("{0}={1}", ODataLiteral.Select, columns));
            }

            if (expandsToCollection && orderbyColumns.Any())
            {
                var columns = string.Join(",", SelectPathSegmentColumns(orderbyColumns, null)
                    .Select(x => x.Key + (x.Value ? " desc" : string.Empty)).ToList());
                if (!string.IsNullOrEmpty(columns))
                    clauses.Add(string.Format("{0}={1}", ODataLiteral.OrderBy, columns));
            }

            if (clauses.Any())
                text += string.Format("({0})", string.Join(";", clauses));

            return text;
        }

        private IList<string> SelectPathSegmentColumns(
            IList<string> columns, string path, IList<string> excludePaths = null)
        {
            if (string.IsNullOrEmpty(path))
            {
                var resultColumns = columns.Where(x => !HasMultipleSegments(x)).ToList();
                if (excludePaths != null)
                    resultColumns.AddRange(columns.Where(x => HasMultipleSegments(x) &&
                        !excludePaths.Any(y => FormatFirstSegment(y).Contains(FormatFirstSegment(x)))));
                return resultColumns;
            }
            else
            {
                return columns.Where(x => HasMultipleSegments(x) && FormatFirstSegment(x) == FormatFirstSegment(path))
                    .Select(x => FormatSkipSegments(x, 1)).ToList();
            }
        }

        private IList<KeyValuePair<string, bool>> SelectPathSegmentColumns(
            IList<KeyValuePair<string, bool>> columns, string path, IList<string> excludePaths = null)
        {
            if (string.IsNullOrEmpty(path))
            {
                var resultColumns = columns.Where(x => !HasMultipleSegments(x)).ToList();
                if (excludePaths != null)
                    resultColumns.AddRange(columns.Where(x => HasMultipleSegments(x) &&
                        !excludePaths.Any(y => FormatFirstSegment(y).Contains(FormatFirstSegment(x)))));
                return resultColumns;
            }
            else
            {
                return columns.Where(x => HasMultipleSegments(x) && FormatFirstSegment(x) == FormatFirstSegment(path))
                    .Select(x => new KeyValuePair<string, bool>(FormatSkipSegments(x, 1), x.Value)).ToList();
            }
        }

        /*private bool IsInnerCollectionOrderBy(string path, EntityCollection entityCollection, IList<KeyValuePair<string, bool>> orderbyColumns)
        {
            var items = path.Split('/');
            var associationName = _session.Metadata.GetNavigationPropertyExactName(entityCollection.Name, items.First());

            if (_session.Metadata.IsNavigationPropertyCollection(entityCollection.Name, associationName) && orderbyColumns.Any())
                return true;

            if (items.Count() > 1)
            {
                path = path.Substring(items.First().Length + 1);
                entityCollection = _session.Metadata.GetEntityCollection(
                    _session.Metadata.GetNavigationPropertyPartnerTypeName(entityCollection.Name, associationName));

                return IsInnerCollectionOrderBy(path, entityCollection, SelectPathSegmentColumns(orderbyColumns, path));
            }

            return false;
        }*/

        private bool IsInnerCollectionOrderBy(string expandAssociation, EntityCollection entityCollection, string orderByColumn)
        {
            var items = expandAssociation.Split('/');
            if (items.First() != FormatFirstSegment(orderByColumn))
                return false;

            var associationName = _session.Metadata.GetNavigationPropertyExactName(entityCollection.Name, items.First());
            if (_session.Metadata.IsNavigationPropertyCollection(entityCollection.Name, associationName))
                return true;

            if (items.Count() > 1)
            {
                expandAssociation = expandAssociation.Substring(items.First().Length + 1);
                entityCollection = _session.Metadata.GetEntityCollection(
                  _session.Metadata.GetNavigationPropertyPartnerTypeName(entityCollection.Name, associationName));

                if (!HasMultipleSegments(orderByColumn) || FormatFirstSegment(orderByColumn) != FormatFirstSegment(expandAssociation))
                    return false;

                orderByColumn = FormatSkipSegments(orderByColumn, 1);
                return IsInnerCollectionOrderBy(expandAssociation, entityCollection, orderByColumn);
            }

            return false;
        }

        private bool HasMultipleSegments(string path)
        {
            return path.Contains("/");
        }

        private bool HasMultipleSegments<T>(KeyValuePair<string, T> path)
        {
            return path.Key.Contains("/");
        }

        private string FormatFirstSegment(string path)
        {
            return path.Split('/').First();
        }

        private string FormatFirstSegment<T>(KeyValuePair<string, T> path)
        {
            return path.Key.Split('/').First();
        }

        private string FormatSkipSegments(string path, int skipCount)
        {
            return string.Join("/", path.Split('/').Skip(skipCount));
        }

        private string FormatSkipSegments<T>(KeyValuePair<string, T> path, int skipCount)
        {
            return string.Join("/", path.Key.Split('/').Skip(skipCount));
        }
    }
}