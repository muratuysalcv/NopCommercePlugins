using EARGE.Core.Configuration;
using EARGE.iyzico;

namespace EARGE.iyzico.Settings
{
    public abstract class iyzicoSettingsBase
    {
        public bool UseSandbox { get; set; }
        /// <summary>
        /// Gets or sets a value indicating whether to "additional fee" is specified as percentage. true - percentage, false - fixed value.
        /// </summary>
        public bool AdditionalFeePercentage { get; set; }
        /// <summary>
        /// Additional fee
        /// </summary>
        public decimal AdditionalFee { get; set; }
    }

    public abstract class iyzicoApiSettingsBase : iyzicoSettingsBase
	{
		public TransactMode TransactMode { get; set; }
		public string ApiKey { get; set; }
		public string SecretKey { get; set; }
		
        public string ApiKey_TEST { get; set; }
        public string SecretKey_TEST { get; set; }

        public string ApiAccountName { get; set; }
        public string Signature { get; set; }
        public string ApiAccountPassword { get; set; }
        
    }

    public class iyzicoDirectPaymentSettings : iyzicoApiSettingsBase, ISettings
    {
		public iyzicoDirectPaymentSettings()
		{
			TransactMode = TransactMode.Authorize;
            UseSandbox = true;
		}
    }

    public class iyzicoExpressPaymentSettings : iyzicoApiSettingsBase, ISettings 
    {
		public iyzicoExpressPaymentSettings()
		{
			UseSandbox = true;
            TransactMode = TransactMode.Authorize;
		}

        /// <summary>
        /// Determines whether the checkout button is displayed beneath the cart
        /// </summary>
        public bool DisplayCheckoutButton { get; set; }

        /// <summary>
        /// Determines whether the shipment address has  to be confirmed by iyzico 
        /// </summary>
        public bool ConfirmedShipment { get; set; }

        /// <summary>
        /// Determines whether the shipment address is transmitted to iyzico
        /// </summary>
        public bool NoShipmentAddress { get; set; }

        /// <summary>
        /// Callback timeout
        /// </summary>
        public int CallbackTimeout { get; set; }

        /// <summary>
        /// Default shipping price
        /// </summary>
        public decimal DefaultShippingPrice { get; set; }
    }

    public class iyzicoStandardPaymentSettings : iyzicoSettingsBase, ISettings
    {
		public iyzicoStandardPaymentSettings()
		{
			UseSandbox = true;
            PdtValidateOrderTotal = true;
            EnableIpn = true;
		}

        public string BusinessEmail { get; set; }
        public string PdtToken { get; set; }
        public bool PassProductNamesAndTotals { get; set; }
        public bool PdtValidateOrderTotal { get; set; }
        public bool EnableIpn { get; set; }
        public string IpnUrl { get; set; }
    }

    /// <summary>
    /// Represents payment processor transaction mode
    /// </summary>
    public enum TransactMode : int
    {
        /// <summary>
        /// Authorize
        /// </summary>
        Authorize = 1,
        /// <summary>
        /// Authorize and capture
        /// </summary>
        AuthorizeAndCapture = 2
    }
}
