using System;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using EARGE.Core.Domain.Customers;
using EARGE.Services;
using EARGE.Services.Common;
using EARGE.Services.Customers;
using EARGE.Services.Payments;

namespace EARGE.iyzico.Filters
{
	public class iyzicoExpressCheckoutFilter : IActionFilter
	{
		private static readonly string[] s_interceptableActions = new string[] { "PaymentMethod" };

        private readonly IGenericAttributeService _genericAttributeService;
        private readonly HttpContextBase _httpContext;
        private readonly ICommonServices _services;
        private readonly ICustomerService _customerService;

        public iyzicoExpressCheckoutFilter(IGenericAttributeService genericAttributeService,
            HttpContextBase httpContext, ICommonServices services,
            ICustomerService customerService)
		{
            _genericAttributeService = genericAttributeService;
            _httpContext = httpContext;
            _services = services;
            _customerService = customerService;
		}

		private static bool IsInterceptableAction(string actionName)
		{
			return s_interceptableActions.Contains(actionName, StringComparer.OrdinalIgnoreCase);
		}

		public void OnActionExecuting(ActionExecutingContext filterContext)
		{
            if (filterContext == null || filterContext.ActionDescriptor == null || filterContext.HttpContext == null || filterContext.HttpContext.Request == null)
                return;

            string actionName = filterContext.ActionDescriptor.ActionName;

            var store = _services.StoreContext.CurrentStore;
            var customer = _services.WorkContext.CurrentCustomer;
            
            var attr = Convert.ToBoolean(filterContext.HttpContext.GetCheckoutState().CustomProperties["iyzicoExpressButtonUsed"]);

            //verify paypalexpressprovider was used
            if (attr == true) { 
                _genericAttributeService.SaveAttribute<string>(customer, SystemCustomerAttributeNames.SelectedPaymentMethod, "Payments.iyzicoExpress", store.Id);

                var paymentRequest = _httpContext.Session["OrderPaymentInfo"] as ProcessPaymentRequest;
                if (paymentRequest == null)
                {
                    _httpContext.Session["OrderPaymentInfo"] = new ProcessPaymentRequest();
                }

                //delete property for backward navigation
                _httpContext.GetCheckoutState().CustomProperties.Remove("iyzicoExpressButtonUsed");

                filterContext.Result = new RedirectToRouteResult(
                    new RouteValueDictionary {
                        { "Controller", "Checkout" }, 
                        { "Action", "Confirm" },
                        { "area", null }
                    });
            }
		}

		public void OnActionExecuted(ActionExecutedContext filterContext)
		{
		}
	}
}