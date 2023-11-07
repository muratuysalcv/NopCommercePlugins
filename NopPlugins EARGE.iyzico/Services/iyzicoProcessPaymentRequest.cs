using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EARGE.Services.Payments;

namespace EARGE.iyzico.Services
{
    public class iyzicoProcessPaymentRequest : ProcessPaymentRequest
    {
        /// <summary>
        /// Gets or sets an order Discount Amount
        /// </summary>
        public decimal Discount { get; set; }
    }
}
