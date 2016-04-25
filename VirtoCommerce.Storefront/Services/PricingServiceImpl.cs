﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using VirtoCommerce.Client.Api;
using VirtoCommerce.Client.Model;
using VirtoCommerce.Storefront.Model;
using VirtoCommerce.Storefront.Model.Catalog;
using VirtoCommerce.Storefront.Model.Pricing.Services;
using VirtoCommerce.Storefront.Converters;
using VirtoCommerce.Storefront.Model.Common;

namespace VirtoCommerce.Storefront.Services
{
    public class PricingServiceImpl : IPricingService
    {
        private readonly IPricingModuleApi _pricingApi;
        private readonly Func<WorkContext> _workContextFactory;
        public PricingServiceImpl(Func<WorkContext> workContextFactory, IPricingModuleApi pricingApi)
        {
            _pricingApi = pricingApi;
            _workContextFactory = workContextFactory;
        }

        #region IPricingService Members
        public async Task EvaluateProductPricesAsync(IEnumerable<Product> products)
        {
            var workContext = _workContextFactory();
            //Evaluate products prices
            var evalContext = products.ToServiceModel(workContext);

            var pricesResponse = await _pricingApi.PricingModuleEvaluatePricesAsync(evalContext);
            ApplyProductPricesInternal(products, pricesResponse);
        }

        public void EvaluateProductPrices(IEnumerable<Product> products)
        {
            var workContext = _workContextFactory();
            //Evaluate products prices
            var evalContext = products.ToServiceModel(workContext);

            var pricesResponse = _pricingApi.PricingModuleEvaluatePrices(evalContext);
            ApplyProductPricesInternal(products, pricesResponse);
        }

        #endregion
        private void ApplyProductPricesInternal(IEnumerable<Product> products, IEnumerable<VirtoCommercePricingModuleWebModelPrice> prices)
        {
            var workContext = _workContextFactory();
            foreach (var product in products)
            {
                var tierPrices = new List<TierPrice>();
                var productPricesGroupedByCurrency = prices.Where(p => p.ProductId == product.Id).GroupBy(p => p.Currency);
                foreach (var productPricesCurrencyGroup in productPricesGroupedByCurrency)
                {
                    var productPrice = productPricesCurrencyGroup.FirstOrDefault(p => !p.MinQuantity.HasValue);
                    if (productPrice == null)
                    {
                        productPrice = productPricesCurrencyGroup.OrderBy(p => p.MinQuantity.Value).FirstOrDefault();
                    }
                    if (productPrice != null)
                    {
                        product.Prices.Add(productPrice.ToWebModel(workContext.AllCurrencies, workContext.CurrentLanguage));
                    }

                    var productPricesForCurrentCurrency = productPricesCurrencyGroup.Where(p => p.Currency == workContext.CurrentCurrency.Code).OrderBy(p => p.MinQuantity);
                    tierPrices.AddRange(productPricesForCurrentCurrency.Select(p => p.ToTierPrice(workContext.CurrentCurrency)));
                }

                foreach (var storeCurrency in workContext.CurrentStore.Currencies)
                {
                    var price = product.Prices.FirstOrDefault(x => x.Currency == storeCurrency);
                    if (price == null)
                    {
                        price = new ProductPrice(storeCurrency);
                        if (product.Prices.Any())
                        {
                            price = product.Prices.First().ConvertTo(storeCurrency);
                        }
                        product.Prices.Add(price);
                    }
                }

                product.Currency = workContext.CurrentCurrency;

                product.Price = product.Prices.FirstOrDefault(p => p.Currency == workContext.CurrentCurrency);
                product.Price.TierPrices = tierPrices;
            }

            //var alreadyDefinedProductsPriceGroups = prices.Select(x => x.ToWebModel(workContext.AllCurrencies, workContext.CurrentLanguage)).GroupBy(x => x.ProductId);
            //foreach (var product in products)
            //{
            //    var productPricesGroup = alreadyDefinedProductsPriceGroups.FirstOrDefault(x => x.Key == product.Id);
            //    if (productPricesGroup != null)
            //    {
            //        //Get first price for each currency
            //        product.Prices = productPricesGroup.GroupBy(x => x.Currency).Select(x => x.FirstOrDefault()).Where(x => x != null).ToList();
            //    }
            //    //Need add product price for all store currencies (even if not returned from api need make it by currency exchange convertation)
            //    foreach (var storeCurrency in workContext.CurrentStore.Currencies)
            //    {
            //        var price = product.Prices.FirstOrDefault(x => x.Currency == storeCurrency);
            //        if (price == null)
            //        {
            //            price = new ProductPrice(storeCurrency);
            //            if (product.Prices.Any())
            //            {
            //                price = product.Prices.First().ConvertTo(storeCurrency);
            //            }
            //            product.Prices.Add(price);
            //        }
            //    }
            //    product.Currency = workContext.CurrentCurrency;
            //    product.Price = product.Prices.FirstOrDefault(x => x.Currency.Equals(workContext.CurrentCurrency));
            //}
        }
    }
}