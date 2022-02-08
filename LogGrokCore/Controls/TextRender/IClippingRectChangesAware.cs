using System.Windows;

namespace LogGrokCore.Controls.TextRender;

public interface IClippingRectChangesAware
{
    void OnChildRectChanged(Rect rect);
}