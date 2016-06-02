﻿using System.Linq;
using Omu.ValueInjecter;
using VirtoCommerce.Storefront.Model;
using VirtoCommerce.Storefront.Model.Cart;
using VirtoCommerce.Storefront.Model.Catalog;
using VirtoCommerce.Storefront.Model.Common;
using VirtoCommerce.Storefront.Model.Marketing;

namespace VirtoCommerce.Storefront.Converters
{
    public static class CartLineItemConverter
    {
        public static LineItem ToLineItem(this Product product, Language language, int quantity)
        {
            var lineItemWebModel = new LineItem(product.Price.Currency, language);

            lineItemWebModel.InjectFrom<NullableAndEnumValueInjecter>(product);

            lineItemWebModel.ImageUrl = product.PrimaryImage != null ? product.PrimaryImage.Url : null;
            lineItemWebModel.ListPrice = product.Price.ListPrice;
            lineItemWebModel.ListPriceWithTax = product.Price.ListPriceWithTax;
            lineItemWebModel.SalePrice = product.Price.GetTierPrice(quantity).Price;
            lineItemWebModel.SalePriceWithTax = product.Price.GetTierPrice(quantity).PriceWithTax;
            lineItemWebModel.ProductId = product.Id;
            lineItemWebModel.Quantity = quantity;

            lineItemWebModel.ThumbnailImageUrl = product.PrimaryImage != null ? product.PrimaryImage.Url : null;

            return lineItemWebModel;
        }

        public static LineItem ToWebModel(this CartModule.Client.Model.LineItem serviceModel, Currency currency, Language language)
        {

            var webModel = new LineItem(currency, language);

            webModel.InjectFrom<NullableAndEnumValueInjecter>(serviceModel);

            if (serviceModel.TaxDetails != null)
            {
                webModel.TaxDetails = serviceModel.TaxDetails.Select(td => td.ToWebModel(currency)).ToList();
            }

            if (serviceModel.DynamicProperties != null)
            {
                webModel.DynamicProperties = serviceModel.DynamicProperties.Select(dp => dp.ToWebModel()).ToList();
            }

            if (!serviceModel.Discounts.IsNullOrEmpty())
            {
                webModel.Discounts.AddRange(serviceModel.Discounts.Select(x => x.ToWebModel(new[] { currency }, language)));
            }
            webModel.IsGift = serviceModel.IsGift == true;
            webModel.IsReccuring = serviceModel.IsReccuring == true;
            webModel.ListPrice = new Money(serviceModel.ListPrice ?? 0, currency);
            webModel.RequiredShipping = serviceModel.RequiredShipping == true;
            webModel.SalePrice = new Money(serviceModel.SalePrice ?? 0, currency);
            webModel.TaxIncluded = serviceModel.TaxIncluded == true;
            webModel.TaxTotal = new Money(serviceModel.TaxTotal ?? 0, currency);
            webModel.Weight = (decimal?)serviceModel.Weight;
            webModel.Width = (decimal?)serviceModel.Width;
            webModel.Height = (decimal?)serviceModel.Height;
            webModel.Length = (decimal?)serviceModel.Length;
            webModel.ValidationType = EnumUtility.SafeParse(serviceModel.ValidationType, ValidationType.PriceAndQuantity);

            return webModel;
        }

        public static CartModule.Client.Model.LineItem ToServiceModel(this LineItem webModel)
        {
            var serviceModel = new CartModule.Client.Model.LineItem();

            serviceModel.InjectFrom<NullableAndEnumValueInjecter>(webModel);

            serviceModel.Currency = webModel.Currency.Code;
            serviceModel.Discounts = webModel.Discounts.Select(d => d.ToServiceModel()).ToList();
            serviceModel.DiscountTotal = (double)webModel.DiscountTotal.Amount;
            serviceModel.ExtendedPrice = (double)webModel.ExtendedPrice.Amount;

            serviceModel.ListPrice = (double)webModel.ListPrice.Amount;
            serviceModel.PlacedPrice = (double)webModel.PlacedPrice.Amount;
            serviceModel.SalePrice = (double)webModel.SalePrice.Amount;
            serviceModel.TaxDetails = webModel.TaxDetails.Select(td => td.ToCartApiModel()).ToList();
            serviceModel.DynamicProperties = webModel.DynamicProperties.Select(dp => dp.ToCartApiModel()).ToList();
            serviceModel.TaxTotal = (double)webModel.TaxTotal.Amount;
            serviceModel.VolumetricWeight = (double)(webModel.VolumetricWeight ?? 0);
            serviceModel.Weight = (double?)webModel.Weight;
            serviceModel.Width = (double?)webModel.Width;
            serviceModel.Height = (double?)webModel.Height;
            serviceModel.Length = (double?)webModel.Length;
            serviceModel.ValidationType = webModel.ValidationType.ToString();

            return serviceModel;
        }

        public static PromotionProductEntry ToPromotionItem(this LineItem lineItem)
        {
            var promoItem = new PromotionProductEntry();

            promoItem.InjectFrom(lineItem);

            promoItem.Discount = new Money(lineItem.DiscountTotal.Amount, lineItem.DiscountTotal.Currency);
            promoItem.Price = new Money(lineItem.PlacedPrice.Amount, lineItem.PlacedPrice.Currency);
            promoItem.Quantity = lineItem.Quantity;
            promoItem.Variations = null; // TODO

            return promoItem;
        }

        public static CartShipmentItem ToShipmentItem(this LineItem lineItem)
        {
            var shipmentItem = new CartShipmentItem
            {
                LineItem = lineItem,
                Quantity = lineItem.Quantity
            };
            return shipmentItem;
        }
    }
}
