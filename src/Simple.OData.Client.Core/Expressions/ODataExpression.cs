using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Simple.OData.Client;

public partial class ODataExpression
{
	private readonly ODataExpression _functionCaller;
	private readonly ODataExpression _left;
	private readonly ODataExpression _right;
	private readonly ExpressionType _operator = ExpressionType.Default;
	private readonly Type _conversionType;

	public string? Reference { get; private set; }
	public object? Value { get; private set; }
	public ExpressionFunction? Function { get; private set; }
	public bool IsValueConversion => _conversionType != null;

	internal ODataExpression()
	{
	}

	public ODataExpression(Expression expression)
		: this(FromLinqExpression(expression))
	{
	}

	internal protected ODataExpression(ODataExpression expression)
	{
		_functionCaller = expression._functionCaller;
		_left = expression._left;
		_right = expression._right;
		_operator = expression._operator;
		_conversionType = expression._conversionType;

		Reference = expression.Reference;
		Value = expression.Value;
		Function = expression.Function;
	}

	internal protected ODataExpression(object value)
	{
		Value = value;
	}

	internal protected ODataExpression(string reference)
	{
		Reference = reference;
	}

	internal protected ODataExpression(string reference, object value)
	{
		Reference = reference;
		Value = value;
	}

	internal protected ODataExpression(ExpressionFunction function)
	{
		Function = function;
	}

	internal protected ODataExpression(ODataExpression left, ODataExpression right, ExpressionType expressionOperator)
	{
		_left = left;
		_right = right;
		_operator = expressionOperator;
	}

	internal protected ODataExpression(ODataExpression caller, string reference)
	{
		_functionCaller = caller;
		Reference = reference;
	}

	internal protected ODataExpression(ODataExpression caller, ExpressionFunction function)
	{
		_functionCaller = caller;
		Function = function;
	}

	internal protected ODataExpression(ODataExpression expr, Type conversionType)
	{
		_conversionType = conversionType;
		Value = expr;
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

	public bool IsNull => Value == null &&
		  Reference == null &&
		  Function == null &&
		  _operator == ExpressionType.Default;

	public string AsString(ISession session)
	{
		return Format(new ExpressionContext(session));
	}

	private static readonly char[] _propertySeperator = { '.', '/' };
	internal bool ExtractLookupColumns(IDictionary<string, object> lookupColumns)
	{
		switch (_operator)
		{
			case ExpressionType.And:
				var ok = _left.ExtractLookupColumns(lookupColumns);
				if (ok)
				{
					ok = _right.ExtractLookupColumns(lookupColumns);
				}

				return ok;

			case ExpressionType.Equal:
				var expr = IsValueConversion ? this : _left;
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
					{
						lookupColumns.Add(key, _right);
					}
				}

				return true;

			default:
				if (IsValueConversion)
				{
					return (Value as ODataExpression).ExtractLookupColumns(lookupColumns);
				}
				else
				{
					return false;
				}
		}
	}

	internal bool HasTypeConstraint(string? typeName)
	{
		if (_operator == ExpressionType.And)
		{
			return _left.HasTypeConstraint(typeName) || _right.HasTypeConstraint(typeName);
		}
		else if (Function != null && Function.FunctionName == ODataLiteral.IsOf)
		{
			return Function.Arguments.Last().HasTypeConstraint(typeName);
		}
		else if (Value != null)
		{
			return Value is Type valueType && valueType.Name == typeName;
		}
		else
		{
			return false;
		}
	}

	public override bool Equals(object obj)
	{
		return obj is ODataExpression expression &&
			   EqualityComparer<ODataExpression>.Default.Equals(_functionCaller, expression._functionCaller) &&
			   EqualityComparer<ODataExpression>.Default.Equals(_left, expression._left) &&
			   EqualityComparer<ODataExpression>.Default.Equals(_right, expression._right) &&
			   _operator == expression._operator &&
			   EqualityComparer<Type>.Default.Equals(_conversionType, expression._conversionType) &&
			   Reference == expression.Reference &&
			   EqualityComparer<object>.Default.Equals(Value, expression.Value) &&
			   EqualityComparer<ExpressionFunction>.Default.Equals(Function, expression.Function) &&
			   IsValueConversion == expression.IsValueConversion &&
			   IsNull == expression.IsNull;
	}

	public override int GetHashCode()
	{
		var hashCode = -1908471857;
		hashCode = hashCode * -1521134295 + EqualityComparer<ODataExpression>.Default.GetHashCode(_functionCaller);
		hashCode = hashCode * -1521134295 + EqualityComparer<ODataExpression>.Default.GetHashCode(_left);
		hashCode = hashCode * -1521134295 + EqualityComparer<ODataExpression>.Default.GetHashCode(_right);
		hashCode = hashCode * -1521134295 + _operator.GetHashCode();
		hashCode = hashCode * -1521134295 + EqualityComparer<Type>.Default.GetHashCode(_conversionType);
		hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Reference);
		hashCode = hashCode * -1521134295 + EqualityComparer<object>.Default.GetHashCode(Value);
		hashCode = hashCode * -1521134295 + EqualityComparer<ExpressionFunction>.Default.GetHashCode(Function);
		hashCode = hashCode * -1521134295 + IsValueConversion.GetHashCode();
		hashCode = hashCode * -1521134295 + IsNull.GetHashCode();
		return hashCode;
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

	public override bool Equals(object obj)
	{
		return obj is ODataExpression<T> expression &&
			   base.Equals(obj) &&
			   Reference == expression.Reference &&
			   EqualityComparer<object>.Default.Equals(Value, expression.Value) &&
			   EqualityComparer<ExpressionFunction>.Default.Equals(Function, expression.Function) &&
			   IsValueConversion == expression.IsValueConversion &&
			   IsNull == expression.IsNull;
	}

	public override int GetHashCode()
	{
		var hashCode = 223019041;
		hashCode = hashCode * -1521134295 + base.GetHashCode();
		hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Reference);
		hashCode = hashCode * -1521134295 + EqualityComparer<object>.Default.GetHashCode(Value);
		hashCode = hashCode * -1521134295 + EqualityComparer<ExpressionFunction>.Default.GetHashCode(Function);
		hashCode = hashCode * -1521134295 + IsValueConversion.GetHashCode();
		hashCode = hashCode * -1521134295 + IsNull.GetHashCode();
		return hashCode;
	}
}
