using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Simple.OData.Client.Extensions;
using Xunit;
using SpatialV3 = System.Spatial;
using SpatialV4 = Microsoft.Spatial;

namespace Simple.OData.Client.Tests.Extensions
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
#pragma warning disable CS0649 // Unused private types or members should be removed        {
            public string StringField;
            public string[] StringCollectionField;
#pragma warning restore CS0649 // Unused private types or members should be removed        {


            public string StringProperty { get; set; }
            public string StringPropertyPrivateSetter { get; private set; }
            public int IntProperty { get; set; }
            public EnumType EnumProperty { get; set; }
            public string[] StringArrayProperty { get; set; }
            public int[] IntArrayProperty { get; set; }
            public Collection<int> IntCollectionProperty { get; set; }
            public SubclassType CompoundProperty { get; set; }
            public SubclassType[] CompoundCollectionProperty { get; set; }
            public SpatialV3.GeographyPoint PointV3 { get; set; }
            public SpatialV4.GeographyPoint PointV4 { get; set; }
        }

        class SubclassType
        {
            public string StringProperty { get; set; }
            public int IntProperty { get; set; }
        }

        private ITypeCache _typeCache => TypeCaches.TypeCache("test", null);

        [Fact]
        public void ToObjectPrimitiveProperties()
        {
            var dict = new Dictionary<string, object>
            {
                { "StringProperty", "a" }, 
                { "IntProperty", 1 },
            };

            var value = dict.ToObject<ClassType>(_typeCache);
            Assert.Equal("a", value.StringProperty);
            Assert.Equal(1, value.IntProperty);
        }

        [Fact]
        public void ToObjectEnumPropertyFromInt()
        {
            var dict = new Dictionary<string, object>
            {
                { "EnumProperty", EnumType.One },
            };

            var value = dict.ToObject<ClassType>(_typeCache);
            Assert.Equal(EnumType.One, value.EnumProperty);
        }

        [Fact]
        public void ToObjectEnumPropertyFromString()
        {
            var dict = new Dictionary<string, object>
            {
                { "EnumProperty", "One" },
            };

            var value = dict.ToObject<ClassType>(_typeCache);
            Assert.Equal(EnumType.One, value.EnumProperty);
        }

        [Fact]
        public void ToObjectCombinedEnumPropertyFromInt()
        {
            var dict = new Dictionary<string, object>
            {
                { "EnumProperty", EnumType.One | EnumType.Two },
            };

            var value = dict.ToObject<ClassType>(_typeCache);
            Assert.Equal(EnumType.Three, value.EnumProperty);
        }

        [Fact]
        public void ToObjectCombinedEnumPropertyFromString()
        {
            var dict = new Dictionary<string, object>
            {
                { "EnumProperty", "One,Two" },
            };

            var value = dict.ToObject<ClassType>(_typeCache);
            Assert.Equal(EnumType.Three, value.EnumProperty);
        }

        [Fact]
        public void ToObjectUnknownProperty()
        {
            var dict = new Dictionary<string, object>
            {
                { "StringProperty", "a" }, 
                { "IntProperty", 1 },
                { "UnknownProperty", "u" }
            };

            var value = dict.ToObject<ClassType>(_typeCache);
            Assert.Equal("a", value.StringProperty);
            Assert.Equal(1, value.IntProperty);
        }

        [Fact]
        public void ToObjectPrivateSetter()
        {
            var dict = new Dictionary<string, object>
            {
                { "StringProperty", "a" }, 
                { "IntProperty", 1 },
                { "StringPropertyPrivateSetter", "p" }
            };

            var value = dict.ToObject<ClassType>(_typeCache);
            Assert.Equal("a", value.StringProperty);
            Assert.Equal(1, value.IntProperty);
            Assert.Equal("p", value.StringPropertyPrivateSetter);
        }

        [Fact]
        public void ToObjectField()
        {
            var dict = new Dictionary<string, object>
            {
                { "StringProperty", "a" }, 
                { "IntProperty", 1 },
                { "StringField", "f" }
            };

            var value = dict.ToObject<ClassType>(_typeCache);
            Assert.Equal("a", value.StringProperty);
            Assert.Equal(1, value.IntProperty);
            Assert.Null(value.StringField);
        }

        [Fact]
        public void ToObjectStringArray()
        {
            var dict = new Dictionary<string, object>
            {
                { "StringProperty", "a" }, 
                { "IntProperty", 1 },
                { "StringArrayProperty", new [] {"x", "y", "z"}  }
            };

            var value = dict.ToObject<ClassType>(_typeCache);
            Assert.Equal("a", value.StringProperty);
            Assert.Equal(1, value.IntProperty);
            for (var index = 0; index < 3; index++)
            {
                Assert.Equal((dict["StringArrayProperty"] as IList<string>)[index], value.StringArrayProperty[index]);
            }
        }

        [Fact]
        public void ToObjectIntArray()
        {
            var dict = new Dictionary<string, object>
            {
                { "StringProperty", "a" }, 
                { "IntProperty", 1 },
                { "IntArrayProperty", new [] {1, 2, 3}  }
            };

            var value = dict.ToObject<ClassType>(_typeCache);
            Assert.Equal("a", value.StringProperty);
            Assert.Equal(1, value.IntProperty);
            for (var index = 0; index < 3; index++)
            {
                Assert.Equal((dict["IntArrayProperty"] as IList<int>)[index], value.IntArrayProperty[index]);
            }
        }

        [Fact]
        public void ToObjectIntCollection()
        {
            var dict = new Dictionary<string, object>
            {
                { "StringProperty", "a" },
                { "IntProperty", 1 },
                { "IntCollectionProperty", new Collection<int> {1, 2, 3}  }
            };

            var value = dict.ToObject<ClassType>(_typeCache);
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
            var dict = new Dictionary<string, object>
            {
                { "StringProperty", "a" }, 
                { "IntProperty", 1 },
                { "CompoundProperty", new Dictionary<string, object> { { "StringProperty", "z" }, { "IntProperty", 0 } }  }
            };

            var value = dict.ToObject<ClassType>(_typeCache);
            Assert.Equal("a", value.StringProperty);
            Assert.Equal(1, value.IntProperty);
            Assert.Equal("z", value.CompoundProperty.StringProperty);
            Assert.Equal(0, value.CompoundProperty.IntProperty);
        }

        [Fact]
        public void ToObjectCompoundCollectionProperty()
        {
            var dict = new Dictionary<string, object>
            {
                { "StringProperty", "a" }, 
                { "IntProperty", 1 },
                { "CompoundCollectionProperty", new[]
                    {
                        new Dictionary<string, object> { { "StringProperty", "x" }, { "IntProperty", 1 } }, 
                        new Dictionary<string, object> { { "StringProperty", "y" }, { "IntProperty", 2 } },
                        new Dictionary<string, object> { { "StringProperty", "z" }, { "IntProperty", 3 } },
                    } 
                }
            };

            var value = dict.ToObject<ClassType>(_typeCache);
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

            var value = dict.ToObject<ODataEntry>(_typeCache);
            Assert.Equal("a", value["StringProperty"]);
            Assert.Equal(1, value["IntProperty"]);
        }

        [Fact]
        public void ToObjectDynamicODataEntry()
        {
            var _ = ODataDynamic.Expression;
            var dict = new Dictionary<string, object>
            {
                { "StringProperty", "a" }, 
                { "IntProperty", 1 },
            };

            _typeCache.Register<ODataEntry>();

            dynamic value = dict.ToObject<ODataEntry>(_typeCache, true);
            Assert.Equal("a", value.StringProperty);
            Assert.Equal(1, value.IntProperty);
        }

        [Fact]
        public void ToObjectCollectionDynamicODataEntry()
        {
            var _ = ODataDynamic.Expression;
            var dict = new[]
            {
                new Dictionary<string, object> {{"StringProperty", "a"}, {"IntProperty", 1}},
                new Dictionary<string, object> {{"StringProperty", "b"}, {"IntProperty", 2}},
            };

            var values = (dict.Select(x => x.ToObject<ODataEntry>(_typeCache, true)) as IEnumerable<dynamic>).ToArray();
            for (var index = 0; index < values.Count(); index++)
            {
                Assert.Equal(dict[index]["StringProperty"], values[index].StringProperty);
                Assert.Equal(dict[index]["IntProperty"], values[index].IntProperty);
            }
        }

        [Fact]
        public void ToObjectSpatialV3()
        {
            var dict = new Dictionary<string, object>
            {
                { "PointV3", SpatialV3.GeographyPoint.Create(SpatialV3.CoordinateSystem.Geography(100), 1, 2, null, null) },
            };

            _typeCache.Converter.RegisterTypeConverter(typeof(SpatialV3.GeographyPoint), V3.Adapter.TypeConverters.CreateGeographyPoint);

            var value = dict.ToObject<ClassType>(_typeCache);
            Assert.Equal(100, value.PointV3.CoordinateSystem.EpsgId);
            Assert.Equal(1d, value.PointV3.Latitude);
            Assert.Equal(2d, value.PointV3.Longitude);
        }

        [Fact]
        public void ToObjectSpatialV4()
        {
            var dict = new Dictionary<string, object>
            {
                { "PointV4", SpatialV4.GeographyPoint.Create(SpatialV4.CoordinateSystem.Geography(100), 1, 2, null, null) },
            };

            _typeCache.Converter.RegisterTypeConverter(typeof(SpatialV4.GeographyPoint), V4.Adapter.TypeConverters.CreateGeographyPoint);

            var value = dict.ToObject<ClassType>(_typeCache);
            Assert.Equal(100, value.PointV4.CoordinateSystem.EpsgId);
            Assert.Equal(1d, value.PointV4.Latitude);
            Assert.Equal(2d, value.PointV4.Longitude);
        }

        [Fact]
        public void ToObjectBaseType_NoType()
        {
            var dict = new Dictionary<string, object>
            {
                { "TransportID", 1 },
            };

            var value = dict.ToObject<Transport>(_typeCache);
            Assert.Equal(1, value.TransportID);
        }

        [Fact]
        public void ToObjectBaseType_Type()
        {
            var dict = new Dictionary<string, object>
            {
                { FluentCommand.AnnotationsLiteral, new ODataEntryAnnotations { TypeName = "Northwind.Transport" }} ,
                { "TransportID", 1 },
            };

            var value = dict.ToObject<Transport>(_typeCache);
            Assert.Equal(1, value.TransportID);
        }

        [Fact]
        public void ToObjectBaseType_SubType()
        {
            var dict = new Dictionary<string, object>
            {
                { FluentCommand.AnnotationsLiteral, new ODataEntryAnnotations { TypeName = "Northwind.Ship" } },
                { "TransportID", 1 },
                { "ShipName", "Sloop John B" },
            };

            var value = dict.ToObject<Transport>(_typeCache);
            var ship = value as Ship;
            Assert.NotNull(ship);
            Assert.Equal(1, ship.TransportID);
            Assert.Equal("Sloop John B", ship.ShipName);
        }

        [Fact]
        public void ToObjectBaseType_MissingSubType()
        {
            var dict = new Dictionary<string, object>
            {
                { FluentCommand.AnnotationsLiteral, new ODataEntryAnnotations { TypeName = "Northwind.Plane" } },
                { "TransportID", 1 },
                { "PlaneName", "Concorde" },
            };

            var value = dict.ToObject<Transport>(_typeCache);
            Assert.Equal(1, value.TransportID);
        }

        class ClassNoDefaultConstructor
        {
            public ClassNoDefaultConstructor(string arg) {}
        }

        [Fact]
        public void ToObjectClassWithoutDefaultCtor()
        {
            var dict = new Dictionary<string, object>
            {
                { "Data", new ClassNoDefaultConstructor("test") },
            };

            Assert.Throws<ConstructorNotFoundException>(() => dict.ToObject<ClassNoDefaultConstructor>(_typeCache));
        }
    }
}