using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CorexProd.WPF.Helpers
{
    public static class ReadableTextHelper
    {
        public static void Apply(DependencyObject element)
        {
            if (element is TextBlock textBlock && ShouldUseBlack(textBlock.Foreground))
            {
                textBlock.Foreground = Brushes.Black;
            }

            if (element is Control control && ShouldUseBlack(control.Foreground))
            {
                control.Foreground = Brushes.Black;
            }
        }

        private static bool ShouldUseBlack(Brush brush)
        {
            if (brush is not SolidColorBrush solidBrush)
            {
                return false;
            }

            Color color = solidBrush.Color;
            double luminance = GetLuminance(color);
            double saturation = GetSaturation(color);

            return luminance is > 0.05 and < 0.85 && saturation < 0.35;
        }

        private static double GetLuminance(Color color) =>
            ((0.2126 * color.R) + (0.7152 * color.G) + (0.0722 * color.B)) / 255;

        private static double GetSaturation(Color color)
        {
            double max = Math.Max(color.R, Math.Max(color.G, color.B)) / 255.0;
            double min = Math.Min(color.R, Math.Min(color.G, color.B)) / 255.0;

            if (max == 0)
            {
                return 0;
            }

            return (max - min) / max;
        }
    }
}
