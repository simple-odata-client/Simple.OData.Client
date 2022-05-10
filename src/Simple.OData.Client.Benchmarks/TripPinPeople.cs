using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Newtonsoft.Json;
using Simple.OData.Client.Tests;
using Xunit;

namespace Simple.OData.Client.Benchmarks;

internal class PeopleCollection
{
	[JsonProperty("value")]
	public Person[] People { get; set; }
}

public class TripPinPeople
{
	[Benchmark]
	public async static Task FindTypedPeopleWithTripsAndFriends()
	{
		var result = await Utils.GetClient("TripPin.xml", "TripPin_result_20.json")
			.For<Person>()
			.Expand(x => new { x.Trips, x.Friends })
			.FindEntriesAsync().ConfigureAwait(false);

		Assert.Equal(20, result.ToList().Count);
	}

	[Benchmark]
	public async static Task FindUntypedPeopleWithTripsAndFriends()
	{
		var result = await Utils.GetClient("TripPin.xml", "TripPin_result_20.json")
			.For("People")
			.Expand(new[] { "Trips", "Friends" })
			.FindEntriesAsync().ConfigureAwait(false);

		Assert.Equal(20, result.ToList().Count);
	}

	[Benchmark]
	public static void ConvertWithNewtonsoftJson()
	{
		var json = File.ReadAllText(@"..\..\..\..\..\..\..\Resources\" + "TripPin_result_20.json");
		var result = JsonConvert.DeserializeObject<PeopleCollection>(json);
		Assert.Equal(20, result.People.Length);
	}
}
