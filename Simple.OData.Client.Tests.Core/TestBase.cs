using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Microsoft.Data.OData;
using Moq;

#pragma warning disable 3008

namespace Simple.OData.Client.Tests
{
    public abstract class TestBase : IDisposable
    {
        protected readonly IODataClient _client;
        internal ISession _session;

        protected TestBase()
        {
            _client = CreateClient(this.MetadataFile);
        }

        public IODataClient CreateClient(string metadataFile, INameMatchResolver matchResolver = null)
        {
            var baseUri = new Uri("http://localhost/" + metadataFile);
            var metadataString = GetResourceAsString(@"Resources." + metadataFile);
            var settings = new ODataClientSettings
            {
                BaseUri = baseUri,
                MetadataDocument = metadataString,
                NameMatchResolver = matchResolver,
            };
            _session = Session.FromMetadata(baseUri, metadataString);
            return new ODataClient(settings);
        }

        public abstract string MetadataFile { get; }
        public abstract IFormatSettings FormatSettings { get; }

        public void Dispose()
        {
        }

        public static string GetResourceAsString(string resourceName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceNames = assembly.GetManifestResourceNames();
            string completeResourceName = resourceNames.FirstOrDefault(o => o.EndsWith("." + resourceName, StringComparison.CurrentCultureIgnoreCase));
            using (Stream resourceStream = assembly.GetManifestResourceStream(completeResourceName))
            {
                TextReader reader = new StreamReader(resourceStream);
                return reader.ReadToEnd();
            }
        }

        public IODataResponseMessageAsync SetUpResourceMock(string resourceName)
        {
            var document = GetResourceAsString(resourceName);
            var mock = new Mock<IODataResponseMessageAsync>();
            mock.Setup(x => x.GetStreamAsync()).ReturnsAsync(new MemoryStream(Encoding.UTF8.GetBytes(document)));
            mock.Setup(x => x.GetStream()).Returns(new MemoryStream(Encoding.UTF8.GetBytes(document)));
            mock.Setup(x => x.GetHeader("Content-Type")).Returns(() => "application/atom+xml; type=feed; charset=utf-8");
            return mock.Object;
        }
    }
}
