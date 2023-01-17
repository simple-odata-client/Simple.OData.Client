using System.Collections.Generic;

namespace WebApiOData.V4.Samples.Models;

public class MoviesContext
{
	public List<Movie> Movies { get; set; } = new List<Movie>()
			{
				new Movie() { ID=1, Title = "Maximum Payback", Year = 1990, Details = new() { Synopsis = "..." } },
				new Movie() { ID=2, Title = "Inferno of Retribution", Year = 2005 },
				new Movie() { ID=3, Title = "Fatal Vengeance 2", Year = 2012 },
				new Movie() { ID=4, Title = "Sudden Danger", Year = 2012 },
				new Movie() { ID=5, Title = "Beyond Outrage", Year = 2014 },
				new Movie() { ID=6, Title = "The Nut Job", Year = 2014 }
			};
}
