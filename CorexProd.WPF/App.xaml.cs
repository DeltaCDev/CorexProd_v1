using HandyControl.Tools;
using System.Globalization;
using System.Threading;
using System.Windows;

namespace CorexProd.WPF
{
    public partial class App : Application
    {
        public App()
        {
            CultureInfo cultura = new("es-PE");

            Thread.CurrentThread.CurrentCulture = cultura;
            Thread.CurrentThread.CurrentUICulture = cultura;

            ConfigHelper.Instance.SetLang("es");

            InitializeComponent();
        }
    }
}