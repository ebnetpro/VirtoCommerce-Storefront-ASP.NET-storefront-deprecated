using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using VirtoCommerce.Storefront.Model.Common;
using VirtoCommerce.Storefront.Model.Marketing;
using VirtoCommerce.Storefront.Model.Subscriptions;

namespace VirtoCommerce.Storefront.Model.Catalog
{
    public partial class Product : Entity, IDiscountable, ITaxable
    {
        public Product()
        {
            Properties = new List<CatalogProperty>();
            VariationProperties = new List<CatalogProperty>();
            Prices = new List<ProductPrice>();
            Assets = new List<Asset>();
            Variations = new List<Product>();
            Images = new List<Image>();
            Descriptions = new List<EditorialReview>();
            Discounts = new List<Discount>();
            Associations = new List<Association>();
            TaxDetails = new List<TaxDetail>();
        }

        public Product(Currency currency, Language language)
            : this()
        {
            Currency = currency;
            Price = new ProductPrice(currency);
        }

        /// <summary>
        /// Manufacturer part number for this product
        /// </summary>
        public string ManufacturerPartNumber { get; set; }

        /// <summary>
        /// Global trade item number
        /// </summary>
        public string Gtin { get; set; }

        /// <summary>
        /// Product code
        /// </summary>
        public string Sku { get; set; }

        /// <summary>
        /// Name of this product
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Product catalog id
        /// </summary>
        public string CatalogId { get; set; }

        /// <summary>
        /// Category id of this product
        /// </summary>
        public string CategoryId { get; set; }

        /// <summary>
        /// All parent categories ids concatenated with "/". E.g. (1/21/344)
        /// </summary>
        public string Outline { get; set; }

        /// <summary>
        /// Date of last indexing of product, if null - product never was indexed
        /// </summary>
        public DateTime? IndexingDate { get; set; }

        /// <summary>
        /// Titular item id for a variation
        /// </summary>
        public string TitularItemId { get; set; }

        /// <summary>
        /// Indicating whether this product is buyable
        /// </summary>
        public bool IsBuyable { get; set; }

        /// <summary>
        /// Indicating whether this product is active
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Indicating whether this product is instock
        /// </summary>
        public bool IsInStock { get; set; }

        /// <summary>
        /// Indicating whether this product inventory is tracked
        /// </summary>
        public bool TrackInventory { get; set; }

        /// <summary>
        /// Maximum quantity of the product that a customer can buy
        /// </summary>
        public int MaxQuantity { get; set; }

        /// <summary>
        /// Minimum quantity of the product that a customer can buy
        /// </summary>
        public int MinQuantity { get; set; }

        /// <summary>
        /// Type of product (can be Physical, Digital or Subscription)
        /// </summary>
        public string ProductType { get; set; }

        /// <summary>
        /// Weight unit (for physical product only)
        /// </summary>
        public string WeightUnit { get; set; }

        /// <summary>
        /// Weight of product (for physical product only)
        /// </summary>
        public decimal? Weight { get; set; }

        /// <summary>
        /// Package type
        /// </summary>
        public string PackageType { get; set; }
        /// <summary>
        /// Dimensions measure unit of size (for physical product only)
        /// </summary>
        public string MeasureUnit { get; set; }

        /// <summary>
        /// Height of product size (for physical product only)
        /// </summary>
        public decimal? Height { get; set; }

        /// <summary>
        /// Length of product size (for physical product only)
        /// </summary>
        public decimal? Length { get; set; }

        /// <summary>
        /// Width of product size (for physical product only)
        /// </summary>
        public decimal? Width { get; set; }

        /// <summary>
        /// Indicating whether this product can be reviewed in storefront
        /// </summary>
        public bool EnableReview { get; set; }

        /// <summary>
        /// Maximum number of downloads of product (for digital product only)
        /// </summary>
        public decimal MaxNumberOfDownload { get; set; }

        /// <summary>
        /// Download expiration date (for digital product only)
        /// </summary>
        public DateTime? DownloadExpiration { get; set; }

        /// <summary>
        /// Type of the download (for digital product only)
        /// </summary>
        public string DownloadType { get; set; }

        /// <summary>
        /// Indicating whether this product has user agreement (for digital product only)
        /// </summary>
        public bool HasUserAgreement { get; set; }

        /// <summary>
        /// Type of product shipping
        /// </summary>
        public string ShippingType { get; set; }

        public string VendorId { get; set; }

        /// <summary>
        /// Product's vendor
        /// </summary>
        public Vendor Vendor { get; set; }

        /// <summary>
        /// List og variation properties
        /// </summary>
        public ICollection<CatalogProperty> VariationProperties { get; set; }

        /// <summary>
        /// List of product assets
        /// </summary>
        public ICollection<Asset> Assets { get; set; }

        /// <summary>
        /// List of product variations
        /// </summary>
        public ICollection<Product> Variations { get; set; }

        /// <summary>
        /// Related or associated products
        /// </summary>
        public ICollection<Association> Associations { get; set; }

        /// <summary>
        /// Product description in current language
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Product editorial reviews
        /// </summary>
        public ICollection<EditorialReview> Descriptions { get; set; }

        /// <summary>
        /// Current product price
        /// </summary>
        public ProductPrice Price { get; set; }

        /// <summary>
        /// Product prices for other currencies
        /// </summary>
        public ICollection<ProductPrice> Prices { get; set; }

        /// <summary>
        /// Inventory for default fulfilment center
        /// </summary>
        public Inventory Inventory { get; set; }

        /// <summary>
        /// Inventory of all fulfillment centers.
        /// </summary>
        public ICollection<Inventory> InventoryAll { get; set; }

        /// <summary>
        /// product seo info
        /// </summary>
        public SeoInfo SeoInfo { get; set; }

        /// <summary>
        /// Product main image
        /// </summary>
        public Image PrimaryImage { get; set; }

        /// <summary>
        /// List of product images
        /// </summary>
        public ICollection<Image> Images { get; set; }

        public bool IsQuotable
        {
            get
            {
                return true;
            }
        }

        public bool IsAvailable { get; set; }

        /// <summary>
        /// if the product is sold by subscription only this property contains the recurrence plan
        /// </summary>
        public PaymentPlan PaymentPlan { get; set; }

        /// <summary>
        /// Apply prices to product
        /// </summary>
        /// <param name="prices"></param>
        /// <param name="currentCurrency"></param>
        /// <param name="allCurrencies"></param>
        public void ApplyPrices(IEnumerable<ProductPrice> prices, Currency currentCurrency, IEnumerable<Currency> allCurrencies)
        {
            Prices.Clear();
            Price = null;

            Currency = currentCurrency;
            //group prices by currency
            var groupByCurrencyPrices = prices.GroupBy(x => x.Currency).Where(x => x.Any());
            foreach (var currencyGroup in groupByCurrencyPrices)
            {
                //For each currency need get nominal price (with min qty)
                var orderedPrices = currencyGroup.OrderBy(x => x.MinQuantity ?? 0).ThenBy(x => x.ListPrice);
                var nominalPrice = orderedPrices.FirstOrDefault();
                //and add to nominal price other prices as tier prices
                nominalPrice.TierPrices.AddRange(orderedPrices.Select(x => new TierPrice(x.SalePrice, x.MinQuantity ?? 1)));
                //Add nominal price to product prices list 
                Prices.Add(nominalPrice);
            }

            ApplyPricesWithExchangeRates(allCurrencies);

            //Set current product price for current currency
            Price = Prices.FirstOrDefault(x => x.Currency == currentCurrency);
        }

        private void ApplyPricesWithExchangeRates(IEnumerable<Currency> allCurrencies)
        {
            //Need add product price for all currencies (even if not returned from API need make it by currency exchange conversation)
            foreach (var currency in allCurrencies)
            {
                var price = Prices.FirstOrDefault(x => x.Currency == currency);
                if (price == null)
                {
                    price = new ProductPrice(currency);
                    //Convert exist price to new currency
                    if (Prices.Any())
                    {
                        price = Prices.First().ConvertTo(currency);
                        price.TierPrices.Add(new TierPrice(price.SalePrice, 1));
                    }
                    Prices.Add(price);
                }
            }
        }

        #region IHasProperties Members
        public ICollection<CatalogProperty> Properties { get; set; }
        #endregion

        #region ITaxable Members

        /// <summary>
        /// Gets or sets the value of total shipping tax amount
        /// </summary>
        public Money TaxTotal
        {
            get
            {
                return Price != null ? Price.TaxTotal : null;
            }
        }

        public decimal TaxPercentRate
        {
            get
            {
                return Price != null ? Price.TaxPercentRate : 0m;
            }
        }

        /// <summary>
        /// Gets or sets the value of shipping tax type
        /// </summary>
        public string TaxType { get; set; }

        /// <summary>
        /// Gets or sets the collection of line item tax details lines
        /// </summary>
        /// <value>
        /// Collection of TaxDetail objects
        /// </value>
        public ICollection<TaxDetail> TaxDetails { get; set; }

        public void ApplyTaxRates(IEnumerable<TaxRate> taxRates)
        {
            var productTaxRates = taxRates.Where(x => x.Line.Id != null && x.Line.Id.EqualsInvariant(Id ?? ""));
            Price.ApplyTaxRates(productTaxRates);
        }

        #endregion

        #region IDiscountable Members
        public ICollection<Discount> Discounts { get; private set; }

        public Currency Currency { get; set; }

        public void ApplyRewards(IEnumerable<PromotionReward> rewards)
        {
            var productRewards = rewards.Where(r => r.RewardType == PromotionRewardType.CatalogItemAmountReward && (r.ProductId.IsNullOrEmpty() || r.ProductId.EqualsInvariant(Id)));
            if (productRewards == null)
            {
                return;
            }

            Discounts.Clear();
            Price.DiscountAmount = new Money(Math.Max(0, (Price.ListPrice - Price.SalePrice).Amount), Currency);

            foreach (var reward in productRewards)
            {
                //Initialize tier price discount amount by default values
                var discount = reward.ToDiscountModel(Price.SalePrice);
                foreach (var tierPrice in Price.TierPrices)
                {
                    tierPrice.DiscountAmount = new Money(Math.Max(0, (Price.ListPrice - tierPrice.Price).Amount), Currency);
                }

                if (reward.IsValid)
                {
                    Discounts.Add(discount);
                    Price.DiscountAmount += discount.Amount;

                    //apply discount to tier prices
                    foreach (var tierPrice in Price.TierPrices)
                    {
                        discount = reward.ToDiscountModel(tierPrice.Price);
                        tierPrice.DiscountAmount += discount.Amount;
                    }
                }
            }
        }

        #endregion

        public string Url { get; set; }

        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "product #{0} sku: {1} name: {2}", Id ?? "undef", Sku ?? "undef", Name ?? "undef");
        }
    }
}
