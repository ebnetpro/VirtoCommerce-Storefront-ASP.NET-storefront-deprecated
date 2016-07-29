﻿using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using VirtoCommerce.Storefront.Model;
using VirtoCommerce.Storefront.Model.Catalog;
using VirtoCommerce.Storefront.Model.Common;
using VirtoCommerce.Storefront.Model.Services;

namespace VirtoCommerce.Storefront.Controllers
{
    [OutputCache(CacheProfile = "ProductCachingProfile")]
    public class ProductController : StorefrontControllerBase
    {
        private readonly ICatalogSearchService _catalogSearchService;

        public ProductController(WorkContext context, IStorefrontUrlBuilder urlBuilder, ICatalogSearchService catalogSearchService)
            : base(context, urlBuilder)
        {
            _catalogSearchService = catalogSearchService;
        }

        /// <summary>
        /// GET: /product/{productId}
        /// This action used by storefront to get product details by product id
        /// </summary>
        /// <param name="productId"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<ActionResult> ProductDetails(string productId)
        {
            var product = (await _catalogSearchService.GetProductsAsync(new[] { productId }, WorkContext.CurrentProductResponseGroup)).FirstOrDefault();
            WorkContext.CurrentProduct = product;

            if (product != null)
            {
                WorkContext.CurrentPageSeo = product.SeoInfo;

                if (product.CategoryId != null)
                {
                    var category = (await _catalogSearchService.GetCategoriesAsync(new[] { product.CategoryId }, CategoryResponseGroup.Full)).FirstOrDefault();
                    WorkContext.CurrentCategory = category;

                    if (category != null)
                    {
                        category.Products = new MutablePagedList<Product>((pageNumber, pageSize, sortInfos) =>
                        {
                            var criteria = WorkContext.CurrentCatalogSearchCriteria.Clone();
                            criteria.CategoryId = product.CategoryId;
                            criteria.PageNumber = pageNumber;
                            criteria.PageSize = pageSize;
                            if (string.IsNullOrEmpty(criteria.SortBy) && !sortInfos.IsNullOrEmpty())
                            {
                                criteria.SortBy = SortInfo.ToString(sortInfos);
                            }
                            return _catalogSearchService.SearchProducts(criteria).Products;
                        });
                    }
                }
            }

            return View("product", WorkContext);
        }
    }
}
