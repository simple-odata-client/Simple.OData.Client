using System;
using System.Collections.Generic;
#if ODATA_V4
using Microsoft.Spatial;
#else
using System.Spatial;
#endif

namespace Simple.OData.Client.Tests
{
    enum PersonGender
    {
        Male,
        Female,
        Unknown,
    }

    class PersonBase
    {
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string[] Emails { get; set; }
        public List<Location> AddressInfo { get; set; }
        public PersonGender Gender { get; set; }
        public long Concurrency { get; set; }
    }

    class Person : PersonBase
    {
        public IEnumerable<Person> Friends { get; set; }
        public IEnumerable<Trip> Trips { get; set; }
        public IEnumerable<Photo> Photos { get; set; }
    }

    class PersonWithDateTime : PersonBase
    {
        public IEnumerable<PersonWithDateTime> Friends { get; set; }
        public IEnumerable<TripWithDateTime> Trips { get; set; }
        public IEnumerable<Photo> Photos { get; set; }
    }

    class PersonWithOpenTypeFields : Person
    {
        public string OpenTypeString { get; set; }
        public int OpenTypeInt { get; set; }
    }

    class PersonWithOpenTypeContainer : Person
    {
        public IDictionary<string, object> Properties { get; set; }
    }

    class PersonWithSets
    {
        public string UserName { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string[] Emails { get; set; }
        public List<Location> AddressInfo { get; set; }
        public PersonGender Gender { get; set; }
        public long Concurrency { get; set; }

        public ISet<Person> Friends { get; set; }
        public HashSet<Trip> Trips { get; set; }
        public HashSet<Photo> Photos { get; set; }
    }

    class PersonWithAnnotations : Person
    {
        public ODataEntryAnnotations Annotations { get; set; }
    }

    class Me : Person
    {
    }

    class TripBase
    {
        public int TripId { get; set; }
        public Guid ShareId { get; set; }
        public string Description { get; set; }
        public string Name { get; set; }
        public float Budget { get; set; }
        public DateTimeOffset StartsAt { get; set; }
        public DateTimeOffset EndsAt { get; set; }
        public IList<string> Tags { get; set; }

        public IEnumerable<Photo> Photos { get; set; }
    }

    class Trip : TripBase
    {
        public IEnumerable<PlanItem> PlanItems { get; set; }
    }

    class TripWithDateTime : TripBase
    {
        public IEnumerable<PlanItemWithDateTime> PlanItems { get; set; }
    }

    class Photo
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public byte[] Media { get; set; }
    }

    class PlanItem
    {
        public int PlanItemId { get; set; }
        public string ConfirmationCode { get; set; }
        public DateTimeOffset? StartsAt { get; set; }
        public DateTimeOffset? EndsAt { get; set; }
        public TimeSpan Duration { get; set; }
    }

    class PlanItemWithDateTime
    {
        public int PlanItemId { get; set; }
        public string ConfirmationCode { get; set; }
        public DateTime? StartsAt { get; set; }
        public DateTime? EndsAt { get; set; }
        public TimeSpan Duration { get; set; }
    }

    class PublicTransportation : PlanItem
    {
        public string SeatNumber { get; set; }
    }

    class Flight : PublicTransportation
    {
        public string FlightNumber { get; set; }
        public Airport From { get; set; }
        public Airport To { get; set; }
        public Airline Airline { get; set; }
    }

    class Event : PlanItem
    {
        public string Description { get; set; }
        public EventLocation OccursAt { get; set; }
    }

    class Airline
    {
        public string AirlineCode { get; set; }
        public string Name { get; set; }
    }

    class Airport
    {
        public string IcaoCode { get; set; }
        public string IataCode { get; set; }
        public string Name { get; set; }
        public AirportLocation Location { get; set; }
    }

    class Location
    {
        public class LocationCity
        {
            public string CountryRegion { get; set; }
            public string Name { get; set; }
            public string Region { get; set; }
        }

        public string Address { get; set; }
        public LocationCity City { get; set; }
    }

    class AirportLocation : Location
    {
        public GeographyPoint Loc { get; set; }
    }

    class EventLocation : Location
    {
        public string BuildingInfo { get; set; }
    }
}
