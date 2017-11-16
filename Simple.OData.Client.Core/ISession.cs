using System;
using System.Net;
using System.Net.Http;

namespace Simple.OData.Client
{
    /// <summary>
    /// Provide access to session-specific details.
    /// </summary>
    public interface ISession : IDisposable
    {
        /// <summary>
        /// Gets OData client configuration settings.
        /// </summary>
        ODataClientSettings Settings { get; }

        /// <summary>
        /// Gets OData client adapter.
        /// </summary>
        IODataAdapter Adapter { get; }

        /// <summary>
        /// Gets OData service metadata.
        /// </summary>
        IMetadata Metadata { get; }

        /// <summary>
        /// Writes a trace message.
        /// </summary>
        /// <param name="message">Trace message format string.</param>
        /// <param name="messageParams">Trace message parameters.</param>
        void Trace(string message, params object[] messageParams);

        /// <summary>
        /// Obtains an instance of <see cref="HttpConnection"/> that is used to issue HTTP requests to OData service.
        /// </summary>
        /// <returns>An <see cref="HttpClient"/> instance.</returns>
        HttpConnection GetHttpConnection();
    }
}