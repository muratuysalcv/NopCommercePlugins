
using EARGE.Core.Configuration;

namespace EARGE.ShippingByWeight
{
    public class ShippingByWeightSettings : ISettings
    {
        public bool LimitMethodsToCreated { get; set; }

        public bool CalculatePerWeightUnit { get; set; }
    }
}