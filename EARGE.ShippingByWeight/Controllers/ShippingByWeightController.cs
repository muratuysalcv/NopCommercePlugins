using System.Globalization;
using System.Linq;
using System.Threading;
using System.Web.Mvc;
using EARGE.Core.Domain.Common;
using EARGE.Core.Domain.Directory;
using EARGE.ShippingByWeight.Domain;
using EARGE.ShippingByWeight.Models;
using EARGE.ShippingByWeight.Services;
using EARGE.Services.Configuration;
using EARGE.Services.Directory;
using EARGE.Services.Shipping;
using EARGE.Services.Stores;
using EARGE.Web.Framework.Controllers;
using Telerik.Web.Mvc;
using EARGE.Core;

namespace EARGE.ShippingByWeight.Controllers
{

    [AdminAuthorize]
    public class ShippingByWeightController : PluginControllerBase
    {
        private readonly IShippingService _shippingService;
        private readonly IStoreService _storeService;
        private readonly ICountryService _countryService;
        private readonly IStateProvinceService _stateProvinceService;
        private readonly ShippingByWeightSettings _shippingByWeightSettings;
        private readonly IShippingByWeightService _shippingByWeightService;
        private readonly ISettingService _settingService;

        private readonly ICurrencyService _currencyService;
        private readonly CurrencySettings _currencySettings;
        private readonly IMeasureService _measureService;
        private readonly MeasureSettings _measureSettings;
        private readonly AdminAreaSettings _adminAreaSettings;
        private readonly IStoreContext _storeContext;

        public ShippingByWeightController(IShippingService shippingService,
            IStoreService storeService, ICountryService countryService, ShippingByWeightSettings shippingByWeightSettings,
            IShippingByWeightService shippingByWeightService, ISettingService settingService,
            ICurrencyService currencyService, CurrencySettings currencySettings,
            IMeasureService measureService, MeasureSettings measureSettings,
            AdminAreaSettings adminAreaSettings,
            IStoreContext storeContext,
            IStateProvinceService stateProvinceService
            )
        {
            this._stateProvinceService = stateProvinceService;
            this._storeContext = storeContext;
            this._shippingService = shippingService;
            this._storeService = storeService;
            this._countryService = countryService;
            this._shippingByWeightSettings = shippingByWeightSettings;
            this._shippingByWeightService = shippingByWeightService;
            this._settingService = settingService;

            this._currencyService = currencyService;
            this._currencySettings = currencySettings;
            this._measureService = measureService;
            this._measureSettings = measureSettings;
            this._adminAreaSettings = adminAreaSettings;
        }

        public ActionResult Configure()
        {
            var shippingMethods = _shippingService.GetAllShippingMethods();
            if (shippingMethods.Count == 0)
                return Content("No shipping methods can be loaded");

            var model = new ShippingByWeightListModel();
            foreach (var sm in shippingMethods)
                model.AvailableShippingMethods.Add(new SelectListItem() { Text = sm.Name, Value = sm.Id.ToString() });

            //stores
            model.AvailableStores.Add(new SelectListItem() { Text = "*", Value = "0" });
            foreach (var store in _storeService.GetAllStores())
                model.AvailableStores.Add(new SelectListItem() { Text = store.Name, Value = store.Id.ToString() });

            model.AvailableCountries.Add(new SelectListItem() { Text = "*", Value = "0" });
            var countries = _countryService.GetAllCountries(true);
            foreach (var c in countries)
                model.AvailableCountries.Add(new SelectListItem() { Text = c.Name, Value = c.Id.ToString() });


            model.AvailableStateProvinces.Add(new SelectListItem() { Text = "*", Value = "0" });
            var states = _stateProvinceService.GetStateProvincesByCountryId(77, false);
            foreach (var c in states)
                model.AvailableStateProvinces.Add(new SelectListItem() { Text = c.Name, Value = c.Id.ToString() });

            model.LimitMethodsToCreated = _shippingByWeightSettings.LimitMethodsToCreated;
            model.CalculatePerWeightUnit = _shippingByWeightSettings.CalculatePerWeightUnit;
            model.PrimaryStoreCurrencyCode = _currencyService.GetCurrencyById(_currencySettings.PrimaryStoreCurrencyId).CurrencyCode;
            model.BaseWeightIn = _measureService.GetMeasureWeightById(_measureSettings.BaseWeightId).Name;
            model.GridPageSize = _adminAreaSettings.GridPageSize;

            return View(model);
        }

        [HttpPost, GridAction(EnableCustomBinding = true)]
        public ActionResult RatesList(GridCommand command)
        {
            int totalCount;
            var data = _shippingByWeightService.GetShippingByWeightModels(command.Page - 1, command.PageSize, out totalCount);

            var model = new GridModel<ShippingByWeightModel>
            {
                Data = data,
                Total = totalCount
            };

            return new JsonResult
            {
                Data = model
            };
        }

        [GridAction(EnableCustomBinding = true)]
        public ActionResult RateUpdate(ShippingByWeightModel model, GridCommand command)
        {
            var sbw = _shippingByWeightService.GetById(model.Id);
            if (sbw.StoreId != _storeContext.CurrentStore.Id)
                throw new SmartException("Access denied");
            sbw.From = model.From;
            sbw.To = model.To;
            //sbw.UsePercentage = model.UsePercentage;
            sbw.ShippingChargeAmount = model.ShippingChargeAmount;
            //sbw.ShippingChargePercentage = model.ShippingChargePercentage;
            //sbw.SmallQuantitySurcharge = model.SmallQuantitySurcharge;
            //sbw.SmallQuantityThreshold = model.SmallQuantityThreshold;
            sbw.DesiStart = model.DesiStart;
            sbw.DesiEnd = model.DesiEnd;
            sbw.DistanceEnd = model.DistanceKmEnd;
            sbw.DistanceStart = model.DistanceKmStart;
            _shippingByWeightService.UpdateShippingByWeightRecord(sbw);

            return RatesList(command);
        }

        [GridAction(EnableCustomBinding = true)]
        public ActionResult RateDelete(int id, GridCommand command)
        {
            var sbw = _shippingByWeightService.GetById(id);
            if (sbw != null)
                if (sbw.StoreId != _storeContext.CurrentStore.Id)
                    throw new SmartException("access denied");

            _shippingByWeightService.DeleteShippingByWeightRecord(sbw);

            return RatesList(command);
        }

        [HttpPost, ActionName("Configure")]
        [FormValueRequired("addshippingbyweightrecord")]
        public ActionResult AddShippingByWeightRecord(ShippingByWeightListModel model)
        {
            if (!ModelState.IsValid)
            {
                return Configure();
            }

            var sbw = new ShippingByWeightRecord()
            {
                StoreId = _storeContext.CurrentStore.Id,
                ShippingMethodId = model.AddShippingMethodId,
                CountryId = model.AddCountryId,
                DesiStart = model.AddDesiStart,
                DesiEnd = model.AddDesiEnd,
                StateProvinceId = model.AddStateProvinceId,
                DistanceStart = model.AddDistanceStart,
                DistanceEnd = model.AddDistanceEnd,
                From = model.AddFrom,
                To = model.AddTo,

                //UsePercentage = model.AddUsePercentage,
                ShippingChargeAmount = model.AddShippingChargeAmount,
                //ShippingChargePercentage = model.AddShippingChargePercentage,
                //SmallQuantitySurcharge = model.SmallQuantitySurcharge,
                //SmallQuantityThreshold = model.SmallQuantityThreshold,
            };
            _shippingByWeightService.InsertShippingByWeightRecord(sbw);

            return Configure();
        }

        [HttpPost, ActionName("Configure")]
        [FormValueRequired("savegeneralsettings")]
        public ActionResult SaveGeneralSettings(ShippingByWeightListModel model)
        {
            //save settings
            _shippingByWeightSettings.LimitMethodsToCreated = model.LimitMethodsToCreated;
            _shippingByWeightSettings.CalculatePerWeightUnit = model.CalculatePerWeightUnit;
            _settingService.SaveSetting(_shippingByWeightSettings);

            return Configure();
        }

    }
}
