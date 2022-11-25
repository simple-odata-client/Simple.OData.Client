using System;
using System.Collections.Generic;
using System.Linq;

namespace Simple.OData.Client;

public abstract class MetadataBase : IMetadata
{
	protected MetadataBase(INameMatchResolver nameMatchResolver, bool ignoreUnmappedProperties, bool unqualifiedNameCall)
	{
		IgnoreUnmappedProperties = ignoreUnmappedProperties;
		NameMatchResolver = nameMatchResolver;
		UnqualifiedNameCall = unqualifiedNameCall;
	}

	public bool IgnoreUnmappedProperties { get; }

	public INameMatchResolver NameMatchResolver { get; }

	public bool UnqualifiedNameCall { get; }

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

	public abstract string GetStructuralPropertyPath(string collectionName, params string[] propertyNames);

	public abstract IEnumerable<string> GetDeclaredKeyPropertyNames(string collectionName);

	public abstract IEnumerable<string> GetNavigationPropertyNames(string collectionName);

	public abstract IEnumerable<IEnumerable<string>> GetAlternateKeyPropertyNames(string collectionName);

	public abstract bool HasNavigationProperty(string collectionName, string propertyName);

	public abstract string GetNavigationPropertyExactName(string collectionName, string propertyName);

	public abstract string GetNavigationPropertyPartnerTypeName(string collectionName, string propertyName);

	public abstract bool IsNavigationPropertyCollection(string collectionName, string propertyName);

	public abstract string GetFunctionFullName(string functionName);

	public abstract EntityCollection? GetFunctionReturnCollection(string functionName);

	public abstract string GetFunctionVerb(string functionName);

	public abstract string GetActionFullName(string actionName);

	public abstract EntityCollection GetActionReturnCollection(string actionName);

	public EntityCollection GetEntityCollection(string collectionPath)
	{
		var segments = collectionPath.Split('/');
		if (segments.Length > 1)
		{
			if (SegmentsIncludeTypeSpecification(segments))
			{
				var baseEntitySet = GetEntityCollection(Utils.ExtractCollectionName(segments[segments.Length - 2]));
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
		{
			return rootCollection;
		}

		var associationName = GetNavigationPropertyExactName(rootCollection.Name, segments.First());
		var typeName = IsSingleSegmentWithTypeSpecification(segments)
			? segments.Last()
			: GetNavigationPropertyPartnerTypeName(rootCollection.Name, associationName);
		var entityCollection = GetEntityCollection(typeName);

		return segments.Count() == 1 || IsSingleSegmentWithTypeSpecification(segments)
			? entityCollection
			: NavigateToCollection(entityCollection, segments.Skip(1));
	}

	protected static bool SegmentsIncludeTypeSpecification(IEnumerable<string> segments)
	{
		return segments.Last().Contains(".");
	}

	protected static bool IsSingleSegmentWithTypeSpecification(IEnumerable<string> segments)
	{
		return segments.Count() == 2 && SegmentsIncludeTypeSpecification(segments);
	}

	public EntryDetails ParseEntryDetails(
		string collectionName,
		IDictionary<string, object> entryData,
		string? contentId = null)
	{
		var entryDetails = new EntryDetails();

		foreach (var item in entryData)
		{
			if (HasStructuralProperty(collectionName, item.Key))
			{
				entryDetails.AddProperty(item.Key, item.Value);
			}
			else if (HasNavigationProperty(collectionName, item.Key))
			{
				if (IsNavigationPropertyCollection(collectionName, item.Key))
				{
					switch (item.Value)
					{
						case null:
							entryDetails.AddLink(item.Key, null, contentId);
							break;
						case IEnumerable<object> collection:
							foreach (var element in collection)
							{
								entryDetails.AddLink(item.Key, element, contentId);
							}

							break;
					}
				}
				else
				{
					entryDetails.AddLink(item.Key, item.Value, contentId);
				}
			}
			else if (IsOpenType(collectionName))
			{
				entryDetails.HasOpenTypeProperties = true;
				entryDetails.AddProperty(item.Key, item.Value);
			}
			else if (!IgnoreUnmappedProperties)
			{
				throw new UnresolvableObjectException(item.Key, $"No property or association found for [{item.Key}].");
			}
		}

		return entryDetails;
	}

	public static IEnumerable<string> GetCollectionPathSegments(string path)
	{
		return path.Split('/').Select(x => x.Contains("(") ? x.Substring(0, x.IndexOf("(", StringComparison.Ordinal)) : x);
	}
}
