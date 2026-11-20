using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Data;

namespace CorexProd.WPF.Helpers;

/// <summary>
/// Restringe campos de cantidades de prendas a números enteros no negativos.
/// Los valores internos pueden continuar siendo decimal para no alterar la base de datos.
/// </summary>
public static class IntegerInputBehavior
{
    private static bool _autoInitialized;

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(IntegerInputBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);
    public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

    public static void Initialize()
    {
        if (_autoInitialized) return;
        _autoInitialized = true;
        EventManager.RegisterClassHandler(typeof(TextBox), FrameworkElement.LoadedEvent,
            new RoutedEventHandler(OnTextBoxLoaded), true);
    }

    private static void OnTextBoxLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBox textBox || GetIsEnabled(textBox) || !EsCampoCantidad(textBox)) return;
        SetIsEnabled(textBox, true);
    }

    private static bool EsCampoCantidad(TextBox textBox)
    {
        string nombre = textBox.Name ?? string.Empty;
        string ruta = BindingOperations.GetBinding(textBox, TextBox.TextProperty)?.Path?.Path ?? string.Empty;
        string texto = $"{nombre} {ruta}".ToLowerInvariant();
        if (texto.Contains("precio") || texto.Contains("importe") || texto.Contains("descuento") ||
            texto.Contains("acuenta") || texto.Contains("saldo") || texto.Contains("tarifa")) return false;
        return texto.Contains("cantidad") || texto.Contains("stock") || texto.Contains("despach") ||
               texto.Contains("reserv") || texto.Contains("merma") ||
               texto.Contains("recibid") || texto.Contains("enviad") || texto.Contains("retorn");
    }

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBox textBox) return;

        if ((bool)e.NewValue)
        {
            textBox.PreviewTextInput += PreviewTextInput;
            DataObject.AddPastingHandler(textBox, OnPaste);
        }
        else
        {
            textBox.PreviewTextInput -= PreviewTextInput;
            DataObject.RemovePastingHandler(textBox, OnPaste);
        }
    }

    private static void PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (sender is TextBox textBox)
            e.Handled = !EsEntero(textBox, e.Text);
    }

    private static void OnPaste(object sender, DataObjectPastingEventArgs e)
    {
        if (sender is not TextBox textBox || !e.DataObject.GetDataPresent(DataFormats.Text))
        {
            e.CancelCommand();
            return;
        }

        string textoPegado = e.DataObject.GetData(DataFormats.Text)?.ToString() ?? string.Empty;
        if (!EsEntero(textBox, textoPegado)) e.CancelCommand();
    }

    private static bool EsEntero(TextBox textBox, string textoNuevo)
    {
        string texto = textBox.Text.Remove(textBox.SelectionStart, textBox.SelectionLength)
            .Insert(textBox.SelectionStart, textoNuevo);
        return Regex.IsMatch(texto, "^\\d*$");
    }
}
