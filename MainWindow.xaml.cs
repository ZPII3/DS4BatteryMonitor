using HidSharp;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;
using System.ComponentModel;
using System.IO;
using System.Text.Json;

namespace DS4BatteryMonitor
{
    public partial class MainWindow : Window
    {
        // Native methods for window dragging and icon management
        [DllImport("user32.dll")] public static extern bool ReleaseCapture();
        [DllImport("user32.dll")] public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
        [DllImport("user32.dll")] public static extern bool DestroyIcon(IntPtr hIcon);

        private System.Windows.Forms.NotifyIcon _trayIcon;
        private DispatcherTimer _updateTimer;
        private IntPtr _currentIconHandle = IntPtr.Zero;

        // Sony DualShock 4 Identifiers
        private const int VendorId = 0x054C;
        private const int ProductId = 0x09CC;

        private int lastLevel = -1;
        private bool isCharging = false;

        public MainWindow()
        {
            InitializeComponent();
            LoadSettings();

            _trayIcon = new System.Windows.Forms.NotifyIcon();

            // Initial UI setup on startup
            UpdateUI(-1, false); // Shows "?" and sets bar width to 0
            SetupTrayIcon();

            // Setup polling timer (checks battery every 10 seconds)
            _updateTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
            _updateTimer.Tick += (s, e) => UpdateBatteryStatus();
            _updateTimer.Start();

            UpdateBatteryStatus();
        }

        public class AppSettings
        {
            public double WindowLeft { get; set; } = 100;
            public double WindowTop { get; set; } = 100;
            public bool IsVisible { get; set; } = false;
            public string CurrentLanguage { get; set; } = "Japanese";

            // Dictionary for localization data
            public System.Collections.Generic.Dictionary<string, System.Collections.Generic.Dictionary<string, string>> Languages { get; set; } = new()
            {
                ["Japanese"] = new()
                {
                    ["Show"] = "ウィンドウを表示",
                    ["Hide"] = "ウィンドウを非表示",
                    ["Exit"] = "終了",
                    ["TopMost"] = "常時手前表示",
                    ["Min"] = "最小化",
                    ["Lang"] = "言語 (Language)",
                    ["Disconnected"] = "未接続"
                },
                ["English"] = new()
                {
                    ["Show"] = "Show Window",
                    ["Hide"] = "Hide Window",
                    ["Exit"] = "Exit",
                    ["TopMost"] = "Always on Top",
                    ["Min"] = "Minimize",
                    ["Lang"] = "Language",
                    ["Disconnected"] = "Disconnected"
                }
            };
        }

        private string _settingsPath => GetSettingsPath();

        private static string GetSettingsPath()
        {
            using var process = System.Diagnostics.Process.GetCurrentProcess();
            var exePath = process.MainModule?.FileName;
            var directory = Path.GetDirectoryName(exePath) ?? AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(directory, "settings.json");
        }

        private AppSettings _config = new();

        private void LoadSettings()
        {
            if (File.Exists(_settingsPath))
            {
                try
                {
                    string json = File.ReadAllText(_settingsPath);
                    _config = JsonSerializer.Deserialize<AppSettings>(json) ?? new();
                    this.Left = _config.WindowLeft;
                    this.Top = _config.WindowTop;
                    if (_config.IsVisible) this.Show(); else this.Hide();
                }
                catch { _config = new(); }
            }
        }

        private void SaveSettings()
        {
            _config.WindowLeft = this.Left;
            _config.WindowTop = this.Top;
            _config.IsVisible = this.IsVisible;

            // Save JSON with formatting and Unicode support (for Japanese characters)
            var options = new JsonSerializerOptions
            {
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.Create(System.Text.Unicode.UnicodeRanges.All),
                WriteIndented = true
            };

            string json = JsonSerializer.Serialize(_config, options);
            File.WriteAllText(_settingsPath, json);
        }

        private void SetupTrayIcon()
        {
            var lang = _config.Languages[_config.CurrentLanguage];
            _trayIcon.Visible = true;
            UpdateTrayIcon(lastLevel, isCharging);

            var menu = new System.Windows.Forms.ContextMenuStrip();

            // Toggle window visibility with a checkbox in the tray menu
            var showItem = new System.Windows.Forms.ToolStripMenuItem(lang["Show"], null, (s, e) => {
                this.Dispatcher.Invoke(() => {
                    if (this.IsVisible) this.Hide();
                    else { this.Show(); this.WindowState = WindowState.Normal; }
                    SetupTrayIcon(); // Re-generate to update checkmark state
                });
            });
            showItem.Checked = this.IsVisible;
            menu.Items.Add(showItem);

            // Submenu for language selection
            var langMenu = new System.Windows.Forms.ToolStripMenuItem(lang["Lang"]);
            foreach (var lName in _config.Languages.Keys)
            {
                var item = new System.Windows.Forms.ToolStripMenuItem(lName, null, (s, e) => {
                    _config.CurrentLanguage = lName;
                    SetupTrayIcon();
                    SaveSettings();
                });
                item.Checked = (lName == _config.CurrentLanguage);
                langMenu.DropDownItems.Add(item);
            }
            menu.Items.Add(langMenu);

            var aboutItem = new System.Windows.Forms.ToolStripMenuItem("About", null, (s, e) => {
                var result = System.Windows.MessageBox.Show(
                    "DS4 Battery Monitor v1.0\n\nOpen GitHub repository in your browser?",
                    "About",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "https://github.com/ZPII3/DS4BatteryMonitor",
                        UseShellExecute = true
                    });
                }
            });
            menu.Items.Add(aboutItem);

            menu.Items.Add("-");
            menu.Items.Add(lang["Exit"], null, (s, e) => {
                SaveSettings();
                _trayIcon.Visible = false;
                System.Windows.Application.Current.Shutdown();
            });

            _trayIcon.ContextMenuStrip = menu;
        }

        private void UpdateBatteryStatus()
        {
            var device = DeviceList.Local.GetHidDevices(VendorId, ProductId).FirstOrDefault();
            if (device == null) { ResetStatus(); return; }

            try
            {
                using (var stream = device.Open())
                {
                    // DS4 Bluetooth Output Report (0x11) to request status
                    byte[] report = new byte[78];
                    report[0] = 0x11; report[1] = 0x80; report[2] = 0x00; report[3] = 0x01;

                    // Compute CRC32 for the report (required for DS4 BT communication)
                    byte[] crcData = new byte[75];
                    crcData[0] = 0xA2;
                    Array.Copy(report, 0, crcData, 1, 74);
                    uint crc = Crc32.Compute(crcData, 75);
                    Array.Copy(BitConverter.GetBytes(crc), 0, report, 74, 4);

                    stream.Write(report);
                    System.Threading.Thread.Sleep(150); // Small delay to allow device processing

                    byte[] inputReport = new byte[device.GetMaxInputReportLength()];
                    int bytesRead = stream.Read(inputReport);

                    if (bytesRead > 0 && inputReport[0] == 0x11)
                    {
                        // Byte 32 contains battery info (low 4 bits: level, bit 4: charging status)
                        int batteryByte = inputReport[32];
                        int level = Math.Min((batteryByte & 0x0F) * 10, 100);
                        bool charging = (batteryByte & 0x10) != 0;
                        UpdateUI(level, charging);
                    }
                    else { ResetStatus(); }
                }
            }
            catch { ResetStatus(); }
        }

        private void UpdateUI(int level, bool charging)
        {
            this.lastLevel = level;
            this.isCharging = charging;
            var lang = _config.Languages[_config.CurrentLanguage];

            // Update Battery Bar UI
            BatteryBar.Width = (this.Width - 2) * (Math.Max(0, level) / 100.0);
            BatteryBar.Fill = charging ? System.Windows.Media.Brushes.DodgerBlue :
                             (level <= 20 ? System.Windows.Media.Brushes.Red : System.Windows.Media.Brushes.LimeGreen);

            // Update Status Text and Tray Tooltip
            if (level == -1)
            {
                StatusText.Text = lang.ContainsKey("Disconnected") ? lang["Disconnected"] : "?";
                _trayIcon.Text = "DS4: " + (lang.ContainsKey("Disconnected") ? lang["Disconnected"] : "Disconnected");
            }
            else
            {
                StatusText.Text = charging ? $"⚡{level}%" : $"{level}%";
                _trayIcon.Text = $"DS4: {level}%";
            }

            // Redraw tray icon (displays "?" if disconnected)
            UpdateTrayIcon(level, charging);
        }

        private void UpdateTrayIcon(int level, bool charging)
        {
            using (var bmp = new System.Drawing.Bitmap(24, 24))
            using (var g = System.Drawing.Graphics.FromImage(bmp))
            {
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                g.Clear(System.Drawing.Color.Transparent);

                using (var blackPen = new System.Drawing.Pen(System.Drawing.Color.Black, 1))
                {
                    // Draw battery frame
                    g.DrawRectangle(blackPen, 0, 5, 20, 13); // Main body
                    g.FillRectangle(System.Drawing.Brushes.Black, 21, 9, 2, 5); // Terminal tip

                    if (level == -1)
                    {
                        // Draw "?" for disconnected state
                        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                        using (var font = new System.Drawing.Font("Arial", 12, System.Drawing.FontStyle.Bold))
                        {
                            g.DrawString("?", font, System.Drawing.Brushes.Gray, 3, 4);
                        }
                    }
                    else
                    {
                        // Draw filled battery level
                        int currentLevel = Math.Max(0, Math.Min(level, 100));
                        int fillWidth = (int)(18 * (currentLevel / 100.0));
                        var brush = charging ? System.Drawing.Brushes.DodgerBlue : (currentLevel <= 20 ? System.Drawing.Brushes.Red : System.Drawing.Brushes.LimeGreen);

                        if (fillWidth > 0)
                        {
                            g.FillRectangle(brush, 1, 6, fillWidth, 12);
                        }
                    }
                }
                // Update tray icon and manage memory handles
                IntPtr hIcon = bmp.GetHicon();

                // Allow null by using '?' to avoid CS8600 warning
                System.Drawing.Icon? oldIcon = _trayIcon.Icon;

                if (_currentIconHandle != IntPtr.Zero) DestroyIcon(_currentIconHandle);
                _currentIconHandle = hIcon;

                // Replace with the new icon handle
                _trayIcon.Icon = System.Drawing.Icon.FromHandle(hIcon);

                // Safely dispose the previous icon object to prevent memory leaks
                oldIcon?.Dispose();
            }
        }

        private void ResetStatus() => UpdateUI(-1, false);

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            // Allow window dragging from any part of the UI
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                ReleaseCapture();
                SendMessage(new System.Windows.Interop.WindowInteropHelper(this).Handle, 0xA1, 0x2, 0);
            }
        }

        private void MenuTopMost_Click(object sender, RoutedEventArgs e) => this.Topmost = !this.Topmost;

        private void MenuMinimize_Click(object sender, RoutedEventArgs e) => this.Hide();

        protected override void OnClosing(CancelEventArgs e)
        {
            _trayIcon.Visible = false; // Cleanup tray icon on close
            base.OnClosing(e);
        }

        /// <summary>
        /// Standard CRC32 implementation for DS4 communication compatibility.
        /// </summary>
        public static class Crc32
        {
            private static readonly uint[] Table;
            static Crc32()
            {
                Table = new uint[256];
                for (uint i = 0; i < 256; i++)
                {
                    uint r = i;
                    for (int j = 0; j < 8; j++)
                        r = (r & 1) != 0 ? (r >> 1) ^ 0xEDB88320 : r >> 1;
                    Table[i] = r;
                }
            }
            public static uint Compute(byte[] bytes, int length)
            {
                uint crc = 0xFFFFFFFF;
                for (int i = 0; i < length; i++)
                    crc = (crc >> 8) ^ Table[(crc ^ bytes[i]) & 0xFF];
                return crc ^ 0xFFFFFFFF;
            }
        }
    }
}