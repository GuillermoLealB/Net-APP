using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.ServiceProcess;
using FileWatcher.Models;
using FileWatcher.Notification;
using FileWatcher.WService.Settings;
using Humanizer;
using JabilCore.Utilities.Enumeration;
using JabilCore.Utilities.Model;
using static JabilCore.Utilities.IO.File;

namespace FileWatcher.WService
{
    // TODO: Replicate to WorkerService from .NetCore
    /// <summary>
    /// Worker Service Execution
    /// </summary>
    public partial class WorkerService : ServiceBase
    {
        private readonly List<Assembly> _assemblies = new List<Assembly>();
        private FileWatcherSettings _config;
        private string _baseMessage;

        public void OnDebug()
        {
            _baseMessage = $"[FilerWatcher.WService<{Environment.MachineName}>]";
            OnStart(null);

        }

        /// <summary>
        /// Worker Service Construct 
        /// </summary>
        public WorkerService()
        {
            InitializeComponent();
            //_baseMessage = $"[FilerWatcher.WService<{Environment.MachineName}>]";
        }

        /// <summary>
        /// OnStop Method
        /// </summary>
        protected override void OnStop() 
        {
            try
            {
                SendErrorToSupport($"{_baseMessage} The FileWatcher Service was stopped.");
            }
            catch (Exception e)
            {
                EventLog.WriteEntry(
                    $"{_baseMessage} Email delivery failed: When the FileWatcher Service was stopped > {e.Message} > {e.StackTrace}",
                    EventLogEntryType.Error);
            }
        }

        private void SendErrorToSupport(string msg)
        {
            //EventLog.WriteEntry(msg, EventLogEntryType.Error);

            EmailNotification.Send(
                @from: "filewatcher@jabil.com",
                to: _config.Support.DistributionList,
                subject: "FileWatcherWService",
                body: msg);
        }

        /// <summary>
        /// OnStart Method
        /// </summary>
        /// <param name="args"></param>
        /// <exception cref="Exception"></exception>
        protected override void OnStart(string[] args)
        {
            try
            {
                _config = new FileWatcherSettings(); 
                
                //_config.Plugins.ForEach(p =>
                //{
                //    _assemblies.Add(Assembly.LoadFile(_config.SettingsPath + "Plugins\\" + p));
                //});

                foreach (var folder in _config.Folders)
                {
                    ConnectToOrigin(folder);

                    FilesStopped(folder);

                    folder.Watcher = new FileSystemWatcher
                    {
                        Path = folder.Origin.Path,
                        //Filter = string.IsNullOrEmpty(folder.Origin.Filter) ? "*.*" : folder.Origin.Filter,
                        EnableRaisingEvents = true,
                        IncludeSubdirectories=true,
                        InternalBufferSize = 1024 * 64,
                    };

                    folder.Watcher.Error += WatcherOnError;

                    // TODO: Expand options
                    //var eventInfo = folder.Watcher.GetType().GetEvent(folder.Event.EventType.ToString());
                    //if (eventInfo != null)
                    //    eventInfo.AddEventHandler(folder.Watcher, new FileSystemEventHandler(WatcherOnCreated));

                    //folder.Watcher.EnableRaisingEvents = true;
                    folder.Watcher.Created += OnChanged;
                    //folder.Watcher.Deleted += OnChanged;
                    //folder.Watcher.Changed += OnChanged;
                    //folder.Watcher.Renamed += OnChanged;
                    //EventLog.WriteEntry(
                    //    $"{folder.BaseMessage} FileWatcher was stared. EventType: {folder.Event.EventType} & ActionType: {folder.Event.ActionType}");

                    //if (folder.Watcher.EnableRaisingEvents)
                    //{

                    //}
                }
            }
            catch (Exception e)
            {
                SendErrorToSupport($"{_baseMessage} {e.Message} > {e.StackTrace}");
            }
        }

        void RestartProcess()
        {
            foreach (var folder in _config.Folders)
            {
                FilesStopped(folder);
            }
            
        }
        private static void OnChanged(object source, FileSystemEventArgs eventArgs)
        {
            var msg = $"{eventArgs.ChangeType} - {eventArgs.FullPath}{System.Environment.NewLine}";
            File.AppendAllText(@"D:\Documents\Personal\Info\\log.txt", msg);
        }
        private static void ConnectToOrigin(FolderWatcher folder)
        {
            var folderDataOrigin = folder.Origin.Credentials != null
                ? new SharedFolderData
                {
                    DirectoryBase = folder.Origin.Path,
                    Domain = folder.Origin.Credentials.Domain,
                    User = folder.Origin.Credentials.User,
                    Password = folder.Origin.Credentials.Password
                }
                : new FolderData
                {
                    DirectoryBase = folder.Origin.Path,
                };

            if (!CheckConnection(folderDataOrigin))
                throw new Exception("The connection to the origin folder failed.");
        }

        //private void WatcherOnCreated(object sender, FileSystemEventArgs eventArgs)
        //{
        //    var folder = _config.Folders.FirstOrDefault(f => f.Watcher == (FileSystemWatcher) sender);

        //    if (folder == null)
        //    {
        //        SendErrorToSupport(
        //            $"{_baseMessage} Creation of a file {eventArgs.Name} was detected and could not identify its parent driver");
        //        return;
        //    }

        //    System.Threading.Thread.Sleep(1000 * folder.Event.StandByAction);

        //    try
        //    {
        //        // todo: change description by trigger
        //        EventLog.WriteEntry(
        //            $"{folder.BaseMessage} File {eventArgs.Name} was {folder.Event.EventType}.");
        //        ExecuteAction(folder, eventArgs.Name);
        //    }
        //    catch (Exception e)
        //    {   var msg = $"{folder.BaseMessage} {e.Message}";

        //        EventLog.WriteEntry(msg + $" > {e.StackTrace}", EventLogEntryType.Error);

        //        EmailNotification.Send(
        //            @from: "filewatcher@jabil.com",
        //            to: folder.Event.DistributionList,
        //            subject: "FileWatcherWService",
        //            body: msg);

        //        if (folder != null)
        //        {
        //            ConnectToOrigin(folder);
        //            FilesStopped(folder);
        //            //folder.Watcher.EnableRaisingEvents = true;
        //        }
        //    }
        //}

        private void ExecuteAction(FolderWatcher folder, string fileName)
        {
            var attempts = 1;
            var toDo = true;
            var totalTime = 0;
            while (toDo)
            {
                // TODO: evaluar si es posible mover esta seccion previo al ciclo WHILE
                var folderDataOrigin = folder.Origin.Credentials != null
                    ? new SharedFolderData
                    {
                        DirectoryBase = folder.Origin.Path,
                        FileName = fileName,
                        Domain = folder.Origin.Credentials.Domain,
                        User = folder.Origin.Credentials.User,
                        Password = folder.Origin.Credentials.Password
                    }
                    : new FolderData
                    {
                        DirectoryBase = folder.Origin.Path,
                        FileName = fileName
                    };

                var newFileName = (folder.Destination.Prefix ?? string.Empty) +
                                  Path.GetFileNameWithoutExtension(fileName) +
                                  (string.IsNullOrEmpty(folder.Destination.Suffix)
                                      ? string.Empty
                                      : DateTime.Now.ToString(folder.Destination.Suffix)) +
                                  Path.GetExtension(fileName);

                IFolderData folderDataDestination = null;
                switch (folder.Destination.FolderType)
                {
                    case FolderType.Normal:
                        folderDataDestination = new FolderData
                        {
                            DirectoryBase = folder.Destination.Path,
                            FileName = newFileName
                        };
                        break;
                    case FolderType.SharedFolder:
                        folderDataDestination = new SharedFolderData
                        {
                            DirectoryBase = folder.Destination.Path,
                            FileName = newFileName,
                            Domain = folder.Destination.Credentials.Domain,
                            User = folder.Destination.Credentials.User,
                            Password = folder.Destination.Credentials.Password
                        };
                        break;
                    case FolderType.Sftp:
                        folderDataDestination = new SFtpData
                        {
                            DirectoryBase = folder.Destination.Path,
                            Domain = folder.Destination.Credentials.Domain,
                            UserName = folder.Destination.Credentials.User,
                            Password = folder.Destination.Credentials.Password,
                            Port = folder.Destination.Credentials.Port,
                            FileName = newFileName
                        };
                        break;
                }

                if (folderDataDestination.GetType() == typeof(SharedFolderData))
                {
                    if (!CheckConnection(folderDataDestination))
                        throw new Exception("The connection to the destination folder failed.");

                }
                FileResult fileResult;
                switch (folder.Event.ActionType)
                {
                    case ActionType.Custom:
                        fileResult = CustomActionCaller(origin: folderDataOrigin,
                            destination: folderDataDestination,
                            customAction: folder.Destination.CustomAction);
                        break;
                    case ActionType.Copy:
                        fileResult = Copy(folderDataOrigin: folderDataOrigin,
                            folderDataTarget: folderDataDestination);
                        break;
                    case ActionType.Move:
                        fileResult = Move(folderDataOrigin: folderDataOrigin,
                            folderDataTarget: folderDataDestination);
                        break;
                    case ActionType.Notify:
                        fileResult = new FileResult {Status = FileStatus.Success};
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (fileResult.Status != FileStatus.Success)
                {
                    EventLog.WriteEntry(
                        $"{folder.BaseMessage} This is the {attempts.ToOrdinalWords()} attempt to file {fileName}: {fileResult.Exception.Message}");

                    System.Threading.Thread.Sleep(1000 * attempts * folder.Event.DelayInSeconds);
                    totalTime += attempts * folder.Event.DelayInSeconds;
                    attempts++;

                    if (attempts > folder.Event.Attempts)
                        throw new Exception(
                            $"The file {fileName} is still locked after {attempts - 1} attempts and {totalTime} seconds waiting. ");
                }
                else
                {
                    // TODO: Log in files
                    //EventLog.WriteEntry(
                    //    $"{folder.BaseMessage} File Action {folder.Event.ActionType} Successful > {newFileName}");

                    toDo = false;
                }
            }
        }

        private FileResult CustomActionCaller(IFolderData origin, IFolderData destination, string customAction)
        {
            var type = _assemblies.FirstOrDefault(a => a.GetTypes().Any(t => t.FullName == customAction))
                ?.GetType(customAction);

            var newInstance = (ICustomAction) Activator.CreateInstance(type ??
                                                                       throw new InvalidOperationException(
                                                                           "CustomAction | Type not match (The class cannot be found)."));

            return newInstance.Run(origin, destination);
        }

        private void WatcherOnError(object sender, ErrorEventArgs errorEventArgs)
        {
            var folder = _config.Folders.FirstOrDefault(f => f.Watcher == (FileSystemWatcher) sender);
            try
            {
                if (folder == null)
                    throw new Exception(
                        $"An error was detected in the file watcher and could not identify your parent controller.");

                var msg = $"{folder.BaseMessage} {errorEventArgs.GetException().Message}";
                EventLog.WriteEntry(msg + $" > {errorEventArgs.GetException().StackTrace}", EventLogEntryType.Error);

                EmailNotification.Send(
                    @from: "filewatcher@jabil.com",
                    to: folder.Event.DistributionList,
                    subject: "FileWatcherWService",
                    body: msg
                );
            }
            catch (Exception e)
            {
                SendErrorToSupport($"{_baseMessage} {e.Message} > {e.StackTrace}");
            }
            finally
            {
                //if (folder != null && !folder.Watcher.EnableRaisingEvents)
                if (folder != null)
                {
                    ConnectToOrigin(folder);
                    FilesStopped(folder);
                    //folder.Watcher.EnableRaisingEvents = true;
                }
            }
        }

        private void FilesStopped(FolderWatcher folder)
        {
            var files = Directory.EnumerateFiles(folder.Origin.Path, folder.Origin.Filter).ToList();

            foreach (var file in files)
            {
                ExecuteAction(folder, file);
            }
        }
    }
}