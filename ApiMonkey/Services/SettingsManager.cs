using ApiMonkey.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;

namespace ApiMonkey.Services
{
    internal class SettingsManager
    {
        private const int MAX_COLLECTIONS = 1_000_000_000;

        private static SettingsManager? _instance;
        public static SettingsManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new SettingsManager();

                return _instance;
            }
        }

        public static SettingsData Settings => Instance.settings;

        private SettingsData settings;
        private readonly JsonSerializerOptions _saveJsonOptions = new()
        {
            WriteIndented = true
        };

        private SettingsManager()
        {
            LoadSettingsFromDisk(GetSettingsPath());
        }

        public static void Save()
        {
            Instance.SaveSettingsToDisk(GetSettingsPath());
        }

        private void LoadSettingsFromDisk(string path)
        {
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                settings = JsonSerializer.Deserialize<SettingsData>(json);
            }

            if (settings != null)
                return;

            settings = new SettingsData([
                GetCreateSettingsDirectory()
            ]);

            SaveSettingsToDisk(path);
        }

        internal static string GetDefaultRequestsPath()
        {
            return Path.Combine(GetCreateSettingsDirectory(), "requests");
        }

        private void SaveSettingsToDisk(string path)
        {
            var json = JsonSerializer.Serialize(settings, _saveJsonOptions);
            File.WriteAllText(path, json);
        }

        internal static string GetNewCollectionPath()
        {
            var settingsDir = GetCreateSettingsDirectory();
            var collectionsDir = Path.Combine(settingsDir, "collections");
            Directory.CreateDirectory(collectionsDir);

            // Find the first available collection name (collection-#)
            for (int i = 1; i < MAX_COLLECTIONS; i++)
            {
                var collectionPath = Path.Combine(collectionsDir, $"collection-{i}");

                if (!Directory.Exists(collectionPath))
                    return collectionPath;
            }

            return collectionsDir;
        }

        internal static string GetSettingsPath()
        {
            var settingsDir = GetCreateSettingsDirectory();
            return Path.Combine(settingsDir, "settings.json");
        }

        private static string GetCreateSettingsDirectory()
        {
            // We get the real path, so if we ever need to open the folder in explorer, it will show the true folder (instead of the virtualized one that Windows creates for the app)
            var realRoaming = Windows.Storage.ApplicationData.Current.RoamingFolder.Path;
            var settingsDir = Path.Combine(realRoaming, "ApiMonkey");
            Directory.CreateDirectory(settingsDir);

            return settingsDir;
        }
    }
}
