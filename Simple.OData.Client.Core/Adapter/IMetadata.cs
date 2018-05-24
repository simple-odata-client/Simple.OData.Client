using System.Collections.Generic;

#pragma warning disable 1591

namespace Simple.OData.Client
{
    public interface IMetadata
    {
        ISession Session { get; }

        EntityCollection GetEntityCollection(string collectionPath);
        EntityCollection GetDerivedEntityCollection(EntityCollection baseCollection, string entityTypeName);
        EntityCollection NavigateToCollection(string path);
        EntityCollection NavigateToCollection(EntityCollection rootCollection, string path);

        string GetEntityCollectionExactName(string collectionName);
        bool EntityCollectionRequiresOptimisticConcurrencyCheck(string collectionName);

        string GetEntityTypeExactName(string collectionName);
        string GetLinkedCollectionName(string instanceTypeName, string typeName, out bool isSingleton);

        string GetQualifiedTypeName(string typeOrCollectionName);

        bool IsOpenType(string collectionName);
        bool IsTypeWithId(string typeName);
        IEnumerable<string> GetStructuralPropertyNames(string collectionName);
        bool HasStructuralProperty(string collectionName, string propertyName);
        string GetStructuralPropertyExactName(string collectionName, string propertyName);
        string GetStructuralPropertyPath(string collectionName, params string[] propertyNames);
        IEnumerable<string> GetDeclaredKeyPropertyNames(string collectionName);

        bool HasNavigationProperty(string collectionName, string propertyName);
        string GetNavigationPropertyExactName(string collectionName, string propertyName);
        string GetNavigationPropertyPartnerTypeName(string collectionName, string propertyName);
        bool IsNavigationPropertyCollection(string collectionName, string propertyName);

        string GetFunctionFullName(string functionName);
        EntityCollection GetFunctionReturnCollection(string functionName);
        string GetFunctionVerb(string functionName);
        string GetActionFullName(string actionName);
        EntityCollection GetActionReturnCollection(string functionName);

        EntryDetails ParseEntryDetails(string collectionName, IDictionary<string, object> entryData, string contentId = null);
    }
}