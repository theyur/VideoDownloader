using System;
using System.Windows;
using System.Windows.Data;

namespace VideoDownloader.App.Converters
{
	public class BoolToVisibleReversedConverter: IValueConverter
	{
		#region Constructors
		/// <summary>
		/// The default constructor
		/// </summary>

		#endregion

		#region Properties
		public bool Collapse { get; set; }

		#endregion

		#region IValueConverter Members
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			bool bValue = System.Convert.ToBoolean(value);
			if (!bValue)
			{
				return Visibility.Visible;
			}
			else
			{
				return Collapse ? Visibility.Collapsed : Visibility.Hidden;
			}
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			var visibility = (Visibility)value;
			return visibility == Visibility.Visible;
		}
		#endregion
	}
}
