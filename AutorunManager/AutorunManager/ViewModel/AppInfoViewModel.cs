using System.ComponentModel;
using System.Windows.Input;
using Xamarin.Forms;

namespace AutorunManager.ViewModel
{
    public class AppInfoViewModel : INotifyPropertyChanged
    {
        public ICommand SwitchCommand { get; set; }
        public string AppName { get; set; }
        public string PackageName { get; set; }
        public ImageSource Icon { get; set; }

        private bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged(nameof(IsSelected));
                }
            }
        }

        public AppInfoViewModel()
        {
            SwitchCommand = new Command<AppInfoViewModel>(Switch);
        }

        private void Switch(AppInfoViewModel appInfo)
        {
            IsSelected = !IsSelected;
            MainViewModel.Context.OnAppSelected();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
