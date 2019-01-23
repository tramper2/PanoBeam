using System;
using System.Windows;
using System.Windows.Data;
using PanoBeam.BlendControls.CurveControl.Enums;

namespace PanoBeam.BlendControls.CurveControl
{
    public class ControlPointVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var param = parameter as string;
            var cpt = value as ControlPointType?;
            if (param == "Spline")
            {
                if (cpt == ControlPointType.Spline)
                {
                    return Visibility.Visible;
                }
            }
            else if (param == "Line")
            {
                if (cpt == ControlPointType.Line)
                {
                    return Visibility.Visible;
                }
            }
            return Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}