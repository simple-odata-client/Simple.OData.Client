using System;
using System.Collections.Generic;
using System.Linq;

namespace Simple.OData.Client
{
    public class ODataExpandAssociation : IEquatable<ODataExpandAssociation>
    {
        public ODataExpandAssociation(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException($"Parameter {nameof(name)} should not be null or empty.", nameof(name));
            Name = name;
        }
        
        public string Name { get; }
        
        public List<ODataExpandAssociation> ExpandAssociations { get; } = new List<ODataExpandAssociation>();
        
        public List<ODataOrderByColumn> OrderByColumns { get; }  = new List<ODataOrderByColumn>();
        
        public ODataExpression FilterExpression { get; set; }

        public static ODataExpandAssociation From(string association)
        {
            if (string.IsNullOrEmpty(association))
                throw new ArgumentException($"Parameter {nameof(association)} should not be null or empty.", nameof(association));
            
            var items = association.Split('/');
            var expandAssociation = new ODataExpandAssociation(items.First());
            var currentAssociation = expandAssociation;
            foreach (var item in items.Skip(1))
            {
                currentAssociation.ExpandAssociations.Add(new ODataExpandAssociation(item));
                currentAssociation = currentAssociation.ExpandAssociations.First();
            }

            return expandAssociation;
        }

        public ODataExpandAssociation Clone()
        {
            var clone = new ODataExpandAssociation(Name);
            clone.ExpandAssociations.AddRange(ExpandAssociations.Select(a => a.Clone()));
            clone.FilterExpression = FilterExpression;
            clone.OrderByColumns.AddRange(OrderByColumns);
            return clone;
        }

        public bool Equals(ODataExpandAssociation other)
        {
            if (other == null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Name == other.Name;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
    }
}