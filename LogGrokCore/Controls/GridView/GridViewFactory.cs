using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using LogGrokCore.Data;
using LogGrokCore.Filter;

namespace LogGrokCore.Controls.GridView
{
    public class GridViewFactory
    {
        private readonly LogMetaInformation _meta;
        private readonly Func<string, FilterViewModel>? _filterViewModelFactory;
        public GridViewFactory(LogMetaInformation meta, 
                bool canFilter,
                Func<string, FilterViewModel>? filterViewModelFactory)
        {
            _meta = meta;
            if (canFilter && filterViewModelFactory != null)
                _filterViewModelFactory = filterViewModelFactory;
            else if (canFilter && filterViewModelFactory == null)
                throw new ArgumentException($"filterViewModelFactory cannot be null if canFilter is true.");
            else if (!canFilter && filterViewModelFactory != null)
                throw new ArgumentException($"filterViewModelFactory must be null if canFilter is false.");
        }

        public ViewBase CreateView()
        {
            var indexFieldName = "Index";
            var view = new System.Windows.Controls.GridView();

            view.Columns.Add(new LogGridViewColumn
            {
                HeaderTemplate = new DataTemplate(typeof(DependencyObject))
                {
                    VisualTree = new FrameworkElementFactory(typeof(PinGridViewhHeader))
                },
                CellTemplate = new DataTemplate(typeof(DependencyObject))
                {
                    VisualTree = new FrameworkElementFactory(typeof(PinGridViewCell))
                }
            });
            
            foreach (var fieldHeader in indexFieldName.Yield().Concat(_meta.FieldNames))
            {
                DataTemplate CreateHeaderTemplate()
                {
                    var frameworkElementFactory = new FrameworkElementFactory(typeof(LogGridViewHeader));

                    FilterViewModel? filterViewModel = null;
                    if (_filterViewModelFactory != null && _meta.IsFieldIndexed(fieldHeader))
                    {
                        filterViewModel = _filterViewModelFactory(fieldHeader);
                    }
                    
                    frameworkElementFactory.SetValue(FrameworkElement.DataContextProperty, 
                        new HeaderViewModel(fieldHeader, filterViewModel));
                     
                    var dataTemplate = new DataTemplate(typeof(DependencyObject))
                    {
                        VisualTree = frameworkElementFactory
                    };
                    return dataTemplate;
                }
                
                DataTemplate CreateCellTemplate()
                {
                    var frameworkElementFactory = new FrameworkElementFactory(typeof(LogGridViewCell));

                    Func<LineViewModel, string> GetComponent(string fieldName)
                    {
                        var idx = Array.IndexOf(_meta.FieldNames, fieldName);
                        return ln => ln.GetValue(idx);
                    }

                    if (fieldHeader == null) throw new InvalidOperationException();
                    var valueGetter =
                        fieldHeader == indexFieldName 
                            ? ln => ln.Index.ToString()
                            : GetComponent(fieldHeader);
                    
                    frameworkElementFactory.SetValue(LogGridViewCell.ValueGetterProperty,valueGetter);
                    var dataTemplate = new DataTemplate(typeof(DependencyObject))
                    {
                        VisualTree = frameworkElementFactory
                    };
                    
                    return dataTemplate;
                }

                view.Columns.Add(new LogGridViewColumn
                {  
                    HeaderTemplate = CreateHeaderTemplate(),
                    CellTemplate = CreateCellTemplate()
                });
            }

            return view;
        }
    }
}