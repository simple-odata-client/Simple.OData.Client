using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Simple.OData.Client.Extensions;

namespace Simple.OData.Client;

public partial class ODataExpression
{
	private static ODataExpression ParseLinqExpression(Expression expression)
	{
		return expression.NodeType switch
		{
			ExpressionType.MemberAccess => ParseMemberExpression(expression),
			ExpressionType.Call => ParseCallExpression(expression),
			ExpressionType.Lambda => ParseLambdaExpression(expression),
			ExpressionType.Constant => ParseConstantExpression(expression),
			ExpressionType.Not or ExpressionType.Convert or ExpressionType.Negate => ParseUnaryExpression(expression),
			ExpressionType.Equal or ExpressionType.NotEqual or ExpressionType.LessThan or ExpressionType.LessThanOrEqual or ExpressionType.GreaterThan or ExpressionType.GreaterThanOrEqual or ExpressionType.And or ExpressionType.AndAlso or ExpressionType.Or or ExpressionType.OrElse or ExpressionType.Add or ExpressionType.AddChecked or ExpressionType.Subtract or ExpressionType.SubtractChecked or ExpressionType.Multiply or ExpressionType.MultiplyChecked or ExpressionType.Divide or ExpressionType.Modulo => ParseBinaryExpression(expression),
			ExpressionType.TypeIs => ParseTypeIsExpression(expression),
			ExpressionType.TypeAs => ParseTypeAsExpression(expression),
			ExpressionType.Parameter => ParseTypedParameterExpression(),
			ExpressionType.New => ParseNewExpression(expression),
			_ => throw Utils.NotSupportedExpression(expression),
		};
	}

	private static ODataExpression ParseMemberExpression(
		Expression expression,
		Stack<MemberInfo>? memberChain = null)
	{
		var memberExpression = expression as MemberExpression;
		if (memberExpression.Expression == null)
		{
			return new ODataExpression(EvaluateStaticMember(memberExpression));
		}
		else
		{
			memberChain ??= new Stack<MemberInfo>();
			memberChain.Push(memberExpression.Member);
			switch (memberExpression.Expression.NodeType)
			{
				case ExpressionType.Parameter:
					// NOTE: Can't support ITypeCache here as we might be dealing with dynamic types/expressions
					var memberNames = string.Join(".", memberChain.Select(x => x.GetMappedName()));
					return FromReference(memberNames);
				case ExpressionType.Constant:
					return ParseConstantExpression(memberExpression.Expression, memberChain);
				case ExpressionType.MemberAccess:
					// NOTE: Can't support ITypeCache here as we might be dealing with dynamic types/expressions
					var memberName = memberExpression.Member.GetMappedName();
					if (FunctionMapping.ContainsFunction(memberName, 0))
					{
						return FromFunction(memberName, ParseMemberExpression(memberExpression.Expression), new List<object>());
					}
					else
					{
						return ParseMemberExpression(memberExpression.Expression as MemberExpression, memberChain);
					}

				default:
					throw Utils.NotSupportedExpression(expression);
			}
		}
	}

	private static ODataExpression ParseCallExpression(Expression expression)
	{
		var callExpression = expression as MethodCallExpression;
		if (callExpression.Object == null)
		{
			var target = callExpression.Arguments.FirstOrDefault();
			if (target != null)
			{
				var callArguments = string.Equals(callExpression.Method.DeclaringType.FullName, "System.Convert", StringComparison.Ordinal)
					? callExpression.Arguments
					: callExpression.Arguments.Skip(1);
				return FromFunction(callExpression.Method.Name, ParseLinqExpression(target), callArguments);
			}

			throw Utils.NotSupportedExpression(expression);
		}

		var arguments = new List<object>();
		arguments.AddRange(callExpression.Arguments.Select(ParseCallArgumentExpression));
		switch (callExpression.Object.NodeType)
		{
			case ExpressionType.MemberAccess:
				var memberExpression = callExpression.Object as MemberExpression;
				return new ODataExpression(
					ParseMemberExpression(memberExpression),
					new ExpressionFunction(callExpression.Method.Name, arguments));

			case ExpressionType.Call:
				if (string.Equals(callExpression.Method.Name, nameof(object.ToString), StringComparison.Ordinal))
				{
					return ParseCallExpression(callExpression.Object);
				}
				else
				{
					return new ODataExpression(
						new ODataExpression(callExpression.Object),
						new ExpressionFunction(callExpression.Method.Name, callExpression.Arguments));
				}

			case ExpressionType.Constant:
				return new ODataExpression(ParseConstantExpression(callExpression.Object),
					new ExpressionFunction(callExpression.Method.Name, arguments));
		}

		throw Utils.NotSupportedExpression(callExpression.Object);
	}

	private static ODataExpression ParseArrayExpression(Expression expression)
	{
		var binaryEpression = expression as BinaryExpression;
		var arrayExpression = ParseMemberExpression(binaryEpression.Left);
		var indexExpression = ParseConstantExpression(binaryEpression.Right);

		return FromValue((arrayExpression.Value as Array).GetValue(int.Parse(indexExpression.Value.ToString(), CultureInfo.InvariantCulture)));
	}

	private static ODataExpression ParseCallArgumentExpression(Expression expression)
	{
		return expression.NodeType switch
		{
			ExpressionType.Constant => ParseConstantExpression(expression),
			ExpressionType.MemberAccess => ParseMemberExpression(expression),
			ExpressionType.ArrayIndex => ParseArrayExpression(expression),
			ExpressionType.Convert => ParseUnaryExpression(expression),
			ExpressionType.Call => ParseCallExpression(expression),
			_ => throw Utils.NotSupportedExpression(expression),
		};
	}

	private static ODataExpression ParseLambdaExpression(Expression expression)
	{
		var lambdaExpression = expression as LambdaExpression;

		return ParseLinqExpression(lambdaExpression.Body);
	}

	private static ODataExpression ParseConstantExpression(
		Expression expression,
		Stack<MemberInfo>? members = null)
	{
		var constExpression = expression as ConstantExpression;

		if (constExpression.Value is Expression)
		{
			return ParseConstantExpression(constExpression.Value as Expression, members);
		}
		else
		{
			if (constExpression.Type.IsValue() || constExpression.Type == typeof(string))
			{
				return new ODataExpression(constExpression.Value);
			}
			else
			{
				return new ODataExpression(EvaluateConstValue(constExpression.Value,
					members ?? new Stack<MemberInfo>()));
			}
		}
	}

	private static ODataExpression ParseUnaryExpression(Expression expression)
	{
		var unaryExpression = (expression as UnaryExpression).Operand;
		var odataExpression = ParseLinqExpression(unaryExpression);
		return expression.NodeType switch
		{
			ExpressionType.Not => !odataExpression,
			ExpressionType.Convert => new ODataExpression(odataExpression, expression.Type),
			ExpressionType.Negate => -odataExpression,
			_ => throw Utils.NotSupportedExpression(expression),
		};
	}

	private static ODataExpression ParseBinaryExpression(Expression expression)
	{
		var binaryExpression = expression as BinaryExpression;

		var leftExpression = ParseLinqExpression(binaryExpression.Left);
		var rightExpression = ParseLinqExpression(binaryExpression.Right);

		if (IsConvertFromCustomEnum(binaryExpression.Left, out var enumType))
		{
			return ParseBinaryExpression(leftExpression, ParseLinqExpression(Expression.Convert(binaryExpression.Right, enumType)), expression);
		}
		else if (IsConvertFromCustomEnum(binaryExpression.Right, out enumType))
		{
			return ParseBinaryExpression(ParseLinqExpression(Expression.Convert(binaryExpression.Left, enumType)), rightExpression, expression);
		}

		return ParseBinaryExpression(leftExpression, rightExpression, expression);
	}

	private static ODataExpression ParseBinaryExpression(ODataExpression leftExpression, ODataExpression rightExpression, Expression operandExpression)
	{
		return operandExpression.NodeType switch
		{
			ExpressionType.Equal => leftExpression == rightExpression,
			ExpressionType.NotEqual => leftExpression != rightExpression,
			ExpressionType.LessThan => leftExpression < rightExpression,
			ExpressionType.LessThanOrEqual => leftExpression <= rightExpression,
			ExpressionType.GreaterThan => leftExpression > rightExpression,
			ExpressionType.GreaterThanOrEqual => leftExpression >= rightExpression,
			ExpressionType.And or ExpressionType.AndAlso => leftExpression && rightExpression,
			ExpressionType.Or or ExpressionType.OrElse => leftExpression || rightExpression,
			ExpressionType.Add or ExpressionType.AddChecked => leftExpression + rightExpression,
			ExpressionType.Subtract or ExpressionType.SubtractChecked => leftExpression - rightExpression,
			ExpressionType.Multiply or ExpressionType.MultiplyChecked => leftExpression * rightExpression,
			ExpressionType.Divide => leftExpression / rightExpression,
			ExpressionType.Modulo => leftExpression % rightExpression,
			_ => throw Utils.NotSupportedExpression(operandExpression),
		};
	}

	private static bool IsConvertFromCustomEnum(Expression expression, out Type? enumType)
	{
		enumType = null;
		if (expression.NodeType == ExpressionType.Convert)
		{
			var unaryExpression = (expression as UnaryExpression).Operand;
			enumType = unaryExpression.Type;
			return enumType.IsEnumType() && !Utils.IsSystemType(enumType);
		}

		return false;
	}

	private static ODataExpression ParseTypeIsExpression(Expression expression)
	{
		var typeIsExpression = expression as TypeBinaryExpression;
		var targetExpression = ParseLinqExpression(typeIsExpression.Expression);
		return FromFunction(
			new ExpressionFunction()
			{
				FunctionName = "isof",
				Arguments = new List<ODataExpression>() { targetExpression, FromValue(typeIsExpression.TypeOperand) },
			});
	}

	private static ODataExpression ParseTypeAsExpression(Expression expression)
	{
		var typeAsExpression = expression as UnaryExpression;
		var targetExpression = ParseLinqExpression(typeAsExpression.Operand);
		return FromFunction(
			new ExpressionFunction()
			{
				FunctionName = "cast",
				Arguments = new List<ODataExpression>() { targetExpression, FromValue(typeAsExpression.Type) },
			});
	}

	private static ODataExpression ParseTypedParameterExpression()
	{
		return null;
	}

	private static ODataExpression ParseNewExpression(Expression expression)
	{
		var newExpression = expression as NewExpression;
		return FromValue(newExpression.Constructor.Invoke(newExpression.Arguments.Select(x => ParseLinqExpression(x).Value).ToArray()));
	}

	private static object EvaluateStaticMember(MemberExpression expression)
	{
		object? value = null;
		switch (expression.Member)
		{
			case FieldInfo field:
				value = field.GetValue(null);
				break;
			case PropertyInfo property:
				if (property.GetIndexParameters().Length != 0)
				{
					throw new ArgumentException("cannot eliminate closure references to indexed properties.");
				}

				value = property.GetValueEx(null);
				break;
		}

		return value;
	}

	private static object EvaluateConstValue(object value, Stack<MemberInfo> memberChain)
	{
		if (!memberChain.Any())
		{
			return value;
		}

		var member = memberChain.Pop();

		object itemValue;
		switch (member)
		{
			case PropertyInfo property:
				if (property.GetIndexParameters().Length != 0)
				{
					throw new ArgumentException("cannot evaluate constant value of indexed properties.");
				}

				itemValue = property.GetValueEx(value);
				break;
			case FieldInfo field:
				itemValue = field.GetValueEx(value);
				break;
			default:
				return value;
		}

		return EvaluateConstValue(itemValue, memberChain);
	}
}
