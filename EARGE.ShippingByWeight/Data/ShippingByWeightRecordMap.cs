using System.Data.Entity.ModelConfiguration;
using EARGE.ShippingByWeight.Domain;

namespace EARGE.ShippingByWeight.Data
{
    public partial class ShippingByWeightRecordMap : EntityTypeConfiguration<ShippingByWeightRecord>
    {
        public ShippingByWeightRecordMap()
        {
            this.ToTable("ShippingByWeight");
            this.HasKey(x => x.Id);
        }
    }
}