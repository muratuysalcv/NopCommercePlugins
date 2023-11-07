using System.Web.Mvc;
using System.Web.Routing;
using EARGE.Web.Framework.Mvc.Routes;

namespace EARGE.ShippingByWeight
{
    public partial class RouteProvider : IRouteProvider
    {
        public void RegisterRoutes(RouteCollection routes)
        {
            routes.MapRoute("EARGE.ShippingByWeight",
                 "Plugins/ShippingByWeight/{action}",
                 new { controller = "ShippingByWeight", action = "Configure" },
                 new[] { "EARGE.ShippingByWeight.Controllers" }
            )
            .DataTokens["area"] = "EARGE.ShippingByWeight";
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
