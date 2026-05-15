using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Simple.OData.Client.Extensions;

namespace Simple.OData.Client
{
	public partial class ODataExpression
	{
		public IDictionary<string, IList<ODataExpression>> ProcessFilter(ISession session, EntityCollection EntityCollection)
		{
			IDictionary<string, IList<ODataExpression>> entityFilters = new Dictionary<string, IList<ODataExpression>>();
			ProcessFilter(new ExpressionContext(session), EntityCollection, entityFilters, null, true);
			return entityFilters;
		}

		internal HashSet<string> ProcessFilter(ExpressionContext context, EntityCollection EntityCollection, IDictionary<string, IList<ODataExpression>> entityFilters, HashSet<string> currentEntities, bool isBase)
		{
			if (context.IsQueryOption && _operator != ExpressionType.Default &&
				_operator != ExpressionType.And && _operator != ExpressionType.Equal)
			{
				throw new InvalidOperationException("Invalid custom query option");
			}

			if (_operator == ExpressionType.Default && !this.IsValueConversion)
			{
				bool isAlwaysTrueFilter = false, isAlwaysFalseFilter = false;
				currentEntities = this.Reference != null ?
					ProcessReference(context, EntityCollection, entityFilters, currentEntities, false) : this.Function != null ?
					ProcessFunction(context, EntityCollection, entityFilters, currentEntities, false) :
					ProcessValue(context, EntityCollection, entityFilters, currentEntities, isBase, out isAlwaysFalseFilter, out isAlwaysTrueFilter);

				return AddFilter(EntityCollection, entityFilters, currentEntities, isBase, isAlwaysFalseFilter, isAlwaysTrueFilter);
			}
			else if (this.IsValueConversion)
			{
				var expr = this.Value as ODataExpression;
				if (expr.Reference == null && expr.Function == null && !expr.IsValueConversion)
				{
					object result;
					if (expr.Value != null && expr.Value.GetType().IsEnumType())
					{
						expr = new ODataExpression(expr.Value);
					}
					else if (Utils.TryConvert(expr.Value, _conversionType, out result))
					{
						expr = new ODataExpression(result);
					}
				}
				return ProcessExpression(expr, context, EntityCollection, entityFilters, currentEntities, false);
			}
			else if (_operator == ExpressionType.Not || _operator == ExpressionType.Negate)
			{
				currentEntities = ProcessExpression(_left, context, EntityCollection, entityFilters, currentEntities, false);
				return AddFilter(EntityCollection, entityFilters, currentEntities, isBase);
			}
			else
			{
				if (isBase && (_operator == ExpressionType.And || _operator == ExpressionType.AndAlso))
				{
					ProcessExpression(_left, context, EntityCollection, entityFilters, null, true);
					ProcessExpression(_right, context, EntityCollection, entityFilters, null, true);
					return null;
				}
				else
				{

					currentEntities = ProcessExpression(_left, context, EntityCollection, entityFilters, currentEntities, false);
					currentEntities = ProcessExpression(_right, context, EntityCollection, entityFilters, currentEntities, false);
				}
				return AddFilter(EntityCollection, entityFilters, currentEntities, isBase);
			}
		}

		private HashSet<string> AddFilter(EntityCollection EntityCollection, IDictionary<string, IList<ODataExpression>> entityFilters, HashSet<string> currentEntities, bool isBase) => AddFilter(EntityCollection, entityFilters, currentEntities, isBase, false, false);

		private HashSet<string> AddFilter(EntityCollection EntityCollection, IDictionary<string, IList<ODataExpression>> entityFilters, HashSet<string> currentEntities, bool isBase, bool isAlwaysFalseFilter, bool isAlwaysTrueFilter)
		{
			if (isBase)
			{
				if (!isAlwaysTrueFilter)
				{ // Optimize out always-true filters
					string filterEntity = (currentEntities == null || currentEntities.Count == 0 || currentEntities.Count > 1) ? EntityCollection.Name : currentEntities.First();
					IList<ODataExpression> filters;
					if (!entityFilters.TryGetValue(filterEntity, out filters))
					{
						filters = new List<ODataExpression>();
						entityFilters.Add(filterEntity, filters);
					}
					filters.Add(this);
				}
				return null;
			}
			return currentEntities;
		}

		private static HashSet<string> ProcessExpression(ODataExpression expr, ExpressionContext context, EntityCollection EntityCollection, IDictionary<string, IList<ODataExpression>> entityFilters, HashSet<string> currentEntities, bool isBase)
		{
			if (ReferenceEquals(expr, null))
			{
				return currentEntities;
			}
			else
			{
				return expr.ProcessFilter(context, EntityCollection, entityFilters, currentEntities, isBase);
			}
		}

		private HashSet<string> ProcessFunction(ExpressionContext context, EntityCollection EntityCollection, IDictionary<string, IList<ODataExpression>> entityFilters, HashSet<string> currentEntities, bool isBase)
		{
			currentEntities = ProcessExpression(_functionCaller, context, EntityCollection, entityFilters, currentEntities, isBase);
			foreach (ODataExpression expr in Function.Arguments)
			{
				currentEntities = ProcessExpression(expr, context, EntityCollection, entityFilters, currentEntities, isBase);
			}
			return currentEntities;
		}

		private HashSet<string> ProcessReference(ExpressionContext context, EntityCollection EntityCollection, IDictionary<string, IList<ODataExpression>> entityFilters, HashSet<string> currentEntities, bool isBase)
		{
			currentEntities = currentEntities ?? new HashSet<string>();
			currentEntities.Add(GetEntityPath(context, EntityCollection));
			return currentEntities;
		}

		private HashSet<string> ProcessValue(ExpressionContext context, EntityCollection EntityCollection, IDictionary<string, IList<ODataExpression>> entityFilters, HashSet<string> currentEntities, bool isBase, out bool isAlwaysFalseFilter, out bool isAlwaysTrueFilter)
		{
			if (isBase && !IsNull && Value is bool)
			{
				isAlwaysTrueFilter = (bool)Value;
				isAlwaysFalseFilter = !isAlwaysTrueFilter;
			}
			else isAlwaysTrueFilter = isAlwaysFalseFilter = false;

			return currentEntities;
		}

		private string GetEntityPath(ExpressionContext context, EntityCollection entityCollection)
		{
			string[] path = Reference.Split('/');
			ISession _session = context.Session;
			string baseEntity = entityCollection.Name;
			int to;
			for(to = 0; to < path.Length - 1; to++)
			{
				if(!_session.Metadata.HasNavigationProperty(entityCollection.Name, path[to]))
					break;

				string associationName = _session.Metadata.GetNavigationPropertyExactName(entityCollection.Name, path[to]);
				if (_session.Metadata.IsNavigationPropertyCollection(entityCollection.Name, associationName))
				{
					if(context.Session.Adapter.AdapterVersion != AdapterVersion.V3)
						Reference = string.Join("/", path, to + 1, path.Length - to - 1);
					return string.Join("/", path, 0, to + 1);
				}
				entityCollection = _session.Metadata.GetEntityCollection(
				  _session.Metadata.GetNavigationPropertyPartnerTypeName(entityCollection.Name, associationName));
			}
			return baseEntity;
		}
	}
}
