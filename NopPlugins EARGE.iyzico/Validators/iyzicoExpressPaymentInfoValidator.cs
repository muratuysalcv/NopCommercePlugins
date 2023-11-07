using FluentValidation;
using EARGE.iyzico.Models;
using EARGE.Services.Localization;
using EARGE.Web.Framework.Validators;

namespace EARGE.iyzico.Validators
{
	public class iyzicoExpressPaymentInfoValidator : AbstractValidator<iyzicoExpressPaymentInfoModel>
	{
		public iyzicoExpressPaymentInfoValidator(ILocalizationService localizationService) {

		}
	}
}