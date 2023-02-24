using System.Collections.Generic;
using System.IO;
using FileWatcher.Models;
using Newtonsoft.Json;

namespace FileWatcher.Service.Settings
{
    public class FileWatcherSettings
    {
        private readonly dynamic _configuration;

        public IEnumerable<FolderWatcher> Folders => GetFolders();

        private IEnumerable<FolderWatcher> GetFolders()
        {
            return JsonConvert.DeserializeObject<List<FolderWatcher>>(_configuration.Folders.ToString());
        }

        public FileWatcherSettings()
        {
            //var fileJson = File.ReadAllText("C:\\Users\\m3m0l\\Documents\\Work\\Application\\FileWatcher\\FileWatcher\\FileWatcher.Service\\Settings\\FileWatcherSettings.json");
            var fileJson = File.ReadAllText("C:\\Users\\3601346\\Source\\Repos\\GuillermoLealB\\FileWatcher-Base-Azure\\FileWatcher.Service\\Settings\\FileWatcherSettings.json");
            _configuration = JsonConvert.DeserializeObject<dynamic>(fileJson);
        }
    }
}
