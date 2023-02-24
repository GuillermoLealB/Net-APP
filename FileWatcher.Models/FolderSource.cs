namespace FileWatcher.Models
{
    public class FolderSource
    {
        public string Path { get; set; }
        public string Filter { get; set; }
        public string Prefix { get; set; }
        public string Suffix { get; set; }
        public FolderCredential Credentials { get; set; }
        public string CustomAction { get; set; }
        public FolderType FolderType { get; set; } = FolderType.Normal;
    }
}