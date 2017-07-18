using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simple.OData.Client.Core.Adapter
{
    /// <summary>
    /// Implementing classes provide IODataAdapters for specific OData protocol versions
    /// </summary>
    public interface IAdapterFactory
    {
        /// <summary>
        /// Determines if the factory can provide an IODataAdapter for the specified OData protool version
        /// </summary>
        /// <param name="protocolVersion">OData protocol version</param>
        /// <returns>true if the factory can create an adapter for the passed protocol version, false otherwise</returns>
        bool CanLoadForVersion(string protocolVersion);

        /// <summary>
        /// Creates a new IODataAdapter for the passed protocol version with the specified ISession and metadata
        /// </summary>
        /// <param name="protocolVersion">OData protool version</param>
        /// <param name="session">ISession</param>
        /// <param name="metadataDocument">service metadata (as xml string)</param>
        /// <returns></returns>
        IODataAdapter LoadAdapter(string protocolVersion, ISession session, string metadataDocument);
    }
}
