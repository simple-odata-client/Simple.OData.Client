using System.Data.Services;
using System.Data.Services.Providers;

namespace ActionProviderImplementation;

public interface IParameterMarshaller
{
	object[] Marshall(DataServiceOperationContext operationContext, ServiceAction serviceAction, object[] parameters);
}
