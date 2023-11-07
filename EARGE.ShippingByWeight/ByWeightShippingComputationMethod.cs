using System;
using System.Data.Entity.Migrations;
using System.Web.Routing;
using EARGE.Core;
using EARGE.Core.Domain.Shipping;
using EARGE.Core.Plugins;
using EARGE.ShippingByWeight.Data;
using EARGE.ShippingByWeight.Data.Migrations;
using EARGE.ShippingByWeight.Services;
using EARGE.Services;
using EARGE.Services.Catalog;
using EARGE.Services.Localization;
using EARGE.Services.Shipping;
using EARGE.Services.Shipping.Tracking;
using EARGE.Core.Infrastructure;
using EARGE.Services.Directory;

namespace EARGE.ShippingByWeight
{
    public class ByWeightShippingComputationMethod : BasePlugin, IShippingRateComputationMethod, IConfigurable
    {
        #region Fields

        private readonly IShippingService _shippingService;
        private readonly IStoreContext _storeContext;
        private readonly IShippingByWeightService _shippingByWeightService;
        private readonly IPriceCalculationService _priceCalculationService;
        private readonly ShippingByWeightSettings _shippingByWeightSettings;
        private readonly ShippingByWeightObjectContext _objectContext;
        private readonly ILocalizationService _localizationService;
        private readonly IPriceFormatter _priceFormatter;
        private readonly ICommonServices _commonServices;

        #endregion

        #region Ctor
        public ByWeightShippingComputationMethod(IShippingService shippingService,
            IStoreContext storeContext,
            IShippingByWeightService shippingByWeightService,
            IPriceCalculationService priceCalculationService,
            ShippingByWeightSettings shippingByWeightSettings,
            ShippingByWeightObjectContext objectContext,
            ILocalizationService localizationService,
            IPriceFormatter priceFormatter,
            ICommonServices commonServices)
        {
            this._shippingService = shippingService;
            this._storeContext = storeContext;
            this._shippingByWeightService = shippingByWeightService;
            this._priceCalculationService = priceCalculationService;
            this._shippingByWeightSettings = shippingByWeightSettings;
            this._objectContext = objectContext;
            this._localizationService = localizationService;
            this._priceFormatter = priceFormatter;
            this._commonServices = commonServices;
        }
        #endregion

        #region Utilities

        private decimal? GetRate(decimal subTotal, decimal weight, int shippingMethodId, int storeId, int countryId, int desi, int distance)
        {
            decimal? shippingTotal = null;

            var shippingByWeightRecord = _shippingByWeightService.FindRecord(shippingMethodId, storeId, countryId, weight, desi, distance);
            if (shippingByWeightRecord == null)
            {
                if (_shippingByWeightSettings.LimitMethodsToCreated)
                    return null;
                else
                    return decimal.Zero;
            }

            if (shippingByWeightRecord.UsePercentage && shippingByWeightRecord.ShippingChargePercentage <= decimal.Zero)
            {
                return decimal.Zero;
            }
            if (!shippingByWeightRecord.UsePercentage && shippingByWeightRecord.ShippingChargeAmount <= decimal.Zero)
            {
                return decimal.Zero;
            }

            if (shippingByWeightRecord.UsePercentage)
            {
                shippingTotal = Math.Round((decimal)((((float)subTotal) * ((float)shippingByWeightRecord.ShippingChargePercentage)) / 100f), 2);
            }
            else
            {
                shippingTotal = shippingByWeightRecord.ShippingChargeAmount;
            }

            if (shippingTotal < decimal.Zero)
            {
                shippingTotal = decimal.Zero;
            }

            return shippingTotal;
        }

        #endregion

        #region Methods

        /// <summary>
        ///  Gets available shipping options
        /// </summary>
        /// <param name="getShippingOptionRequest">A request for getting shipping options</param>
        /// <returns>Represents a response of getting shipping rate options</returns>
        public GetShippingOptionResponse GetShippingOptions(GetShippingOptionRequest getShippingOptionRequest)
        {
            if (getShippingOptionRequest == null)
                throw new ArgumentNullException("getShippingOptionRequest");

            var response = new GetShippingOptionResponse();

            if (getShippingOptionRequest.Items == null || getShippingOptionRequest.Items.Count == 0)
            {
                response.AddError("No shipment items");
                return response;
            }

            int storeId = _storeContext.CurrentStore.Id;
            decimal subTotal = decimal.Zero;
            int countryId = 0;
            int stateProvinceId = 0;
            int distance = 0;
            if (getShippingOptionRequest.ShippingAddress != null)
            {
                countryId = getShippingOptionRequest.ShippingAddress.CountryId ?? 0;
                if (getShippingOptionRequest.ShippingAddress.StateProvinceId.HasValue)
                {
                    stateProvinceId = getShippingOptionRequest.ShippingAddress.StateProvinceId.Value;
                    var stateProvinceService = EngineContext.Current.Resolve<IStateProvinceService>();
                    var stateProvince = stateProvinceService.GetStateProvinceById(stateProvinceId);
                    if (stateProvince != null)
                    {
                        distance = stateProvince.DistanceKM;
                    }
                }
            }

            foreach (var shoppingCartItem in getShippingOptionRequest.Items)
            {
                if (shoppingCartItem.Item.IsFreeShipping || !shoppingCartItem.Item.IsShipEnabled)
                    continue;
                subTotal += _priceCalculationService.GetSubTotal(shoppingCartItem, true);
            }
            decimal weight = _shippingService.GetShoppingCartTotalWeight(getShippingOptionRequest.Items);

            int desi = _shippingService.GetShoppingCartTotalDesi(getShippingOptionRequest.Items);

            var shippingMethods = _shippingService.GetAllShippingMethods(countryId);
            foreach (var shippingMethod in shippingMethods)
            {
                var record = _shippingByWeightService.FindRecord(shippingMethod.Id, storeId, countryId, weight, desi, distance);

                decimal? rate = GetRate(subTotal, weight, shippingMethod.Id, storeId, countryId, desi, distance);
                if (rate.HasValue)
                {
                    var shippingOption = new ShippingOption();
                    shippingOption.Name = shippingMethod.GetLocalized(x => x.Name);

                    if (record != null && record.SmallQuantityThreshold > subTotal)
                    {
                        shippingOption.Description = shippingMethod.GetLocalized(x => x.Description)
                            + _localizationService.GetResource("Plugin.Shipping.ByWeight.SmallQuantitySurchargeNotReached").FormatWith(
                                _priceFormatter.FormatPrice(record.SmallQuantitySurcharge),
                                _priceFormatter.FormatPrice(record.SmallQuantityThreshold));

                        shippingOption.Rate = rate.Value + record.SmallQuantitySurcharge;
                    }
                    else
                    {
                        shippingOption.Description = shippingMethod.GetLocalized(x => x.Description);
                        shippingOption.Rate = rate.Value;
                    }
                    response.ShippingOptions.Add(shippingOption);
                }
            }


            return response;
        }

        /// <summary>
        /// Gets fixed shipping rate (if shipping rate computation method allows it and the rate can be calculated before checkout).
        /// </summary>
        /// <param name="getShippingOptionRequest">A request for getting shipping options</param>
        /// <returns>Fixed shipping rate; or null in case there's no fixed shipping rate</returns>
        public decimal? GetFixedRate(GetShippingOptionRequest getShippingOptionRequest)
        {
            return null;
        }

        /// <summary>
        /// Gets a route for provider configuration
        /// </summary>
        /// <param name="actionName">Action name</param>
        /// <param name="controllerName">Controller name</param>
        /// <param name="routeValues">Route values</param>
        public void GetConfigurationRoute(out string actionName, out string controllerName, out RouteValueDictionary routeValues)
        {
            actionName = "Configure";
            controllerName = "ShippingByWeight";
            routeValues = new RouteValueDictionary() { { "area", "EARGE.ShippingByWeight" } };
        }

        /// <summary>
        /// Install plugin
        /// </summary>
        public override void Install()
        {
            _localizationService.ImportPluginResourcesFromXml(this.PluginDescriptor);

            base.Install();
        }

        /// <summary>
        /// Uninstall plugin
        /// </summary>
        public override void Uninstall()
        {
            var migrator = new DbMigrator(new Configuration());
            migrator.Update(DbMigrator.InitialDatabase);

            _localizationService.DeleteLocaleStringResources(PluginDescriptor.ResourceRootKey);

            base.Uninstall();
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets a shipping rate computation method type
        /// </summary>
        public ShippingRateComputationMethodType ShippingRateComputationMethodType
        {
            get
            {
                return ShippingRateComputationMethodType.Offline;
            }
        }


        /// <summary>
        /// Gets a shipment tracker
        /// </summary>
        public IShipmentTracker ShipmentTracker
        {
            get
            {
                //uncomment a line below to return a general shipment tracker (finds an appropriate tracker by tracking number)
                //return new GeneralShipmentTracker(EngineContext.Current.Resolve<ITypeFinder>());
                return null;
            }
        }

        #endregion
    }
}
