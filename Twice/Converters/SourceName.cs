﻿using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Twice.Models.Twitter;

namespace Twice.Converters
{
	internal class SourceName : IValueConverter
	{
		public object Convert( object value, Type targetType, object parameter, CultureInfo culture )
		{
			string str = value as string;

			return str == null
				? DependencyProperty.UnsetValue
				: new TweetSource( str ).Name;
		}

		public object ConvertBack( object value, Type targetType, object parameter, CultureInfo culture )
		{
			throw new NotSupportedException();
		}
	}
}