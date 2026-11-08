using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PrintIT.Models;
using PrintIT.Services;
using System.Collections.ObjectModel;

namespace PrintIT.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly InventoryStorageService _storageService;

    [ObservableProperty]
    public partial ObservableCollection<InventorySession> Sessions { get; set; } = new();

    [ObservableProperty]
    public partial bool IsEmpty { get; set; }

    public MainViewModel(InventoryStorageService storageService)
    {
        _storageService = storageService;
        LoadSessions();
    }

    public void LoadSessions()
    {
        Sessions = _storageService.LoadSessions();
        IsEmpty = Sessions.Count == 0;
    }

    [RelayCommand]
    private async Task CreateNewSessionAsync()
    {
        // Запрос названия сессии у пользователя
        string title = await Shell.Current.DisplayPromptAsync(
            "Новый учёт",
            "Назовите учёт так, чтобы его было легко найти позже — например, по корпусу или этажу.",
            initialValue: $"Учёт от {DateTime.Now:d MMMM}",
            accept: "Создать",
            cancel: "Отмена");

        if (string.IsNullOrWhiteSpace(title)) return;

        var newSession = new InventorySession { Title = title.Trim() };
        Sessions.Insert(0, newSession);
        IsEmpty = false;
        _storageService.SaveSessions(Sessions);

        // Переход на экран учёта
        await Shell.Current.GoToAsync("InventoryPage", new Dictionary<string, object>
        {
            { "Session", newSession }
        });
    }

    [RelayCommand]
    private async Task OpenSessionAsync(InventorySession session)
    {
        if (session == null) return;

        await Shell.Current.GoToAsync("InventoryPage", new Dictionary<string, object>
        {
            { "Session", session }
        });
    }

    [RelayCommand]
    private async Task DeleteSessionAsync(InventorySession session)
    {
        if (session == null) return;

        bool confirm = await Shell.Current.DisplayAlert(
            "Удалить учёт?",
            $"«{session.Title}» и все его кабинеты будут удалены. Это действие нельзя отменить.",
            "Удалить",
            "Отмена");

        if (confirm)
        {
            Sessions.Remove(session);
            IsEmpty = Sessions.Count == 0;
            _storageService.SaveSessions(Sessions);
        }
    }
}