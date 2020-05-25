﻿using System;
using System.Data;
using GuidHelpers;

namespace SplurgeStop.Domain.PurchaseTransaction
{
    public class LineItem
    {

        internal LineItem()
        {
            Id = new LineItemId(SequentialGuid.NewSequentialGuid());
        }

        public LineItemId Id { get; }

        public Price Price { get; set; }

        //public int Quantitity { get; set; } // this will always be pieces; other "measurements" in Product 
        //(change type? Could be g, l, kg, pieces)

        //public string Notes { get; set; }
    }

    public class LineItemBuilder
    {
        public Price Price { get; set; }

        public static LineItemBuilder LineItem(Price price)
        {
            return new LineItemBuilder { Price = price };
        }

        public LineItemBuilder WithPrice(Price price)
        {
            this.Price = price;
            return this;
        }

        public LineItem Build()
        {
            LineItem lineItem = new LineItem();
            lineItem.Price = Price ?? throw new ArgumentNullException("Price is required.");
            return lineItem;
        }
    }

}
