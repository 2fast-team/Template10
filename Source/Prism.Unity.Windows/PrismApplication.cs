using Prism.Ioc;
using Unity;
using Unity.Extension;

namespace Prism.Unity
{
    public abstract class PrismApplication : PrismApplicationBase
    {
        protected override IContainerExtension CreateContainerExtension()
        {
            return new UnityContainerExtension(new UnityContainer());
        }
    }
}
