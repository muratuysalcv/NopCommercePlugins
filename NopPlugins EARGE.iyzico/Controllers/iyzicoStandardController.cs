﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using EARGE.Core;
using EARGE.Core.Domain.Orders;
using EARGE.Core.Domain.Payments;
using EARGE.Core.Logging;
using EARGE.iyzico.Models;
using EARGE.iyzico.Settings;
using EARGE.Services;
using EARGE.Services.Localization;
using EARGE.Services.Orders;
using EARGE.Services.Payments;
using EARGE.Services.Stores;
using EARGE.Web.Framework.Controllers;
using EARGE.Web.Framework.Settings;
using EARGE.Web.Framework.Plugins;
using Autofac;
using EARGE.iyzico.Validators;

namespace EARGE.iyzico.Controllers
{
	public class iyzicoStandardController : PaymentControllerBase
	{
        private readonly PluginHelper _helper;
		private readonly IPaymentService _paymentService;
		private readonly IOrderService _orderService;
		private readonly IOrderProcessingService _orderProcessingService;
		private readonly IStoreContext _storeContext;
		private readonly IWorkContext _workContext;
		private readonly IWebHelper _webHelper;
		private readonly PaymentSettings _paymentSettings;
		private readonly ILocalizationService _localizationService;
        private readonly ICommonServices _services;
        private readonly IStoreService _storeService;
        private readonly IComponentContext ctx;


        public iyzicoStandardController(
			IPaymentService paymentService, IOrderService orderService,
			IOrderProcessingService orderProcessingService,
			IStoreContext storeContext,
			IWorkContext workContext,
			IWebHelper webHelper,
			PaymentSettings paymentSettings,
            ILocalizationService localizationService, 
            ICommonServices services,
            IStoreService storeService)
		{
			_paymentService = paymentService;
			_orderService = orderService;
			_orderProcessingService = orderProcessingService;
			_storeContext = storeContext;
			_workContext = workContext;
			_webHelper = webHelper;
			_paymentSettings = paymentSettings;
			_localizationService = localizationService;
            _services = services;
            _storeService = storeService;
            _helper = new PluginHelper(ctx, "EARGE.iyzico", "Plugins.Payments.iyzicoStandard");
		}

		[AdminAuthorize, ChildActionOnly]
		public ActionResult Configure()
		{
            var model = new iyzicoStandardConfigurationModel();
            int storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _services.WorkContext);
            var settings = _services.Settings.LoadSetting<iyzicoStandardPaymentSettings>(storeScope);

            model.Copy(settings, true);

            var storeDependingSettingHelper = new StoreDependingSettingHelper(ViewData);
            storeDependingSettingHelper.GetOverrideKeys(settings, model, storeScope, _services.Settings);

            return View(model);
		}

		[HttpPost, AdminAuthorize, ChildActionOnly]
        public ActionResult Configure(iyzicoStandardConfigurationModel model, FormCollection form)
		{
            if (!ModelState.IsValid)
                return Configure();

			ModelState.Clear();

            var storeDependingSettingHelper = new StoreDependingSettingHelper(ViewData);
            int storeScope = this.GetActiveStoreScopeConfiguration(_storeService, _services.WorkContext);
            var settings = _services.Settings.LoadSetting<iyzicoStandardPaymentSettings>(storeScope);

            model.Copy(settings, false);

            storeDependingSettingHelper.UpdateSettings(settings, form, storeScope, _services.Settings);

			// multistore context not possible, see IPN handling
			_services.Settings.SaveSetting(settings, x => x.UseSandbox, 0, false);

            _services.Settings.ClearCache();
            NotifySuccess(_services.Localization.GetResource("Plugins.Payments.iyzico.ConfigSaveNote"));

            return Configure();
		}

		public ActionResult PaymentInfo()
		{
            var model = new iyzicoDirectPaymentInfoModel();

            //CC types
            model.CreditCardTypes.Add(new SelectListItem() {
                Text = "Visa",
                Value = "Visa",
            });
            model.CreditCardTypes.Add(new SelectListItem() {
                Text = "Master card",
                Value = "MasterCard",
            });
            model.CreditCardTypes.Add(new SelectListItem() {
                Text = "Discover",
                Value = "Discover",
            });
            model.CreditCardTypes.Add(new SelectListItem() {
                Text = "Amex",
                Value = "Amex",
            });

            //years
            for (int i = 0; i < 15; i++) {
                string year = Convert.ToString(DateTime.Now.Year + i);
                model.ExpireYears.Add(new SelectListItem() {
                    Text = year,
                    Value = year,
                });
            }

            //months
            for (int i = 1; i <= 12; i++) {
                string text = (i < 10) ? "0" + i.ToString() : i.ToString();
                model.ExpireMonths.Add(new SelectListItem() {
                    Text = text,
                    Value = i.ToString(),
                });
            }

            //set postback values
            var form = this.GetPaymentData();
            model.CardholderName = form["CardholderName"];
            model.CardNumber = form["CardNumber"];
            model.CardCode = form["CardCode"];
            var selectedCcType = model.CreditCardTypes.Where(x => x.Value.Equals(form["CreditCardType"], StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            if (selectedCcType != null)
                selectedCcType.Selected = true;
            var selectedMonth = model.ExpireMonths.Where(x => x.Value.Equals(form["ExpireMonth"], StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            if (selectedMonth != null)
                selectedMonth.Selected = true;
            var selectedYear = model.ExpireYears.Where(x => x.Value.Equals(form["ExpireYear"], StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();
            if (selectedYear != null)
                selectedYear.Selected = true;

            return PartialView(model);
        }

		[NonAction]
		public override IList<string> ValidatePaymentForm(FormCollection form)
		{
            var warnings = new List<string>();

            //validate
            var validator = new PaymentInfoValidator(_services.Localization);
            var model = new iyzicoDirectPaymentInfoModel() {
                CardholderName = form["CardholderName"],
                CardNumber = form["CardNumber"],
                CardCode = form["CardCode"],
                ExpireMonth = form["ExpireMonth"],
                ExpireYear = form["ExpireYear"]
            };

            var validationResult = validator.Validate(model);
            if (!validationResult.IsValid)
                foreach (var error in validationResult.Errors)
                    warnings.Add(error.ErrorMessage);
            return warnings;
        }

		[NonAction]
		public override ProcessPaymentRequest GetPaymentInfo(FormCollection form)
		{
            var paymentInfo = new ProcessPaymentRequest();
            paymentInfo.CreditCardType = form["CreditCardType"];
            paymentInfo.CreditCardName = form["CardholderName"];
            paymentInfo.CreditCardNumber = form["CardNumber"];
            paymentInfo.CreditCardExpireMonth = int.Parse(form["ExpireMonth"]);
            paymentInfo.CreditCardExpireYear = int.Parse(form["ExpireYear"]);
            paymentInfo.CreditCardCvv2 = form["CardCode"];
            return paymentInfo;
        }

        [NonAction]
        public override string GetPaymentSummary(FormCollection form) {
            var number = form["CardNumber"];
            return "{0}, {1}, {2}".FormatCurrent(
                form["CreditCardType"],
                form["CardholderName"],
                number.Mask(4)
            );
        }

        [ValidateInput(false)]
		public ActionResult PDTHandler(FormCollection form)
		{
			string tx = _webHelper.QueryString<string>("tx");
			Dictionary<string, string> values;
			string response;

			var provider = _paymentService.LoadPaymentMethodBySystemName("Payments.iyzicoStandard", true);
            var processor = provider != null ? provider.Value as iyzicoStandardProvider : null;
			if (processor == null)
				throw new SmartException(_localizationService.GetResource("Plugins.Payments.iyzicoStandard.NoModuleLoading"));

            var settings = _services.Settings.LoadSetting<iyzicoStandardPaymentSettings>();

			if (processor.GetPDTDetails(tx, settings, out values, out response))
			{
				string orderNumber = string.Empty;
				values.TryGetValue("custom", out orderNumber);
				Guid orderNumberGuid = Guid.Empty;
				try
				{
					orderNumberGuid = new Guid(orderNumber);
				}
				catch { }
				Order order = _orderService.GetOrderByGuid(orderNumberGuid);
				if (order != null)
				{
					decimal total = decimal.Zero;
					try
					{
						total = decimal.Parse(values["mc_gross"], new CultureInfo("en-US"));
					}
					catch (Exception exc)
					{
						Logger.Error(_localizationService.GetResource("Plugins.Payments.iyzicoStandard.FailedGetGross"), exc);
					}

					string payer_status = string.Empty;
					values.TryGetValue("payer_status", out payer_status);
					string payment_status = string.Empty;
					values.TryGetValue("payment_status", out payment_status);
					string pending_reason = string.Empty;
					values.TryGetValue("pending_reason", out pending_reason);
					string mc_currency = string.Empty;
					values.TryGetValue("mc_currency", out mc_currency);
					string txn_id = string.Empty;
					values.TryGetValue("txn_id", out txn_id);
					string payment_type = string.Empty;
					values.TryGetValue("payment_type", out payment_type);
					string payer_id = string.Empty;
					values.TryGetValue("payer_id", out payer_id);
					string receiver_id = string.Empty;
					values.TryGetValue("receiver_id", out receiver_id);
					string invoice = string.Empty;
					values.TryGetValue("invoice", out invoice);
					string payment_fee = string.Empty;
					values.TryGetValue("payment_fee", out payment_fee);

					string paymentNote = _localizationService.GetResource("Plugins.Payments.iyzicoStandard.PaymentNote").FormatWith(
						total, mc_currency, payer_status, payment_status, pending_reason, txn_id, payment_type,	payer_id, receiver_id, invoice, payment_fee);

					//order note
					order.OrderNotes.Add(new OrderNote()
					{
						Note = paymentNote,
						DisplayToCustomer = false,
						CreatedOnUtc = DateTime.UtcNow
					});
					_orderService.UpdateOrder(order);

					//validate order total
                    if (settings.PdtValidateOrderTotal && !Math.Round(total, 2).Equals(Math.Round(order.OrderTotal, 2)))
					{
						Logger.Error(_localizationService.GetResource("Plugins.Payments.iyzicoStandard.UnequalTotalOrder").FormatWith(total, order.OrderTotal));

						return RedirectToAction("Index", "Home", new { area = "" });
					}

					//mark order as paid
					if (_orderProcessingService.CanMarkOrderAsPaid(order))
					{
						order.AuthorizationTransactionId = txn_id;
						_orderService.UpdateOrder(order);

						_orderProcessingService.MarkOrderAsPaid(order);
					}
				}

				return RedirectToAction("Completed", "Checkout", new { area = "" });
			}
			else
			{
				string orderNumber = string.Empty;
				values.TryGetValue("custom", out orderNumber);
				Guid orderNumberGuid = Guid.Empty;
				try
				{
					orderNumberGuid = new Guid(orderNumber);
				}
				catch { }
				Order order = _orderService.GetOrderByGuid(orderNumberGuid);
				if (order != null)
				{
					//order note
					order.OrderNotes.Add(new OrderNote()
					{
						Note = "{0} {1}".FormatWith(_localizationService.GetResource("Plugins.Payments.iyzicoStandard.PdtFailed"), response),
						DisplayToCustomer = false,
						CreatedOnUtc = DateTime.UtcNow
					});
					_orderService.UpdateOrder(order);
				}
				return RedirectToAction("Index", "Home", new { area = "" });
			}
		}

		[ValidateInput(false)]
		public ActionResult IPNHandler()
		{
			Debug.WriteLine("iyzico Standard IPN: {0}".FormatWith(Request.ContentLength));

			byte[] param = Request.BinaryRead(Request.ContentLength);
			string strRequest = Encoding.ASCII.GetString(param);
			Dictionary<string, string> values;

			var provider = _paymentService.LoadPaymentMethodBySystemName("Payments.iyzicoStandard", true);
			var processor = provider != null ? provider.Value as iyzicoStandardProvider : null;
			if (processor == null)
				throw new SmartException(_localizationService.GetResource("Plugins.Payments.iyzicoStandard.NoModuleLoading"));

			if (processor.VerifyIPN(strRequest, out values))
			{
				#region values
				decimal total = decimal.Zero;
				try
				{
					total = decimal.Parse(values["mc_gross"], new CultureInfo("en-US"));
				}
				catch { }

				string payer_status = string.Empty;
				values.TryGetValue("payer_status", out payer_status);
				string payment_status = string.Empty;
				values.TryGetValue("payment_status", out payment_status);
				string pending_reason = string.Empty;
				values.TryGetValue("pending_reason", out pending_reason);
				string mc_currency = string.Empty;
				values.TryGetValue("mc_currency", out mc_currency);
				string txn_id = string.Empty;
				values.TryGetValue("txn_id", out txn_id);
				string txn_type = string.Empty;
				values.TryGetValue("txn_type", out txn_type);
				string rp_invoice_id = string.Empty;
				values.TryGetValue("rp_invoice_id", out rp_invoice_id);
				string payment_type = string.Empty;
				values.TryGetValue("payment_type", out payment_type);
				string payer_id = string.Empty;
				values.TryGetValue("payer_id", out payer_id);
				string receiver_id = string.Empty;
				values.TryGetValue("receiver_id", out receiver_id);
				string invoice = string.Empty;
				values.TryGetValue("invoice", out invoice);
				string payment_fee = string.Empty;
				values.TryGetValue("payment_fee", out payment_fee);

				#endregion

				var sb = new StringBuilder();
				sb.AppendLine("iyzico IPN:");
				foreach (KeyValuePair<string, string> kvp in values)
				{
					sb.AppendLine(kvp.Key + ": " + kvp.Value);
				}

                var newPaymentStatus = GetPaymentStatus(payment_status, pending_reason);
				sb.AppendLine("{0}: {1}".FormatWith(_localizationService.GetResource("Plugins.Payments.iyzicoStandard.NewPaymentStatus"), newPaymentStatus));

				switch (txn_type)
				{
					case "recurring_payment_profile_created":
						//do nothing here
						break;
					case "recurring_payment":
						#region Recurring payment
						{
							Guid orderNumberGuid = Guid.Empty;
							try
							{
								orderNumberGuid = new Guid(rp_invoice_id);
							}
							catch { }

							var initialOrder = _orderService.GetOrderByGuid(orderNumberGuid);
							if (initialOrder != null)
							{
								var recurringPayments = _orderService.SearchRecurringPayments(0, 0, initialOrder.Id, null);
								foreach (var rp in recurringPayments)
								{
									switch (newPaymentStatus)
									{
										case PaymentStatus.Authorized:
										case PaymentStatus.Paid: {
												var recurringPaymentHistory = rp.RecurringPaymentHistory;
												if (recurringPaymentHistory.Count == 0)
												{
													//first payment
													var rph = new RecurringPaymentHistory()
													{
														RecurringPaymentId = rp.Id,
														OrderId = initialOrder.Id,
														CreatedOnUtc = DateTime.UtcNow
													};
													rp.RecurringPaymentHistory.Add(rph);
													_orderService.UpdateRecurringPayment(rp);
												}
												else
												{
													//next payments
													_orderProcessingService.ProcessNextRecurringPayment(rp);
												}
											}
											break;
									}
								}

								//this.OrderService.InsertOrderNote(newOrder.OrderId, sb.ToString(), DateTime.UtcNow);
								Logger.Information(_localizationService.GetResource("Plugins.Payments.iyzicoStandard.IpnLogInfo"), new SmartException(sb.ToString()));
							}
							else
							{
								Logger.Error(_localizationService.GetResource("Plugins.Payments.iyzicoStandard.IpnOrderNotFound"), new SmartException(sb.ToString()));
							}
						}
						#endregion
						break;
					default:
						#region Standard payment
						{
							string orderNumber = string.Empty;
							values.TryGetValue("custom", out orderNumber);
							Guid orderNumberGuid = Guid.Empty;
							try
							{
								orderNumberGuid = new Guid(orderNumber);
							}
							catch { }

							var order = _orderService.GetOrderByGuid(orderNumberGuid);
							if (order != null)
							{
								//order note
								order.OrderNotes.Add(new OrderNote()
								{
									Note = sb.ToString(),
									DisplayToCustomer = false,
									CreatedOnUtc = DateTime.UtcNow
								});
								_orderService.UpdateOrder(order);

								switch (newPaymentStatus)
								{
									case PaymentStatus.Pending:
										{
										}
										break;
									case PaymentStatus.Authorized: {
											if (_orderProcessingService.CanMarkOrderAsAuthorized(order))
											{
												_orderProcessingService.MarkAsAuthorized(order);
											}
										}
										break;
									case PaymentStatus.Paid:
										{
											if (_orderProcessingService.CanMarkOrderAsPaid(order))
											{

												order.AuthorizationTransactionId = txn_id;
												_orderService.UpdateOrder(order);

												_orderProcessingService.MarkOrderAsPaid(order);
											}
										}
										break;
									case PaymentStatus.Refunded: {
											if (_orderProcessingService.CanRefundOffline(order))
											{
												_orderProcessingService.RefundOffline(order);
											}
										}
										break;
									case PaymentStatus.Voided: {
											if (_orderProcessingService.CanVoidOffline(order))
											{
												_orderProcessingService.VoidOffline(order);
											}
										}
										break;
									default:
										break;
								}
							}
							else
							{
								Logger.Error(_localizationService.GetResource("Plugins.Payments.iyzicoStandard.IpnOrderNotFound"), new SmartException(sb.ToString()));
							}
						}
						#endregion
						break;
				}
			}
			else
			{
				Logger.Error(_localizationService.GetResource("Plugins.Payments.iyzicoStandard.IpnFailed"), new SmartException(strRequest));
			}

			//nothing should be rendered to visitor
			return Content("");
		}

        /// <summary>
        /// Gets a payment status
        /// </summary>
        /// <param name="paymentStatus">iyzico payment status</param>
        /// <param name="pendingReason">iyzico pending reason</param>
        /// <returns>Payment status</returns>
        public PaymentStatus GetPaymentStatus(string paymentStatus, string pendingReason)
        {
            var result = PaymentStatus.Pending;

            if (paymentStatus == null)
                paymentStatus = string.Empty;

            if (pendingReason == null)
                pendingReason = string.Empty;

            switch (paymentStatus.ToLowerInvariant())
            {
                case "pending":
                    switch (pendingReason.ToLowerInvariant())
                    {
                        case "authorization":
                            result = PaymentStatus.Authorized;
                            break;
                        default:
                            result = PaymentStatus.Pending;
                            break;
                    }
                    break;
                case "processed":
                case "completed":
                case "canceled_reversal":
                    result = PaymentStatus.Paid;
                    break;
                case "denied":
                case "expired":
                case "failed":
                case "voided":
                    result = PaymentStatus.Voided;
                    break;
                case "refunded":
                case "reversed":
                    result = PaymentStatus.Refunded;
                    break;
                default:
                    break;
            }
            return result;
        }

		public ActionResult CancelOrder(FormCollection form)
		{
			var order = _orderService.SearchOrders(_storeContext.CurrentStore.Id, _workContext.CurrentCustomer.Id, null, null, null, null, null, null, null, null, 0, 1)
				.FirstOrDefault();

			if (order != null)
			{
				return RedirectToAction("Details", "Order", new { id = order.Id, area = "" });
			}

			return RedirectToAction("Index", "Home", new { area = "" });
		}
	}
}