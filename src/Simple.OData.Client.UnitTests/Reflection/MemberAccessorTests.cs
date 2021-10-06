using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Simple.OData.Client.Tests.Reflection
{
    public class MemberAccessorTests
    {
        class TestClass
        {
            public string instanceField;
            public static string staticField = "staticFieldValue";
            public static string staticFieldToSet = "staticFieldToSetValue";

            public string InstanceProprety { get; set; }
            public static string StaticProperty { get; } = "staticPropertyValue";
            public static string StaticPropertyToSet { get; set; } = "staticPropertyToSetValue";
        }

        [Fact]
        public void ShouldGetInstancePropertyValue()
        {
            var instance = new TestClass
            {
                InstanceProprety = "instancePropertyValue"
            };

            Assert.Equal(instance.InstanceProprety, MemberAccessor.GetValue<string>(instance, nameof(TestClass.InstanceProprety)));
        }

        [Fact]
        public void ShouldGetInstanceFieldValue()
        {
            var instance = new TestClass
            {
                instanceField = "instanceFieldValue"
            };

            Assert.Equal(instance.instanceField, MemberAccessor.GetValue<string>(instance, nameof(TestClass.instanceField)));
        }

        [Fact]
        public void ShouldGetStaticPropertyValue()
        {
            Assert.Equal(TestClass.StaticProperty, MemberAccessor.GetValue<string>(null, typeof(TestClass).GetProperty(nameof(TestClass.StaticProperty))));
        }

        [Fact]
        public void ShouldGetStaticFieldValue()
        {
            Assert.Equal(TestClass.staticField, MemberAccessor.GetValue<string>(null, typeof(TestClass).GetField(nameof(TestClass.staticField))));
        }






        [Fact]
        public void ShouldSetInstancePropertyValue()
        {
            var instance = new TestClass
            {
                InstanceProprety = "instancePropertyValue"
            };

            MemberAccessor.SetValue(instance, nameof(TestClass.InstanceProprety), "test");

            Assert.Equal("test", instance.InstanceProprety);
        }

        [Fact]
        public void ShouldSetInstanceFieldValue()
        {
            var instance = new TestClass
            {
                instanceField = "instanceFieldValue"
            };

            MemberAccessor.SetValue(instance, nameof(TestClass.instanceField), "test");

            Assert.Equal("test", instance.instanceField);
        }

        [Fact]
        public void ShouldSetStaticPropertyValue()
        {
            MemberAccessor.SetValue(null, typeof(TestClass).GetProperty(nameof(TestClass.StaticPropertyToSet)), "test");

            Assert.Equal("test", TestClass.StaticPropertyToSet);
        }

        [Fact]
        public void ShouldSetStaticFieldValue()
        {
            MemberAccessor.SetValue(null, typeof(TestClass).GetField(nameof(TestClass.staticFieldToSet)), "test");

            Assert.Equal("test", TestClass.staticFieldToSet);
        }
    }
}

