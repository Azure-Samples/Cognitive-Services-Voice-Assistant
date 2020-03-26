// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace JsonViewerControl.ValueConverters
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Windows.Data;
    using Newtonsoft.Json.Linq;

    // This converter is only used by JProperty tokens whose Value is Array/Object
#pragma warning disable CA1812 // Avoid uninstantiated internal classes
    internal class ComplexPropertyMethodToValueConverter : IValueConverter
#pragma warning restore CA1812 // Avoid uninstantiated internal classes
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

            var invocationResult = methodInfo.Invoke(value, Array.Empty<object>());
            var jTokens = (IEnumerable<JToken>)invocationResult;
            return jTokens.First().Children();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException(this.GetType().Name + " can only be used for one way conversion.");
        }
    }
}
