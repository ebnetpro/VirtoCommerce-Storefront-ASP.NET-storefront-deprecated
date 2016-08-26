﻿using System.Collections.Generic;
using System.Linq;
using Omu.ValueInjecter;
using VirtoCommerce.Storefront.Model;
using VirtoCommerce.Storefront.Model.Common;
using VirtoCommerce.Storefront.Model.Order;
using orderModel = VirtoCommerce.Storefront.AutoRestClients.OrdersModuleApi.Models;

namespace VirtoCommerce.Storefront.Converters
{
    public static class OrderLineItemConverter
    {
        public static LineItem ToWebModel(this orderModel.LineItem lineItem, IEnumerable<Currency> availCurrencies, Language language)
        {
            var webModel = new LineItem();

            var currency = availCurrencies.FirstOrDefault(x => x.Equals(lineItem.Currency)) ?? new Currency(language, lineItem.Currency);

            webModel.InjectFrom(lineItem);

            webModel.Currency = currency;
            webModel.DiscountAmount = new Money(lineItem.DiscountAmount ?? 0, currency);

            if (lineItem.DynamicProperties != null)
            {
                webModel.DynamicProperties = lineItem.DynamicProperties.Select(dp => dp.ToWebModel()).ToList();
            }

            webModel.BasePrice = new Money(lineItem.BasePrice ?? 0, currency);
            webModel.Price = new Money(lineItem.Price ?? 0, currency);
            webModel.Tax = new Money(lineItem.Tax ?? 0, currency);

            if (lineItem.TaxDetails != null)
            {
                webModel.TaxDetails = lineItem.TaxDetails.Select(td => td.ToWebModel(currency)).ToList();
            }

            return webModel;
        }
    }
}
