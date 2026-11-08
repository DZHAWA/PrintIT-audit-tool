using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PrintIT.Models;
using PrintIT.Services;
using System.Collections.ObjectModel;

namespace PrintIT.ViewModels;

[QueryProperty(nameof(Session), "Session")]
public partial class InventoryViewModel : ObservableObject
{
    private readonly PrinterCatalogService _catalogService;
    private readonly InventoryStorageService _storageService;

    [ObservableProperty]
    public partial InventorySession? Session { get; set; }

    [ObservableProperty]
    public partial string NewRoomNumber { get; set; } = string.Empty;

    // Свойства для всплывающего окна / панели добавления техники
    [ObservableProperty]
    public partial bool IsAddPrinterDialogVisible { get; set; }

    [ObservableProperty]
    public partial string SelectedRoomTitle { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PrinterSearchText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsPrinterUrgent { get; set; }

    [ObservableProperty]
    public partial bool ShowAddCustomButton { get; set; }

    // Сводка под заголовком: "4 кабинета · 12 устройств · 2 на замену"
    [ObservableProperty]
    public partial string SummaryText { get; set; } = string.Empty;

    public ObservableCollection<string> FilteredCatalog { get; } = new();

    private OfficeRoom? _targetRoom;

    public InventoryViewModel(PrinterCatalogService catalogService, InventoryStorageService storageService)
    {
        _catalogService = catalogService;
        _storageService = storageService;

        _catalogService.CatalogChanged += RefreshCatalog;
        RefreshCatalog();
    }

    partial void OnPrinterSearchTextChanged(string value)
    {
        FilterCatalog(value);
    }

    partial void OnSessionChanged(InventorySession? value)
    {
        UpdateSummary();
    }

    private void UpdateSummary()
    {
        if (Session == null)
        {
            SummaryText = string.Empty;
            return;
        }

        var parts = new List<string>
        {
            Plural.Format(Session.Rooms.Count, "кабинет", "кабинета", "кабинетов"),
            Plural.Format(Session.TotalPrinters, "устройство", "устройства", "устройств")
        };

        if (Session.UrgentCount > 0)
            parts.Add($"{Session.UrgentCount} на замену");

        SummaryText = string.Join("  ·  ", parts);
    }

    private void RefreshCatalog()
    {
        FilterCatalog(PrinterSearchText);
    }

    private void FilterCatalog(string query)
    {
        var all = _catalogService.GetAll();
        FilteredCatalog.Clear();

        string trimmed = query?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(trimmed))
        {
            foreach (var item in all) FilteredCatalog.Add(item);
            ShowAddCustomButton = false;
            return;
        }

        var matches = all.Where(m => m.Contains(trimmed, StringComparison.OrdinalIgnoreCase)).ToList();
        foreach (var match in matches) FilteredCatalog.Add(match);

        // Показываем кнопку "Добавить в справочник", если точного совпадения нет
        ShowAddCustomButton = !all.Any(m => m.Equals(trimmed, StringComparison.OrdinalIgnoreCase));
    }

    [RelayCommand]
    private void AddRoom()
    {
        string roomNum = NewRoomNumber?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(roomNum) || Session == null) return;

        // Добавляем кабинет в начало списка
        Session.Rooms.Insert(0, new OfficeRoom(roomNum));
        NewRoomNumber = string.Empty;
        SaveCurrentState();
    }

    [RelayCommand]
    private async Task DeleteRoomAsync(OfficeRoom room)
    {
        if (room == null || Session == null) return;

        bool confirm = await Shell.Current.DisplayAlert(
            $"Удалить кабинет {room.RoomNumber}?",
            room.Printers.Count > 0
                ? $"Вместе с кабинетом будут удалены {Plural.Format(room.Printers.Count, "устройство", "устройства", "устройств")}."
                : "Кабинет будет удалён из учёта.",
            "Удалить",
            "Отмена");

        if (confirm)
        {
            Session.Rooms.Remove(room);
            SaveCurrentState();
        }
    }

    [RelayCommand]
    private void OpenAddPrinterDialog(OfficeRoom room)
    {
        if (room == null) return;

        _targetRoom = room;
        SelectedRoomTitle = $"Кабинет {room.RoomNumber}";
        PrinterSearchText = string.Empty;
        IsPrinterUrgent = false;
        FilterCatalog(string.Empty);
        IsAddPrinterDialogVisible = true;
    }

    [RelayCommand]
    private void CloseAddPrinterDialog()
    {
        IsAddPrinterDialogVisible = false;
        _targetRoom = null;
    }

    [RelayCommand]
    private void SelectModelAndAdd(string model)
    {
        if (_targetRoom == null || string.IsNullOrWhiteSpace(model)) return;

        _targetRoom.Printers.Add(new PrinterItem(model.Trim(), IsPrinterUrgent));
        CloseAddPrinterDialog();
        SaveCurrentState();
    }

    [RelayCommand]
    private void RegisterCustomModelAndAdd()
    {
        string newModel = PrinterSearchText?.Trim() ?? string.Empty;
        if (_targetRoom == null || string.IsNullOrWhiteSpace(newModel)) return;

        // Регистрируем в общий постоянный каталог
        _catalogService.AddModel(newModel);

        // Добавляем в текущий кабинет
        _targetRoom.Printers.Add(new PrinterItem(newModel, IsPrinterUrgent));
        CloseAddPrinterDialog();
        SaveCurrentState();
    }

    [RelayCommand]
    private void ToggleUrgent(PrinterItem printer)
    {
        if (printer == null) return;

        printer.IsUrgent = !printer.IsUrgent;
        SaveCurrentState();
    }

    [RelayCommand]
    private void DeletePrinter(PrinterItem printer)
    {
        if (printer == null || Session == null) return;

        foreach (var room in Session.Rooms)
        {
            if (room.Printers.Contains(printer))
            {
                room.Printers.Remove(printer);
                SaveCurrentState();
                break;
            }
        }
    }

    [RelayCommand]
    private async Task GoToExportAsync()
    {
        if (Session == null) return;

        await Shell.Current.GoToAsync("ExportPage", new Dictionary<string, object>
        {
            { "Session", Session }
        });
    }

    private void SaveCurrentState()
    {
        if (Session == null) return;

        var allSessions = _storageService.LoadSessions();
        var existing = allSessions.FirstOrDefault(s => s.Id == Session.Id);

        if (existing != null)
        {
            int index = allSessions.IndexOf(existing);
            allSessions[index] = Session;
        }
        else
        {
            allSessions.Insert(0, Session);
        }

        _storageService.SaveSessions(allSessions);
        UpdateSummary();
    }
}