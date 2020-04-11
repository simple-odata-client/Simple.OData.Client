using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
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

        public static bool ContainsMatch(IEnumerable<string> actualNames, string requestedName, INameMatchResolver matchResolver)
        {
            return actualNames.Any(x => matchResolver.IsMatch(x, requestedName));
        }

        public static bool AllMatch(IEnumerable<string> subset, IEnumerable<string> superset, INameMatchResolver matchResolver)
        {
            return subset.All(x => superset.Any(y => matchResolver.IsMatch(x, y)));
        }

        public static T BestMatch<T>(this IEnumerable<T> collection, 
            Func<T, string> fieldFunc, string value, INameMatchResolver matchResolver)
            where T : class
        {
            if (ReferenceEquals(matchResolver, ODataNameMatchResolver.Strict))
                return collection.FirstOrDefault(x => matchResolver.IsMatch(fieldFunc(x), value));

            return collection
                .Where(x => matchResolver.IsMatch(fieldFunc(x), value))
                .Select(x => new { Match = x, IsStrictMatch = ODataNameMatchResolver.Strict.IsMatch(fieldFunc(x), value) })
                .OrderBy(x => x.IsStrictMatch ? 0 : 1)
                .Select(x => x.Match).FirstOrDefault();
        }

        public static T BestMatch<T>(this IEnumerable<T> collection, 
            Func<T, bool> condition, Func<T, string> fieldFunc, string value,
            INameMatchResolver matchResolver)
            where T : class
        {
            if (ReferenceEquals(matchResolver, ODataNameMatchResolver.Strict))
                return collection.FirstOrDefault(x => condition(x) && matchResolver.IsMatch(fieldFunc(x), value));

            return collection
                .Where(x => condition(x) && matchResolver.IsMatch(fieldFunc(x), value))
                .Select(x => new { Match = x, IsStrictMatch = ODataNameMatchResolver.Strict.IsMatch(fieldFunc(x), value) })
                .OrderBy(x => x.IsStrictMatch ? 0 : 1)
                .Select(x => x.Match).FirstOrDefault();
        }

        public static Exception NotSupportedExpression(Expression expression)
        {
            return new NotSupportedException($"Not supported expression of type {expression.GetType()} ({expression.NodeType}): {expression}");
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
                    ? $"?{baseQuery}"
                    : $"{uri.Query}&{baseQuery}";

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

        public static Task<T> GetTaskFromResult<T>(T result)
        {
            return Task.FromResult(result);
        }
    }
}
