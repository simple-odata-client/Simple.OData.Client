using System;

#pragma warning disable 1591

namespace Simple.OData.Client
{
    public interface IODataModelAdapter
    {
        AdapterVersion AdapterVersion { get; }
        string ProtocolVersion { get; set; }
        object Model { get; set;  }
    }
}
