using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace AutorunManager
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
        }
    }

    public class MainViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private List<AppInfoViewModel> _appsCache { get; set; }
        private ObservableCollection<AppInfoViewModel> _apps { get; set; }
        public ObservableCollection<AppInfoViewModel> Apps
        {
            get
            {
                return _apps;
            }
            set
            {
                if (_apps != value)
                {
                    _apps = new ObservableCollection<AppInfoViewModel>(value.OrderByDescending(x => x.IsSelected).ThenBy(x => x.AppName).ToList());
                    OnPropertyChanged(nameof(Apps));
                }
            }
        }

        private string _searchText;
        public string SearchText
        {
            get { return _searchText; }
            set
            {
                if (_searchText != value)
                {
                    _searchText = value;
                    OnPropertyChanged(nameof(SearchText));
                    Search();
                }
            }
        }

        private string _filePath;

        private static MainViewModel _context;

        public MainViewModel()
        {
            _context = this;

            Apps = new ObservableCollection<AppInfoViewModel>();

            // Путь к файлу на устройстве для сохранения выбранных приложений
            _filePath = Path.Combine(FileSystem.AppDataDirectory, "applist.txt");

            LoadInstalledApps();
            LoadSelectedApps();
        }

        public static MainViewModel Context => _context;

        private void LoadInstalledApps()
        {
            try
            {
                IAppInfoService appInfoService = DependencyService.Get<IAppInfoService>();
                List<AppInfo> installedApps = appInfoService.GetInstalledApps();

                // Очищаем коллекцию перед добавлением новых данных
                Apps.Clear();

                foreach (var app in installedApps)
                {
                    Apps.Add(new AppInfoViewModel
                    {
                        AppName = app.AppName,
                        PackageName = app.PackageName,
                        Icon = app.Icon
                    });
                }
            }
            catch { }
        }

        private async void LoadSelectedApps()
        {
            try
            {
                if (File.Exists(_filePath))
                {
                    var selectedPackages = File.ReadAllLines(_filePath);
                    foreach (var app in Apps)
                    {
                        if (selectedPackages.Contains(app.PackageName))
                        {
                            app.IsSelected = true;
                        }
                    }

                    Apps = new ObservableCollection<AppInfoViewModel>(Apps);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading app list: {ex.Message}");
            }
        }

        private async void SaveSelectedApps()
        {
            try
            {
                if(_appsCache != null)
                {
                    var selectedApps = Apps.Where(app => app.IsSelected).ToList();

                    foreach(var selectedApp in selectedApps)
                    {
                        foreach(var app in _appsCache)
                        {
                            if(selectedApp.AppName == app.AppName && selectedApp.PackageName == app.PackageName)
                            {
                                app.IsSelected = selectedApp.IsSelected;
                            }
                        }
                    }

                    var selectedPackages = _appsCache.Where(app => app.IsSelected).Select(app => app.PackageName);
                    File.WriteAllLines(_filePath, selectedPackages);
                }
                else
                {
                    var selectedPackages = Apps.Where(app => app.IsSelected).Select(app => app.PackageName);
                    File.WriteAllLines(_filePath, selectedPackages);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving app list: {ex.Message}");
            }
        }

        public void OnAppSelected()
        {
            SaveSelectedApps();
        }

        private void Search()
        {
            if (_appsCache == null)
            {
                _appsCache = Apps.ToList();
            }

            if (string.IsNullOrEmpty(SearchText))
            {
                Apps = new ObservableCollection<AppInfoViewModel>(_appsCache);
                return;
            }

            Apps.Clear();
            Apps = new ObservableCollection<AppInfoViewModel>(_appsCache.Where(x =>
            {
                return x.AppName.ToUpper().Contains(SearchText.ToUpper())
                    || x.PackageName.ToUpper().Contains(SearchText.ToUpper());
            }).ToList());
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

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
