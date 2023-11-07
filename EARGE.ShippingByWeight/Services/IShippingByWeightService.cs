using System.Collections.Generic;
using System.Linq;
using EARGE.Core;
using EARGE.ShippingByWeight.Domain;
using EARGE.ShippingByWeight.Models;

namespace EARGE.ShippingByWeight.Services
{
    public partial interface IShippingByWeightService
    {
		/// <summary>
		/// Get queryable shipping by weight records
		/// </summary>
		IQueryable<ShippingByWeightRecord> GetShippingByWeightRecords();

		/// <summary>
		/// Get paged shipping by weight records
		/// </summary>
		IPagedList<ShippingByWeightRecord> GetShippingByWeightRecords(int pageIndex, int pageSize);

		/// <summary>
		/// Get models for shipping by weight records
		/// </summary>
		IList<ShippingByWeightModel> GetShippingByWeightModels(int pageIndex, int pageSize, out int totalCount);

		ShippingByWeightRecord FindRecord(int shippingMethodId, int storeId, int countryId, decimal weight,int desi=0,int distance=0);

        ShippingByWeightRecord GetById(int shippingByWeightRecordId);
		
		void DeleteShippingByWeightRecord(ShippingByWeightRecord shippingByWeightRecord);

        void InsertShippingByWeightRecord(ShippingByWeightRecord shippingByWeightRecord);

        void UpdateShippingByWeightRecord(ShippingByWeightRecord shippingByWeightRecord);
    }
}
