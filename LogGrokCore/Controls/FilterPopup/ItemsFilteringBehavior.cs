using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using ImTools;

namespace LogGrokCore.Controls.FilterPopup
{
    public static class ItemsFilteringBehavior
    {
        public static readonly DependencyProperty TextFilterProperty = DependencyProperty.RegisterAttached(
            "TextFilter", typeof(string), typeof(ItemsFilteringBehavior), 
            new PropertyMetadata(null, TextFilterChanged));

        public static void SetTextFilter(ItemsControl element, string? value)
        {
            element.SetValue(TextFilterProperty, value);
        }

        public static string? GetTextFilter(ItemsControl element)
        {
            return (string) element.GetValue(TextFilterProperty);
        }

        public static readonly DependencyProperty FilteredPropertyProperty = DependencyProperty.RegisterAttached(
            "FilteredProperty", typeof(string), typeof(ItemsFilteringBehavior), 
            new PropertyMetadata(default(string)));

        public static void SetFilteredProperty(ItemsControl control, string value)
        {
            control.SetValue(FilteredPropertyProperty, value);
        }

        public static string GetFilteredProperty(ItemsControl control)
        {
            return (string) control.GetValue(FilteredPropertyProperty);
        }

        private static Type? GetEnumerableInterface(Type type)
        {
            return type.GetInterfaces()
                .Concat(type.Yield())
                .FirstOrDefault(
                    o => o.IsGenericType 
                         && o.GetGenericTypeDefinition() == typeof(IEnumerable<>));
        }
        
        private static Type? GetEnumerationType(Type type)
        {
            return GetEnumerableInterface(type)?.GetGenericArguments().First();            
        }
        
       private static void TextFilterChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args)
        {
            var itemsControl = (ItemsControl)dependencyObject;
            var filteredPropertyName = GetFilteredProperty(itemsControl);

            var items = itemsControl.ItemsSource switch
            {
                ICollectionView itemsSource => itemsSource.SourceCollection,
                { } itemsSource => itemsSource
            };

            if (items == null) return;
            
            var filteredProperty = GetEnumerationType(items.GetType())?.GetProperty(filteredPropertyName);
            if (filteredProperty == null) return;
            
            var textFilter = (string)args.NewValue;
            itemsControl.Items.Filter = CreateItemsFilter(textFilter, filteredProperty);
            itemsControl.Items.Refresh();
        }
        
        private static Predicate<object>? CreateItemsFilter(string textFilter, PropertyInfo  filteredProperty)
        {
            if (string.IsNullOrWhiteSpace(textFilter))
                return null; 
            
            bool Filter(object item)
            {
                var value = filteredProperty.GetValue(item, null);
                if (value == null) return false;
                return value.ToString()?.IndexOf(textFilter, StringComparison.OrdinalIgnoreCase) >= 0;
            }

            return Filter;
        }
    }
}
