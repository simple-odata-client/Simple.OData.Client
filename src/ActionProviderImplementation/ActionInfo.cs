using System;
using System.Data.Services;
using System.Data.Services.Providers;
using System.Linq;
using System.Reflection;

namespace ActionProviderImplementation;

public class ActionInfo
{
	public ActionInfo(MethodInfo actionMethod, OperationParameterBindingKind bindable, string availabilityMethodName)
	{
		ActionMethod = actionMethod;
		Binding = bindable;

		if ((Binding == OperationParameterBindingKind.Sometimes))
		{
			AvailabilityCheckMethod = GetAvailabilityMethod(availabilityMethodName);
			if (AvailabilityCheckMethod.GetCustomAttributes(typeof(SkipCheckForFeeds), true).Length != 0)
			{
				SkipAvailabilityCheckForFeeds = true;
			}
		}
		else
		{
			if (availabilityMethodName != null)
			{
				throw new Exception("Unexpected availabilityMethodName provided.");
			}
		}
	}

	public MethodInfo ActionMethod { get; private set; }

	public OperationParameterBindingKind Binding { get; private set; }

	public void AssertAvailable(object context, object? entity, bool inFeedContext)
	{
		if (entity == null)
		{
			return;
		}

		if (!IsAvailable(context, entity, inFeedContext))
		{
			throw new DataServiceException(404, "Action not found.");
		}
	}

	public bool IsAvailable(object context, object entity, bool inFeedContext)
	{
		if (Binding == OperationParameterBindingKind.Always)
		{
			return true;
		}
		else if (Binding == OperationParameterBindingKind.Never)
		{
			return true; // TODO: need a way to verify we are NOT being bound... although I think this is impossible.
		}
		else if (inFeedContext && SkipAvailabilityCheckForFeeds)
		{
			return true;
		}
		else
		{
			return (bool)AvailabilityCheckMethod.Invoke(context, new object[] { entity });
		}
	}

	private MethodInfo AvailabilityCheckMethod { get; set; }
	private bool SkipAvailabilityCheckForFeeds { get; set; }
	private MethodInfo GetAvailabilityMethod(string? availabilityMethodName)
	{
		if (availabilityMethodName == null)
		{
			throw new Exception("If the action is conditionally available you need to provide a method to calculate availability.");
		}

		var declaringType = ActionMethod.DeclaringType;
		var method = declaringType.GetMethod(availabilityMethodName);

		if (method == null)
		{
			throw new Exception($"Availability Method {availabilityMethodName} was not found on type {declaringType.FullName}");
		}

		if (method.ReturnType != typeof(bool))
		{
			throw new Exception($"AvailabilityCheck method ({availabilityMethodName}) MUST return bool.");
		}

		var actionBindingParameterType = ActionMethod.GetParameters().First().ParameterType;
		var methodParameters = method.GetParameters();
		if (methodParameters.Count() != 1 || methodParameters.First().ParameterType != actionBindingParameterType)
		{
			throw new Exception($"AvailabilityCheck method was expected to have this signature 'bool {availabilityMethodName}({actionBindingParameterType.FullName})'");
		}

		return method;
	}
}
