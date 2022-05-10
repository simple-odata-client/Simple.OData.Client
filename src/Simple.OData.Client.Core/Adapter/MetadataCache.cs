using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Simple.OData.Client.Adapter;

/// <summary>
/// A caching layer for <see cref="IMetadata"/>
/// </summary>
public class MetadataCache : IMetadata
{
	private readonly IMetadata metadata;
	private readonly ConcurrentDictionary<string, EntityCollection> ec;
	private readonly ConcurrentDictionary<string, EntityCollection> nav;
	private readonly ConcurrentDictionary<string, string> ecen;
	private readonly ConcurrentDictionary<string, bool> roc;
	private readonly ConcurrentDictionary<string, string> eten;
	private readonly ConcurrentDictionary<string, string> qtn;
	private readonly ConcurrentDictionary<string, bool> ot;
	private readonly ConcurrentDictionary<string, bool> tid;
	private readonly ConcurrentDictionary<string, bool> sp;
	private readonly ConcurrentDictionary<string, bool> np;
	private readonly ConcurrentDictionary<string, IList<string>> npn;
	private readonly ConcurrentDictionary<string, bool> npc;
	private readonly ConcurrentDictionary<string, IList<string>> spns;
	private readonly ConcurrentDictionary<string, IList<string>> dkpns;
	private readonly ConcurrentDictionary<string, IList<IList<string>>> akpns;
	private readonly ConcurrentDictionary<string, string> npen;
	private readonly ConcurrentDictionary<string, string> spp;
	private readonly ConcurrentDictionary<string, string> ffn;
	private readonly ConcurrentDictionary<string, string> fv;
	private readonly ConcurrentDictionary<string, string> afn;
	private readonly ConcurrentDictionary<string, string> nppt;
	private readonly ConcurrentDictionary<string, EntityCollection> arc;
	private readonly ConcurrentDictionary<string, EntityCollection> frc;
	private readonly ConcurrentDictionary<string, string> spen;

	public MetadataCache(IMetadata metadata)
	{
		this.metadata = metadata;
		IgnoreUnmappedProperties = (metadata as MetadataBase).IgnoreUnmappedProperties;

		ec = new ConcurrentDictionary<string, EntityCollection>();
		nav = new ConcurrentDictionary<string, EntityCollection>();
		ecen = new ConcurrentDictionary<string, string>();
		roc = new ConcurrentDictionary<string, bool>();
		eten = new ConcurrentDictionary<string, string>();
		qtn = new ConcurrentDictionary<string, string>();
		ot = new ConcurrentDictionary<string, bool>();
		tid = new ConcurrentDictionary<string, bool>();
		sp = new ConcurrentDictionary<string, bool>();
		np = new ConcurrentDictionary<string, bool>();
		npn = new ConcurrentDictionary<string, IList<string>>();
		npc = new ConcurrentDictionary<string, bool>();
		spns = new ConcurrentDictionary<string, IList<string>>();
		dkpns = new ConcurrentDictionary<string, IList<string>>();
		akpns = new ConcurrentDictionary<string, IList<IList<string>>>();
		spp = new ConcurrentDictionary<string, string>();
		npen = new ConcurrentDictionary<string, string>();
		ffn = new ConcurrentDictionary<string, string>();
		fv = new ConcurrentDictionary<string, string>();
		afn = new ConcurrentDictionary<string, string>();
		nppt = new ConcurrentDictionary<string, string>();
		arc = new ConcurrentDictionary<string, EntityCollection>();
		frc = new ConcurrentDictionary<string, EntityCollection>();
		spen = new ConcurrentDictionary<string, string>();
	}

	public bool IgnoreUnmappedProperties { get; }

	public EntityCollection GetEntityCollection(string collectionPath)
	{
		return ec.GetOrAdd(collectionPath, x => metadata.GetEntityCollection(x));
	}

	public EntityCollection GetDerivedEntityCollection(EntityCollection baseCollection, string entityTypeName)
	{
		// Can't easily cache as the key would be a collection
		return metadata.GetDerivedEntityCollection(baseCollection, entityTypeName);
	}

	public EntityCollection NavigateToCollection(string path)
	{
		return nav.GetOrAdd(path, x => metadata.NavigateToCollection(x));
	}

	public EntityCollection NavigateToCollection(EntityCollection rootCollection, string path)
	{
		// Can't easily cache as the key would be a collection
		return metadata.NavigateToCollection(rootCollection, path);
	}

	public string GetEntityCollectionExactName(string collectionName)
	{
		return ecen.GetOrAdd(collectionName, x => metadata.GetEntityCollectionExactName(x));
	}

	public bool EntityCollectionRequiresOptimisticConcurrencyCheck(string collectionName)
	{
		return roc.GetOrAdd(collectionName, x => metadata.EntityCollectionRequiresOptimisticConcurrencyCheck(x));
	}

	public string GetEntityTypeExactName(string collectionName)
	{
		return eten.GetOrAdd(collectionName, x => metadata.GetEntityTypeExactName(x));
	}

	public string GetLinkedCollectionName(string instanceTypeName, string typeName, out bool isSingleton)
	{
		// Not sure if we can handle the out isSingleton side effect
		return metadata.GetLinkedCollectionName(instanceTypeName, typeName, out isSingleton);
	}

	public string GetQualifiedTypeName(string typeOrCollectionName)
	{
		return qtn.GetOrAdd(typeOrCollectionName, x => metadata.GetQualifiedTypeName(x));
	}

	public bool IsOpenType(string collectionName)
	{
		return ot.GetOrAdd(collectionName, x => metadata.IsOpenType(x));
	}

	public bool IsTypeWithId(string typeName)
	{
		return tid.GetOrAdd(typeName, x => metadata.IsTypeWithId(x));
	}

	public IEnumerable<string> GetStructuralPropertyNames(string collectionName)
	{
		return spns.GetOrAdd(collectionName, x => metadata.GetStructuralPropertyNames(collectionName).ToList());
	}

	public bool HasStructuralProperty(string collectionName, string propertyName)
	{
		return sp.GetOrAdd($"{collectionName}/{propertyName}", x => metadata.HasStructuralProperty(collectionName, propertyName));
	}

	public string GetStructuralPropertyExactName(string collectionName, string propertyName)
	{
		return spen.GetOrAdd($"{collectionName}/{propertyName}", x => metadata.GetStructuralPropertyExactName(collectionName, propertyName));
	}

	public string GetStructuralPropertyPath(string collectionName, params string[] propertyNames)
	{
		return spp.GetOrAdd(string.Join("/", propertyNames), x => metadata.GetStructuralPropertyPath(collectionName, propertyNames));
	}

	public IEnumerable<string> GetDeclaredKeyPropertyNames(string collectionName)
	{
		return dkpns.GetOrAdd(collectionName, x => metadata.GetDeclaredKeyPropertyNames(collectionName).ToList());
	}

	public IEnumerable<string> GetNavigationPropertyNames(string collectionName)
	{
		return npn.GetOrAdd(collectionName, x => metadata.GetNavigationPropertyNames(collectionName).ToList());
	}

	public IEnumerable<IEnumerable<string>> GetAlternateKeyPropertyNames(string collectionName)
	{
		return akpns.GetOrAdd(collectionName, x => metadata.GetAlternateKeyPropertyNames(collectionName).Select(y => (IList<string>)y.ToList()).ToList());
	}

	public bool HasNavigationProperty(string collectionName, string propertyName)
	{
		return np.GetOrAdd($"{collectionName}/{propertyName}", x => metadata.HasNavigationProperty(collectionName, propertyName));
	}

	public string GetNavigationPropertyExactName(string collectionName, string propertyName)
	{
		return npen.GetOrAdd($"{collectionName}/{propertyName}", x => metadata.GetNavigationPropertyExactName(collectionName, propertyName));
	}

	public string GetNavigationPropertyPartnerTypeName(string collectionName, string propertyName)
	{
		return nppt.GetOrAdd($"{collectionName}/{propertyName}", x => metadata.GetNavigationPropertyPartnerTypeName(collectionName, propertyName));
	}

	public bool IsNavigationPropertyCollection(string collectionName, string propertyName)
	{
		return npc.GetOrAdd($"{collectionName}/{propertyName}", x => metadata.IsNavigationPropertyCollection(collectionName, propertyName));
	}

	public string GetFunctionFullName(string functionName)
	{
		return ffn.GetOrAdd(functionName, x => metadata.GetFunctionFullName(x));
	}

	public EntityCollection GetFunctionReturnCollection(string functionName)
	{
		return frc.GetOrAdd(functionName, x => metadata.GetFunctionReturnCollection(x));
	}

	public string GetFunctionVerb(string functionName)
	{
		return fv.GetOrAdd(functionName, x => metadata.GetFunctionVerb(x));
	}

	public string GetActionFullName(string actionName)
	{
		return afn.GetOrAdd(actionName, x => metadata.GetActionFullName(x));
	}

	public EntityCollection GetActionReturnCollection(string functionName)
	{
		return arc.GetOrAdd(functionName, x => metadata.GetActionReturnCollection(functionName));
	}

	public EntryDetails ParseEntryDetails(
		string collectionName,
		IDictionary<string, object> entryData,
		string? contentId = null)
	{
		// Copied from MetadataBase so we use caches for the property acquisition
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
}
