using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using AutorunManager.Services;
using Xamarin.Essentials;
using Xamarin.Forms;
using AppInfo = AutorunManager.Model.AppInfo;

namespace AutorunManager.ViewModel
{
    public class MainViewModel : INotifyPropertyChanged
    {
        public ICommand SetInitializationTimeCommand { get; set; }

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

        private string _initializationTime;
        public string InitializationTime
        {
            get { return _initializationTime; }
            set
            {
                if (_initializationTime != value)
                {
                    _initializationTime = ValidateNumber(value) == 0
                        ? ""
                        : value;
                    OnPropertyChanged(nameof(InitializationTime));
                }
            }
        }

        private string _appsFilePath;
        private string _initializationTimeFilePath;

        private static MainViewModel _context;

        public MainViewModel()
        {
            _context = this;

            SetInitializationTimeCommand = new Command<MainViewModel>(SetInitializationTime);

            Apps = new ObservableCollection<AppInfoViewModel>();

            _appsFilePath = Path.Combine(FileSystem.AppDataDirectory, "applist.txt");
            _initializationTimeFilePath = Path.Combine(FileSystem.AppDataDirectory, "initializationTime.txt");

            LoadInstalledApps();
            LoadSelectedApps();
            LoadInitializationTime();
        }

        public static MainViewModel Context => _context;

        private void SetInitializationTime(MainViewModel viewModel)
        {
            SaveInitializationTime();
        }

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

        private void LoadSelectedApps()
        {
            try
            {
                if (File.Exists(_appsFilePath))
                {
                    var selectedPackages = File.ReadAllLines(_appsFilePath);
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

        private void LoadInitializationTime()
        {
            try
            {
                if (File.Exists(_initializationTimeFilePath))
                {
                    var initializationTime = File.ReadAllText(_initializationTimeFilePath);
                    InitializationTime = initializationTime;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading app list: {ex.Message}");
            }
        }

        private void SaveInitializationTime()
        {
            try
            {
                var initializationTime = ValidateNumber(InitializationTime);

                File.WriteAllText(_initializationTimeFilePath, initializationTime.ToString());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving app list: {ex.Message}");
            }
        }

        private void SaveSelectedApps()
        {
            try
            {
                if (_appsCache != null)
                {
                    var selectedApps = Apps.Where(app => app.IsSelected).ToList();

                    foreach (var selectedApp in selectedApps)
                    {
                        foreach (var app in _appsCache)
                        {
                            if (selectedApp.AppName == app.AppName && selectedApp.PackageName == app.PackageName)
                            {
                                app.IsSelected = selectedApp.IsSelected;
                            }
                        }
                    }

                    var selectedPackages = _appsCache.Where(app => app.IsSelected).Select(app => app.PackageName);
                    File.WriteAllLines(_appsFilePath, selectedPackages);
                }
                else
                {
                    var selectedPackages = Apps.Where(app => app.IsSelected).Select(app => app.PackageName);
                    File.WriteAllLines(_appsFilePath, selectedPackages);
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

        private int ValidateNumber(string input)
        {
            if (int.TryParse(input, out int result))
            {
                return result;
            }
            return 0;
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
