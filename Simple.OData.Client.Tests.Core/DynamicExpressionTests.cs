using System;
using System.Globalization;
using Xunit;

namespace Simple.OData.Client.Tests
{
    public class DynamicExpressionV4Tests : DynamicExpressionTests
    {
        public override string MetadataFile { get { return "Northwind4.xml"; } }
        public override IFormatSettings FormatSettings { get { return new ODataV4Format(); } }
    }

    public abstract class DynamicExpressionTests : TestBase
    {
        [Fact]
        public void And()
        {
            var x = ODataDynamic.Expression;
            var filter = x.CategoryID == 1 && x.ProductName == "Chai";
            Assert.Equal("CategoryID eq 1 and ProductName eq 'Chai'", filter.AsString(_session));
        }

        [Fact]
        public void Or()
        {
            var x = ODataDynamic.Expression;
            var filter = x.ProductName == "Chai" || x.CategoryID == 1;
            Assert.Equal("ProductName eq 'Chai' or CategoryID eq 1", filter.AsString(_session));
        }

        [Fact]
        public void Not()
        {
            var x = ODataDynamic.Expression;
            var filter = !(x.ProductName == "Chai");
            Assert.Equal("not(ProductName eq 'Chai')", filter.AsString(_session));
        }

        [Fact]
        public void Precedence()
        {
            var x = ODataDynamic.Expression;
            var filter = (x.CategoryID == 1 || x.CategoryID == 2) && x.ProductName == "Chai";
            Assert.Equal("(CategoryID eq 1 or CategoryID eq 2) and ProductName eq 'Chai'", filter.AsString(_session));
        }

        [Fact]
        public void EqualString()
        {
            var x = ODataDynamic.Expression;
            var filter = x.ProductName == "Chai";
            Assert.Equal("ProductName eq 'Chai'", filter.AsString(_session));
        }

        [Fact]
        public void EqualNumeric()
        {
            var x = ODataDynamic.Expression;
            var filter = x.CategoryID == 1;
            Assert.Equal("CategoryID eq 1", filter.AsString(_session));
        }

        [Fact]
        public void NotEqualNumeric()
        {
            var x = ODataDynamic.Expression;
            var filter = x.CategoryID != 1;
            Assert.Equal("CategoryID ne 1", filter.AsString(_session));
        }

        [Fact]
        public void GreaterNumeric()
        {
            var x = ODataDynamic.Expression;
            var filter = x.CategoryID > 1;
            Assert.Equal("CategoryID gt 1", filter.AsString(_session));
        }

        [Fact]
        public void GreaterOrEqualNumeric()
        {
            var x = ODataDynamic.Expression;
            var filter = x.CategoryID >= 1.5;
            Assert.Equal(string.Format("CategoryID ge 1.5{0}", FormatSettings.DoubleNumberSuffix), filter.AsString(_session));
        }

        [Fact]
        public void LessNumeric()
        {
            var x = ODataDynamic.Expression;
            var filter = x.CategoryID < 1;
            Assert.Equal("CategoryID lt 1", filter.AsString(_session));
        }

        [Fact]
        public void LessOrEqualNumeric()
        {
            var x = ODataDynamic.Expression;
            var filter = x.CategoryID <= 1;
            Assert.Equal("CategoryID le 1", filter.AsString(_session));
        }

        [Fact]
        public void AddEqualNumeric()
        {
            var x = ODataDynamic.Expression;
            var filter = x.CategoryID + 1 == 2;
            Assert.Equal("CategoryID add 1 eq 2", filter.AsString(_session));
        }

        [Fact]
        public void SubEqualNumeric()
        {
            var x = ODataDynamic.Expression;
            var filter = x.CategoryID - 1 == 2;
            Assert.Equal("CategoryID sub 1 eq 2", filter.AsString(_session));
        }

        [Fact]
        public void MulEqualNumeric()
        {
            var x = ODataDynamic.Expression;
            var filter = x.CategoryID * 1 == 2;
            Assert.Equal("CategoryID mul 1 eq 2", filter.AsString(_session));
        }

        [Fact]
        public void DivEqualNumeric()
        {
            var x = ODataDynamic.Expression;
            var filter = x.CategoryID / 1 == 2;
            Assert.Equal("CategoryID div 1 eq 2", filter.AsString(_session));
        }

        [Fact]
        public void ModEqualNumeric()
        {
            var x = ODataDynamic.Expression;
            var filter = x.CategoryID % 1 == 2;
            Assert.Equal("CategoryID mod 1 eq 2", filter.AsString(_session));
        }

        [Fact]
        public void EqualLong()
        {
            var x = ODataDynamic.Expression;
            var filter = x.CategoryID == 1L;
            Assert.Equal(string.Format("CategoryID eq 1{0}", FormatSettings.LongNumberSuffix), filter.AsString(_session));
        }

        [Fact]
        public void EqualDecimal()
        {
            var x = ODataDynamic.Expression;
            var filter = x.Total == 1M;
            Assert.Equal(string.Format("Total eq 1{0}", FormatSettings.DecimalNumberSuffix), filter.AsString(_session));
        }

        [Fact]
        public void EqualDecimalWithFractionalPart()
        {
            var x = ODataDynamic.Expression;
            var filter = x.Total == 1.23M;
            Assert.Equal(string.Format("Total eq 1.23{0}", FormatSettings.DecimalNumberSuffix), filter.AsString(_session));
        }

        [Fact]
        public void EqualGuid()
        {
            var x = ODataDynamic.Expression;
            var filter = x.LinkID == Guid.Empty;
            Assert.Equal(string.Format("LinkID eq {0}", FormatSettings.GetGuidFormat("00000000-0000-0000-0000-000000000000")), filter.AsString(_session));
        }

        [Fact]
        public void EqualDateTime()
        {
            if (FormatSettings.ODataVersion < 4)
            {
                var x = ODataDynamic.Expression;
                var filter = x.Updated == new DateTime(2013, 1, 1, 0, 0, 0, 123, DateTimeKind.Utc);
                Assert.Equal("Updated eq datetime'2013-01-01T00:00:00.123Z'", filter.AsString(_session));
            }
        }

        [Fact]
        public void EqualDateTimeOffset()
        {
            var x = ODataDynamic.Expression;
            var filter = x.Updated == new DateTimeOffset(new DateTime(2013, 1, 1, 0, 0, 0, DateTimeKind.Utc));
            Assert.Equal(string.Format("Updated eq {0}", FormatSettings.GetDateTimeOffsetFormat("2013-01-01T00:00:00Z")), filter.AsString(_session));
        }

        [Fact]
        public void EqualTimeSpan()
        {
            var x = ODataDynamic.Expression;
            var filter = x.Period == new TimeSpan(1, 2, 3);
            Assert.Equal(string.Format("Period eq {0}'PT1H2M3S'", FormatSettings.TimeSpanPrefix), filter.AsString(_session));
        }

        [Fact]
        public void LengthOfStringEqual()
        {
            var x = ODataDynamic.Expression;
            var filter = x.ProductName.Length() == 4;
            Assert.Equal("length(ProductName) eq 4", filter.AsString(_session));
        }

        [Fact]
        public void StringToLowerEqual()
        {
            var x = ODataDynamic.Expression;
            var filter = x.ProductName.ToLower() == "chai";
            Assert.Equal("tolower(ProductName) eq 'chai'", filter.AsString(_session));
        }

        [Fact]
        public void StringToUpperEqual()
        {
            var x = ODataDynamic.Expression;
            var filter = x.ProductName.ToUpper() == "CHAI";
            Assert.Equal("toupper(ProductName) eq 'CHAI'", filter.AsString(_session));
        }

        [Fact]
        public void StringStartsWithEqual()
        {
            var x = ODataDynamic.Expression;
            var filter = x.ProductName.StartsWith("Ch") == true;
            Assert.Equal("startswith(ProductName,'Ch') eq true", filter.AsString(_session));
        }

        [Fact]
        public void StringEndsWithEqual()
        {
            var x = ODataDynamic.Expression;
            var filter = x.ProductName.EndsWith("Ch") == true;
            Assert.Equal("endswith(ProductName,'Ch') eq true", filter.AsString(_session));
        }

        [Fact]
        public void StringContainsEqualTrue()
        {
            var x = ODataDynamic.Expression;
            var filter = x.ProductName.Contains("ai") == true;
            Assert.Equal(string.Format("{0} eq true", FormatSettings.GetContainsFormat("ProductName", "ai")), filter.AsString(_session));
        }

        [Fact]
        public void StringContainsEqualFalse()
        {
            var x = ODataDynamic.Expression;
            var filter = x.ProductName.Contains("ai") == false;
            Assert.Equal(string.Format("{0} eq false", FormatSettings.GetContainsFormat("ProductName", "ai")), filter.AsString(_session));
        }

        [Fact]
        public void StringContains()
        {
            var x = ODataDynamic.Expression;
            var filter = x.ProductName.Contains("ai");
            Assert.Equal(FormatSettings.GetContainsFormat("ProductName", "ai"), filter.AsString(_session));
        }

        [Fact]
        public void StringNotContains()
        {
            var x = ODataDynamic.Expression;
            var filter = !x.ProductName.Contains("ai");
            Assert.Equal(string.Format("not {0}", FormatSettings.GetContainsFormat("ProductName", "ai")), filter.AsString(_session));
        }

        [Fact]
        public void StringToLowerAndContains()
        {
            var x = ODataDynamic.Expression;
            var filter = x.ProductName.ToLower().Contains("Chai");
            Assert.Equal(FormatSettings.GetContainsFormat("tolower(ProductName)", "Chai"), filter.AsString(_session));
        }

        [Fact]
        public void IndexOfStringEqual()
        {
            var x = ODataDynamic.Expression;
            var filter = x.ProductName.IndexOf("ai") == 1;
            Assert.Equal("indexof(ProductName,'ai') eq 1", filter.AsString(_session));
        }

        [Fact]
        public void SubstringWithPositionEqual()
        {
            var x = ODataDynamic.Expression;
            var filter = x.ProductName.Substring(1) == "hai";
            Assert.Equal("substring(ProductName,1) eq 'hai'", filter.AsString(_session));
        }

        [Fact]
        public void SubstringWithPositionAndLengthEqual()
        {
            var x = ODataDynamic.Expression;
            var filter = x.ProductName.Substring(1,2) == "ha";
            Assert.Equal("substring(ProductName,1,2) eq 'ha'", filter.AsString(_session));
        }

        [Fact]
        public void ReplaceStringEqual()
        {
            var x = ODataDynamic.Expression;
            var filter = x.ProductName.Replace("a","o") == "Choi";
            Assert.Equal("replace(ProductName,'a','o') eq 'Choi'", filter.AsString(_session));
        }

        [Fact]
        public void TrimEqual()
        {
            var x = ODataDynamic.Expression;
            var filter = x.ProductName.Trim() == "Chai";
            Assert.Equal("trim(ProductName) eq 'Chai'", filter.AsString(_session));
        }

        [Fact]
        public void ConcatEqual()
        {
            var x = ODataDynamic.Expression;
            var filter = x.ProductName.Concat("Chai") == "ChaiChai";
            Assert.Equal("concat(ProductName,'Chai') eq 'ChaiChai'", filter.AsString(_session));
        }

        [Fact]
        public void DayEqual()
        {
            var x = ODataDynamic.Expression;
            var filter = x.CreationTime.Day == 1;
            Assert.Equal("day(CreationTime) eq 1", filter.AsString(_session));
        }

        [Fact]
        public void MonthEqual()
        {
            var x = ODataDynamic.Expression;
            var filter = x.CreationTime.Month == 2;
            Assert.Equal("month(CreationTime) eq 2", filter.AsString(_session));
        }

        [Fact]
        public void YearEqual()
        {
            var x = ODataDynamic.Expression;
            var filter = x.CreationTime.Year == 3;
            Assert.Equal("year(CreationTime) eq 3", filter.AsString(_session));
        }

        [Fact]
        public void HourEqual()
        {
            var x = ODataDynamic.Expression;
            var filter = x.CreationTime.Hour == 4;
            Assert.Equal("hour(CreationTime) eq 4", filter.AsString(_session));
        }

        [Fact]
        public void MinuteEqual()
        {
            var x = ODataDynamic.Expression;
            var filter = x.CreationTime.Minute == 5;
            Assert.Equal("minute(CreationTime) eq 5", filter.AsString(_session));
        }

        [Fact]
        public void SecondEqual()
        {
            var x = ODataDynamic.Expression;
            var filter = x.CreationTime.Second == 6;
            Assert.Equal("second(CreationTime) eq 6", filter.AsString(_session));
        }

        [Fact]
        public void RoundEqual()
        {
            var x = ODataDynamic.Expression;
            var filter = x.Price.Round() == 1;
            Assert.Equal("round(Price) eq 1", filter.AsString(_session));
        }

        [Fact]
        public void FloorEqual()
        {
            var x = ODataDynamic.Expression;
            var filter = x.Price.Floor() == 1;
            Assert.Equal("floor(Price) eq 1", filter.AsString(_session));
        }

        [Fact]
        public void CeilingEqual()
        {
            var x = ODataDynamic.Expression;
            var filter = x.Price.Ceiling() == 2;
            Assert.Equal("ceiling(Price) eq 2", filter.AsString(_session));
        }

        [Fact]
        public void EqualNestedProperty()
        {
            var x = ODataDynamic.Expression;
            var filter = x.Nested.ProductID == 1;
            Assert.Equal("Nested/ProductID eq 1", filter.AsString(_session));
        }

        [Fact]
        public void FilterWithEnum()
        {
            var x = ODataDynamic.Expression;
            var filter = x.Address.Type == AddressType.Corporate;
            Assert.Equal(string.Format("Address/Type eq {0}", FormatSettings.GetEnumFormat(AddressType.Corporate, typeof(AddressType), "NorthwindModel")),
                filter.AsString(_session));
        }

        [Fact]
        public void FilterWithEnum_LocalVar()
        {
            var x = ODataDynamic.Expression;
            var addressType = AddressType.Corporate;
            var filter = x.Address.Type == addressType;
            Assert.Equal(string.Format("Address/Type eq {0}", FormatSettings.GetEnumFormat(AddressType.Corporate, typeof(AddressType), "NorthwindModel")),
                filter.AsString(_session));
        }

        [Fact]
        public void FilterWithEnum_Const()
        {
            var x = ODataDynamic.Expression;
            const AddressType addressType = AddressType.Corporate;
            var filter = x.Address.Type == addressType;
            Assert.Equal(string.Format("Address/Type eq {0}", FormatSettings.GetEnumFormat(AddressType.Corporate, typeof(AddressType), "NorthwindModel")),
                filter.AsString(_session));
        }
    }
}