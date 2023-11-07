using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using EARGE.Core;
using EARGE.Core.Domain.Customers;
using EARGE.Core.Domain.Directory;
using EARGE.Core.Domain.Orders;
using EARGE.Core.Domain.Payments;
using EARGE.Core.Domain.Shipping;
using EARGE.Core.Infrastructure;
using EARGE.Core.Plugins;
using EARGE.iyzico.Controllers;
using EARGE.iyzico.PayPalSvc;
using EARGE.iyzico.Services;
using EARGE.iyzico.Settings;
using EARGE.Services;
using EARGE.Services.Catalog;
using EARGE.Services.Common;
using EARGE.Services.Configuration;
using EARGE.Services.Customers;
using EARGE.Services.Directory;
using EARGE.Services.Orders;
using EARGE.Services.Payments;
using EARGE.Services.Shipping;

namespace EARGE.iyzico
{
    [SystemName("Payments.iyzicoExpress")]
    [FriendlyName("iyzico Express")]
    [DisplayOrder(0)]
    public partial class iyzicoExpress : iyzicoProviderBase<iyzicoExpressPaymentSettings>
    {
        private readonly ICurrencyService _currencyService;
        private readonly CurrencySettings _currencySettings;
        private readonly IPriceCalculationService _priceCalculationService;
        private readonly IGenericAttributeService _genericAttributeService;
        private readonly IStateProvinceService _stateProvinceService;
        private readonly IGiftCardService _giftCardService;
        private readonly IShippingService _shippingService;
        private readonly ICustomerService _customerService;
        private readonly ICountryService _countryService;
        private readonly HttpContextBase _httpContext;
        
        public iyzicoExpress(
            ICurrencyService currencyService,
            CurrencySettings currencySettings,
            IPriceCalculationService priceCalculationService,
            IGenericAttributeService genericAttributeService,
            IStateProvinceService stateProvinceService,
            IGiftCardService giftCardService,
            IShippingService shippingService,
            ICustomerService customerService,
            ICountryService countryService,
            HttpContextBase httpContext)
        {
            _currencyService = currencyService;
            _currencySettings = currencySettings;
            _priceCalculationService = priceCalculationService;
            _genericAttributeService = genericAttributeService;
            _stateProvinceService = stateProvinceService;
            _giftCardService = giftCardService;
            _shippingService = shippingService;
            _customerService = customerService;
            _countryService = countryService;
            _httpContext = httpContext;
        }

		protected override string GetResourceRootKey()
		{
			return "Plugins.Payments.iyzicoExpress";
		}

        /// <summary>
        /// Process a payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public override ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
			var result = new ProcessPaymentResult();
            var doPayment = DoExpressCheckoutPayment(processPaymentRequest);
			var settings = CommonServices.Settings.LoadSetting<iyzicoExpressPaymentSettings>(processPaymentRequest.StoreId);

            if (doPayment.Ack == AckCodeType.Success)
            {
                if (iyzicoHelper.GetPaymentAction(settings) == PaymentActionCodeType.Authorization)
                {
                    result.NewPaymentStatus = PaymentStatus.Authorized;
                }
                else
                {
                    result.NewPaymentStatus = PaymentStatus.Paid;
                }
                result.AuthorizationTransactionId = processPaymentRequest.PaypalToken;
                result.CaptureTransactionId = doPayment.DoExpressCheckoutPaymentResponseDetails.PaymentInfo.FirstOrDefault().TransactionID;
                result.CaptureTransactionResult = doPayment.Ack.ToString();
            }
            else
            {
                result.NewPaymentStatus = PaymentStatus.Pending;
            }

            return result;
        }

        /// <summary>
        /// Post process payment (used by payment gateways that require redirecting to a third-party URL)
        /// </summary>
        /// <param name="postProcessPaymentRequest">Payment info required for an order processing</param>
        public override void PostProcessPayment(PostProcessPaymentRequest postProcessPaymentRequest)
        {
            //TODO:
            //handle Giropay

            //if(!String.IsNullOrEmpty(postProcessPaymentRequest.GiroPayUrl))
            //    return re

        }

        public override CapturePaymentResult Capture(CapturePaymentRequest capturePaymentRequest)
        {
            var result = new DoCaptureResponseType();
            var settings = CommonServices.Settings.LoadSetting<iyzicoExpressPaymentSettings>(capturePaymentRequest.Order.StoreId);

            // build the request
            var req = new DoCaptureReq
            {
                DoCaptureRequest = new DoCaptureRequestType()
            };

            //execute request
            
            //using (var service = new PayPalAPIAASoapBinding())
            //{
            //    service.Url = iyzicoHelper.GetPaypalServiceUrl(settings);
            //    service.RequesterCredentials = iyzicoHelper.GetPaypalApiCredentials(settings);
            //    result = service.DoCapture(req);
            //}

            var capturePaymentResult = new CapturePaymentResult();

            if (result.Ack == AckCodeType.Success)
            {
                capturePaymentResult.CaptureTransactionId = result.DoCaptureResponseDetails.PaymentInfo.TransactionID;
                capturePaymentResult.CaptureTransactionResult = "Success";
            }
            else
            {
                capturePaymentResult.CaptureTransactionResult = "Error";
                capturePaymentResult.Errors.Add(result.Errors.FirstOrDefault().LongMessage);
            }

            return capturePaymentResult;
        }

        /// <summary>
        /// Process recurring payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public override ProcessPaymentResult ProcessRecurringPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();
            //TODO
            return result;
        }

        protected override string GetControllerName()
        {
            return "iyzicoExpress";
        }

        public override Type GetControllerType()
        {
            return typeof(iyzicoExpressController);
        }

        public override PaymentMethodType PaymentMethodType
        {
            get
            {
                return PaymentMethodType.StandardAndButton;
            }
        }

        public SetExpressCheckoutResponseType SetExpressCheckout(iyzicoProcessPaymentRequest processPaymentRequest,
            IList<Core.Domain.Orders.OrganizedShoppingCartItem> cart)
        {
            var result = new SetExpressCheckoutResponseType();
            var currentStore = CommonServices.StoreContext.CurrentStore;
			var settings = CommonServices.Settings.LoadSetting<iyzicoExpressPaymentSettings>(processPaymentRequest.StoreId);
            
            var req = new SetExpressCheckoutReq
            {
                SetExpressCheckoutRequest = new SetExpressCheckoutRequestType
                {
                    Version = iyzicoHelper.GetApiVersion(),
                    SetExpressCheckoutRequestDetails = new SetExpressCheckoutRequestDetailsType()
                }
            };

            var details = new SetExpressCheckoutRequestDetailsType
            {
                PaymentAction = iyzicoHelper.GetPaymentAction(settings),
                PaymentActionSpecified = true,
                CancelURL = CommonServices.WebHelper.GetStoreLocation(currentStore.SslEnabled) + "cart",
                ReturnURL = CommonServices.WebHelper.GetStoreLocation(currentStore.SslEnabled) + "Plugins/EARGE.iyzico/iyzicoExpress/GetDetails",
                //CallbackURL = _webHelper.GetStoreLocation(currentStore.SslEnabled) + "Plugins/EARGE.iyzico/iyzicoExpress/ShippingOptions?CustomerID=" + _workContext.CurrentCustomer.Id.ToString(),
                //CallbackTimeout = _payPalExpressPaymentSettings.CallbackTimeout.ToString() 
                ReqConfirmShipping = settings.ConfirmedShipment.ToString(),
                NoShipping = settings.NoShipmentAddress.ToString()
            };

            // populate cart
            decimal itemTotal = decimal.Zero;
            var cartItems = new List<PaymentDetailsItemType>();
            foreach (OrganizedShoppingCartItem item in cart)
            {
                decimal shoppingCartUnitPriceWithDiscountBase = _priceCalculationService.GetUnitPrice(item, true);
                decimal shoppingCartUnitPriceWithDiscount = _currencyService.ConvertFromPrimaryStoreCurrency(shoppingCartUnitPriceWithDiscountBase, CommonServices.WorkContext.WorkingCurrency);
                decimal priceIncludingTier = shoppingCartUnitPriceWithDiscount;

                cartItems.Add(new PaymentDetailsItemType()
                {
                    Name = item.Item.Product.Name,
                    Number = item.Item.Product.Sku,
                    Quantity = item.Item.Quantity.ToString(),
                    Amount = new BasicAmountType()  // this is the per item cost
                    {
                        currencyID = iyzicoHelper.GetPaypalCurrency(_currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId)),
                        Value = (priceIncludingTier).ToString("N", new CultureInfo("en-us"))
                    }
                });
                itemTotal += (item.Item.Quantity * priceIncludingTier);
            };

            // additional handling fee
            var additionalHandlingFee = GetAdditionalHandlingFee(cart);
            cartItems.Add(new PaymentDetailsItemType()
            {
                Name = "Zahlartgebühren",
                Quantity = "1",
                Amount = new BasicAmountType()  
                {
                    currencyID = iyzicoHelper.GetPaypalCurrency(_currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId)),
                    Value = (additionalHandlingFee).ToString("N", new CultureInfo("en-us"))
                }
            });
            itemTotal += GetAdditionalHandlingFee(cart);

            //shipping
            decimal shippingTotal = decimal.Zero;
            if (cart.RequiresShipping())
            {
                decimal? shoppingCartShippingBase = OrderTotalCalculationService.GetShoppingCartShippingTotal(cart);
                if (shoppingCartShippingBase.HasValue && shoppingCartShippingBase > 0)
                {
                    shippingTotal = _currencyService.ConvertFromPrimaryStoreCurrency(shoppingCartShippingBase.Value, CommonServices.WorkContext.WorkingCurrency);
                }
                else
                {
                    shippingTotal = settings.DefaultShippingPrice;
                }
            }

            //This is the default if the Shipping Callback fails
            //var shippingOptions = new List<ShippingOptionType>();
            //shippingOptions.Add(new ShippingOptionType()
            //{
            //    ShippingOptionIsDefault = "true",
            //    ShippingOptionName = "Standard Shipping",
            //    ShippingOptionAmount = new BasicAmountType()
            //    {
            //        Value = shippingTotal.ToString(), //This is the default value used for shipping if the Instant Update API returns an error or does not answer within the callback time
            //        currencyID = PaypalHelper.GetPaypalCurrency(_currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId))
            //    }
            //});
            //details.FlatRateShippingOptions = shippingOptions.ToArray();
            //details.TotalType = TotalType.EstimatedTotal;

            // get total tax
            //SortedDictionary<decimal, decimal> taxRates = null;
            //decimal shoppingCartTaxBase = OrderTotalCalculationService.GetTaxTotal(cart, out taxRates);
            //decimal shoppingCartTax = _currencyService.ConvertFromPrimaryStoreCurrency(shoppingCartTaxBase, CommonServices.WorkContext.WorkingCurrency);
            
            // discount
            decimal discount = -processPaymentRequest.Discount;

            if (discount != 0)
            {
                cartItems.Add(new PaymentDetailsItemType()
                {
                    Name = "Threadrock Discount",
                    Quantity = "1",
                    Amount = new BasicAmountType() // this is the total discount
                    {
                        currencyID = iyzicoHelper.GetPaypalCurrency(_currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId)),
                        Value = discount.ToString("N", new CultureInfo("en-us"))
                    }
                });

                itemTotal += discount;
            }

            // get customer
            int customerId = Convert.ToInt32(CommonServices.WorkContext.CurrentCustomer.Id.ToString());
            var customer = _customerService.GetCustomerById(customerId);

            if (!cart.IsRecurring())
            {
                //we don't apply gift cards for recurring products
                var giftCards = _giftCardService.GetActiveGiftCardsAppliedByCustomer(customer);
                if (giftCards != null)
                {
                    foreach (var gc in giftCards)
                    {
                        if (itemTotal > decimal.Zero)
                        {
                            decimal remainingAmount = gc.GetGiftCardRemainingAmount();
                            decimal amountCanBeUsed = decimal.Zero;
                            if (itemTotal > remainingAmount)
                                amountCanBeUsed = remainingAmount;
                            else
                                amountCanBeUsed = itemTotal - .01M;

                            decimal amountToSubtract = -amountCanBeUsed;

                            cartItems.Add(new PaymentDetailsItemType()
                            {
                                Name = "Giftcard Applied",
                                Quantity = "1",
                                Amount = new BasicAmountType()
                                {
                                    currencyID = iyzicoHelper.GetPaypalCurrency(_currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId)),
                                    Value = amountToSubtract.ToString("N", new CultureInfo("en-us"))
                                }
                            });

                            //reduce subtotal
                            itemTotal += amountToSubtract;
                        }
                    }
                }
            }

            // populate payment details
            var paymentDetails = new PaymentDetailsType
            {
                ItemTotal = new BasicAmountType
                {
                    Value = Math.Round(itemTotal, 2).ToString("N", new CultureInfo("en-us")),
                    currencyID = iyzicoHelper.GetPaypalCurrency(_currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId))
                },
                ShippingTotal = new BasicAmountType
                {
                    Value = Math.Round(shippingTotal, 2).ToString("N", new CultureInfo("en-us")),
                    currencyID = iyzicoHelper.GetPaypalCurrency(_currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId))
                },
                //TaxTotal = new BasicAmountType
                //{
                //    Value = Math.Round(shoppingCartTax, 2).ToString("N", new CultureInfo("en-us")),
                //    currencyID = iyzicoHelper.GetPaypalCurrency(_currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId))
                //},
                OrderTotal = new BasicAmountType
                {
                    Value = Math.Round(itemTotal + shippingTotal, 2).ToString("N", new CultureInfo("en-us")),
                    currencyID = iyzicoHelper.GetPaypalCurrency(_currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId))
                },
                Custom = processPaymentRequest.OrderGuid.ToString(),
                ButtonSource = EARGEAppVersion.CurrentFullVersion,
                PaymentAction = iyzicoHelper.GetPaymentAction(settings),
                PaymentDetailsItem = cartItems.ToArray()
            };
            details.PaymentDetails = new[] { paymentDetails };

            details.ShippingMethodSpecified = true;

            req.SetExpressCheckoutRequest.SetExpressCheckoutRequestDetails.Custom = processPaymentRequest.OrderGuid.ToString();
            req.SetExpressCheckoutRequest.SetExpressCheckoutRequestDetails = details;

            //using (var service = new iyzicoAPIAASoapBinding())
            //{
            //    service.Url = iyzicoHelper.GetPaypalServiceUrl(settings);
            //    service.RequesterCredentials = iyzicoHelper.GetPaypalApiCredentials(settings);
            //    result = service.SetExpressCheckout(req);
            //}

            
            _httpContext.GetCheckoutState().CustomProperties.Add("iyzicoExpressButtonUsed", true);
            return result;
        }

        public GetExpressCheckoutDetailsResponseType GetExpressCheckoutDetails(string token)
        {
            var result = new GetExpressCheckoutDetailsResponseType();
			var settings = CommonServices.Settings.LoadSetting<iyzicoExpressPaymentSettings>(CommonServices.StoreContext.CurrentStore.Id);

            //using (var service = new iyzicoAPIAASoapBinding())
            //{
            //    var req = new GetExpressCheckoutDetailsReq();
            //    req.GetExpressCheckoutDetailsRequest = new GetExpressCheckoutDetailsRequestType
            //    {
            //        Token = token,
            //        Version = iyzicoHelper.GetApiVersion()
            //    };

            //    service.Url = iyzicoHelper.GetPaypalServiceUrl(settings);
            //    service.RequesterCredentials = iyzicoHelper.GetPaypalApiCredentials(settings);
            //    result = service.GetExpressCheckoutDetails(req);
            //}
            return result;
        }

        public ProcessPaymentRequest SetCheckoutDetails(ProcessPaymentRequest processPaymentRequest, GetExpressCheckoutDetailsResponseDetailsType checkoutDetails)
        {
            int customerId = Convert.ToInt32(CommonServices.WorkContext.CurrentCustomer.Id.ToString());
            var customer = _customerService.GetCustomerById(customerId);
			var settings = CommonServices.Settings.LoadSetting<iyzicoExpressPaymentSettings>(CommonServices.StoreContext.CurrentStore.Id);

            CommonServices.WorkContext.CurrentCustomer = customer;

            //var cart = customer.ShoppingCartItems.Where(sci => sci.ShoppingCartType == ShoppingCartType.ShoppingCart).ToList();
            var cart = CommonServices.WorkContext.CurrentCustomer.GetCartItems(ShoppingCartType.ShoppingCart, CommonServices.StoreContext.CurrentStore.Id);

            // get/update billing address
            string billingFirstName = checkoutDetails.PayerInfo.PayerName.FirstName;
            string billingLastName = checkoutDetails.PayerInfo.PayerName.LastName;
            string billingEmail = checkoutDetails.PayerInfo.Payer;
            string billingAddress1 = checkoutDetails.PayerInfo.Address.Street1;
            string billingAddress2 = checkoutDetails.PayerInfo.Address.Street2;
            string billingPhoneNumber = checkoutDetails.PayerInfo.ContactPhone;
            string billingCity = checkoutDetails.PayerInfo.Address.CityName;
            int? billingStateProvinceId = null;
            var billingStateProvince = _stateProvinceService.GetStateProvinceByAbbreviation(checkoutDetails.PayerInfo.Address.StateOrProvince);
            if (billingStateProvince != null)
                billingStateProvinceId = billingStateProvince.Id;
            string billingZipPostalCode = checkoutDetails.PayerInfo.Address.PostalCode;
            int? billingCountryId = null;
            var billingCountry = _countryService.GetCountryByTwoLetterIsoCode(checkoutDetails.PayerInfo.Address.Country.ToString());
            if (billingCountry != null)
                billingCountryId = billingCountry.Id;

            var billingAddress = customer.Addresses.ToList().FindAddress(
                billingFirstName, billingLastName, billingPhoneNumber,
                billingEmail, string.Empty, string.Empty, billingAddress1, billingAddress2, billingCity,
                billingStateProvinceId, billingZipPostalCode, billingCountryId);

            if (billingAddress == null)
            {
                billingAddress = new Core.Domain.Common.Address()
                {
                    FirstName = billingFirstName,
                    LastName = billingLastName,
                    PhoneNumber = billingPhoneNumber,
                    Email = billingEmail,
                    FaxNumber = string.Empty,
                    Company = string.Empty,
                    Address1 = billingAddress1,
                    Address2 = billingAddress2,
                    City = billingCity,
                    StateProvinceId = billingStateProvinceId,
                    ZipPostalCode = billingZipPostalCode,
                    CountryId = billingCountryId,
                    CreatedOnUtc = DateTime.UtcNow,
                };
                customer.Addresses.Add(billingAddress);
            }

            //set default billing address
            customer.BillingAddress = billingAddress;
            _customerService.UpdateCustomer(customer);

            var genericAttributeService = EngineContext.Current.Resolve<IGenericAttributeService>();
            genericAttributeService.SaveAttribute<ShippingOption>(customer, SystemCustomerAttributeNames.SelectedShippingOption, null);

            bool shoppingCartRequiresShipping = cart.RequiresShipping();
            if (shoppingCartRequiresShipping)
            {
                var paymentDetails = checkoutDetails.PaymentDetails.FirstOrDefault();
                string[] shippingFullname = paymentDetails.ShipToAddress.Name.Trim().Split(new char[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                string shippingFirstName = shippingFullname[0];
                string shippingLastName = string.Empty;
                if (shippingFullname.Length > 1)
                    shippingLastName = shippingFullname[1];
                string shippingEmail = checkoutDetails.PayerInfo.Payer;
                string shippingAddress1 = paymentDetails.ShipToAddress.Street1;
                string shippingAddress2 = paymentDetails.ShipToAddress.Street2;
                string shippingPhoneNumber = paymentDetails.ShipToAddress.Phone;
                string shippingCity = paymentDetails.ShipToAddress.CityName;
                int? shippingStateProvinceId = null;
                var shippingStateProvince = _stateProvinceService.GetStateProvinceByAbbreviation(paymentDetails.ShipToAddress.StateOrProvince);
                if (shippingStateProvince != null)
                    shippingStateProvinceId = shippingStateProvince.Id;
                int? shippingCountryId = null;
                string shippingZipPostalCode = paymentDetails.ShipToAddress.PostalCode;
                var shippingCountry = _countryService.GetCountryByTwoLetterIsoCode(paymentDetails.ShipToAddress.Country.ToString());
                if (shippingCountry != null)
                    shippingCountryId = shippingCountry.Id;

                var shippingAddress = customer.Addresses.ToList().FindAddress(
                    shippingFirstName, shippingLastName, shippingPhoneNumber,
                    shippingEmail, string.Empty, string.Empty,
                    shippingAddress1, shippingAddress2, shippingCity,
                    shippingStateProvinceId, shippingZipPostalCode, shippingCountryId);

                if (shippingAddress == null)
                {
                    shippingAddress = new Core.Domain.Common.Address()
                    {
                        FirstName = shippingFirstName,
                        LastName = shippingLastName,
                        PhoneNumber = shippingPhoneNumber,
                        Email = shippingEmail,
                        FaxNumber = string.Empty,
                        Company = string.Empty,
                        Address1 = shippingAddress1,
                        Address2 = shippingAddress2,
                        City = shippingCity,
                        StateProvinceId = shippingStateProvinceId,
                        ZipPostalCode = shippingZipPostalCode,
                        CountryId = shippingCountryId,
                        CreatedOnUtc = DateTime.UtcNow,
                    };
                    customer.Addresses.Add(shippingAddress);
                }

                customer.ShippingAddress = shippingAddress;
                _customerService.UpdateCustomer(customer);
            }

            bool isShippingSet = false;
            GetShippingOptionResponse getShippingOptionResponse = _shippingService.GetShippingOptions(cart, customer.ShippingAddress);

            if (checkoutDetails.UserSelectedOptions != null)
            {
                if (getShippingOptionResponse.Success && getShippingOptionResponse.ShippingOptions.Count > 0)
                {
                    foreach (var shippingOption in getShippingOptionResponse.ShippingOptions)
                    {
                        if (checkoutDetails.UserSelectedOptions.ShippingOptionName.Contains(shippingOption.Name) &&
                            checkoutDetails.UserSelectedOptions.ShippingOptionName.Contains(shippingOption.Description))
                        {
                            _genericAttributeService.SaveAttribute(CommonServices.WorkContext.CurrentCustomer, SystemCustomerAttributeNames.SelectedShippingOption, shippingOption);
                            isShippingSet = true;
                            break;
                        }

                    }
                }

                if (!isShippingSet)
                {
                    var shippingOption = new ShippingOption();
                    shippingOption.Name = checkoutDetails.UserSelectedOptions.ShippingOptionName;
                    decimal shippingPrice = settings.DefaultShippingPrice;
                    decimal.TryParse(checkoutDetails.UserSelectedOptions.ShippingOptionAmount.Value, out shippingPrice);
                    shippingOption.Rate = shippingPrice;
                    _genericAttributeService.SaveAttribute(customer, SystemCustomerAttributeNames.SelectedShippingOption, shippingOption);
                }
            }

            processPaymentRequest.PaypalPayerId = checkoutDetails.PayerInfo.PayerID;


            return processPaymentRequest;
        }

        public DoExpressCheckoutPaymentResponseType DoExpressCheckoutPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new DoExpressCheckoutPaymentResponseType();
			var settings = CommonServices.Settings.LoadSetting<iyzicoExpressPaymentSettings>(processPaymentRequest.StoreId);

            // populate payment details
            var paymentDetails = new PaymentDetailsType
            {
                OrderTotal = new BasicAmountType
                {
                    Value = Math.Round(processPaymentRequest.OrderTotal, 2).ToString("N", new CultureInfo("en-us")),
                    currencyID = iyzicoHelper.GetPaypalCurrency(_currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId))
                },
                Custom = processPaymentRequest.OrderGuid.ToString(),
                ButtonSource = EARGEAppVersion.CurrentFullVersion
            };

            // build the request
            var req = new DoExpressCheckoutPaymentReq
            {
                DoExpressCheckoutPaymentRequest = new DoExpressCheckoutPaymentRequestType
                {
                    Version = iyzicoHelper.GetApiVersion(),
                    DoExpressCheckoutPaymentRequestDetails = new DoExpressCheckoutPaymentRequestDetailsType
                    {
                        Token = processPaymentRequest.PaypalToken,
                        PayerID = processPaymentRequest.PaypalPayerId,
                        PaymentAction = iyzicoHelper.GetPaymentAction(settings),
                        PaymentActionSpecified = true,
                        PaymentDetails = new PaymentDetailsType[]
                        {
                            paymentDetails
                        }
                    }
                }
            };

            ////execute request
            //using (var service = new iyzicoAPIAASoapBinding())
            //{
            //    service.Url = iyzicoHelper.GetPaypalServiceUrl(settings);
            //    service.RequesterCredentials = iyzicoHelper.GetPaypalApiCredentials(settings);
            //    result = service.DoExpressCheckoutPayment(req);
            //}
            return result;
        }

    }
}