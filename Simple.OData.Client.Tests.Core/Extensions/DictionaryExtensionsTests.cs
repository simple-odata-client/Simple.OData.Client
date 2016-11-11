using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Spatial;
using Xunit;
using Simple.OData.Client.Extensions;

namespace Simple.OData.Client.Tests
{
    public class DictionaryExtensionsTests
    {
        [Flags]
        enum EnumType
        {
            Zero,
            One,
            Two,
            Three,
        }

        class ClassType
        {
            public string StringField;
            public string[] StringCollectionField;

            public string StringProperty { get; set; }
            public string StringPropertyPrivateSetter { get; private set; }
            public int IntProperty { get; set; }
            public EnumType EnumProperty { get; set; }
            public string[] StringCollectionProperty { get; set; }
            public int[] IntCollectionProperty { get; set; }
            public SubclassType CompoundProperty { get; set; }
            public SubclassType[] CompoundCollectionProperty { get; set; }
            public GeographyPoint Point { get; set; }
        }

        class SubclassType
        {
            public string StringProperty { get; set; }
            public int IntProperty { get; set; }
        }

        [Fact]
        public void ToObjectPrimitiveProperties()
        {
            var dict = new Dictionary<string, object>()
            {
                { "StringProperty", "a" }, 
                { "IntProperty", 1 },
            };

            var value = dict.ToObject<ClassType>();
            Assert.Equal("a", value.StringProperty);
            Assert.Equal(1, value.IntProperty);
        }

        [Fact]
        public void ToObjectEnumPropertyFromInt()
        {
            var dict = new Dictionary<string, object>()
            {
                { "EnumProperty", EnumType.One },
            };

            var value = dict.ToObject<ClassType>();
            Assert.Equal(EnumType.One, value.EnumProperty);
        }

        [Fact]
        public void ToObjectEnumPropertyFromString()
        {
            var dict = new Dictionary<string, object>()
            {
                { "EnumProperty", "One" },
            };

            var value = dict.ToObject<ClassType>();
            Assert.Equal(EnumType.One, value.EnumProperty);
        }

        [Fact]
        public void ToObjectCombinedEnumPropertyFromInt()
        {
            var dict = new Dictionary<string, object>()
            {
                { "EnumProperty", EnumType.One | EnumType.Two },
            };

            var value = dict.ToObject<ClassType>();
            Assert.Equal(EnumType.Three, value.EnumProperty);
        }

        [Fact]
        public void ToObjectCombinedEnumPropertyFromString()
        {
            var dict = new Dictionary<string, object>()
            {
                { "EnumProperty", "One,Two" },
            };

            var value = dict.ToObject<ClassType>();
            Assert.Equal(EnumType.Three, value.EnumProperty);
        }

        [Fact]
        public void ToObjectUnknownProperty()
        {
            var dict = new Dictionary<string, object>()
            {
                { "StringProperty", "a" }, 
                { "IntProperty", 1 },
                { "UnknownProperty", "u" }
            };

            var value = dict.ToObject<ClassType>();
            Assert.Equal("a", value.StringProperty);
            Assert.Equal(1, value.IntProperty);
        }

        [Fact]
        public void ToObjectPrivateSetter()
        {
            var dict = new Dictionary<string, object>()
            {
                { "StringProperty", "a" }, 
                { "IntProperty", 1 },
                { "StringPropertyPrivateSetter", "p" }
            };

            var value = dict.ToObject<ClassType>();
            Assert.Equal("a", value.StringProperty);
            Assert.Equal(1, value.IntProperty);
            Assert.Equal("p", value.StringPropertyPrivateSetter);
        }

        [Fact]
        public void ToObjectField()
        {
            var dict = new Dictionary<string, object>()
            {
                { "StringProperty", "a" }, 
                { "IntProperty", 1 },
                { "StringField", "f" }
            };

            var value = dict.ToObject<ClassType>();
            Assert.Equal("a", value.StringProperty);
            Assert.Equal(1, value.IntProperty);
            Assert.Equal(null, value.StringField);
        }

        [Fact]
        public void ToObjectStringCollection()
        {
            var dict = new Dictionary<string, object>()
            {
                { "StringProperty", "a" }, 
                { "IntProperty", 1 },
                { "StringCollectionProperty", new [] {"x", "y", "z"}  }
            };

            var value = dict.ToObject<ClassType>();
            Assert.Equal("a", value.StringProperty);
            Assert.Equal(1, value.IntProperty);
            for (var index = 0; index < 3; index++)
            {
                Assert.Equal((dict["StringCollectionProperty"] as IList<string>)[index], value.StringCollectionProperty[index]);
            }
        }

        [Fact]
        public void ToObjectIntCollection()
        {
            var dict = new Dictionary<string, object>()
            {
                { "StringProperty", "a" }, 
                { "IntProperty", 1 },
                { "IntCollectionProperty", new [] {1, 2, 3}  }
            };

            var value = dict.ToObject<ClassType>();
            Assert.Equal("a", value.StringProperty);
            Assert.Equal(1, value.IntProperty);
            for (var index = 0; index < 3; index++)
            {
                Assert.Equal((dict["IntCollectionProperty"] as IList<int>)[index], value.IntCollectionProperty[index]);
            }
        }

        [Fact]
        public void ToObjectCompoundProperty()
        {
            var dict = new Dictionary<string, object>()
            {
                { "StringProperty", "a" }, 
                { "IntProperty", 1 },
                { "CompoundProperty", new Dictionary<string, object>() { { "StringProperty", "z" }, { "IntProperty", 0 } }  }
            };

            var value = dict.ToObject<ClassType>();
            Assert.Equal("a", value.StringProperty);
            Assert.Equal(1, value.IntProperty);
            Assert.Equal("z", value.CompoundProperty.StringProperty);
            Assert.Equal(0, value.CompoundProperty.IntProperty);
        }

        [Fact]
        public void ToObjectCompoundCollectionProperty()
        {
            var dict = new Dictionary<string, object>()
            {
                { "StringProperty", "a" }, 
                { "IntProperty", 1 },
                { "CompoundCollectionProperty", new[]
                    {
                        new Dictionary<string, object>() { { "StringProperty", "x" }, { "IntProperty", 1 } }, 
                        new Dictionary<string, object>() { { "StringProperty", "y" }, { "IntProperty", 2 } },
                        new Dictionary<string, object>() { { "StringProperty", "z" }, { "IntProperty", 3 } },
                    } 
                }
            };

            var value = dict.ToObject<ClassType>();
            Assert.Equal("a", value.StringProperty);
            Assert.Equal(1, value.IntProperty);
            for (var index = 0; index < 3; index++)
            {
                var kv = (dict["CompoundCollectionProperty"] as IList<IDictionary<string, object>>)[index];
                Assert.Equal(kv["StringProperty"], value.CompoundCollectionProperty[index].StringProperty);
                Assert.Equal(kv["IntProperty"], value.CompoundCollectionProperty[index].IntProperty);
            }
        }

        [Fact]
        public void ToObjectODataEntry()
        {
            var dict = new Dictionary<string, object>()
            {
                { "StringProperty", "a" }, 
                { "IntProperty", 1 },
            };

            var value = dict.ToObject<ODataEntry>(null, false);
            Assert.Equal("a", value["StringProperty"]);
            Assert.Equal(1, value["IntProperty"]);
        }

        [Fact]
        public void ToObjectDynamicODataEntry()
        {
            var _ = ODataDynamic.Expression;
            var dict = new Dictionary<string, object>()
            {
                { "StringProperty", "a" }, 
                { "IntProperty", 1 },
            };

            dynamic value = dict.ToObject<ODataEntry>(null, true);
            Assert.Equal("a", value.StringProperty);
            Assert.Equal(1, value.IntProperty);
        }

        [Fact]
        public void ToObjectCollectionDynamicODataEntry()
        {
            var _ = ODataDynamic.Expression;
            var dict = new[]
            {
                new Dictionary<string, object>() {{"StringProperty", "a"}, {"IntProperty", 1}},
                new Dictionary<string, object>() {{"StringProperty", "b"}, {"IntProperty", 2}},
            };

            var values = (dict.Select(x => x.ToObject<ODataEntry>(null, true)) as IEnumerable<dynamic>).ToArray();
            for (var index = 0; index < values.Count(); index++)
            {
                Assert.Equal(dict[index]["StringProperty"], values[index].StringProperty);
                Assert.Equal(dict[index]["IntProperty"], values[index].IntProperty);
            }
        }


        [Fact]
        public void ToObjectSpatial()
        {
            CustomConverters.RegisterTypeConverter(typeof(GeographyPoint), V4.Adapter.TypeConverters.CreateGeographyPoint);
            var dict = new Dictionary<string, object>()
            {
                { "Point", GeographyPoint.Create(CoordinateSystem.Geography(100), 1, 2, null, null) },
            };

            var value = dict.ToObject<ClassType>();
            Assert.Equal(100, value.Point.CoordinateSystem.EpsgId);
            Assert.Equal(1d, value.Point.Latitude);
            Assert.Equal(2d, value.Point.Longitude);
        }

        class ClassNoDefaultConstructor
        {
            public ClassNoDefaultConstructor(string arg) {}
        }

        [Fact]
        public void ToObjectClassWithoutDefaultCtor()
        {
            var dict = new Dictionary<string, object>()
            {
                { "Data", new ClassNoDefaultConstructor("test") },
            };

            Assert.Throws<InvalidOperationException>(() => dict.ToObject<ClassNoDefaultConstructor>());
        }
    }
}
