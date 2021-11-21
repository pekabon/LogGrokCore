using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
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
            _filterViewModelFactory = canFilter switch
            {
                true when filterViewModelFactory != null => filterViewModelFactory,
                true when filterViewModelFactory == null => throw new ArgumentException(
                    $"filterViewModelFactory cannot be null if canFilter is true."),
                false when filterViewModelFactory != null => throw new ArgumentException(
                    $"filterViewModelFactory must be null if canFilter is false."),
                _ => _filterViewModelFactory
            };
        }

        public ViewBase CreateView(double[]? widths)
        {
            if (widths != null && widths.Length != _meta.FieldNames.Length + 2)
            {
                throw new InvalidOperationException("Invalid columnWidthSettings");
            }

            var indexFieldName = "Index";
            var view = new System.Windows.Controls.GridView();

            view.Columns.Add(new LogGridViewColumn
            {
                HeaderTemplate = new DataTemplate(typeof(DependencyObject))
                {
                    VisualTree = new FrameworkElementFactory(typeof(PinGridViewhHeader))
                },
                CellTemplate =  CreatePinCellTemplate(),
                Width = widths == null? 0 : widths[0]
            });

            var columnIndex = 1;
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
                    var binding = new Binding
                    {
                        Path = 
                            fieldHeader == indexFieldName ? 
                                new PropertyPath(nameof(LineViewModel.IndexViewModel)) : 
                                new PropertyPath(".[(0)]", Array.IndexOf(_meta.FieldNames, fieldHeader)),
                        Mode = BindingMode.OneWay
                    };
                    frameworkElementFactory.SetBinding(ContentControl.ContentProperty, binding);
                    var dataTemplate = new DataTemplate(typeof(DependencyObject))
                    {
                        VisualTree = frameworkElementFactory
                    };
                    return dataTemplate;
                }

                view.Columns.Add(new LogGridViewColumn
                {  
                    HeaderTemplate = CreateHeaderTemplate(),
                    CellTemplate = CreateCellTemplate(),
                    Width = widths == null ? 0 : widths[columnIndex++]
                });
            }

            return view;
        }
        
        private static DataTemplate CreatePinCellTemplate()
        {
            var factory = new FrameworkElementFactory(typeof(PinControl));
            var binding = new Binding
            {
                Path = new PropertyPath(nameof(LineViewModel.IsMarked)),
                Mode = BindingMode.TwoWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            factory.SetBinding(ToggleButton.IsCheckedProperty, binding);
            return new DataTemplate {VisualTree = factory};
        }
    }
}