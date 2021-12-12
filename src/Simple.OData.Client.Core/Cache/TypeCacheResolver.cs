using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Simple.OData.Client.Extensions;

namespace Simple.OData.Client;

/// <summary>
/// Holds information about a type.
/// </summary>
public class TypeCacheResolver
{
	private readonly INameMatchResolver _nameMatchResolver;

	/// <summary>
	/// Creates a new instance of the <see cref="TypeCacheResolver"/> class.
	/// </summary>
	/// <param name="type">Type of the cached properties.</param>
	/// <param name="nameMatchResolver">Name match resolver.</param>
	/// <param name="dynamicType">Whether the cached type is dynamic.</param>
	/// <param name="dynamicContainerName">Dynamic container name.</param>
	public TypeCacheResolver(Type type, INameMatchResolver nameMatchResolver, bool dynamicType = false, string dynamicContainerName = "DynamicProperties")
	{
		_nameMatchResolver = nameMatchResolver;

		Type = type;
		DerivedTypes = type.DerivedTypes().ToList();
		IsDynamicType = dynamicType;
		DynamicPropertiesName = dynamicContainerName;
		TypeInfo = type.GetTypeInfo();
		AllProperties = type.GetAllProperties().ToList();
		DeclaredProperties = type.GetDeclaredProperties().ToList();
		AllFields = type.GetAllFields().ToList();
		DeclaredFields = type.GetDeclaredFields().ToList();
		MappedName = type.GetMappedName();
		MappedProperties = type.GetMappedProperties().ToList();
		MappedPropertiesWithNames = type.GetMappedPropertiesWithNames().ToList();

		IsAnonymousType = type.IsAnonymousType();
		AnnotationsProperty = AllProperties.FirstOrDefault(x => x.PropertyType == typeof(ODataEntryAnnotations));
	}

	/// <summary>
	/// Gets the type we are responsible for.
	/// </summary>
	public Type Type { get; }

	/// <summary>
	/// Get all derived types in the same assembly.
	/// </summary>
	public IList<Type> DerivedTypes { get; }

	public TypeInfo TypeInfo { get; }

	/// <summary>
	/// Gets the mapped name i.e. the OData type we correspond to.
	/// </summary>
	public string MappedName { get; }

	/// <summary>
	/// Gets whether we are a dynamic type
	/// </summary>
	public bool IsDynamicType { get; }

	/// <summary>
	/// Gets whether we are an anonymous type
	/// </summary>
	public bool IsAnonymousType { get; }

	/// <summary>
	/// Gets whether we are a generic type
	/// </summary>
	public bool IsGeneric => TypeInfo.IsGenericType;

	/// <summary>
	/// Gets whether we are a value type
	/// </summary>
	public bool IsValue => TypeInfo.IsValueType;

	/// <summary>
	/// Gets whether we are an enum
	/// </summary>
	public bool IsEnumType => TypeInfo.IsEnum;

	/// <summary>
	/// Gets the base type
	/// </summary>
	public Type BaseType => TypeInfo.BaseType;

	/// <summary>
	/// Gets the dynamic properties container name
	/// </summary>
	// TODO: Store this as a PropertyInfo?
	public string DynamicPropertiesName { get; set; }

	/// <summary>
	/// Gets all properties
	/// </summary>
	public IList<PropertyInfo> AllProperties { get; }

	/// <summary>
	/// Gets declared properties
	/// </summary>
	public IList<PropertyInfo> DeclaredProperties { get; }

	/// <summary>
	/// Gets all fields
	/// </summary>
	public IList<FieldInfo> AllFields { get; }

	/// <summary>
	/// Gets declared fields.
	/// </summary>
	public IList<FieldInfo> DeclaredFields { get; }

	/// <summary>
	/// Gets mapped properties
	/// </summary>
	public IList<PropertyInfo> MappedProperties { get; }

	/// <summary>
	/// Gets mapped properties with the mapped name
	/// </summary>
	public IList<Tuple<PropertyInfo, string>> MappedPropertiesWithNames { get; }

	public PropertyInfo AnnotationsProperty { get; }

	/// <summary>
	/// Gets a mapped property
	/// </summary>
	/// <param name="propertyName"></param>
	/// <returns></returns>
	public PropertyInfo GetMappedProperty(string propertyName)
	{
		return (from t in MappedPropertiesWithNames where _nameMatchResolver.IsMatch(t.Item2, propertyName) select t.Item1).FirstOrDefault();
	}

	public string GetMappedName(PropertyInfo propertyInfo)
	{
		return (from t in MappedPropertiesWithNames where t.Item1 == propertyInfo select t.Item2).FirstOrDefault();
	}

	public string GetMappedName(string propertyName)
	{
		return (from t in MappedPropertiesWithNames where _nameMatchResolver.IsMatch(t.Item1.Name, propertyName) select t.Item2).FirstOrDefault();
	}

	public PropertyInfo GetAnyProperty(string propertyName)
	{
		var currentType = Type;
		while (currentType != null && currentType != typeof(object))
		{
			var property = currentType.GetTypeInfo().GetDeclaredProperty(propertyName);
			if (property != null)
			{
				return property;
			}

			currentType = currentType.GetTypeInfo().BaseType;
		}

		return null;
	}

	public PropertyInfo GetDeclaredProperty(string propertyName)
	{
		return TypeInfo.GetDeclaredProperty(propertyName);
	}

	[Obsolete("The includeNonPublic field is ignored.  Use the method GetAnyField(string fieldName) instead.")]
	public FieldInfo GetAnyField(string fieldName, bool includeNonPublic)
	{
		return GetAnyField(fieldName);
	}

	public FieldInfo GetAnyField(string fieldName)
	{
		var currentType = Type;
		while (currentType != null && currentType != typeof(object))
		{
			var field = currentType.GetDeclaredField(fieldName);
			if (field != null)
			{
				return field;
			}

			currentType = currentType.GetTypeInfo().BaseType;
		}

		return null;
	}

	public FieldInfo GetDeclaredField(string fieldName)
	{
		return TypeInfo.GetDeclaredField(fieldName);
	}

	public MethodInfo GetDeclaredMethod(string methodName)
	{
		return TypeInfo.GetDeclaredMethod(methodName);
	}

	public IEnumerable<ConstructorInfo> GetDeclaredConstructors()
	{
		return TypeInfo.DeclaredConstructors.Where(x => !x.IsStatic);
	}

	public ConstructorInfo GetDefaultConstructor()
	{
		return GetDeclaredConstructors().SingleOrDefault(x => x.GetParameters().Length == 0);
	}

	public TypeAttributes GetTypeAttributes()
	{
		return TypeInfo.Attributes;
	}

	public Type[] GetGenericTypeArguments()
	{
		return TypeInfo.GenericTypeArguments;
	}

	public bool IsTypeAssignableFrom(Type otherType)
	{
		return TypeInfo.IsAssignableFrom(otherType.GetTypeInfo());
	}

	public bool HasCustomAttribute(Type attributeType, bool inherit)
	{
		return TypeInfo.GetCustomAttribute(attributeType, inherit) != null;
	}
}
