using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Xceed.Wpf.AvalonDock;

namespace LogGrokCore.AvalonDock
{
    public static class DocumentContextMenu
    {
        public static DependencyProperty AdditionalContextMenuItemsProperty =
                DependencyProperty.RegisterAttached(
                    "AdditionalContextMenuItemsInternal",
                    typeof(MenuItemCollection),
                    typeof(DocumentContextMenu),
                    new PropertyMetadata(OnChanged));

        public static MenuItemCollection GetAdditionalContextMenuItems(DockingManager d)
        {
            return (MenuItemCollection)d.GetValue(AdditionalContextMenuItemsProperty);
        }

        public static void SetAdditionalContextMenuItems(DockingManager d, MenuItemCollection items)
        {
            d.SetValue(AdditionalContextMenuItemsProperty, items);
        }

        private static void OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var dockingManager = (DockingManager)d;

            HashSet<object> GetItems(object obj)
            {
                return obj switch
                {
                    null => new HashSet<object>(),
                    MenuItemCollection collection => new HashSet<object>(collection),
                    _ => throw new InvalidOperationException()
                };
            }

            var oldItems = GetItems(e.OldValue);
            var newItems = GetItems(e.NewValue);

            void UpdateContextMenu(ContextMenu contextMenu)
            {
                var contextMenuItems = contextMenu.Items;
                foreach (var item in oldItems.Except(newItems))
                {
                    contextMenuItems.Remove(item);
                }

                foreach (var item in newItems.Except(oldItems))
                {
                    contextMenuItems.Add(item);
                }
            }

            ContextMenu? documentContextMenu = dockingManager.DocumentContextMenu;
            if (documentContextMenu != null)
            {
                UpdateContextMenu(documentContextMenu);
            }
            else
            {
                var descriptor =
                    DependencyPropertyDescriptor.FromProperty(
                        DockingManager.DocumentContextMenuProperty, typeof(DockingManager));
                descriptor.AddValueChanged(dockingManager, (o, e) =>
                {
                    ContextMenu? contextMenu = dockingManager.DocumentContextMenu;
                    if (contextMenu != null)
                    {
                        UpdateContextMenu(contextMenu);
                    }
                });
            }
        }
    }
}
