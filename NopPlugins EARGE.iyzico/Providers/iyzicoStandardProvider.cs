using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Routing;
using EARGE.Core.Domain.Common;
using EARGE.Core.Domain.Directory;
using EARGE.Core.Domain.Orders;
using EARGE.Core.Domain.Payments;
using EARGE.Core.Domain.Shipping;
using EARGE.Core.Logging;
using EARGE.Core.Plugins;
using EARGE.iyzico.Controllers;
using EARGE.iyzico.Services;
using EARGE.iyzico.Settings;
using EARGE.Services;
using EARGE.Services.Directory;
using EARGE.Services.Localization;
using EARGE.Services.Orders;
using EARGE.Services.Payments;
using EARGE.Services.Customers;
using EARGE.Core.Infrastructure;
using EARGE.Services.Catalog;
using EARGE.Core.Domain.Discounts;
using Iyzipay.Model;

namespace EARGE.iyzico
{
    /// <summary>
    /// iyzicoStandard provider
    /// </summary>
    [SystemName("Payments.iyzicoStandard")]
    [FriendlyName("iyzico Standard")]
    [DisplayOrder(2)]
    public partial class iyzicoStandardProvider : PaymentPluginBase, IConfigurable
    {
        private readonly ICurrencyService _currencyService;
        private readonly CurrencySettings _currencySettings;
        private readonly IOrderTotalCalculationService _orderTotalCalculationService;
        private readonly HttpContextBase _httpContext;
        private readonly ICommonServices _commonServices;
        private readonly ILogger _logger;
        private readonly EARGE.Core.IWorkContext _workContext;
        private readonly IShoppingCartService _shoppingCartService;
        private readonly Core.IStoreContext _storeContext;


        public iyzicoStandardProvider(ICurrencyService currencyService,
            HttpContextBase httpContext,
            CurrencySettings currencySettings,
            IOrderTotalCalculationService orderTotalCalculationService,
            ICommonServices commonServices,
            ILogger logger,
            EARGE.Core.IWorkContext workContext,
            IShoppingCartService shoppingCartService,
            Core.IStoreContext storeContext)
        {
            _storeContext = storeContext;
            _shoppingCartService = shoppingCartService;
            _workContext = workContext;
            _currencyService = currencyService;
            _currencySettings = currencySettings;
            _orderTotalCalculationService = orderTotalCalculationService;
            _httpContext = httpContext;
            _commonServices = commonServices;
            _logger = logger;
        }

        /// <summary>
        /// Process a payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public override ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();
            result.NewPaymentStatus = PaymentStatus.Paid;

            var settings = _commonServices.Settings.LoadSetting<iyzicoStandardPaymentSettings>(processPaymentRequest.StoreId);


            var _priceCalculation = EngineContext.Current.Resolve<IPriceCalculationService>();
            var _shoppingCartService = EngineContext.Current.Resolve<IShoppingCartService>();

            var _securitySettings = _commonServices.Settings.LoadSetting<EARGE.Core.Domain.Security.SecuritySettings>(processPaymentRequest.StoreId);

            Iyzipay.Options options = new Iyzipay.Options();
            options.ApiKey = _securitySettings.iyzicoLiveApiKey;
            options.SecretKey = _securitySettings.iyzicoLiveApiSecret;
            options.BaseUrl = "https://api.iyzipay.com";


            var items = _workContext.CurrentCustomer.GetCartItems(ShoppingCartType.ShoppingCart, _storeContext.CurrentStore.Id);


            var request = new Iyzipay.Request.CreatePaymentRequest();
            request.Locale = Iyzipay.Model.Locale.TR.ToString();
            request.ConversationId = _workContext.CurrentCustomer.Id + "_" + processPaymentRequest.StoreId;
            request.Price = items.Sum(x => (x.Item.Quantity * x.Item.Product.Price)).ToString("0.0").Replace(",", ".");
            request.PaidPrice = processPaymentRequest.OrderTotal.ToString("0.0").Replace(",", ".");
            if (_workContext.WorkingCurrency.CurrencyCode == "TRY")
                request.Currency = Iyzipay.Model.Currency.TRY.ToString();
            else if (_workContext.WorkingCurrency.CurrencyCode == "EUR")
                request.Currency = Iyzipay.Model.Currency.EUR.ToString();
            else if (_workContext.WorkingCurrency.CurrencyCode == "GBP")
                request.Currency = Iyzipay.Model.Currency.GBP.ToString();
            else
                throw new Exception("Your payment process has an error about payment currency. Please correct your informations.");

            request.Installment = 1;
            request.BasketId = _workContext.CurrentCustomer.Id + "";
            request.PaymentChannel = Iyzipay.Model.PaymentChannel.WEB.ToString();
            request.PaymentGroup = Iyzipay.Model.PaymentGroup.PRODUCT.ToString();




            var paymentCard = new Iyzipay.Model.PaymentCard();
            paymentCard.CardHolderName = processPaymentRequest.CreditCardName;
            paymentCard.CardNumber = processPaymentRequest.CreditCardNumber;
            string expireMonth = processPaymentRequest.CreditCardExpireMonth + "";
            if (processPaymentRequest.CreditCardExpireMonth < 10)
            {
                expireMonth = "0" + expireMonth;
            }
            paymentCard.ExpireMonth = processPaymentRequest.CreditCardExpireMonth + "";
            paymentCard.ExpireYear = processPaymentRequest.CreditCardExpireYear + "";
            paymentCard.Cvc = processPaymentRequest.CreditCardCvv2;
            paymentCard.RegisterCard = 0;
            request.PaymentCard = paymentCard;

            var billingAddress = _workContext.CurrentCustomer.BillingAddress;
            var shippingAddress = _workContext.CurrentCustomer.ShippingAddress;

            var buyer = new Iyzipay.Model.Buyer();
            buyer.Id = _workContext.CurrentCustomer.Id + "";
            buyer.Name = _workContext.CurrentCustomer.GetFirstName();
            buyer.Surname = _workContext.CurrentCustomer.GetLastName();
            buyer.GsmNumber = _workContext.CurrentCustomer.GetPhone();
            buyer.Email = _workContext.CurrentCustomer.Email;
            buyer.IdentityNumber = billingAddress.IdentityNumber;
            //buyer.LastLoginDate = "2015-10-05 12:43:35";
            //buyer.RegistrationDate = "2013-04-21 15:12:09";

            var ipaddress = _httpContext.Request.Headers["CF-Connecting-IP"];

            if (string.IsNullOrEmpty(ipaddress))
            {
                ipaddress = HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
                if (String.IsNullOrEmpty(ipaddress))
                    ipaddress = HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"];
            }


            buyer.RegistrationAddress = billingAddress.Address1 + " " + billingAddress.StateProvince.Name + " / " + billingAddress.Country.Name;
            buyer.Ip = ipaddress;
            buyer.City = billingAddress.StateProvince.Name;
            buyer.Country = billingAddress.Country.Name;
            buyer.ZipCode = billingAddress.ZipPostalCode;
            request.Buyer = buyer;

            var iyzicoShippingAddress = new Iyzipay.Model.Address();
            iyzicoShippingAddress.ContactName = shippingAddress.FirstName + " " + shippingAddress.LastName;
            iyzicoShippingAddress.City = shippingAddress.StateProvince.Name;
            iyzicoShippingAddress.Country = shippingAddress.Country.Name;
            iyzicoShippingAddress.Description = shippingAddress.Address1;
            iyzicoShippingAddress.ZipCode = shippingAddress.ZipPostalCode;
            request.ShippingAddress = iyzicoShippingAddress;

            var iyzicoBillingAddress = new Iyzipay.Model.Address();
            iyzicoBillingAddress.ContactName = billingAddress.FirstName + " " + billingAddress.LastName;
            iyzicoBillingAddress.City = billingAddress.StateProvince.Name;
            iyzicoBillingAddress.Country = billingAddress.Country.Name;
            iyzicoBillingAddress.Description = billingAddress.Address1;
            iyzicoBillingAddress.ZipCode = billingAddress.ZipPostalCode;
            request.BillingAddress = iyzicoBillingAddress;

            List<Iyzipay.Model.BasketItem> basketItems = new List<Iyzipay.Model.BasketItem>();
            foreach (var item in items)
            {
                var firstBasketItem = new Iyzipay.Model.BasketItem();

                //Get sub-total and discounts that apply to sub-total
                decimal orderSubTotalDiscountAmountBase = decimal.Zero;
                Discount orderSubTotalAppliedDiscount = null;
                decimal subTotalWithoutDiscountBase = decimal.Zero;
                decimal subTotalWithDiscountBase = decimal.Zero;
                IList<OrganizedShoppingCartItem> list = new List<OrganizedShoppingCartItem>() { item };
                _orderTotalCalculationService.GetShoppingCartSubTotal(list,
                    out orderSubTotalDiscountAmountBase, out orderSubTotalAppliedDiscount,
                    out subTotalWithoutDiscountBase, out subTotalWithDiscountBase);


                firstBasketItem.Id = item.Item.Id + "";
                try
                {
                    firstBasketItem.Category1 = item.Item.Product.ProductCategories.FirstOrDefault().Category.Name;
                }
                catch (Exception ex)
                {
                    firstBasketItem.Category1 = "urun";
                }
                firstBasketItem.Name = item.Item.Product.Name;
                firstBasketItem.ItemType = Iyzipay.Model.BasketItemType.PHYSICAL.ToString();
                firstBasketItem.Price = (subTotalWithDiscountBase).ToString("0.0").Replace(",", ".");

                basketItems.Add(firstBasketItem);
            }
            request.BasketItems = basketItems;

            bool Mode3D = true;


            // 3d
            if (Mode3D && _httpContext.Session["3DPaymentCompletedPaymentId"] != null)
            {
                // iþlem baþarýlý olmasý adýna hiçbir hata kodu eklenmez.
                _httpContext.Session["3DPaymentCompletedPaymentId"] = null;
            }
            else if (Mode3D)
            {
                request.CallbackUrl = (_httpContext.Request.IsSecureConnection ? "https://" : "http://") + _httpContext.Request.Url.Host + "/checkout/ThreeDPaymentResponse?encKey="+_httpContext.Session["encKey"]+"";
                // standard
                ThreedsInitialize payment = ThreedsInitialize.Create(request, options);
                if (payment.Status != "success")
                {
                    result.AddError(payment.ErrorMessage);
                }
                else
                {
                    result.NewPaymentStatus = PaymentStatus.Pending3DPayment;
                    result.ThreeDPaymentHtml = payment.HtmlContent;
                    result.Errors.Add("3D");
                    _httpContext.Session["iyzico3DHtmlContent"] = payment.HtmlContent;
                }
            }
            else
            {
                Iyzipay.Model.Payment payment = Iyzipay.Model.Payment.Create(request, options);
                if (payment.Status != "success")
                {
                    result.AddError(payment.ErrorMessage);
                }
            }

            return result;
        }

        /// <summary>
        /// Post process payment (used by payment gateways that require redirecting to a third-party URL)
        /// </summary>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        public override void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {

        }

        /// <summary>
        /// Gets a value indicating whether customers can complete a payment after order is placed but not completed (for redirection payment methods)
        /// </summary>
        /// <param name="order">Order</param>
        /// <returns>Result</returns>
        public override bool CanRePostProcessPayment(Order order)
        {
            if (order == null)
                throw new ArgumentNullException("order");

            if (order.PaymentStatus == PaymentStatus.Pending && (DateTime.UtcNow - order.CreatedOnUtc).TotalSeconds > 5)
            {
                return true;
            }
            return true;
        }

        public override Type GetControllerType()
        {
            return typeof(iyzicoStandardController);
        }

        public override decimal GetAdditionalHandlingFee(IList<OrganizedShoppingCartItem> cart)
        {
            var result = decimal.Zero;
            try
            {
                var settings = _commonServices.Settings.LoadSetting<iyzicoStandardPaymentSettings>(_commonServices.StoreContext.CurrentStore.Id);

                result = this.CalculateAdditionalFee(_orderTotalCalculationService, cart, settings.AdditionalFee, settings.AdditionalFeePercentage);
            }
            catch (Exception)
            {
            }
            return result;
        }


        /// <summary>
        /// Gets PDT details
        /// </summary>
        /// <param name="tx">TX</param>
        /// <param name="values">Values</param>
        /// <param name="response">Response</param>
        /// <returns>Result</returns>
        public bool GetPDTDetails(string tx, iyzicoStandardPaymentSettings settings, out Dictionary<string, string> values, out string response)
        {
            var req = (HttpWebRequest)WebRequest.Create(iyzicoHelper.GetPaypalUrl(settings));
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";

            string formContent = string.Format("cmd=_notify-synch&at={0}&tx={1}", settings.PdtToken, tx);
            req.ContentLength = formContent.Length;

            using (var sw = new StreamWriter(req.GetRequestStream(), Encoding.ASCII))
                sw.Write(formContent);

            response = null;
            using (var sr = new StreamReader(req.GetResponse().GetResponseStream()))
                response = HttpUtility.UrlDecode(sr.ReadToEnd());

            values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            bool firstLine = true, success = false;
            foreach (string l in response.Split('\n'))
            {
                string line = l.Trim();
                if (firstLine)
                {
                    success = line.Equals("SUCCESS", StringComparison.OrdinalIgnoreCase);
                    firstLine = false;
                }
                else
                {
                    int equalPox = line.IndexOf('=');
                    if (equalPox >= 0)
                        values.Add(line.Substring(0, equalPox), line.Substring(equalPox + 1));
                }
            }

            return success;
        }

        /// <summary>
        /// Splits the difference of two value into a portion value (for each item) and a rest value
        /// </summary>
        /// <param name="difference">The difference value</param>
        /// <param name="numberOfLines">Number of lines\items to split the difference</param>
        /// <param name="portion">Portion value</param>
        /// <param name="rest">Rest value</param>
        private void SplitDifference(decimal difference, int numberOfLines, out decimal portion, out decimal rest)
        {
            portion = rest = decimal.Zero;

            if (numberOfLines == 0)
                numberOfLines = 1;

            int intDifference = (int)(difference * 100);
            int intPortion = (int)Math.Truncate((double)intDifference / (double)numberOfLines);
            int intRest = intDifference % numberOfLines;

            portion = Math.Round(((decimal)intPortion) / 100, 2);
            rest = Math.Round(((decimal)intRest) / 100, 2);

            Debug.Assert(difference == ((numberOfLines * portion) + rest));
        }

        /// <summary>
        /// Get all iyzico line items
        /// </summary>
        /// <param name="postProcessPaymentRequest">Post process paymenmt request object</param>
        /// <param name="checkoutAttributeValues">List with checkout attribute values</param>
        /// <param name="cartTotal">Receives the calculated cart total amount</param>
        /// <returns>All items for iyzico Standard API</returns>
        public List<iyzicoLineItem> GetLineItems(PostProcessPaymentRequest postProcessPaymentRequest, out decimal cartTotal)
        {
            cartTotal = decimal.Zero;

            var order = postProcessPaymentRequest.Order;
            var lst = new List<iyzicoLineItem>();

            // order items
            foreach (var orderItem in order.OrderItems)
            {
                var item = new iyzicoLineItem()
                {
                    Type = iyzicoItemType.CartItem,
                    Name = orderItem.Product.GetLocalized(x => x.Name),
                    Quantity = orderItem.Quantity,
                    Amount = orderItem.UnitPriceExclTax
                };
                lst.Add(item);

                cartTotal += orderItem.PriceExclTax;
            }

            // checkout attributes.... are included in order total
            //foreach (var caValue in checkoutAttributeValues)
            //{
            //	var attributePrice = _taxService.GetCheckoutAttributePrice(caValue, false, order.Customer);

            //	if (attributePrice > decimal.Zero && caValue.CheckoutAttribute != null)
            //	{
            //		var item = new iyzicoLineItem()
            //		{
            //			Type = iyzicoItemType.CheckoutAttribute,
            //			Name = caValue.CheckoutAttribute.GetLocalized(x => x.Name),
            //			Quantity = 1,
            //			Amount = attributePrice
            //		};
            //		lst.Add(item);

            //		cartTotal += attributePrice;
            //	}
            //}

            // shipping
            if (order.OrderShippingExclTax > decimal.Zero)
            {
                var item = new iyzicoLineItem()
                {
                    Type = iyzicoItemType.Shipping,
                    Name = T("Plugins.Payments.iyzicoStandard.ShippingFee").Text,
                    Quantity = 1,
                    Amount = order.OrderShippingExclTax
                };
                lst.Add(item);

                cartTotal += order.OrderShippingExclTax;
            }

            // payment fee
            if (order.PaymentMethodAdditionalFeeExclTax > decimal.Zero)
            {
                var item = new iyzicoLineItem()
                {
                    Type = iyzicoItemType.PaymentFee,
                    Name = T("Plugins.Payments.iyzicoStandard.PaymentMethodFee").Text,
                    Quantity = 1,
                    Amount = order.PaymentMethodAdditionalFeeExclTax
                };
                lst.Add(item);

                cartTotal += order.PaymentMethodAdditionalFeeExclTax;
            }

            // tax
            if (order.OrderTax > decimal.Zero)
            {
                var item = new iyzicoLineItem()
                {
                    Type = iyzicoItemType.Tax,
                    Name = T("Plugins.Payments.iyzicoStandard.SalesTax").Text,
                    Quantity = 1,
                    Amount = order.OrderTax
                };
                lst.Add(item);

                cartTotal += order.OrderTax;
            }

            return lst;
        }

        /// <summary>
        /// Manually adjusts the net prices for cart items to avoid rounding differences with the iyzico API.
        /// </summary>
        /// <param name="paypalItems">iyzico line items</param>
        /// <param name="postProcessPaymentRequest">Post process paymenmt request object</param>
        /// <remarks>
        /// In detail: We add what we have thrown away in the checkout when we rounded prices to two decimal places.
        /// It's a workaround. Better solution would be to store the thrown away decimal places for each OrderItem in the database.
        /// More details: http://magento.xonu.de/magento-extensions/empfehlungen/magento-paypal-rounding-error-fix/
        /// </remarks>
        public void AdjustLineItemAmounts(List<iyzicoLineItem> paypalItems, PostProcessPaymentRequest postProcessPaymentRequest)
        {
            try
            {
                var cartItems = paypalItems.Where(x => x.Type == iyzicoItemType.CartItem);

                if (cartItems.Count() <= 0)
                    return;

                decimal totalEARGE = Math.Round(postProcessPaymentRequest.Order.OrderSubtotalExclTax, 2);
                decimal totaliyzico = decimal.Zero;
                decimal delta, portion, rest;

                // calculate what iyzico calculates
                cartItems.Each(x => totaliyzico += (x.AmountRounded * x.Quantity));
                totaliyzico = Math.Round(totaliyzico, 2, MidpointRounding.AwayFromZero);

                // calculate difference
                delta = Math.Round(totalEARGE - totaliyzico, 2);
                if (delta == decimal.Zero)
                    return;

                // prepare lines... only lines with quantity = 1 are adjustable. if there is no one, create one.
                if (!cartItems.Any(x => x.Quantity == 1))
                {
                    var item = cartItems.First(x => x.Quantity > 1);
                    item.Quantity -= 1;
                    var newItem = item.Clone();
                    newItem.Quantity = 1;
                    paypalItems.Insert(paypalItems.IndexOf(item) + 1, newItem);
                }

                var cartItemsOneQuantity = paypalItems.Where(x => x.Type == iyzicoItemType.CartItem && x.Quantity == 1);
                Debug.Assert(cartItemsOneQuantity.Count() > 0);

                SplitDifference(delta, cartItemsOneQuantity.Count(), out portion, out rest);

                if (portion != decimal.Zero)
                {
                    cartItems
                        .Where(x => x.Quantity == 1)
                        .Each(x => x.Amount = x.Amount + portion);
                }

                if (rest != decimal.Zero)
                {
                    var restItem = cartItems.First(x => x.Quantity == 1);
                    restItem.Amount = restItem.Amount + rest;
                }

                //"SM: {0}, PP: {1}, delta: {2} (portion: {3}, rest: {4})".FormatWith(totalEARGE, totaliyzico, delta, portion, rest).Dump();
            }
            catch (Exception exc)
            {
                _logger.Error(exc.Message, exc);
            }
        }


        /// <summary>
        /// Verifies IPN
        /// </summary>
        /// <param name="formString">Form string</param>
        /// <param name="values">Values</param>
        /// <returns>Result</returns>
        public bool VerifyIPN(string formString, out Dictionary<string, string> values)
        {
            // settings: multistore context not possible here. we need the custom value to determine what store it is.
            var settings = _commonServices.Settings.LoadSetting<iyzicoStandardPaymentSettings>();

            var req = (HttpWebRequest)WebRequest.Create(iyzicoHelper.GetPaypalUrl(settings));
            req.Method = "POST";
            req.ContentType = "application/x-www-form-urlencoded";
            req.UserAgent = HttpContext.Current.Request.UserAgent;

            string formContent = string.Format("{0}&cmd=_notify-validate", formString);
            req.ContentLength = formContent.Length;

            using (var sw = new StreamWriter(req.GetRequestStream(), Encoding.ASCII))
            {
                sw.Write(formContent);
            }

            string response = null;
            using (var sr = new StreamReader(req.GetResponse().GetResponseStream()))
            {
                response = HttpUtility.UrlDecode(sr.ReadToEnd());
            }
            bool success = response.Trim().Equals("VERIFIED", StringComparison.OrdinalIgnoreCase);

            values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (string l in formString.Split('&'))
            {
                string line = HttpUtility.UrlDecode(l).Trim();
                int equalPox = line.IndexOf('=');
                if (equalPox >= 0)
                    values.Add(line.Substring(0, equalPox), line.Substring(equalPox + 1));
            }

            return success;
        }

        /// <summary>
        /// Gets a route for provider configuration
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="routeValues">Route values</param>
        public override void GetConfigurationRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "Configure";
            controllerName = "iyzicoStandard";
            routeValues = new RouteValueDictionary() { { "area", "EARGE.iyzico" } };
        }

        /// <summary>
        /// Gets a route for payment info
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="routeValues">Route values</param>
        public override void GetPaymentInfoRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "PaymentInfo";
            controllerName = "iyzicoStandard";
            routeValues = new RouteValueDictionary() { { "area", "EARGE.iyzico" } };
        }

        #region Properties

        public override PaymentMethodType PaymentMethodType
        {
            get
            {
                return PaymentMethodType.Standard;
            }
        }

        #endregion
    }
}
