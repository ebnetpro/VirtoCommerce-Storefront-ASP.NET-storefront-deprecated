using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Mvc;
using VirtoCommerce.Storefront.Common;
using VirtoCommerce.Storefront.Model;
using VirtoCommerce.Storefront.Model.Cart.Services;
using VirtoCommerce.Storefront.Model.Catalog;
using VirtoCommerce.Storefront.Model.Common;
using VirtoCommerce.Storefront.Model.Common.Exceptions;
using VirtoCommerce.Storefront.Model.Quote;
using VirtoCommerce.Storefront.Model.Quote.Services;
using VirtoCommerce.Storefront.Model.Services;

namespace VirtoCommerce.Storefront.Controllers.Api
{
    [HandleJsonError]
    public class ApiQuoteRequestController : StorefrontControllerBase
    {
        private readonly IQuoteRequestBuilder _quoteRequestBuilder;
        private readonly ICartBuilder _cartBuilder;
        private readonly ICatalogSearchService _catalogSearchService;

        public ApiQuoteRequestController(WorkContext workContext, IStorefrontUrlBuilder urlBuilder, ICartBuilder cartBuilder, IQuoteRequestBuilder quoteRequestBuilder, ICatalogSearchService catalogSearchService)
            : base(workContext, urlBuilder)
        {
            _quoteRequestBuilder = quoteRequestBuilder;
            _cartBuilder = cartBuilder;
            _catalogSearchService = catalogSearchService;
        }

        // GET: storefrontapi/quoterequests/{number}/itemscount
        [HttpGet]
        public async Task<ActionResult> GetItemsCount(string number)
        {
            await _quoteRequestBuilder.LoadQuoteRequestAsync(number, WorkContext.CurrentLanguage, WorkContext.AllCurrencies);

            var quoteRequest = _quoteRequestBuilder.QuoteRequest;
            EnsureQuoteRequestBelongsToCurrentCustomer(quoteRequest);

            return Json(new { Id = quoteRequest.Id, ItemsCount = quoteRequest.ItemsCount });
        }

        // GET: storefrontapi/quoterequests/{number}
        [HttpGet]
        public async Task<ActionResult> Get(string number)
        {
            var builder = await _quoteRequestBuilder.LoadQuoteRequestAsync(number, WorkContext.CurrentLanguage, WorkContext.AllCurrencies);
            var quoteRequest = builder.QuoteRequest;

            EnsureQuoteRequestBelongsToCurrentCustomer(quoteRequest);

            quoteRequest.Customer = WorkContext.CurrentCustomer;

            return Json(quoteRequest);
        }

        // GET: storefrontapi/quoterequest/current
        [HttpGet]
        public ActionResult GetCurrent()
        {
            EnsureQuoteRequestBelongsToCurrentCustomer(WorkContext.CurrentQuoteRequest);

            return Json(WorkContext.CurrentQuoteRequest);
        }

        // POST: storefrontapi/quoterequests/current/items?productId=...&quantity=...
        [HttpPost]
        public async Task<ActionResult> AddItem(string productId, int quantity)
        {
            EnsureQuoteRequestBelongsToCurrentCustomer(WorkContext.CurrentQuoteRequest);
            _quoteRequestBuilder.TakeQuoteRequest(WorkContext.CurrentQuoteRequest);

            using (await AsyncLock.GetLockByKey(GetAsyncLockQuoteKey(_quoteRequestBuilder.QuoteRequest.Id)).LockAsync())
            {
                var products = await _catalogSearchService.GetProductsAsync(new[] { productId }, ItemResponseGroup.ItemLarge);
                if (products != null && products.Any())
                {
                    _quoteRequestBuilder.AddItem(products.First(), quantity);
                    await _quoteRequestBuilder.SaveAsync();
                }
            }

            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }

        // DELETE: storefrontapi/quoterequest/{number}/items/{itemId}
        [HttpDelete]
        public async Task<ActionResult> RemoveItem(string number, string itemId)
        {
            await _quoteRequestBuilder.LoadQuoteRequestAsync(number, WorkContext.CurrentLanguage, WorkContext.AllCurrencies);

            using (await AsyncLock.GetLockByKey(GetAsyncLockQuoteKey(_quoteRequestBuilder.QuoteRequest.Id)).LockAsync())
            {
                _quoteRequestBuilder.RemoveItem(itemId);
                await _quoteRequestBuilder.SaveAsync();
            }
            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }

        // POST: storefrontapi/quoterequest/{number}/submit
        [HttpPost]
        public async Task<ActionResult> Submit(string number, QuoteRequestFormModel quoteForm)
        {
            await _quoteRequestBuilder.LoadQuoteRequestAsync(number, WorkContext.CurrentLanguage, WorkContext.AllCurrencies);

            EnsureQuoteRequestBelongsToCurrentCustomer(_quoteRequestBuilder.QuoteRequest);

            using (await AsyncLock.GetLockByKey(WorkContext.CurrentQuoteRequest.Id).LockAsync())
            {
                _quoteRequestBuilder.Update(quoteForm).Submit();
                await _quoteRequestBuilder.SaveAsync();
            }

            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }

        // POST: storefrontapi/quoterequest/{number}/reject
        [HttpPost]
        public async Task<ActionResult> Reject(string number)
        {
            await _quoteRequestBuilder.LoadQuoteRequestAsync(number, WorkContext.CurrentLanguage, WorkContext.AllCurrencies);

            EnsureQuoteRequestBelongsToCurrentCustomer(_quoteRequestBuilder.QuoteRequest);

            using (await AsyncLock.GetLockByKey(_quoteRequestBuilder.QuoteRequest.Id).LockAsync())
            {
                _quoteRequestBuilder.Reject();
                await _quoteRequestBuilder.SaveAsync();
            }
            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }

        // PUT: storefrontapi/quoterequest/{number}/update
        [HttpPut]
        public async Task<ActionResult> Update(string number, QuoteRequestFormModel quoteRequest)
        {
            await _quoteRequestBuilder.LoadQuoteRequestAsync(number, WorkContext.CurrentLanguage, WorkContext.AllCurrencies);

            EnsureQuoteRequestBelongsToCurrentCustomer(_quoteRequestBuilder.QuoteRequest);

            using (await AsyncLock.GetLockByKey(_quoteRequestBuilder.QuoteRequest.Id).LockAsync())
            {
                _quoteRequestBuilder.Update(quoteRequest);
                await _quoteRequestBuilder.SaveAsync();
            }

            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }

        // POST: storefrontapi/quoterequests/{number}/totals
        [HttpPost]
        public async Task<ActionResult> CalculateTotals(string number, QuoteRequestFormModel quoteRequest)
        {
            await _quoteRequestBuilder.LoadQuoteRequestAsync(number, WorkContext.CurrentLanguage, WorkContext.AllCurrencies);

            EnsureQuoteRequestBelongsToCurrentCustomer(_quoteRequestBuilder.QuoteRequest);

            //Apply user changes without saving
            _quoteRequestBuilder.Update(quoteRequest);
            await _quoteRequestBuilder.CalculateTotalsAsync();

            return Json(_quoteRequestBuilder.QuoteRequest.Totals);
        }

        // POST: storefrontapi/quoterequests/{number}/confirm
        [HttpPost]
        public async Task<ActionResult> Confirm(string number, QuoteRequestFormModel quoteRequest)
        {
            await _quoteRequestBuilder.LoadQuoteRequestAsync(number, WorkContext.CurrentLanguage, WorkContext.AllCurrencies);

            EnsureQuoteRequestBelongsToCurrentCustomer(_quoteRequestBuilder.QuoteRequest);

            _quoteRequestBuilder.Update(quoteRequest).Confirm();
            await _quoteRequestBuilder.SaveAsync();

            await _cartBuilder.TakeCartAsync(WorkContext.CurrentCart);
            await _cartBuilder.FillFromQuoteRequestAsync(_quoteRequestBuilder.QuoteRequest);
            await _cartBuilder.SaveAsync();

            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }

        private static string GetAsyncLockQuoteKey(string quoteId)
        {
            return "quote-request:" + quoteId;
        }

        private void EnsureQuoteRequestBelongsToCurrentCustomer(QuoteRequest quote)
        {
            if (WorkContext.CurrentCustomer.Id != quote.CustomerId)
            {
                throw new StorefrontException("Requested quote not belongs to user " + WorkContext.CurrentCustomer.UserName);
            }
        }
    }
}
