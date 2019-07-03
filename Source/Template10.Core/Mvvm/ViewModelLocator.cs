﻿using System;
using System.Linq;
using System.Reflection;
using Windows.UI.Xaml;

namespace Template10.Mvvm
{
    public class ViewModelLocator
    {
        public static bool? GetAutowireViewModel(DependencyObject obj)
        {
            return (bool?)obj.GetValue(AutowireViewModelProperty);
        }
        public static void SetAutowireViewModel(DependencyObject obj, bool? value)
        {
            obj.SetValue(AutowireViewModelProperty, value);
        }
        public static readonly DependencyProperty AutowireViewModelProperty =
            DependencyProperty.RegisterAttached("AutowireViewModel", typeof(bool?),
                typeof(ViewModelLocator), new PropertyMetadata(null, AutowireViewModelChanged));
        private static void AutowireViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!Windows.ApplicationModel.DesignMode.DesignModeEnabled)
            {
                if (((bool?)e.NewValue) == true)
                {
                    Prism.Mvvm.ViewModelLocationProvider.AutoWireViewModelChanged(d, Bind);
                }
            }
        }
        private static void Bind(object view, object viewmodel)
        {
            (view as FrameworkElement).DataContext = viewmodel;
        }
    }
}
