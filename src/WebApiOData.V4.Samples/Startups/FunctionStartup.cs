using System;
using System.Web.Http;
using Microsoft.AspNet.OData.Batch;
using Microsoft.AspNet.OData.Builder;
using Microsoft.AspNet.OData.Extensions;
using Microsoft.OData.Edm;
using WebApiOData.V4.Samples.Controllers;
using WebApiOData.V4.Samples.Models;

namespace WebApiOData.V4.Samples.Startups
{
    public class FunctionStartup : Startup
    {
        public FunctionStartup()
            : base(typeof(ProductsController))
        {
        }

        protected override void ConfigureController(HttpConfiguration config)
        {
            config.MapODataServiceRoute(
                routeName: "OData functions",
                routePrefix: "functions",
                model: GetEdmModel(config),
                batchHandler: new DefaultODataBatchHandler(new HttpServer(config)));
        }

        private static IEdmModel GetEdmModel(HttpConfiguration config)
        {
            ODataModelBuilder builder = new ODataConventionModelBuilder(config);

            builder.EntitySet<Product>("Products");

            var productType = builder.EntityType<Product>();

            // Function bound to a collection
            // Returns the most expensive product, a single entity
            productType.Collection
                .Function("MostExpensive")
                .Returns<double>();

            // Function bound to a collection
            // Returns top 3 product, a collection
            productType.Collection
                .Function("MostExpensives")
                .ReturnsCollectionFromEntitySet<Product>("Products");

            // Function bound to a collection
            // Returns the top 10 product, a collection
            productType.Collection
                .Function("Top10")
                .ReturnsCollectionFromEntitySet<Product>("Products");

            // Function bound to a single entity
            // Returns the instance's price rank among all products
            productType
                .Function("GetPriceRank")
                .Returns<int>();

            // Function bound to a single entity
            // Accept a string as parameter and return a double
            // This function calculate the general sales tax base on the 
            // state
            productType
                .Function("CalculateGeneralSalesTax")
                .Returns<double>()
                .Parameter<string>("state");
            
            // Function bound to a single entity
            // Returns the list of movies with a product placement for this product
            productType
                .Function("Placements")
                .ReturnsCollectionFromEntitySet<Movie>("Movies");

            // Unbound Function
            builder.Function("GetSalesTaxRate")
                .Returns<double>()
                .Parameter<string>("state");

            return builder.GetEdmModel();
        }
    }
}