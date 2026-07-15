using System.Reflection;

namespace CorexProd.WPF.Helpers
{
    public static class AppVersionHelper
    {
        public static string Version
        {
            get
            {
                Assembly assembly = Assembly.GetEntryAssembly() ?? typeof(AppVersionHelper).Assembly;

                string? informationalVersion = assembly
                    .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                    ?.InformationalVersion;

                if (!string.IsNullOrWhiteSpace(informationalVersion))
                {
                    return informationalVersion.Split('+')[0];
                }

                return assembly.GetName().Version?.ToString() ?? "1.0.0";
            }
        }

        public static string Title => $"CorexProd v{Version}";
    }
}
