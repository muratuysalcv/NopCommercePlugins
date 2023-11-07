using System;
using System.Web.Mvc;
using EARGE.iyzico.Settings;
using EARGE.Web.Framework;
using EARGE.Web.Framework.Mvc;

namespace EARGE.iyzico.Models
{
    public abstract class ApiConfigurationModel: ModelBase
	{
        public string[] ConfigGroups { get; set; }

        [SmartResourceDisplayName("Plugins.Payments.iyzico.UseSandbox")]
		public bool UseSandbox { get; set; }

		[SmartResourceDisplayName("Plugins.Payments.iyzico.TransactMode")]
		public int TransactMode { get; set; }
		public SelectList TransactModeValues { get; set; }

		[SmartResourceDisplayName("Plugins.Payments.iyzico.ApiAccountName")]
		public string ApiAccountName { get; set; }

		[SmartResourceDisplayName("Plugins.Payments.iyzico.ApiAccountPassword")]
		public string ApiAccountPassword { get; set; }

		[SmartResourceDisplayName("Plugins.Payments.iyzico.Signature")]
		public string Signature { get; set; }

		[SmartResourceDisplayName("Plugins.Payments.iyzico.AdditionalFee")]
		public decimal AdditionalFee { get; set; }

		[SmartResourceDisplayName("Plugins.Payments.iyzico.AdditionalFeePercentage")]
		public bool AdditionalFeePercentage { get; set; }

        [SmartResourceDisplayName("Plugins.Payments.iyzicoExpress.Fields.ApiKey")]
        public string ApiKey { get; set; }
        [SmartResourceDisplayName("Plugins.Payments.iyzicoExpress.Fields.SecretKey")]
        public string SecretKey { get; set; }


        [SmartResourceDisplayName("Plugins.Payments.iyzicoExpress.Fields.ApiKeyTest")]
        public string ApiKey_TEST { get; set; }
        [SmartResourceDisplayName("Plugins.Payments.iyzicoExpress.Fields.SecretKeyTest")]
        public string SecretKey_TEST { get; set; }

	}

    public class iyzicoDirectConfigurationModel : ApiConfigurationModel
    {
        public void Copy(iyzicoDirectPaymentSettings settings, bool fromSettings)
        {
            if (fromSettings)
            {
                UseSandbox = settings.UseSandbox;
                TransactMode = Convert.ToInt32(settings.TransactMode);
                ApiAccountName = settings.ApiAccountName;
                ApiAccountPassword = settings.ApiAccountPassword;
                Signature = settings.Signature;
                AdditionalFee = settings.AdditionalFee;
                AdditionalFeePercentage = settings.AdditionalFeePercentage;
                ApiKey = settings.ApiKey;
                SecretKey = settings.SecretKey;
                ApiKey_TEST = settings.ApiKey_TEST;
                SecretKey_TEST = settings.SecretKey_TEST;
            }
            else
            {
                settings.UseSandbox = UseSandbox;
                settings.TransactMode = (TransactMode)TransactMode;
                settings.ApiAccountName = ApiAccountName;
                settings.ApiAccountPassword = ApiAccountPassword;
                settings.Signature = Signature;
                settings.AdditionalFee = AdditionalFee;
                settings.AdditionalFeePercentage = AdditionalFeePercentage;
                settings.ApiKey = ApiKey;
                settings.ApiKey_TEST = ApiKey_TEST;
                settings.SecretKey = SecretKey;
                settings.SecretKey_TEST = SecretKey_TEST;
            }
        }
    }

    public class iyzicoExpressConfigurationModel : ApiConfigurationModel
    {
        [SmartResourceDisplayName("Plugins.Payments.iyzicoExpress.Fields.DisplayCheckoutButton")]
        public bool DisplayCheckoutButton { get; set; }

        [SmartResourceDisplayName("Plugins.Payments.iyzicoExpress.Fields.ConfirmedShipment")]
        public bool ConfirmedShipment { get; set; }

        [SmartResourceDisplayName("Plugins.Payments.iyzicoExpress.Fields.NoShipmentAddress")]
        public bool NoShipmentAddress { get; set; }

        [SmartResourceDisplayName("Plugins.Payments.iyzicoExpress.Fields.CallbackTimeout")]
        public int CallbackTimeout { get; set; }

        [SmartResourceDisplayName("Plugins.Payments.iyzicoExpress.Fields.DefaultShippingPrice")]
        public decimal DefaultShippingPrice { get; set; }


    



        public void Copy(iyzicoExpressPaymentSettings settings, bool fromSettings)
        {
            if (fromSettings)
            {
                UseSandbox = settings.UseSandbox;
                TransactMode = Convert.ToInt32(settings.TransactMode);
                ApiAccountName = settings.ApiAccountName;
                ApiAccountPassword = settings.ApiAccountPassword;
                Signature = settings.Signature;
                AdditionalFee = settings.AdditionalFee;
                AdditionalFeePercentage = settings.AdditionalFeePercentage;
                DisplayCheckoutButton = settings.DisplayCheckoutButton;
                ConfirmedShipment = settings.ConfirmedShipment;
                NoShipmentAddress = settings.NoShipmentAddress;
                CallbackTimeout = settings.CallbackTimeout;
                DefaultShippingPrice = settings.DefaultShippingPrice;
                ApiKey=settings.ApiKey;
                SecretKey=settings.SecretKey;
                ApiKey_TEST=settings.ApiKey_TEST;
                SecretKey_TEST = settings.SecretKey_TEST;
            }
            else
			{
                settings.UseSandbox = UseSandbox;
                settings.TransactMode = (TransactMode)TransactMode;
                settings.ApiAccountName = ApiAccountName;
                settings.ApiAccountPassword = ApiAccountPassword;
                settings.Signature = Signature;
                settings.AdditionalFee = AdditionalFee;
                settings.AdditionalFeePercentage = AdditionalFeePercentage;
                settings.DisplayCheckoutButton = DisplayCheckoutButton;
                settings.ConfirmedShipment = ConfirmedShipment;
                settings.NoShipmentAddress = NoShipmentAddress;
                settings.CallbackTimeout = CallbackTimeout;
                settings.DefaultShippingPrice = DefaultShippingPrice;
                settings.ApiKey = ApiKey;
                settings.ApiKey_TEST = ApiKey_TEST;
                settings.SecretKey = SecretKey;
                settings.SecretKey_TEST = SecretKey_TEST;
            }
        }

    }


}