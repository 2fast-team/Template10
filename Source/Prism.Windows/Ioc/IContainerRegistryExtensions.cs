using System;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Navigation;

namespace Prism.Ioc
{
    public static partial class IContainerRegistryExtensions
    {
        private static void Register(IContainerRegistry container, string key, Type view, Type viewModel)
        {
            if (viewModel != null)
            {
                container.Register(viewModel);
                ViewModelLocationProvider.Register(view.ToString(), viewModel);
            }
            PageNavigationRegistry.Register(key, (view, viewModel));
        }

        public static void RegisterView<TView, TViewModel>(this IContainerRegistry registry)
            => Register(registry, typeof(TView).Name, typeof(TView), typeof(TViewModel));
        public static void RegisterView<TView, TViewModel>(this IContainerRegistry registry, string key)
            => Register(registry, key, typeof(TView), typeof(TViewModel));
        public static void RegisterView<TView>(this IContainerRegistry registry)
            => Register(registry, typeof(TView).Name, typeof(TView), null);
        public static void RegisterView<TView>(this IContainerRegistry registry, string key)
            => Register(registry, key, typeof(TView), null);
    }
}
