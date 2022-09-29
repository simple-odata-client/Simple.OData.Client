using System.Collections.Generic;

namespace Simple.OData.Client;

/// <summary>
/// Abstraction over the OData Edm model.
/// </summary>
public interface IMetadata
{
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

	IEnumerable<string> GetNavigationPropertyNames(string collectionName);

	/// <summary>
	/// Gets a collection of key name collections that represent the alternate keys of the given entity.
	/// Alternate keys are only supported on V4. On V3 this method will always return an empty enumeration.
	/// </summary>
	/// <see cref="https://github.com/OData/vocabularies/blob/master/OData.Community.Keys.V1.md"/>
	/// <param name="collectionName">The collection name of the entity</param>
	/// <returns>An empty enumeration of string enumerations representing the key names</returns>
	IEnumerable<IEnumerable<string>> GetAlternateKeyPropertyNames(string collectionName);

	bool HasNavigationProperty(string collectionName, string propertyName);

	string GetNavigationPropertyExactName(string collectionName, string propertyName);

	string GetNavigationPropertyPartnerTypeName(string collectionName, string propertyName);

	bool IsNavigationPropertyCollection(string collectionName, string propertyName);

	string GetFunctionFullName(string functionName);

	EntityCollection? GetFunctionReturnCollection(string functionName);

	string GetFunctionVerb(string functionName);

	string GetActionFullName(string actionName);

	EntityCollection GetActionReturnCollection(string functionName);

	EntryDetails ParseEntryDetails(
		string collectionName,
		IDictionary<string, object> entryData,
		string? contentId = null);
}
