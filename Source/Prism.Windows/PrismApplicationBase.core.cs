using Prism.Events;
using Prism.Ioc;
using Prism.Logging;
using Prism.Mvvm;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Core;
using Prism.Core.Services;
using Windows.UI.Core.Preview;
using Prism.Modularity;

namespace Prism
{
    public abstract partial class PrismApplicationBase
    {
        public static new PrismApplicationBase Current => (PrismApplicationBase)Application.Current;
        private static readonly SemaphoreSlim _startSemaphore = new SemaphoreSlim(1, 1);
        public const string NavigationServiceParameterName = "navigationService";
        private readonly bool _logStartingEvents = false;

        public PrismApplicationBase()
        {
            InternalInitialize();
            _logger.Log("[App.Constructor()]", Category.Info, Priority.None);

            CoreApplication.Exiting += (s, e) =>
            {
                var stopArgs = new StopArgs(StopKind.CoreApplicationExiting) { CoreApplicationEventArgs = e };
                OnStop(stopArgs);
                OnStopAsync(stopArgs);
            };

            WindowService.WindowCreatedCallBacks.Add(Guid.Empty, args =>
            {
                WindowService.WindowCreatedCallBacks.Remove(Guid.Empty);

                args.Window.Closed += (s, e) =>
                {
                    OnStop(new StopArgs(StopKind.CoreWindowClosed) { CoreWindowEventArgs = e });
                    OnStopAsync(new StopArgs(StopKind.CoreWindowClosed) { CoreWindowEventArgs = e }).RunSynchronously();
                };

                SystemNavigationManagerPreview.GetForCurrentView().CloseRequested += async (s, e) =>
                {
                    var deferral = e.GetDeferral();
                    try
                    {
                        OnStop(new StopArgs(StopKind.CloseRequested) { CloseRequestedPreviewEventArgs = e });
                        await OnStopAsync(new StopArgs(StopKind.CloseRequested) { CloseRequestedPreviewEventArgs = e });
                    }
                    finally
                    {
                        deferral.Complete();
                    }
                };
            });

            base.Suspending += async (s, e) =>
            {
                new SuspensionUtilities().SetSuspendDate(DateTime.Now);
                var deferral = e.SuspendingOperation.GetDeferral();
                try
                {
                    var stopArgs = new StopArgs(StopKind.Suspending) { SuspendingEventArgs = e };
                    OnStop(stopArgs);
                    await OnStopAsync(stopArgs);
                }
                finally
                {
                    deferral.Complete();
                }
            };

            base.Resuming += async (s, e) =>
            {
                var resumeArgs = new ResumeArgs
                {
                    PreviousExecutionState = ApplicationExecutionState.Suspended,
                };
                var startArgs = new StartArgs(resumeArgs, StartKinds.ResumeInMemory);
                await InternalStartAsync(startArgs);
            };
        }

        public Func<SplashScreen, UIElement> ExtendedSplashScreenFactory { get; set; }

        private IContainerExtension _containerExtension;
        private IModuleCatalog _moduleCatalog;
        public IContainerProvider Container => _containerExtension;

        private void InternalInitialize()
        {
            // don't forget there is no logger yet
            if (_logStartingEvents)
            {
                _logger.Log($"{nameof(PrismApplicationBase)}.{nameof(InternalInitialize)}", Category.Info, Priority.None);
            }

            // dependecy injection
            ContainerLocator.SetContainerExtension(CreateContainerExtension);

            Debug.WriteLine("[App.RegisterTypes()]");
            _containerExtension = ContainerLocator.Current;

            //_moduleCatalog = CreateModuleCatalog();

            //var regionAdapterMappins = _containerExtension.Resolve<RegionAdapterMappings>();
            //ConfigureRegionAdapterMappings(regionAdapterMappins);

            //var defaultRegionBehaviors = _containerExtension.Resolve<IRegionBehaviorFactory>();
            //ConfigureDefaultRegionBehaviors(defaultRegionBehaviors);

            RegisterTypes(_containerExtension);
            if (_containerExtension is IContainerRegistry registry)
            {
                registry.RegisterSingleton<ILoggerFacade, DebugLogger>();
                registry.RegisterSingleton<IEventAggregator, EventAggregator>();
                RegisterInternalTypes(registry);
            }
            Debug.WriteLine("Dependency container has just been finalized.");
            _containerExtension.FinalizeExtension();

            ConfigureModuleCatalog(_moduleCatalog);

            // now we can start logging instead of debug/write
            _logger = Container.Resolve<ILoggerFacade>();

            // finalize the application
            ConfigureViewModelLocator();
        }

        private static int _initialized = 0;
        private ILoggerFacade _logger;

        private void CallOnInitializedOnlyOnce()
        {
            // don't forget there is no logger yet
            if (_logStartingEvents)
            {
                _logger.Log($"{nameof(PrismApplicationBase)}.{nameof(CallOnInitializedOnlyOnce)}", Category.Info, Priority.None);
            }

            // once and only once, ever
            if (Interlocked.Increment(ref _initialized) == 1)
            {
                _logger.Log("[App.OnInitialize()]", Category.Info, Priority.None);
                OnInitialized();
            }
        }

        private static int _started = 0;

        private async Task InternalStartAsync(StartArgs startArgs)
        {
            await _startSemaphore.WaitAsync();
            if (_logStartingEvents)
            {
                _logger.Log($"{nameof(PrismApplicationBase)}.{nameof(InternalStartAsync)}({startArgs})", Category.Info, Priority.None);
            }

            // sometimes activation is rased through the base.onlaunch. We'll fix that.
            if (Interlocked.Increment(ref _started) > 1 && startArgs.StartKind == StartKinds.Launch)
            {
                startArgs.StartKind = StartKinds.Activate;
            }

            SetupExtendedSplashScreen();

            try
            {
                CallOnInitializedOnlyOnce();
                var suspensionUtil = new SuspensionUtilities();
                if (suspensionUtil.IsResuming(startArgs, out var resumeArgs))
                {
                    startArgs.StartKind = StartKinds.ResumeFromTerminate;
                    startArgs.Arguments = resumeArgs;
                }
                suspensionUtil.ClearSuspendDate();

                _logger.Log($"[App.OnStart(startKind:{startArgs.StartKind}, startCause:{startArgs.StartCause})]", Category.Info, Priority.None);
                OnStart(startArgs);

                _logger.Log($"[App.OnStartAsync(startKind:{startArgs.StartKind}, startCause:{startArgs.StartCause})]", Category.Info, Priority.None);
                await OnStartAsync(startArgs);
            }
            finally
            {
                _startSemaphore.Release();
            }

            void SetupExtendedSplashScreen()
            {
                if (startArgs.StartKind == StartKinds.Launch
                    && startArgs.Arguments is IActivatedEventArgs act
                    && Window.Current.Content is null
                    && !(ExtendedSplashScreenFactory is null))
                {
                    try
                    {
                        Window.Current.Content = ExtendedSplashScreenFactory(act.SplashScreen);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Error during {nameof(ExtendedSplashScreenFactory)}.", ex);
                    }
                }
            }
        }

        #region overrides

        public virtual void OnStop(IStopArgs stopArgs) { /* empty */ }

        public virtual Task OnStopAsync(IStopArgs stopArgs)
        {
            return Task.CompletedTask;
        }

        public abstract void RegisterTypes(IContainerRegistry container);

        public virtual void OnInitialized() { /* empty */ }

        public virtual void OnStart(IStartArgs args) {  /* empty */ }

        public virtual Task OnStartAsync(IStartArgs args)
        {
            return Task.CompletedTask;
        }

        public virtual void ConfigureViewModelLocator()
        {
            // this is a testability method
            ViewModelLocationProvider.SetDefaultViewModelFactory((view, type) =>
            {
                return _containerExtension.ResolveViewModelForView(view, type);
            });
        }

        /// <summary>
        /// Creates the container used by Prism.
        /// </summary>
        /// <returns>The container</returns>
        protected abstract IContainerExtension CreateContainerExtension();

        /// <summary>
        /// Creates the <see cref="IModuleCatalog"/> used by Prism.
        /// </summary>
        ///  <remarks>
        /// The base implementation returns a new ModuleCatalog.
        /// </remarks>
        //protected virtual IModuleCatalog CreateModuleCatalog()
        //{
        //    return new ModuleCatalog();
        //}

        /// <summary>
        /// Initializes the modules.
        /// </summary>
        protected virtual void InitializeModules()
        {
            //IModuleManager manager = containerProvider.Resolve<IModuleManager>();
            //manager.Run();
        }


        /// <summary>
        /// Configures the <see cref="IModuleCatalog"/> used by Prism.
        /// </summary>
        protected virtual void ConfigureModuleCatalog(IModuleCatalog moduleCatalog) { }

        protected virtual void RegisterInternalTypes(IContainerRegistry containerRegistry)
        {
            // don't forget there is no logger yet
            Debug.WriteLine($"{nameof(PrismApplicationBase)}.{nameof(RegisterInternalTypes)}()");
        }

        #endregion
    }
}
