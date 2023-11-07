using EARGE.Core.Plugins;
using EARGE.iyzico.Settings;
using EARGE.Services.Configuration;
using EARGE.Services.Localization;

namespace EARGE.iyzico
{
	public class Plugin : BasePlugin
	{
		private readonly ISettingService _settingService;
		private readonly ILocalizationService _localizationService;

		public Plugin(
			ISettingService settingService,
			ILocalizationService localizationService)
		{
			_settingService = settingService;
			_localizationService = localizationService;
		}

		public override void Install()
		{
			_settingService.SaveSetting<iyzicoExpressPaymentSettings>(new iyzicoExpressPaymentSettings());
			_settingService.SaveSetting<iyzicoDirectPaymentSettings>(new iyzicoDirectPaymentSettings());
			_settingService.SaveSetting<iyzicoStandardPaymentSettings>(new iyzicoStandardPaymentSettings());

			_localizationService.ImportPluginResourcesFromXml(this.PluginDescriptor);

			base.Install();
		}

		public override void Uninstall()
		{
            _settingService.DeleteSetting<iyzicoExpressPaymentSettings>();
            _settingService.DeleteSetting<iyzicoDirectPaymentSettings>();
            _settingService.DeleteSetting<iyzicoStandardPaymentSettings>();

			_localizationService.DeleteLocaleStringResources(PluginDescriptor.ResourceRootKey);

			base.Uninstall();
		}
	}
}
