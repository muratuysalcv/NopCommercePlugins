using System;
using System.Globalization;
using Autofac;
using EARGE.Core;
using EARGE.Core.Domain.Catalog;
using EARGE.Core.Domain.Directory;
using EARGE.Core.Domain.Payments;
using EARGE.Core.Plugins;
using EARGE.iyzico.Controllers;
using EARGE.iyzico.PayPalSvc;
using EARGE.iyzico.Services;
using EARGE.iyzico.Settings;
using EARGE.Services.Configuration;
using EARGE.Services.Customers;
using EARGE.Services.Directory;
using EARGE.Services.Payments;
using Iyzipay.Request;
using Iyzipay.Model;
using System.Collections.Generic;
using Iyzipay;
using EARGE.Services.Orders;
using System.Linq;

namespace EARGE.iyzico
{
    /// <summary>
    /// iyzicoDirect provider
    /// </summary>
    [SystemName("Payments.iyzicoDirect")]
    [FriendlyName("iyzico Direct")]
    [DisplayOrder(1)]
    public class iyzicoDirectProvider : iyzicoProviderBase<iyzicoDirectPaymentSettings>
    {
        #region Fields

        private readonly ICurrencyService _currencyService;
        private readonly ICustomerService _customerService;
        private readonly CurrencySettings _currencySettings;
        private readonly IWorkContext _workContext;
        private readonly IShoppingCartService _cartService;
        private readonly IStoreContext _storeContext;


        #endregion

        #region Ctor

        public iyzicoDirectProvider(ICurrencyService currencyService,
            ICustomerService customerService,
            CurrencySettings currencySettings,
            IComponentContext ctx,
            IWorkContext workContext,
            IShoppingCartService cartService,
            IStoreContext storeContext)
        {
            _storeContext = storeContext;
            _currencyService = currencyService;
            _customerService = customerService;
            _currencySettings = currencySettings;
            _workContext = workContext;
            _cartService = cartService;
        }

        #endregion

        #region Methods

        protected override string GetResourceRootKey()
        {
            return "Plugins.Payments.iyzicoDirect";
        }

        /// <summary>
        /// Process a payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public override ProcessPaymentResult ProcessPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();

            var customer = _customerService.GetCustomerById(processPaymentRequest.CustomerId);
            var settings = CommonServices.Settings.LoadSetting<iyzicoDirectPaymentSettings>(processPaymentRequest.StoreId);

            Options options = new Options();
            if (settings.UseSandbox)
            {
                options.ApiKey = settings.ApiKey_TEST;
                options.SecretKey = settings.SecretKey_TEST;
                options.BaseUrl = "https://sandbox-api.iyzipay.com";
            }
            else
            {
                options.ApiKey = settings.ApiKey;
                options.SecretKey = settings.SecretKey;
                options.BaseUrl = "https://api.iyzipay.com";
            }

            var currentCustomer = _workContext.CurrentCustomer;
            var shippingAddress = currentCustomer.ShippingAddress;
            var billingAddress = currentCustomer.BillingAddress;


            CreatePaymentRequest request = new CreatePaymentRequest();
            request.Locale = Locale.TR.ToString();
            request.ConversationId = processPaymentRequest.OrderGuid.ToString();
            request.Price = processPaymentRequest.OrderTotal.ToString("N2").Replace(",", ".");
            request.PaidPrice = processPaymentRequest.OrderTotal.ToString("N2").Replace(",", ".");
            request.Currency = Iyzipay.Model.Currency.TRY.ToString();
            request.Installment = 1;
            request.BasketId = request.ConversationId;
            request.PaymentChannel = PaymentChannel.WEB.ToString();
            request.PaymentGroup = PaymentGroup.PRODUCT.ToString();

            var paymentCard = new Iyzipay.Model.PaymentCard();
            paymentCard.CardHolderName = processPaymentRequest.CreditCardName;
            paymentCard.CardNumber = processPaymentRequest.CreditCardNumber;
            paymentCard.ExpireMonth = processPaymentRequest.CreditCardExpireMonth.ToString(); //"12";
            paymentCard.ExpireYear = processPaymentRequest.CreditCardExpireYear.ToString();// "2030";
            paymentCard.Cvc = processPaymentRequest.CreditCardCvv2; //"123";
            paymentCard.RegisterCard = 0;
            request.PaymentCard = paymentCard;


            Buyer buyer = new Buyer();
            buyer.Id = customer.Id + "";
            buyer.Name = customer.GetFirstName();
            buyer.Surname = customer.GetLastName();
            buyer.GsmNumber = customer.GetPhone();
            buyer.Email = customer.Email;
            buyer.IdentityNumber = billingAddress.IdentityNumber;
            buyer.RegistrationAddress = shippingAddress.Address1;
            buyer.Ip = CommonServices.WebHelper.GetCurrentIpAddress();
            buyer.City = billingAddress.StateProvince.Name;
            buyer.Country = billingAddress.Country.Name;
            buyer.ZipCode = billingAddress.ZipPostalCode;
            request.Buyer = buyer;

            Iyzipay.Model.Address iyzicoShippingAddress = new Iyzipay.Model.Address();
            iyzicoShippingAddress.ContactName = shippingAddress.FirstName + " " + shippingAddress.LastName;
            iyzicoShippingAddress.City = shippingAddress.StateProvince.Name;
            iyzicoShippingAddress.Country = shippingAddress.Country.Name;
            iyzicoShippingAddress.Description = shippingAddress.Address1 + " " + shippingAddress.Address2;
            iyzicoShippingAddress.ZipCode = shippingAddress.ZipPostalCode;
            request.ShippingAddress = iyzicoShippingAddress;

            var iyzicoBillingAddress = new Iyzipay.Model.Address();
            iyzicoBillingAddress.ContactName = billingAddress.FirstName + " " + billingAddress.LastName;
            iyzicoBillingAddress.City = billingAddress.StateProvince.Name;
            iyzicoBillingAddress.Country = billingAddress.Country.Name;
            iyzicoBillingAddress.Description = billingAddress.Address1 + " " + billingAddress.Address2;
            iyzicoBillingAddress.ZipCode = billingAddress.ZipPostalCode;
            request.BillingAddress = iyzicoBillingAddress;

            var cartItems = currentCustomer.GetCartItems(Core.Domain.Orders.ShoppingCartType.ShoppingCart, _storeContext.CurrentStore.Id);
            List<BasketItem> basketItems = new List<BasketItem>();
            foreach (var item in cartItems)
            {
                BasketItem firstBasketItem = new BasketItem();
                firstBasketItem.Id = item.Item.ProductId + "";

                firstBasketItem.Name = item.Item.Product.Name;
                firstBasketItem.ItemType = BasketItemType.PHYSICAL.ToString();
                firstBasketItem.Price = (item.Item.Product.Price * item.Item.Quantity).ToString("N2").Replace(",", ".");
                var categoryName = "Product";
                var selectedCategory = item.Item.Product.ProductCategories.FirstOrDefault();
                if (selectedCategory != null) categoryName = selectedCategory.Category.Name;
                firstBasketItem.Category1 = categoryName;
                basketItems.Add(firstBasketItem);
            }
            request.BasketItems = basketItems;
            Payment payment = Payment.Create(request, options);

            if (!string.IsNullOrEmpty(payment.ErrorCode))
            {
                result.AddError(payment.ErrorMessage);
            }

            //send request
            //using (var service = new PayPalAPIAASoapBinding())
            //{
            //    service.Url = iyzicoHelper.GetPaypalServiceUrl(settings);
            //    service.RequesterCredentials = iyzicoHelper.GetPaypalApiCredentials(settings);
            //    DoDirectPaymentResponseType response = service.DoDirectPayment(req);

            //    string error = "";
            //    bool success = iyzicoHelper.CheckSuccess(Helper, response, out error);
            //    if (success)
            //    {
            //        result.AvsResult = response.AVSCode;
            //        result.AuthorizationTransactionCode = response.CVV2Code;
            //        if (settings.TransactMode == TransactMode.Authorize)
            //        {
            //            result.AuthorizationTransactionId = response.TransactionID;
            //            result.AuthorizationTransactionResult = response.Ack.ToString();

            //            result.NewPaymentStatus = PaymentStatus.Authorized;
            //        }
            //        else
            //        {
            //            result.CaptureTransactionId = response.TransactionID;
            //            result.CaptureTransactionResult = response.Ack.ToString();

            //            result.NewPaymentStatus = PaymentStatus.Paid;
            //        }
            //    }
            //    else
            //    {
            //        result.AddError(error);
            //    }
            //}
            return result;
        }

        /// <summary>
        /// Process recurring payment
        /// </summary>
        /// <param name="processPaymentRequest">Payment info required for an order processing</param>
        /// <returns>Process payment result</returns>
        public override ProcessPaymentResult ProcessRecurringPayment(ProcessPaymentRequest processPaymentRequest)
        {
            var result = new ProcessPaymentResult();

            var customer = _customerService.GetCustomerById(processPaymentRequest.CustomerId);
            var settings = CommonServices.Settings.LoadSetting<iyzicoDirectPaymentSettings>(processPaymentRequest.StoreId);

            var req = new CreateRecurringPaymentsProfileReq();
            req.CreateRecurringPaymentsProfileRequest = new CreateRecurringPaymentsProfileRequestType();
            req.CreateRecurringPaymentsProfileRequest.Version = iyzicoHelper.GetApiVersion();
            var details = new CreateRecurringPaymentsProfileRequestDetailsType();
            req.CreateRecurringPaymentsProfileRequest.CreateRecurringPaymentsProfileRequestDetails = details;

            details.CreditCard = new CreditCardDetailsType();
            details.CreditCard.CreditCardNumber = processPaymentRequest.CreditCardNumber;
            details.CreditCard.CreditCardType = iyzicoHelper.GetPaypalCreditCardType(processPaymentRequest.CreditCardType);
            details.CreditCard.ExpMonthSpecified = true;
            details.CreditCard.ExpMonth = processPaymentRequest.CreditCardExpireMonth;
            details.CreditCard.ExpYearSpecified = true;
            details.CreditCard.ExpYear = processPaymentRequest.CreditCardExpireYear;
            details.CreditCard.CVV2 = processPaymentRequest.CreditCardCvv2;
            details.CreditCard.CardOwner = new PayerInfoType();
            details.CreditCard.CardOwner.PayerCountry = iyzicoHelper.GetPaypalCountryCodeType(customer.BillingAddress.Country);
            details.CreditCard.CreditCardTypeSpecified = true;

            details.CreditCard.CardOwner.Address = new AddressType();
            details.CreditCard.CardOwner.Address.CountrySpecified = true;
            details.CreditCard.CardOwner.Address.Street1 = customer.BillingAddress.Address1;
            details.CreditCard.CardOwner.Address.Street2 = customer.BillingAddress.Address2;
            details.CreditCard.CardOwner.Address.CityName = customer.BillingAddress.City;
            if (customer.BillingAddress.StateProvince != null)
                details.CreditCard.CardOwner.Address.StateOrProvince = customer.BillingAddress.StateProvince.Abbreviation;
            else
                details.CreditCard.CardOwner.Address.StateOrProvince = "CA";
            details.CreditCard.CardOwner.Address.Country = iyzicoHelper.GetPaypalCountryCodeType(customer.BillingAddress.Country);
            details.CreditCard.CardOwner.Address.PostalCode = customer.BillingAddress.ZipPostalCode;
            details.CreditCard.CardOwner.Payer = customer.BillingAddress.Email;
            details.CreditCard.CardOwner.PayerName = new PersonNameType();
            details.CreditCard.CardOwner.PayerName.FirstName = customer.BillingAddress.FirstName;
            details.CreditCard.CardOwner.PayerName.LastName = customer.BillingAddress.LastName;

            //start date
            details.RecurringPaymentsProfileDetails = new RecurringPaymentsProfileDetailsType();
            details.RecurringPaymentsProfileDetails.BillingStartDate = DateTime.UtcNow;
            details.RecurringPaymentsProfileDetails.ProfileReference = processPaymentRequest.OrderGuid.ToString();

            //schedule
            details.ScheduleDetails = new ScheduleDetailsType();
            details.ScheduleDetails.Description = Helper.GetResource("RecurringPayment");
            details.ScheduleDetails.PaymentPeriod = new BillingPeriodDetailsType();
            details.ScheduleDetails.PaymentPeriod.Amount = new BasicAmountType();
            details.ScheduleDetails.PaymentPeriod.Amount.Value = Math.Round(processPaymentRequest.OrderTotal, 2).ToString("N", new CultureInfo("en-us"));
            details.ScheduleDetails.PaymentPeriod.Amount.currencyID = iyzicoHelper.GetPaypalCurrency(_currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId));
            details.ScheduleDetails.PaymentPeriod.BillingFrequency = processPaymentRequest.RecurringCycleLength;
            switch (processPaymentRequest.RecurringCyclePeriod)
            {
                case RecurringProductCyclePeriod.Days:
                    details.ScheduleDetails.PaymentPeriod.BillingPeriod = BillingPeriodType.Day;
                    break;
                case RecurringProductCyclePeriod.Weeks:
                    details.ScheduleDetails.PaymentPeriod.BillingPeriod = BillingPeriodType.Week;
                    break;
                case RecurringProductCyclePeriod.Months:
                    details.ScheduleDetails.PaymentPeriod.BillingPeriod = BillingPeriodType.Month;
                    break;
                case RecurringProductCyclePeriod.Years:
                    details.ScheduleDetails.PaymentPeriod.BillingPeriod = BillingPeriodType.Year;
                    break;
                default:
                    throw new SmartException(Helper.GetResource("NotSupportedPeriod"));
            }
            details.ScheduleDetails.PaymentPeriod.TotalBillingCycles = processPaymentRequest.RecurringTotalCycles;
            details.ScheduleDetails.PaymentPeriod.TotalBillingCyclesSpecified = true;

            //using (var service = new iyzicoAPIAASoapBinding())
            //{
            //    service.Url = iyzicoHelper.GetPaypalServiceUrl(settings);
            //    service.RequesterCredentials = iyzicoHelper.GetPaypalApiCredentials(settings);
            //    CreateRecurringPaymentsProfileResponseType response = service.CreateRecurringPaymentsProfile(req);

            //    string error = "";
            //    bool success = iyzicoHelper.CheckSuccess(Helper, response, out error);
            //    if (success)
            //    {
            //        result.NewPaymentStatus = PaymentStatus.Pending;
            //        if (response.CreateRecurringPaymentsProfileResponseDetails != null)
            //        {
            //            result.SubscriptionTransactionId = response.CreateRecurringPaymentsProfileResponseDetails.ProfileID;
            //        }
            //    }
            //    else
            //    {
            //        result.AddError(error);
            //    }
            //}

            return result;
        }

        protected override string GetControllerName()
        {
            return "iyzicoDirect";
        }

        public override Type GetControllerType()
        {
            return typeof(iyzicoDirectController);
        }

        #endregion

        #region Properties

        public override bool RequiresInteraction
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Gets a recurring payment type of payment method
        /// </summary>
        public override RecurringPaymentType RecurringPaymentType
        {
            get
            {
                return RecurringPaymentType.Automatic;
            }
        }

        /// <summary>
        /// Gets a payment method type
        /// </summary>
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