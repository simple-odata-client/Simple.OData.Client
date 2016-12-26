using System.Collections.Generic;

#pragma warning disable 1591

namespace Simple.OData.Client
{
    public class ReferenceLink
    {
        public string LinkName { get; set; }
        public object LinkData { get; set; }
        public string ContentId { get; set; }
    }

    public class EntryDetails
    {
        private readonly IDictionary<string, object> _properties = new Dictionary<string, object>();
        private readonly IDictionary<string, List<ReferenceLink>> _links = new Dictionary<string, List<ReferenceLink>>();

        public IDictionary<string, object> Properties { get { return _properties; } }
        public IDictionary<string, List<ReferenceLink>> Links { get { return _links; } }
        public bool HasOpenTypeProperties { get; set; }

        public void AddProperty(string propertyName, object propertyValue)
        {
            _properties.Add(propertyName, propertyValue);
        }

        public void AddLink(string linkName, object linkData, string contentId = null)
        {
            List<ReferenceLink> links;
            if (!_links.TryGetValue(linkName, out links))
            {
                links = new List<ReferenceLink>();
                _links.Add(linkName, links);
            }
            links.Add(new ReferenceLink() { LinkName = linkName, LinkData = linkData, ContentId = contentId });
        }
    }
}

