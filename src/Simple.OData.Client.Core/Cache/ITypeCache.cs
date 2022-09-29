using System;
using System.Collections.Generic;
using System.Reflection;

namespace Simple.OData.Client;

/// <summary>
/// Access the type definition with good performance.
/// </summary>
public interface ITypeCache
{
	/// <summary>
	/// Gets the type converters.
	/// </summary>
	ITypeConverter Converter { get; }

	/// <summary>
	/// Register the type, along with its dynamic properties name, also applies to sub-types in the same assembly
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="dynamicContainerName"></param>
	void Register<T>(string dynamicContainerName = "DynamicProperties");

	/// <summary>
	/// Register the type, along with its dynamic properties name, also applies to sub-types in the same assembly
	/// </summary>
	/// <param name="type"></param>
	/// <param name="dynamicContainerName"></param>
	void Register(Type type, string dynamicContainerName = "DynamicProperties");

	/// <summary>
	/// Get whether the type is a dynamic
	/// </summary>
	/// <param name="type"></param>
	/// <returns></returns>
	[Obsolete("Should check DynamicContainerName")]
	bool IsDynamicType(Type type);

	/// <summary>
	/// Gets the dynamic properties container name for a type, will be <see cref="string.Empty"/> unless registered
	/// </summary>
	/// <param name="type"></param>
	/// <returns></returns>
	string DynamicContainerName(Type type);

	/// <summary>
	/// Get the OData annotations property if any.
	/// </summary>
	PropertyInfo GetAnnotationsProperty(Type type);

	/// <summary>
	/// Get all derived types in the same assembly.
	/// </summary>
	/// <param name="type"></param>
	/// <returns></returns>
	IEnumerable<Type> GetDerivedTypes(Type type);

	/// <summary>
	/// Get the mapped properties for a type
	/// </summary>
	/// <param name="type"></param>
	/// <returns></returns>
	IEnumerable<PropertyInfo> GetMappedProperties(Type type);

	/// <summary>
	/// Get the mapped properties for a type along with the associated property name
	/// </summary>
	/// <param name="type"></param>
	/// <returns></returns>
	IEnumerable<Tuple<PropertyInfo, string>> GetMappedPropertiesWithNames(Type type);

	/// <summary>
	/// Get a property that is either directly named or mapped via an attribute
	/// </summary>
	/// <param name="type"></param>
	/// <param name="propertyName"></param>
	/// <returns></returns>
	PropertyInfo GetMappedProperty(Type type, string propertyName);

	/// <summary>
	/// Get the mapping name for a property
	/// </summary>
	/// <param name="type"></param>
	/// <param name="propertyInfo"></param>
	/// <returns></returns>
	string GetMappedName(Type type, PropertyInfo propertyInfo);

	/// <summary>
	/// Get the mapping name for a named property
	/// </summary>
	/// <param name="type"></param>
	/// <param name="propertyName"></param>
	/// <returns></returns>
	string GetMappedName(Type type, string propertyName);

	/// <summary>
	/// Get all properties for a type
	/// </summary>
	/// <param name="type"></param>
	/// <returns></returns>
	IEnumerable<PropertyInfo> GetAllProperties(Type type);

	/// <summary>
	/// Get a directly named property
	/// </summary>
	/// <param name="type"></param>
	/// <param name="propertyName"></param>
	/// <returns></returns>
	PropertyInfo GetNamedProperty(Type type, string propertyName);

	/// <summary>
	/// Get declared properties for a type.
	/// </summary>
	/// <param name="type"></param>
	/// <returns></returns>
	IEnumerable<PropertyInfo> GetDeclaredProperties(Type type);

	/// <summary>
	/// Get a declared property
	/// </summary>
	/// <param name="type"></param>
	/// <param name="propertyName"></param>
	/// <returns></returns>
	PropertyInfo GetDeclaredProperty(Type type, string propertyName);

	/// <summary>
	/// Get all fields for a type
	/// </summary>
	/// <param name="type"></param>
	/// <returns></returns>
	IEnumerable<FieldInfo> GetAllFields(Type type);

	/// <summary>
	/// Get a field for a type
	/// </summary>
	/// <param name="type"></param>
	/// <param name="fieldName"></param>
	/// <param name="includeNonPublic"></param>
	/// <returns></returns>
	FieldInfo GetAnyField(Type type, string fieldName, bool includeNonPublic = false);

	/// <summary>
	/// Get declared fields for a type
	/// </summary>
	/// <param name="type"></param>
	/// <returns></returns>
	IEnumerable<FieldInfo> GetDeclaredFields(Type type);

	/// <summary>
	/// Get a declared field for a type
	/// </summary>
	/// <param name="type"></param>
	/// <param name="fieldName"></param>
	/// <returns></returns>
	FieldInfo GetDeclaredField(Type type, string fieldName);

	/// <summary>
	/// Get a declared method for a type
	/// </summary>
	/// <param name="type"></param>
	/// <param name="methodName"></param>
	/// <returns></returns>
	MethodInfo GetDeclaredMethod(Type type, string methodName);

	/// <summary>
	/// Get the declared constructors for a type
	/// </summary>
	/// <param name="type"></param>
	/// <returns></returns>
	IEnumerable<ConstructorInfo> GetDeclaredConstructors(Type type);

	/// <summary>
	/// Get the default constructor for a type
	/// </summary>
	/// <param name="type"></param>
	/// <returns></returns>
	ConstructorInfo GetDefaultConstructor(Type type);

	/// <summary>
	/// Get the type's attributes
	/// </summary>
	/// <param name="type"></param>
	/// <returns></returns>
	TypeAttributes GetTypeAttributes(Type type);

	/// <summary>
	/// Get the generic type arguments for a type
	/// </summary>
	/// <param name="type"></param>
	/// <returns></returns>
	Type[] GetGenericTypeArguments(Type type);

	/// <summary>
	/// Get whether a type is anonymous
	/// </summary>
	/// <param name="type"></param>
	/// <returns></returns>
	bool IsAnonymousType(Type type);

	/// <summary>
	/// Get whether we can assign a value of this type to another
	/// </summary>
	/// <param name="type"></param>
	/// <param name="otherType"></param>
	/// <returns></returns>
	bool IsTypeAssignableFrom(Type type, Type otherType);

	/// <summary>
	/// Get whether a type has a specific custom attribute
	/// </summary>
	/// <param name="type"></param>
	/// <param name="attributeType"></param>
	/// <param name="inherit"></param>
	/// <returns></returns>
	bool HasCustomAttribute(Type type, Type attributeType, bool inherit);

	/// <summary>
	/// Get whether a type is generic
	/// </summary>
	/// <param name="type"></param>
	/// <returns></returns>
	bool IsGeneric(Type type);

	/// <summary>
	/// Gets whether a type is a value type
	/// </summary>
	/// <param name="type"></param>
	/// <returns></returns>
	bool IsValue(Type type);

	/// <summary>
	/// Gets whether a type is an enum
	/// </summary>
	/// <param name="type"></param>
	/// <returns></returns>
	bool IsEnumType(Type type);

	/// <summary>
	/// Get the base type of a type
	/// </summary>
	/// <param name="type"></param>
	/// <returns></returns>
	Type GetBaseType(Type type);

	/// <summary>
	/// Get the mapped name of a type.
	/// </summary>
	/// <param name="type"></param>
	/// <returns></returns>
	string GetMappedName(Type type);

	bool TryConvert(object value, Type targetType, out object result);

	object Convert(object value, Type targetType);
}
