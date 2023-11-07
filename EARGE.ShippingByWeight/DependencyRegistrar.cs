using Autofac;
using Autofac.Core;
using Autofac.Integration.Mvc;
using EARGE.Core.Data;
using EARGE.Core.Infrastructure;
using EARGE.Core.Infrastructure.DependencyManagement;
using EARGE.Data;
using EARGE.ShippingByWeight.Data;
using EARGE.ShippingByWeight.Domain;
using EARGE.ShippingByWeight.Services;

namespace EARGE.ShippingByWeight
{
    public class DependencyRegistrar : IDependencyRegistrar
    {
		public virtual void Register(ContainerBuilder builder, ITypeFinder typeFinder, bool isActiveModule)
        {
			builder.RegisterType<ShippingByWeightService>().As<IShippingByWeightService>().InstancePerRequest();

            // data layer
            // register named context
            builder.Register<IDbContext>(c => new ShippingByWeightObjectContext(DataSettings.Current.DataConnectionString))
                .Named<IDbContext>(ShippingByWeightObjectContext.ALIASKEY)
                .InstancePerRequest();

			builder.Register<ShippingByWeightObjectContext>(c => new ShippingByWeightObjectContext(DataSettings.Current.DataConnectionString))
                .InstancePerRequest();

            // override required repository with our custom context
            builder.RegisterType<EfRepository<ShippingByWeightRecord>>()
                .As<IRepository<ShippingByWeightRecord>>()
                .WithParameter(ResolvedParameter.ForNamed<IDbContext>(ShippingByWeightObjectContext.ALIASKEY))
                .InstancePerRequest();
        }

        public int Order
        {
            get { return 1; }
        }
    }
}
