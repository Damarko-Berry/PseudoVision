using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;

namespace PVChannelManager
{
    public class FillPanal : StackPanel
    {
        protected override Size ArrangeOverride(Size arrangeSize)
        {
            double totalHeight = arrangeSize.Height;
            double totalWidth = arrangeSize.Width;
            int childCount = InternalChildren.Count;

            if (Orientation == Orientation.Vertical)
            {
                double childHeight = totalHeight / childCount;
                for (int i = 0; i < childCount; i++)
                {
                    UIElement child = InternalChildren[i];
                    child.Arrange(new Rect(0, i * childHeight, totalWidth, childHeight));
                }
            }
            else
            {
                double childWidth = totalWidth / childCount;
                for (int i = 0; i < childCount; i++)
                {
                    UIElement child = InternalChildren[i];
                    child.Arrange(new Rect(i * childWidth, 0, childWidth, totalHeight));
                }
            }

            return arrangeSize;
        }
    }
}
