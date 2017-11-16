using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Simple.OData.Client.Tests
{
#if ODATA_V4
    public class TripPinTestsV4Json : TripPinTests
    {
        public TripPinTestsV4Json() : base(TripPinV4ReadWriteUri, ODataPayloadFormat.Json) { }
    }
#endif

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

            int count = 0;
            var people = await client
                .For<PersonWithAnnotations>("Person")
                .FindEntriesAsync(annotations);
            count += people.Count();

            while (annotations.NextPageLink != null)
            {
                people = await client
                    .For<PersonWithAnnotations>()
                    .FindEntriesAsync(annotations.NextPageLink, annotations);
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
                .FindEntriesAsync(annotations);

            Assert.Equal(1, people.Count());
            Assert.Null(annotations.NextPageLink);
        }

        [Fact]
        public async Task FindPeopleByGender()
        {
            var people = await _client
                .For<Person>()
                .Filter(x => (int)x.Gender == (int)PersonGender.Male)
                .FindEntriesAsync();

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
                .FindEntryAsync();

            Assert.NotNull(person.Annotations.Id);
        }

        [Fact]
        public async Task FindPersonExpandTripsAndFriends()
        {
            var person = await _client
                .For<Person>()
                .Key("russellwhyte")
                .Expand(x => new { x.Trips, x.Friends })
                .FindEntryAsync();
            Assert.Equal(3, person.Trips.Count());
            Assert.Equal(4, person.Friends.Count());
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
                .FindEntryAsync();
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
            var person = await _client
                .For("People")
                .Key("russellwhyte")
                .Expand("Friends")
                .OrderBy("Friends/LastName")
                .FindEntryAsync();
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
                .FindEntriesAsync();
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
                .FindEntriesAsync();
            Assert.Equal(3, flights.Count());
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
                .FindEntriesAsync();
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
                .FindEntriesAsync();
            Assert.Equal(0, flights.Count());
        }

        [Fact]
        public async Task FindPersonTwoLevelExpand()
        {
            var person = await _client
                .For<Person>()
                .Key("russellwhyte")
                .Expand(x => x.Friends.Select(y => y.Friends))
                .FindEntryAsync();
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
                .FindEntryAsync();
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
                .FindEntriesAsync();
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
                .FindEntriesAsync();
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
                .FindEntriesAsync();
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
                .FindEntryAsync();
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
                .FindEntryAsync();
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
                .FindEntriesAsync();
            Assert.Equal(2, flights.Count());
            Assert.True(flights.Any(x => x.FlightNumber == "FM1930"));
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
                .FindEntriesAsync();
            Assert.Equal(1, flights.Count());
            Assert.True(flights.All(x => x.FlightNumber == "FM1930"));
        }

        [Fact]
        public async Task UpdatePersonLastName()
        {
            var person = await _client
                .For<Person>()
                .Filter(x => x.UserName == "russellwhyte")
                .Set(new { LastName = "White" })
                .UpdateEntryAsync();
            Assert.Equal("White", person.LastName);
        }

        [Fact]
        public async Task UpdatePersonEmail()
        {
            var person = await _client
                .For<Person>()
                .Filter(x => x.UserName == "russellwhyte")
                .Set(new { Emails = new[] { "russell.whyte@gmail.com" } })
                .UpdateEntryAsync();
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
                .UpdateEntryAsync();
            Assert.Equal("Boise", person.AddressInfo.First().City.Name);
        }

        [Fact]
        public async Task InsertPersonWithTypedOpenProperty()
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
                .InsertEntryAsync();

            person = await _client
                .For<PersonWithOpenTypeFields>("Person")
                .Key("gregorsamsa")
                .FindEntryAsync();
            Assert.Equal(@"""Description""", person.OpenTypeString);
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
                .InsertEntryAsync();

            person = await _client
                .For<PersonWithOpenTypeContainer>("Person")
                .WithProperties(x => x.Properties)
                .Key("gregorsamsa")
                .FindEntryAsync();
            Assert.Equal(@"""Description""", person.Properties["OpenTypeString"]);
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
                .InsertEntryAsync();
            await _client
                .For<PersonWithOpenTypeContainer>("Person")
                .Key("gregorsamsa")
                .WithProperties(x => x.Properties)
                .Set(new
                {
                    UserName = "gregorsamsa",
                    Properties = new Dictionary<string, object> { { "OpenTypeString", "New description" } },
                })
                .UpdateEntryAsync();
            person = await _client
                .For<PersonWithOpenTypeContainer>("Person")
                .WithProperties(x => x.Properties)
                .Key("gregorsamsa")
                .FindEntryAsync();
            Assert.Equal(@"""New description""", person.Properties["OpenTypeString"]);
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
                .InsertEntryAsync();

            person = await _client
                .For<PersonWithOpenTypeFields>("Person")
                .Filter(x => x.OpenTypeString == "Description")
                .FindEntryAsync();
            Assert.Equal(@"""Description""", person.OpenTypeString);
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
                .InsertEntryAsync();

            person = await _client
                .For<PersonWithOpenTypeContainer>("Person")
                .WithProperties(x => x.Properties)
                .Filter(x => x.Properties["OpenTypeString"].ToString() == "Description")
                .FindEntryAsync();
            Assert.Equal(@"""Description""", person.Properties["OpenTypeString"]);
        }

        [Fact(Skip = "Fails at server")]
        public async Task InsertPersonWithLinkToPeople()
        {
            var friend = await _client
                .For<Person>()
                .Key("russellwhyte")
                .FindEntryAsync();

            var person = await _client
                .For<Person>()
                .Set(new
                {
                    UserName = "gregorsamsa",
                    FirstName = "Gregor",
                    LastName = "Samsa",
                    Friends = new[] { friend },
                })
                .InsertEntryAsync();

            person = await _client
                .For<Person>()
                .Key("gregorsamsa")
                .FindEntryAsync();

            Assert.NotNull(person);
        }

        [Fact(Skip = "Fails at server")]
        public async Task InsertPersonWithLinkToMe()
        {
            var friend = await _client
                .For<Me>()
                .FindEntryAsync();

            var person = await _client
                .For<Person>()
                .Set(new
                {
                    UserName = "gregorsamsa",
                    FirstName = "Gregor",
                    LastName = "Samsa",
                    Friends = new[] { friend },
                })
                .InsertEntryAsync();

            person = await _client
                .For<Person>()
                .Key("gregorsamsa")
                .FindEntryAsync();

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
                .InsertEntryAsync();

            person = await _client
                .For<PersonWithOpenTypeFields>("Person")
                .Filter(x => x.OpenTypeString == "Description")
                .FindEntryAsync();
                Assert.Equal(@"""Description""", person.OpenTypeString);
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
                .InsertEntryAsync();

            person = await _client
                .For<PersonWithOpenTypeFields>("Person")
                .Key("gregorsamsa")
                .Select(x => new { x.UserName, x.OpenTypeString })
                .FindEntryAsync();
            Assert.Equal(@"""\""Description\""""", person.OpenTypeString);
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
                .InsertEntryAsync();

            person = await _client
                .For<PersonWithOpenTypeFields>("Person")
                .Key("gregorsamsa")
                .Select(x => new { x.UserName, x.OpenTypeInt })
                .FindEntryAsync();
            Assert.Equal(1, person.OpenTypeInt);
        }

        [Fact]
        public async Task FindMe()
        {
            var person = await _client
                .For<Person>("Me")
                .FindEntryAsync();
            Assert.Equal("aprilcline", person.UserName);
            Assert.Equal(2, person.Emails.Count());
            Assert.Equal("Lander", person.AddressInfo.Single().City.Name);
            Assert.Equal(PersonGender.Female, person.Gender);
        }

        [Fact]
        public async Task FindMeSelectAddressInfo()
        {
            var person = await _client
                .For<Person>("Me")
                .Select(x => x.AddressInfo)
                .FindEntryAsync();
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
                    .UpdateEntryAsync();
            });
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
                .FindEntriesAsync();
            Assert.Equal(8, airlines.Count());
        }

        [Fact]
        public async Task FindAllAirports()
        {
            var airports = await _client
                .For<Airport>()
                .FindEntriesAsync();
            Assert.Equal(8, airports.Count());
        }

        [Fact]
        public async Task FindAirportByCode()
        {
            var airport = await _client
                .For<Airport>()
                .Key("KSFO")
                .FindEntryAsync();
            Assert.Equal("SFO", airport.IataCode);
            Assert.Equal("San Francisco", airport.Location.City.Name);
            Assert.Equal(4326, airport.Location.Loc.CoordinateSystem.EpsgId);
            Assert.Equal(37.6188888888889, airport.Location.Loc.Latitude);
            Assert.Equal(-122.374722222222, airport.Location.Loc.Longitude);
        }

        [Fact]
        public async Task FindAirportByLocationCityRegionEquals()
        {
            var airport = await _client
                .For<Airport>()
                .Filter(x => x.Location.City.Region == "California")
                .FindEntryAsync();
            Assert.Equal("SFO", airport.IataCode);
        }

        [Fact]
        public async Task FindAirportByLocationCityRegionContains()
        {
            var airport = await _client
                .For<Airport>()
                .Filter(x => x.Location.City.Region.Contains("California"))
                .FindEntryAsync();
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
                .InsertEntryAsync();

            tripEvent = await command
                .Key(tripEvent.PlanItemId)
                .FindEntryAsync();

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
                .InsertEntryAsync();

            tripEvent = await command
                .Key(tripEvent.PlanItemId)
                .Set(new { Description = "This is a new description" })
                .UpdateEntryAsync();

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
                .InsertEntryAsync();

            await command
                .Key(tripEvent.PlanItemId)
                .DeleteEntryAsync();

            tripEvent = await command
                .Key(tripEvent.PlanItemId)
                .FindEntryAsync();

            Assert.Null(tripEvent);
        }

        [Fact]
        public async Task FindPersonTrips()
        {
            var trips = await _client
                .For<Person>()
                .Key("russellwhyte")
                .NavigateTo<Trip>()
                .FindEntriesAsync();

            Assert.Equal(3, trips.Count());
        }

        [Fact]
        public async Task FindPersonTripsWithDateTime()
        {
            var trips = await _client
                .For<Person>()
                .Key("russellwhyte")
                .NavigateTo<TripWithDateTime>("Trip")
                .FindEntriesAsync();

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
                .FindEntriesAsync();

            Assert.Equal(1, trips.Count());
            Assert.Contains("New York", trips.Single().Description);
        }

        [Fact]
        public async Task GetNearestAirport()
        {
            var airport = await _client
                .Unbound<Airport>()
                .Function("GetNearestAirport")
                .Set(new { lat = 100d, lon = 100d })
                .ExecuteAsSingleAsync();

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
                .InsertEntryAsync();

            await _client
                .Unbound()
                .Action("ResetDataSource")
                .ExecuteAsync();

            tripEvent = await command
                .Filter(x => x.PlanItemId == tripEvent.PlanItemId)
                .FindEntryAsync();

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
                .ExecuteAsSingleAsync();
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
                .ExecuteAsSingleAsync();

            await batch.ExecuteAsync();
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
                .ExecuteAsEnumerableAsync();
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
                .ExecuteAsEnumerableAsync();
            Assert.Equal(0, people.Count());
        }

        [Fact]
        public async Task GetInvolvedPeopleInBatch()
        {
            var batch = new ODataBatch(_client);

            IEnumerable<object> people = null;
            batch += async x =>
            {
                people = await x.For<Person>()
                    .Key("scottketchum")
                    .NavigateTo<Trip>()
                    .Key(0)
                    .Function("GetInvolvedPeople")
                    .ExecuteAsEnumerableAsync();
            };

            await batch.ExecuteAsync();
            Assert.Equal(2, people.Count());
        }

        [Fact]
        public async Task Batch()
        {
            IEnumerable<Airline> airlines1 = null;
            IEnumerable<Airline> airlines2 = null;

            var batch = new ODataBatch(_client);
            batch += async c => airlines1 = await c
               .For<Airline>()
               .FindEntriesAsync();
            batch += c => c
               .For<Airline>()
               .Set(new Airline() { AirlineCode = "TT", Name = "Test Airline" })
               .InsertEntryAsync(false);
            batch += async c => airlines2 = await c
               .For<Airline>()
               .FindEntriesAsync();
            await batch.ExecuteAsync();

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
                .FindEntryAsync();

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
                .FindEntryAsync();

            Assert.Null(tripEvent);
        }

        [Fact]
        public async Task GetFavoriteAirline()
        {
            var airport = await _client
                .For<Person>()
                .Key("russellwhyte")
                .Function("GetFavoriteAirline")
                .ExecuteAsArrayAsync<Airline>();

            Assert.Equal("AA", airport.First().AirlineCode);
        }

        [Fact]
        public async Task GetPhotoMedia()
        {
            var photo = await _client
                .For<Photo>()
                .Key(1)
                .FindEntryAsync();
            photo.Media = await _client
                .For<Photo>()
                .Key(photo.Id)
                .Media()
                .GetStreamAsArrayAsync();

            Assert.Equal(12277, photo.Media.Length);
        }

        [Fact]
        public async Task SetPhotoMedia()
        {
            var photo = await _client
                .For<Photo>()
                .Key(1)
                .FindEntryAsync();
            photo.Media = await _client
                .For<Photo>()
                .Key(photo.Id)
                .Media()
                .GetStreamAsArrayAsync();
            var byteCount = photo.Media.Length;

            await _client
                .For<Photo>()
                .Key(photo.Id)
                .Media()
                .SetStreamAsync(photo.Media, "image/jpeg", true);
            photo.Media = await _client
                .For<Photo>()
                .Key(photo.Id)
                .Media()
                .GetStreamAsArrayAsync();

            Assert.Equal(byteCount, photo.Media.Length);
        }

        private Event CreateTestEvent()
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
}