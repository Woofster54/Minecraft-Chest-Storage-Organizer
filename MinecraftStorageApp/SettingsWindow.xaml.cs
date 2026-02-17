using DocumentFormat.OpenXml.Wordprocessing;
using MinecraftStorage.Core.Models;
using MinecraftStorage.Core.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;


namespace MinecraftStorageApp
{
    /// <summary>
    /// Interaction logic for SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        private AppSettings _settings;
        private SettingsService _settingsService;
        public SettingsWindow(AppSettings settings)
        {
            InitializeComponent();
            _settingsService = new SettingsService();
            _settings = settings;

            LoadSettings();
        }
        private void LoadSettings()
        {
            WorldNameBox.Text = _settings.WorldName;
            MinXBox.Text = _settings.MinX.ToString();
            MaxXBox.Text = _settings.MaxX.ToString();
            MinYBox.Text = _settings.MinY.ToString();
            MaxYBox.Text = _settings.MaxY.ToString();
            MinZBox.Text = _settings.MinZ.ToString();
            MaxZBox.Text = _settings.MaxZ.ToString();
            CredentialPathBox.Text = _settings.GoogleCredentialPath;
            SpreadsheetIdBox.Text = _settings.SpreadsheetId;
            SheetNameBox.Text = _settings.SheetName;
            PriorityItemsBox.Text = string.Join(Environment.NewLine, _settings.PriorityItems);
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            _settings.WorldName = WorldNameBox.Text;
            _settings.MinX = int.Parse(MinXBox.Text);
            _settings.MaxX = int.Parse(MaxXBox.Text);
            _settings.MinY = int.Parse(MinYBox.Text);
            _settings.MaxY = int.Parse(MaxYBox.Text);
            _settings.MinZ = int.Parse(MinZBox.Text);
            _settings.MaxZ = int.Parse(MaxZBox.Text);
            _settings.GoogleCredentialPath = CredentialPathBox.Text;
            _settings.SpreadsheetId = SpreadsheetIdBox.Text;
            _settings.SheetName = SheetNameBox.Text;

            _settings.PriorityItems = PriorityItemsBox.Text
    .Split(Environment.NewLine)
    .Select(x => x.Trim())
    .Where(x => !string.IsNullOrWhiteSpace(x))
    .ToList();

            _settingsService.Save(_settings);

            System.Windows.MessageBox.Show("Settings saved successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            this.Close();
        }
    }
}

