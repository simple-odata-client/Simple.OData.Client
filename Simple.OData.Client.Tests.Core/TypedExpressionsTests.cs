using System;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace Simple.OData.Client.Tests
{
    public class TypedExpressionV3Tests : TypedExpressionTests
    {
        public override string MetadataFile { get { return "Northwind.xml"; } }
        public override IFormatSettings FormatSettings { get { return new ODataV3Format(); } }
    }

    public class TypedExpressionV4Tests : TypedExpressionTests
    {
        public override string MetadataFile { get { return "Northwind4.xml"; } }
        public override IFormatSettings FormatSettings { get { return new ODataV4Format(); } }
    }

    public abstract class TypedExpressionTests : TestBase
    {
        class DataAttribute : Attribute
        {
            public string Name { get; set; }
            public string PropertyName { get; set; }
        }
        class DataMemberAttribute : Attribute
        {
            public string Name { get; set; }
            public string PropertyName { get; set; }
        }
        class OtherAttribute : Attribute
        {
            public string Name { get; set; }
            public string PropertyName { get; set; }
        }

        class TestEntity
        {
            public int ProductID { get; set; }
            public string ProductName { get; set; }
            public Guid LinkID { get; set; }
            public decimal Price { get; set; }
            public Address Address { get; set; }
            public DateTime CreationTime { get; set; }
            public DateTimeOffset Updated { get; set; }
            public TimeSpan Period { get; set; }
            public TestEntity Nested { get; set; }
            public TestEntity[] Collection { get; set; }

            [Column(Name = "Name")] 
            public string MappedName1 { get; set; }
            [Data(Name = "Name", PropertyName = "OtherName")]
            public string MappedName2 { get; set; }
            [DataMember(Name = "Name", PropertyName = "OtherName")]
            public string MappedName3 { get; set; }
            [Other(Name = "OtherName", PropertyName = "Name")]
            public string MappedName4 { get; set; }
            [DataMember(Name = "Name", PropertyName = "OtherName")]
            [Other(Name = "OtherName", PropertyName = "OtherName")]
            public string MappedName5 { get; set; }
        }

        [Fact]
        public void And()
        {
            Expression<Func<TestEntity, bool>> filter = x => x.ProductID == 1 && x.ProductName == "Chai";
            Assert.Equal("ProductID eq 1 and ProductName eq 'Chai'", ODataExpression.FromLinqExpression(filter).AsString(_session));
        }

        [Fact]
        public void Or()
        {
            Expression<Func<TestEntity, bool>> filter = x => x.ProductName == "Chai" || x.ProductID == 1;
            Assert.Equal("ProductName eq 'Chai' or ProductID eq 1", ODataExpression.FromLinqExpression(filter).AsString(_session));
        }

        [Fact]
        public void Not()
        {
            Expression<Func<TestEntity, bool>> filter = x => !(x.ProductName == "Chai");
            Assert.Equal("not (ProductName eq 'Chai')", ODataExpression.FromLinqExpression(filter).AsString(_session));
        }

        [Fact]
        public void Precedence()
        {
            Expression<Func<TestEntity, bool>> filter = x => (x.ProductID == 1 || x.ProductID == 2) && x.ProductName == "Chai";
            Assert.Equal("(ProductID eq 1 or ProductID eq 2) and ProductName eq 'Chai'", ODataExpression.FromLinqExpression(filter).AsString(_session));
        }

        [Fact]
        public void EqualString()
        {
            Expression<Func<TestEntity, bool>> filter = x => x.ProductName == "Chai";
            Assert.Equal("ProductName eq 'Chai'", ODataExpression.FromLinqExpression(filter).AsString(_session));
        }

        [Fact]
        public void EqualToString()
        {
            Expression<Func<TestEntity, bool>> filter = x => x.ProductName.ToString() == "Chai";
            Assert.Equal("ProductName eq 'Chai'", ODataExpression.FromLinqExpression(filter).AsString(_session));
        }

        [Fact]
        public void EqualNumeric()
        {
            Expression<Func<TestEntity, bool>> filter = x => x.ProductID == 1;
            Assert.Equal("ProductID eq 1", ODataExpression.FromLinqExpression(filter).AsString(_session));
        }

        [Fact]
        public void NotEqualNumeric()
        {
            Expression<Func<TestEntity, bool>> filter = x => x.ProductID != 1;
            Assert.Equal("ProductID ne 1", ODataExpression.FromLinqExpression(filter).AsString(_session));
        }

        [Fact]
        public void GreaterNumeric()
        {
            Expression<Func<TestEntity, bool>> filter = x => x.ProductID > 1;
            Assert.Equal("ProductID gt 1", ODataExpression.FromLinqExpression(filter).AsString(_session));
        }

        [Fact]
        public void GreaterOrEqualNumeric()
        {
            Expression<Func<TestEntity, bool>> filter = x => x.ProductID >= 1.5;
            Assert.Equal(string.Format("ProductID ge 1.5{0}", FormatSettings.DoubleNumberSuffix), ODataExpression.FromLinqExpression(filter).AsString(_session));
        }

        [Fact]
        public void LessNumeric()
        {
            Expression<Func<TestEntity, bool>> filter = x => x.ProductID < 1;
            Assert.Equal("ProductID lt 1", ODataExpression.FromLinqExpression(filter).AsString(_session));
        }

        [Fact]
        public void LessOrEqualNumeric()
        {
            Expression<Func<TestEntity, bool>> filter = x => x.ProductID <= 1;
            Assert.Equal("ProductID le 1", ODataExpression.FromLinqExpression(filter).AsString(_session));
        }

        [Fact]
        public void AddEqualNumeric()
        {
            Expression<Func<TestEntity, bool>> filter = x => x.ProductID + 1 == 2;
            Assert.Equal("ProductID add 1 eq 2", ODataExpression.FromLinqExpression(filter).AsString(_session));
        }

        [Fact]
        public void SubEqualNumeric()
        {
            Expression<Func<TestEntity, bool>> filter = x => x.ProductID - 1 == 2;
            Assert.Equal("ProductID sub 1 eq 2", ODataExpression.FromLinqExpression(filter).AsString(_session));
        }

        [Fact]
        public void MulEqualNumeric()
        {
            Expression<Func<TestEntity, bool>> filter = x => x.ProductID * 1 == 2;
            Assert.Equal("ProductID mul 1 eq 2", ODataExpression.FromLinqExpression(filter).AsString(_session));
        }

        [Fact]
        public void DivEqualNumeric()
        {
            Expression<Func<TestEntity, bool>> filter = x => x.ProductID / 1 == 2;
            Assert.Equal("ProductID div 1 eq 2", ODataExpression.FromLinqExpression(filter).AsString(_session));
        }

        [Fact]
        public void ModEqualNumeric()
        {
            Expression<Func<TestEntity, bool>> filter = x => x.ProductID % 1 == 2;
            Assert.Equal("ProductID mod 1 eq 2", ODataExpression.FromLinqExpression(filter).AsString(_session));
        }

        [Fact]
        public void EqualLong()
        {
            Expression<Func<TestEntity, bool>> filter = x => x.ProductID == 1L;
            Assert.Equal(string.Format("ProductID eq 1{0}", FormatSettings.LongNumberSuffix), ODataExpression.FromLinqExpression(filter).AsString(_session));
        }

        [Fact]
        public void EqualDecimal()
        {
            Expression<Func<TestEntity, bool>> filter = x => x.Price == 1M;
            Assert.Equal(string.Format("Price eq 1{0}", FormatSettings.DecimalNumberSuffix), ODataExpression.FromLinqExpression(filter).AsString(_session));
        }

        [Fact]
        public void EqualDecimalWithFractionalPart()
        {
            Expression<Func<TestEntity, bool>> filter = x => x.Price == 1.23M;
            Assert.Equal(string.Format("Price eq 1.23{0}", FormatSettings.DecimalNumberSuffix), ODataExpression.FromLinqExpression(filter).AsString(_session));
        }

        [Fact]
        public void EqualGuid()
        {
            Expression<Func<TestEntity, bool>> filter = x => x.LinkID == Guid.Empty;
            Assert.Equal(string.Format("LinkID eq {0}", FormatSettings.GetGuidFormat("00000000-0000-0000-0000-000000000000")), ODataExpression.FromLinqExpression(filter).AsString(_session));
        }

        [Fact]
        public void EqualDateTime()
        {
            if (FormatSettings.ODataVersion < 4)
            {
                Expression<Func<TestEntity, bool>> filter = x => x.CreationTime == new DateTime(2013, 1, 1);
                Assert.Equal("CreationTime eq datetime'2013-01-01T00:00:00'", ODataExpression.FromLinqExpression(filter).AsString(_session));
            }
        }

        [Fact]
        public void EqualDateTimeOffset()
        {
            Expression<Func<TestEntity, bool>> filter = x => x.Updated == new DateTimeOffset(new DateTime(2013, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            Assert.Equal(string.Format("Updated eq {0}", FormatSettings.GetDateTimeOffsetFormat("2013-01-01T00:00:00Z")), ODataExpression.FromLinqExpression(filter).AsString(_session));
        }

        [Fact]
        public void EqualTimeSpan()
        {
            Expression<Func<TestEntity, bool>> filter = x => x.Period == new TimeSpan(1, 2, 3);
            Assert.Equal(string.Format("Period eq {0}'PT1H2M3S'", FormatSettings.TimeSpanPrefix), ODataExpression.FromLinqExpression(filter).AsString(_session));
        }

        [Fact]
        public void LengthOfStringEqual()
        {
            Expression<Func<TestEntity, bool>> filter = x => x.ProductName.Length == 4;
            Assert.Equal("length(ProductName) eq 4", ODataExpression.FromLinqExpression(filter).AsString(_session));
        }

        [Fact]
        public void StringToLowerEqual()
        {
            Expression<Func<TestEntity, bool>> filter = x => x.ProductName.ToLower() == "chai";
            Assert.Equal("tolower(ProductName) eq 'chai'", ODataExpression.FromLinqExpression(filter).AsString(_session));
        }

        [Fact]
        public void StringToUpperEqual()
        {
            Expression<Func<TestEntity, bool>> filter = x => x.ProductName.ToUpper() == "CHAI";
            Assert.Equal("toupper(ProductName) eq 'CHAI'", ODataExpression.FromLinqExpression(filter).AsString(_session));
        }

        [Fact]
        public void StringStartsWithEqual()
        {
            Expression<Func<TestEntity, bool>> filter = x => x.ProductName.StartsWith("Ch") == true;
            Assert.Equal("startswith(ProductName,'Ch') eq true", ODataExpression.FromLinqExpression(filter).AsString(_session));
        }

        [Fact]
        public void StringEndsWithEqual()
        {
            Expression<Func<TestEntity, bool>> filter = x => x.ProductName.EndsWith("Ch") == true;
            Assert.Equal("endswith(ProductName,'Ch') eq true", ODataExpression.FromLinqExpression(filter).AsString(_session));
        }

        [Fact]
        public void StringContainsEqualTrue()
        {
            Expression<Func<TestEntity, bool>> filter = x => x.ProductName.Contains("ai") == true;
            Assert.Equal(string.Format("{0} eq true", FormatSettings.GetContainsFormat("ProductName", "ai")), ODataExpression.FromLinqExpression(filter).AsString(_session));
        }

        [Fact]
        public void StringContainsEqualFalse()
        {
            Expression<Func<TestEntity, bool>> filter = x => x.ProductName.Contains("ai") == false;
            Assert.Equal(string.Format("{0} eq false", FormatSettings.GetContainsFormat("ProductName", "ai")), ODataExpression.FromLinqExpression(filter).AsString(_session));
        }

        [Fact]
        public void StringContains()
        {
            Expression<Func<TestEntity, bool>> filter = x => x.ProductName.Contains("ai");
            Assert.Equal(FormatSettings.GetContainsFormat("ProductName", "ai"), ODataExpression.FromLinqExpression(filter).AsString(_session));
        }

        [Fact]
        public void StringNotContains()
        {
            Expression<Func<TestEntity, bool>> filter = x => !x.ProductName.Contains("ai");
            Assert.Equal(string.Format("not {0}", FormatSettings.GetContainsFormat("ProductName", "ai")), ODataExpression.FromLinqExpression(filter).AsString(_session));
        }

        [Fact]
        public void StringToLowerAndContains()
        {
            Expression<Func<TestEntity, bool>> filter = x => x.ProductName.ToLower().Contains("Chai");
            Assert.Equal(FormatSettings.GetContainsFormat("tolower(ProductName)","Chai"), ODataExpression.FromLinqExpression(filter).AsString(_session));
        }

        [Fact]
        public void IndexOfStringEqual()
        {
            Expression<Func<TestEntity, bool>> filter = x => x.ProductName.IndexOf("ai") == 1;
            Assert.Equal("indexof(ProductName,'ai') eq 1", ODataExpression.FromLinqExpression(filter).AsString(_session));
        }

        [Fact]
        public void SubstringWithPositionEqual()
        {
            Expression<Func<TestEntity, bool>> filter = x => x.ProductName.Substring(1) == "hai";
            Assert.Equal("substring(ProductName,1) eq 'hai'", ODataExpression.FromLinqExpression(filter).AsString(_session));
        }

        [Fact]
        public void SubstringWithPositionAndLengthEqual()
        {
            Expression<Func<TestEntity, bool>> filter = x => x.ProductName.Substring(1, 2) == "ha";
            Assert.Equal("substring(ProductName,1,2) eq 'ha'", ODataExpression.FromLinqExpression(filter).AsString(_session));
        }

        [Fact]
        public void ReplaceStringEqual()
        {
            Expression<Func<TestEntity, bool>> filter = x => x.ProductName.Replace("a", "o") == "Choi";
            Assert.Equal("replace(ProductName,'a','o') eq 'Choi'", ODataExpression.FromLinqExpression(filter).AsString(_session));
        }

        [Fact]
        public void TrimEqual()
        {
            Expression<Func<TestEntity, bool>> filter = x => x.ProductName.Trim() == "Chai";
            Assert.Equal("trim(ProductName) eq 'Chai'", ODataExpression.FromLinqExpression(filter).AsString(_session));
        }

        [Fact]
        public void ConcatEqual()
        {
            Expression<Func<TestEntity, bool>> filter = x => string.Concat(x.ProductName, "Chai") == "ChaiChai";
            Assert.Equal("concat(ProductName,'Chai') eq 'ChaiChai'", ODataExpression.FromLinqExpression(filter).AsString(_session));
        }

        [Fact]
        public void DayEqual()
        {
            Expression<Func<TestEntity, bool>> filter = x => x.CreationTime.Day == 1;
            Assert.Equal("day(CreationTime) eq 1", ODataExpression.FromLinqExpression(filter).AsString(_session));
        }

        [Fact]
        public void MonthEqual()
        {
            Expression<Func<TestEntity, bool>> filter = x => x.CreationTime.Month == 2;
            Assert.Equal("month(CreationTime) eq 2", ODataExpression.FromLinqExpression(filter).AsString(_session));
        }

        [Fact]
        public void YearEqual()
        {
            Expression<Func<TestEntity, bool>> filter = x => x.CreationTime.Year == 3;
            Assert.Equal("year(CreationTime) eq 3", ODataExpression.FromLinqExpression(filter).AsString(_session));
        }

        [Fact]
        public void HourEqual()
        {
            Expression<Func<TestEntity, bool>> filter = x => x.CreationTime.Hour == 4;
            Assert.Equal("hour(CreationTime) eq 4", ODataExpression.FromLinqExpression(filter).AsString(_session));
        }

        [Fact]
        public void MinuteEqual()
        {
            Expression<Func<TestEntity, bool>> filter = x => x.CreationTime.Minute == 5;
            Assert.Equal("minute(CreationTime) eq 5", ODataExpression.FromLinqExpression(filter).AsString(_session));
        }

        [Fact]
        public void SecondEqual()
        {
            Expression<Func<TestEntity, bool>> filter = x => x.CreationTime.Second == 6;
            Assert.Equal("second(CreationTime) eq 6", ODataExpression.FromLinqExpression(filter).AsString(_session));
        }

        [Fact]
        public void RoundEqual()
        {
            Expression<Func<TestEntity, bool>> filter = x => decimal.Round(x.Price) == 1;
            Assert.Equal(string.Format("round(Price) eq 1{0}", FormatSettings.DecimalNumberSuffix), ODataExpression.FromLinqExpression(filter).AsString(_session));
        }

        [Fact]
        public void FloorEqual()
        {
            Expression<Func<TestEntity, bool>> filter = x => decimal.Floor(x.Price) == 1;
            Assert.Equal(string.Format("floor(Price) eq 1{0}", FormatSettings.DecimalNumberSuffix), ODataExpression.FromLinqExpression(filter).AsString(_session));
        }

        [Fact]
        public void CeilingEqual()
        {
            Expression<Func<TestEntity, bool>> filter = x => decimal.Ceiling(x.Price) == 2;
            Assert.Equal(string.Format("ceiling(Price) eq 2{0}", FormatSettings.DecimalNumberSuffix), ODataExpression.FromLinqExpression(filter).AsString(_session));
        }

        [Fact]
        public void EqualNestedProperty()
        {
            Expression<Func<TestEntity, bool>> filter = x => x.Nested.ProductID == 1;
            Assert.Equal("Nested/ProductID eq 1", ODataExpression.FromLinqExpression(filter).AsString(_session));
        }

        [Fact]
        public void EqualNestedPropertyLengthOfStringEqual()
        {
            Expression<Func<TestEntity, bool>> filter = x => x.Nested.ProductName.Length == 4;
            Assert.Equal("length(Nested/ProductName) eq 4", ODataExpression.FromLinqExpression(filter).AsString(_session));
        }

        [Fact]
        public void ConvertEqual()
        {
            var id = "1";
            Expression<Func<TestEntity, bool>> filter = x => x.Nested.ProductID == Convert.ToInt32(id);
            Assert.Equal("Nested/ProductID eq 1", ODataExpression.FromLinqExpression(filter).AsString(_session));
        }

        [Fact]
        public void FilterWithMappedProperties()
        {
            Expression<Func<TestEntity, bool>> filter = x => x.MappedName1 == "Milk";
            Assert.Equal("Name eq 'Milk'", ODataExpression.FromLinqExpression(filter).AsString(_session));
            filter = x => x.MappedName2 == "Milk";
            Assert.Equal("Name eq 'Milk'", ODataExpression.FromLinqExpression(filter).AsString(_session));
            filter = x => x.MappedName3 == "Milk";
            Assert.Equal("Name eq 'Milk'", ODataExpression.FromLinqExpression(filter).AsString(_session));
            filter = x => x.MappedName4 == "Milk";
            Assert.Equal("Name eq 'Milk'", ODataExpression.FromLinqExpression(filter).AsString(_session));
            filter = x => x.MappedName5 == "Milk";
            Assert.Equal("Name eq 'Milk'", ODataExpression.FromLinqExpression(filter).AsString(_session));
        }

        [Fact]
        public void FilterWithEnum()
        {
            Expression<Func<TestEntity, bool>> filter = x => x.Address.Type == AddressType.Corporate;
            Assert.Equal(string.Format("Address/Type eq {0}", FormatSettings.GetEnumFormat(AddressType.Corporate, typeof(AddressType), "NorthwindModel")), 
                ODataExpression.FromLinqExpression(filter).AsString(_session));
        }

        [Fact]
        public void FilterWithEnum_LocalVar()
        {
            var addressType = AddressType.Corporate;
            Expression<Func<TestEntity, bool>> filter = x => x.Address.Type == addressType;
            Assert.Equal(string.Format("Address/Type eq {0}", FormatSettings.GetEnumFormat(AddressType.Corporate, typeof(AddressType), "NorthwindModel")),
                ODataExpression.FromLinqExpression(filter).AsString(_session));
        }

        private AddressType addressType = AddressType.Corporate;

        [Fact]
        public void FilterWithEnum_MemberVar()
        {
            Expression<Func<TestEntity, bool>> filter = x => x.Address.Type == this.addressType;
            Assert.Equal(string.Format("Address/Type eq {0}", FormatSettings.GetEnumFormat(AddressType.Corporate, typeof(AddressType), "NorthwindModel")),
                ODataExpression.FromLinqExpression(filter).AsString(_session));
        }

        [Fact]
        public void FilterWithEnum_Const()
        {
            const AddressType addressType = AddressType.Corporate;
            Expression<Func<TestEntity, bool>> filter = x => x.Address.Type == addressType;
            Assert.Equal(string.Format("Address/Type eq {0}", FormatSettings.GetEnumFormat(AddressType.Corporate, typeof(AddressType), "NorthwindModel")),
                ODataExpression.FromLinqExpression(filter).AsString(_session));
        }

        [Fact]
        public void FilterWithEnum_PrefixFree()
        {
            var enumPrefixFree = _session.Settings.EnumPrefixFree;
            _session.Settings.EnumPrefixFree = true;
            try
            {
                Expression<Func<TestEntity, bool>> filter = x => x.Address.Type == AddressType.Corporate;
                Assert.Equal(string.Format("Address/Type eq {0}", FormatSettings.GetEnumFormat(AddressType.Corporate, typeof(AddressType), "NorthwindModel", true)),
                    ODataExpression.FromLinqExpression(filter).AsString(_session));
            }
            finally
            {
                _session.Settings.EnumPrefixFree = enumPrefixFree;
            }
        }

        [Fact]
        public void FilterWithEnum_HasFlag()
        {
            Expression<Func<TestEntity, bool>> filter = x => x.Address.Type.HasFlag(AddressType.Corporate);
            Assert.Equal(string.Format("Address/Type has {0}", FormatSettings.GetEnumFormat(AddressType.Corporate, typeof(AddressType), "NorthwindModel")),
                ODataExpression.FromLinqExpression(filter).AsString(_session));
        }

        [Fact]
        public void FilterWithEnum_ToString()
        {
            Expression<Func<TestEntity, bool>> filter = x => x.Address.Type.ToString() == AddressType.Corporate.ToString();
            Assert.Equal(string.Format("Address/Type eq {0}", FormatSettings.GetEnumFormat(AddressType.Corporate, typeof(AddressType), "NorthwindModel")),
                ODataExpression.FromLinqExpression(filter).AsString(_session));
        }

        enum TestEnum { Zero, One, Two}

        [Fact]
        public void FilterDateTimeRange()
        {
            DateTime beforeDT = new DateTime(2013, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            DateTime afterDT = new DateTime(2014, 2, 2, 0, 0, 0, DateTimeKind.Utc);
            Expression<Func<TestEntity, bool>> filter = x => (x.CreationTime >= beforeDT) && (x.CreationTime < afterDT);
            if (FormatSettings.ODataVersion < 4)
            {
                Assert.Equal("CreationTime ge datetime'2013-01-01T00:00:00Z' and CreationTime lt datetime'2014-02-02T00:00:00Z'",
                    ODataExpression.FromLinqExpression(filter).AsString(_session));
            }
            else
            {
                Assert.Equal("CreationTime ge 2013-01-01T00:00:00Z and CreationTime lt 2014-02-02T00:00:00Z",
                    ODataExpression.FromLinqExpression(filter).AsString(_session));
            }
        }

        [Fact]
        public void ExpressionBuilder()
        {
            Expression<Predicate<TestEntity>> condition1 = x => x.ProductName == "Chai";
            Expression<Func<TestEntity, bool>> condition2 = x => x.ProductID == 1;
            var filter = new ODataExpression(condition1);
            filter = filter || new ODataExpression(condition2);
            Assert.Equal("ProductName eq 'Chai' or ProductID eq 1", filter.AsString(_session));
        }

        [Fact]
        public void ExpressionBuilderGeneric()
        {
            var filter = new ODataExpression<TestEntity>(x => x.ProductName == "Chai");
            filter = filter || new ODataExpression<TestEntity>(x => x.ProductID == 1);
            Assert.Equal("ProductName eq 'Chai' or ProductID eq 1", filter.AsString(_session));
        }

        [Fact]
        public void ExpressionBuilderGrouping()
        {
            Expression<Predicate<TestEntity>> condition1 = x => x.ProductName == "Chai";
            Expression<Func<TestEntity, bool>> condition2 = x => x.ProductID == 1;
            Expression<Predicate<TestEntity>> condition3 = x => x.ProductName == "Kaffe";
            Expression<Func<TestEntity, bool>> condition4 = x => x.ProductID == 2;
            var filter1 = new ODataExpression(condition1) || new ODataExpression(condition2);
            var filter2 = new ODataExpression(condition3) || new ODataExpression(condition4);
            var filter = filter1 && filter2;
            Assert.Equal("(ProductName eq 'Chai' or ProductID eq 1) and (ProductName eq 'Kaffe' or ProductID eq 2)", filter.AsString(_session));
        }
    }
}
