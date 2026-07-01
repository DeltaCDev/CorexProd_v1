namespace CorexProd.App.Pages;

public partial class SplashPage : ContentPage
{
    private bool _started;

    public SplashPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_started)
            return;

        _started = true;
        await AnimateLogoAsync();
        await Shell.Current.GoToAsync("//login");
    }

    private async Task AnimateLogoAsync()
    {
        Task pulseOne = PulseOne.ScaleToAsync(1.12, 620, Easing.CubicOut);
        Task pulseTwo = PulseTwo.ScaleToAsync(0.92, 760, Easing.CubicOut);
        Task logoFade = LogoMark.FadeToAsync(1, 360, Easing.CubicOut);
        Task logoScale = LogoMark.ScaleToAsync(1, 720, Easing.SpringOut);
        Task logoRotate = LogoMark.RotateToAsync(0, 680, Easing.CubicOut);

        await Task.WhenAll(pulseOne, pulseTwo, logoFade, logoScale, logoRotate);

        await Task.WhenAll(
            AnimateBarAsync(BlueBar, 0, 220),
            AnimateBarAsync(GreenBar, 90, 240),
            AnimateBarAsync(OrangeBar, 180, 260));

        await LogoStage.ScaleToAsync(1.04, 160, Easing.CubicOut);
        await LogoStage.ScaleToAsync(1, 180, Easing.CubicIn);
        await Task.Delay(280);
    }

    private static async Task AnimateBarAsync(VisualElement bar, int delay, uint length)
    {
        if (delay > 0)
            await Task.Delay(delay);

        await bar.ScaleXToAsync(1, length, Easing.CubicOut);
    }
}
