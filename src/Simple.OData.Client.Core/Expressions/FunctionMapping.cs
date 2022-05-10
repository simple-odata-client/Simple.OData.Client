using System;
using System.Collections.Generic;
using System.Linq;

namespace Simple.OData.Client;

internal class FunctionMapping
{
	public class FunctionDefinition
	{
		public FunctionDefinition(ExpressionFunction.FunctionCall functionCall, FunctionMapping functionMapping, AdapterVersion adapterVersion = AdapterVersion.Any)
		{
			FunctionCall = functionCall;
			FunctionMapping = functionMapping;
			AdapterVersion = adapterVersion;
		}

		public ExpressionFunction.FunctionCall FunctionCall { get; set; }
		public FunctionMapping FunctionMapping { get; set; }
		public AdapterVersion AdapterVersion { get; set; }
	}

	public string FunctionName { get; private set; }
	public Func<string, ODataExpression, IEnumerable<object>, ODataExpression> FunctionMapper { get; private set; }

	private FunctionMapping(string functionName)
	{
		FunctionName = functionName;
	}

	private static readonly Func<FunctionDefinition, Func<string, ODataExpression, IEnumerable<object>, ODataExpression>> FunctionWithTarget =
		function =>
			(functionName, target, arguments) => ODataExpression.FromFunction(
			new ExpressionFunction()
			{
				FunctionName = function.FunctionMapping.FunctionName,
				Arguments = new List<ODataExpression>() { target },
			});

	private static readonly Func<FunctionDefinition, Func<string, ODataExpression, IEnumerable<object>, ODataExpression>> FunctionWithTargetAndArguments =
		function =>
			(functionName, target, arguments) => ODataExpression.FromFunction(
				new ExpressionFunction()
				{
					FunctionName = function.FunctionMapping.FunctionName,
					Arguments = MergeArguments(target, arguments),
				});

	private static readonly Func<FunctionDefinition, Func<string, ODataExpression, IEnumerable<object>, ODataExpression>> FunctionWithArgumentsAndTarget =
		function =>
			(functionName, target, arguments) => ODataExpression.FromFunction(
			new ExpressionFunction()
			{
				FunctionName = function.FunctionMapping.FunctionName,
				Arguments = MergeArguments(arguments, target),
			});

	public static readonly FunctionDefinition[] DefinedFunctions =
		{
				CreateFunctionDefinition("Contains", 1, "substringof", FunctionWithArgumentsAndTarget, AdapterVersion.V3),
				CreateFunctionDefinition("Contains", 1, "contains", FunctionWithTargetAndArguments, AdapterVersion.V4),
				CreateFunctionDefinition("StartsWith", 1, "startswith", FunctionWithTargetAndArguments),
				CreateFunctionDefinition("EndsWith", 1, "endswith", FunctionWithTargetAndArguments),
				CreateFunctionDefinition("Length", 0, "length", FunctionWithTarget),
				CreateFunctionDefinition("IndexOf", 1, "indexof", FunctionWithTargetAndArguments),
				CreateFunctionDefinition("Replace", 2, "replace", FunctionWithTargetAndArguments),
				CreateFunctionDefinition("Substring", 1, "substring", FunctionWithTargetAndArguments),
				CreateFunctionDefinition("Substring", 2, "substring", FunctionWithTargetAndArguments),
				CreateFunctionDefinition("ToLower", 0, "tolower", FunctionWithTarget),
				CreateFunctionDefinition("ToUpper", 0, "toupper", FunctionWithTarget),
				CreateFunctionDefinition("Trim", 0, "trim", FunctionWithTarget),
				CreateFunctionDefinition("Concat", 1, "concat", FunctionWithTargetAndArguments),
				CreateFunctionDefinition("Year", 0, "year", FunctionWithTarget),
				CreateFunctionDefinition("Month", 0, "month", FunctionWithTarget),
				CreateFunctionDefinition("Day", 0, "day", FunctionWithTarget),
				CreateFunctionDefinition("Hour", 0, "hour", FunctionWithTarget),
				CreateFunctionDefinition("Minute", 0, "minute", FunctionWithTarget),
				CreateFunctionDefinition("Second", 0, "second", FunctionWithTarget),
				CreateFunctionDefinition("Milliseconds", 0, "fractionalseconds", FunctionWithTarget, AdapterVersion.V4),
				CreateFunctionDefinition("Date", 0, "date", FunctionWithTarget, AdapterVersion.V4),
				CreateFunctionDefinition("Time", 0, "time", FunctionWithTarget, AdapterVersion.V4),
				CreateFunctionDefinition("Totaloffsetminutes", 0, "totaloffsetminutes", FunctionWithTarget, AdapterVersion.V4),
				CreateFunctionDefinition("Now", 0, "now", FunctionWithTarget, AdapterVersion.V4),
				CreateFunctionDefinition("Maxdatetime", 0, "maxdatetime", FunctionWithTarget, AdapterVersion.V4),
				CreateFunctionDefinition("Mindatetime", 0, "mindatetime", FunctionWithTarget, AdapterVersion.V4),
				CreateFunctionDefinition("Totalseconds", 0, "totalseconds", FunctionWithTarget, AdapterVersion.V4),
				CreateFunctionDefinition("Round", 0, "round", FunctionWithTarget),
				CreateFunctionDefinition("Floor", 0, "floor", FunctionWithTarget),
				CreateFunctionDefinition("Ceiling", 0, "ceiling", FunctionWithTarget),
			};

	public static bool ContainsFunction(string functionName, int argumentCount)
	{
		return DefinedFunctions.Any(x => x.FunctionCall.Equals(new ExpressionFunction.FunctionCall(functionName, argumentCount)));
	}

	public static bool TryGetFunctionMapping(
		string functionName,
		int argumentCount,
		AdapterVersion adapterVersion,
		out FunctionMapping? functionMapping)
	{
		functionMapping = null;
		var function = DefinedFunctions.SingleOrDefault(x =>
			x.FunctionCall.Equals(new ExpressionFunction.FunctionCall(functionName, argumentCount)) &&
			(x.AdapterVersion & adapterVersion) == adapterVersion);

		if (function != null)
		{
			functionMapping = function.FunctionMapping;
		}

		return function != null;
	}

	private static FunctionDefinition CreateFunctionDefinition(string functionName, int argumentCount, string mappedFunctionName,
		Func<FunctionDefinition, Func<string, ODataExpression, IEnumerable<object>, ODataExpression>> mapper, AdapterVersion adapterVersion = AdapterVersion.Any)
	{
		var functionCall = new ExpressionFunction.FunctionCall(functionName, argumentCount);
		var functionMapping = new FunctionMapping(mappedFunctionName);
		var function = new FunctionDefinition(functionCall, functionMapping, adapterVersion);
		functionMapping.FunctionMapper = mapper(function);
		return function;
	}

	private static List<ODataExpression> MergeArguments(ODataExpression argument, IEnumerable<object> arguments)
	{
		var collection = new List<ODataExpression>
			{
				argument
			};
		collection.AddRange(arguments.Select(ODataExpression.FromValue));
		return collection;
	}

	private static List<ODataExpression> MergeArguments(IEnumerable<object> arguments, ODataExpression argument)
	{
		var collection = new List<ODataExpression>();
		collection.AddRange(arguments.Select(ODataExpression.FromValue));
		collection.Add(argument);
		return collection;
	}
}
