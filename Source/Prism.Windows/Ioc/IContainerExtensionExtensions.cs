﻿using Prism.Navigation;
using System;
using Windows.UI.Xaml.Controls;

namespace Prism.Ioc
{
    public static partial class IContainerExtensionExtensions
    {
        public static object ResolveViewModelForView(this IContainerExtension extension, object view, Type viewModelType)
        {
            if (view is Page page)
            {
                var service = NavigationService.Instances[page.Frame];
                return extension.Resolve(viewModelType, (typeof(INavigationService), service));
            }
            else
            {
                return extension.Resolve(viewModelType);
            }
        }
    }
}
