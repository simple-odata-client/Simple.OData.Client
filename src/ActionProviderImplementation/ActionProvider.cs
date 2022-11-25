using System;
using System.Collections.Generic;
using System.Data.Services;
using System.Data.Services.Providers;
using System.Linq;

namespace ActionProviderImplementation;

public class ActionProvider : IDataServiceActionProvider
{
	private static readonly Dictionary<Type, List<ServiceAction>> _cache = new();
	private static readonly Dictionary<string, ServiceAction> _actionsByName = new();
	private readonly Type _instanceType;
	private readonly object _context;
	private readonly IParameterMarshaller _marshaller;

	public ActionProvider(object context, IParameterMarshaller marshaller)
	{
		_context = context;
		_instanceType = context.GetType();
		_marshaller = marshaller;
	}

	public bool AdvertiseServiceAction(DataServiceOperationContext operationContext, ServiceAction serviceAction, object resourceInstance, bool inFeed, ref Microsoft.Data.OData.ODataAction actionToSerialize)
	{
		var customState = serviceAction.CustomState as ActionInfo;
		return customState.IsAvailable(_context, resourceInstance, inFeed);
	}

	public IDataServiceInvokable CreateInvokable(DataServiceOperationContext operationContext, ServiceAction serviceAction, object[] parameters)
	{
		return new ActionInvokable(operationContext, serviceAction, _context, parameters, _marshaller);
	}

	public IEnumerable<ServiceAction> GetServiceActions(DataServiceOperationContext operationContext)
	{
		return GetActions(operationContext);
	}

	public IEnumerable<ServiceAction> GetServiceActionsByBindingParameterType(DataServiceOperationContext operationContext, ResourceType resourceType)
	{
		return GetActions(operationContext).Where(a => a.Parameters.Count > 0 && a.Parameters.First().ParameterType == resourceType);
	}

	public bool TryResolveServiceAction(DataServiceOperationContext operationContext, string serviceActionName, out ServiceAction serviceAction)
	{
		if (_actionsByName.ContainsKey(serviceActionName))
		{
			serviceAction = _actionsByName[serviceActionName];
		}
		else
		{
			serviceAction = GetActions(operationContext).SingleOrDefault(a => a.Name == serviceActionName);
			if (serviceAction != null)
			{
				_actionsByName[serviceActionName] = serviceAction;
			}
		}

		return serviceAction != null;
	}

	private List<ServiceAction> GetActions(DataServiceOperationContext context)
	{
		if (_cache.ContainsKey(_instanceType))
		{
			return _cache[_instanceType];
		}

		var metadata = context.GetService(typeof(IDataServiceMetadataProvider)) as IDataServiceMetadataProvider;
		var factory = new ActionFactory(metadata);

		var actions = factory.GetActions(_instanceType).ToList();
		_cache[_instanceType] = actions;

		return actions;
	}
}
