﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Prism.Ioc;
using Prism.Logging;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;

namespace Prism.Navigation
{
    public class NavigationService : INavigationService, INavigationService2
    {
        IFrameFacade INavigationService2.FrameFacade => _frame;

        public static Dictionary<Frame, INavigationService> Instances { get; } = new Dictionary<Frame, INavigationService>();

        private readonly IFrameFacade _frame;
        private readonly ILoggerFacade _logger;

        internal NavigationService(Frame frame, string id)
        {
            _frame = new FrameFacade(frame, this, id);
            _frame.CanGoBackChanged += (s, e) =>
                CanGoBackChanged?.Invoke(this, EventArgs.Empty);
            _frame.CanGoForwardChanged += (s, e) =>
                CanGoForwardChanged?.Invoke(this, EventArgs.Empty);
            Instances.Add(frame, this);
            _logger = PrismApplicationBase.Current.Container.Resolve<ILoggerFacade>();
        }

        public async Task RefreshAsync()
            => await _frame.RefreshAsync();

        #region GoForward

        public event EventHandler CanGoForwardChanged;

        public bool CanGoForward()
            => _frame.CanGoForward();

        public async Task<INavigationResult> GoForwardAsync()
            => await GoForwardAsync(
                parameters: default);

        public async Task<INavigationResult> GoForwardAsync(INavigationParameters parameters)
        {
            if (parameters == null && (_frame as IFrameFacade2).Frame.ForwardStack.Any())
            {
                var previous = (_frame as IFrameFacade2).Frame.ForwardStack.Last().Parameter?.ToString();
                parameters = new NavigationParameters(previous);
            }

            return await _frame.GoForwardAsync(
                  parameters: parameters);
        }
        #endregion

        #region GoBack

        public event EventHandler CanGoBackChanged;

        public bool CanGoBack()
            => _frame.CanGoBack();

        /// <summary>
        /// Navigates to the most recent entry in the back navigation history by popping the calling Page off the navigation stack.
        /// </summary>
        /// <returns>If <c>true</c> a go back operation was successful. If <c>false</c> the go back operation failed.</returns>
        public async Task<INavigationResult> GoBackAsync()
            => await GoBackAsync(
                parameters: default,
                infoOverride: default);

        /// <summary>
        /// Navigates to the most recent entry in the back navigation history by popping the calling Page off the navigation stack.
        /// </summary>
        /// <param name="parameters">The navigation parameters</param>
        /// <returns>If <c>true</c> a go back operation was successful. If <c>false</c> the go back operation failed.</returns>
        public async Task<INavigationResult> GoBackAsync(INavigationParameters parameters)
            => await GoBackAsync(
                parameters: parameters,
                infoOverride: default);

        public async Task<INavigationResult> GoBackAsync(INavigationParameters parameters = null, NavigationTransitionInfo infoOverride = null)
        {
            if (parameters == null && (_frame as IFrameFacade2).Frame.BackStack.Any())
            {
                var previous = (_frame as IFrameFacade2).Frame.BackStack.Last().Parameter?.ToString();
                if (previous is null)
                {
                    parameters = new NavigationParameters();
                }
                else
                {
                    parameters = new NavigationParameters(previous);
                }
            }

            return await _frame.GoBackAsync(
                    parameters: parameters,
                    infoOverride: infoOverride);
        }
        #endregion

        #region Navigate(string)

        public async Task<INavigationResult> NavigateAsync(string path)
            => await NavigateAsync(
                uri: new Uri(path, UriKind.RelativeOrAbsolute),
                parameter: default,
                infoOverride: default);

        public async Task<INavigationResult> NavigateAsync(string path, INavigationParameters parameters)
            => await NavigateAsync(
                uri: new Uri(path, UriKind.RelativeOrAbsolute),
                parameter: parameters,
                infoOverride: default);

        public async Task<INavigationResult> NavigateAsync(string path, INavigationParameters parameter, NavigationTransitionInfo infoOverride)
            => await NavigateAsync(
                uri: new Uri(path, UriKind.RelativeOrAbsolute),
                parameter: parameter,
                infoOverride: infoOverride);
        #endregion

        #region Navigate(Uri)

        public async Task<INavigationResult> NavigateAsync(Uri uri)
            => await NavigateAsync(
                uri: uri,
                parameter: default,
                infoOverride: default);

        public async Task<INavigationResult> NavigateAsync(Uri uri, INavigationParameters parameters)
            => await NavigateAsync(
                uri: uri,
                parameter: parameters,
                infoOverride: default);

        public async Task<INavigationResult> NavigateAsync(Uri uri, INavigationParameters parameter, NavigationTransitionInfo infoOverride)
        {
            _logger.Log($"{nameof(NavigationService)}.{nameof(NavigateAsync)}(uri:{uri} parameter:{parameter} info:{infoOverride})", Category.Info, Priority.None);

            return await _frame.NavigateAsync(
                uri: uri,
                parameter: parameter,
                infoOverride: infoOverride);
        }
        #endregion
    }
}
