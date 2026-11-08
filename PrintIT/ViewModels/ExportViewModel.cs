using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PrintIT.Models;
using PrintIT.Services;

namespace PrintIT.ViewModels;

public partial class ExportViewModel : ObservableObject, IQueryAttributable
{
    private readonly ExportService _exportService;

    [ObservableProperty]
    public partial InventorySession? Session { get; set; }

    [ObservableProperty]
    public partial bool IsDocxSelected { get; set; } = true;

    [ObservableProperty]
    public partial bool IsXlsxSelected { get; set; }

    [ObservableProperty]
    public partial bool OnlyUrgent { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(GenerateButtonText))]
    public partial bool IsBusy { get; set; }

    // Что попадёт в отчёт: "В отчёт попадут 12 устройств из 4 кабинетов"
    [ObservableProperty]
    public partial string ReportSummary { get; set; } = string.Empty;

    // Результат последней выгрузки: "Отчет_картриджи_....docx сохранён в «Загрузки»"
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasResult))]
    public partial string ResultMessage { get; set; } = string.Empty;

    public bool HasResult => !string.IsNullOrEmpty(ResultMessage);

    public string GenerateButtonText => IsBusy ? "Формирую отчёт…" : "Сформировать отчёт";

    public ExportViewModel(ExportService exportService)
    {
        _exportService = exportService;
    }

    // Гарантированное получение переданного объекта Session при переходе
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("Session", out var sessionObj) && sessionObj is InventorySession session)
        {
            Session = session;
        }
    }

    partial void OnSessionChanged(InventorySession? value) => UpdateSummary();

    partial void OnOnlyUrgentChanged(bool value) => UpdateSummary();

    private void UpdateSummary()
    {
        if (Session == null)
        {
            ReportSummary = string.Empty;
            return;
        }

        var rooms = Session.Rooms
            .Where(r => r.Printers.Any(p => !OnlyUrgent || p.IsUrgent))
            .ToList();
        int devices = rooms.Sum(r => r.Printers.Count(p => !OnlyUrgent || p.IsUrgent));

        if (devices == 0)
        {
            ReportSummary = OnlyUrgent
                ? "Устройств, которым нужна замена, нет — отчёт будет пустым"
                : "В учёте пока нет устройств — отчёт будет пустым";
            return;
        }

        string verb = Plural.Choose(devices, "попадёт", "попадут", "попадут");
        ReportSummary = $"В отчёт {verb} {Plural.Format(devices, "устройство", "устройства", "устройств")} " +
                        $"из {rooms.Count} {Plural.Choose(rooms.Count, "кабинета", "кабинетов", "кабинетов")}";
    }

    [RelayCommand]
    private void SelectDocx()
    {
        IsDocxSelected = true;
        IsXlsxSelected = false;
    }

    [RelayCommand]
    private void SelectXlsx()
    {
        IsDocxSelected = false;
        IsXlsxSelected = true;
    }

    [RelayCommand]
    private async Task GenerateAndShareAsync()
    {
        if (Session == null)
        {
            await Shell.Current.DisplayAlert("Не удалось сформировать отчёт", "Данные учёта не найдены. Вернитесь назад и откройте учёт заново.", "OK");
            return;
        }

        if (IsBusy) return;

        try
        {
            IsBusy = true;
            ResultMessage = string.Empty;

            string generatedPath = IsDocxSelected
                ? _exportService.GenerateDocx(Session, OnlyUrgent)
                : _exportService.GenerateXlsx(Session, OnlyUrgent);

            string savedPath = await _exportService.SaveToDownloadsAsync(generatedPath);
            ResultMessage = $"{Path.GetFileName(savedPath)} сохранён в «Загрузки»";

            await _exportService.ShareReportAsync(generatedPath, $"Отчёт: {Session.Title}");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Не удалось сформировать отчёт", ex.Message, "OK");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
