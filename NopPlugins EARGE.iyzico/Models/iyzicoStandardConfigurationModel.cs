using EARGE.iyzico.Settings;
using EARGE.Web.Framework;
using EARGE.Web.Framework.Mvc;

namespace EARGE.iyzico.Models
{
    public class iyzicoStandardConfigurationModel : ModelBase
	{
        [SmartResourceDisplayName("Plugins.Payments.iyzico.UseSandbox")]
		public bool UseSandbox { get; set; }

		[SmartResourceDisplayName("Plugins.Payments.iyzicoStandard.Fields.BusinessEmail")]
		public string BusinessEmail { get; set; }

		[SmartResourceDisplayName("Plugins.Payments.iyzicoStandard.Fields.PDTToken")]
		public string PdtToken { get; set; }

		[SmartResourceDisplayName("Plugins.Payments.iyzicoStandard.Fields.PDTValidateOrderTotal")]
		public bool PdtValidateOrderTotal { get; set; }

		[SmartResourceDisplayName("Plugins.Payments.iyzicoStandard.Fields.AdditionalFee")]
		public decimal AdditionalFee { get; set; }

		[SmartResourceDisplayName("Plugins.Payments.iyzicoStandard.Fields.AdditionalFeePercentage")]
		public bool AdditionalFeePercentage { get; set; }

		[SmartResourceDisplayName("Plugins.Payments.iyzicoStandard.Fields.PassProductNamesAndTotals")]
		public bool PassProductNamesAndTotals { get; set; }

		[SmartResourceDisplayName("Plugins.Payments.iyzicoStandard.Fields.EnableIpn")]
		public bool EnableIpn { get; set; }

		[SmartResourceDisplayName("Plugins.Payments.iyzicoStandard.Fields.IpnUrl")]
		public string IpnUrl { get; set; }

        public void Copy(iyzicoStandardPaymentSettings settings, bool fromSettings)
        {
            if (fromSettings)
            {
                UseSandbox = settings.UseSandbox;
                BusinessEmail = settings.BusinessEmail;
                PdtToken = settings.PdtToken;
                PdtValidateOrderTotal = settings.PdtValidateOrderTotal;
                AdditionalFee = settings.AdditionalFee;
                AdditionalFeePercentage = settings.AdditionalFeePercentage;
                PassProductNamesAndTotals = settings.PassProductNamesAndTotals;
                EnableIpn = settings.EnableIpn;
                IpnUrl = settings.IpnUrl;
            }
            else
            {
                settings.UseSandbox = UseSandbox;
                settings.BusinessEmail = BusinessEmail;
                settings.PdtToken = PdtToken;
                settings.PdtValidateOrderTotal = PdtValidateOrderTotal;
                settings.AdditionalFee = AdditionalFee;
                settings.AdditionalFeePercentage = AdditionalFeePercentage;
                settings.PassProductNamesAndTotals = PassProductNamesAndTotals;
                settings.EnableIpn = EnableIpn;
                settings.IpnUrl = IpnUrl;
            }

        }
	}
}