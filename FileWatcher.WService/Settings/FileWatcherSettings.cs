using System.Collections.Generic;
using System.IO;
using System.Linq;
using FileWatcher.Models;
using Newtonsoft.Json;

namespace FileWatcher.WService.Settings
{
    /// <summary>
    /// FileWatcherSettings Class
    /// </summary>
    public class FileWatcherSettings
    {
        /// <summary>
        /// Settings Path
        /// </summary>
        public string SettingsPath => "C:\\Users\\3601346\\Source\\Repos\\GuillermoLealB\\FileWatcher-Base-Azure\\FileWatcher.WService\\Settings\\";
        public IEnumerable<FolderWatcher> Folders { get; }  
        public SupportSettings Support { get; }
        public List<string> Plugins { get; } = new List<string>();


        /// <summary>
        /// Process the Initial Settings 
        /// </summary>
        public FileWatcherSettings()
        {
            var fileJson =
                File.ReadAllText(SettingsPath + "FileWatcherSettings.json");
            var configuration = JsonConvert.DeserializeObject<dynamic>(fileJson);

            if (configuration.Folders != null)
                Folders = JsonConvert.DeserializeObject<List<FolderWatcher>>(configuration.Folders.ToString());

            if (configuration.Support != null)
                Support = JsonConvert.DeserializeObject<SupportSettings>(configuration.Support.ToString());

            if(configuration.Plugins != null)
                Plugins = JsonConvert.DeserializeObject<List<string>>(configuration.Plugins.ToString());

            // Plugins = Directory.GetFiles(SettingsPath + "Plugins\\", "*.dll")
            //     .ToList();
        }
    }
}