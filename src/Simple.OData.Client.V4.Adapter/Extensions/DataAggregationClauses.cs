using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

namespace Simple.OData.Client.V4.Adapter.Extensions;

internal interface IDataAggregationClause
{
	string Format(ExpressionContext context);
}

internal class FilterClause : IDataAggregationClause
{
	private ODataExpression _filterExpression;
	private string _filter;

	internal FilterClause(string filter)
	{
		_filter = filter;
	}

	internal FilterClause(ODataExpression expression)
	{
		_filterExpression = expression;
	}

	internal void Append(string filter)
	{
		_filter = $"{_filter} and {filter}";
	}

	internal void Append(ODataExpression expression)
	{
		_filterExpression = _filterExpression && expression;
	}

	public string Format(ExpressionContext context)
	{
		if (string.IsNullOrEmpty(_filter) && _filterExpression is not null)
		{
			_filter = _filterExpression.Format(context);
		}

		return string.IsNullOrEmpty(_filter)
			? string.Empty
			: $"filter({_filter})";
	}
}

internal class GroupByClause<T> : IDataAggregationClause
{
	private readonly IEnumerable<string> _columns;
	private readonly AggregationClauseCollection<T>? _aggregation;

	internal GroupByClause(
		IEnumerable<string> columns,
		AggregationClauseCollection<T>? aggregation = null)
	{
		_columns = columns;
		_aggregation = aggregation;
	}

	public string Format(ExpressionContext context)
	{
		var formattedAggregation = _aggregation?.Format(context);
		var aggregation = string.IsNullOrEmpty(formattedAggregation)
			? string.Empty
			: $",{formattedAggregation}";

		return $"groupby(({string.Join(",", _columns)}){aggregation})";
	}
}

internal class AggregationClauseCollection<T> : IDataAggregationClause
{
	private readonly ICollection<AggregationClause<T>> _clauses = new List<AggregationClause<T>>();

	internal void Add(AggregationClause<T> clause)
	{
		_clauses.Add(clause);
	}

	public string Format(ExpressionContext context)
	{
		return _clauses.Any()
			? $"aggregate({string.Join(",", _clauses.Select(c => c.Format(context)))})"
			: string.Empty;
	}
}

internal class AggregationClause<T>
{
	private static readonly Dictionary<string, string> KnownFunctionTemplates = new()
	{
		{ nameof(IAggregationFunction<T>.Average), "{0} with average" },
		{ nameof(IAggregationFunction<T>.Sum), "{0} with sum" },
		{ nameof(IAggregationFunction<T>.Min), "{0} with min" },
		{ nameof(IAggregationFunction<T>.Max), "{0} with max" },
		{ nameof(IAggregationFunction<T>.CountDistinct), "{0} with countdistinct" },
		{ nameof(IAggregationFunction<T>.Count), "$count" }
	};

	private readonly string _propertyName;
	private string _aggregatedColumnName;
	private string _aggregationMethodName;
	private readonly MethodCallExpression _expression;

	internal AggregationClause(string propertyName, Expression expression)
	{
		_propertyName = propertyName;
		if (expression is not MethodCallExpression methodCallExpression)
		{
			throw new ArgumentException($"Expression should be a method call.");
		}

		_expression = methodCallExpression;
	}

	internal AggregationClause(string propertyName, string aggregatedColumnName, string aggregationMethodName)
	{
		_propertyName = propertyName;
		_aggregatedColumnName = aggregatedColumnName;
		_aggregationMethodName = aggregationMethodName;
	}

	public string Format(ExpressionContext context)
	{
		var function = FormatFunction(context);
		return $"{function} as {_propertyName}";
	}

	private string FormatFunction(ExpressionContext context)
	{
		if (_expression != null)
		{
			_aggregatedColumnName = string.Empty;
			if (_expression.Arguments.Any())
			{
				var aggregationMethodArgument = _expression.Arguments[0];
				_aggregatedColumnName = aggregationMethodArgument.ExtractColumnName(context.Session.TypeCache);
			}

			_aggregationMethodName = _expression.Method.Name;
		}

		if (KnownFunctionTemplates.TryGetValue(_aggregationMethodName, out var function))
		{
			return string.Format(CultureInfo.InvariantCulture, function, _aggregatedColumnName);
		}

		throw new InvalidOperationException($"Unknown aggregation method '{_aggregationMethodName}'");
	}
}
