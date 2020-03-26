// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace JsonViewerControl.ValueConverters
{
    using System;
    using System.Globalization;
    using System.Windows.Data;

    public sealed class MethodToValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var methodName = parameter as string;
            if (value == null || methodName == null)
            {
                return null;
            }

            var methodInfo = value.GetType().GetMethod(methodName, Array.Empty<Type>());
            if (methodInfo == null)
            {
                return null;
            }

            var returnValue = methodInfo.Invoke(value, Array.Empty<object>());
            return returnValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException(this.GetType().Name + " can only be used for one way conversion.");
        }
    }
}
