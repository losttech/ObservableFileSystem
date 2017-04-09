namespace LostTech.App
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    public sealed class ObservableFile: INotifyPropertyChanged
    {
        string fullName;
        int changeCount;

        public string FullName
        {
            get {
                return this.fullName;
            }
            internal set {
                this.fullName = value;
                this.OnPropertyChanged();
            }
        }

        public int ChangeCount
        {
            get {
                return this.changeCount;
            }
            internal set {
                this.changeCount = value;
                this.OnPropertyChanged();
            }
        }

        internal ObservableFile(string fullName) { this.FullName = fullName; }

        void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
