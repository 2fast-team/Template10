using System.Threading.Tasks;
using Prism.Navigation;

namespace Prism.Mvvm
{
    public abstract class ViewModelBase : BindableBase,
        IConfirmNavigation,
        IConfirmNavigationAsync,
        IDestructible,
        INavigatedAware,
        IInitialize,
        IInitializeAsync,
        INavigatingAwareAsync
    {
        public INavigationService NavigationService { get; internal set; }

        public virtual bool CanNavigate(INavigationParameters parameters)
        {
            return true;
        }

        public Task<bool> CanNavigateAsync(INavigationParameters parameters)
        {
            return Task.FromResult(true);
        }

        public virtual void Destroy() { /* empty */ }

        public virtual void Initialize(INavigationParameters parameters) { /* empty */ }

        public virtual Task InitializeAsync(INavigationParameters parameters)
        {
            return Task.CompletedTask;
        }

        public virtual void OnNavigatedFrom(INavigationParameters parameters) { /* empty */ }

        public virtual void OnNavigatedTo(INavigationParameters parameters) { /* empty */ }

        public virtual Task OnNavigatedToAsync(INavigationParameters parameters)
        {
            return Task.CompletedTask;
        }

        public virtual void OnNavigatingTo(INavigationParameters parameters) { /* empty */ }

        public virtual Task OnNavigatingToAsync(INavigationParameters parameters)
        {
            return Task.CompletedTask;
        }
    }
}
