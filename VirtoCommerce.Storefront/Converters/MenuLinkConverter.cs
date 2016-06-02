﻿using System.Linq;
using Omu.ValueInjecter;
using VirtoCommerce.Storefront.Model;

namespace VirtoCommerce.Storefront.Converters
{
    public static class MenuLinkConverter
    {
        public static MenuLinkList ToWebModel(this ContentModule.Client.Model.MenuLinkList serviceModel)
        {
            var webModel = new MenuLinkList();

            webModel.InjectFrom(serviceModel);

            webModel.Language = string.IsNullOrEmpty(serviceModel.Language) ? Language.InvariantLanguage : new Language(serviceModel.Language);

            if (serviceModel.MenuLinks != null)
            {
                webModel.MenuLinks = serviceModel.MenuLinks.Select(ml => ml.ToWebModel()).ToList();
            }

            return webModel;
        }

        public static MenuLink ToWebModel(this ContentModule.Client.Model.MenuLink serviceModel)
        {
            var webModel = new MenuLink();

            if (serviceModel.AssociatedObjectType != null)
            {
                if ("product" == serviceModel.AssociatedObjectType.ToLowerInvariant())
                {
                    webModel = new ProductMenuLink();
                }
                else if ("category" == serviceModel.AssociatedObjectType.ToLowerInvariant())
                {
                    webModel = new CategoryMenuLink();
                }
            }

            webModel.InjectFrom(serviceModel);

            return webModel;
        }
    }
}
