using System;
using System.Collections.Generic;
using System.Text;

using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace Prism
{
    internal static partial class DependencyObjectExtensions
    {
        /// <summary>
        /// Compatibility method to determine if the current thread can access a <see cref="DependencyObject"/>
        /// </summary>
        /// <param name="instance">The instance to check</param>
        /// <returns><c>true</c> if the current thread has access to the instance, otherwise <c>false</c></returns>
        public static bool CheckAccess(this DependencyObject instance)
            => instance.Dispatcher.HasThreadAccess;

        /// <summary>
        /// Determines if a <see cref="DependencyProperty"/> has a binding set
        /// </summary>
        /// <param name="instance">The to use to search for the property</param>
        /// <param name="property">The property to search</param>
        /// <returns><c>true</c> if there is an active binding, otherwise <c>false</c></returns>
        public static bool HasBinding(this FrameworkElement instance, DependencyProperty property)
            => instance.GetBindingExpression(property) != null;
    }
}
