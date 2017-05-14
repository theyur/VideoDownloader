using System;
using System.Windows;
using System.Windows.Data;

namespace VideoDownloader.App.Converters
{
	public class BoolToVisibleConverter : IValueConverter
	{
		#region Constructors

		#endregion

		#region Properties
		public bool Collapse { get; set; }

		#endregion

		#region IValueConverter Members
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			bool bValue = System.Convert.ToBoolean(value);
			if (bValue)
			{
				return Visibility.Visible;
			}
		    return Collapse ? Visibility.Collapsed : Visibility.Hidden;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			Visibility visibility = (Visibility)value;

			return visibility == Visibility.Visible;
		}
		#endregion
	}
}
