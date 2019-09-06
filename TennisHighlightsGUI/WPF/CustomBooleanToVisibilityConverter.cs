using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace TennisHighlightsGUI
{
    /// <summary>
    /// The custom boolean to visibility converter
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <seealso cref="System.Windows.Data.IValueConverter" />
    public class CustomBooleanToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// Gets or sets the true.
        /// </summary>
        public Visibility True { get; set; }
        /// <summary>
        /// Gets or sets the false.
        /// </summary>
        public Visibility False { get; set; }

        /// <summary>
        /// Converts the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="targetType">Type of the target.</param>
        /// <param name="parameter">The parameter.</param>
        /// <param name="culture">The culture.</param>
        public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool && ((bool)value) ? True : False;
        }

        /// <summary>
        /// Converts back the value
        /// </returns>
        public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Visibility && EqualityComparer<Visibility>.Default.Equals((Visibility)value, True);
        }
    }
}
