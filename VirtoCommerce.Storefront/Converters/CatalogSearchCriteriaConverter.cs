﻿using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Storefront.Model;
using VirtoCommerce.Storefront.Model.Catalog;
using catalogModel = VirtoCommerce.Storefront.AutoRestClients.CatalogModuleApi.Models;
using searchModel = VirtoCommerce.Storefront.AutoRestClients.SearchApiModuleApi.Models;

namespace VirtoCommerce.Storefront.Converters
{
    public static class CatalogSearchCriteriaConverter
    {
        public static catalogModel.SearchCriteria ToCatalogApiModel(this CatalogSearchCriteria criteria, WorkContext workContext)
        {
            var result = new catalogModel.SearchCriteria
            {
                StoreId = workContext.CurrentStore.Id,
                Keyword = criteria.Keyword,
                ResponseGroup = criteria.ResponseGroup.ToString(),
                SearchInChildren = criteria.SearchInChildren,
                CategoryId = criteria.CategoryId,
                CatalogId = criteria.CatalogId,
                VendorId = criteria.VendorId,
                Currency = criteria.Currency == null ? workContext.CurrentCurrency.Code : criteria.Currency.Code,
                HideDirectLinkedCategories = true,
                Terms = criteria.Terms.ToStrings(),
                PricelistIds = workContext.CurrentPricelists.Where(p => p.Currency == workContext.CurrentCurrency.Code).Select(p => p.Id).ToList(),
                Skip = criteria.Start,
                Take = criteria.PageSize,
                Sort = criteria.SortBy
            };

            if (criteria.VendorIds != null)
            {
                result.VendorIds = criteria.VendorIds.ToList();
            }

            return result;
        }

        public static searchModel.ProductSearch ToSearchApiModel(this ProductSearchCriteria criteria, WorkContext workContext)
        {
            var result = new searchModel.ProductSearch()
            {
                SearchPhrase = criteria.Keyword,
                Outline = criteria.Outline,
                Currency = criteria.Currency == null ? workContext.CurrentCurrency.Code : criteria.Currency.Code,
                Terms = criteria.Terms.ToStrings(),
                PriceLists = workContext.CurrentPricelists.Where(p => p.Currency == workContext.CurrentCurrency.Code).Select(p => p.Id).ToList(),
                Skip = criteria.Start,
                Take = criteria.PageSize                
            };

            // Add vendor id to terms
            if (!string.IsNullOrEmpty(criteria.VendorId))
            {
                if (result.Terms == null)
                {
                    result.Terms = new List<string>();
                }

                result.Terms.Add(string.Format("vendor:{0}", criteria.VendorId));
            }

            if (criteria.SortBy != null)
                result.Sort = new string[] { criteria.SortBy };

            return result;
        }

        public static searchModel.CategorySearch ToSearchApiModel(this CategorySearchCriteria criteria, WorkContext workContext)
        {
            
            var result = new searchModel.CategorySearch()
            {
                Skip = criteria.Start,
                Take = criteria.PageSize,
                Outline = criteria.Outline
            };

            if (criteria.SortBy != null)
                result.Sort = new string[] { criteria.SortBy };

            return result;
        }
    }
}
