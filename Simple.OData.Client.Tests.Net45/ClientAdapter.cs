using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Simple.OData.Client.Tests
{
    public class ClientAdapter
    {
        private readonly ODataClient _client;

        public ClientAdapter(Uri baseUri)
        {
            _client = new ODataClient(baseUri);
        }

        public Task<IEnumerable<IDictionary<string, object>>> FindEntriesAsync(string commandText)
        {
            return _client.FindEntriesAsync(commandText);
        }

        public Task<IDictionary<string, object>> FindEntryAsync(string commandText)
        {
            return _client.FindEntryAsync(commandText);
        }

        public Task<IDictionary<string, object>> GetEntryAsync(string collection, IDictionary<string, object> entryKey)
        {
            return _client.GetEntryAsync(collection, entryKey);
        }

        public Task<IDictionary<string, object>> InsertEntryAsync(string collection, IDictionary<string, object> entryData, bool resultRequired = true)
        {
            return _client.InsertEntryAsync(collection, entryData, resultRequired);
        }

        public async Task UpdateEntryAsync(string collection, IDictionary<string, object> entryKey, IDictionary<string, object> entryData)
        {
            await _client.UpdateEntryAsync(collection, entryKey, entryData);
        }

        public async Task DeleteEntryAsync(string collection, IDictionary<string, object> entrykey)
        {
            await _client.DeleteEntryAsync(collection, entrykey);
        }
    }
}
