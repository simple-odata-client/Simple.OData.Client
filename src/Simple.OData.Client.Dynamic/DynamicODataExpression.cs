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
                    ctorArguments = new[] { Expression.Constant(this.Value), Expression.Constant(binder.Name) };
                }
                else
                {
                    var reference = this.HasValue && !string.IsNullOrEmpty((this.Value as ODataExpression).Reference)
                        ? string.Join("/", (this.Value as ODataExpression).Reference, binder.Name)
                        : binder.Name;
                    ctor = CtorWithString;
                    ctorArguments = new[] { Expression.Constant(reference) };
                }

                return new DynamicMetaObject(
                    Expression.New(ctor, ctorArguments),
                    BindingRestrictions.GetTypeRestriction(Expression, LimitType));
            }

            public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
            {
                var ctor = CtorWithStringAndValue;
				Expression objectExpression = value.Expression;
				if ((value.Value != null && value.Value.GetType().IsValue()) ||
					(value.Value == null && Nullable.GetUnderlyingType(objectExpression.Type) != null))
				{
					objectExpression = Expression.Convert(objectExpression, typeof(object));
				}
                var ctorArguments = new[] { Expression.Constant(binder.Name), objectExpression };

                return new DynamicMetaObject(
                    Expression.New(ctor, ctorArguments),
                    BindingRestrictions.GetTypeRestriction(Expression, LimitType));
            }

            public override DynamicMetaObject BindInvokeMember(
                InvokeMemberBinder binder, DynamicMetaObject[] args)
            {
                if (FunctionMapping.ContainsFunction(binder.Name, args.Count()))
                {
                    var expression = Expression.New(CtorWithExpressionAndExpressionFunction,
                        new[]
                        {
                            Expression.Constant(this.Value), 
                            Expression.Constant(new ExpressionFunction(binder.Name, args.Select(x => x.Value)))
                        });

                    return new DynamicMetaObject(
                        expression,
                        BindingRestrictions.GetTypeRestriction(Expression, LimitType));
                }
                else if (string.Equals(binder.Name, ODataLiteral.Any, StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(binder.Name, ODataLiteral.All, StringComparison.OrdinalIgnoreCase))
                {
                    var expression = Expression.New(CtorWithExpressionAndExpressionFunction,
                        new[]
                        {
                            Expression.Constant(this.Value), 
                            Expression.Constant(new ExpressionFunction(binder.Name, args.Select(x => x.Value)))
                        });

                    return new DynamicMetaObject(
                        expression,
                        BindingRestrictions.GetTypeRestriction(Expression, LimitType));
                }
                else if (string.Equals(binder.Name, ODataLiteral.IsOf, StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(binder.Name, ODataLiteral.Is, StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(binder.Name, ODataLiteral.Cast, StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(binder.Name, ODataLiteral.As, StringComparison.OrdinalIgnoreCase))
                {
                    var functionName = string.Equals(binder.Name, ODataLiteral.Is, StringComparison.OrdinalIgnoreCase)
                        ? ODataLiteral.IsOf
                        : string.Equals(binder.Name, ODataLiteral.As, StringComparison.OrdinalIgnoreCase)
                            ? ODataLiteral.Cast
                            : binder.Name;
                    var expression = Expression.New(CtorWithExpressionAndExpressionFunction,
                        new[]
                        {
                            Expression.Constant(this.Value), 
                            Expression.Constant(new ExpressionFunction(
                                functionName, 
                                new [] { (this.Value as ODataExpression).IsNull ? null : this.Value, args.First().Value }))
                        });

                    return new DynamicMetaObject(
                        expression,
                        BindingRestrictions.GetTypeRestriction(Expression, LimitType));
                }
                else
                {
                    return base.BindInvokeMember(binder, args);
                }
            }

            public override DynamicMetaObject BindBinaryOperation(BinaryOperationBinder binder, DynamicMetaObject arg)
            {
                if (arg.RuntimeType != null && arg.RuntimeType.IsEnumType())
                {
                    var expression = Expression.New(CtorWithExpressionAndExpressionAndOperator,
                        new[]
                        {
                            Expression.Constant(this.Value), 
                            Expression.Constant(new ODataExpression(arg.Value)),
                            Expression.Constant(binder.Operation)
                        });

                    return new DynamicMetaObject(
                        expression,
                        BindingRestrictions.GetTypeRestriction(Expression, LimitType));
                }
                else
                {
                    return base.BindBinaryOperation(binder, arg);
                }
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
