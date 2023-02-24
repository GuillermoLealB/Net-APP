using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using FileWatcher.Models;
using FileWatcher.Notification;
using FileWatcher.Service.Settings;
using JabilCore.Utilities.Model;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using static JabilCore.Utilities.IO.File;

namespace FileWatcher.Service
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private FileWatcherSettings _watcherSettings;

        public Worker(ILogger<Worker> logger)
        {
            _watcherSettings = new FileWatcherSettings();
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            foreach (var folderWatcher in _watcherSettings.Folders)
            {
                try
                {
                    _logger.LogInformation($"Worker [{{folderId}}] running at: {{0}}", folderWatcher.Id, DateTimeOffset.Now);
                    var folderDataOrigin = folderWatcher.Origin.Credentials != null
                        ? new SharedFolderData
                        {
                            DirectoryBase = folderWatcher.Origin.Path,
                            Domain = folderWatcher.Origin.Credentials.Domain,
                            User = folderWatcher.Origin.Credentials.User,
                            Password = folderWatcher.Origin.Credentials.Password
                        }
                        : new FolderData
                        {
                            DirectoryBase = folderWatcher.Origin.Path,
                        };

                    //if (!ValidateConnection(folderDataOrigin))
                    //{
                    //    var body = $"FileWatcher.Service<Worker>[{folderWatcher.Id}] The connection to the origin folder failed";
                    //    EmailNotification.Send(
                    //        @from: "pedro_luna@jabil.com",
                    //        to: folderWatcher.Event.DistributionList,
                    //        subject: "FileWatcherService",
                    //        body: body);
                    //    return;
                    //}

                    folderWatcher.Watcher = new FileSystemWatcher
                    {
                        Path = folderWatcher.Origin.Path,
                        EnableRaisingEvents = true,
                        IncludeSubdirectories = true,
                        //Filter = string.IsNullOrEmpty(folderWatcher.Origin.Filter) ? "*.*" : folderWatcher.Origin.Filter
                    };

                    var eventInfo = folderWatcher.Watcher.GetType().GetEvent(folderWatcher.Event.EventType.ToString());
                    //if (eventInfo != null) eventInfo.AddEventHandler(folderWatcher.Watcher, new FileSystemEventHandler(OnCreated));
                    folderWatcher.Watcher.Created += OnChanged;
                    //folderWatcher.Watcher.Deleted += OnChanged;
                    //folderWatcher.Watcher.Changed += OnChanged;
                    //folderWatcher.Watcher.Renamed += OnChanged;

                    //folderWatcher.Watcher.EnableRaisingEvents = true;
                    //folderWatcher.Watcher.IncludeSubdirectories= true;/// incluimos Subdirectorios 
                }
                catch (Exception e)
                {
                    var body = $"FileWatcher.Service<Worker>[{folderWatcher.Id}] {e.Message}";
                    EmailNotification.Send(
                        @from: "pedro_luna@jabil.com",
                        to: folderWatcher.Event.DistributionList,
                        subject: "FileWatcherService",
                        body: body);
                }
            }
        }

        //private static void OnCreated(object source, FileSystemEventArgs eventArgs)
        //{
        //    var msg = $"{eventArgs.ChangeType} - {eventArgs.FullPath}{System.Environment.NewLine}";
        //    File.AppendAllText(@"D:\\FileInfo\\log.txt", msg);
        //}

        private static void OnChanged(object source, FileSystemEventArgs eventArgs)
        {

            var msg = $"{eventArgs.ChangeType} - {eventArgs.FullPath}{System.Environment.NewLine}";
            
            string text = File.ReadLines(eventArgs.FullPath).Skip(8).Take(1).First();
            if (text == "TP")
            {
                Console.WriteLine("si pasa ");
                String serial = File.ReadLines(eventArgs.FullPath).Take(1).First();
                Console.WriteLine("Su numero de serie es: " + serial);
            }
            else
            { Console.WriteLine("NO pasa "); }

            Console.WriteLine(text);

            
        }

        //private static void OnDeleted(object source, FileSystemEventArgs eventArgs)
        //{
        //    var msg = $"{eventArgs.ChangeType} - {eventArgs.FullPath}{System.Environment.NewLine}";
        //    File.AppendAllText(@"D:\\FileInfo\\log.txt", msg);

        //}
    }
}
