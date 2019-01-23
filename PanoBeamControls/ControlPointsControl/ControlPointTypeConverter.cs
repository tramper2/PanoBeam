using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace PanoBeam.Controls.ControlPointsControl
{
    public class ControlPointTypeConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(parameter == null) return Visibility.Collapsed;
            if (value == null) return Visibility.Collapsed;
            var type = (ControlPointType)parameter;
            if ((ControlPointType)value == type)
            {
                return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}