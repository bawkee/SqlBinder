using System;
using System.Globalization;
using System.Windows.Controls;
using System.Windows.Data;

namespace SqlBinder.DemoApp.Converters
{
	public class ComboBoxNullItemConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value;
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value is ComboBoxItem ? null : value;
	}
}
