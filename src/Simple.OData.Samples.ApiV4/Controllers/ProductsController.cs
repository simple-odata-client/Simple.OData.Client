using System.Collections.Concurrent;
using System.Web.Http.OData.Routing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Formatter;
using Microsoft.AspNetCore.OData.Query;
using Microsoft.AspNetCore.OData.Routing;
using Microsoft.AspNetCore.OData.Routing.Controllers;
using WebApiOData.V4.Samples.Models;

namespace WebApiOData.V4.Samples.Controllers;

public class ProductsController : ODataController
{
	private static readonly ConcurrentDictionary<int, Product> _data;

	static ProductsController()
	{
		_data = new ConcurrentDictionary<int, Product>();
		var rand = new Random();

		Enumerable.Range(0, 100)
			.Select(i => new Product
			{
				Id = i,
				Name = "Product " + i,
				Price = rand.NextDouble() * 1000
			})
			.ToList()
			.ForEach(p => _data.TryAdd(p.Id, p));
	}

	public ProductsController()
	{
	}

	[Route("*")]
	public IActionResult Default()
	{
		return Ok("OK!!!");
	}

	[EnableQuery]
	public static IQueryable<Product> Get()
	{
		return _data.Values.AsQueryable();
	}

	public IActionResult GetProduct(int key)
	{
		if (_data.TryGetValue(key, out var retval))
		{
			return Ok(retval);
		}
		else
		{
			return NotFound();
		}
	}

	[HttpGet]
	public IActionResult MostExpensive()
	{
		var retval = _data.Max(pair => pair.Value.Price);

		return Ok(retval);
	}

	// Returns top 3 most expensive products
	// This is needed to check function name matching
	[HttpGet]
	public IActionResult MostExpensives()
	{
		var retval = _data.Values.OrderByDescending(p => p.Price).Take(3).ToList();

		return Ok(retval);
	}

	// Returns the top ten most expensive products
	[HttpGet]
	public IActionResult Top10()
	{
		var retval = _data.Values.OrderByDescending(p => p.Price).Take(10).ToList();

		return Ok(retval);
	}

	[HttpGet]
	public IActionResult GetPriceRank(int key)
	{
		if (_data.TryGetValue(key, out var product))
		{
			// NOTE: Use where clause to get the rank of the price may not
			// offer the good time complexity. The following code is intended
			// for demonstration only.
			return Ok(_data.Values.Count(one => one.Price > product.Price));
		}
		else
		{
			return NotFound();
		}
	}

	[HttpGet]
	public IActionResult CalculateGeneralSalesTax(int key, string state)
	{
		var taxRate = GetRate(state);

		if (_data.TryGetValue(key, out var product))
		{
			var tax = product.Price * taxRate / 100;
			return Ok(tax);
		}
		else
		{
			return NotFound();
		}
	}

	[HttpGet]
	[ODataRoute("GetSalesTaxRate(state={state})")]
	public IActionResult GetSalesTaxRate([FromODataUri] string state)
	{
		return Ok(GetRate(state));
	}

	[HttpGet]
	[EnableQuery]
	[ODataRoute("Products({key})/Default.Placements()")]
	public IActionResult Placements([FromODataUri] int key, ODataQueryOptions<Movie> options)
	{
		var source = new MoviesContext().Movies.Where(x => x.ID < key).AsQueryable();
		return Ok(source);
	}

	private static double GetRate(string state)
	{
		var taxRate = state switch
		{
			"AZ" => 5.6,
			"CA" => 7.5,
			"CT" => 6.35,
			"GA" => 4,
			"IN" => 7,
			"KS" => 6.15,
			"KY" => 6,
			"MA" => 6.25,
			"NV" => 6.85,
			"NJ" => 7,
			"NY" => 4,
			"NC" => 4.75,
			"ND" => 5,
			"PA" => 6,
			"TN" => 7,
			"TX" => 6.25,
			"VA" => 4.3,
			"WA" => 6.5,
			"WV" => 6,
			"WI" => 5,
			_ => 0,
		};
		return taxRate;
	}
}
