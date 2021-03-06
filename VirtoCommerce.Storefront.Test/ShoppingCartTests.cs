using System;
using Moq;
using VirtoCommerce.Storefront.Builders;
using VirtoCommerce.Storefront.Model;
using VirtoCommerce.Storefront.Model.Cart.Services;
using VirtoCommerce.Storefront.Model.Common;
using VirtoCommerce.Storefront.Model.Customer;
using VirtoCommerce.Storefront.Model.Inventory.Services;
using VirtoCommerce.Storefront.Services;
using Xunit;

namespace VirtoCommerce.Storefront.Test
{
    public class ShoppingCartTests : StorefrontTestBase
    {
        [Fact]
        [Trait("Category", "Integration")]
        public void CreateNewCart_AnonymousUser_CheckThatNewCartCreated()
        {
            var cartBuilder = GetCartBuilder();
            var workContext = GetTestWorkContext();
            var anonymousCustomer = new CustomerInfo
            {
                Id = Guid.NewGuid().ToString(),
                IsRegisteredUser = false
            };
            var task = cartBuilder.LoadOrCreateNewTransientCartAsync("default", workContext.CurrentStore, anonymousCustomer, workContext.CurrentLanguage, workContext.CurrentCurrency);
            task.Wait();
            Assert.True(cartBuilder.Cart.IsTransient());

            cartBuilder.SaveAsync().Wait();
            var cart = cartBuilder.Cart;
            Assert.False(cart.IsTransient());

            task = cartBuilder.LoadOrCreateNewTransientCartAsync("default", workContext.CurrentStore, anonymousCustomer, workContext.CurrentLanguage, workContext.CurrentCurrency);
            task.Wait();
            Assert.Equal(cart.Id, cartBuilder.Cart.Id);

            cartBuilder.RemoveCartAsync();
        }

        [Fact]
        public void ManageItemsInCart_AnonymousUser_CheckThatItemsAndTotalsChanged()
        {
        }

        [Fact]
        public void MergeAnonymousCart_RegisteredUser_CheckThatAllMerged()
        {
        }

        [Fact]
        public void SingleShipmentAndPaymentWithCouponCheckout_RegisteredUser_CheckOrderCreated()
        {
        }

        [Fact]
        public void MultipleShipmentAndPartialPaymentWithCouponCheckout_RegisteredUser_CheckOrderCreated()
        {
        }

        private ICartBuilder GetCartBuilder()
        {
            var catalogApi = GetCatalogApiClient();
            var cartApi = GetCartApiClient();
            var marketingApi = GetMarketingApiClient();
            var inventoryApi = GetInventoryApiClient();
            var pricingApi = GetPricingApiClient();
            var customerApi = GetCustomerApiClient();
            var orderApi = GetOrderApiClient();
            var quoteApi = GetQuoteApiClient();
            var storeApi = GetStoreApiClient();
            var subscriptionsApi = GetSubscriptionsApiClient();
            var coreApi = GetCoreApiClient();

            var cacheManager = new Mock<ILocalCacheManager>().Object;
            var inventoryService = new Mock<IInventoryService>().Object;
            var workContextFactory = new Func<WorkContext>(GetTestWorkContext);
            var promotionEvaluator = new PromotionEvaluator(marketingApi);
            var taxEvaluator = new TaxEvaluator(coreApi);

            var pricingService = new PricingServiceImpl(pricingApi, null, promotionEvaluator, inventoryService);
            var customerService = new CustomerServiceImpl(workContextFactory, customerApi, orderApi, quoteApi, storeApi, null, cacheManager);
            var catalogSearchService = new CatalogSearchServiceImpl(workContextFactory, catalogApi, inventoryApi, pricingService, customerService, null, null, inventoryService);

            var retVal = new CartBuilder(workContextFactory, cartApi, catalogSearchService, cacheManager, promotionEvaluator, taxEvaluator, subscriptionsApi, null);
            return retVal;
        }
    }
}
