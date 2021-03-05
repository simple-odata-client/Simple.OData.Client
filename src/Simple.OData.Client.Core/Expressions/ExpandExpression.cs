using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Simple.OData.Client
{
    internal static class ExpandExpression
    {
        public static IEnumerable<ODataExpandAssociation> ExtractExpandAssociations<T>(
            this Expression<Func<T, object>> expression,
            ITypeCache typeCache)
        {
            var expandExpressionVisitor = new ExpandExpressionVisitor(typeCache);
            expandExpressionVisitor.Visit(expression);

            if (expandExpressionVisitor.ExpandAssociations.Any())
                return expandExpressionVisitor.ExpandAssociations;

            throw Utils.NotSupportedExpression(expression);
        }
    }

    internal class ExpandExpressionVisitor : ExpressionVisitor
    {
        private readonly ITypeCache _typeCache;
        
        public ExpandExpressionVisitor(ITypeCache typeCache)
        {
            _typeCache = typeCache;
        }

        public List<ODataExpandAssociation> ExpandAssociations { get; } = new List<ODataExpandAssociation>();

        protected override Expression VisitMember(MemberExpression node)
        {
            var memberName = _typeCache.GetMappedName(node.Expression.Type, node.Member.Name);
            var association = new ODataExpandAssociation(memberName);
            var associationCollection = node.Expression is MemberExpression
                ? AddNestedExpandAssociationAndGetDeepestChild(node.Expression).ExpandAssociations
                : ExpandAssociations;
            associationCollection.Add(association);
            
            return node;
        }

        protected override Expression VisitUnary(UnaryExpression node)
        {
            if (node.NodeType != ExpressionType.Convert) return base.VisitUnary(node);
            
            ExpandAssociations.AddRange(ExtractNestedExpandAssociations(node.Operand));
            return node;
        }

        protected override Expression VisitMethodCall(MethodCallExpression node)
        {
            if (node.Arguments.Count != 2)
                throw Utils.NotSupportedExpression(node);

            var association = AddNestedExpandAssociationAndGetDeepestChild(node.Arguments[0]);

            switch (node.Method.Name)
            {
                case "Select":
                {
                    association.ExpandAssociations.AddRange(ExtractNestedExpandAssociations(node.Arguments[1]));
                    
                    return node;
                }
                case "OrderBy":
                case "ThenBy":
                {
                    if ((node.Arguments[0] as MethodCallExpression)?.Method.Name == "Select")
                        throw Utils.NotSupportedExpression(node);

                    association.OrderByColumns
                        .AddRange(node.Arguments[1]
                            .ExtractColumnNames(_typeCache).Select(c => new ODataOrderByColumn(c, false)));

                    return node;
                }
                case "OrderByDescending":
                case "ThenByDescending":
                {
                    if ((node.Arguments[0] as MethodCallExpression)?.Method.Name == "Select")
                        throw Utils.NotSupportedExpression(node);

                    association.OrderByColumns
                        .AddRange(node.Arguments[1]
                            .ExtractColumnNames(_typeCache).Select(c => new ODataOrderByColumn(c, true)));

                    return node;
                }
                case "Where":
                {
                    var filterExpression =
                        ODataExpression.FromLinqExpression((node.Arguments[1] as LambdaExpression)?.Body);
                    if (ReferenceEquals(association.FilterExpression, null))
                        association.FilterExpression = filterExpression;
                    else
                        association.FilterExpression = association.FilterExpression && filterExpression;

                    return node;
                }
                default:
                    throw Utils.NotSupportedExpression(node);
            }
        }

        protected override Expression VisitNew(NewExpression node)
        {
            ExpandAssociations.AddRange(node.Arguments.SelectMany(ExtractNestedExpandAssociations));
            return node;
        }

        private ODataExpandAssociation AddNestedExpandAssociationAndGetDeepestChild(Expression nestedExpression)
        {
            var nestedAssociation = ExtractNestedExpandAssociations(nestedExpression).First();
            ExpandAssociations.Add(nestedAssociation);
            var deepestChildAssociation = nestedAssociation;
            while (deepestChildAssociation.ExpandAssociations.Any())
            {
                deepestChildAssociation = deepestChildAssociation.ExpandAssociations.First();
            }
            return deepestChildAssociation;
        }

        private IEnumerable<ODataExpandAssociation> ExtractNestedExpandAssociations(Expression expression)
        {
            var nestedExpandExpressionVisitor = new ExpandExpressionVisitor(_typeCache);
            nestedExpandExpressionVisitor.Visit(expression);
            return nestedExpandExpressionVisitor.ExpandAssociations;
        }
    }
}