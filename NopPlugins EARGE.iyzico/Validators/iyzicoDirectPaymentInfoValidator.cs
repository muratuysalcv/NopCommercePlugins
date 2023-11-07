﻿using FluentValidation;
using EARGE.iyzico.Models;
using EARGE.Services.Localization;
using EARGE.Web.Framework.Validators;

namespace EARGE.iyzico.Validators
{
	public class PaymentInfoValidator : AbstractValidator<iyzicoDirectPaymentInfoModel>
	{
		public PaymentInfoValidator(ILocalizationService localizationService) {
			//useful links:
			//http://fluentvalidation.codeplex.com/wikipage?title=Custom&referringTitle=Documentation&ANCHOR#CustomValidator
			//http://benjii.me/2010/11/credit-card-validator-attribute-for-asp-net-mvc-3/

			RuleFor(x => x.CardholderName).NotEmpty().WithMessage(localizationService.GetResource("Payment.CardholderName.Required"));
			RuleFor(x => x.CardNumber).IsCreditCard().WithMessage(localizationService.GetResource("Payment.CardNumber.Wrong"));
			RuleFor(x => x.CardCode).Matches(@"^[0-9]{3,4}$").WithMessage(localizationService.GetResource("Payment.CardCode.Wrong"));
			RuleFor(x => x.ExpireMonth).NotEmpty().WithMessage(localizationService.GetResource("Payment.ExpireMonth.Required"));
			RuleFor(x => x.ExpireYear).NotEmpty().WithMessage(localizationService.GetResource("Payment.ExpireYear.Required"));
		}
	}
}