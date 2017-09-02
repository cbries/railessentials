using System.Windows;
using System.Windows.Controls;

namespace RailwayEssentialMdi.Styles
{
    public class PanesStyleSelector : StyleSelector
    {
        public Style ToolStyle
        {
            get;
            set;
        }

        public override System.Windows.Style SelectStyle(object item, System.Windows.DependencyObject container)
        {
            if (item is ViewModels.RailwayEssentialModel)
                return ToolStyle;

            return base.SelectStyle(item, container);
        }
    }
}
