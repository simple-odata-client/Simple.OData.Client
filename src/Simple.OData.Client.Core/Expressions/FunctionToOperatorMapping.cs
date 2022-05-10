using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Simple.OData.Client;

internal abstract class FunctionToOperatorMapping
{
	public static bool TryGetOperatorMapping(ODataExpression functionCaller, ExpressionFunction function, AdapterVersion adapterVersion,
		out FunctionToOperatorMapping operatorMapping)
	{
		operatorMapping = DefinedMappings.SingleOrDefault(x => x.CanMap(function.FunctionName, function.Arguments.Count, functionCaller, adapterVersion));
		return operatorMapping != null;
	}

	public abstract string Format(ExpressionContext context, ODataExpression functionCaller, List<ODataExpression> functionArguments);

	protected abstract bool CanMap(string functionName, int argumentCount, ODataExpression functionCaller, AdapterVersion adapterVersion = AdapterVersion.Any);

	private static readonly FunctionToOperatorMapping[] DefinedMappings =
	{
			new InOperatorMapping()
		};
}

internal class InOperatorMapping : FunctionToOperatorMapping
{
	public override string Format(ExpressionContext context, ODataExpression functionCaller, List<ODataExpression> functionArguments)
	{
		if (functionCaller.Value is not IEnumerable list)
		{
			throw new ArgumentException("Function caller should have a value");
		}

		var listAsString = new StringBuilder();
		var delimiter = string.Empty;
		listAsString.Append('(');
		foreach (var item in list)
		{
			listAsString.Append(delimiter);
			listAsString.Append(context.Session.Adapter.GetCommandFormatter().ConvertValueToUriLiteral(item, false));
			delimiter = ",";
		}

		listAsString.Append(')');

		// to work around the issue in OData/odata.net (https://github.com/OData/odata.net/issues/2016) the 'in' is always grouped
		// the workaround can be removed later if this issue is fixed
		return $"({functionArguments[0].Format(context)} in {listAsString})";
	}

	protected override bool CanMap(string functionName, int argumentCount, ODataExpression functionCaller, AdapterVersion adapterVersion = AdapterVersion.Any)
	{
		return functionName == nameof(Enumerable.Contains) &&
			   argumentCount == 1 &&
			   functionCaller.Value != null &&
			   functionCaller.Value.GetType() != typeof(string) &&
			   IsInstanceOfType(typeof(IEnumerable), functionCaller.Value) &&
			   adapterVersion == AdapterVersion.V4;
	}

	private static bool IsInstanceOfType(Type expectedType, object? value)
	{
		if (value == null)
		{
			return false;
		}

		var valueType = value.GetType();
		if (expectedType.IsAssignableFrom(valueType))
		{
			return true;
		}

		if (expectedType.IsGenericType && !expectedType.GenericTypeArguments.Any() && valueType.IsGenericType)
		{
			var genericArgumentTypes = valueType.GenericTypeArguments;
			if (!genericArgumentTypes.Any())
			{
				genericArgumentTypes = new[] { typeof(object) };
				valueType = valueType.MakeGenericType(genericArgumentTypes);
			}

			var expectedGenericType = expectedType.MakeGenericType(genericArgumentTypes);
			return expectedGenericType.IsAssignableFrom(valueType);
		}

		return false;
	}
}
