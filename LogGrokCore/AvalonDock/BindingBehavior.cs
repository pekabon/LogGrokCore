using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Xceed.Wpf.AvalonDock;

namespace LogGrokCore.AvalonDock
{
    public static class BindingBehavior
    {
        public static readonly DependencyProperty DocumentsSourceProperty;

        public static readonly DependencyProperty CurrentDocumentProperty;

        public static readonly DependencyProperty DocumentViewTemplateProperty;

        public static readonly DependencyProperty DocumentViewTemplateSelectorProperty;

        public static readonly DependencyProperty ObservableCollectionFactoryLinkProperty;

        public static readonly DependencyProperty OnDocumentCloseCommandProperty;

        static BindingBehavior()
        {
            DocumentsSourceProperty = DependencyProperty.RegisterAttached("DocumentsSource", typeof(IList), typeof(BindingBehavior), new PropertyMetadata(null, OnChanged), null);
            CurrentDocumentProperty = DependencyProperty.RegisterAttached("CurrentDocument", typeof(object), typeof(BindingBehavior), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnCurrentDocumentChanged), null);
            DocumentViewTemplateProperty = DependencyProperty.RegisterAttached("DocumentViewTemplate", typeof(DataTemplate), typeof(BindingBehavior), new PropertyMetadata(null, OnChanged), null);
            DocumentViewTemplateSelectorProperty = DependencyProperty.RegisterAttached("DocumentViewTemplateSelector", typeof(DataTemplateSelector), typeof(BindingBehavior), new PropertyMetadata(null, OnChanged), null);
            ObservableCollectionFactoryLinkProperty = DependencyProperty.RegisterAttached("ObservableCollectionFactoryLink", typeof(object), typeof(BindingBehavior), null, null);
            OnDocumentCloseCommandProperty = DependencyProperty.RegisterAttached("OnDocumentCloseCommand", typeof(ICommand), typeof(BindingBehavior), null, null);
        }

        public static object? GetObservableCollectionFactoryLink(DockingManager dockingManager)
        {
            return dockingManager.GetValue(ObservableCollectionFactoryLinkProperty);
        }

        public static IList? GetDocumentsSource(DockingManager dockingManager)
        {
            return (IList)dockingManager.GetValue(DocumentsSourceProperty);
        }

        public static void SetDocumentsSource(DockingManager dockingManager, IList value)
        {
            dockingManager.SetValue(DocumentsSourceProperty, value);
        }

        public static void SetDocumentViewTemplate(DockingManager dockingManager, DataTemplate value)
        {
            dockingManager.SetValue(DocumentViewTemplateProperty, value);
        }

        public static void SetObservableCollectionFactoryLink(DockingManager dockingManager, object value)
        {
            dockingManager.SetValue(ObservableCollectionFactoryLinkProperty, value);
        }

        public static ICommand? GetOnDocumentCloseCommand(DockingManager dockingManager)
        {
            return (ICommand)dockingManager.GetValue(OnDocumentCloseCommandProperty);
        }

        public static void SetCurrentDocument(DockingManager dockingManager, object value)
        {
            dockingManager.SetValue(CurrentDocumentProperty, value);
        }

        public static DataTemplate? GetDocumentViewTemplate(DockingManager dockingManager)
        {
            return (DataTemplate)dockingManager.GetValue(DocumentViewTemplateProperty);
        }

        public static DataTemplateSelector? GetDocumentViewTemplateSelector(DockingManager dockingManager)
        {
            return (DataTemplateSelector)dockingManager.GetValue(DocumentViewTemplateSelectorProperty);
        }

        private static void OnCurrentDocumentChanged(DependencyObject d, DependencyPropertyChangedEventArgs args)
        {
            var dockingManager = (DockingManager)d;

            var documentToDocumentViewLink =
                (ObservableCollectionFactoryLink<UIElement>?)GetObservableCollectionFactoryLink(dockingManager);

            if (documentToDocumentViewLink?.TargetFromSource(args.NewValue) != null)
            {
                dockingManager.ActiveContent = documentToDocumentViewLink.TargetFromSource(args.NewValue);
            }
        }

        private static void OnChanged(DependencyObject d , DependencyPropertyChangedEventArgs _)
        {
            var dockingManager = (DockingManager)d;

            void SetDocumentsSourceWithViewFactory(IList documentsSource, Func<object, UIElement> factory)
            {
                var targetCollection = new ObservableCollection<UIElement>();
                var documentToDocumentViewLink =
                    new ObservableCollectionFactoryLink<UIElement>(documentsSource, targetCollection, factory);

                SetObservableCollectionFactoryLink(dockingManager, documentToDocumentViewLink);
                dockingManager.DocumentsSource = targetCollection;

                dockingManager.DocumentClosed += (_, args) =>
                {
                    var closedSource = documentToDocumentViewLink.SourceFromTarget((UIElement)args.Document.Content);
                    documentsSource.Remove(closedSource);

                    var command = GetOnDocumentCloseCommand(dockingManager);
                    command?.Execute(closedSource);
                };

                dockingManager.ActiveContentChanged += (_, __) =>
                        {
                            var activeSource =
                                documentToDocumentViewLink.SourceFromTarget((UIElement)dockingManager.ActiveContent);

                            if (activeSource is object value)
                            {
                                SetCurrentDocument(dockingManager, value);
                            }
                        };
            }

            void SetDocumentsSourceWithTemplate(IList documents, DataTemplate datatemplate)
            {
                UIElement DocumentViewFactory(object doc)
                {
                    return new ContentControl() { Content = doc, ContentTemplate = datatemplate};
                }

                SetDocumentsSourceWithViewFactory(documents, DocumentViewFactory);
            }

            void SetDocumentsSourceWithTemplateSelector(IList documents, DataTemplateSelector templateSelector)
            {
                UIElement DocumentViewFactory(object doc)
                {
                    return new ContentControl { Content = doc,
                        ContentTemplate = templateSelector.SelectTemplate(doc, null) };
                }

                SetDocumentsSourceWithViewFactory(documents, DocumentViewFactory);
            }

            var (ds, dt, dts) =
                (GetDocumentsSource(dockingManager),
                GetDocumentViewTemplate(dockingManager),
                GetDocumentViewTemplateSelector(dockingManager));

            switch ((ds, dt, dts))
            {
                case (null, _, _):
                case (_, null, null): break;
                case (IList documents, DataTemplate template, null):
                    SetDocumentsSourceWithTemplate(documents, template);
                    break;
                case (IList documents, null, DataTemplateSelector templateSelector):
                    SetDocumentsSourceWithTemplateSelector(documents, templateSelector);
                    break;
                case (_, object dd, object t) when dd != null && t != null:
                    throw new InvalidOperationException("Unable set DocumentViewTemplate & DocumentViewTemplateSelector simultaniously");
                case (_, _, _) : break;
            }
        }
    }
}
