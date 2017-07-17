using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Simple.OData.Client.Core.Adapter
{
    public interface IAdapterFactory
    {
        bool CanLoadForVersion(string protocolVersion);
        IODataAdapter LoadAdapter(string protocolVersion, ISession session, string metadataDocument);
    }
}
