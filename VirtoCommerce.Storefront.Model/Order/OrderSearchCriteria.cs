﻿using System.Collections.Specialized;
using VirtoCommerce.Storefront.Model.Common;

namespace VirtoCommerce.Storefront.Model.Order
{
    public class OrderSearchCriteria : PagedSearchCriteria
    {
        private static int _defaultPageSize = 20;

        public static int DefaultPageSize
        {
            get { return _defaultPageSize; }
            set { _defaultPageSize = value; }
        }

        public OrderSearchCriteria(NameValueCollection queryString)
            : base(queryString, DefaultPageSize)
        {
        }
    }
}
