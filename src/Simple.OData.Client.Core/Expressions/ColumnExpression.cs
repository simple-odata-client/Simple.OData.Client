using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Simple.OData.Client
{
    internal static class ColumnExpression
    {
        public static IEnumerable<string> ExtractColumnNames<T>(this Expression<Func<T, object>> expression, ITypeCache typeCache)
        {
            var lambdaExpression = expression as LambdaExpression;
            if (lambdaExpression == null)
                throw Utils.NotSupportedExpression(expression);

            switch (lambdaExpression.Body.NodeType)
            {
                case ExpressionType.MemberAccess:
                case ExpressionType.Convert:
                    return ExtractColumnNames(lambdaExpression.Body, typeCache);

                case ExpressionType.Call:
                    return ExtractColumnNames(lambdaExpression.Body as MethodCallExpression, typeCache);

                case ExpressionType.New:
                    return ExtractColumnNames(lambdaExpression.Body as NewExpression, typeCache);

                default:
                    throw Utils.NotSupportedExpression(lambdaExpression.Body);
            }
        }

        public static string ExtractColumnName(this Expression expression, ITypeCache typeCache)
        {
            return expression.ExtractColumnNames(typeCache).Single();
        }

        public static IEnumerable<string> ExtractColumnNames(this Expression expression, ITypeCache typeCache)
        {
            switch (expression.NodeType)
            {
                case ExpressionType.MemberAccess:
                    return ExtractColumnNames(expression as MemberExpression, typeCache);

                case ExpressionType.Convert:
                    return ExtractColumnNames((expression as UnaryExpression).Operand, typeCache);

                case ExpressionType.Lambda:
                    return ExtractColumnNames((expression as LambdaExpression).Body, typeCache);

                case ExpressionType.Call:
                    return ExtractColumnNames(expression as MethodCallExpression, typeCache);

                case ExpressionType.New:
                    return ExtractColumnNames(expression as NewExpression, typeCache);

                default:
                    throw Utils.NotSupportedExpression(expression);
            }
        }

        private static IEnumerable<string> ExtractColumnNames(MethodCallExpression callExpression, ITypeCache typeCache)
        {
            if (callExpression.Method.Name == "Select" && callExpression.Arguments.Count == 2)
            {
                return ExtractColumnNames(callExpression.Arguments[0], typeCache)
                    .SelectMany(x => ExtractColumnNames(callExpression.Arguments[1], typeCache)
                        .Select(y => string.Join("/", x, y)));
            }
            else if (callExpression.Method.Name == "OrderBy" && callExpression.Arguments.Count == 2)
            {
                if (callExpression.Arguments[0] is MethodCallExpression && ((callExpression.Arguments[0] as MethodCallExpression).Method.Name == "Select"))
                    throw Utils.NotSupportedExpression(callExpression);

                return ExtractColumnNames(callExpression.Arguments[0], typeCache)
                    .SelectMany(x => ExtractColumnNames(callExpression.Arguments[1], typeCache)
                        .OrderBy(y => string.Join("/", x, y)));
            }
            else if (callExpression.Method.Name == "OrderByDescending" && callExpression.Arguments.Count == 2)
            {
                if (callExpression.Arguments[0] is MethodCallExpression && ((callExpression.Arguments[0] as MethodCallExpression).Method.Name == "Select"))
                    throw Utils.NotSupportedExpression(callExpression);

                return ExtractColumnNames(callExpression.Arguments[0], typeCache)
                    .SelectMany(x => ExtractColumnNames(callExpression.Arguments[1], typeCache)
                        .OrderByDescending(y => string.Join("/", x, y)));
            }
            else
            {
                throw Utils.NotSupportedExpression(callExpression);
            }
        }

        private static IEnumerable<string> ExtractColumnNames(NewExpression newExpression, ITypeCache typeCache)
        {
            return newExpression.Arguments.SelectMany(x => ExtractColumnNames(x, typeCache));
        }

        private static IEnumerable<string> ExtractColumnNames(MemberExpression memberExpression, ITypeCache typeCache)
        {
            var memberName = typeCache.GetMappedName(memberExpression.Expression.Type, memberExpression.Member.Name);

            //var memberName = memberExpression.Expression.Type
            //    .GetNamedProperty(memberExpression.Member.Name)
            //    .GetMappedName();

            return memberExpression.Expression is MemberExpression
                ? memberExpression.Expression.ExtractColumnNames(typeCache).Select(x => string.Join("/", x, memberName))
                : new[] { memberName };
        }
    }
}