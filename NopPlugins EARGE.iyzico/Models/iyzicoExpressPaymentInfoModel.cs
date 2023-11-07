using EARGE.Web.Framework.Mvc;

namespace EARGE.iyzico.Models
{
    public class iyzicoExpressPaymentInfoModel : ModelBase
    {
        public iyzicoExpressPaymentInfoModel()
        {
            
        }

        public bool CurrentPageIsBasket { get; set; }

        public string SubmitButtonImageUrl { get; set; }

    }
}