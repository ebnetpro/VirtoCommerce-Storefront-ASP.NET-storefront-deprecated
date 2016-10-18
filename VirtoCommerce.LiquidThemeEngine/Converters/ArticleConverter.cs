﻿using System.Linq;
using Omu.ValueInjecter;
using VirtoCommerce.LiquidThemeEngine.Objects;
using VirtoCommerce.Storefront.Model.Common;
using StorefrontModel = VirtoCommerce.Storefront.Model.StaticContent;
using System.Collections.Generic;
using VirtoCommerce.LiquidThemeEngine.Filters;
using Microsoft.Practices.ServiceLocation;
using VirtoCommerce.LiquidThemeEngine.Objects.Factories;

namespace VirtoCommerce.LiquidThemeEngine.Converters
{
    public static class ArticleConverter
    {
        public static Article ToShopifyModel(this StorefrontModel.BlogArticle article)
        {
            var converter = ServiceLocator.Current.GetInstance<ShopifyModelConverter>();
            return converter.ToLiquidArticle(article);
        }
    }

    public partial class ShopifyModelConverter
    {
       public virtual Article ToLiquidArticle(StorefrontModel.BlogArticle article)
        {
            var factory = ServiceLocator.Current.GetInstance<ShopifyModelFactory>();
            var retVal = factory.CreateArticle();

            retVal.InjectFrom<NullableAndEnumValueInjecter>(article);
            retVal.Handle = article.Url;
            retVal.CreatedAt = article.CreatedDate;
            retVal.PublishedAt = article.PublishedDate ?? article.CreatedDate;
            retVal.Tags = article.Tags != null ? article.Tags.OrderBy(t => t).Select(t => t.Handelize()).ToArray() : null;
            retVal.Comments = new MutablePagedList<Comment>(new List<Comment>());
            if (article.Category != null)
            {
                retVal.Category = article.Category.Handelize();
            }
            return retVal;
        }
    }
}