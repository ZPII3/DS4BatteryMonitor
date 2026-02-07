using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Text.Json;

namespace DS4BatteryMonitor_WPF
{
    public partial class App : System.Windows.Application
    {
        // Keep static to prevent GC (Garbage Collection) until app exits
        private static Mutex? _appMutex;

        protected override void OnStartup(StartupEventArgs e)
        {
            // 1. Single Instance Check (Unique per user session)
            const string mutexName = "Global\\DS4BatteryMonitor_WPF_Unique_Mutex";
            _appMutex = new Mutex(true, mutexName, out bool createdNew);

            if (!createdNew)
            {
                // Show localized error message from settings.json
                ShowAlreadyRunningMessage();

                _appMutex.Dispose();
                Environment.Exit(0);
                return;
            }

            // 2. Settings File Integrity Check
            SanitizeSettings();

            // 3. Initialize Application
            this.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            base.OnStartup(e);

            // MainWindow.xaml.cs handles tray icon and visibility in its constructor/LoadSettings
            var mainWindow = new DS4BatteryMonitor.MainWindow();
        }

        private void ShowAlreadyRunningMessage()
        {
            string settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
            string message = "Application is already running."; // Default fallback
            string title = "Warning";

            if (File.Exists(settingsPath))
            {
                try
                {
                    string json = File.ReadAllText(settingsPath);
                    // Access AppSettings class inside MainWindow namespace
                    var config = JsonSerializer.Deserialize<DS4BatteryMonitor.MainWindow.AppSettings>(json);
                    if (config != null && config.Languages.ContainsKey(config.CurrentLanguage))
                    {
                        var lang = config.Languages[config.CurrentLanguage];
                        if (lang.ContainsKey("AlreadyRunning")) message = lang["AlreadyRunning"];
                        if (lang.ContainsKey("Warning")) title = lang["Warning"];
                    }
                }
                catch { /* Use default if file is corrupted */ }
            }
            System.Windows.MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Exclamation);
        }

        private void SanitizeSettings()
        {
            string settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
            if (File.Exists(settingsPath))
            {
                try
                {
                    string json = File.ReadAllText(settingsPath);
                    using var doc = JsonDocument.Parse(json);
                }
                catch
                {
                    // If JSON is broken, backup and delete to force reset in MainWindow
                    try { File.Copy(settingsPath, settingsPath + ".bak", true); } catch { }
                    File.Delete(settingsPath);
                }
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            if (_appMutex != null)
            {
                try { _appMutex.ReleaseMutex(); } catch { }
                _appMutex.Dispose();
            }
            base.OnExit(e);
        }
    }
}