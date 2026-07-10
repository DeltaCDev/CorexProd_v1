using CorexProd.App.Controls;

namespace CorexProd.App.Pages;

public sealed class HomePageV2 : HomePage
{
    public HomePageV2()
    {
        Grid? overlay = this.FindByName<Grid>("MenuDrawerOverlay");
        if (overlay is null)
            return;

        overlay.StyleId = "MenuDrawerOverlay";
        overlay.ColumnDefinitions.Clear();
        overlay.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(86, GridUnitType.Star) });
        overlay.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(14, GridUnitType.Star) });

        View? closeArea = overlay.Children.OfType<View>()
            .FirstOrDefault(view => Grid.GetColumn(view) == 1);

        overlay.Children.Clear();

        Border panel = new()
        {
            BackgroundColor = Color.FromArgb("#061F33"),
            StrokeThickness = 0,
            Content = new SidebarMenuView()
        };

        Grid.SetColumn(panel, 0);
        overlay.Children.Add(panel);

        if (closeArea is not null)
        {
            Grid.SetColumn(closeArea, 1);
            overlay.Children.Add(closeArea);
        }
    }
}
