using System;
using System.Collections.Generic;
using System.Linq;
using Simple.OData.Client.Extensions;

namespace Simple.OData.Client.V4.Adapter.Extensions
{
    public class DynamicDataAggregation
    {
        private readonly DataAggregationBuilderHolder _underlyingDataAggregationBuilder;

        internal DynamicDataAggregation()
        {
            _underlyingDataAggregationBuilder = new DataAggregationBuilderHolder();
        }
        
        public DynamicDataAggregation Filter(string filter)
        {
            var filterClause = (FilterClause) _underlyingDataAggregationBuilder.LastOrDefault(x => x is FilterClause);
            if (filterClause != null)
            {
                filterClause.Append(filter);
            }
            else
            {
                filterClause = new FilterClause(filter);
                _underlyingDataAggregationBuilder.Add(filterClause);
            }
            return this;
        }
        
        public DynamicDataAggregation Filter(ODataExpression filter)
        {
            if (_underlyingDataAggregationBuilder.LastOrDefault() is FilterClause filterClause)
            {
                filterClause.Append(filter);
            }
            else
            {
                filterClause = new FilterClause(filter);
                _underlyingDataAggregationBuilder.Add(filterClause);
            }
            return this;
        }

        public DynamicDataAggregation Aggregate(object aggregation)
        {
            var aggregationClauses = new AggregationClauseCollection<object>();
            var objectType = aggregation.GetType();
            var declaredProperties = objectType.GetDeclaredProperties();
            foreach (var property in declaredProperties)
            {
                var propertyValue = property.GetValueEx(aggregation);
                if (propertyValue is ValueTuple<string, ODataExpression> aggregatedProperty) 
                    aggregationClauses.Add(new AggregationClause<object>(property.Name, aggregatedProperty.Item2?.Reference, aggregatedProperty.Item1));
            }
            _underlyingDataAggregationBuilder.Add(aggregationClauses);
            return this;
        }

        public DynamicDataAggregation GroupBy(object groupBy)
        {
            var groupByColumns = new List<string>();
            var aggregationClauses = new AggregationClauseCollection<object>();
            if (groupBy is ODataExpression groupByExpression)
            {
                groupByColumns.Add(groupByExpression.Reference);
            }
            else
            {
                var objectType = groupBy.GetType();
                var declaredProperties = objectType.GetDeclaredProperties();
                foreach (var property in declaredProperties)
                {
                    var propertyValue = property.GetValueEx(groupBy);
                    switch (propertyValue)
                    {
                        case ODataExpression oDataExpression:
                            groupByColumns.Add(oDataExpression.Reference);
                            break;
                        case ValueTuple<string, ODataExpression> aggregatedProperty:
                            aggregationClauses.Add(new AggregationClause<object>(property.Name, aggregatedProperty.Item2?.Reference, aggregatedProperty.Item1));
                            break;
                    }
                }
            }
            _underlyingDataAggregationBuilder.Add(new GroupByClause<object>(groupByColumns, aggregationClauses));
            return this;
        }

        internal DataAggregationBuilder CreateBuilder()
        {
            return _underlyingDataAggregationBuilder;
        }

        private class DataAggregationBuilderHolder : DataAggregationBuilder
        {
            internal void Add(IDataAggregationClause dataAggregationClause)
            {
                DataAggregationClauses.Add(dataAggregationClause);
            }

            internal IDataAggregationClause LastOrDefault(Func<IDataAggregationClause, bool> predicate = null)
            {
                return DataAggregationClauses.LastOrDefault(predicate ?? (x => true));
            }
        }
    }
}