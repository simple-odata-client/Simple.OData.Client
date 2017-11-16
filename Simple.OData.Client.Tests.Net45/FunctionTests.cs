using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Simple.OData.Client.Tests
{
    using Entry = System.Collections.Generic.Dictionary<string, object>;

    public class FunctionTests : TestBase
    {
        public FunctionTests()
            : base(true)
        {
        }

        [Fact]
        public async Task FunctionWithStringToInt()
        {
            var result = await _client
                .Unbound()
                .Function("ParseInt")
                .Set(new Entry() { { "number", "1" } })
                .ExecuteAsScalarAsync<int>();
            
            Assert.Equal(1, result);
        }

        [Fact]
        public async Task FunctionWithString()
        {
            var result = await _client
                .Unbound()
                .Function("ReturnString")
                .Set(new Entry() { { "text", "abc" } })
                .ExecuteAsScalarAsync<string>();

            Assert.Equal("abc", result);
        }

        [Fact]
        public async Task FunctionWithStringAsCollection()
        {
            var result = await _client
                .Unbound<string>()
                .Function("ReturnString")
                .Set(new Entry() { { "text", "abc" } })
                .ExecuteAsSingleAsync();

            Assert.Equal("abc", result);
        }

        [Fact]
        public async Task FunctionWithIntCollectionSingleElement()
        {
            var result = await _client
                .Unbound()
                .Function("ReturnIntCollection")
                .Set(new Entry() { { "count", 1 } })
                .ExecuteAsArrayAsync<int>();
            
            Assert.Equal(new[] { 1 }, result);
        }

        [Fact]
        public async Task FunctionWithIntCollectionMultipleElements()
        {
            var result = await _client
                .Unbound()
                .Function("ReturnIntCollection")
                .Set(new Entry() { { "count", 3 } })
                .ExecuteAsArrayAsync<int>();
            
            Assert.Equal(new[] { 1, 2, 3 }, result);
        }

        [Fact]
        public async Task FunctionWithLong()
        {
            var result = await _client
                .Unbound()
                .Function("PassThroughLong")
                .Set(new Entry() { { "number", 1L } })
                .ExecuteAsScalarAsync<long>();
            
            Assert.Equal(1L, result);
        }

        [Fact]
        public async Task FunctionWithDateTime()
        {
            var dateTime = new DateTime(2013, 1, 1, 12, 13, 14);
            var result = await _client
                .Unbound()
                .Function("PassThroughDateTime")
                .Set(new Entry() { { "dateTime", dateTime } })
                .ExecuteAsScalarAsync<DateTime>();
            
            Assert.Equal(dateTime, result);
        }

        [Fact]
        public async Task FunctionWithLocalDateTime()
        {
            var dateTime = DateTime.Now;
            var result = await _client
                .Unbound()
                .Function("PassThroughDateTime")
                .Set(new Entry() { { "dateTime", dateTime } })
                .ExecuteAsScalarAsync<DateTime>();

            Assert.Equal(dateTime, result);
        }

        [Fact]
        public async Task FunctionWithGuid()
        {
            var guid = Guid.NewGuid();
            var result = await _client
                .Unbound()
                .Function("PassThroughGuid")
                .Set(new Entry() { { "guid", guid } })
                .ExecuteAsScalarAsync<Guid>();
            
            Assert.Equal(guid, result);
        }

        [Fact]
        public async Task FunctionWithComplexType()
        {
            var address = new Address { City = "Oslo", Country = "Norway", Region = "Oslo", PostalCode = "1234" };
            var result = await _client
                .Unbound<IDictionary<string, object>>()
                .Action("PassThroughAddress")
                .Set(new Entry() {{"address", address}})
                .ExecuteAsSingleAsync();

            result = result["PassThroughAddress"] as IDictionary<string, object>;
            Assert.Equal("Oslo", result["City"]);
            Assert.Equal("Norway", result["Country"]);
        }

        [Fact]
        public async Task FunctionWithComplexTypeCollectionSingleElement()
        {
            var result = (await _client
                .Unbound<IDictionary<string, object>>()
                .Function("ReturnAddressCollection")
                .Set(new Entry() { { "count", 1 } })
                .ExecuteAsSingleAsync());

            Assert.Equal("Oslo", result["City"]);
            Assert.Equal("Norway", result["Country"]);
        }

        [Fact]
        public async Task FunctionWithComplexTypeCollectionMultipleElements()
        {
            var result = (await _client
                .Unbound<IDictionary<string, object>>()
                .Function("ReturnAddressCollection")
                .Set(new Entry() { { "count", 3 } })
                .ExecuteAsEnumerableAsync()).ToArray();

            Assert.Equal("Oslo", result[0]["City"]);
            Assert.Equal("Norway", result[0]["Country"]);
            Assert.Equal("Oslo", result[1]["City"]);
            Assert.Equal("Oslo", result[2]["City"]);
        }

        [Fact]
        public async Task FunctionWithComplexTypeCollectionEmpty()
        {
            var result = (await _client
                .Unbound<IDictionary<string, object>>()
                .Function("ReturnAddressCollection")
                .Set(new Entry() { { "count", 0 } })
                .ExecuteAsEnumerableAsync()).ToArray();

            Assert.Equal(0, result.Length);
        }
    }
}
