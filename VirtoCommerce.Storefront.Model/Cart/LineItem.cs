using System;
using System.Collections.Generic;
using System.Linq;
using VirtoCommerce.Storefront.Model.Cart.Services;
using VirtoCommerce.Storefront.Model.Cart.ValidationErrors;
using VirtoCommerce.Storefront.Model.Catalog;
using VirtoCommerce.Storefront.Model.Common;
using VirtoCommerce.Storefront.Model.Marketing;
using VirtoCommerce.Storefront.Model.Subscriptions;

namespace VirtoCommerce.Storefront.Model.Cart
{
    public partial class LineItem : Entity, IDiscountable, IValidatable, ITaxable
    {
        public LineItem(Currency currency, Language language)
        {
            Currency = currency;
            LanguageCode = language.CultureName;
            ListPrice = new Money(currency);
            SalePrice = new Money(currency);
            DiscountAmount = new Money(currency);
            Discounts = new List<Discount>();
            TaxDetails = new List<TaxDetail>();
            DynamicProperties = new List<DynamicProperty>();
            ValidationErrors = new List<ValidationError>();
            IsValid = true;
        }

        /// <summary>
        /// Gets or sets line item created date
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Gets or sets the product corresponding to line item
        /// </summary>
        public Product Product { get; set; }

        /// <summary>
        /// Gets or sets the value of product id
        /// </summary>
        public string ProductId { get; set; }

        /// <summary>
        /// Gets or sets the type of product (can be Physical, Digital or Subscription)
        /// </summary>
        public string ProductType { get; set; }

        /// <summary>
        /// Gets or sets the value of catalog id
        /// </summary>
        public string CatalogId { get; set; }

        /// <summary>
        /// Gets or sets the value of category id
        /// </summary>
        public string CategoryId { get; set; }

        /// <summary>
        /// Gets or sets the value of product SKU
        /// </summary>
        public string Sku { get; set; }

        /// <summary>
        /// Gets or sets the value of line item name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the value of line item quantity
        /// </summary>
        public int Quantity { get; set; }

        /// <summary>
        /// Gets or Sets InStockQuantity
        /// </summary>
        public int InStockQuantity { get; set; }

        /// <summary>
        /// Gets or sets the value of line item warehouse location
        /// </summary>
        public string WarehouseLocation { get; set; }

        /// <summary>
        /// Gets or sets the value of line item shipping method code
        /// </summary>
        public string ShipmentMethodCode { get; set; }

        /// <summary>
        /// Gets or sets the requirement for line item shipping
        /// </summary>
        public bool RequiredShipping { get; set; }

        /// <summary>
        /// Gets or sets the value of line item thumbnail image absolute URL
        /// </summary>
        public string ThumbnailImageUrl { get; set; }

        /// <summary>
        /// Gets or sets the value of line item image absolute URL
        /// </summary>
        public string ImageUrl { get; set; }

        /// <summary>
        /// Gets or sets the flag of line item is a gift
        /// </summary>
        public bool IsGift { get; set; }

        /// <summary>
        /// Gets or sets the value of language code
        /// </summary>
        /// <value>
        /// Culture name in ISO 3166-1 alpha-3 format
        /// </value>
        public string LanguageCode { get; private set; }

        /// <summary>
        /// Gets or sets the value of line item comment
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// Gets or sets the flag of line item is recurring
        /// </summary>
        public bool IsReccuring { get; set; }

        /// <summary>
        /// Gets or sets flag of line item has tax
        /// </summary>
        public bool TaxIncluded { get; set; }

        /// <summary>
        /// Gets or sets the value of line item volumetric weight
        /// </summary>
        public decimal? VolumetricWeight { get; set; }

        /// <summary>
        /// Gets or sets the value of line item weight unit
        /// </summary>
        public string WeightUnit { get; set; }

        /// <summary>
        /// Gets or sets the value of line item weight
        /// </summary>
        public decimal? Weight { get; set; }

        /// <summary>
        /// Gets or sets the value of line item measurement unit
        /// </summary>
        public string MeasureUnit { get; set; }

        /// <summary>
        /// Gets or sets the value of line item height
        /// </summary>
        public decimal? Height { get; set; }

        /// <summary>
        /// Gets or sets the value of line item length
        /// </summary>
        public decimal? Length { get; set; }

        /// <summary>
        /// Gets or sets the value of line item width
        /// </summary>
        public decimal? Width { get; set; }

        public bool IsReadOnly { get; set; }

        /// <summary>
        /// Gets or sets the value of line item original price
        /// </summary>
        public Money ListPrice { get; set; }

        /// <summary>
        /// if the product is sold by subscription only this property contains the recurrence plan
        /// </summary>
        public PaymentPlan PaymentPlan { get; set; }

        /// <summary>
        /// Gets or sets the value of line item original price including tax 
        /// </summary>
        public Money ListPriceWithTax
        {
            get
            {
                return ListPrice + ListPrice * TaxPercentRate;
            }
        }

        /// <summary>
        /// Gets or sets the value of line item sale price
        /// </summary>
        public Money SalePrice { get; set; }

        /// <summary>
        /// Gets or sets the value of line item sale price with tax
        /// </summary>
        public Money SalePriceWithTax
        {
            get
            {
                return SalePrice + SalePrice * TaxPercentRate;
            }
        }

        /// <summary>
        /// Gets the value of line item actual price (include all types of discounts)
        /// </summary>
        public Money PlacedPrice
        {
            get
            {
                return ListPrice - DiscountAmount;
            }
        }

        public Money PlacedPriceWithTax
        {
            get
            {
                return ListPriceWithTax - DiscountAmountWithTax;
            }
        }

        /// <summary>
        /// Gets the value of line item extended price (placed price * line item quantity)
        /// </summary>
        public Money ExtendedPrice
        {
            get
            {
                return PlacedPrice * Quantity;
            }
        }

        public Money ExtendedPriceWithTax
        {
            get
            {
                return PlacedPriceWithTax * Quantity;
            }
        }

        public Money DiscountAmount { get; set; }
        public Money DiscountAmountWithTax
        {
            get
            {
                return DiscountAmount + DiscountAmount * TaxPercentRate;
            }
        }

        /// <summary>
        /// Gets the value of line item total discount amount
        /// </summary>
        public Money DiscountTotal
        {
            get
            {
                return DiscountAmount * Math.Max(1, Quantity);
            }
        }

        public Money DiscountTotalWithTax
        {
            get
            {
                return DiscountAmountWithTax * Math.Max(1, Quantity);
            }
        }

        /// <summary>
        /// Used for dynamic properties management, contains object type string
        /// </summary>
        /// <value>Used for dynamic properties management, contains object type string</value>
        public string ObjectType { get; set; }

        /// <summary>
        /// Dynamic properties collections
        /// </summary>
        /// <value>Dynamic properties collections</value>
        public ICollection<DynamicProperty> DynamicProperties { get; set; }

        #region IValidatable Members
        public bool IsValid { get; set; }
        public ICollection<ValidationError> ValidationErrors { get; set; }
        #endregion


        #region ITaxable Members
        /// <summary>
        /// Gets or sets the value of total shipping tax amount
        /// </summary>
        public virtual Money TaxTotal
        {
            get
            {
                return ExtendedPrice * TaxPercentRate;
            }
        }

        public decimal TaxPercentRate { get; set; }

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
            TaxPercentRate = 0m;
            var lineItemTaxRate = taxRates.FirstOrDefault(x => x.Line.Id != null && x.Line.Id.EqualsInvariant(Id ?? ""));
            if(lineItemTaxRate == null)
            {
                lineItemTaxRate = taxRates.FirstOrDefault(x => x.Line.Code != null && x.Line.Code.EqualsInvariant(Sku ?? ""));
            }           
            if (lineItemTaxRate != null && lineItemTaxRate.Rate.Amount > 0)
            {
                var amount = ExtendedPrice.Amount > 0 ? ExtendedPrice.Amount : SalePrice.Amount;
                if (amount > 0)
                {
                    TaxPercentRate = TaxRate.TaxPercentRound(lineItemTaxRate.Rate.Amount / amount);
                }
            }
        }
        #endregion


        #region IDiscountable  Members
        public Currency Currency { get; private set; }

        public ICollection<Discount> Discounts { get; private set; }

        public void ApplyRewards(IEnumerable<PromotionReward> rewards)
        {
            var lineItemRewards = rewards.Where(x => x.RewardType == PromotionRewardType.CatalogItemAmountReward && (x.ProductId.IsNullOrEmpty() || x.ProductId.EqualsInvariant(ProductId)));
            
            Discounts.Clear();

            DiscountAmount = new Money(Math.Max(0, (ListPrice - SalePrice).Amount), Currency);

            foreach (var reward in lineItemRewards)
            {
                var discount = reward.ToDiscountModel(SalePrice);
                if (reward.Quantity > 0)
                {
                    var money = discount.Amount * Math.Min(reward.Quantity, Quantity);
                    //TODO: need allocate more rightly between each quantities
                    discount.Amount = money.Allocate(Quantity).FirstOrDefault();
                }
                if (reward.IsValid)
                {
                    Discounts.Add(discount);
                    DiscountAmount += discount.Amount;
                }
            }
        }
        #endregion

        public override string ToString()
        {
            return string.Format("cart lineItem #{0} {1} qty: {2}", Id ?? "undef", Name ?? "undef", Quantity);
        }
    }
}