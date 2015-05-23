using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace SwipCardSystem.View.Converter
{
    public class BooleanToVisibily : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if ((bool)value)
            {
                if (bool.Parse(parameter.ToString()))
                {                    
                    return Visibility.Hidden;
                }
                else
                {
                    return Visibility.Visible;
                }
            }
            else
            {
                if (bool.Parse(parameter.ToString()))
                {
                    return Visibility.Visible;
                }
                else
                {
                    return Visibility.Hidden;
                }
            }
        }


        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }


    }
}
