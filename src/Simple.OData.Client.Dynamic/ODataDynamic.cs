using System.Collections.Generic;
using Simple.OData.Client.Extensions;

namespace Simple.OData.Client;

public static class ODataDynamic
{
	static ODataDynamic()
	{
		DictionaryExtensions.CreateDynamicODataEntry = (x, tc) => new DynamicODataEntry(x, tc);
	}

	public static dynamic Expression => new DynamicODataExpression();

	public static ODataExpression ExpressionFromReference(string reference)
	{
		return ODataExpression.FromReference(reference);
	}

	public static ODataExpression ExpressionFromValue(object value)
	{
		return ODataExpression.FromValue(value);
	}

	public static ODataExpression ExpressionFromFunction(string functionName, string targetName, IEnumerable<object> arguments)
	{
		var targetExpression = ODataExpression.FromReference(targetName);
		return ODataExpression.FromFunction(functionName, targetExpression, arguments);
	}
}
