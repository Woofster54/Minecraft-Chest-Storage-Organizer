using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MinecraftStorage.Core.Services;
using MinecraftStorage.Core.Models;
using System.Diagnostics;
using System.Timers;
using System.Windows.Media;
using Microsoft.Win32;


namespace MinecraftStorageApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private List<ChestItemRecord> _currentRecords = new();
        private System.Timers.Timer _processCheckTimer;
        private bool _minecraftWasRunning = false;
        private bool _isScanning = false;
        private System.Windows.Forms.NotifyIcon _trayIcon;

        public MainWindow()
        {
            InitializeComponent();

            EnableStartup();

            // Create tray icon
            _trayIcon = new System.Windows.Forms.NotifyIcon();
            _trayIcon.Icon = System.Drawing.SystemIcons.Application;
            _trayIcon.Visible = true;
            _trayIcon.Text = "Minecraft Storage Sync";

            // 👇 Add startup notification here
            Loaded += (s, e) =>
            {
                Task.Delay(2000).ContinueWith(_ =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        _trayIcon.ShowBalloonTip(
                            3000,
                            "Minecraft Storage",
                            "Background service started successfully.",
                            System.Windows.Forms.ToolTipIcon.Info);
                    });
                });
            };

            // 👇 STEP 4 GOES RIGHT HERE
            var contextMenu = new System.Windows.Forms.ContextMenuStrip();

            contextMenu.Items.Add("Open", null, (s, e) =>
            {
                this.Show();
                this.WindowState = WindowState.Normal;
                this.ShowInTaskbar = true;
            });

            contextMenu.Items.Add("Exit", null, (s, e) =>
            {
                _trayIcon.Visible = false;
                System.Windows.Application.Current.Shutdown();
            });

            _trayIcon.ContextMenuStrip = contextMenu;

            // Start Minecraft process monitor
            _processCheckTimer = new System.Timers.Timer(3000);
            _processCheckTimer.Elapsed += CheckMinecraftProcess;
            _processCheckTimer.Start();
        }
        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);

            this.WindowState = WindowState.Minimized;
            this.ShowInTaskbar = false;
            this.Hide();
        }

        //protected override void OnSourceInitialized(EventArgs e)
        //{
        //    base.OnSourceInitialized(e);

        //    this.Hide(); // hide window on startup
        //}
        private void EnableStartup()
        {
            string appName = "MinecraftStorageAutoSync";
            string exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;

            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Run", true))
            {
                key.SetValue(appName, exePath);
            }
        }
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }
        private async void ScanButton_Click(object sender, RoutedEventArgs e)
        {
           await AutoScan();
            StatusText.Text = $"Manual Scan Completed at {DateTime.Now:T}";
        }
        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            StatusText.Text = "Exporting to Excel...";
        }
        private void SyncButton_Click(object sender, RoutedEventArgs e)
        {
            StatusText.Text = "Syncing to Google Sheets...";
        }
        private void CheckMinecraftProcess(object? sender, ElapsedEventArgs e)
        {
            bool gameRunning = Process.GetProcesses()
       .Any(p =>
       {
           try
           {
               string name = p.ProcessName;

               return name.Equals("LunarClient", StringComparison.OrdinalIgnoreCase) ||
                      name.Equals("javaw", StringComparison.OrdinalIgnoreCase);
           }
           catch
           {
               return false;
           }
       });

            if (gameRunning)
            {
                _minecraftWasRunning = true;
            }
            else
            {
                if (_minecraftWasRunning)
                {
                    _minecraftWasRunning = false;

                    Dispatcher.Invoke(async () =>
                    {
                        StatusText.Text = "Game closed. Auto scanning...";
                        await AutoScan();
                    });
                }
            }
        }


        private async Task AutoScan()
        {
            StatusText.Text = "Scanning storage...";
            if (_isScanning) return;

            _isScanning = true;

            _processCheckTimer.Stop();   // 👈 stop checking during scan

            ScanButton.IsEnabled = false;
            ScanProgressBar.Value = 0;

            try
            {
                var scanner = new StorageScanner();

                string regionPath = @"C:\Users\mrste\AppData\Roaming\.minecraft\saves\F it we ball\region\";

                var progress = new Progress<int>(value =>
                {
                    ScanProgressBar.Value = value;
                });

                _currentRecords = await Task.Run(() =>
                    scanner.ScanBase(
                        regionPath,
                        -316, -228,
                        67, 133,
                        -54, 70,
                        progress
                    )
                );

                var exporter = new ExcelExportService();
                exporter.Export(@"C:\Users\mrste\Desktop\BaseInventory.xlsx", _currentRecords);

                var sheetService = new GoogleSheetsService(
                    @"C:\Users\mrste\Desktop\Minecraft Storage\minecraft-storage-sync-c6b992f100b8.json", "1b3AQTYK_wOcfOqxZO6L0gHPewjS2ezobUL3fYRCm3gk"
                );

                await Task.Run(() =>
                {
                    sheetService.UpdateSheet(_currentRecords);
                });

                StatusText.Text = $"Auto update complete at {DateTime.Now:T}";
                _trayIcon.ShowBalloonTip(
    3000,
    "Minecraft Storage Updated",
    "Inventory synced successfully.",
    System.Windows.Forms.ToolTipIcon.Info);
            }
            finally
            {
                _isScanning = false;
                ScanButton.IsEnabled = true;

                _processCheckTimer.Start();   // 👈 resume monitoring
            }

        }
    }

}