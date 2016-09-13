﻿using System.Collections.Generic;
using System.Linq;
using Omu.ValueInjecter;
using VirtoCommerce.Storefront.Model;
using VirtoCommerce.Storefront.Model.Cart;
using VirtoCommerce.Storefront.Model.Catalog;
using VirtoCommerce.Storefront.Model.Common;
using VirtoCommerce.Storefront.Model.Marketing;
using marketingModel = VirtoCommerce.Storefront.AutoRestClients.MarketingModuleApi.Models;

namespace VirtoCommerce.Storefront.Converters
{
    public static class PromotionEvaluationContextConverter
    {
        
        public static PromotionEvaluationContext ToPromotionEvaluationContext(this WorkContext workContext, IEnumerable<Product> products = null)
        {
            var retVal = new PromotionEvaluationContext
            {
                CartPromoEntries = workContext.CurrentCart.Items.Select(x => x.ToPromotionItem()).ToList(),
                CartTotal = workContext.CurrentCart.Total,
                Coupon = workContext.CurrentCart.Coupon != null ? workContext.CurrentCart.Coupon.Code : null,
                Currency = workContext.CurrentCurrency,
                CustomerId = workContext.CurrentCustomer.Id,
                IsRegisteredUser = workContext.CurrentCustomer.IsRegisteredUser,
                Language = workContext.CurrentLanguage,
                StoreId = workContext.CurrentStore.Id
            };
            //Set cart lineitems as default promo items
            retVal.PromoEntries = retVal.CartPromoEntries;
            if (workContext.CurrentProduct != null)
            {
                retVal.PromoEntry = workContext.CurrentProduct.ToPromotionItem();
            }

            if (products != null)
            {
                retVal.PromoEntries = products.Select(x => x.ToPromotionItem()).ToList();
            }
            return retVal;
        }

        public static marketingModel.PromotionEvaluationContext ToServiceModel(this PromotionEvaluationContext webModel)
        {
            var serviceModel = new marketingModel.PromotionEvaluationContext();

            serviceModel.InjectFrom<NullableAndEnumValueInjecter>(webModel);

            serviceModel.CartPromoEntries = webModel.CartPromoEntries.Select(pe => pe.ToServiceModel()).ToList();
            serviceModel.CartTotal = webModel.CartTotal != null ? (double?)webModel.CartTotal.Amount : null;
            serviceModel.Currency = webModel.Currency != null ? webModel.Currency.Code : null;
            serviceModel.Language = webModel.Language != null ? webModel.Language.CultureName : null;
            serviceModel.PromoEntries = webModel.PromoEntries.Select(pe => pe.ToServiceModel()).ToList();
            serviceModel.PromoEntry = webModel.PromoEntry != null ? webModel.PromoEntry.ToServiceModel() : null;
            serviceModel.ShipmentMethodPrice = webModel.ShipmentMethodPrice != null ? (double?)webModel.ShipmentMethodPrice.Amount : null;

            return serviceModel;
        }
    }
}
