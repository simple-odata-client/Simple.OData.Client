using System.Web.Http;
using System.Web.Http.Dispatcher;
using Owin;

namespace WebApiOData.V4.Samples;

public abstract class Startup(Type controllerType)
{
	private readonly Type _controllerType = controllerType;

	protected abstract void ConfigureController(HttpConfiguration config);

	public void Configuration(IAppBuilder builder)
	{
		var config = new HttpConfiguration();

		config.Services.Replace(
			typeof(IHttpControllerTypeResolver),
			new CustomHttpControllerTypeResolver(_controllerType));

		config.Select().Expand().Filter().OrderBy().MaxTop(null).Count();//https://stackoverflow.com/a/40021161/19671

		ConfigureController(config);

		builder.UseWebApi(config);
	}
}
