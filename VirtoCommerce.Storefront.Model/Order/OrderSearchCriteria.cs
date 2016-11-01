﻿using System.Collections.Specialized;
using VirtoCommerce.Storefront.Model.Common;

namespace VirtoCommerce.Storefront.Model.Order
{
    public class OrderSearchCriteria : PagedSearchCriteria
    {
        public static int DefaultPageSize { get; set; }

        public OrderSearchCriteria(NameValueCollection queryString)
            : base(queryString, DefaultPageSize)
        {
        }
    }
}
