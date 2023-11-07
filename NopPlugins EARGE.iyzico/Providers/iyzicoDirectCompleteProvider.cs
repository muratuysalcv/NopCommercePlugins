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

namespace EARGE.iyzico
{
	/// <summary>
	/// iyzicoDirect provider
	/// </summary>
    [SystemName("Payments.iyzicoDirectComplete")]
    [FriendlyName("iyzico Direct Complete")]
    [DisplayOrder(1)]
    public class iyzicoDirectCompleteProvider : iyzicoProviderBase<iyzicoDirectPaymentSettings>
	{
		#region Fields

		private readonly ICurrencyService _currencyService;
		private readonly ICustomerService _customerService;
		private readonly CurrencySettings _currencySettings;

		#endregion

		#region Ctor

        public iyzicoDirectCompleteProvider(ICurrencyService currencyService, 
            ICustomerService customerService,
			CurrencySettings currencySettings, 
            IComponentContext ctx)
		{
			_currencyService = currencyService;
			_customerService = customerService;
			_currencySettings = currencySettings;
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
            
            var req = new DoDirectPaymentReq();
            req.DoDirectPaymentRequest = new DoDirectPaymentRequestType();
            req.DoDirectPaymentRequest.Version = iyzicoHelper.GetApiVersion();

            var details = new DoDirectPaymentRequestDetailsType();
            req.DoDirectPaymentRequest.DoDirectPaymentRequestDetails = details;
            details.IPAddress = CommonServices.WebHelper.GetCurrentIpAddress();

			if (details.IPAddress.IsEmpty())
                details.IPAddress = "127.0.0.1";

            if (settings.TransactMode == TransactMode.Authorize)
                details.PaymentAction = PaymentActionCodeType.Authorization;
            else
                details.PaymentAction = PaymentActionCodeType.Sale;

            //credit card
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
            //billing address
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
            //order totals
            var payPalCurrency = iyzicoHelper.GetPaypalCurrency(_currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId));
            details.PaymentDetails = new PaymentDetailsType();
            details.PaymentDetails.OrderTotal = new BasicAmountType();
            details.PaymentDetails.OrderTotal.Value = Math.Round(processPaymentRequest.OrderTotal, 2).ToString("N", new CultureInfo("en-us"));
            details.PaymentDetails.OrderTotal.currencyID = payPalCurrency;
            details.PaymentDetails.Custom = processPaymentRequest.OrderGuid.ToString();
            details.PaymentDetails.ButtonSource = EARGEAppVersion.CurrentFullVersion;
            //shipping
            if (customer.ShippingAddress != null)
            {
                if (customer.ShippingAddress.StateProvince != null && customer.ShippingAddress.Country != null)
                {
                    var shippingAddress = new AddressType();
                    shippingAddress.Name = customer.ShippingAddress.FirstName + " " + customer.ShippingAddress.LastName;
                    shippingAddress.Street1 = customer.ShippingAddress.Address1;
                    shippingAddress.CityName = customer.ShippingAddress.City;
                    shippingAddress.StateOrProvince = customer.ShippingAddress.StateProvince.Abbreviation;
                    shippingAddress.PostalCode = customer.ShippingAddress.ZipPostalCode;
                    shippingAddress.Country = (CountryCodeType)Enum.Parse(typeof(CountryCodeType), customer.ShippingAddress.Country.TwoLetterIsoCode, true);
                    shippingAddress.CountrySpecified = true;
                    details.PaymentDetails.ShipToAddress = shippingAddress;
                }
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