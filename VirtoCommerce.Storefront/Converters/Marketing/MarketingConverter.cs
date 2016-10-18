﻿using System.Collections.Generic;
using System.Linq;
using Microsoft.Practices.ServiceLocation;
using Omu.ValueInjecter;
using VirtoCommerce.Storefront.Common;
using VirtoCommerce.Storefront.Model;
using VirtoCommerce.Storefront.Model.Catalog;
using VirtoCommerce.Storefront.Model.Common;
using VirtoCommerce.Storefront.Model.Marketing;
using VirtoCommerce.Storefront.Model.Marketing.Factories;
using coreDto = VirtoCommerce.Storefront.AutoRestClients.CoreModuleApi.Models;
using marketingDto = VirtoCommerce.Storefront.AutoRestClients.MarketingModuleApi.Models;
namespace VirtoCommerce.Storefront.Converters
{

    public static class MarketingConverterExtension
    {
        public static MarketingConverter MarketingConverterInstance
        {
            get
            {
                return ServiceLocator.Current.GetInstance<MarketingConverter>();
            }
        }

        public static PromotionEvaluationContext ToPromotionEvaluationContext(this WorkContext workContext, IEnumerable<Product> products = null)
        {
            return MarketingConverterInstance.ToPromotionEvaluationContext(workContext, products);
        }

        public static marketingDto.PromotionEvaluationContext ToPromotionEvaluationContextDto(this PromotionEvaluationContext promoEvaluationContext)
        {
            return MarketingConverterInstance.ToPromotionEvaluationContextDto(promoEvaluationContext);
        }

        public static Promotion ToWebModel(this marketingDto.Promotion promotionDto)
        {
            return MarketingConverterInstance.ToPromotion(promotionDto);
        }

        public static marketingDto.ProductPromoEntry ToProductPromoEntryDto(this PromotionProductEntry promoProductEntry)
        {
            return MarketingConverterInstance.ToProductPromoEntryDto(promoProductEntry);
        }

        public static PromotionReward ToPromotionReward(this marketingDto.PromotionReward rewardDto, Currency currency)
        {
            return MarketingConverterInstance.ToPromotionReward(rewardDto, currency);
        }

        public static DynamicContentItem ToDynamicContentItem(this marketingDto.DynamicContentItem contentItemDto)
        {
            return MarketingConverterInstance.ToDynamicContentItem(contentItemDto);
        }

        public static DynamicProperty ToDynamicProperty(this marketingDto.DynamicObjectProperty propertyDto)
        {
            return MarketingConverterInstance.ToDynamicProperty(propertyDto);
        }

        public static marketingDto.DynamicObjectProperty ToMarketingDynamicPropertyDto(this DynamicProperty property)
        {
            return MarketingConverterInstance.ToMarketingDynamicPropertyDto(property);
        }
    }

    public class MarketingConverter
    {
        public virtual DynamicProperty ToDynamicProperty(marketingDto.DynamicObjectProperty propertyDto)
        {
            return propertyDto.JsonConvert<coreDto.DynamicObjectProperty>().ToDynamicProperty();
        }

        public virtual marketingDto.DynamicObjectProperty ToMarketingDynamicPropertyDto(DynamicProperty property)
        {
            return property.ToDynamicPropertyDto().JsonConvert<marketingDto.DynamicObjectProperty>();
        }

        public virtual DynamicContentItem ToDynamicContentItem(marketingDto.DynamicContentItem contentItemDto)
        {
            var result = ServiceLocator.Current.GetInstance<MarketingFactory>().CreateDynamicContentItem();

            result.InjectFrom<NullableAndEnumValueInjecter>(contentItemDto);

            if (contentItemDto.DynamicProperties != null)
            {
                result.DynamicProperties = contentItemDto.DynamicProperties.Select(ToDynamicProperty).ToList();
            }

            return result;
        }

        public virtual PromotionReward ToPromotionReward(marketingDto.PromotionReward serviceModel, Currency currency)
        {
            var result = ServiceLocator.Current.GetInstance<MarketingFactory>().CreatePromotionReward();

            result.InjectFrom<NullableAndEnumValueInjecter>(serviceModel);

            result.Amount = (decimal)(serviceModel.Amount ?? 0);
            result.AmountType = EnumUtility.SafeParse(serviceModel.AmountType, AmountType.Absolute);
            result.CouponAmount = new Money(serviceModel.CouponAmount ?? 0, currency);
            result.CouponMinOrderAmount = new Money(serviceModel.CouponMinOrderAmount ?? 0, currency);
            result.Promotion = serviceModel.Promotion.ToWebModel();
            result.RewardType = EnumUtility.SafeParse(serviceModel.RewardType, PromotionRewardType.CatalogItemAmountReward);
            result.ShippingMethodCode = serviceModel.ShippingMethod;

            return result;
        }

        public marketingDto.ProductPromoEntry ToProductPromoEntryDto(PromotionProductEntry promoProductEntry)
        {
            var serviceModel = new marketingDto.ProductPromoEntry();

            serviceModel.InjectFrom<NullableAndEnumValueInjecter>(promoProductEntry);

            serviceModel.Discount = promoProductEntry.Discount != null ? (double?)promoProductEntry.Discount.Amount : null;
            serviceModel.Price = promoProductEntry.Price != null ? (double?)promoProductEntry.Price.Amount : null;
            serviceModel.Variations = promoProductEntry.Variations != null ? promoProductEntry.Variations.Select(ToProductPromoEntryDto).ToList() : null;

            return serviceModel;
        }

        public virtual Promotion ToPromotion(marketingDto.Promotion promotionDto)
        {
            var result = ServiceLocator.Current.GetInstance<MarketingFactory>().CreatePromotion();

            result.InjectFrom<NullableAndEnumValueInjecter>(promotionDto);

            result.Coupons = promotionDto.Coupons;

            return result;
        }

        public virtual PromotionEvaluationContext ToPromotionEvaluationContext(WorkContext workContext, IEnumerable<Product> products = null)
        {
            var result = ServiceLocator.Current.GetInstance<MarketingFactory>().CreatePromotionEvaluationContext();
            result.CartPromoEntries = workContext.CurrentCart.Items.Select(x => x.ToPromotionItem()).ToList();
            result.CartTotal = workContext.CurrentCart.Total;
            result.Coupon = workContext.CurrentCart.Coupon != null ? workContext.CurrentCart.Coupon.Code : null;
            result.Currency = workContext.CurrentCurrency;
            result.CustomerId = workContext.CurrentCustomer.Id;
            result.IsRegisteredUser = workContext.CurrentCustomer.IsRegisteredUser;
            result.Language = workContext.CurrentLanguage;
            result.StoreId = workContext.CurrentStore.Id;

            //Set cart lineitems as default promo items
            result.PromoEntries = result.CartPromoEntries;

            if (workContext.CurrentProduct != null)
            {
                result.PromoEntry = workContext.CurrentProduct.ToPromotionItem();
            }

            if (products != null)
            {
                result.PromoEntries = products.Select(x => x.ToPromotionItem()).ToList();
            }

            return result;
        }

        public virtual marketingDto.PromotionEvaluationContext ToPromotionEvaluationContextDto(PromotionEvaluationContext promoEvalContext)
        {
            var result = new marketingDto.PromotionEvaluationContext();

            result.InjectFrom<NullableAndEnumValueInjecter>(promoEvalContext);

            result.CartPromoEntries = promoEvalContext.CartPromoEntries.Select(ToProductPromoEntryDto).ToList();
            result.CartTotal = promoEvalContext.CartTotal != null ? (double?)promoEvalContext.CartTotal.Amount : null;
            result.Currency = promoEvalContext.Currency != null ? promoEvalContext.Currency.Code : null;
            result.Language = promoEvalContext.Language != null ? promoEvalContext.Language.CultureName : null;
            result.PromoEntries = promoEvalContext.PromoEntries.Select(ToProductPromoEntryDto).ToList();
            result.PromoEntry = promoEvalContext.PromoEntry != null ? ToProductPromoEntryDto(promoEvalContext.PromoEntry) : null;
            result.ShipmentMethodPrice = promoEvalContext.ShipmentMethodPrice != null ? (double?)promoEvalContext.ShipmentMethodPrice.Amount : null;

            return result;
        }
    }
}
