using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media.Media3D;

namespace WPF3D.ToolBox.Converters
{
    public sealed class Rotate3DConverter
        : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double doubleValue = System.Convert.ToDouble(value);
            Vector3D axis = (Vector3D) System.Convert.ChangeType(parameter, typeof(Vector3D)); 

            return new AxisAngleRotation3D(axis, doubleValue);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}