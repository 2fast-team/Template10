﻿using System;
using System.Diagnostics;
using System.Linq;
using Prism.Navigation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using System.Collections.ObjectModel;
using win = Windows;
using System.Threading;
using Prism.Utilities;
using Prism.Services;
using Windows.UI.Xaml.Controls;
using NavigationView = Microsoft.UI.Xaml.Controls.NavigationView;
using NavigationViewItem = Microsoft.UI.Xaml.Controls.NavigationViewItem;
using NavigationViewItemInvokedEventArgs = Microsoft.UI.Xaml.Controls.NavigationViewItemInvokedEventArgs;
using Prism.Ioc;
using Prism;

namespace Template10.Controls
{
    public class NavViewEx : NavigationView
    {
        private CoreDispatcher _dispatcher;
        private Frame _frame;
        public NavigationService NavigationService { get; private set; }

        public NavViewEx()
        {
            DefaultStyleKey = typeof(NavigationView);

            if (win.ApplicationModel.DesignMode.DesignModeEnabled
                || win.ApplicationModel.DesignMode.DesignMode2Enabled)
            {
                return;
            }

            Content = _frame = new Frame();
            _dispatcher = _frame.Dispatcher;

            _frame.Navigated += (s, e) =>
            {
                if (TryFindItem(e.SourcePageType, e.Parameter, out var item))
                {
                    SetSelectedItem(item, false);
                    if (item == null)
                    {
                        return;
                    }
                }
            };

            NavigationService = (NavigationService)PrismApplicationBase.Current.Container.Resolve<IPlatformNavigationService>("navigationService", (typeof(Frame), _frame));

            ItemInvoked += (s, e) =>
            {
                SelectedItem = (e.IsSettingsInvoked) ? SettingsItem : Find(e.InvokedItemContainer as NavigationViewItem);
                if (SelectedItem == null)
                {
                    return;
                }
                var item = SelectedItem as NavigationViewItem;
                Header = item.Content;
            };

            //RegisterPropertyChangedCallback(IsPaneOpenProperty, (s, e) =>
            //{
            //    UpdatePaneHeadersVisibility();
            //});

            //Window.Current.CoreWindow.SizeChanged += (s, e) =>
            //{
            //    UpdatePageHeaderContent();
            //};

            //Loaded += (s, e) =>
            //{
            //    UpdatePaneHeadersVisibility();
            //    UpdatePageHeaderContent();
            //};
        }

        private void UpdatePaneHeadersVisibility()
        {
            foreach (var item in MenuItems.OfType<NavigationViewItemHeader>())
            {
                switch (ItemHeaderBehavior)
                {
                    case ItemHeaderBehaviors.Hide:
                        item.Opacity = IsPaneOpen ? 1 : 0;
                        break;
                    case ItemHeaderBehaviors.Remove:
                        item.Visibility = IsPaneOpen ? Visibility.Visible : Visibility.Collapsed;
                        break;
                    case ItemHeaderBehaviors.None:
                        // empty
                        break;
                }
            }
        }

        private static SemaphoreSlim _updatePageHeaderSemaphore = new SemaphoreSlim(1, 1);

        private void UpdatePageHeaderContent()
        {
            _updatePageHeaderSemaphore.Wait();

            bool localTryGetCommandBar(out CommandBar bar)
            {
                var children = XamlUtilities.RecurseChildren(this);
                var bars = children
                    .OfType<CommandBar>();
                if (!bars.Any())
                {
                    bar = default(CommandBar);
                    return false;
                }
                bar = bars.First();
                return true;
            }

            void localUpdatePageHeaderCommands(ObservableCollection<object> headerCommands)
            {
                if (!localTryGetCommandBar(out var bar))
                {
                    return;
                }

                var previous = bar.PrimaryCommands
                    .OfType<DependencyObject>()
                    .Where(x => x.GetValue(NavViewProps.PageHeaderCommandDynamicItemProperty) is bool value && value);

                foreach (var command in previous.OfType<ICommandBarElement>().ToArray())
                {
                    bar.PrimaryCommands.Remove(command);
                }

                foreach (var command in headerCommands.Reverse().OfType<DependencyObject>().ToArray())
                {
                    command.SetValue(NavViewProps.PageHeaderCommandDynamicItemProperty, true);
                    bar.PrimaryCommands.Insert(0, command as ICommandBarElement);
                }
            }

            try
            {
                if (_frame.Content is Page page)
                {
                    if (page.GetValue(NavViewProps.HeaderTextProperty) is string headerText && !Equals(Header, headerText))
                    {
                        Header = headerText;
                    }

                    if (page.GetValue(NavViewProps.HeaderCommandsProperty) is ObservableCollection<object> headerCommands && headerCommands.Any())
                    {
                        localUpdatePageHeaderCommands(headerCommands);
                    }
                }
            }
            finally
            {
                _updatePageHeaderSemaphore.Release();
            }
        }


        public enum ItemHeaderBehaviors { Hide, Remove, None }
        public ItemHeaderBehaviors ItemHeaderBehavior { get; set; } = ItemHeaderBehaviors.Remove;

        public string SettingsNavigationUri { get; set; }
        public event EventHandler SettingsInvoked;

        private object PreviousItem
        {
            get;set;
        }

        public new object SelectedItem
        {
            set => SetSelectedItem(value);
            get => base.SelectedItem;
        }

        private async void SetSelectedItem(object selectedItem, bool withNavigation = true)
        {
            if (selectedItem == null)
            {
                base.SelectedItem = null;
            }
            else if (selectedItem == PreviousItem)
            {
                // already set
            }
            else if (selectedItem == SettingsItem)
            {
                if (SettingsNavigationUri != null)
                {
                    if (withNavigation)
                    {
                        if ((await NavigationService.NavigateAsync(SettingsNavigationUri)).Success)
                        {
                            PreviousItem = selectedItem;
                            base.SelectedItem = selectedItem;
                            SettingsInvoked?.Invoke(this, EventArgs.Empty);
                        }
                        else
                        {
                            base.SelectedItem = null;
                        }
                    }
                    else
                    {
                        PreviousItem = selectedItem;
                        base.SelectedItem = selectedItem;
                    }


                }
            }
            else if (selectedItem is NavigationViewItem item)
            {
                if (item.GetValue(NavViewProps.NavigationUriProperty) is string path)
                {
			        if (!withNavigation)
			        {
				        PreviousItem = item;
				        base.SelectedItem = item;
			        }
			        else if ((await NavigationService.NavigateAsync(path)).Success)
			        {
				        PreviousItem = selectedItem;
				        base.SelectedItem = selectedItem;
			        }
			        else
			        {
				        base.SelectedItem = PreviousItem;
				        Debug.WriteLine($"{selectedItem}.{nameof(NavViewProps.NavigationUriProperty)} navigation failed.");
			        }
                }
                else
                {
                    Debug.WriteLine($"{selectedItem}.{nameof(NavViewProps.NavigationUriProperty)} is not valid Uri");
                }
            }
        }

        private bool TryFindItem(Type type,object parameter, out object item)
        {
            // registered?

            if (!PageRegistry.TryGetRegistration(type, out var info))
            {
                item = null;
                return false;
            }

            // search settings

            if (NavigationQueue.TryParse(SettingsNavigationUri, null, out var settings))
            {
                if (type == settings.Last().View && (string)parameter == settings.Last().QueryString)
                {
                    item = SettingsItem;
                    return true;
                }
                else
                {
                    // not settings
                }
            }

            // filter menu items

            var menuItems = MenuItems
                .OfType<NavigationViewItem>()
                .Select(x => new
                {
                    Item = x,
                    Path = x.GetValue(NavViewProps.NavigationUriProperty) as string
                })
                .Where(x => !string.IsNullOrEmpty(x.Path));

            // search filtered items

            foreach (var menuItem in menuItems)
            {
                if (NavigationQueue.TryParse(menuItem.Path, null, out var menuQueue)
                    && Equals(menuQueue.Last().View, type) && menuQueue.Last().QueryString == (string)parameter)
                {
                    item = menuItem.Item;
                    return true;
                }
            }

            // not found

            item = null;
            return false;
        }

        private NavigationViewItem Find(NavigationViewItem item)
        {
            return this.MenuItems.OfType<NavigationViewItem>().SingleOrDefault(x => x.Equals(item));
            //return this.MenuItems.OfType<NavigationViewItem>().SingleOrDefault(x => x.Content.Equals(content));
        }
    }
}
