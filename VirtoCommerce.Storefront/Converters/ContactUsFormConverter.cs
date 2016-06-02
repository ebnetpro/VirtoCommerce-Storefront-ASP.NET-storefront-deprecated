﻿using System.Linq;
using VirtoCommerce.Storefront.Model;
using VirtoCommerce.StoreModule.Client.Model;

namespace VirtoCommerce.Storefront.Converters
{
    public static class ContactUsFormConverter
    {
        public static SendDynamicNotificationRequest ToServiceModel(this ContactUsForm contactUsForm, WorkContext workContext)
        {
            var retVal = new SendDynamicNotificationRequest
            {
                Language = workContext.CurrentLanguage.CultureName,
                StoreId = workContext.CurrentStore.Id,
                Type = contactUsForm.FormType,
                Fields = contactUsForm.Contact.ToDictionary(x => x.Key, x => x.Value != null ? x.Value.ToString() : string.Empty)
            };
            return retVal;
        }
    }
}
