﻿using win = Windows;
using Prism.Events;
using Prism.Ioc;
using Prism.Logging;
using Prism.Navigation;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using System.Linq;
using Prism.Services;

namespace Prism
{
    public abstract partial class PrismApplicationBase
    {
        public new static PrismApplicationBase Current => (PrismApplicationBase)Application.Current;

        private static SemaphoreSlim _startSemaphore = new SemaphoreSlim(1, 1);

        public const string NavigationServiceParameterName = "navigationService";

        public PrismApplicationBase()
        {
            InternalInitialize();
            (this as IPrismApplicationEvents).WindowCreated += (s, e)
                => GestureService.SetupForCurrentView(e.Window.CoreWindow);
        }

        public virtual void ConfigureViewModelLocator()
        {
            ViewModelLocationProvider.SetDefaultViewModelFactory((view, type) =>
            {
                return _containerExtension.ResolveViewModelForView(view, type);
            });
        }

        IContainerExtension _containerExtension;
        public IContainerProvider Container => _containerExtension;

        public virtual IContainerExtension CreateContainer()
        {
            return new DefaultContainerExtension();
        }

        protected virtual void RegisterRequiredTypes(IContainerRegistry container)
        {
            // required for view-models

            container.Register<INavigationService, NavigationService>(NavigationServiceParameterName);

            // standard prism services

            container.RegisterSingleton<ILoggerFacade, EmptyLogger>();
            container.RegisterSingleton<IEventAggregator, EventAggregator>();
        }

        public abstract void RegisterTypes(IContainerRegistry container);

        public virtual void OnInitialized() { /* empty */ }

        public virtual void OnStart(StartArgs args) {  /* empty */ }

        public virtual Task OnStartAsync(StartArgs args) => Task.CompletedTask;

        private void InternalInitialize()
        {
            // don't forget there is no logger yet
            Debug.WriteLine($"{nameof(PrismApplicationBase)}.{nameof(InternalInitialize)}");

            // dependecy injection
            _containerExtension = CreateContainer();
            RegisterRequiredTypes(_containerExtension as IContainerRegistry);
            RegisterTypes(_containerExtension as IContainerRegistry);
            _containerExtension.FinalizeExtension();

            // finalize the application
            ConfigureViewModelLocator();
            OnInitialized();
        }

        private async Task InternalStartAsync(StartArgs startArgs)
        {
            await _startSemaphore.WaitAsync();
            Debug.WriteLine($"{nameof(PrismApplicationBase)}.{nameof(InternalStartAsync)}({startArgs})");
            try
            {
                Window.Current.Activate();
                OnStart(startArgs);
                await OnStartAsync(startArgs);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ERROR {ex.Message}");
                Debugger.Break();
            }
            finally
            {
                _startSemaphore.Release();
            }
        }
    }
}
