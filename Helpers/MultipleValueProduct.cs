using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace OpenCVVideoRedactor.Helpers
{
    class MultipleValueProduct : IMultiValueConverter
    {

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var thickness = values[0] as Thickness?;
            if (thickness != null)
            {
                var product = values.Skip(1).Select(n => n as double?).Aggregate((n, m) => n * m);
                if (product == null) return thickness;
                var value = product.Value;
                return new Thickness(thickness.Value.Left*value, thickness.Value.Top * value, thickness.Value.Right * value, thickness.Value.Bottom * value);
            }
            return values.Select(n => n as double?).Aggregate((n, m) => n * m) ?? values[0];
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
