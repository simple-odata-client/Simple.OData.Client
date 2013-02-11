﻿using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

using Entry = System.Collections.Generic.Dictionary<string, object>;

namespace Simple.OData.Client.Tests
{
    public class UpdateTests : TestBase
    {
        [Fact]
        public void UpdateByKey()
        {
            _client
                .For("Products")
                .Key(1)
                .Set(new {UnitPrice = 123m})
                .UpdateEntry();

            var product = _client
                .For("Products")
                .Filter("ProductID eq 1")
                .FindEntry();

            Assert.Equal(123m, product["UnitPrice"]);
        }

        [Fact]
        public void UpdateByObjectAsKey()
        {
            var product = _client
                .For("Products")
                .Key(1)
                .FindEntry();

            _client
                .For("Products")
                .Key(product)
                .Set(new { UnitPrice = 456m })
                .UpdateEntry();

            product = _client
                .For("Products")
                .Filter("ProductID eq 1")
                .FindEntry();

            Assert.Equal(456m, product["UnitPrice"]);
        }

        //[Fact]
        //public void AddSingleAssociation()
        //{
        //    var category = _db.Categories.Insert(CategoryName: "Test1");
        //    var product = _db.Products.Insert(ProductName: "Test2", UnitPrice: 18m);

        //    _db.Products.UpdateByProductName(ProductName: "Test2", Category: category);

        //    product = _db.Products.FindByProductName("Test2");
        //    Assert.Equal(category.CategoryID, product.CategoryID);
        //    category = _db.Category.WithProducts().FindByCategoryName("Test1");
        //    Assert.True(category.Products.Count == 1);
        //}

        //[Fact]
        //public void UpdateSingleAssociation()
        //{
        //    var category = _db.Categories.Insert(CategoryName: "Test1");
        //    var product = _db.Products.Insert(ProductName: "Test2", UnitPrice: 18m, CategoryID: 1);

        //    _db.Products.UpdateByProductName(ProductName: "Test2", Category: category);

        //    product = _db.Products.FindByProductName("Test2");
        //    Assert.Equal(category.CategoryID, product.CategoryID);
        //    category = _db.Category.WithProducts().FindByCategoryName("Test1");
        //    Assert.True(category.Products.Count == 1);
        //}

        //[Fact]
        //public void RemoveSingleAssociation()
        //{
        //    var category = _db.Categories.Insert(CategoryName: "Test6");
        //    var product = _db.Products.Insert(ProductName: "Test7", UnitPrice: 18m, Category: category);
        //    product = _db.Products.FindByProductName("Test7");
        //    _db.Products.UpdateByProductName(ProductName: "Test7", Category: category);
        //    product = _db.Products.FindByProductName("Test7");
        //    Assert.Equal(category.CategoryID, product.CategoryID);

        //    _db.Products.UpdateByProductName(ProductName: "Test7", Category: null);

        //    product = _db.Products.FindByProductName("Test7");
        //    Assert.Null(product.CategoryID);
        //}

        //[Fact]
        //public void UpdateFieldsAndAddAssociation()
        //{
        //    var category = _db.Categories.Insert(CategoryName: "Test1");
        //    var product = _db.Products.Insert(ProductName: "Test2", UnitPrice: 18m, CategoryID: 1);

        //    _db.Products.UpdateByProductName(ProductName: "Test2", UnitPrice: 19m, Category: category);

        //    product = _db.Products.FindByProductName("Test2");
        //    Assert.Equal(19m, product.UnitPrice);
        //    Assert.Equal(category.CategoryID, product.CategoryID);
        //}

        //[Fact]
        //public void UpdateFieldsAndRemoveAssociation()
        //{
        //    var category = _db.Categories.Insert(CategoryName: "Test1");
        //    var product = _db.Products.Insert(ProductName: "Test2", UnitPrice: 18m, CategoryID: 1);

        //    _db.Products.UpdateByProductName(ProductName: "Test2", UnitPrice: 19m, Category: null);

        //    product = _db.Products.FindByProductName("Test2");
        //    Assert.Equal(19m, product.UnitPrice);
        //    Assert.Null(product.CategoryID);
        //}

        //[Fact]
        //public void UpdateMultipleAssociations()
        //{
        //    var category = _db.Categories.Insert(CategoryName: "Test3");
        //    var product1 = _db.Products.Insert(ProductName: "Test4", UnitPrice: 21m, CategoryID: 1);
        //    var product2 = _db.Products.Insert(ProductName: "Test5", UnitPrice: 22m, CategoryID: 1);

        //    _db.Categories.UpdateByCategoryName(CategoryName: "Test3", Products: new[] { product1, product2 });

        //    category = _db.Category.WithProducts().FindByCategoryName("Test3");
        //    Assert.Equal(2, category.Products.Count);
        //}
    }
}
