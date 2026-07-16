using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace CorexProd.WPF.Helpers
{
    public static class AppUpdateService
    {
        private const string UpdateFileName = "CorexProd.WPF.exe";
        private const string UpdateOkFileName = "CorexProd.update-ok";
        private const string DefaultUpdateSourcePath = @"\\192.168.10.200\compartido\SISTEMA ERP\APLICACION\UltimaVersion";

        public static async Task CheckForUpdatesAsync(Window owner)
        {
            ShowUpdateCompletedMessage(owner);

            string updateDirectory = ConfigurationManager.AppSettings["UpdateSourcePath"] ?? DefaultUpdateSourcePath;
            string remoteExe = Path.Combine(updateDirectory, UpdateFileName);
            string localExe = GetLocalExePath();

            if (!File.Exists(remoteExe) || !File.Exists(localExe))
            {
                return;
            }

            Version? localVersion = GetFileVersion(localExe);
            Version? remoteVersion = GetFileVersion(remoteExe);

            if (localVersion == null || remoteVersion == null || remoteVersion <= localVersion)
            {
                return;
            }

            MessageBoxResult answer = MessageBox.Show(
                owner,
                $"Hay una nueva version disponible: v{remoteVersion}. ¿Deseas actualizar ahora?",
                "Actualizacion disponible",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (answer != MessageBoxResult.Yes)
            {
                return;
            }

            Window updatingWindow = ShowUpdatingWindow(owner);
            await Task.Delay(700);

            StartUpdater(remoteExe, localExe, remoteVersion.ToString());
            updatingWindow.Close();
            Application.Current.Shutdown();
        }

        private static void ShowUpdateCompletedMessage(Window owner)
        {
            string updateOkFile = GetUpdateOkFilePath();
            if (!File.Exists(updateOkFile))
            {
                return;
            }

            string version = File.ReadAllText(updateOkFile).Trim();
            TryDelete(updateOkFile);

            if (string.IsNullOrWhiteSpace(version))
            {
                version = AppVersionHelper.Version;
            }

            MessageBox.Show(
                owner,
                $"Sistema actualizado correctamente. Ya puede usar CorexProd v.{version}",
                "Sistema actualizado",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private static Window ShowUpdatingWindow(Window owner)
        {
            Window window = new()
            {
                Title = "Actualizando",
                Owner = owner,
                Width = 360,
                Height = 130,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.ToolWindow,
                Content = new TextBlock
                {
                    Text = "Actualizando CorexProd...",
                    FontSize = 18,
                    FontWeight = FontWeights.SemiBold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Center
                }
            };

            window.Show();
            return window;
        }

        private static void StartUpdater(string sourceExe, string destinationExe, string newVersion)
        {
            string scriptPath = Path.Combine(Path.GetTempPath(), $"CorexProdUpdater_{Guid.NewGuid():N}.cmd");
            string flagFile = GetUpdateOkFilePath();

            File.WriteAllText(scriptPath, $"""
                @echo off
                setlocal
                set "SRC={sourceExe}"
                set "DST={destinationExe}"
                set "FLAG={flagFile}"
                timeout /t 2 /nobreak >nul
                copy /Y "%SRC%" "%DST%" >nul
                if errorlevel 1 exit /b 1
                echo {newVersion}>"%FLAG%"
                start "" "%DST%"
                del "%~f0"
                """);

            ProcessStartInfo startInfo = new()
            {
                FileName = "cmd.exe",
                Arguments = $"/c \"{scriptPath}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                WindowStyle = ProcessWindowStyle.Hidden
            };

            Process.Start(startInfo);
        }

        private static Version? GetFileVersion(string filePath)
        {
            string? version = FileVersionInfo.GetVersionInfo(filePath).ProductVersion
                ?? FileVersionInfo.GetVersionInfo(filePath).FileVersion;

            if (string.IsNullOrWhiteSpace(version))
            {
                return null;
            }

            version = version.Split('+')[0].TrimStart('v', 'V');
            return Version.TryParse(version, out Version? parsedVersion) ? parsedVersion : null;
        }

        private static string GetLocalExePath() =>
            Path.Combine(AppContext.BaseDirectory, UpdateFileName);

        private static string GetUpdateOkFilePath() =>
            Path.Combine(AppContext.BaseDirectory, UpdateOkFileName);

        private static void TryDelete(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch
            {
                // Si no se puede borrar, no bloqueamos el inicio del sistema.
            }
        }
    }
}
