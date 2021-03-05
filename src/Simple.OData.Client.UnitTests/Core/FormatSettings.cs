using System;

namespace Simple.OData.Client.Tests.Core
{
    public interface IFormatSettings
    {
        int ODataVersion { get; }
        string LongNumberSuffix { get; }
        string DoubleNumberSuffix { get; }
        string DecimalNumberSuffix { get; }
        string TimeSpanPrefix { get; }
        string GetDateTimeOffsetFormat(string text, bool escapeString = false);
        string GetGuidFormat(string text, bool escapeString = false);
        string GetEnumFormat(object value, Type enumType, string ns, bool prefixFree = false, bool escapeString = false);
        string GetContainsFormat(string item, string text, bool escapeString = false);
        string GetContainedInFormat(string item, string text, bool escapeString = false);
    }

    class ODataV3Format : IFormatSettings
    {
        public int ODataVersion { get { return 3; } }
        public string LongNumberSuffix { get { return "L"; } }
        public string DoubleNumberSuffix { get { return "D"; } }
        public string DecimalNumberSuffix { get { return "M"; } }
        public string TimeSpanPrefix { get { return "time"; } }

        public string GetDateTimeOffsetFormat(string text, bool escapeString = false)
        {
            var result = $"datetimeoffset'{text}'";
            if (escapeString)
                result = Uri.EscapeDataString(result);
            return result;
        }

        public string GetGuidFormat(string text, bool escapeString = false)
        {
            var result = $"guid'{text}'";
            if (escapeString)
                result = Uri.EscapeDataString(result);
            return result;
        }

        public string GetEnumFormat(object value, Type enumType, string ns, bool prefixFree = false, bool escapeString = false)
        {
            return Convert.ToInt32(value).ToString();
        }

        public string GetContainsFormat(string item, string text, bool escapeString = false)
        {
            var result = $"substringof('{text}',{item})";
            if (escapeString)
                result = Uri.EscapeDataString(result);
            return result;
        }
        
        public string GetContainedInFormat(string item, string text, bool escapeString = false)
        {
            var result = $"substringof({item},'{text}')";
            if (escapeString)
                result = Uri.EscapeDataString(result);
            return result;
        }
    }

    class ODataV4Format : IFormatSettings
    {
        public ODataV4Format(bool escapeUri = false)
        {
        }

        public int ODataVersion { get { return 4; } }
        public string LongNumberSuffix { get { return string.Empty; } }
        public string DoubleNumberSuffix { get { return string.Empty; } }
        public string DecimalNumberSuffix { get { return string.Empty; } }
        public string TimeSpanPrefix { get { return "duration"; } }

        public string GetDateTimeOffsetFormat(string text, bool escapeString = false)
        {
            return escapeString ? Uri.EscapeDataString(text) : text;
        }

        public string GetGuidFormat(string text, bool escapeString = false)
        {
            return escapeString ? Uri.EscapeDataString(text) : text;
        }

        public string GetEnumFormat(object value, Type enumType, string ns, bool prefixFree = false, bool escapeString = false)
        {
            var result = prefixFree
                ? $"'{Enum.ToObject(enumType, value)}'"
                : $"{ns}.{enumType.Name}'{Enum.ToObject(enumType, value)}'";
            if (escapeString)
                result = Uri.EscapeDataString(result);
            return result;
        }

        public string GetContainsFormat(string item, string text, bool escapeString = false)
        {
            var result = $"contains({item},'{text}')";
            if (escapeString)
                result = Uri.EscapeDataString(result);
            return result;
        }

        public string GetContainedInFormat(string item, string text, bool escapeString = false)
        {
            var result = $"contains('{text}',{item})";
            if (escapeString)
                result = Uri.EscapeDataString(result);
            return result;
        }
    }
}