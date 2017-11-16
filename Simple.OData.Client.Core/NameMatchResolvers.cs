using System;
using System.Linq;
using Simple.OData.Client.Extensions;

namespace Simple.OData.Client
{
    public static class ODataNameMatchResolver
    {
        public static INameMatchResolver Strict = new ExactMatchResolver();
        public static INameMatchResolver Alpahumeric = new ExactMatchResolver(true);
        public static INameMatchResolver AlpahumericCaseInsensitive = new ExactMatchResolver(true, StringComparison.InvariantCultureIgnoreCase);
        public static INameMatchResolver NotStrict = new BestMatchResolver();
    }

    public class ExactMatchResolver : INameMatchResolver
    {
        private readonly StringComparison _stringComparison;
        private readonly bool _alphanumComparison;

        public ExactMatchResolver(bool alphanumComparison = false, StringComparison stringComparison = StringComparison.InvariantCulture)
        {
            _alphanumComparison = alphanumComparison;
            _stringComparison = stringComparison;
        }

        public bool IsMatch(string actualName, string requestedName)
        {
            actualName = actualName.Split('.').Last();
            requestedName = requestedName.Split('.').Last();
            if (_alphanumComparison)
            {
                actualName = actualName.Homogenize();
                requestedName = requestedName.Homogenize();
            }

            return actualName.Equals(requestedName, _stringComparison);
        }
    }

    public class BestMatchResolver : INameMatchResolver
    {
        private readonly IPluralizer _pluralizer;

        public BestMatchResolver()
        {
            _pluralizer = new SimplePluralizer();
        }

        public bool IsMatch(string actualName, string requestedName)
        {
            actualName = actualName.Split('.').Last().Homogenize();
            requestedName = requestedName.Split('.').Last().Homogenize();

            return actualName == requestedName || 
                   (actualName == _pluralizer.Singularize(requestedName) ||
                    actualName == _pluralizer.Pluralize(requestedName) ||
                    _pluralizer.Singularize(actualName) == requestedName ||
                    _pluralizer.Pluralize(actualName) == requestedName);
        }
    }
}