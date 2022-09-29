using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Simple.OData.Client.V4.Adapter.Extensions
{
	internal abstract class DataAggregationBuilder
	{
		protected readonly List<IDataAggregationClause> DataAggregationClauses;
		private DataAggregationBuilder? _nextDataAggregationBuilder;

		protected DataAggregationBuilder()
		{
			DataAggregationClauses = new List<IDataAggregationClause>();
		}

		internal string Build(ResolvedCommand command, ISession session)
		{
			var context = new ExpressionContext(session, null, null, command.DynamicPropertiesContainerName);
			var commandText = string.Empty;
			foreach (var applyClause in DataAggregationClauses)
			{
				var formattedApplyClause = applyClause.Format(context);
				if (string.IsNullOrEmpty(formattedApplyClause))
				{
					continue;
				}

				if (commandText.Length > 0)
				{
					commandText += "/";
				}

				commandText += formattedApplyClause;
			}

			return AddNextCommand(commandText, command, session);
		}

		internal void Append(DataAggregationBuilder nextDataAggregationBuilder)
		{
			if (_nextDataAggregationBuilder != null)
			{
				_nextDataAggregationBuilder.Append(nextDataAggregationBuilder);
				return;
			}

			_nextDataAggregationBuilder = nextDataAggregationBuilder;
		}

		private string AddNextCommand(string commandText, ResolvedCommand command, ISession session)
		{
			if (_nextDataAggregationBuilder == null)
			{
				return commandText;
			}

			var nestedCommand = _nextDataAggregationBuilder.Build(command, session);
			if (string.IsNullOrEmpty(nestedCommand))
			{
				return commandText;
			}

			if (commandText.Length > 0)
			{
				commandText += "/";
			}

			commandText += nestedCommand;

			return commandText;
		}
	}

	/// <inheritdoc cref="IDataAggregation{T}"/>
	internal class DataAggregationBuilder<T> : DataAggregationBuilder, IDataAggregation<T>
		where T : class
	{
		private readonly ISession _session;

		internal DataAggregationBuilder(ISession session) : base()
		{
			_session = session;
		}

		public IDataAggregation<T> Filter(Expression<Func<T, bool>> filter)
		{
			if (DataAggregationClauses.LastOrDefault() is FilterClause filterClause)
			{
				filterClause.Append(ODataExpression.FromLinqExpression(filter));
			}
			else
			{
				filterClause = new FilterClause(ODataExpression.FromLinqExpression(filter));
				DataAggregationClauses.Add(filterClause);
			}

			return this;
		}

		public IDataAggregation<TR> Aggregate<TR>(Expression<Func<T, IAggregationFunction<T>, TR>> aggregation) where TR : class
		{
			var aggregationClauses = ExtractAggregationClauses(aggregation);
			DataAggregationClauses.Add(aggregationClauses);
			var nextDataAggregationBuilder = new DataAggregationBuilder<TR>(_session);
			Append(nextDataAggregationBuilder);
			return nextDataAggregationBuilder;
		}

		private static AggregationClauseCollection<T> ExtractAggregationClauses<TR>(Expression<Func<T, IAggregationFunction<T>, TR>> expression) where TR : class
		{
			var aggregationClauses = new AggregationClauseCollection<T>();
			switch (expression.Body)
			{
				case NewExpression newExpression:
					{
						var membersCount = Math.Min(newExpression.Members.Count, newExpression.Arguments.Count);
						for (var index = 0; index < membersCount; index++)
						{
							if (newExpression.Arguments[index] is MethodCallExpression methodCallExpression && methodCallExpression.Method.DeclaringType == typeof(IAggregationFunction<T>))
							{
								aggregationClauses.Add(new AggregationClause<T>(newExpression.Members[index].Name, newExpression.Arguments[index]));
							}
						}

						break;
					}
				case MemberInitExpression memberInitExpression:
					{
						foreach (var assignment in memberInitExpression.Bindings.OfType<MemberAssignment>())
						{
							if (assignment.Expression is MethodCallExpression methodCallExpression && methodCallExpression.Method.DeclaringType == typeof(IAggregationFunction<T>))
							{
								aggregationClauses.Add(new AggregationClause<T>(assignment.Member.Name, assignment.Expression));
							}
						}

						break;
					}
				default:
					throw new AggregateException("Expression should be a NewExpression or MemberInitExpression");
			}

			return aggregationClauses;
		}

		public IDataAggregation<TR> GroupBy<TR>(Expression<Func<T, IAggregationFunction<T>, TR>> groupBy) where TR : class
		{
			var groupByColumns = new List<string>();
			AggregationClauseCollection<T>? aggregationClauses = null;
			if (groupBy.Body is MemberExpression memberExpression)
			{
				groupByColumns.Add(memberExpression.ExtractColumnName(_session.TypeCache));
			}
			else
			{
				aggregationClauses = ExtractAggregationClauses(groupBy);
				groupByColumns.AddRange(ExtractGroupByColumns(groupBy));
			}

			var groupByClause = new GroupByClause<T>(groupByColumns, aggregationClauses);
			DataAggregationClauses.Add(groupByClause);
			var nextDataAggregationBuilder = new DataAggregationBuilder<TR>(_session);
			Append(nextDataAggregationBuilder);
			return nextDataAggregationBuilder;
		}

		private IEnumerable<string> ExtractGroupByColumns<TR>(Expression<Func<T, IAggregationFunction<T>, TR>> expression) where TR : class
		{
			switch (expression.Body)
			{
				case NewExpression newExpression:
					{
						var membersCount = Math.Min(newExpression.Members.Count, newExpression.Arguments.Count);
						for (var index = 0; index < membersCount; index++)
						{
							switch (newExpression.Arguments[index])
							{
								case MemberExpression _:
									yield return newExpression.Arguments[index].ExtractColumnName(_session.TypeCache);
									break;
								case MemberInitExpression memberInitExpression:
									{
										foreach (var columnName in ExtractColumnNames(memberInitExpression))
										{
											yield return columnName;
										}

										break;
									}
							}
						}

						break;
					}
				case MemberInitExpression memberInitExpression:
					{
						foreach (var columnName in ExtractColumnNames(memberInitExpression))
						{
							yield return columnName;
						}

						break;
					}
				default:
					throw new AggregateException("Expression should be a NewExpression or MemberInitExpression");
			}
		}

		private IEnumerable<string> ExtractColumnNames(MemberInitExpression expression)
		{
			var columnNames = new List<string>();
			foreach (var assignment in expression.Bindings.OfType<MemberAssignment>())
			{
				switch (assignment.Expression)
				{
					case MemberExpression _:
						columnNames.Add(assignment.Expression.ExtractColumnName(_session.TypeCache));
						break;
					case MemberInitExpression memberInitExpression:
						columnNames.AddRange(ExtractColumnNames(memberInitExpression));
						break;
				}
			}

			return columnNames;
		}
	}
}