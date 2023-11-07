using System;
using System.Collections.Generic;
using System.Linq;
using EARGE.Core;
using EARGE.Core.Data;
using EARGE.ShippingByWeight.Domain;
using EARGE.ShippingByWeight.Models;
using EARGE.Services.Directory;
using EARGE.Services.Shipping;
using EARGE.Services.Stores;

namespace EARGE.ShippingByWeight.Services
{
    public partial class ShippingByWeightService : IShippingByWeightService
    {
        #region Fields

        private readonly IRepository<ShippingByWeightRecord> _sbwRepository;
        private readonly IStoreService _storeService;
        private readonly IShippingService _shippingService;
        private readonly ICountryService _countryService;
        private readonly IStateProvinceService _stateProvinceService;
        private readonly IStoreContext _storeContext;

        #endregion

        #region Ctor

        public ShippingByWeightService(
            IRepository<ShippingByWeightRecord> sbwRepository,
            IStoreService storeService,
            IShippingService shippingService,
            ICountryService countryService,
            IStoreContext storeContext,
            IStateProvinceService stateProvinceService
            )
        {
            _stateProvinceService = stateProvinceService;
            _storeContext = storeContext;
            _sbwRepository = sbwRepository;
            _storeService = storeService;
            _shippingService = shippingService;
            _countryService = countryService;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Get queryable shipping by weight records
        /// </summary>
        public virtual IQueryable<ShippingByWeightRecord> GetShippingByWeightRecords()
        {
            var query =
                from x in _sbwRepository.Table.Where(x => x.StoreId == _storeContext.CurrentStore.Id)
                orderby x.StoreId, x.CountryId, x.ShippingMethodId, x.From
                select x;

            return query;
        }

        /// <summary>
        /// Get paged shipping by weight records
        /// </summary>
        public virtual IPagedList<ShippingByWeightRecord> GetShippingByWeightRecords(int pageIndex, int pageSize)
        {
            var result = new PagedList<ShippingByWeightRecord>(GetShippingByWeightRecords(), pageIndex, pageSize);
            return result;
        }

        /// <summary>
        /// Get models for shipping by weight records
        /// </summary>
        public virtual IList<ShippingByWeightModel> GetShippingByWeightModels(int pageIndex, int pageSize, out int totalCount)
        {
            // data join would be much better but not possible here cause ShippingByWeightObjectContext cannot be shared across repositories
            var records = GetShippingByWeightRecords(pageIndex, pageSize);
            totalCount = records.TotalCount;

            if (records.Count <= 0)
                return new List<ShippingByWeightModel>();

            var allStores = _storeService.GetAllStores();

            var result = records.Select(x =>
            {
                var store = allStores.FirstOrDefault(y => y.Id == x.StoreId);
                var shippingMethod = _shippingService.GetShippingMethodById(x.ShippingMethodId);
                var country = _countryService.GetCountryById(x.CountryId);
                var stateProvince = _stateProvinceService.GetStateProvinceById(x.StateProvinceId);

                string stateProvinceName = "*";
                if (stateProvince != null)
                    stateProvinceName = stateProvince.Name;

                string countryName = "*";
                if (country != null)
                    countryName = country.Name;

                var model = new ShippingByWeightModel()
                {
                    Id = x.Id,
                    StoreId = x.StoreId,
                    ShippingMethodId = x.ShippingMethodId,
                    CountryId = x.CountryId,
                    From = x.From,
                    To = x.To,
                    UsePercentage = x.UsePercentage,
                    ShippingChargePercentage = x.ShippingChargePercentage,
                    ShippingChargeAmount = x.ShippingChargeAmount,
                    SmallQuantitySurcharge = x.SmallQuantitySurcharge,
                    SmallQuantityThreshold = x.SmallQuantityThreshold,
                    StoreName = (store == null ? "*" : store.Name),
                    ShippingMethodName = (shippingMethod == null ? "".NaIfEmpty() : shippingMethod.Name),
                    CountryName = countryName,
                    StateProvinceName = stateProvinceName,
                    DistanceKmStart = x.DistanceStart,
                    DistanceKmEnd = x.DistanceEnd,
                    DesiStart = x.DesiStart,
                    DesiEnd = x.DesiEnd
                };

                return model;
            })
            .ToList();

            return result;
        }

        public virtual ShippingByWeightRecord FindRecord(int shippingMethodId, int storeId, int countryId, decimal weight, int desi = 0, int distance = 0)
        {
            var existingRecords = GetShippingByWeightRecords()
                .Where(x => x.ShippingMethodId == shippingMethodId &&
                    x.DesiStart <= desi && x.DesiEnd >= desi
                    && x.DistanceStart <= distance
                    && x.DistanceEnd >= distance
                )
                .ToList();

            //filter by store
            var matchedByStore = new List<ShippingByWeightRecord>();
            foreach (var sbw in existingRecords)
            {
                if (storeId == sbw.StoreId)
                    matchedByStore.Add(sbw);
            }

            if (matchedByStore.Count == 0)
            {
                foreach (var sbw in existingRecords)
                {
                    if (sbw.StoreId == 0)
                        matchedByStore.Add(sbw);
                }
            }

            //filter by country
            var matchedByCountry = new List<ShippingByWeightRecord>();
            foreach (var sbw in matchedByStore)
            {
                if (countryId == sbw.CountryId)
                    matchedByCountry.Add(sbw);
            }

            if (matchedByCountry.Count == 0)
            {
                foreach (var sbw in matchedByStore)
                {
                    if (sbw.CountryId == 0)
                        matchedByCountry.Add(sbw);
                }
            }

            return matchedByCountry.FirstOrDefault();
        }

        public virtual ShippingByWeightRecord GetById(int shippingByWeightRecordId)
        {
            if (shippingByWeightRecordId == 0)
                return null;

            var record = _sbwRepository.GetById(shippingByWeightRecordId);

            if (record != null)
                if (!_storeContext.CheckStoreId(record.StoreId))
                    return null;

            return record;
        }

        public virtual void DeleteShippingByWeightRecord(ShippingByWeightRecord shippingByWeightRecord)
        {
            if (shippingByWeightRecord == null)
                throw new ArgumentNullException("shippingByWeightRecord");
            if (!_storeContext.CheckStoreId(shippingByWeightRecord.StoreId))
                throw new ArgumentNullException("shippingByWeightRecord");

            _sbwRepository.Delete(shippingByWeightRecord);
        }

        public virtual void InsertShippingByWeightRecord(ShippingByWeightRecord shippingByWeightRecord)
        {
            if (shippingByWeightRecord == null)
                throw new ArgumentNullException("shippingByWeightRecord");
            shippingByWeightRecord.StoreId = _storeContext.CurrentStore.Id;

            _sbwRepository.Insert(shippingByWeightRecord);
        }

        public virtual void UpdateShippingByWeightRecord(ShippingByWeightRecord shippingByWeightRecord)
        {
            if (shippingByWeightRecord == null)
                throw new ArgumentNullException("shippingByWeightRecord");
            if (!_storeContext.CheckStoreId(shippingByWeightRecord.StoreId))
                throw new ArgumentNullException("shippingByWeightRecord");

            _sbwRepository.Update(shippingByWeightRecord);
        }

        #endregion
    }
}
