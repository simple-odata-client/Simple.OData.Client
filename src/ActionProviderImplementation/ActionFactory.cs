using System;
using System.Collections.Generic;
using System.Data.Services.Providers;
using System.Linq;
using System.Reflection;

namespace ActionProviderImplementation;

public class ActionFactory
{
	private readonly IDataServiceMetadataProvider _metadata;

	//TODO: make this list complete
	private static readonly Type[] __primitives = new[] {
			typeof(bool),
			typeof(short),
			typeof(int),
			typeof(long),
			typeof(string),
			typeof(decimal),
			typeof(Guid),
			typeof(bool?),
			typeof(short?),
			typeof(int?),
			typeof(long?),
			typeof(decimal?),
			typeof(Guid?)
		};

	public ActionFactory(IDataServiceMetadataProvider metadata)
	{
		_metadata = metadata;
	}

	public IEnumerable<ServiceAction> GetActions(Type typeWithActions)
	{
		var actionInfos = ActionFinder.GetActionsFromType(typeWithActions).ToArray();

		foreach (var actionInfo in actionInfos)
		{
			var method = actionInfo.ActionMethod;

			var actionName = method.Name;
			var returnType = method.ReturnType == typeof(void) ? null : GetResourceType(method.ReturnType);
			var resourceSet = GetResourceSet(returnType);

			var parameters = GetParameters(method, actionInfo.Binding != OperationParameterBindingKind.Never);
			var action = new ServiceAction(
				actionName,
				returnType,
				resourceSet,
				actionInfo.Binding,
				parameters
			)
			{

				// Store the method associated with this Action.
				CustomState = actionInfo
			};
			action.SetReadOnly();
			yield return action;
		}
	}
	// Only allow EntityType or IQueryable<EntityType> for the binding parameter
	private ResourceType GetBindingParameterResourceType(Type type)
	{
		var resourceType = GetResourceType(type);
		if (resourceType.ResourceTypeKind == ResourceTypeKind.EntityType)
		{
			return resourceType;
		}
		else if (resourceType.ResourceTypeKind == ResourceTypeKind.EntityCollection)
		{
			if (type.GetGenericTypeDefinition() == typeof(IQueryable<>))
			{
				return resourceType;
			}
		}

		throw new Exception($"Type {type.FullName} is not a valid binding parameter");
	}
	// Only allow Primitive/Complex or IEnumerable<Primitive>/IEnumerable<Complex>
	private ResourceType GetParameterResourceType(Type type)
	{
		var resourceType = GetResourceType(type);
		if (resourceType.ResourceTypeKind == ResourceTypeKind.EntityType)
		{
			throw new Exception($"Entity Types ({type.FullName}) MUST not be used as non-binding parameters.");
		}
		else if (resourceType.ResourceTypeKind == ResourceTypeKind.EntityCollection)
		{
			throw new Exception($"Entity Type Collections ({type.FullName}) MUST not be used as non-binding parameters.");
		}
		else if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IQueryable<>))
		{
			throw new Exception("IQueryable<> is not supported for non-binding parameters.");
		}

		return resourceType;
	}
	// Allow all types: Primitive/Complex/Entity and IQueryable<> and IEnumerable<>
	private ResourceType GetResourceType(Type type)
	{
		if (type.IsGenericType)
		{
			var typeDef = type.GetGenericTypeDefinition();
			if (typeDef.GetGenericArguments().Count() == 1)
			{
				if (typeDef == typeof(IEnumerable<>) || typeDef == typeof(IQueryable<>))
				{
					var elementResource = GetResourceType(type.GetGenericArguments().Single());
					if ((elementResource.ResourceTypeKind | ResourceTypeKind.EntityType) == ResourceTypeKind.EntityType)
					{
						return ResourceType.GetEntityCollectionResourceType(elementResource);
					}
					else
					{
						return ResourceType.GetCollectionResourceType(elementResource);
					}
				}
			}

			throw new Exception($"Generic action parameter type {type} not supported");
		}

		if (ActionFactory.__primitives.Contains(type))
		{
			return ResourceType.GetPrimitiveResourceType(type);
		}

		var resourceType = _metadata.Types.SingleOrDefault(s => s.Name == type.Name);
		if (resourceType == null)
		{
			throw new Exception($"Generic action parameter type {type} not supported");
		}

		return resourceType;
	}

	/// <summary>
	/// Given a type try to find the resource set.
	/// </summary>
	/// <param name="type"></param>
	/// <returns></returns>
	private ResourceSet? GetResourceSet(ResourceType? type)
	{
		if (type == null)
		{
			return null;
		}
		else if (type.ResourceTypeKind == ResourceTypeKind.EntityCollection)
		{
			var ecType = type as EntityCollectionResourceType;
			return GetResourceSet(ecType.ItemType);
		}
		else if (type.ResourceTypeKind == ResourceTypeKind.EntityType)
		{
			var set = _metadata.ResourceSets.SingleOrDefault(rs => rs.ResourceType == type);
			if (set != null)
			{
				return set;
			}
			else if (type.BaseType != null)
			{
				return GetResourceSet(type.BaseType);
			}
		}

		return null;
	}
	private IEnumerable<ServiceActionParameter> GetParameters(MethodInfo method, bool isBindable)
	{
		IEnumerable<ParameterInfo> parameters = method.GetParameters();
		if (isBindable)
		{
			var bindingParameter = parameters.First();
			yield return new ServiceActionParameter(
					bindingParameter.Name,
					GetBindingParameterResourceType(bindingParameter.ParameterType)
			);
			parameters = parameters.Skip(1);
		}

		foreach (var parameter in parameters)
		{
			yield return new ServiceActionParameter(
				parameter.Name,
				GetParameterResourceType(parameter.ParameterType)
			);
		}
	}
}
