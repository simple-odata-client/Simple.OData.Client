using System.Web.Http.Dispatcher;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Routing.Controllers;

namespace WebApiOData.V4.Samples;

public class CustomHttpControllerTypeResolver(Type controllerType) : DefaultHttpControllerTypeResolver(IsController(controllerType))
{
	private static Predicate<Type> IsController(Type controllerType)
	{
		bool predicate(Type t) =>
			t == typeof(MetadataController)
			|| t == controllerType;

		return predicate;
	}
}
