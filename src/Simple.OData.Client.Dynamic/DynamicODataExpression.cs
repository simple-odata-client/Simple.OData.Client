using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Simple.OData.Client.Extensions;

#pragma warning disable 1591

namespace Simple.OData.Client
{
    public class DynamicODataExpression : ODataExpression,  IDynamicMetaObjectProvider
    {
        internal DynamicODataExpression()
        {
        }

        protected DynamicODataExpression(object value)
            : base(value)
        {
        }

        protected DynamicODataExpression(string reference)
            : base(reference)
        {
        }

        protected DynamicODataExpression(string reference, object value)
            : base(reference, value)
        {
        }

        protected DynamicODataExpression(ExpressionFunction function)
            : base(function)
        {
        }

        protected DynamicODataExpression(ODataExpression left, ODataExpression right, ExpressionType expressionOperator)
            : base(left, right, expressionOperator)
        {
        }

        protected DynamicODataExpression(ODataExpression caller, string reference)
            : base(caller, reference)
        {
        }

        protected DynamicODataExpression(ODataExpression caller, ExpressionFunction function)
            : base(caller, function)
        {
        }

        public DynamicMetaObject GetMetaObject(Expression parameter)
        {
            return new DynamicExpressionMetaObject(parameter, this);
        }

        private class DynamicExpressionMetaObject : DynamicMetaObject
        {
            internal DynamicExpressionMetaObject(
                Expression parameter,
                DynamicODataExpression value)
                : base(parameter, BindingRestrictions.Empty, value)
            {
            }

            public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
            {
                ConstructorInfo ctor;
                Expression[] ctorArguments;
                if (FunctionMapping.ContainsFunction(binder.Name, 0))
                {
                    ctor = CtorWithExpressionAndString;
                    ctorArguments = new Expression[] { Expression.Convert(Expression, LimitType), Expression.Constant(binder.Name) };
                }
                else
                {
                    Expression<Func<bool, ODataExpression, string>> calculateReference = (hv, e) => hv && !string.IsNullOrEmpty(e.Reference)
                        ? string.Join("/", e.Reference, binder.Name)
                        : binder.Name;
                    var referenceExpression = Expression.Invoke(calculateReference, Expression.Constant(HasValue), Expression.Convert(Expression, LimitType));
                    ctor = CtorWithString;
                    ctorArguments = new Expression[] { referenceExpression };
                }

                return new DynamicMetaObject(
                    Expression.New(ctor, ctorArguments),
                    BindingRestrictions.GetTypeRestriction(Expression, LimitType));
            }

            public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
            {
                var ctor = CtorWithStringAndValue;
                Expression objectExpression = Expression.Convert(value.Expression, typeof(object));
                var ctorArguments = new[] { Expression.Constant(binder.Name), objectExpression };

                return new DynamicMetaObject(
                    Expression.New(ctor, ctorArguments),
                    BindingRestrictions.GetTypeRestriction(Expression, LimitType));
            }

            public override DynamicMetaObject BindInvokeMember(
                InvokeMemberBinder binder, DynamicMetaObject[] args)
            {
                var expressionFunctionConstructor = typeof(ExpressionFunction).GetConstructor(new[] {typeof(string), typeof(IEnumerable<object>)});
                if (FunctionMapping.ContainsFunction(binder.Name, args.Length))
                {
                    var expression = Expression.New(CtorWithExpressionAndExpressionFunction, 
                        Expression.Convert(Expression, LimitType), 
                        Expression.New(expressionFunctionConstructor, 
                            Expression.Constant(binder.Name), 
                            Expression.NewArrayInit(typeof(object), args.Select(x => Expression.Convert(x.Expression, typeof(object))))));

                    return new DynamicMetaObject(
                        expression,
                        BindingRestrictions.GetTypeRestriction(Expression, LimitType));
                }
                if (string.Equals(binder.Name, ODataLiteral.Any, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(binder.Name, ODataLiteral.All, StringComparison.OrdinalIgnoreCase))
                {
                    var expression = Expression.New(CtorWithExpressionAndExpressionFunction, 
                        Expression.Convert(Expression, LimitType), 
                        Expression.New(expressionFunctionConstructor, 
                            Expression.Constant(binder.Name), 
                            Expression.NewArrayInit(typeof(object), args.Select(x => Expression.Convert(x.Expression, typeof(object))))));

                    return new DynamicMetaObject(
                        expression,
                        BindingRestrictions.GetTypeRestriction(Expression, LimitType));
                }
                if (string.Equals(binder.Name, ODataLiteral.IsOf, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(binder.Name, ODataLiteral.Is, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(binder.Name, ODataLiteral.Cast, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(binder.Name, ODataLiteral.As, StringComparison.OrdinalIgnoreCase))
                {
                    var functionName = string.Equals(binder.Name, ODataLiteral.Is, StringComparison.OrdinalIgnoreCase)
                        ? ODataLiteral.IsOf
                        : string.Equals(binder.Name, ODataLiteral.As, StringComparison.OrdinalIgnoreCase)
                            ? ODataLiteral.Cast
                            : binder.Name;

                    var isNullProperty = typeof(DynamicODataExpression).GetProperty(nameof(IsNull));
                    var expressionFunctionArguments = Expression.Condition(Expression.MakeMemberAccess(Expression.Convert(Expression, LimitType), isNullProperty),
                        Expression.Convert(Expression, typeof(object)),
                        Expression.Convert(args.First().Expression, typeof(object)));
                    
                    var expression = Expression.New(CtorWithExpressionAndExpressionFunction, 
                        Expression.Convert(Expression, LimitType),
                        Expression.New(expressionFunctionConstructor, 
                            Expression.Constant(functionName), 
                            Expression.NewArrayInit(typeof(object), expressionFunctionArguments)));

                    return new DynamicMetaObject(
                        expression,
                        BindingRestrictions.GetTypeRestriction(Expression, LimitType));
                }

                return base.BindInvokeMember(binder, args);
            }

            public override DynamicMetaObject BindBinaryOperation(BinaryOperationBinder binder, DynamicMetaObject arg)
            {
                if (arg.RuntimeType != null && arg.RuntimeType.IsEnumType())
                {
                    var ctor = typeof(ODataExpression).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.NonPublic, null, new[] {typeof(object)}, null);
                    var expression = Expression.New(CtorWithExpressionAndExpressionAndOperator, 
                        Expression.Convert(Expression, LimitType), 
                        Expression.New(ctor, Expression.Convert(arg.Expression, typeof(object))), 
                        Expression.Constant(binder.Operation));

                    return new DynamicMetaObject(
                        expression,
                        BindingRestrictions.GetTypeRestriction(Expression, LimitType));
                }

                return base.BindBinaryOperation(binder, arg);
            }
        }

        private static IEnumerable<ConstructorInfo> GetConstructorInfo()
        {
            return _ctors ?? (_ctors = typeof(DynamicODataExpression).GetDeclaredConstructors().ToArray());
        }

        private static ConstructorInfo CtorWithString
        {
            get
            {
                return _ctorWithString ??
                    (_ctorWithString = GetConstructorInfo().Single(x =>
                    x.GetParameters().Count() == 1 &&
                    x.GetParameters()[0].ParameterType == typeof(string)));
            }
        }

        private static ConstructorInfo CtorWithStringAndValue
        {
            get
            {
                return _ctorWithStringAndStringAndValue ??
                    (_ctorWithStringAndStringAndValue = GetConstructorInfo().Single(x =>
                    x.GetParameters().Count() == 2 &&
                    x.GetParameters()[0].ParameterType == typeof(string) &&
                    x.GetParameters()[1].ParameterType == typeof(object)));
            }
        }

        private static ConstructorInfo CtorWithExpressionAndString
        {
            get
            {
                return _ctorWithExpressionAndString ??
                       (_ctorWithExpressionAndString = GetConstructorInfo().Single(x =>
                           x.GetParameters().Count() == 2 &&
                           x.GetParameters()[0].ParameterType == typeof(ODataExpression) &&
                           x.GetParameters()[1].ParameterType == typeof(string)));
            }
        }

        private static ConstructorInfo CtorWithExpressionAndExpressionFunction
        {
            get
            {
                return _ctorWithExpressionAndFunction ??
                       (_ctorWithExpressionAndFunction = GetConstructorInfo().Single(x =>
                           x.GetParameters().Count() == 2 &&
                           x.GetParameters()[0].ParameterType == typeof(ODataExpression) &&
                           x.GetParameters()[1].ParameterType == typeof(ExpressionFunction)));
            }
        }

        private static ConstructorInfo CtorWithExpressionAndExpressionAndOperator
        {
            get
            {
                return _ctorWithExpressionAndExpressionAndOperator ??
                       (_ctorWithExpressionAndExpressionAndOperator = GetConstructorInfo().Single(x =>
                           x.GetParameters().Count() == 3 &&
                           x.GetParameters()[0].ParameterType == typeof(ODataExpression) &&
                           x.GetParameters()[1].ParameterType == typeof(ODataExpression) &&
                           x.GetParameters()[2].ParameterType == typeof(ExpressionType)));
            }
        }

        private static ConstructorInfo[] _ctors;
        private static ConstructorInfo _ctorWithString;
        private static ConstructorInfo _ctorWithStringAndStringAndValue;
        private static ConstructorInfo _ctorWithExpressionAndString;
        private static ConstructorInfo _ctorWithExpressionAndFunction;
        private static ConstructorInfo _ctorWithExpressionAndExpressionAndOperator;
    }
}
