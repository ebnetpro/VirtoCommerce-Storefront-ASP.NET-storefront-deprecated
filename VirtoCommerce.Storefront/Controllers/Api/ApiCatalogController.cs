using System.Threading.Tasks;
using System.Web.Mvc;
using VirtoCommerce.LiquidThemeEngine.Filters;
using VirtoCommerce.Storefront.Common;
using VirtoCommerce.Storefront.Model;
using VirtoCommerce.Storefront.Model.Catalog;
using VirtoCommerce.Storefront.Model.Common;
using VirtoCommerce.Storefront.Model.Services;

namespace VirtoCommerce.Storefront.Controllers.Api
{
    [HandleJsonError]
    public class ApiCatalogController : StorefrontControllerBase
    {
        private readonly ICatalogSearchService _catalogSearchService;
        public ApiCatalogController(WorkContext workContext, IStorefrontUrlBuilder urlBuilder, ICatalogSearchService catalogSearchService)
            : base(workContext, urlBuilder)
        {
            _catalogSearchService = catalogSearchService;
        }

        // storefrontapi/catalog/search
        [HttpPost]
        public async Task<ActionResult> SearchProducts(ProductSearchCriteria searchCriteria)
        {
            var retVal = await _catalogSearchService.SearchProductsAsync(searchCriteria);
            foreach (var product in retVal.Products)
            {
                product.Url = base.UrlBuilder.ToAppAbsolute(product.Url);
            }
            return Json(new
            {
                Products = retVal.Products,
                Aggregations = retVal.Aggregations,
                MetaData = retVal.Products.GetMetaData()
            });
        }

        // storefrontapi/products?productIds=...&respGroup=...
        [HttpGet]
        public async Task<ActionResult> GetProductsByIds(string[] productIds, ItemResponseGroup respGroup = ItemResponseGroup.ItemLarge)
        {
            var retVal = await _catalogSearchService.GetProductsAsync(productIds, respGroup);
            return Json(retVal);
        }

        // storefrontapi/categories/search
        [HttpPost]
        public async Task<ActionResult> SearchCategories(CategorySearchCriteria searchCriteria)
        {
            var retVal = await _catalogSearchService.SearchCategoriesAsync(searchCriteria);
            foreach (var category in retVal)
            {
                category.Url = base.UrlBuilder.ToAppAbsolute(category.Url);
            }
            return Json(new
            {
                Categories = retVal,
                MetaData = retVal.GetMetaData()
            });
        }

        // GET: storefrontapi/categories
        [HttpGet]
        public async Task<ActionResult> GetCategoriesByIds(string[] categoryIds, CategoryResponseGroup respGroup = CategoryResponseGroup.Full)
        {
            var retVal = await _catalogSearchService.GetCategoriesAsync(categoryIds, respGroup);
            return Json(retVal);
        }

    }
}