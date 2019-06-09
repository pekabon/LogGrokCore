using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows;

namespace LogGrokCore.Controls
{
    internal readonly struct VisibleItem : IEquatable<VisibleItem>
    {
        public VisibleItem(UIElement element, int index, double upperBound, double lowerBound)
        {
            Element = element;
            Index = index;
            UpperBound = upperBound;
            LowerBound = lowerBound;
            Debug.Assert(Height > 0);
        }

        public readonly UIElement Element;
        
        public readonly int Index;
        
        public readonly double UpperBound;
        
        public readonly double LowerBound;

        public double Height => LowerBound - UpperBound;

        public VisibleItem Move(double offset) => new VisibleItem(Element, Index, UpperBound + offset, LowerBound + offset);

        public VisibleItem MoveTo(double newUpperBound) => Move(newUpperBound - UpperBound);

        public void Deconstruct(out UIElement uiElement, out int index, out double upperBound, out double lowerBound)
        {
            uiElement = Element;
            index = Index;
            upperBound = UpperBound;
            lowerBound = LowerBound;
        }

        public override string ToString() => $"{Index}, {UpperBound:##.##} : {LowerBound:##.##} -- {Element}";

        public bool Equals(VisibleItem other)
        {
            return Element == other.Element && Index == other.Index 
                && UpperBound == other.UpperBound && LowerBound == other.LowerBound;
        }
    }
}
