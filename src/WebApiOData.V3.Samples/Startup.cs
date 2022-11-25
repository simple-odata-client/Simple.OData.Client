using System;
using System.Web.Http;
using System.Web.Http.OData.Batch;
using System.Web.Http.OData.Builder;
using System.Web.Http.OData.Extensions;
using System.Web.Http.OData.Routing;
using System.Web.Http.OData.Routing.Conventions;
using Microsoft.Data.Edm;
using Microsoft.Data.OData;
using Microsoft.Data.OData.Query;
using Owin;
using WebApiOData.V3.Samples.Models;

namespace WebApiOData.V3.Samples;

public class Startup
{
	public static void Configuration(IAppBuilder builder)
	{
		var config = new HttpConfiguration();

		// Add a custom route convention for non-bindable actions.
		// (Web API does not have a built-in routing convention for non-bindable actions.)
		var conventions = ODataRoutingConventions.CreateDefault();
		conventions.Insert(0, new NonBindableActionRoutingConvention("NonBindableActions"));

		// Map the OData route.
		config.Routes.MapODataServiceRoute(
			routeName: "OData actions",
			routePrefix: "actions",
			model: GetModel(),
			pathHandler: new DefaultODataPathHandler(),
			routingConventions: conventions,
			batchHandler: new DefaultODataBatchHandler(new HttpServer(config)));

		builder.UseWebApi(config);
	}

	// Builds the EDM model for the OData service, including the OData action definitions.
	private static IEdmModel GetModel()
	{
		ODataModelBuilder modelBuilder = new ODataConventionModelBuilder();
		var moviesEntitySet = modelBuilder.EntitySet<Movie>("Movies");
		moviesEntitySet.EntityType.Ignore(m => m.TimeStamp);    // Don't expose timestamp to clients

		// Now add actions to the EDM.

		// CheckOut
		// URI: ~/odata/Movies(1)/CheckOut
		// Transient action. It is not available when the item is already checked out.
		var checkout = modelBuilder.Entity<Movie>().TransientAction("CheckOut");

		// Provide a function that returns a link to the action, when the action is available, or
		// returns null when the action is not available.
		checkout.HasActionLink(ctx =>
		{
			var movie = ctx.EntityInstance as Movie;

			// Note: In some cases, checking whether the action is available may be relatively expensive.
			// For example, it might require a DB lookup. 

			// Avoid doing expensive checks inside a loop (i.e., when serializing a feed). Instead, simply 
			// mark the action as available, by returning an action link. 

			// The SkipExpensiveAvailabilityChecks flag says whether to skip expensive checks. If this flag 
			// is true AND your availability check is expensive, skip the check and return a link.

			// In this sample, the check is not really expensive, but we honor the flag to show how it works.
			var createLink = true;
			if (ctx.SkipExpensiveAvailabilityChecks)
			{
				// Caller asked us to skip the availability check.
				createLink = true;
			}
			else if (!movie.IsCheckedOut) // Here is the "expensive" check
			{
				createLink = true;
			}

			if (createLink)
			{
				// Return the URI of the action.
				return new Uri(ctx.Url.CreateODataLink(
				new EntitySetPathSegment(ctx.EntitySet),
				new KeyValuePathSegment(ODataUriUtils.ConvertToUriLiteral(movie.ID, ODataVersion.V3)),
				new ActionPathSegment(checkout.Name)));
			}
			else
			{
				return null;
			}
		}, followsConventions: true);   // "followsConventions" means the action follows OData conventions.
		checkout.ReturnsFromEntitySet<Movie>("Movies");

		// ReturnMovie
		// URI: ~/odata/Movies(1)/Return
		// Always bindable. If the movie is not checked out, the action is a no-op.
		// Binds to a single entity; no parameters.
		var returnAction = modelBuilder.Entity<Movie>().Action("Return");
		returnAction.ReturnsFromEntitySet<Movie>("Movies");

		// SetDueDate action
		// URI: ~/odata/Movies(1)/SetDueDate
		// Binds to a single entity; takes an action parameter.
		var setDueDate = modelBuilder.Entity<Movie>().Action("SetDueDate");
		setDueDate.Parameter<DateTime>("DueDate");
		setDueDate.ReturnsFromEntitySet<Movie>("Movies");

		// CheckOut action
		// URI: ~/odata/Movies/CheckOut
		// Shows how to bind to a collection, instead of a single entity.
		// This action also accepts $filter queries. For example:
		//     ~/odata/Movies/CheckOut?$filter=Year eq 2005
		var checkOutFromCollection = modelBuilder.Entity<Movie>().Collection.Action("CheckOut");
		checkOutFromCollection.ReturnsCollectionFromEntitySet<Movie>("Movies");

		// CheckOutMany action
		// URI: ~/odata/Movies/CheckOutMany
		// Shows an action that takes a collection parameter.
		var checkoutMany = modelBuilder.Entity<Movie>().Collection.Action("CheckOutMany");
		checkoutMany.CollectionParameter<int>("MovieIDs");
		checkoutMany.ReturnsCollectionFromEntitySet<Movie>("Movies");

		// CreateMovie action
		// URI: ~/odata/CreateMovie
		// Non-bindable action. You invoke it from the service root.
		var createMovie = modelBuilder.Action("CreateMovie");
		createMovie.Parameter<string>("Title");
		createMovie.ReturnsFromEntitySet<Movie>("Movies");

		return modelBuilder.GetEdmModel();
	}
}
