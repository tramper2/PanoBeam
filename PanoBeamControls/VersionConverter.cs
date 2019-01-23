using System;
using System.Globalization;
using System.Reflection;
using System.Windows.Data;

namespace PanoBeam.Controls
{
    public class VersionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return $"Version {Assembly.GetExecutingAssembly().GetName().Version}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}