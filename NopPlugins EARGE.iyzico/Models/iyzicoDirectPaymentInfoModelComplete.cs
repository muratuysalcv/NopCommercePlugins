using System.Collections.Generic;
using System.Web.Mvc;
using EARGE.Web.Framework;
using EARGE.Web.Framework.Mvc;

namespace EARGE.iyzico.Models
{
    public class iyzicoDirectPaymentInfoModelComplete : iyzicoDirectPaymentInfoModel
    {
        public int OrderId { get; set; }

        public decimal PaymentAmount { get; set; }



        public iyzicoDirectPaymentInfoModelComplete()
        {

        }
    }
}