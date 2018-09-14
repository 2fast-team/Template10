﻿using Prism.Ioc;
using System;
using Unity;
using Unity.Resolution;

namespace Prism.Unity
{
    public static class PrismIocExtensions
    {
        public static object Resolve(this IContainerProvider containerProvider, Type type, params (string Name, object Value)[] parameters)
        {
            var overrides = new ParameterOverrides();
            foreach (var (Name, Value) in parameters)
            {
                overrides.Add(Name, Value);
            }
            return containerProvider.GetContainer().Resolve(type, overrides);
        }

        public static T Resolve<T>(this IContainerProvider containerProvider, params (string Name, object Value)[] parameters)
        {
            return (T)Resolve(containerProvider, typeof(T), parameters);
        }

        public static IUnityContainer GetContainer(this IContainerProvider containerProvider)
        {
            return ((IContainerExtension<IUnityContainer>)containerProvider).Instance;
        }

        public static IUnityContainer GetContainer(this IContainerRegistry containerRegistry)
        {
            return ((IContainerExtension<IUnityContainer>)containerRegistry).Instance;
        }
    }
}
