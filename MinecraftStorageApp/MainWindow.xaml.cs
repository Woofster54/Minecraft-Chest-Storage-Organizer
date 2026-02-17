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
using System.IO;


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
        private AppSettings _settings;
        private SettingsService _settingsService;
        private Dictionary<string, int> _itemTotals = new();
        private Dictionary<string, string> _displayToInternalMap = new();
        private Dictionary<string, string> _lowDisplayToInternal = new();
        private Dictionary<string, int> _previousTotals = new();
        private string _snapshotPath = System.IO.Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MinecraftStorageApp",
            "snapshot.json");
        public MainWindow()
        {
            InitializeComponent();

            EnableStartup();
            
            _settingsService = new SettingsService();
            _settings = _settingsService.Load();
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
        private void LoadSnapshot()
        {
            if (!File.Exists(_snapshotPath))
            {
                _previousTotals = new Dictionary<string, int>();
                return;
            }

            string json = File.ReadAllText(_snapshotPath);
            _previousTotals = System.Text.Json.JsonSerializer
                .Deserialize<Dictionary<string, int>>(json)
                ?? new Dictionary<string, int>();
        }
        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            var settingsWindow = new SettingsWindow(_settings);
            settingsWindow.Owner = this;
            settingsWindow.ShowDialog();

            // Reload settings after closing
            _settings = _settingsService.Load();
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
            ChestNumber.Text = $"Chests found: {_currentRecords.Count}";
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
        private string FormatItemName(string internalName)
        {
            string name = internalName;

            if (name.Contains(":"))
                name = name.Split(':')[1];

            name = name.Replace("_", " ");

            return System.Globalization.CultureInfo
                .CurrentCulture
                .TextInfo
                .ToTitleCase(name);
        }
        private void SearchBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string typedText = SearchBox.Text;

            if (_displayToInternalMap.TryGetValue(typedText, out string internalName))
            {
                if (_itemTotals.TryGetValue(internalName, out int count))
                {
                    SearchResultText.Text = $"Total: {count}";
                }
                else
                {
                    SearchResultText.Text = "Total: 0";
                }
            }
            else
            {
                SearchResultText.Text = "Total: 0";
            }
        }


        private async Task AutoScan()
        {
            LoadSnapshot();
            StatusText.Text = "Scanning storage...";
         
            if (_isScanning) return;

            _isScanning = true;

            _processCheckTimer.Stop();   // 👈 stop checking during scan

            ScanButton.IsEnabled = false;
            ScanProgressBar.Value = 0;

            try
            {
                var scanner = new StorageScanner();

                string regionPath = System.IO.Path.Combine(
       Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
       ".minecraft",
       "saves",
       _settings.WorldName,
       "region");
                

                var progress = new Progress<int>(value =>
                {
                    ScanProgressBar.Value = value;
                });

                _currentRecords = await Task.Run(() =>
                 scanner.ScanBase(
    regionPath,
    _settings.MinX, _settings.MaxX,
    _settings.MinY, _settings.MaxY,
    _settings.MinZ, _settings.MaxZ,
    progress
));
                

                var exporter = new ExcelExportService();
                exporter.Export(@"C:\Users\mrste\Desktop\BaseInventory.xlsx", _currentRecords);

                var sheetService = new GoogleSheetsService(
    _settings.GoogleCredentialPath,
    _settings.SpreadsheetId
);

                
                _itemTotals = _currentRecords
      .GroupBy(r => r.ItemName)
      .ToDictionary(g => g.Key, g => g.Sum(x => x.Quantity));
                var topFive = _itemTotals
    .OrderByDescending(x => x.Value)
    .Take(5)
    .ToList();

                var changes = new List<string>();

                foreach (var kvp in _itemTotals)
                {
                    int previous = 0;
                    _previousTotals.TryGetValue(kvp.Key, out previous);

                    int diff = kvp.Value - previous;

                    if (diff != 0)
                    {
                        string sign = diff > 0 ? "+" : "";
                        changes.Add($"{sign}{diff} {FormatItemName(kvp.Key)}");
                    }
                }

                // Detect removed items
                foreach (var kvp in _previousTotals)
                {
                    if (!_itemTotals.ContainsKey(kvp.Key))
                    {
                        changes.Add($"-{kvp.Value} {FormatItemName(kvp.Key)}");
                    }
                }

                Dispatcher.Invoke(() =>
                {
                    ChangesList.ItemsSource = changes;
                });


                Dispatcher.Invoke(() =>
                {
                    TopFiveList.ItemsSource = topFive
                        .Select(x => $"{FormatItemName(x.Key)} – {x.Value}")
                        .ToList();
                });

                _displayToInternalMap.Clear();

                var displayNames = new List<string>();

                foreach (var internalName in _itemTotals.Keys)
                {
                    string displayName = FormatItemName(internalName);

                    _displayToInternalMap[displayName] = internalName;
                    displayNames.Add(displayName);
                }

                Dispatcher.Invoke(() =>
                {
                    SearchBox.ItemsSource = displayNames.OrderBy(x => x).ToList();
                });
                var lowItems = new List<string>();
                _lowDisplayToInternal.Clear();

                foreach (var priority in _settings.PriorityItems)
                {
                    string trimmedPriority = priority.Trim().ToLower();

                    var match = _itemTotals
                        .FirstOrDefault(kvp =>
                            kvp.Key.ToLower().EndsWith(trimmedPriority));

                    int count = match.Equals(default(KeyValuePair<string, int>))
                        ? 0
                        : match.Value;

                    if (count < 64)
                    {
                        string internalKey = match.Key ?? $"minecraft:{trimmedPriority}";
                        string display = $"{FormatItemName(internalKey)} – {count}";

                        _lowDisplayToInternal[display] = internalKey;
                        lowItems.Add(display);
                    }
                }

                Dispatcher.Invoke(() =>
                {
                    LowPriorityList.ItemsSource = lowItems;
                });
                var json = System.Text.Json.JsonSerializer.Serialize(_itemTotals, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });
                await Task.Run(() =>
                {
                    sheetService.UpdateSheet(_currentRecords, _itemTotals, lowItems, changes);
                });

                File.WriteAllText(_snapshotPath, json);
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
        private void LowPriorityList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LowPriorityList.SelectedItem == null)
                return;

            string display = LowPriorityList.SelectedItem.ToString();

            if (!_lowDisplayToInternal.TryGetValue(display, out string internalName))
                return;

            var locations = _currentRecords
                .Where(r => r.ItemName == internalName)
                .Select(r => $"({r.ChestX}, {r.ChestY}, {r.ChestZ}) – {r.Quantity}")
                .ToList();

            if (locations.Count == 0)
            {
                System.Windows.MessageBox.Show("No locations found.");
                return;
            }

            string message = string.Join(Environment.NewLine, locations);

            System.Windows.MessageBox.Show(
                message,
                $"{FormatItemName(internalName)} Locations");
        }
    }

}