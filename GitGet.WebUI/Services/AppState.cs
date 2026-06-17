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

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? OnDownloadCountChanged;

    private void OnPropertyChanged(string propertyName)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
