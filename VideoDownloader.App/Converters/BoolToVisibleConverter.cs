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
			else
			{
				if (Collapse)
					return Visibility.Collapsed;
				else
					return Visibility.Hidden;
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			Visibility visibility = (Visibility)value;

			if (visibility == Visibility.Visible)
				return true;
			else
				return false;
		}
		#endregion
	}
}
