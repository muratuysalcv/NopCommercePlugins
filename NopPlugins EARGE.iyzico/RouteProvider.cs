using System.Web.Mvc;
using System.Web.Routing;
using EARGE.Web.Framework.Mvc.Routes;

namespace EARGE.iyzico
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(RouteCollection routes)
        {
            routes.MapRoute("EARGE.iyzicoExpress",
                "Plugins/EARGE.iyzico/{controller}/{action}",
                new { controller = "iyzicoExpress", action = "Index" },
                new[] { "EARGE.iyzico.Controllers" }
            )
            .DataTokens["area"] = "EARGE.iyzico";

            routes.MapRoute("EARGE.iyzicoDirect",
                "Plugins/EARGE.iyzico/{controller}/{action}",
                new { controller = "iyzicoDirect", action = "Index" },
                new[] { "EARGE.iyzico.Controllers" }
            )
            .DataTokens["area"] = "EARGE.iyzico";

            routes.MapRoute("EARGE.iyzicoStandard",
                "Plugins/EARGE.iyzico/{controller}/{action}",
                new { controller = "iyzicoStandard", action = "Index" },
                new[] { "EARGE.iyzico.Controllers" }
            )
            .DataTokens["area"] = "EARGE.iyzico";

            //Legacay Routes
            routes.MapRoute("EARGE.iyzicoExpress.IPN",
                 "Plugins/PaymentiyzicoExpress/IPNHandler",
                 new { controller = "iyzicoExpress", action = "IPNHandler" },
                 new[] { "EARGE.iyzico.Controllers" }
            )
            .DataTokens["area"] = "EARGE.iyzico";

            routes.MapRoute("EARGE.iyzicoDirect.IPN",
                 "Plugins/PaymentiyzicoDirect/IPNHandler",
                 new { controller = "iyzicoDirect", action = "IPNHandler" },
                 new[] { "EARGE.iyzico.Controllers" }
            )
            .DataTokens["area"] = "EARGE.iyzico";

            routes.MapRoute("EARGE.iyzicoStandard.IPN",
                 "Plugins/PaymentiyzicoStandard/IPNHandler",
                 new { controller = "iyzicoStandard", action = "IPNHandler" },
                 new[] { "EARGE.iyzico.Controllers" }
            )
            .DataTokens["area"] = "EARGE.iyzico";

            routes.MapRoute("EARGE.iyzicoStandard.PDT",
                 "Plugins/PaymentiyzicoStandard/PDTHandler",
                 new { controller = "iyzicoStandard", action = "PDTHandler" },
                 new[] { "EARGE.iyzico.Controllers" }
            )
            .DataTokens["area"] = "EARGE.iyzico";

            routes.MapRoute("EARGE.iyzicoExpress.RedirectFromPaymentInfo",
                 "Plugins/PaymentiyzicoExpress/RedirectFromPaymentInfo",
                 new { controller = "iyzicoExpress", action = "RedirectFromPaymentInfo" },
                 new[] { "EARGE.iyzico.Controllers" }
            )
            .DataTokens["area"] = "EARGE.iyzico";

            routes.MapRoute("EARGE.iyzicoStandard.CancelOrder",
                 "Plugins/PaymentiyzicoStandard/CancelOrder",
                 new { controller = "iyzicoStandard", action = "CancelOrder" },
                 new[] { "EARGE.iyzico.Controllers" }
            )
            .DataTokens["area"] = "EARGE.iyzico";
        }

        public int Priority
        {
            get
            {
                return 0;
            }
        }
    }
}
