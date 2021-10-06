using System;

namespace Simple.OData.Client.Tests
{
    public class TypeWithIndexer
    {
        public string Name { get; set; }

        public char this[int index] => Name[index];
    }
}