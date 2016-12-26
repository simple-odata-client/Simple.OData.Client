using System;
using System.Collections.Generic;
using System.Linq;

#pragma warning disable 1591

namespace Simple.OData.Client
{
    public abstract class MetadataBase : IMetadata
    {
        public abstract ISession Session { get; }

        public abstract string GetEntityCollectionExactName(string collectionName);
        public abstract string GetDerivedEntityTypeExactName(string collectionName, string entityTypeName);
        public abstract bool EntityCollectionRequiresOptimisticConcurrencyCheck(string collectionName);
        public abstract string GetEntityTypeExactName(string collectionName);
        public abstract string GetLinkedCollectionName(string instanceTypeName, string typeName, out bool isSingleton);
        public abstract string GetQualifiedTypeName(string collectionName);
        public abstract bool IsOpenType(string collectionName);
        public abstract bool IsTypeWithId(string collectionName);
        public abstract IEnumerable<string> GetStructuralPropertyNames(string collectionName);
        public abstract bool HasStructuralProperty(string collectionName, string propertyName);
        public abstract string GetStructuralPropertyExactName(string collectionName, string propertyName);
        public abstract IEnumerable<string> GetDeclaredKeyPropertyNames(string collectionName);
        public abstract bool HasNavigationProperty(string collectionName, string propertyName);
        public abstract string GetNavigationPropertyExactName(string collectionName, string propertyName);
        public abstract string GetNavigationPropertyPartnerTypeName(string collectionName, string propertyName);
        public abstract bool IsNavigationPropertyCollection(string collectionName, string propertyName);
        public abstract string GetFunctionFullName(string functionName);
        public abstract EntityCollection GetFunctionReturnCollection(string functionName);
        public abstract string GetFunctionVerb(string functionName);
        public abstract string GetActionFullName(string actionName);
        public abstract EntityCollection GetActionReturnCollection(string actionName);

        public EntityCollection GetEntityCollection(string collectionPath)
        {
            var segments = collectionPath.Split('/');
            if (segments.Count() > 1)
            {
                if (SegmentsIncludeTypeSpecification(segments))
                {
                    var baseEntitySet = this.GetEntityCollection(Utils.ExtractCollectionName(segments[segments.Length - 2]));
                    return GetDerivedEntityCollection(baseEntitySet, Utils.ExtractCollectionName(segments.Last()));
                }
                else
                {
                    return new EntityCollection(GetEntityCollectionExactName(Utils.ExtractCollectionName(segments.Last())));
                }
            }
            else
            {
                return new EntityCollection(GetEntityCollectionExactName(Utils.ExtractCollectionName(collectionPath)));
            }
        }

        public EntityCollection GetDerivedEntityCollection(EntityCollection baseCollection, string entityTypeName)
        {
            var actualName = GetDerivedEntityTypeExactName(baseCollection.Name, entityTypeName);
            return new EntityCollection(actualName, baseCollection);
        }

        public EntityCollection NavigateToCollection(string path)
        {
            var segments = GetCollectionPathSegments(path);
            return IsSingleSegmentWithTypeSpecification(segments)
                ? GetEntityCollection(path)
                : NavigateToCollection(GetEntityCollection(segments.First()), segments.Skip(1));
        }

        public EntityCollection NavigateToCollection(EntityCollection rootCollection, string path)
        {
            return NavigateToCollection(rootCollection, GetCollectionPathSegments(path));
        }

        private EntityCollection NavigateToCollection(EntityCollection rootCollection, IEnumerable<string> segments)
        {
            if (!segments.Any())
                return rootCollection;

            var associationName = GetNavigationPropertyExactName(rootCollection.Name, segments.First());
            var typeName = IsSingleSegmentWithTypeSpecification(segments)
                ? segments.Last()
                : GetNavigationPropertyPartnerTypeName(rootCollection.Name, associationName);
            var entityCollection = GetEntityCollection(typeName);

            return segments.Count() == 1 || IsSingleSegmentWithTypeSpecification(segments)
                ? entityCollection
                : NavigateToCollection(entityCollection, segments.Skip(1));
        }

        protected bool SegmentsIncludeTypeSpecification(IEnumerable<string> segments)
        {
            return segments.Last().Contains(".");
        }

        protected bool IsSingleSegmentWithTypeSpecification(IEnumerable<string> segments)
        {
            return segments.Count() == 2 && SegmentsIncludeTypeSpecification(segments);
        }

        public EntryDetails ParseEntryDetails(string collectionName, IDictionary<string, object> entryData, string contentId = null)
        {
            var entryDetails = new EntryDetails();

            foreach (var item in entryData)
            {
                if (this.HasStructuralProperty(collectionName, item.Key))
                {
                    entryDetails.AddProperty(item.Key, item.Value);
                }
                else if (this.HasNavigationProperty(collectionName, item.Key))
                {
                    if (this.IsNavigationPropertyCollection(collectionName, item.Key))
                    {
                        if (item.Value == null)
                        {
                            entryDetails.AddLink(item.Key, null, contentId);
                        }
                        else
                        {
                            var collection = item.Value as IEnumerable<object>;
                            if (collection != null)
                            {
                                foreach (var element in collection)
                                {
                                    entryDetails.AddLink(item.Key, element, contentId);
                                }
                            }
                        }
                    }
                    else
                    {
                        entryDetails.AddLink(item.Key, item.Value, contentId);
                    }
                }
                else if (this.IsOpenType(collectionName))
                {
                    entryDetails.HasOpenTypeProperties = true;
                    entryDetails.AddProperty(item.Key, item.Value);
                }
                else if (!this.Session.Settings.IgnoreUnmappedProperties)
                {
                    throw new UnresolvableObjectException(item.Key, String.Format("No property or association found for [{0}].", item.Key));
                }
            }

            return entryDetails;
        }

        public IEnumerable<string> GetCollectionPathSegments(string path)
        {
            return path.Split('/').Select(x => x.Contains("(") ? x.Substring(0, x.IndexOf("(")) : x);
        }
    }
}