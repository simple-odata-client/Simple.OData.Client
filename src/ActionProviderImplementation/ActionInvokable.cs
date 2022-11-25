using System;
using System.Data.Services;
using System.Data.Services.Providers;

namespace ActionProviderImplementation;

public class ActionInvokable : IDataServiceInvokable
{
	private readonly ServiceAction _serviceAction;
	private readonly Action _action;
	private bool _hasRun = false;
	private object _result;

	public ActionInvokable(DataServiceOperationContext operationContext, ServiceAction serviceAction, object site, object[] parameters, IParameterMarshaller marshaller)
	{
		_serviceAction = serviceAction;
		var info = serviceAction.CustomState as ActionInfo;
		var marshalled = marshaller.Marshall(operationContext, serviceAction, parameters);

		info.AssertAvailable(site, marshalled[0], true);
		_action = () => CaptureResult(info.ActionMethod.Invoke(site, marshalled));
	}
	public void CaptureResult(object o)
	{
		if (_hasRun)
		{
			throw new Exception("Invoke not available. This invokable has already been Invoked.");
		}

		_hasRun = true;
		_result = o;
	}
	public object GetResult()
	{
		if (!_hasRun)
		{
			throw new Exception("Results not available. This invokable hasn't been Invoked.");
		}

		return _result;
	}
	public void Invoke()
	{
		try
		{
			_action();
		}
		catch
		{
			throw new DataServiceException(500, $"Exception executing action {_serviceAction.Name}");
		}
	}
}
