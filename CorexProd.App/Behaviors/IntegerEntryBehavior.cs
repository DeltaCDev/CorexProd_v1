using System.Linq;

namespace CorexProd.App.Behaviors;

public sealed class IntegerEntryBehavior : Behavior<Entry>
{
    private bool _actualizando;

    protected override void OnAttachedTo(Entry bindable)
    {
        base.OnAttachedTo(bindable);
        bindable.TextChanged += OnTextChanged;
    }

    protected override void OnDetachingFrom(Entry bindable)
    {
        bindable.TextChanged -= OnTextChanged;
        base.OnDetachingFrom(bindable);
    }

    private void OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        if (_actualizando || sender is not Entry entry) return;
        string limpio = new((e.NewTextValue ?? string.Empty).Where(char.IsDigit).ToArray());
        if (limpio == e.NewTextValue) return;
        _actualizando = true;
        entry.Text = limpio;
        entry.CursorPosition = limpio.Length;
        _actualizando = false;
    }
}
