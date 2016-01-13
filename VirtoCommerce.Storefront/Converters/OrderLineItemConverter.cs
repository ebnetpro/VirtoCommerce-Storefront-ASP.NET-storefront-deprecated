﻿using Omu.ValueInjecter;
using System.Linq;
using VirtoCommerce.Client.Model;
using VirtoCommerce.Storefront.Model.Common;
using VirtoCommerce.Storefront.Model.Order;
using System.Collections.Generic;

namespace VirtoCommerce.Storefront.Converters
{
    public static class OrderLineItemConverter
    {
        public static LineItem ToWebModel(this VirtoCommerceOrderModuleWebModelLineItem lineItem, IEnumerable<Currency> availCurrencies)
        {
            var webModel = new LineItem();

            var currency = availCurrencies.FirstOrDefault(x => x.IsHasSameCode(lineItem.Currency)) ?? new Currency(lineItem.Currency, 1);
         
            webModel.InjectFrom(lineItem);

            webModel.Currency = currency;
            webModel.DiscountAmount = new Money(lineItem.DiscountAmount ?? 0, currency);

            if (lineItem.DynamicProperties != null)
            {
                webModel.DynamicProperties = lineItem.DynamicProperties.Select(dp => dp.ToWebModel()).ToList();
            }

            webModel.Price = new Money(lineItem.Price ?? 0, currency);
            webModel.Tax = new Money(lineItem.Tax ?? 0, currency);

            if (lineItem.TaxDetails != null)
            {
                webModel.TaxDetails = lineItem.TaxDetails.Select(td => td.ToWebModel()).ToList();
            }

            return webModel;
        }
    }
}