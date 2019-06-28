using System;
using System.Drawing.Text;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ImTools;
using LogGrokCore.Controls.GridView;
using LogGrokCore.Data;

namespace LogGrokCore
{
    public class HeaderViewModel
    {
        public HeaderViewModel(string name)
        {
            Name = name;
        }

        public string Name { get; }
    }

    public class GridViewFactory
    {
        private readonly LogMetaInformation _meta;

        public GridViewFactory(LogMetaInformation meta)
        {
            _meta = meta;
        }

        public ViewBase CreateView()
        {
            var indexFieldName = "Index";
            var view = new GridView();
            foreach (var fieldHeader in indexFieldName.Yield().Concat(_meta.FieldNames))
            {
                DataTemplate CreateHeaderTemplate()
                {
                        var frameworkElementFactory = new FrameworkElementFactory(typeof(LogGridViewHeader));
                        frameworkElementFactory.SetValue(FrameworkElement.DataContextProperty, fieldHeader);
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
                        var idx = _meta.FieldNames.IndexOf(fieldName);
                        return ln => ln.GetValue(idx);
                    }

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