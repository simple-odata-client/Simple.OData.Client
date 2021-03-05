using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using Simple.OData.Client.Extensions;

#pragma warning disable 1591

namespace Simple.OData.Client
{
    public partial class ODataExpression
    {
        private readonly ODataExpression _functionCaller;
        private readonly ODataExpression _left;
        private readonly ODataExpression _right;
        private readonly ExpressionType _operator = ExpressionType.Default;
        private readonly Type _conversionType;

        public string Reference { get; private set; }
        public object Value { get; private set; }
        public ExpressionFunction Function { get; private set; }
        public bool IsValueConversion { get { return _conversionType != null; } }

        internal ODataExpression()
        {
        }

        public ODataExpression(Expression expression)
            : this(FromLinqExpression(expression))
        {
        }

        protected internal ODataExpression(ODataExpression expression)
        {
            _functionCaller = expression._functionCaller;
            _left = expression._left;
            _right = expression._right;
            _operator = expression._operator;
            _conversionType = expression._conversionType;

            this.Reference = expression.Reference;
            this.Value = expression.Value;
            this.Function = expression.Function;
        }

        protected internal ODataExpression(object value)
        {
            this.Value = value;
        }

        protected internal ODataExpression(string reference)
        {
            this.Reference = reference;
        }

        protected internal ODataExpression(string reference, object value)
        {
            this.Reference = reference;
            this.Value = value;
        }

        protected internal ODataExpression(ExpressionFunction function)
        {
            this.Function = function;
        }

        protected internal ODataExpression(ODataExpression left, ODataExpression right, ExpressionType expressionOperator)
        {
            _left = left;
            _right = right;
            _operator = expressionOperator;
        }

        protected internal ODataExpression(ODataExpression caller, string reference)
        {
            _functionCaller = caller;
            this.Reference = reference;
        }

        protected internal ODataExpression(ODataExpression caller, ExpressionFunction function)
        {
            _functionCaller = caller;
            this.Function = function;
        }

        protected internal ODataExpression(ODataExpression expr, Type conversionType)
        {
            _conversionType = conversionType;
            this.Value = expr;
        }

        internal static ODataExpression FromReference(string reference)
        {
            return new ODataExpression(reference, (object)null);
        }

        internal static ODataExpression FromValue(object value)
        {
            return new ODataExpression(value);
        }

        internal static ODataExpression FromFunction(ExpressionFunction function)
        {
            return new ODataExpression(function);
        }

        internal static ODataExpression FromFunction(string functionName, ODataExpression targetExpression, IEnumerable<object> arguments)
        {
            return new ODataExpression(
                targetExpression,
                new ExpressionFunction(functionName, arguments));
        }

        internal static ODataExpression FromFunction(string functionName, ODataExpression targetExpression, IEnumerable<Expression> arguments)
        {
            return new ODataExpression(
                targetExpression,
                new ExpressionFunction(functionName, arguments));
        }

        internal static ODataExpression FromLinqExpression(Expression expression)
        {
            return ParseLinqExpression(expression);
        }

        public bool IsNull
        {
            get { return this.Value == null && 
                this.Reference == null && 
                this.Function == null &&
                _operator == ExpressionType.Default;
            }
        }

        public string AsString(ISession session)
        {
            return Format(new ExpressionContext(session));
        }

        private static readonly char[] _propertySeperator = {'.', '/'};
        internal bool ExtractLookupColumns(IDictionary<string, object> lookupColumns)
        {
            switch (_operator)
            {
                case ExpressionType.And:
                    var ok = _left.ExtractLookupColumns(lookupColumns);
                    if (ok)
                        ok = _right.ExtractLookupColumns(lookupColumns);
                    return ok;

                case ExpressionType.Equal:
                    var expr = this.IsValueConversion ? this : _left;
                    while (expr.IsValueConversion)
                    {
                        expr = expr.Value as ODataExpression;
                    }
                    if (!string.IsNullOrEmpty(expr.Reference))
                    {
                        if (expr.Reference.IndexOfAny(_propertySeperator) >= 0)
                        {
                            //skip child entity - may result in false positives
                            return false;
                        }
                        var key = expr.Reference;
                        if (key != null && !lookupColumns.ContainsKey(key))
                            lookupColumns.Add(key, _right);
                    }
                    return true;

                default:
                    if (this.IsValueConversion)
                    {
                        return (this.Value as ODataExpression).ExtractLookupColumns(lookupColumns);
                    }
                    else
                    {
                        return false;
                    }
            }
        }

        internal bool HasTypeConstraint(string typeName)
        {
            if (_operator == ExpressionType.And)
            {
                return _left.HasTypeConstraint(typeName) || _right.HasTypeConstraint(typeName);
            }
            else if (this.Function != null && this.Function.FunctionName == ODataLiteral.IsOf)
            {
                return this.Function.Arguments.Last().HasTypeConstraint(typeName);
            }
            else if (this.Value != null)
            {
                return this.Value is Type && (this.Value as Type).Name == typeName;
            }
            else
            {
                return false;
            }
        }
    }

    public partial class ODataExpression<T> : ODataExpression
    {
        public ODataExpression(Expression<Predicate<T>> predicate)
            : base(predicate)
        {
        }

        internal ODataExpression(ODataExpression expression)
            : base(expression)
        {
        }
    }
}
