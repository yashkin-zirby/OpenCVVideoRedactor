using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace OpenCVVideoRedactor.Helpers
{
    public class DropBehaviorExtension
    {
        public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
        "IsEnabled", typeof(bool), typeof(DropBehaviorExtension), new FrameworkPropertyMetadata(default(bool), OnPropChanged)
        {
            BindsTwoWayByDefault = false,
        });
        public static readonly DependencyProperty DataTypeProperty = DependencyProperty.RegisterAttached(
        "DataType", typeof(Type), typeof(DropBehaviorExtension), new FrameworkPropertyMetadata(default(Type))
        {
            BindsTwoWayByDefault = false,
        });
        private static void OnPropChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is FrameworkElement fe))
                throw new InvalidOperationException();
            if (e.NewValue is bool && (bool)e.NewValue)
            {
                fe.AllowDrop = true;
                fe.Drop += OnDrop;
                fe.PreviewDragOver += OnPreviewDragOver;
            }
            else
            {
                fe.AllowDrop = false;
                fe.Drop -= OnDrop;
                fe.PreviewDragOver -= OnPreviewDragOver;
            }
        }

        private static void OnPreviewDragOver(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Move;
            e.Handled = true;
        }

        private static void OnDrop(object sender, DragEventArgs e)
        {
            var fe = (FrameworkElement)sender;
            var type = fe.GetValue(DataTypeProperty) as Type;
            var dataContext = fe.DataContext;
            if (!(dataContext is IDropHandler filesDropped))
            {
                if (dataContext != null)
                    Trace.TraceError($"Binding error, '{dataContext.GetType().Name}' doesn't implement '{nameof(IDropHandler)}'.");
                return;
            }
            object? sourceData = null;
            if (fe is ItemsControl) {
                sourceData = ResourceHelper.GetDataFromItemsControl((ItemsControl)fe,e.GetPosition(fe));
            }
            if (type == null || e.Data.GetData(type) != null && sourceData != e.Data.GetData(type))
                filesDropped.OnDataDropped(e.Data, sourceData);
        }

        public static void SetIsEnabled(DependencyObject element, bool value)
        {
            element.SetValue(IsEnabledProperty, value);
        }

        public static bool GetIsEnabled(DependencyObject element)
        {
            return (bool)element.GetValue(IsEnabledProperty);
        }
        public static void SetDataType(DependencyObject element, Type value)
        {
            element.SetValue(DataTypeProperty, value);
        }

        public static Type GetDataType(DependencyObject element)
        {
            return (Type)element.GetValue(DataTypeProperty);
        }
    }
}
