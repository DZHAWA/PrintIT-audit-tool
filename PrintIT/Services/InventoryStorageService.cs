using System.Collections.ObjectModel;
using System.Text.Json;
using PrintIT.Models;

namespace PrintIT.Services;

public class InventoryStorageService
{
    private readonly string _storagePath = Path.Combine(FileSystem.AppDataDirectory, "inventory_sessions.json");

    public ObservableCollection<InventorySession> LoadSessions()
    {
        if (!File.Exists(_storagePath))
            return new ObservableCollection<InventorySession>();

        try
        {
            var json = File.ReadAllText(_storagePath);
            var sessions = JsonSerializer.Deserialize<List<InventorySession>>(json);
            return sessions != null
                ? new ObservableCollection<InventorySession>(sessions)
                : new ObservableCollection<InventorySession>();
        }
        catch
        {
            return new ObservableCollection<InventorySession>();
        }
    }

    public void SaveSessions(IEnumerable<InventorySession> sessions)
    {
        var json = JsonSerializer.Serialize(sessions, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_storagePath, json);
    }
}