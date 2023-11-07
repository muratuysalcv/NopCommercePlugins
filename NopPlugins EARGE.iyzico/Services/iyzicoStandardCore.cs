using System;
using System.Globalization;

namespace EARGE.iyzico.Services
{
	public class iyzicoLineItem : ICloneable<iyzicoLineItem>
	{
		public iyzicoItemType Type { get; set; }
		public string Name { get; set; }
		public int Quantity { get; set; }
		public decimal Amount { get; set; }

		public decimal AmountRounded
		{
			get
			{
				return Math.Round(Amount, 2);
			}
		}

		public iyzicoLineItem Clone()
		{
			var item = new iyzicoLineItem()
			{
				Type = this.Type,
				Name = this.Name,
				Quantity = this.Quantity,
				Amount = this.Amount
			};
			return item;
		}

		object ICloneable.Clone()
		{
			return this.Clone();
		}
	}


	public enum iyzicoItemType : int
	{
		CartItem = 0,
		CheckoutAttribute,
		Shipping,
		PaymentFee,
		Tax
	}
}
