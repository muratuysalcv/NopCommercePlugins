using Autofac;
using Autofac.Integration.Mvc;
using EARGE.Core.Infrastructure;
using EARGE.Core.Infrastructure.DependencyManagement;
using EARGE.Core.Plugins;
using EARGE.Web.Controllers;
using EARGE.iyzico.Filters;

namespace EARGE.iyzico
{
	public class DependencyRegistrar : IDependencyRegistrar
	{
		public virtual void Register(ContainerBuilder builder, ITypeFinder typeFinder, bool isActiveModule)
		{
			if (isActiveModule)
			{
				builder.RegisterType<iyzicoExpressCheckoutFilter>().AsActionFilterFor<CheckoutController>(x => x.PaymentMethod()).InstancePerRequest();
			}
		}

		public int Order
		{
			get { return 1; }
		}
	}
}
