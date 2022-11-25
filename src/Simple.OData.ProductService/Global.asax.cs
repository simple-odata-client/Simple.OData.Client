using System.Web.Http;
using Simple.OData.ProductService.App_Start;

namespace Simple.OData.ProductService;

public class WebApiApplication : System.Web.HttpApplication
{
	protected static void Application_Start()
	{
		GlobalConfiguration.Configure(WebApiConfig.Register);
	}
}
