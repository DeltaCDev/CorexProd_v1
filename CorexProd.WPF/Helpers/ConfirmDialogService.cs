using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CorexProd.WPF.Helpers
{
    public static class ConfirmDialogService
    {
        public static bool Confirmar(string mensaje, string titulo = "Confirmar acción")
        {
            Window ventana = new()
            {
                Title = titulo,
                Width = 420,
                Height = 190,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.NoResize,
                Background = Brushes.White,
                WindowStyle = WindowStyle.SingleBorderWindow
            };

            Grid grid = new()
            {
                Margin = new Thickness(20)
            };

            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            TextBlock texto = new()
            {
                Text = mensaje,
                FontSize = 15,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59))
            };

            StackPanel botones = new()
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 20, 0, 0)
            };

            Button btnSi = new()
            {
                Content = "Sí",
                Width = 90,
                Height = 34,
                Margin = new Thickness(0, 0, 10, 0)
            };

            Button btnNo = new()
            {
                Content = "No",
                Width = 90,
                Height = 34
            };

            bool resultado = false;

            btnSi.Click += (_, _) =>
            {
                resultado = true;
                ventana.Close();
            };

            btnNo.Click += (_, _) =>
            {
                resultado = false;
                ventana.Close();
            };

            botones.Children.Add(btnSi);
            botones.Children.Add(btnNo);

            Grid.SetRow(texto, 0);
            Grid.SetRow(botones, 1);

            grid.Children.Add(texto);
            grid.Children.Add(botones);

            ventana.Content = grid;
            ventana.ShowDialog();

            return resultado;
        }
    }
}