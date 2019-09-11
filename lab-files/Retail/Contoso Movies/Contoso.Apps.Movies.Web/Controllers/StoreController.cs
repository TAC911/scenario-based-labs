﻿using AutoMapper;
using Contoso.Apps.Common;
using Contoso.Apps.Common.Controllers;
using Contoso.Apps.Common.Extensions;
using Contoso.Apps.Movies.Data.Models;
using Contoso.Apps.Movies.Logic;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Web.Mvc;

namespace Contoso.Apps.Movies.Web.Controllers
{
    [AllowAnonymous]
    public class StoreController : BaseController
    {
        public StoreController()
        {
        }

        // GET: Store
        public ActionResult Index(string categoryId)
        {
            Contoso.Apps.Movies.Data.Models.User user = (Contoso.Apps.Movies.Data.Models.User)Session["User"];

            List<Item> products = new List<Item>();

            //only take 10 products...
            if (user != null)
            {
                    string name = user.Email;
                    int userId = user.UserId;
                    products = RecommendationHelper.GetViaFunction("assoc", userId, 12);
            }
            else
            {
                products = RecommendationHelper.GetViaFunction("top", 0, 12);
            }

            var productsVm = Mapper.Map<List<Models.ProductListModel>>(products);

            // Retrieve category listing:
            var categoriesVm = Mapper.Map<List<Models.CategoryModel>>(categories.ToList());

            var vm = new Models.StoreIndexModel
            {
                Products = productsVm,
                Categories = categoriesVm
            };

            return View(vm);
        }

        public ActionResult Genre(int categoryId)
        {
            Uri collectionUri = UriFactory.CreateDocumentCollectionUri(databaseId, "item");
            var query = client.CreateDocumentQuery<Item>(collectionUri, new SqlQuerySpec()
            {
                QueryText = "SELECT * FROM item f WHERE f.CategoryId = @id",
                Parameters = new SqlParameterCollection()
                    {
                        new SqlParameter("@id", categoryId)
                    }
            }, DefaultOptions);

            List<Item> products = query.ToList().Take(12).ToList();

            var productsVm = Mapper.Map<List<Models.ProductListModel>>(products);

            // Retrieve category listing:
            var categoriesVm = Mapper.Map<List<Models.CategoryModel>>(categories.ToList());

            var vm = new Models.StoreIndexModel
            {
                Products = productsVm,
                Categories = categoriesVm
            };

            return View(vm);
        }

        // GET: Store/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            Uri productCollectionUri = UriFactory.CreateDocumentCollectionUri(databaseId, "item");

            var query = client.CreateDocumentQuery<Item>(productCollectionUri, new SqlQuerySpec()
            {
                QueryText = "SELECT * FROM item f WHERE (f.ItemId = @id)",
                Parameters = new SqlParameterCollection()
                    {
                        new SqlParameter("@id", id)
                    }
            }, DefaultOptions);

            Item product = query.ToList().FirstOrDefault();

            if (product == null)
            {
                return HttpNotFound();
            }
            var productVm = Mapper.Map<Models.ProductModel>(product);

            //Get the simliar product to this item...
            var similarProducts = RecommendationHelper.GetViaFunction("content", 0, id.Value);
            var similarProductsVm = Mapper.Map<List<Models.ProductListModel>>(similarProducts);

            // Find related products, based on the category:
            var relatedProducts = items.ToList().Where(p => p.CategoryId == product.CategoryId && p.ItemId != product.ItemId).Take(10).ToList();
            var relatedProductsVm = Mapper.Map<List<Models.ProductListModel>>(relatedProducts);

            // Retrieve category listing:
            var categoriesVm = Mapper.Map<List<Models.CategoryModel>>(categories);

            // Retrieve "new products" as a list of three random products not equal to the displayed one:
            var newProducts = items.ToList().Where(p => p.ItemId != product.ItemId).ToList().Shuffle().Take(3);

            var newProductsVm = Mapper.Map<List<Models.ProductListModel>>(newProducts);

            var vm = new Models.StoreDetailsModel
            {
                Product = productVm,
                RelatedProducts = relatedProductsVm,
                SimilarProducts = similarProductsVm,
                NewProducts = newProductsVm,
                Categories = categoriesVm
            };

            return View(vm);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                
            }
            base.Dispose(disposing);
        }
    }
}