using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Simple.OData.Client.V4.Adapter.Extensions;
using Xunit;

namespace Simple.OData.Client.Tests;

public class TripPinTestsV4Json : TripPinTests
{
	public TripPinTestsV4Json() : base(TripPinV4ReadWriteUri, ODataPayloadFormat.Json) { }
}

public class TripPinRESTierTestsV4Json : TripPinTestBase
{
	public TripPinRESTierTestsV4Json() : base(TripPinV4RESTierUri, ODataPayloadFormat.Json) { }

	[Fact]
	public async Task FindPeopleCountByGender()
	{
		var client = new ODataClient(CreateDefaultSettings(s =>
		{
			s.IgnoreUnmappedProperties = true;
		}));
		var peopleGroupedByGender = await client
			.WithExtensions()
			.For<Person>()
			.Apply(x => x.GroupBy((p, a) => new
			{
				p.Gender,
				Count = a.Count()
			}))
			.FindEntriesAsync().ConfigureAwait(false);

		Assert.True(peopleGroupedByGender.All(x => x.Count > 0));
	}

	[Fact]
	public async Task FindPeopleCountByGenderDynamic()
	{
		var x = ODataDynamic.Expression;
		var b = ODataDynamicDataAggregation.Builder;
		var a = ODataDynamicDataAggregation.AggregationFunction;
		IEnumerable<dynamic> peopleGroupedByGender = await _client
			.WithExtensions()
			.For(x.Person)
			.Apply(b.GroupBy(new
			{
				x.Gender,
				Count = a.Count()
			}))
			.FindEntriesAsync();

		Assert.True(peopleGroupedByGender.First().Count > 0);
	}
}

public abstract class TripPinTests : TripPinTestBase
{
	protected TripPinTests(string serviceUri, ODataPayloadFormat payloadFormat) : base(serviceUri, payloadFormat) { }

	[Fact]
	public async Task FindAllPeople()
	{
		var client = new ODataClient(new ODataClientSettings
		{
			BaseUri = _serviceUri,
			IncludeAnnotationsInResults = true
		});
		var annotations = new ODataFeedAnnotations();

		var count = 0;
		var people = await client
			.For<PersonWithAnnotations>("Person")
			.FindEntriesAsync(annotations).ConfigureAwait(false);
		count += people.Count();

		while (annotations.NextPageLink != null)
		{
			people = await client
				.For<PersonWithAnnotations>()
				.FindEntriesAsync(annotations.NextPageLink, annotations).ConfigureAwait(false);
			count += people.Count();

			foreach (var person in people)
			{
				Assert.NotNull(person.Annotations.Id);
				Assert.NotNull(person.Annotations.ReadLink);
				Assert.NotNull(person.Annotations.EditLink);
			}
		}

		Assert.Equal(count, annotations.Count);
	}

	[Fact]
	public async Task FindSinglePersonWithFeedAnnotations()
	{
		var annotations = new ODataFeedAnnotations();

		var people = await _client
			.For<Person>()
			.Filter(x => x.UserName == "russellwhyte")
			.FindEntriesAsync(annotations).ConfigureAwait(false);

		Assert.Single(people);
		Assert.Null(annotations.NextPageLink);
	}

	[Fact]
	public async Task FindSinglePersonExternalHttpClient()
	{
		var client = new ODataClient(new ODataClientSettings(new HttpClient() { BaseAddress = _serviceUri }));

		var person = await client
			.For<Person>()
			.Key("russellwhyte")
			.FindEntryAsync().ConfigureAwait(false);

		Assert.Equal("russellwhyte", person.UserName);
	}

	[Fact]
	public async Task FindPeopleByGender()
	{
		var people = await _client
			.For<Person>()
			.Filter(x => x.Gender == (int)PersonGender.Male)
			.FindEntriesAsync().ConfigureAwait(false);

		Assert.True(people.All(x => x.Gender == PersonGender.Male));
	}

	[Fact]
	public async Task FindSinglePersonWithEntryAnnotations()
	{
		var client = new ODataClient(new ODataClientSettings
		{
			BaseUri = _serviceUri,
			IncludeAnnotationsInResults = true
		});
		var person = await client
			.For<PersonWithAnnotations>("Person")
			.Filter(x => x.UserName == "russellwhyte")
			.FindEntryAsync().ConfigureAwait(false);

		Assert.NotNull(person.Annotations.Id);
	}

	[Fact]
	public async Task FindPersonExpandTripsAndFriends()
	{
		var person = await _client
			.For<Person>()
			.Key("russellwhyte")
			.Expand(x => new { x.Trips, x.Friends })
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Equal(3, person.Trips.Count());
		Assert.Equal(4, person.Friends.Count());
	}

	[Fact]
	public async Task FindPersonExpandEmptyTrips()
	{
		var person = await _client
			.For<Person>()
			.Key("keithpinckney")
			.Expand(x => new { x.Trips })
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Empty(person.Trips);
		Assert.Null(person.Friends);
		Assert.Null(person.Photo);
	}

	[Fact]
	public async Task FindPersonExpandAndSelectTripsAndFriendsTyped()
	{
		var person = await _client
			.For<Person>()
			.Key("russellwhyte")
			.Expand(x => new { x.Trips, x.Friends })
			.Select(x => x.Trips.Select(y => y.Name))
			.Select(x => x.Friends.Select(y => y.LastName))
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Equal("Trip in US", person.Trips.First().Name);
		Assert.Equal("Ketchum", person.Friends.First().LastName);
	}

	[Fact]
	public async Task FindPersonExpandAndSelectTripsAndFriendsDynamic()
	{
		var x = ODataDynamic.Expression;
		var person = await _client
			.For(x.Person)
			.Key("russellwhyte")
			.Expand(x.Trips, x.Friends)
			.Select(x.Trips.Name)
			.Select(x.Friends.LastName)
			.FindEntryAsync();
		Assert.Equal("Trip in US", (person.Trips as IEnumerable<dynamic>).First().Name);
		Assert.Equal("Ketchum", (person.Friends as IEnumerable<dynamic>).First().LastName);
	}

	[Fact]
	public async Task FindPersonExpandFriendsWithOrderBy()
	{
		_ = await _client
			.For("People")
			.Key("russellwhyte")
			.Expand("Friends")
			.OrderBy("Friends/LastName")
			.FindEntryAsync().ConfigureAwait(false);
		//Assert.Equal(3, person.Trips.Count());
		//Assert.Equal(4, person.Friends.Count());
	}

	[Fact]
	public async Task FindPersonPlanItems()
	{
		var flights = await _client
			.For<Person>()
			.Key("russellwhyte")
			.NavigateTo(x => x.Trips)
			.Key(1003)
			.NavigateTo(x => x.PlanItems)
			.FindEntriesAsync().ConfigureAwait(false);
		Assert.Equal(3, flights.Count());
	}

	[Fact]
	public async Task FindPersonPlanItemsWithDateTime()
	{
		var flights = await _client
			.For<PersonWithDateTime>("People")
			.Key("russellwhyte")
			.NavigateTo(x => x.Trips)
			.Key(1003)
			.NavigateTo(x => x.PlanItems)
			.FindEntriesAsync().ConfigureAwait(false);
		Assert.Equal(3, flights.Count());
	}

	[Fact]
	public async Task FindPersonWithDataContract()
	{
		var person = await _client
			.For<PersonWithDataContract>()
			.Key("russellwhyte")
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Equal("russellwhyte", person.UserName);
	}

	[Fact]
	public async Task FindPersonPlanItemsAsSets()
	{
		var flights = await _client
			.For<PersonWithSets>("People")
			.Key("russellwhyte")
			.NavigateTo(x => x.Trips)
			.Key(1003)
			.NavigateTo(x => x.PlanItems)
			.FindEntriesAsync().ConfigureAwait(false);
		Assert.Equal(3, flights.Count());
	}

	[Fact]
	public async Task FindPersonPlanItemsByDate()
	{
		var now = DateTimeOffset.Now;
		var flights = await _client
			.For<Person>()
			.Key("russellwhyte")
			.NavigateTo(x => x.Trips)
			.Key(1003)
			.NavigateTo(x => x.PlanItems)
			.Filter(x => x.StartsAt == now)
			.FindEntriesAsync().ConfigureAwait(false);
		Assert.Empty(flights);
	}

	[Fact]
	public async Task FindPersonTwoLevelExpand()
	{
		var person = await _client
			.For<Person>()
			.Key("russellwhyte")
			.Expand(x => x.Friends.Select(y => y.Friends))
			.FindEntryAsync().ConfigureAwait(false);
		Assert.NotNull(person);
		Assert.Equal(4, person.Friends.Count());
	}

	[Fact]
	public async Task FindPersonThreeLevelExpand()
	{
		var person = await _client
			.For<Person>()
			.Key("russellwhyte")
			.Expand(x => x.Friends.Select(y => y.Friends.Select(z => z.Friends)))
			.FindEntryAsync().ConfigureAwait(false);
		Assert.NotNull(person);
		Assert.Equal(4, person.Friends.Count());
		Assert.Equal(8, person.Friends.SelectMany(x => x.Friends).Count());
	}

	[Fact]
	public async Task FindPersonWithAnyTrips()
	{
		var flights = await _client
			.For<Person>()
			.Filter(x => x.Trips
				.Any(y => y.Budget > 10000d))
			.Expand(x => x.Trips)
			.FindEntriesAsync().ConfigureAwait(false);
		Assert.True(flights.All(x => x.Trips.Any(y => y.Budget > 10000d)));
		Assert.Equal(2, flights.SelectMany(x => x.Trips).Count());
	}

	[Fact]
	public async Task FindPersonWithAllTrips()
	{
		var flights = await _client
			.For<Person>()
			.Filter(x => x.Trips
				.All(y => y.Budget > 10000d))
			.Expand(x => x.Trips)
			.FindEntriesAsync().ConfigureAwait(false);
		Assert.True(flights.All(x => x.Trips == null || x.Trips.All(y => y.Budget > 10000d)));
	}

	[Fact]
	public async Task FindPersonPlanItemsWithAllTripsAnyPlanItems()
	{
		var duration = TimeSpan.FromHours(4);
		var flights = await _client
			.For<Person>()
			.Filter(x => x.Trips
				.All(y => y.PlanItems
					.Any(z => z.Duration < duration)))
			.FindEntriesAsync().ConfigureAwait(false);
		Assert.Equal(8, flights.Count());
	}

	[Fact]
	public async Task FindPersonFlight()
	{
		var flight = await _client
			.For<Person>()
			.Key("russellwhyte")
			.NavigateTo(x => x.Trips)
			.Key(1003)
			.NavigateTo(x => x.PlanItems)
			.Key(21)
			.As<Flight>()
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Equal("FM1930", flight.FlightNumber);
	}

	[Fact]
	public async Task FindPersonFlightExpandAndSelect()
	{
		var flight = await _client
			.For<Person>()
			.Key("russellwhyte")
			.NavigateTo(x => x.Trips)
			.Key(1003)
			.NavigateTo(x => x.PlanItems)
			.Key(21)
			.As<Flight>()
			.Expand(x => x.Airline)
			.Select(x => new { x.FlightNumber, x.Airline.AirlineCode })
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Null(flight.From);
		Assert.Null(flight.To);
		Assert.Null(flight.Airline.Name);
		Assert.Equal("FM", flight.Airline.AirlineCode);
	}

	[Fact]
	public async Task FindPersonFlights()
	{
		var flights = await _client
			.For<Person>()
			.Key("russellwhyte")
			.NavigateTo(x => x.Trips)
			.Key(1003)
			.NavigateTo(x => x.PlanItems)
			.As<Flight>()
			.FindEntriesAsync().ConfigureAwait(false);
		Assert.Equal(2, flights.Count());
		Assert.Contains(flights, x => x.FlightNumber == "FM1930");
	}

	[Fact]
	public async Task FindPersonExpandPhotoWithSelect()
	{
		var persons = await _client
			.For<Person>()
			.Expand(x => x.Photo)
			.Select(x => new { x.UserName, Photo = new { x.Photo.Name } })
			.Top(1)
			.FindEntriesAsync().ConfigureAwait(false);
		var person = Assert.Single(persons);
		Assert.Null(person.Photo.Media);
		Assert.Equal(default, person.Photo.Id);
	}

	[Fact]
	public async Task FindPersonExpandFriendsWithSelect()
	{
		var persons = await _client
			.For<Person>()
			.Expand(x => x.Friends)
			.Select(x => new { x.UserName, Friends = x.Friends.Select(y => y.UserName) })
			.Top(1)
			.FindEntriesAsync().ConfigureAwait(false);
		var person = Assert.Single(persons);
		Assert.DoesNotContain(person.Friends, x => x.UserName == null);
		Assert.True(person.Friends.All(x => x.FirstName == null));
	}

	[Fact]
	public async Task FindPersonFlightsWithFilter()
	{
		var flights = await _client
			.For<Person>()
			.Key("russellwhyte")
			.NavigateTo(x => x.Trips)
			.Key(1003)
			.NavigateTo(x => x.PlanItems)
			.As<Flight>()
			.Filter(x => x.FlightNumber == "FM1930")
			.FindEntriesAsync().ConfigureAwait(false);
		Assert.Single(flights);
		Assert.True(flights.All(x => x.FlightNumber == "FM1930"));
	}

	[Fact]
	public async Task FindPersonWithEmail()
	{
		var persons = await _client
			.For<Person>()
			.Filter(x => x.Emails.Any(e => e != null))
			.Top(1)
			.FindEntriesAsync().ConfigureAwait(false);
		var person = Assert.Single(persons);
		Assert.NotEmpty(person.Emails);
	}

	[Fact]
	public async Task FindPersonByEmail()
	{
		var persons = await _client
			.For<Person>()
			.Filter(x => x.Emails.Any(e => e == "Russell@example.com"))
			.Top(1)
			.FindEntriesAsync().ConfigureAwait(false);
		var person = Assert.Single(persons);
		Assert.NotEmpty(person.Emails);
	}

	[Fact]
	public async Task FindPersonFromWa()
	{
		var persons = await _client
			.For<Person>()
			.Filter(x => x.AddressInfo.Any(a => a.City.Region == "WA"))
			.Top(1)
			.FindEntriesAsync().ConfigureAwait(false);
		var person = Assert.Single(persons);
		Assert.NotEmpty(person.Emails);
	}

	[Fact]
	public async Task UpdatePersonLastName()
	{
		var person = await _client
			.For<Person>()
			.Filter(x => x.UserName == "russellwhyte")
			.Set(new { LastName = "White" })
			.UpdateEntryAsync().ConfigureAwait(false);
		Assert.Equal("White", person.LastName);
	}

	[Fact]
	public async Task UpdatePersonEmail()
	{
		var person = await _client
			.For<Person>()
			.Filter(x => x.UserName == "russellwhyte")
			.Set(new { Emails = new[] { "russell.whyte@gmail.com" } })
			.UpdateEntryAsync().ConfigureAwait(false);
		Assert.Equal("russell.whyte@gmail.com", person.Emails.First());
	}

	[Fact]
	public async Task UpdatePersonAddress()
	{
		var person = await _client
			.For<Person>()
			.Filter(x => x.UserName == "russellwhyte")
			.Set(new
			{
				AddressInfo = new[]
				{
						new Location()
						{
							Address = "187 Suffolk Ln.",
							City = new Location.LocationCity()
							{
								CountryRegion = "United States",
								Name = "Boise",
								Region = "ID"
							}
						}
				},
			})
			.UpdateEntryAsync().ConfigureAwait(false);
		Assert.Equal("Boise", person.AddressInfo.First().City.Name);
	}

	[Fact]
	public async Task InsertPersonWithTypedOpenProperty()
	{
		_ = await _client
			.For<PersonWithOpenTypeFields>("Person")
			.Set(new
			{
				UserName = "gregorsamsa",
				FirstName = "Gregor",
				LastName = "Samsa",
				OpenTypeString = "Description"
			})
			.InsertEntryAsync().ConfigureAwait(false);

		var person = await _client
			.For<PersonWithOpenTypeFields>("Person")
			.Key("gregorsamsa")
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Equal(@"Description", person.OpenTypeString);
	}

	[Fact]
	public async Task InsertPersonWithDynamicOpenProperty()
	{
		var person = await _client
			.For<PersonWithOpenTypeContainer>("Person")
			.WithProperties(x => x.Properties)
			.Set(new
			{
				UserName = "gregorsamsa",
				FirstName = "Gregor",
				LastName = "Samsa",
				Properties = new Dictionary<string, object> { { "OpenTypeString", "Description" } },
			})
			.InsertEntryAsync().ConfigureAwait(false);

		person = await _client
			.For<PersonWithOpenTypeContainer>("Person")
			.WithProperties(x => x.Properties)
			.Key("gregorsamsa")
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Equal(@"Description", person.Properties["OpenTypeString"]);
	}

	[Fact]
	public async Task UpdatePersonWithDynamicOpenProperty()
	{
		var person = await _client
			.For<PersonWithOpenTypeContainer>("Person")
			.WithProperties(x => x.Properties)
			.Set(new
			{
				UserName = "gregorsamsa",
				FirstName = "Gregor",
				LastName = "Samsa",
				Properties = new Dictionary<string, object> { { "OpenTypeString", "Description" } },
			})
			.InsertEntryAsync().ConfigureAwait(false);
		await _client
			.For<PersonWithOpenTypeContainer>("Person")
			.Key("gregorsamsa")
			.WithProperties(x => x.Properties)
			.Set(new
			{
				UserName = "gregorsamsa",
				Properties = new Dictionary<string, object> { { "OpenTypeString", "New description" } },
			})
			.UpdateEntryAsync().ConfigureAwait(false);
		person = await _client
			.For<PersonWithOpenTypeContainer>("Person")
			.WithProperties(x => x.Properties)
			.Key("gregorsamsa")
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Equal(@"New description", person.Properties["OpenTypeString"]);
		Assert.Equal("Samsa", person.LastName);
	}

	[Fact]
	public async Task FlterPersonOnTypedOpenProperty()
	{
		var person = await _client
			.For<PersonWithOpenTypeFields>("Person")
			.Set(new
			{
				UserName = "gregorsamsa",
				FirstName = "Gregor",
				LastName = "Samsa",
				OpenTypeString = "Description"
			})
			.InsertEntryAsync().ConfigureAwait(false);

		person = await _client
			.For<PersonWithOpenTypeFields>("Person")
			.Filter(x => x.OpenTypeString == "Description")
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Equal(@"Description", person.OpenTypeString);
	}

	[Fact]
	public async Task FlterPersonOnDynamicOpenProperty()
	{
		var person = await _client
			.For<PersonWithOpenTypeContainer>("Person")
			.WithProperties(x => x.Properties)
			.Set(new
			{
				UserName = "gregorsamsa",
				FirstName = "Gregor",
				LastName = "Samsa",
				Properties = new Dictionary<string, object> { { "OpenTypeString", "Description" } },
			})
			.InsertEntryAsync().ConfigureAwait(false);

		person = await _client
			.For<PersonWithOpenTypeContainer>("Person")
			.WithProperties(x => x.Properties)
			.Filter(x => x.Properties["OpenTypeString"].ToString() == "Description")
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Equal(@"Description", person.Properties["OpenTypeString"]);
	}

	[Fact(Skip = "Fails at server")]
	public async Task InsertPersonWithLinkToPeople()
	{
		var friend = await _client
			.For<Person>()
			.Key("russellwhyte")
			.FindEntryAsync().ConfigureAwait(false);
		_ = await _client
			.For<Person>()
			.Set(new
			{
				UserName = "gregorsamsa",
				FirstName = "Gregor",
				LastName = "Samsa",
				Friends = new[] { friend },
			})
			.InsertEntryAsync().ConfigureAwait(false);

		var person = await _client
			.For<Person>()
			.Key("gregorsamsa")
			.FindEntryAsync().ConfigureAwait(false);

		Assert.NotNull(person);
	}

	[Fact(Skip = "Fails at server")]
	public async Task InsertPersonWithLinkToMe()
	{
		var friend = await _client
			.For<Me>()
			.FindEntryAsync().ConfigureAwait(false);
		_ = await _client
			.For<Person>()
			.Set(new
			{
				UserName = "gregorsamsa",
				FirstName = "Gregor",
				LastName = "Samsa",
				Friends = new[] { friend },
			})
			.InsertEntryAsync().ConfigureAwait(false);

		var person = await _client
			.For<Person>()
			.Key("gregorsamsa")
			.FindEntryAsync().ConfigureAwait(false);

		Assert.NotNull(person);
	}

	[Fact]
	public async Task FilterPersonByOpenTypeProperty()
	{
		var person = await _client
			.For<PersonWithOpenTypeFields>("Person")
			.Set(new
			{
				UserName = "gregorsamsa",
				FirstName = "Gregor",
				LastName = "Samsa",
				OpenTypeString = "Description"
			})
			.InsertEntryAsync().ConfigureAwait(false);

		person = await _client
			.For<PersonWithOpenTypeFields>("Person")
			.Filter(x => x.OpenTypeString == "Description")
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Equal(@"Description", person.OpenTypeString);
	}

	[Fact]
	public async Task SelectOpenTypeStringProperty()
	{
		var person = await _client
			.For<PersonWithOpenTypeFields>("Person")
			.Set(new
			{
				UserName = "gregorsamsa",
				FirstName = "Gregor",
				LastName = "Samsa",
				OpenTypeString = @"""Description"""
			})
			.InsertEntryAsync().ConfigureAwait(false);

		person = await _client
			.For<PersonWithOpenTypeFields>("Person")
			.Key("gregorsamsa")
			.Select(x => new { x.UserName, x.OpenTypeString })
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Equal(@"\""Description\""", person.OpenTypeString);
	}

	[Fact]
	public async Task SelectOpenTypeIntProperty()
	{
		var person = await _client
			.For<PersonWithOpenTypeFields>("Person")
			.Set(new
			{
				UserName = "gregorsamsa",
				FirstName = "Gregor",
				LastName = "Samsa",
				OpenTypeInt = 1
			})
			.InsertEntryAsync().ConfigureAwait(false);

		person = await _client
			.For<PersonWithOpenTypeFields>("Person")
			.Key("gregorsamsa")
			.Select(x => new { x.UserName, x.OpenTypeInt })
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Equal(1, person.OpenTypeInt);
	}

	[Fact]
	public async Task FindMe()
	{
		var person = await _client
			.For<Person>("Me")
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Equal("aprilcline", person.UserName);
		Assert.Equal(2, person.Emails.Length);
		Assert.Equal("Lander", person.AddressInfo.Single().City.Name);
		Assert.Equal(PersonGender.Female, person.Gender);
	}

	[Fact]
	public async Task FindMeSelectAddressInfo()
	{
		var person = await _client
			.For<Person>("Me")
			.Select(x => x.AddressInfo)
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Equal("Lander", person.AddressInfo.Single().City.Name);
		Assert.Null(person.UserName);
		Assert.Null(person.Emails);
	}

	[Fact]
	public async Task UpdateMeGender_PreconditionRequired()
	{
		await AssertThrowsAsync<InvalidOperationException>(async () =>
		{
			await _client
				.For<Person>("Me")
				.Set(new { Gender = PersonGender.Male })
				.UpdateEntryAsync().ConfigureAwait(false);
		}).ConfigureAwait(false);
	}

	//[Fact]
	//public async Task UpdateMe_LastName_PreconditionRequired()
	//{
	//    var person = await _client
	//        .For<Person>("Me")
	//        .Set(new { LastName = "newname" })
	//        .UpdateEntryAsync();
	//    Assert.Equal("newname", person.LastName);
	//}

	[Fact]
	public async Task FindAllAirlines()
	{
		var airlines = await _client
			.For<Airline>()
			.FindEntriesAsync().ConfigureAwait(false);
		Assert.Equal(8, airlines.Count());
	}

	[Fact]
	public async Task FindAllAirports()
	{
		var airports = await _client
			.For<Airport>()
			.FindEntriesAsync().ConfigureAwait(false);
		Assert.Equal(8, airports.Count());
	}

	[Fact]
	public async Task FindAirportByCode()
	{
		var airport = await _client
			.For<Airport>()
			.Key("KSFO")
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Equal("SFO", airport.IataCode);
		Assert.Equal("San Francisco", airport.Location.City.Name);
		Assert.Equal(4326, airport.Location.Loc.CoordinateSystem.EpsgId);
		Assert.Equal(37.6188888888889, airport.Location.Loc.Latitude);
		Assert.Equal(-122.374722222222, airport.Location.Loc.Longitude);
	}

	[Fact]
	public async Task FindAirportOrderedByLocationAddress()
	{
		var airports = await _client
			.For<Airport>()
			.OrderBy(x => x.Location.Address)
			.Top(2)
			.FindEntriesAsync().ConfigureAwait(false);
		var first = airports.Select(x => x.Location.Address).First();
		var second = airports.Select(x => x.Location.Address).Last();
		Assert.True(first.CompareTo(second) < 0);
		airports = await _client
			.For<Airport>()
			.OrderByDescending(x => x.Location.Address)
			.Top(2)
			.FindEntriesAsync().ConfigureAwait(false);
		first = airports.Select(x => x.Location.Address).First();
		second = airports.Select(x => x.Location.Address).Last();
		Assert.True(first.CompareTo(second) > 0);
	}

	[Fact]
	public async Task FindAirportOrderedByLocationCityName()
	{
		var airports = await _client
			.For<Airport>()
			.OrderBy(x => x.Location.City.Name)
			.Top(2)
			.FindEntriesAsync().ConfigureAwait(false);
		var first = airports.Select(x => x.Location.City.Name).First();
		var second = airports.Select(x => x.Location.City.Name).Last();
		Assert.True(first.CompareTo(second) < 0);
		airports = await _client
			.For<Airport>()
			.OrderByDescending(x => x.Location.City.Name)
			.Top(2)
			.FindEntriesAsync().ConfigureAwait(false);
		first = airports.Select(x => x.Location.City.Name).First();
		second = airports.Select(x => x.Location.City.Name).Last();
		Assert.True(first.CompareTo(second) > 0);
	}

	[Fact]
	public async Task FindAirportByLocationCityRegionEquals()
	{
		var airport = await _client
			.For<Airport>()
			.Filter(x => x.Location.City.Region == "California")
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Equal("SFO", airport.IataCode);
	}

	[Fact]
	public async Task FindAirportByLocationCityRegionContains()
	{
		var airport = await _client
			.For<Airport>()
			.Filter(x => x.Location.City.Region.Contains("California"))
			.FindEntryAsync().ConfigureAwait(false);
		Assert.Equal("SFO", airport.IataCode);
	}

	[Fact]
	public async Task InsertEvent()
	{
		var command = _client
			.For<Person>()
			.Key("russellwhyte")
			.NavigateTo<Trip>()
			.Key(1003)
			.NavigateTo(x => x.PlanItems)
			.As<Event>();

		var tripEvent = await command
			.Set(CreateTestEvent())
			.InsertEntryAsync().ConfigureAwait(false);

		tripEvent = await command
			.Key(tripEvent.PlanItemId)
			.FindEntryAsync().ConfigureAwait(false);

		Assert.NotNull(tripEvent);
	}

	[Fact]
	public async Task UpdateEvent()
	{
		var command = _client
			.For<Person>()
			.Key("russellwhyte")
			.NavigateTo<Trip>()
			.Key(1003)
			.NavigateTo(x => x.PlanItems)
			.As<Event>();

		var tripEvent = await command
			.Set(CreateTestEvent())
			.InsertEntryAsync().ConfigureAwait(false);

		tripEvent = await command
			.Key(tripEvent.PlanItemId)
			.Set(new { Description = "This is a new description" })
			.UpdateEntryAsync().ConfigureAwait(false);

		Assert.Equal("This is a new description", tripEvent.Description);
	}

	[Fact]
	public async Task DeleteEvent()
	{
		var command = _client
			.For<Person>()
			.Key("russellwhyte")
			.NavigateTo<Trip>()
			.Key(1003)
			.NavigateTo(x => x.PlanItems)
			.As<Event>();

		var tripEvent = await command
			.Set(CreateTestEvent())
			.InsertEntryAsync().ConfigureAwait(false);

		await command
			.Key(tripEvent.PlanItemId)
			.DeleteEntryAsync().ConfigureAwait(false);

		tripEvent = await command
			.Key(tripEvent.PlanItemId)
			.FindEntryAsync().ConfigureAwait(false);

		Assert.Null(tripEvent);
	}

	[Fact]
	public async Task FindPersonTrips()
	{
		var trips = await _client
			.For<Person>()
			.Key("russellwhyte")
			.NavigateTo<Trip>()
			.FindEntriesAsync().ConfigureAwait(false);

		Assert.Equal(3, trips.Count());
	}

	[Fact]
	public async Task FindPersonTripsWithDateTime()
	{
		var trips = await _client
			.For<Person>()
			.Key("russellwhyte")
			.NavigateTo<TripWithDateTime>("Trip")
			.FindEntriesAsync().ConfigureAwait(false);

		Assert.Equal(3, trips.Count());
	}

	[Fact]
	public async Task FindPersonTripsFilterDescription()
	{
		var trips = await _client
			.For<Person>()
			.Key("russellwhyte")
			.NavigateTo<Trip>()
			.Filter(x => x.Description.Contains("New York"))
			.FindEntriesAsync().ConfigureAwait(false);

		Assert.Single(trips);
		Assert.Contains("New York", trips.Single().Description);
	}

	[Fact]
	public async Task GetNearestAirport()
	{
		var airport = await _client
			.Unbound<Airport>()
			.Function("GetNearestAirport")
			.Set(new { lat = 100d, lon = 100d })
			.ExecuteAsSingleAsync().ConfigureAwait(false);

		Assert.Equal("KSEA", airport.IcaoCode);
	}

	[Fact]
	public async Task ResetDataSource()
	{
		var command = _client
			.For<Person>()
			.Key("russellwhyte")
			.NavigateTo<Trip>()
			.Key(1003)
			.NavigateTo(x => x.PlanItems)
			.As<Event>();

		var tripEvent = await command
			.Set(CreateTestEvent())
			.InsertEntryAsync().ConfigureAwait(false);

		await _client
			.Unbound()
			.Action("ResetDataSource")
			.ExecuteAsync().ConfigureAwait(false);

		tripEvent = await command
			.Filter(x => x.PlanItemId == tripEvent.PlanItemId)
			.FindEntryAsync().ConfigureAwait(false);

		Assert.Null(tripEvent);
	}

	[Fact]
	public async Task ShareTrip()
	{
		await _client
			.For<Person>()
			.Key("russellwhyte")
			.Action("ShareTrip")
			.Set(new { userName = "scottketchum", tripId = 1003 })
			.ExecuteAsync().ConfigureAwait(false);
	}

	[Fact]
	public async Task ShareTripInBatch()
	{
		var batch = new ODataBatch(_client);

		batch += async x => await x
			.For<Person>()
			.Key("russellwhyte")
			.Action("ShareTrip")
			.Set(new { userName = "scottketchum", tripId = 1003 })
			.ExecuteAsSingleAsync().ConfigureAwait(false);

		await batch.ExecuteAsync().ConfigureAwait(false);
	}

	[Fact]
	public async Task GetInvolvedPeople()
	{
		var people = await _client
			.For<Person>()
			.Key("scottketchum")
			.NavigateTo<Trip>()
			.Key(0)
			.Function("GetInvolvedPeople")
			.ExecuteAsEnumerableAsync().ConfigureAwait(false);
		Assert.Equal(2, people.Count());
	}

	[Fact]
	public async Task GetInvolvedPeopleEmptyResult()
	{
		var people = await _client
			.For<Person>()
			.Key("russellwhyte")
			.NavigateTo<Trip>()
			.Key(1)
			.Function("GetInvolvedPeople")
			.ExecuteAsEnumerableAsync().ConfigureAwait(false);
		Assert.Empty(people);
	}

	[Fact]
	public async Task GetInvolvedPeopleInBatch()
	{
		var batch = new ODataBatch(_client);

		IEnumerable<object>? people = null;
		batch += async x =>
		{
			people = await x.For<Person>()
				.Key("scottketchum")
				.NavigateTo<Trip>()
				.Key(0)
				.Function("GetInvolvedPeople")
				.ExecuteAsEnumerableAsync().ConfigureAwait(false);
		};

		await batch.ExecuteAsync().ConfigureAwait(false);
		Assert.Equal(2, people.Count());
	}

	[Fact]
	public async Task Batch()
	{
		IEnumerable<Airline>? airlines1 = null;
		IEnumerable<Airline>? airlines2 = null;

		var batch = new ODataBatch(_client);
		batch += async c => airlines1 = await c
		   .For<Airline>()
		   .FindEntriesAsync().ConfigureAwait(false);
		batch += c => c
		   .For<Airline>()
		   .Set(new Airline() { AirlineCode = "TT", Name = "Test Airline" })
		   .InsertEntryAsync(false);
		batch += async c => airlines2 = await c
		   .For<Airline>()
		   .FindEntriesAsync().ConfigureAwait(false);
		await batch.ExecuteAsync().ConfigureAwait(false);

		Assert.Equal(8, airlines1.Count());
		Assert.Equal(8, airlines2.Count());
	}

	[Fact(Skip = "Fails at server: https://github.com/OData/ODataSamples/issues/140")]
	public async Task BatchPayloadRelativeUri()
	{
		IEnumerable<Airline>? airlines1 = null;
		IEnumerable<Airline>? airlines2 = null;

		var client = new ODataClient(CreateDefaultSettings(s =>
		{
			s.BaseUri = _serviceUri;
			s.BatchPayloadUriOption = BatchPayloadUriOption.RelativeUri;
		}));

		var batch = new ODataBatch(client);
		batch += async c => airlines1 = await c
		   .For<Airline>()
		   .FindEntriesAsync().ConfigureAwait(false);
		batch += c => c
		   .For<Airline>()
		   .Set(new Airline() { AirlineCode = "TT", Name = "Test Airline" })
		   .InsertEntryAsync(false);
		batch += async c => airlines2 = await c
		   .For<Airline>()
		   .FindEntriesAsync().ConfigureAwait(false);
		await batch.ExecuteAsync().ConfigureAwait(false);

		Assert.Equal(8, airlines1.Count());
		Assert.Equal(8, airlines2.Count());
	}

	[Fact]
	public async Task BatchPayloadAbsoluteUri()
	{
		IEnumerable<Airline>? airlines1 = null;
		IEnumerable<Airline>? airlines2 = null;

		var client = new ODataClient(CreateDefaultSettings(s =>
		{
			s.BaseUri = _serviceUri;
			s.BatchPayloadUriOption = BatchPayloadUriOption.AbsoluteUri;
		}));

		var batch = new ODataBatch(client);
		batch += async c => airlines1 = await c
		   .For<Airline>()
		   .FindEntriesAsync().ConfigureAwait(false);
		batch += c => c
		   .For<Airline>()
		   .Set(new Airline() { AirlineCode = "TT", Name = "Test Airline" })
		   .InsertEntryAsync(false);
		batch += async c => airlines2 = await c
		   .For<Airline>()
		   .FindEntriesAsync().ConfigureAwait(false);
		await batch.ExecuteAsync().ConfigureAwait(false);

		Assert.Equal(8, airlines1.Count());
		Assert.Equal(8, airlines2.Count());
	}

	[Fact(Skip = "Fails at server: https://github.com/OData/ODataSamples/issues/140")]
	public async Task BatchPayloadAbsoluteUriUsingHostHeader()
	{
		IEnumerable<Airline>? airlines1 = null;
		IEnumerable<Airline>? airlines2 = null;

		var client = new ODataClient(CreateDefaultSettings(s =>
		{
			s.BaseUri = _serviceUri;
			s.BatchPayloadUriOption = BatchPayloadUriOption.AbsoluteUriUsingHostHeader;
		}));

		var batch = new ODataBatch(client);
		batch += async c => airlines1 = await c
		   .For<Airline>()
		   .FindEntriesAsync().ConfigureAwait(false);
		batch += c => c
		   .For<Airline>()
		   .Set(new Airline() { AirlineCode = "TT", Name = "Test Airline" })
		   .InsertEntryAsync(false);
		batch += async c => airlines2 = await c
		   .For<Airline>()
		   .FindEntriesAsync().ConfigureAwait(false);
		await batch.ExecuteAsync().ConfigureAwait(false);

		Assert.Equal(8, airlines1.Count());
		Assert.Equal(8, airlines2.Count());
	}

	[Fact]
	public async Task FindEventWithNonNullStartTime()
	{
		var tripEvent = await _client
			.For<Person>()
			.Key("russellwhyte")
			.NavigateTo<Trip>()
			.Key(1003)
			.NavigateTo(x => x.PlanItems)
			.As<Event>()
			.Filter(x => x.StartsAt < DateTimeOffset.UtcNow)
			.FindEntryAsync().ConfigureAwait(false);

		Assert.NotNull(tripEvent);
	}

	[Fact]
	public async Task FindEventWithNullStartTime()
	{
		var tripEvent = await _client
			.For<Person>()
			.Key("russellwhyte")
			.NavigateTo<Trip>()
			.Key(1003)
			.NavigateTo(x => x.PlanItems)
			.As<Event>()
			.Filter(x => x.StartsAt == null)
			.FindEntryAsync().ConfigureAwait(false);

		Assert.Null(tripEvent);
	}

	[Fact]
	public async Task GetFavoriteAirline()
	{
		var airport = await _client
			.For<Person>()
			.Key("russellwhyte")
			.Function("GetFavoriteAirline")
			.ExecuteAsArrayAsync<Airline>().ConfigureAwait(false);

		Assert.Equal("AA", airport.First().AirlineCode);
	}

	[Fact]
	public async Task GetPhotoMedia()
	{
		var photo = await _client
			.For<Photo>()
			.Key(1)
			.FindEntryAsync().ConfigureAwait(false);
		photo.Media = await _client
			.For<Photo>()
			.Key(photo.Id)
			.Media()
			.GetStreamAsArrayAsync().ConfigureAwait(false);

		Assert.Equal(12277, photo.Media.Length);
	}

	[Fact]
	public async Task SetPhotoMedia()
	{
		var photo = await _client
			.For<Photo>()
			.Key(1)
			.FindEntryAsync().ConfigureAwait(false);
		photo.Media = await _client
			.For<Photo>()
			.Key(photo.Id)
			.Media()
			.GetStreamAsArrayAsync().ConfigureAwait(false);
		var byteCount = photo.Media.Length;

		await _client
			.For<Photo>()
			.Key(photo.Id)
			.Media()
			.SetStreamAsync(photo.Media, "image/jpeg", true).ConfigureAwait(false);
		photo.Media = await _client
			.For<Photo>()
			.Key(photo.Id)
			.Media()
			.GetStreamAsArrayAsync().ConfigureAwait(false);

		Assert.Equal(byteCount, photo.Media.Length);
	}

	private static Event CreateTestEvent()
	{
		return new Event
		{
			ConfirmationCode = "4372899DD",
			Description = "Client Meeting",
			Duration = TimeSpan.FromHours(3),
			EndsAt = DateTimeOffset.Parse("2014-06-01T23:11:17.5479185-07:00"),
			OccursAt = new EventLocation()
			{
				Address = "100 Church Street, 8th Floor, Manhattan, 10007",
				BuildingInfo = "Regus Business Center",
				City = new Location.LocationCity()
				{
					CountryRegion = "United States",
					Name = "New York City",
					Region = "New York",
				}
			},
			PlanItemId = 33,
			StartsAt = DateTimeOffset.Parse("2014-05-25T23:11:17.5459178-07:00"),
		};
	}
}
