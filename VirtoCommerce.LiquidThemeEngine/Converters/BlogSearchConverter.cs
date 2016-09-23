﻿using System.Linq;
using Omu.ValueInjecter;
using PagedList;
using VirtoCommerce.LiquidThemeEngine.Objects;
using VirtoCommerce.Storefront.Model.Common;
using StorefrontModel = VirtoCommerce.Storefront.Model.StaticContent;

namespace VirtoCommerce.LiquidThemeEngine.Converters
{
    public static class BlogSearchConverter
    {
        public static BlogSearch ToShopifyModel(this StorefrontModel.BlogSearchCriteria blogSearchCriteria)
        {
            var retVal = new BlogSearch();

            retVal.InjectFrom<NullableAndEnumValueInjecter>(blogSearchCriteria);
       
            return retVal;
        }
    }
}