﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using VirtoCommerce.Storefront.Model.Cart.Services;
using VirtoCommerce.Storefront.Model.Cart.ValidationErrors;
using VirtoCommerce.Storefront.Model.Common;
using VirtoCommerce.Storefront.Model.Marketing;

namespace VirtoCommerce.Storefront.Model.Cart
{
    public class Shipment : Entity, IDiscountable, IValidatable, ITaxable
    {
        public Shipment()
        {
            Discounts = new List<Discount>();
            Items = new List<CartShipmentItem>();
            TaxDetails = new List<TaxDetail>();
            ValidationErrors = new List<ValidationError>();
            IsValid = true;
        }

        public Shipment(Currency currency)
            :this()
        {
            Currency = currency;
        
            ShippingPrice = new Money(currency);
            ShippingPriceWithTax = new Money(currency);
            DiscountTotal = new Money(currency);
            DiscountTotalWithTax = new Money(currency);
        }
     
        /// <summary>
        /// Gets or sets the value of shipping method code
        /// </summary>
        public string ShipmentMethodCode { get; set; }

        /// <summary>
        /// Gets or sets the value of shipping method option
        /// </summary>
        public string ShipmentMethodOption { get; set; }

        /// <summary>
        /// Gets or sets the value of fulfillment center id
        /// </summary>
        public string FulfilmentCenterId { get; set; }

        /// <summary>
        /// Gets or sets the delivery address
        /// </summary>
        /// <value>
        /// Address object
        /// </value>
        public Address DeliveryAddress { get; set; }

        /// <summary>
        /// Gets or sets the value of volumetric weight
        /// </summary>
        public decimal? VolumetricWeight { get; set; }

        /// <summary>
        /// Gets or sets the value of weight unit
        /// </summary>
        public string WeightUnit { get; set; }

        /// <summary>
        /// Gets or sets the value of weight
        /// </summary>
        public decimal? Weight { get; set; }

        /// <summary>
        /// Gets or sets the value of measurement units
        /// </summary>
        public string MeasureUnit { get; set; }

        /// <summary>
        /// Gets or sets the value of height
        /// </summary>
        public decimal? Height { get; set; }

        /// <summary>
        /// Gets or sets the value of length
        /// </summary>
        public decimal? Length { get; set; }

        /// <summary>
        /// Gets or sets the value of width
        /// </summary>
        public decimal? Width { get; set; }

        /// <summary>
        /// Gets or sets the value of shipping price
        /// </summary>
        public Money ShippingPrice { get; set; }

     
        private Money _shippingPriceWithTax;

        /// <summary>
        /// Gets or sets the value of shipping price including tax
        /// </summary>
        public Money ShippingPriceWithTax
        {
            get
            {
                return _shippingPriceWithTax ?? ShippingPrice;
            }
            set
            {
                _shippingPriceWithTax = value;
            }
        }

        /// <summary>
        /// Gets the value of total shipping price without taxes
        /// </summary>
        public Money Total
        {
            get
            {
                return ShippingPrice - DiscountTotal;
            }
        }

        /// <summary>
        /// Gets the value of total shipping price including taxes
        /// </summary>
        public Money TotalWithTax
        {
            get
            {
                return ShippingPriceWithTax - DiscountTotalWithTax;
            }
        }

        /// <summary>
        /// Gets the value of total shipping discount amount
        /// </summary>
        public Money DiscountTotal { get; set; }
        public Money DiscountTotalWithTax { get; set; }
        /// <summary>
        /// Gets the value of shipping items subtotal
        /// </summary>
        public Money ItemSubtotal
        {
            get
            {
                var itemSubtotal = Items.Sum(i => i.LineItem.ExtendedPrice.Amount);

                return new Money(itemSubtotal, Currency);
            }
        }

        /// <summary>
        /// Gets the value of shipping subtotal
        /// </summary>
        public Money Subtotal
        {
            get
            {
                return ShippingPrice - DiscountTotal;
            }
        }

        public Money SubtotalWithTax
        {
            get
            {
                return ShippingPriceWithTax - DiscountTotalWithTax;
            }
        }

        /// <summary>
        /// Gets or sets the collection of shipping items
        /// </summary>
        /// <value>
        /// Collection of CartShipmentItem objects
        /// </value>
        public ICollection<CartShipmentItem> Items { get; set; }

        #region ITaxable Members
        /// <summary>
        /// Gets or sets the value of total shipping tax amount
        /// </summary>
        public Money TaxTotal
        {
            get
            {
                return TotalWithTax - Total;
            }
        }

        /// <summary>
        /// Gets or sets the value of shipping tax type
        /// </summary>
        public string TaxType { get; set; }

        /// <summary>
        /// Gets or sets the collection of line item tax detalization lines
        /// </summary>
        /// <value>
        /// Collection of TaxDetail objects
        /// </value>
        public ICollection<TaxDetail> TaxDetails { get; set; } 

        public void ApplyTaxRates(IEnumerable<TaxRate> taxRates)
        {
            //Because TaxLine.Id may contains composite string id & extra info
            var shipmentTaxRates = taxRates.Where(x => x.Line.Id.SplitIntoTuple('&').Item1 == Id);
            if (shipmentTaxRates.Any())
            {
                var priceTaxRate = shipmentTaxRates.First(x => x.Line.Id.SplitIntoTuple('&').Item2.EqualsInvariant("price"));
                ShippingPriceWithTax = ShippingPrice + priceTaxRate.Rate;
            }
        }
        #endregion

        #region IValidatable Members
        public bool IsValid { get; set; }
        public ICollection<ValidationError> ValidationErrors { get; set; }
        #endregion

        #region IDiscountable Members
        public ICollection<Discount> Discounts { get; private set; }

        public Currency Currency { get; set; }

        public void ApplyRewards(IEnumerable<PromotionReward> rewards)
        {
            var shipmentRewards = rewards.Where(r => r.RewardType == PromotionRewardType.ShipmentReward && (r.ShippingMethodCode.IsNullOrEmpty() || r.ShippingMethodCode.EqualsInvariant(ShipmentMethodCode)));

            Discounts.Clear();

            DiscountTotal = new Money(0m, Currency);
            DiscountTotalWithTax = new Money(0m, Currency);

            foreach (var reward in shipmentRewards)
            {
                var discount = reward.ToDiscountModel(ShippingPrice, ShippingPriceWithTax);

                if (reward.IsValid)
                {
                    Discounts.Add(discount);
                    DiscountTotal += discount.Amount;
                    DiscountTotalWithTax += discount.AmountWithTax;
                }
            }
        }
        #endregion

        public bool HasSameMethod(ShippingMethod method)
        {           
            // Return true if the fields match:
            return (ShipmentMethodCode.EqualsInvariant(method.ShipmentMethodCode)) && (ShipmentMethodOption.EqualsInvariant(method.OptionName));
        }
    }
}
