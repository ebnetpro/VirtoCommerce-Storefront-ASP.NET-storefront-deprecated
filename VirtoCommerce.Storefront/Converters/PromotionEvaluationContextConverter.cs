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
    public class PromotionEvaluationContextConverter
    {
        protected ProductConverter ProductConverter;

        public PromotionEvaluationContextConverter(ProductConverter productConverter)
        {
            ProductConverter = productConverter;
        }

        public virtual PromotionEvaluationContext ToPromotionEvaluationContext(ShoppingCart cart)
        {
            var promotionItems = cart.Items.Select(i => i.ToPromotionItem()).ToList();

            var retVal = new PromotionEvaluationContext
            {
                CartPromoEntries = promotionItems,
                CartTotal = cart.Total,
                Coupon = cart.Coupon != null ? cart.Coupon.Code : null,
                Currency = cart.Currency,
                CustomerId = cart.Customer.Id,
                IsRegisteredUser = cart.Customer.IsRegisteredUser,
                Language = cart.Language,
                PromoEntries = promotionItems,
                StoreId = cart.StoreId
            };

            return retVal;
        }

        public virtual PromotionEvaluationContext ToPromotionEvaluationContext(WorkContext workContext, IEnumerable<Product> products = null)
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
                retVal.PromoEntry = ProductConverter.ToPromotionItem(workContext.CurrentProduct);
            }

            if (products != null)
            {
                retVal.PromoEntries = products.Select(x => ProductConverter.ToPromotionItem(x)).ToList();
            }
            return retVal;
        }

        public virtual marketingModel.PromotionEvaluationContext ToServiceModel(PromotionEvaluationContext webModel)
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
