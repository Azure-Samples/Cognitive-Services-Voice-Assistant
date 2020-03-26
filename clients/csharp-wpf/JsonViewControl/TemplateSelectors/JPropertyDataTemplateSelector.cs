// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace JsonViewerControl.TemplateSelectors
{
    using System.Windows;
    using System.Windows.Controls;
    using Newtonsoft.Json.Linq;

    public sealed class JPropertyDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate PrimitivePropertyTemplate { get; set; }

        public DataTemplate ComplexPropertyTemplate { get; set; }

        public DataTemplate ArrayPropertyTemplate { get; set; }

        public DataTemplate ObjectPropertyTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item == null)
            {
                return null;
            }

            var frameworkElement = container as FrameworkElement;
            if (frameworkElement == null)
            {
                return null;
            }

            var type = item.GetType();
            if (type == typeof(JProperty))
            {
                var jProperty = item as JProperty;
                switch (jProperty.Value.Type)
                {
                    case JTokenType.Object:
                        return frameworkElement.FindResource("ObjectPropertyTemplate") as DataTemplate;
                    case JTokenType.Array:
                        return frameworkElement.FindResource("ArrayPropertyTemplate") as DataTemplate;
                    default:
                        return frameworkElement.FindResource("PrimitivePropertyTemplate") as DataTemplate;
                }
            }

            var key = new DataTemplateKey(type);
            return frameworkElement.FindResource(key) as DataTemplate;
        }
    }
}
