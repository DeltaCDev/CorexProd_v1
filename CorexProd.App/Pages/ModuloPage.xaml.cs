namespace CorexProd.App.Pages;

[QueryProperty(nameof(Titulo), "titulo")]
public partial class ModuloPage : ContentPage
{
    private string _titulo = "Modulo";

    public ModuloPage()
    {
        InitializeComponent();
        BindingContext = this;
    }

    public string Titulo
    {
        get => _titulo;
        set
        {
            _titulo = Uri.UnescapeDataString(value ?? "Modulo");
            OnPropertyChanged();
        }
    }

    private async void OnVolverClicked(object? sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}
