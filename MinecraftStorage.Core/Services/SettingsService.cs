using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using MinecraftStorage.Core.Models;
using System.Windows;

namespace MinecraftStorage.Core.Services
{
    public class SettingsService
    {

        private readonly string _settingsPath;

        public SettingsService()
        {
            string appFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "MinecraftStorageApp");

            Directory.CreateDirectory(appFolder);

            _settingsPath = Path.Combine(appFolder, "settings.json");
        }

        public AppSettings Load()
        {
            if (!File.Exists(_settingsPath))
            {
                var defaultSettings = new AppSettings();

                Save(defaultSettings);
                return defaultSettings;
            }

            string json = File.ReadAllText(_settingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json)
                   ?? new AppSettings();
        }

        public void Save(AppSettings settings)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };


            string json = JsonSerializer.Serialize(settings, options);
            File.WriteAllText(_settingsPath, json);
            
            
        }
    }
}
