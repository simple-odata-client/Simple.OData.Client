using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable 1591

namespace Simple.OData.Client
{
    public abstract class ODataModelAdapterBase : IODataModelAdapter
    {
        public abstract AdapterVersion AdapterVersion { get; }

        public string ProtocolVersion { get; set; }
 
        public object Model { get; set; }
    }
}
