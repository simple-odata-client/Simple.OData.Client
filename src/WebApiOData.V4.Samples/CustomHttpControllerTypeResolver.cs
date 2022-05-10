using System;
using System.Web.Http.Dispatcher;
using Microsoft.AspNet.OData;

namespace WebApiOData.V4.Samples;

public class CustomHttpControllerTypeResolver : DefaultHttpControllerTypeResolver
{
	public CustomHttpControllerTypeResolver(Type controllerType)
		: base(IsController(controllerType))
	{
	}

	private static Predicate<Type> IsController(Type controllerType)
	{
		bool predicate(Type t) =>
			t == typeof(MetadataController)
			|| t == controllerType;

		return predicate;
	}
}
