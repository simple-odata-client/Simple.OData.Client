using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Simple.OData.Client
{
    /// <summary>
    /// Performs batch processing of OData requests by grouping multiple operations in a single HTTP POST request in accordance with OData protocol
    /// </summary>
    public class ODataBatch
    {
        private readonly ODataClient _client;
        private readonly List<Func<IODataClient, Task>> _actions = new List<Func<IODataClient, Task>>();
        private readonly SimpleDictionary<object, IDictionary<string, object>> _entryMap = new SimpleDictionary<object, IDictionary<string, object>>(); 

        /// <summary>
        /// Initializes a new instance of the <see cref="ODataBatch"/> class.
        /// </summary>
        /// <param name="urlBase">The URL base.</param>
        /// <remarks>
        /// This constructor overload is obsolete. Use <see cref="ODataBatch(Uri)"/> constructor overload./>
        /// </remarks>
        [Obsolete("This constructor overload is obsolete. Use ODataBatch(Uri baseUri) constructor.")]
        public ODataBatch(string urlBase)
            : this(new ODataClientSettings { UrlBase = urlBase })
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ODataBatch"/> class.
        /// </summary>
        /// <param name="baseUri">The URL base.</param>
        public ODataBatch(Uri baseUri)
            : this(new ODataClientSettings { BaseUri = baseUri })
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ODataBatch"/> class.
        /// </summary>
        /// <param name="settings">The settings.</param>
        public ODataBatch(ODataClientSettings settings)
        {
            _client = new ODataClient(settings, _entryMap);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ODataBatch"/> class.
        /// </summary>
        /// <param name="client">The OData client which settings will be used to create a batch.</param>
        public ODataBatch(IODataClient client) : this(client, false) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ODataBatch"/> class.
        /// </summary>
        /// <param name="client">The OData client which will be used to create a batch.</param>
        /// <param name="reuseSession">Flag indicating that the existing session from the <see cref="ODataClient"/>
        /// should be used rather than creating a new one.
        /// </param>
        public ODataBatch(IODataClient client, bool reuseSession)
        {
            _client = reuseSession
                ? new ODataClient((client as ODataClient), _entryMap) 
                : new ODataClient((client as ODataClient).Session.Settings, _entryMap);
        }
        /// <summary>
        /// Adds an OData command to an OData batch.
        /// </summary>
        /// <param name="batch">The OData batch.</param>
        /// <param name="action">The command to add to the batch.</param>
        /// <returns></returns>
        public static ODataBatch operator +(ODataBatch batch, Func<IODataClient, Task> action)
        {
            batch._actions.Add(action);
            return batch;
        }

        /// <summary>
        /// Executes the OData batch by submitting pending requests to the OData service.
        /// </summary>
        /// <returns></returns>
        public Task ExecuteAsync()
        {
            return ExecuteAsync(CancellationToken.None);
        }

        /// <summary>
        /// Executes the OData batch by submitting pending requests to the OData service.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public Task ExecuteAsync(CancellationToken cancellationToken)
        {
            return _client.ExecuteBatchAsync(_actions, cancellationToken);
        }
    }
}
