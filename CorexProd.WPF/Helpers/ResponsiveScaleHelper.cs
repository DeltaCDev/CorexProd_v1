using System.Windows;
using System.Windows.Media;

namespace CorexProd.WPF.Helpers
{
    public static class ResponsiveScaleHelper
    {
        private static readonly DependencyProperty IsScaleAppliedProperty =
            DependencyProperty.RegisterAttached(
                "IsScaleApplied",
                typeof(bool),
                typeof(ResponsiveScaleHelper),
                new PropertyMetadata(false));

        public static void Apply(Window window)
        {
            if (!ShouldScaleWindow(window)
                || window.Content is not FrameworkElement content
                || (bool)content.GetValue(IsScaleAppliedProperty))
            {
                return;
            }

            double scale = GetScale();
            if (scale <= 1.0)
            {
                return;
            }

            content.SetValue(IsScaleAppliedProperty, true);
            content.LayoutTransform = new ScaleTransform(scale, scale);
        }

        private static bool ShouldScaleWindow(Window window) =>
            window is MainWindow
            || window.WindowState == WindowState.Maximized
            || window.Width >= 1000
            || window.Height >= 700;

        private static double GetScale()
        {
            double width = SystemParameters.WorkArea.Width;
            double height = SystemParameters.WorkArea.Height;

            if (width >= 2500 || height >= 1350)
            {
                return 1.50;
            }

            if (width >= 1800 || height >= 1000)
            {
                return 1.28;
            }

            if (width >= 1600 || height >= 920)
            {
                return 1.10;
            }

            return 1.0;
        }
    }
}
