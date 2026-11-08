using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Text.Json.Serialization;

namespace PrintIT.Models;

public partial class PrinterItem : ObservableObject
{
    [ObservableProperty]
    public partial string Model { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsUrgent { get; set; }

    public PrinterItem() { }

    public PrinterItem(string model, bool isUrgent = false)
    {
        Model = model;
        IsUrgent = isUrgent;
    }
}

public partial class OfficeRoom : ObservableObject
{
    [ObservableProperty]
    public partial string RoomNumber { get; set; } = string.Empty;

    public ObservableCollection<PrinterItem> Printers { get; set; } = new();

    public OfficeRoom() { }

    public OfficeRoom(string roomNumber)
    {
        RoomNumber = roomNumber;
    }
}

public class InventorySession
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = $"Учёт от {DateTime.Now:dd.MM.yyyy HH:mm}";
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public ObservableCollection<OfficeRoom> Rooms { get; set; } = new();

    [JsonIgnore]
    public int TotalPrinters => Rooms.Sum(r => r.Printers.Count);

    [JsonIgnore]
    public int UrgentCount => Rooms.Sum(r => r.Printers.Count(p => p.IsUrgent));
}