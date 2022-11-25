using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.OData.Edm;
using Microsoft.OData.Edm.Vocabularies;

namespace Simple.OData.Client.V4.Adapter;

public class Metadata : MetadataBase
{
	private readonly IEdmModel _model;

	public Metadata(IEdmModel model, INameMatchResolver nameMatchResolver, bool ignoreUnmappedProperties, bool unqualifiedNameCall) : base(nameMatchResolver, ignoreUnmappedProperties, unqualifiedNameCall)
	{
		_model = model;
	}

	public override string GetEntityCollectionExactName(string collectionName)
	{
		if (TryGetEntitySet(collectionName, out var entitySet))
		{
			return entitySet.Name;
		}
		else if (TryGetSingleton(collectionName, out var singleton))
		{
			return singleton.Name;
		}
		else if (TryGetEntityType(collectionName, out var entityType))
		{
			return entityType.Name;
		}

		throw new UnresolvableObjectException(collectionName, $"Entity collection [{collectionName}] not found");
	}

	public override bool EntityCollectionRequiresOptimisticConcurrencyCheck(string collectionName)
	{
		IEdmVocabularyAnnotatable? annotatable = null;
		if (TryGetEntitySet(collectionName, out var entitySet))
		{
			annotatable = entitySet;
		}
		else if (TryGetSingleton(collectionName, out var singleton))
		{
			annotatable = singleton;
		}
		else if (TryGetEntityType(collectionName, out var entityType))
		{
			annotatable = entityType;
		}
		else
		{
			return false;
		}

		return annotatable.VocabularyAnnotations(_model).Any(x => x.Term.Name == "OptimisticConcurrency");
	}

	public override string GetDerivedEntityTypeExactName(string collectionName, string entityTypeName)
	{
		IEdmEntityType entityType;
		if (TryGetEntitySet(collectionName, out var entitySet))
		{
			entityType = (_model.FindAllDerivedTypes(entitySet.EntityType())
				.BestMatch(x => (x as IEdmEntityType).Name, entityTypeName, NameMatchResolver)) as IEdmEntityType;
			if (entityType != null)
			{
				return entityType.Name;
			}
		}
		else if (TryGetSingleton(collectionName, out var singleton))
		{
			entityType = (_model.FindDirectlyDerivedTypes(singleton.EntityType())
				.BestMatch(x => (x as IEdmEntityType).Name, entityTypeName, NameMatchResolver)) as IEdmEntityType;
			if (entityType != null)
			{
				return entityType.Name;
			}
		}
		else if (TryGetEntityType(entityTypeName, out entityType))
		{
			return entityType.Name;
		}

		throw new UnresolvableObjectException(entityTypeName, $"Entity type [{entityTypeName}] not found");
	}

	public override string GetEntityTypeExactName(string collectionName)
	{
		var entityType = GetEntityTypes().BestMatch(x => x.Name, collectionName, NameMatchResolver);
		if (entityType != null)
		{
			return entityType.Name;
		}

		throw new UnresolvableObjectException(collectionName, $"Entity type [{collectionName}] not found");
	}

	public override string GetLinkedCollectionName(string instanceTypeName, string typeName, out bool isSingleton)
	{
		isSingleton = false;

		if (TryGetEntitySet(instanceTypeName, out var entitySet))
		{
			return entitySet.Name;
		}

		if (TryGetSingleton(instanceTypeName, out var singleton))
		{
			isSingleton = true;
			return singleton.Name;
		}

		if (TryGetEntityType(instanceTypeName, out var entityType))
		{
			return entityType.Name;
		}

		if (TryGetEntitySet(typeName, out entitySet))
		{
			return entitySet.Name;
		}

		if (TryGetSingleton(typeName, out singleton))
		{
			isSingleton = true;
			return singleton.Name;
		}

		if (TryGetEntityType(typeName, out entityType))
		{
			return entityType.Name;
		}

		throw new UnresolvableObjectException(typeName, $"Linked collection for type [{instanceTypeName}] not found");
	}

	public override string GetQualifiedTypeName(string typeName)
	{
		if (TryGetEntityType(typeName, out var entityType))
		{
			return string.Join(".", entityType.Namespace, entityType.Name);
		}

		if (TryGetComplexType(typeName, out var complexType))
		{
			return string.Join(".", complexType.Namespace, complexType.Name);
		}

		if (TryGetEnumType(typeName, out var enumType))
		{
			return string.Join(".", enumType.Namespace, enumType.Name);
		}

		throw new UnresolvableObjectException(typeName, $"Type [{typeName}] not found");
	}

	public override bool IsOpenType(string collectionName)
	{
		return GetEntityType(collectionName).IsOpen;
	}

	public override bool IsTypeWithId(string collectionName)
	{
		if (TryGetEntityType(collectionName, out var entityType))
		{
			return entityType.DeclaredKey != null;
		}
		else
		{
			return false;
		}
	}

	public override IEnumerable<string> GetStructuralPropertyNames(string collectionName)
	{
		return GetEntityType(collectionName).StructuralProperties().Select(x => x.Name);
	}

	public override bool HasStructuralProperty(string collectionName, string propertyName)
	{
		return GetEntityType(collectionName).StructuralProperties().Any(x => NameMatchResolver.IsMatch(x.Name, propertyName));
	}

	public override string GetStructuralPropertyExactName(string collectionName, string propertyName)
	{
		return GetStructuralProperty(collectionName, propertyName).Name;
	}

	public override string GetStructuralPropertyPath(string collectionName, params string[]? propertyNames)
	{
		if (propertyNames == null || propertyNames.Length == 0)
		{
			throw new ArgumentNullException(nameof(propertyNames));
		}

		var property = GetStructuralProperty(collectionName, propertyNames[0]);
		var exactNames = new List<string> { property.Name };

		for (var i = 1; i < propertyNames.Length; i++)
		{
			var entityType = GetComplexType(property.Type.FullName());
			property = GetStructuralProperty(entityType, propertyNames[i]);
			exactNames.Add(property.Name);

			if (property.Type.IsPrimitive())
			{
				break;
			}
		}

		return string.Join("/", exactNames.ToArray());
	}

	public override bool HasNavigationProperty(string collectionName, string propertyName)
	{
		return GetEntityType(collectionName).NavigationProperties().Any(x => NameMatchResolver.IsMatch(x.Name, propertyName));
	}

	public override string GetNavigationPropertyExactName(string collectionName, string propertyName)
	{
		return GetNavigationProperty(collectionName, propertyName).Name;
	}

	public override string GetNavigationPropertyPartnerTypeName(string collectionName, string propertyName)
	{
		var navigationProperty = GetNavigationProperty(collectionName, propertyName);
		if (!TryGetEntityType(navigationProperty.Type, out var entityType))
		{
			throw new UnresolvableObjectException(propertyName, $"No association found for [{propertyName}].");
		}

		return entityType.Name;
	}

	public override bool IsNavigationPropertyCollection(string collectionName, string propertyName)
	{
		var property = GetNavigationProperty(collectionName, propertyName);
		return property.Type.Definition.TypeKind == EdmTypeKind.Collection;
	}

	public override IEnumerable<string> GetDeclaredKeyPropertyNames(string collectionName)
	{
		var entityType = GetEntityType(collectionName);
		while (entityType.DeclaredKey == null && entityType.BaseEntityType() != null)
		{
			entityType = entityType.BaseEntityType();
		}

		if (entityType.DeclaredKey == null)
		{
			return Array.Empty<string>();
		}

		return entityType.DeclaredKey.Select(x => x.Name);
	}

	/// <summary>
	/// Gets a collection of key name collections that represent the alternate keys of the given entity
	/// </summary>
	/// <see cref="https://github.com/OData/vocabularies/blob/master/OData.Community.Keys.V1.md"/>
	/// <param name="collectionName">The collection name of the entity</param>
	/// <returns>An enumeration of string enumerations representing the key names</returns>
	public override IEnumerable<IEnumerable<string>> GetAlternateKeyPropertyNames(string collectionName)
	{
		var entityType = GetEntityType(collectionName);
		return _model.GetAlternateKeysAnnotation(entityType)
			.Select(x => x.Select(y => y.Key));
	}

	public override IEnumerable<string> GetNavigationPropertyNames(string collectionName)
	{
		return GetEntityType(collectionName).NavigationProperties().Select(x => x.Name);
	}

	public override string GetFunctionFullName(string functionName)
	{
		var function = GetFunction(functionName);
		return function.IsBound && !UnqualifiedNameCall ? function.ShortQualifiedName() : function.Name;
	}

	public override EntityCollection? GetFunctionReturnCollection(string functionName)
	{
		var function = GetFunction(functionName);

		if (function.ReturnType == null)
		{
			return null;
		}

		return !TryGetEntityType(function.ReturnType, out var entityType) ? null : new EntityCollection(entityType.Name);
	}

	public override string GetFunctionVerb(string functionName)
	{
		return RestVerbs.Get;
	}

	public override string GetActionFullName(string actionName)
	{
		var action = GetAction(actionName);
		return action.IsBound && !UnqualifiedNameCall ? action.ShortQualifiedName() : action.Name;
	}

	public override EntityCollection? GetActionReturnCollection(string actionName)
	{
		var action = GetAction(actionName);

		if (action.ReturnType == null)
		{
			return null;
		}

		return !TryGetEntityType(action.ReturnType, out var entityType) ? null : new EntityCollection(entityType.Name);
	}

	private IEnumerable<IEdmEntitySet> GetEntitySets()
	{
		return _model.SchemaElements
			.Where(x => x.SchemaElementKind == EdmSchemaElementKind.EntityContainer)
			.SelectMany(x => (x as IEdmEntityContainer).EntitySets());
	}

	private bool TryGetEntitySet(string entitySetName, out IEdmEntitySet entitySet)
	{
		if (entitySetName.Contains("/"))
		{
			entitySetName = entitySetName.Split('/').First();
		}

		entitySet = _model.SchemaElements
			.Where(x => x.SchemaElementKind == EdmSchemaElementKind.EntityContainer)
			.SelectMany(x => (x as IEdmEntityContainer).EntitySets())
			.BestMatch(x => x.Name, entitySetName, NameMatchResolver);

		return entitySet != null;
	}

	private IEnumerable<IEdmSingleton> GetSingletons()
	{
		return _model.SchemaElements
			.Where(x => x.SchemaElementKind == EdmSchemaElementKind.EntityContainer)
			.SelectMany(x => (x as IEdmEntityContainer).Singletons());
	}

	private bool TryGetSingleton(string singletonName, out IEdmSingleton singleton)
	{
		if (singletonName.Contains("/"))
		{
			singletonName = singletonName.Split('/').First();
		}

		singleton = _model.SchemaElements
			.Where(x => x.SchemaElementKind == EdmSchemaElementKind.EntityContainer)
			.SelectMany(x => (x as IEdmEntityContainer).Singletons())
			.BestMatch(x => x.Name, singletonName, NameMatchResolver);

		return singleton != null;
	}

	private IEnumerable<IEdmEntityType> GetEntityTypes()
	{
		return _model.SchemaElements
			.Where(x => x.SchemaElementKind == EdmSchemaElementKind.TypeDefinition && (x as IEdmType).TypeKind == EdmTypeKind.Entity)
			.Select(x => x as IEdmEntityType);
	}

	private IEdmEntityType GetEntityType(string collectionName)
	{
		if (TryGetEntityType(collectionName, out var entityType))
		{
			return entityType;
		}

		throw new UnresolvableObjectException(collectionName, $"Entity type [{collectionName}] not found");
	}

	private bool TryGetEntityType(string collectionName, out IEdmEntityType? entityType)
	{
		entityType = null;
		if (collectionName.Contains("/"))
		{
			var segments = GetCollectionPathSegments(collectionName).ToList();

			if (SegmentsIncludeTypeSpecification(segments))
			{
				var derivedTypeName = segments.Last();
				var derivedType = GetEntityTypes().SingleOrDefault(x => x.FullName() == derivedTypeName);
				if (derivedType != null)
				{
					entityType = derivedType;
					return true;
				}
			}
			else
			{
				var collection = NavigateToCollection(collectionName);
				var entityTypes = GetEntityTypes();
				entityType = entityTypes.SingleOrDefault(x => x.Name == collection.Name);
				if (entityType != null)
				{
					return true;
				}
				else
				{
					entityType = entityTypes.BestMatch(x => x.Name, collection.Name, NameMatchResolver);
					if (entityType != null)
					{
						return true;
					}
				}
			}
		}
		else
		{
			var entitySet = GetEntitySets()
				.BestMatch(x => x.Name, collectionName, NameMatchResolver);
			if (entitySet != null)
			{
				entityType = entitySet.EntityType();
				return true;
			}

			var singleton = GetSingletons()
				.BestMatch(x => x.Name, collectionName, NameMatchResolver);
			if (singleton != null)
			{
				entityType = singleton.EntityType();
				return true;
			}

			var derivedType = GetEntityTypes().BestMatch(x => x.Name, collectionName, NameMatchResolver);
			if (derivedType != null)
			{
				var baseType = GetEntityTypes()
					.SingleOrDefault(x => _model.FindDirectlyDerivedTypes(x).Contains(derivedType));
				if (baseType != null && GetEntitySets().Any(x => x.EntityType() == baseType))
				{
					entityType = derivedType;
					return true;
				}
				// Check if we can return it anyway
				entityType = derivedType;
				return true;
			}
		}

		return false;
	}

	private static bool TryGetEntityType(IEdmTypeReference typeReference, out IEdmEntityType entityType)
	{
		entityType = typeReference.Definition.TypeKind == EdmTypeKind.Collection
			? (typeReference.Definition as IEdmCollectionType).ElementType.Definition as IEdmEntityType
			: typeReference.Definition.TypeKind == EdmTypeKind.Entity
			? typeReference.Definition as IEdmEntityType
			: null;
		return entityType != null;
	}

	private IEdmComplexType GetComplexType(string typeName)
	{
		if (TryGetComplexType(typeName, out var complexType))
		{
			return complexType;
		}

		throw new UnresolvableObjectException(typeName, $"ComplexType [{typeName}] not found");
	}

	private bool TryGetComplexType(string typeName, out IEdmComplexType complexType)
	{
		complexType = _model.SchemaElements
			.Where(x => x.SchemaElementKind == EdmSchemaElementKind.TypeDefinition && (x as IEdmType).TypeKind == EdmTypeKind.Complex)
			.Select(x => x as IEdmComplexType)
			.BestMatch(x => x.Name, typeName, NameMatchResolver);

		return complexType != null;
	}

	private bool TryGetEnumType(string typeName, out IEdmEnumType enumType)
	{
		enumType = _model.SchemaElements
			.Where(x => x.SchemaElementKind == EdmSchemaElementKind.TypeDefinition && (x as IEdmType).TypeKind == EdmTypeKind.Enum)
			.Select(x => x as IEdmEnumType)
			.BestMatch(x => x.Name, typeName, NameMatchResolver);

		return enumType != null;
	}

	private IEdmStructuralProperty GetStructuralProperty(string collectionName, string propertyName)
	{
		var edmType = GetEntityType(collectionName);
		return GetStructuralProperty(edmType, propertyName);
	}

	private IEdmStructuralProperty GetStructuralProperty(IEdmStructuredType edmType, string propertyName)
	{
		var property = edmType.StructuralProperties().BestMatch(
			x => x.Name, propertyName, NameMatchResolver);

		if (property == null)
		{
			throw new UnresolvableObjectException(propertyName, $"Structural property [{propertyName}] not found");
		}

		return property;
	}

	private IEdmNavigationProperty GetNavigationProperty(string collectionName, string propertyName)
	{
		var property = GetEntityType(collectionName).NavigationProperties()
			.BestMatch(x => x.Name, propertyName, NameMatchResolver);

		if (property == null)
		{
			throw new UnresolvableObjectException(propertyName, $"Association [{propertyName}] not found");
		}

		return property;
	}

	private IEdmFunction GetFunction(string functionName)
	{
		IEdmFunction? function = null;
		if (_model.SchemaElements
			.Where(x => x.SchemaElementKind == EdmSchemaElementKind.EntityContainer)
			.SelectMany(x => (x as IEdmEntityContainer).Elements
				.Where(y => y.ContainerElementKind == EdmContainerElementKind.FunctionImport))
				.BestMatch(x => x.Name, functionName, NameMatchResolver) is IEdmFunctionImport functionImport)
		{
			function = functionImport.Function;
		}

		if (function == null)
		{
			function = _model.SchemaElements
				.BestMatch(x => x.SchemaElementKind == EdmSchemaElementKind.Function,
					x => x.Name, functionName, NameMatchResolver) as IEdmFunction;
		}

		if (function == null)
		{
			throw new UnresolvableObjectException(functionName, $"Function [{functionName}] not found");
		}

		return function;
	}

	private IEdmAction GetAction(string actionName)
	{
		IEdmAction? action = null;
		if (_model.SchemaElements
			.Where(x => x.SchemaElementKind == EdmSchemaElementKind.EntityContainer)
			.SelectMany(x => (x as IEdmEntityContainer).Elements
				.Where(y => y.ContainerElementKind == EdmContainerElementKind.ActionImport))
				.BestMatch(x => x.Name, actionName, NameMatchResolver) is IEdmActionImport actionImport)
		{
			action = actionImport.Action;
		}

		if (action == null)
		{
			action = _model.SchemaElements
				.BestMatch(x => x.SchemaElementKind == EdmSchemaElementKind.Action,
					x => x.Name, actionName, NameMatchResolver) as IEdmAction;
		}

		if (action == null)
		{
			throw new UnresolvableObjectException(actionName, $"Action [{actionName}] not found");
		}

		return action;
	}
}
