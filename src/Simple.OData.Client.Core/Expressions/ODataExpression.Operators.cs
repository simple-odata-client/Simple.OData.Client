using System;
using System.Linq.Expressions;


/* Unmerged change from project 'Simple.OData.Client.Core (netstandard2.0)'
Before:
#pragma warning disable 0660,0661
#pragma warning disable 1591
After:
#pragma warning disable 0660, 0661
*/
namespace Simple.OData.Client;

public partial class ODataExpression
{
	public static implicit operator ODataExpression(bool value) { return FromValue(value); }
	public static implicit operator ODataExpression(byte value) { return FromValue(value); }
	public static implicit operator ODataExpression(sbyte value) { return FromValue(value); }
	public static implicit operator ODataExpression(short value) { return FromValue(value); }
	public static implicit operator ODataExpression(ushort value) { return FromValue(value); }
	public static implicit operator ODataExpression(int value) { return FromValue(value); }
	public static implicit operator ODataExpression(uint value) { return FromValue(value); }
	public static implicit operator ODataExpression(long value) { return FromValue(value); }
	public static implicit operator ODataExpression(ulong value) { return FromValue(value); }
	public static implicit operator ODataExpression(float value) { return FromValue(value); }
	public static implicit operator ODataExpression(double value) { return FromValue(value); }
	public static implicit operator ODataExpression(decimal value) { return FromValue(value); }
	public static implicit operator ODataExpression(DateTime value) { return FromValue(value); }
	public static implicit operator ODataExpression(DateTimeOffset value) { return FromValue(value); }
	public static implicit operator ODataExpression(TimeSpan value) { return FromValue(value); }
	public static implicit operator ODataExpression(Guid value) { return FromValue(value); }
	public static implicit operator ODataExpression(string value) { return FromValue(value); }

	public static ODataExpression operator !(ODataExpression expr)
	{
		return new ODataExpression(expr, null, ExpressionType.Not);
	}

	public static ODataExpression operator -(ODataExpression expr)
	{
		return new ODataExpression(expr, null, ExpressionType.Negate);
	}

	public static ODataExpression operator ==(ODataExpression expr1, ODataExpression expr2)
	{
		return new ODataExpression(expr1, expr2, ExpressionType.Equal);
	}

	public static ODataExpression operator !=(ODataExpression expr1, ODataExpression expr2)
	{
		return new ODataExpression(expr1, expr2, ExpressionType.NotEqual);
	}

	public static ODataExpression operator &(ODataExpression expr1, ODataExpression expr2)
	{
		return new ODataExpression(expr1, expr2, ExpressionType.And);
	}

	public static ODataExpression operator |(ODataExpression expr1, ODataExpression expr2)
	{
		return new ODataExpression(expr1, expr2, ExpressionType.Or);
	}

	public static ODataExpression operator >(ODataExpression expr1, ODataExpression expr2)
	{
		return new ODataExpression(expr1, expr2, ExpressionType.GreaterThan);
	}

	public static ODataExpression operator <(ODataExpression expr1, ODataExpression expr2)
	{
		return new ODataExpression(expr1, expr2, ExpressionType.LessThan);
	}

	public static ODataExpression operator >=(ODataExpression expr1, ODataExpression expr2)
	{
		return new ODataExpression(expr1, expr2, ExpressionType.GreaterThanOrEqual);
	}

	public static ODataExpression operator <=(ODataExpression expr1, ODataExpression expr2)
	{
		return new ODataExpression(expr1, expr2, ExpressionType.LessThanOrEqual);
	}

	public static ODataExpression operator +(ODataExpression expr1, ODataExpression expr2)
	{
		return new ODataExpression(expr1, expr2, ExpressionType.Add);
	}

	public static ODataExpression operator -(ODataExpression expr1, ODataExpression expr2)
	{
		return new ODataExpression(expr1, expr2, ExpressionType.Subtract);
	}

	public static ODataExpression operator *(ODataExpression expr1, ODataExpression expr2)
	{
		return new ODataExpression(expr1, expr2, ExpressionType.Multiply);
	}

	public static ODataExpression operator /(ODataExpression expr1, ODataExpression expr2)
	{
		return new ODataExpression(expr1, expr2, ExpressionType.Divide);
	}

	public static ODataExpression operator %(ODataExpression expr1, ODataExpression expr2)
	{
		return new ODataExpression(expr1, expr2, ExpressionType.Modulo);
	}

	public static bool operator true(ODataExpression expr)
	{
		return false;
	}

	public static bool operator false(ODataExpression expr)
	{
		return false;
	}
}

public partial class ODataExpression<T>
{

	public static ODataExpression<T> operator !(ODataExpression<T> expr)
	{
		return new ODataExpression<T>(new ODataExpression(expr, null, ExpressionType.Not));
	}

	public static ODataExpression<T> operator -(ODataExpression<T> expr)
	{
		return new ODataExpression<T>(new ODataExpression(expr, null, ExpressionType.Negate));
	}

	public static ODataExpression<T> operator ==(ODataExpression<T> expr1, ODataExpression<T> expr2)
	{
		return new ODataExpression<T>(new ODataExpression(expr1, expr2, ExpressionType.Equal));
	}

	public static ODataExpression<T> operator !=(ODataExpression<T> expr1, ODataExpression<T> expr2)
	{
		return new ODataExpression<T>(new ODataExpression(expr1, expr2, ExpressionType.NotEqual));
	}

	public static ODataExpression<T> operator &(ODataExpression<T> expr1, ODataExpression<T> expr2)
	{
		return new ODataExpression<T>(new ODataExpression(expr1, expr2, ExpressionType.And));
	}

	public static ODataExpression<T> operator |(ODataExpression<T> expr1, ODataExpression<T> expr2)
	{
		return new ODataExpression<T>(new ODataExpression(expr1, expr2, ExpressionType.Or));
	}

	public static ODataExpression<T> operator >(ODataExpression<T> expr1, ODataExpression<T> expr2)
	{
		return new ODataExpression<T>(new ODataExpression(expr1, expr2, ExpressionType.GreaterThan));
	}

	public static ODataExpression<T> operator <(ODataExpression<T> expr1, ODataExpression<T> expr2)
	{
		return new ODataExpression<T>(new ODataExpression(expr1, expr2, ExpressionType.LessThan));
	}

	public static ODataExpression<T> operator >=(ODataExpression<T> expr1, ODataExpression<T> expr2)
	{
		return new ODataExpression<T>(new ODataExpression(expr1, expr2, ExpressionType.GreaterThanOrEqual));
	}

	public static ODataExpression<T> operator <=(ODataExpression<T> expr1, ODataExpression<T> expr2)
	{
		return new ODataExpression<T>(new ODataExpression(expr1, expr2, ExpressionType.LessThanOrEqual));
	}

	public static ODataExpression<T> operator +(ODataExpression<T> expr1, ODataExpression<T> expr2)
	{
		return new ODataExpression<T>(new ODataExpression(expr1, expr2, ExpressionType.Add));
	}

	public static ODataExpression<T> operator -(ODataExpression<T> expr1, ODataExpression<T> expr2)
	{
		return new ODataExpression<T>(new ODataExpression(expr1, expr2, ExpressionType.Subtract));
	}

	public static ODataExpression<T> operator *(ODataExpression<T> expr1, ODataExpression<T> expr2)
	{
		return new ODataExpression<T>(new ODataExpression(expr1, expr2, ExpressionType.Multiply));
	}

	public static ODataExpression<T> operator /(ODataExpression<T> expr1, ODataExpression<T> expr2)
	{
		return new ODataExpression<T>(new ODataExpression(expr1, expr2, ExpressionType.Divide));
	}

	public static ODataExpression<T> operator %(ODataExpression<T> expr1, ODataExpression<T> expr2)
	{
		return new ODataExpression<T>(new ODataExpression(expr1, expr2, ExpressionType.Modulo));
	}

	public static bool operator true(ODataExpression<T> expr)
	{
		return false;
	}

	public static bool operator false(ODataExpression<T> expr)
	{
		return false;
	}
}
