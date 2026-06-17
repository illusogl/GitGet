using GitGet.Core.Models;
using System.ComponentModel;

namespace GitGet.WebUI.Services;

public class AppState : INotifyPropertyChanged
{
    private Repository? _selectedRepository;
    private int _activeDownloadCount;
    private string? _currentUserLogin;

    public Repository? SelectedRepository
    {
        get => _selectedRepository;
        set
        {
            _selectedRepository = value;
            OnPropertyChanged(nameof(SelectedRepository));
        }
    }

    public int ActiveDownloadCount
    {
        get => _activeDownloadCount;
        set
        {
            _activeDownloadCount = value;
            OnPropertyChanged(nameof(ActiveDownloadCount));
            OnDownloadCountChanged?.Invoke();
        }
    }

    public string? CurrentUserLogin
    {
        get => _currentUserLogin;
        set
        {
            _currentUserLogin = value;
            OnPropertyChanged(nameof(CurrentUserLogin));
        }
    }

    private bool _isDarkMode;
    public bool IsDarkMode
    {
        get => _isDarkMode;
        set
        {
            if (_isDarkMode != value)
            {
                _isDarkMode = value;
                OnPropertyChanged(nameof(IsDarkMode));
                OnThemeChanged?.Invoke();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? OnDownloadCountChanged;
    public event Action? OnThemeChanged;

    private void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
