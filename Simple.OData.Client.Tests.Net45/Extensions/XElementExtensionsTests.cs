using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Simple.OData.Client.Extensions;
using Xunit;

namespace Simple.OData.Client.Tests
{
    public class XElementExtensionsTests
    {
        [Fact]
        public void TestXElementWithDefaultNamespace()
        {
            var content = Properties.XmlSamples.XmlWithDefaultNamespace;
            var element = XElement.Parse(content);
            var list = element.Elements(null, "child").ToList();
            Assert.Equal(2, list.Count);
            Assert.Equal("Foo", list[0].Element(null, "sub").Value);
            Assert.Equal("Bar", list[1].Element(null, "sub").Value);
        }

        [Fact]
        public void TestXElementWithNoNamespace()
        {
            var content = Properties.XmlSamples.XmlWithNoNamespace;
            var element = XElement.Parse(content);
            var list = element.Elements(null, "child").ToList();
            Assert.Equal(2, list.Count);
            Assert.Equal("Foo", list[0].Element(null, "sub").Value);
            Assert.Equal("Bar", list[1].Element(null, "sub").Value);
        }

        [Fact]
        public void TestXElementWithPrefixedNamespace()
        {
            var content = Properties.XmlSamples.XmlWithPrefixedNamespace;
            var element = XElement.Parse(content);
            var list = element.Elements("c", "child").ToList();
            Assert.Equal(2, list.Count);
            Assert.Equal("Foo", list[0].Element("c", "sub").Value);
            Assert.Equal("Bar", list[1].Element("c", "sub").Value);
        }
    }
}
