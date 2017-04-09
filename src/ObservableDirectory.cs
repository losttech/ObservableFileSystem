namespace LostTech.App
{
    using System;
    using System.Collections.ObjectModel;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public sealed class ObservableDirectory : IDisposable
    {
        readonly FileSystemWatcher watcher;
        readonly TaskScheduler taskScheduler;
        readonly ObservableCollection<ObservableFile> files = new ObservableCollection<ObservableFile>();

        public ReadOnlyObservableCollection<ObservableFile> Files { get; }
        public string Path { get; }

        public ObservableDirectory(string path, bool captureCurrentContext = true)
        {
            this.watcher = new FileSystemWatcher(path);
            foreach(var file in Directory.EnumerateFiles(path).Select(file => new ObservableFile(file)))
                this.files.Add(file);
            this.Files = new ReadOnlyObservableCollection<ObservableFile>(this.files);
            this.taskScheduler = captureCurrentContext ? TaskScheduler.FromCurrentSynchronizationContext() : null;
            this.watcher.Created += EntryCreated;
            this.watcher.Renamed += EntryRenamed;
            this.watcher.Deleted += EntryDeleted;
            this.watcher.Changed += EntryChanged;
            this.watcher.EnableRaisingEvents = true;
            this.Path = path;
        }

        void RunOnScheduler(Action action)
        {
            if (this.taskScheduler == null)
                action();
            else
                Task.Factory.StartNew(action,
                    cancellationToken: CancellationToken.None,
                    creationOptions: TaskCreationOptions.None,
                    scheduler: this.taskScheduler);
        }

        private void EntryDeleted(object sender, FileSystemEventArgs e)
        {
            RunOnScheduler(() => {
                var existing = this.files.FirstOrDefault(file => file.FullName == e.FullPath);
                if (existing != null)
                    this.files.Remove(existing);
            });
        }

        private void EntryRenamed(object sender, RenamedEventArgs e)
        {
            RunOnScheduler(() => {
                // TODO: check what happens if file is moved outside of the directory
                var existing = this.files.FirstOrDefault(file => file.FullName == e.OldFullPath);
                if (existing != null)
                    existing.FullName = e.FullPath;
                else
                    this.files.Add(new ObservableFile(e.FullPath));
            });
        }

        private void EntryCreated(object sender, FileSystemEventArgs e)
        {
            RunOnScheduler(() => {
                var file = new ObservableFile(e.FullPath);
                this.files.Add(file);
            });
        }

        private void EntryChanged(object sender, FileSystemEventArgs e)
        {
            RunOnScheduler(() => {
                var existing = this.files.FirstOrDefault(file => file.FullName == e.FullPath);
                if (existing != null)
                    existing.ChangeCount++;
            });
        }

        public void Dispose() => this.watcher.Dispose();
    }
}
