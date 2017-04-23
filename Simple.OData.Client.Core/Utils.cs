using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Simple.OData.Client.Extensions;

namespace Simple.OData.Client
{
    static class Utils
    {
        public static string StreamToString(Stream stream, bool disposeStream = false)
        {
            if (!disposeStream && stream.CanSeek)
                stream.Seek(0, SeekOrigin.Begin);
            var result = new StreamReader(stream).ReadToEnd();
            if (disposeStream)
                stream.Dispose();
            return result;
        }

        public static byte[] StreamToByteArray(Stream stream, bool disposeStream = false)
        {
            if (!disposeStream && stream.CanSeek)
                stream.Seek(0, SeekOrigin.Begin);
            var bytes = new byte[stream.Length];
            var result = new BinaryReader(stream).ReadBytes(bytes.Length);
            if (disposeStream)
                stream.Dispose();
            return result;
        }

        public static Stream StringToStream(string text)
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(text));
        }

        public static Stream ByteArrayToStream(byte[] bytes)
        {
            return new MemoryStream(bytes);
        }

        public static Stream CloneStream(Stream stream)
        {
            stream.Position = 0;
            var clonedStream = new MemoryStream();
            stream.CopyTo(clonedStream);
            return clonedStream;
        }

        public static bool NamesMatch(string actualName, string requestedName, IPluralizer pluralizer)
        {
            actualName = actualName.Split('.').Last().Homogenize();
            requestedName = requestedName.Split('.').Last().Homogenize();

            return actualName == requestedName || pluralizer != null && 
                (actualName == pluralizer.Singularize(requestedName) || 
                actualName == pluralizer.Pluralize(requestedName) ||
                pluralizer.Singularize(actualName) == requestedName ||
                pluralizer.Pluralize(actualName) == requestedName);
        }

        public static bool ContainsMatch(IEnumerable<string> actualNames, string requestedName, IPluralizer pluralizer)
        {
            return actualNames.Any(x => NamesMatch(x, requestedName, pluralizer));
        }

        public static bool AllMatch(IEnumerable<string> subset, IEnumerable<string> superset, IPluralizer pluralizer)
        {
            return subset.All(x => superset.Any(y => NamesMatch(x, y, pluralizer)));
        }

        public static T BestMatch<T>(this IEnumerable<T> collection, 
            Func<T, string> fieldFunc, string value, IPluralizer pluralizer)
            where T : class
        {
            return collection.FirstOrDefault(x => fieldFunc(x).Homogenize() == value.Homogenize())
                ?? collection.FirstOrDefault(x => NamesMatch(fieldFunc(x), value, pluralizer));
        }

        public static T BestMatch<T>(this IEnumerable<T> collection, 
            Func<T, bool> condition, Func<T, string> fieldFunc, string value, 
            IPluralizer pluralizer)
            where T : class
        {
            return collection.FirstOrDefault(x => fieldFunc(x).Homogenize() == value.Homogenize() && condition(x))
                ?? collection.FirstOrDefault(x => NamesMatch(fieldFunc(x), value, pluralizer) && condition(x));
        }

        public static Exception NotSupportedExpression(Expression expression)
        {
            return new NotSupportedException(String.Format("Not supported expression of type {0} ({1}): {2}",
                expression.GetType(), expression.NodeType, expression));
        }

        public static IEnumerable<PropertyInfo> GetMappedProperties(Type type)
        {
            return type.GetAllProperties().Where(x => !x.IsNotMapped());
        }

        public static PropertyInfo GetMappedProperty(Type type, string propertyName)
        {
            var property = type.GetAnyProperty(propertyName);
            return property == null || property.IsNotMapped() ? null : property;
        }

        public static Uri CreateAbsoluteUri(string baseUri, string relativePath)
        {
            var basePath = string.IsNullOrEmpty(baseUri) ? "http://" : baseUri;
            var uri = new Uri(basePath);
            var baseQuery = uri.Query;
            if (!string.IsNullOrEmpty(baseQuery))
            {
                basePath = basePath.Substring(0, basePath.Length - baseQuery.Length);
                baseQuery = baseQuery.Substring(1);
            }
            if (!basePath.EndsWith("/"))
                basePath += "/";

            uri = new Uri(basePath + relativePath);
            if (string.IsNullOrEmpty(baseQuery))
            {
                return uri;
            }
            else
            {
                var uriHost = uri.AbsoluteUri.Substring(
                    0, uri.AbsoluteUri.Length - uri.AbsolutePath.Length - uri.Query.Length);
                var query = string.IsNullOrEmpty(uri.Query)
                    ? string.Format("?{0}", baseQuery)
                    : string.Format("{0}&{1}", uri.Query, baseQuery);

                return new Uri(uriHost + uri.AbsolutePath + query);
            }
        }

        public static string ExtractCollectionName(string commandText)
        {
            var uri = new Uri(commandText, UriKind.RelativeOrAbsolute);
            if (uri.IsAbsoluteUri)
            {
                return uri.LocalPath.Split('/').Last();
            }
            else
            {
                return commandText.Split('?', '(', '/').First();
            }
        }

        public static bool TryConvert(object value, Type targetType, out object result)
        {
            try
            {
                if (value == null)
                {
                    if (targetType.IsValue())
                        result = Activator.CreateInstance(targetType);
                    else
                        result = null;
                }
                else if (targetType.IsTypeAssignableFrom(value.GetType()))
                {
                    result = value;
                }
                else if (targetType == typeof(string))
                {
                    result = value.ToString();
                }
                else if (targetType.IsEnumType() && value is string)
                {
                    result = Enum.Parse(targetType, value.ToString(), true);
                }
                else if (targetType == typeof(byte[]) && value is string)
                {
                    result = System.Convert.FromBase64String(value.ToString());
                }
                else if (targetType == typeof(string) && value is byte[])
                {
                    result = System.Convert.ToBase64String((byte[])value);
                }
                else if ((targetType == typeof(DateTime) || targetType == typeof(DateTime?)) && value is DateTimeOffset)
                {
                    result = ((DateTimeOffset)value).DateTime;
                }
                else if ((targetType == typeof(DateTimeOffset) || targetType == typeof(DateTimeOffset)) && value is DateTime)
                {
                    result = new DateTimeOffset((DateTime)value);
                }
                else if (targetType.IsEnumType())
                {
                    result = Enum.ToObject(targetType, value);
                }
                else if (targetType == typeof(Guid) && value is string)
                {
                    result = new Guid(value.ToString());
                }
                else if (Nullable.GetUnderlyingType(targetType) != null)
                {
                    result = Convert(value, Nullable.GetUnderlyingType(targetType));
                }
                else
                {
                    result = System.Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
                }
                return true;
            }
            catch (Exception)
            {
                result = null;
                return false;
            }
        }

        public static object Convert(object value, Type targetType)
        {
            object result;

            if (value == null && !targetType.IsValue())
                return null;
            else if (TryConvert(value, targetType, out result))
                return result;

            throw new FormatException(string.Format("Unable to convert value from type {0} to type {1}", value.GetType(), targetType));
        }

        public static bool IsSystemType(Type type)
        {
            return
                type.FullName.StartsWith("System.") ||
                type.FullName.StartsWith("Microsoft.");
        }

        public static bool IsDesktopPlatform()
        {
            var cmdm = Type.GetType("System.ComponentModel.DesignerProperties, PresentationFramework, Version=3.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35");
            return cmdm != null;
        }

#if NET40 || SILVERLIGHT || PORTABLE_LEGACY
        public static Task<T> GetTaskFromResult<T>(T result)
        {
            return TaskEx.FromResult(result);
        }
#else
        public static Task<T> GetTaskFromResult<T>(T result)
        {
            return Task.FromResult(result);
        }
#endif
    }
}
