﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PagedList;
using VirtoCommerce.CatalogModule.Client.Api;
using VirtoCommerce.InventoryModule.Client.Api;
using VirtoCommerce.SearchModule.Client.Api;
using VirtoCommerce.Storefront.Converters;
using VirtoCommerce.Storefront.Model;
using VirtoCommerce.Storefront.Model.Catalog;
using VirtoCommerce.Storefront.Model.Common;
using VirtoCommerce.Storefront.Model.Customer.Services;
using VirtoCommerce.Storefront.Model.Marketing.Services;
using VirtoCommerce.Storefront.Model.Pricing.Services;
using VirtoCommerce.Storefront.Model.Services;

namespace VirtoCommerce.Storefront.Services
{
    public class CatalogSearchServiceImpl : ICatalogSearchService
    {
        private readonly IVirtoCommerceCatalogApi _catalogModuleApi;
        private readonly IPricingService _pricingService;
        private readonly IVirtoCommerceInventoryApi _inventoryModuleApi;
        private readonly IVirtoCommerceSearchApi _searchApi;
        private readonly IPromotionEvaluator _promotionEvaluator;
        private readonly ICustomerService _customerService;
        private readonly Func<WorkContext> _workContextFactory;

        public CatalogSearchServiceImpl(Func<WorkContext> workContextFactory, IVirtoCommerceCatalogApi catalogModuleApi, IPricingService pricingService, IVirtoCommerceInventoryApi inventoryModuleApi, IVirtoCommerceSearchApi searchApi, IPromotionEvaluator promotionEvaluator, ICustomerService customerService)
        {
            _workContextFactory = workContextFactory;
            _catalogModuleApi = catalogModuleApi;
            _pricingService = pricingService;
            _inventoryModuleApi = inventoryModuleApi;
            _searchApi = searchApi;
            _promotionEvaluator = promotionEvaluator;
            _customerService = customerService;
        }

        #region ICatalogSearchService Members
        public async Task<Product[]> GetProductsAsync(string[] ids, ItemResponseGroup responseGroup = ItemResponseGroup.None)
        {
            var workContext = _workContextFactory();
            if (responseGroup == ItemResponseGroup.None)
            {
                responseGroup = workContext.CurrentProductResponseGroup;
            }

            var retVal = (await _catalogModuleApi.CatalogModuleProductsGetProductByIdsAsync(ids.ToList(), ((int)responseGroup).ToString())).Select(x => x.ToWebModel(workContext.CurrentLanguage, workContext.CurrentCurrency, workContext.CurrentStore)).ToArray();

            var allProducts = retVal.Concat(retVal.SelectMany(x => x.Variations)).ToList();

            if (!allProducts.IsNullOrEmpty())
            {
                var taskList = new List<Task>();

                if (responseGroup.HasFlag(ItemResponseGroup.ItemAssociations))
                {
                    taskList.Add(LoadProductAssociationsAsync(allProducts));
                }

                if (responseGroup.HasFlag(ItemResponseGroup.Inventory))
                {
                    taskList.Add(LoadProductInventoriesAsync(allProducts));
                }

                if (responseGroup.HasFlag(ItemResponseGroup.ItemWithPrices))
                {
                    await _pricingService.EvaluateProductPricesAsync(allProducts);
                    if ((responseGroup | ItemResponseGroup.ItemWithDiscounts) == responseGroup)
                    {
                        await LoadProductDiscountsAsync(allProducts);
                    }
                }

                if (responseGroup.HasFlag(ItemResponseGroup.ItemWithVendor))
                {
                    await LoadProductVendorsAsync(allProducts);
                }

                await Task.WhenAll(taskList.ToArray());
            }

            return retVal;
        }

        public async Task<Category[]> GetCategoriesAsync(string[] ids, CategoryResponseGroup responseGroup = CategoryResponseGroup.Info)
        {
            var workContext = _workContextFactory();

            var retVal = (await _catalogModuleApi.CatalogModuleCategoriesGetCategoriesByIdsAsync(ids.ToList(), ((int)responseGroup).ToString())).Select(x => x.ToWebModel(workContext.CurrentLanguage, workContext.CurrentStore)).ToArray();

            return retVal;
        }

        /// <summary>
        /// Async search categories by given criteria 
        /// </summary>
        /// <param name="criteria"></param>
        /// <returns></returns>
        public async Task<IPagedList<Category>> SearchCategoriesAsync(CatalogSearchCriteria criteria)
        {
            var workContext = _workContextFactory();
            criteria = criteria.Clone();
            //exclude products
            criteria.ResponseGroup = criteria.ResponseGroup & (~CatalogSearchResponseGroup.WithProducts);
            //include categories
            criteria.ResponseGroup = criteria.ResponseGroup | CatalogSearchResponseGroup.WithCategories;
            var searchCriteria = criteria.ToCatalogApiModel(workContext);
            var result = await _catalogModuleApi.CatalogModuleSearchSearchAsync(searchCriteria);

            //API temporary does not support paginating request to categories (that's uses PagedList with superset instead StaticPagedList)
            return new PagedList<Category>(result.Categories.Select(x => x.ToWebModel(workContext.CurrentLanguage, workContext.CurrentStore)), criteria.PageNumber, criteria.PageSize);
        }

        /// <summary>
        /// search categories by given criteria 
        /// </summary>
        /// <param name="criteria"></param>
        /// <returns></returns>
        public IPagedList<Category> SearchCategories(CatalogSearchCriteria criteria)
        {
            var workContext = _workContextFactory();
            criteria = criteria.Clone();
            //exclude products
            criteria.ResponseGroup = criteria.ResponseGroup & (~CatalogSearchResponseGroup.WithProducts);
            //include categories
            criteria.ResponseGroup = criteria.ResponseGroup | CatalogSearchResponseGroup.WithCategories;
            var searchCriteria = criteria.ToCatalogApiModel(workContext);
            var categories = _catalogModuleApi.CatalogModuleSearchSearch(searchCriteria).Categories.Select(x => x.ToWebModel(workContext.CurrentLanguage, workContext.CurrentStore)).ToList();

            //API temporary does not support paginating request to categories (that's uses PagedList with superset)
            return new PagedList<Category>(categories, criteria.PageNumber, criteria.PageSize);
        }

        /// <summary>
        /// Async search products by given criteria 
        /// </summary>
        /// <param name="criteria"></param>
        /// <returns></returns>
        public async Task<CatalogSearchResult> SearchProductsAsync(CatalogSearchCriteria criteria)
        {
            criteria = criteria.Clone();
            //exclude categories
            criteria.ResponseGroup = criteria.ResponseGroup & (~CatalogSearchResponseGroup.WithCategories);
            //include products
            criteria.ResponseGroup = criteria.ResponseGroup | CatalogSearchResponseGroup.WithProducts;

            var workContext = _workContextFactory();
            var searchCriteria = criteria.ToSearchApiModel(workContext);
            var result = await _searchApi.SearchModuleSearchAsync(searchCriteria);
            var products = result.Products.Select(x => x.ToWebModel(workContext.CurrentLanguage, workContext.CurrentCurrency, workContext.CurrentStore)).ToList();

            if (!products.IsNullOrEmpty())
            {
                var productsWithVariations = products.Concat(products.SelectMany(x => x.Variations)).ToList();
                var taskList = new List<Task>
                {
                    LoadProductInventoriesAsync(productsWithVariations),
                    _pricingService.EvaluateProductPricesAsync(productsWithVariations)
                };
                await Task.WhenAll(taskList.ToArray());
            }

            return new CatalogSearchResult
            {
                Products = new StaticPagedList<Product>(products, criteria.PageNumber, criteria.PageSize, result.ProductsTotalCount ?? 0),
                Aggregations = !result.Aggregations.IsNullOrEmpty() ? result.Aggregations.Select(x => x.ToWebModel(workContext.CurrentLanguage.CultureName)).ToArray() : new Aggregation[] { }
            };
        }


        /// <summary>
        /// Search products by given criteria 
        /// </summary>
        /// <param name="criteria"></param>
        /// <returns></returns>
        public CatalogSearchResult SearchProducts(CatalogSearchCriteria criteria)
        {
            var workContext = _workContextFactory();
            criteria = criteria.Clone();
            //exclude categories
            criteria.ResponseGroup = criteria.ResponseGroup & (~CatalogSearchResponseGroup.WithCategories);
            //include products
            criteria.ResponseGroup = criteria.ResponseGroup | CatalogSearchResponseGroup.WithProducts;

            var searchCriteria = criteria.ToSearchApiModel(workContext);

            var result = _searchApi.SearchModuleSearch(searchCriteria);
            var products = result.Products.Select(x => x.ToWebModel(workContext.CurrentLanguage, workContext.CurrentCurrency, workContext.CurrentStore)).ToList();
            var productsWithVariations = products.Concat(products.SelectMany(x => x.Variations)).ToList();
            //Unable to make parallel call because its synchronous method (in future this information pricing and inventory will be getting from search index) and this lines can be removed
            _pricingService.EvaluateProductPrices(productsWithVariations);
            LoadProductInventories(productsWithVariations);

            return new CatalogSearchResult
            {
                Products = new StaticPagedList<Product>(products, criteria.PageNumber, criteria.PageSize, result.ProductsTotalCount ?? 0),
                Aggregations = !result.Aggregations.IsNullOrEmpty() ? result.Aggregations.Select(x => x.ToWebModel(workContext.CurrentLanguage.CultureName)).ToArray() : new Aggregation[] { }
            };
        }

        #endregion

        private async Task LoadProductVendorsAsync(List<Product> products)
        {
            var vendorIds = products.Where(p => !string.IsNullOrEmpty(p.VendorId)).Select(p => p.VendorId).Distinct().ToList();
            var vendorTasks = vendorIds.Select(id => _customerService.GetVendorByIdAsync(id));
            var vendors = await Task.WhenAll(vendorTasks);
            foreach (var product in products)
            {
                product.Vendor = vendors.FirstOrDefault(v => v.Id == product.VendorId);
            }
        }

        private async Task LoadProductDiscountsAsync(List<Product> products)
        {
            var workContext = _workContextFactory();
            var promotionContext = workContext.ToPromotionEvaluationContext(products);
            promotionContext.PromoEntries = products.Select(x => x.ToPromotionItem()).ToList();
            await _promotionEvaluator.EvaluateDiscountsAsync(promotionContext, products);
        }

        private async Task LoadProductAssociationsAsync(IEnumerable<Product> products)
        {
            var workContext = _workContextFactory();

            var allAssociations = products.SelectMany(x => x.Associations).ToList();

            var allProductAssociations = allAssociations.OfType<ProductAssociation>().ToList();
            var allCategoriesAssociations = allAssociations.OfType<CategoryAssociation>().ToList();

            if (allProductAssociations.Any())
            {
                var allAssociatedProducts = await GetProductsAsync(allProductAssociations.Select(x => x.ProductId).ToArray(), ItemResponseGroup.ItemInfo | ItemResponseGroup.ItemWithPrices | ItemResponseGroup.Seo);
                foreach (var productAssociation in allProductAssociations)
                {
                    productAssociation.Product = allAssociatedProducts.FirstOrDefault(x => x.Id == productAssociation.ProductId);
                }
            }

            if (allCategoriesAssociations.Any())
            {
                var allAssociatedCategories = await GetCategoriesAsync(allCategoriesAssociations.Select(x => x.CategoryId).ToArray(), CategoryResponseGroup.Info | CategoryResponseGroup.WithSeo | CategoryResponseGroup.WithOutlines | CategoryResponseGroup.WithImages);

                foreach (var categoryAssociation in allCategoriesAssociations)
                {
                    categoryAssociation.Category = allAssociatedCategories.FirstOrDefault(x => x.Id == categoryAssociation.CategoryId);
                    if (categoryAssociation.Category != null && categoryAssociation.Category.Products == null)
                    {
                        categoryAssociation.Category.Products = new MutablePagedList<Product>((pageNumber, pageSize, sortInfos) =>
                       {
                           var criteria = new CatalogSearchCriteria
                           {
                               PageNumber = pageNumber,
                               PageSize = pageSize,
                               CatalogId = workContext.CurrentStore.Catalog,
                               CategoryId = categoryAssociation.CategoryId,
                               SearchInChildren = true,
                               ResponseGroup = CatalogSearchResponseGroup.WithProducts,
                           };
                           if(!sortInfos.IsNullOrEmpty())
                           {
                               criteria.SortBy = SortInfo.ToString(sortInfos);
                           }
                           var searchResult = SearchProducts(criteria);
                           return searchResult.Products;
                       });
                    }
                }
            }

        }

        private async Task LoadProductInventoriesAsync(List<Product> products)
        {
            var inventories = await _inventoryModuleApi.InventoryModuleGetProductsInventoriesAsync(products.Select(x => x.Id).ToList());
            foreach (var item in products)
            {
                item.Inventory = inventories.Where(x => x.ProductId == item.Id).Select(x => x.ToWebModel()).FirstOrDefault();
            }
        }

        private void LoadProductInventories(List<Product> products)
        {
            var inventories = _inventoryModuleApi.InventoryModuleGetProductsInventories(products.Select(x => x.Id).ToList());
            foreach (var item in products)
            {
                item.Inventory = inventories.Where(x => x.ProductId == item.Id).Select(x => x.ToWebModel()).FirstOrDefault();
            }
        }
    }
}
