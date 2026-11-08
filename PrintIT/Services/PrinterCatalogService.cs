using System.Text.Json;

namespace PrintIT.Services;

public class PrinterCatalogService
{
    private readonly string _catalogPath = Path.Combine(FileSystem.AppDataDirectory, "catalog_printers.json");
    private HashSet<string> _models = new(StringComparer.OrdinalIgnoreCase);

    public event Action? CatalogChanged;

    public PrinterCatalogService()
    {
        LoadCatalog();
    }

    public List<string> GetAll() => _models.OrderBy(m => m).ToList();

    public void AddModel(string modelName)
    {
        string trimmed = modelName.Trim();
        if (string.IsNullOrWhiteSpace(trimmed)) return;

        if (_models.Add(trimmed))
        {
            SaveCatalog();
            CatalogChanged?.Invoke();
        }
    }

    private void LoadCatalog()
    {
        if (File.Exists(_catalogPath))
        {
            try
            {
                var json = File.ReadAllText(_catalogPath);
                var items = JsonSerializer.Deserialize<List<string>>(json);
                if (items != null)
                {
                    _models = new HashSet<string>(items, StringComparer.OrdinalIgnoreCase);
                    return;
                }
            }
            catch
            {
                // При ошибке чтения разворачиваем стартовый список
            }
        }

        // Стартовый парк комитета (будет дополняться на лету)
        // Полный нормализованный парк оргтехники комитета (по дампу из Excel)
        _models = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // HP — Черно-белые рабочие лошадки
            "HP LaserJet 1010",
            "HP LaserJet 1320",
            "HP LaserJet P2015",
            "HP LaserJet P2015dn",
            "HP LaserJet P2055",
            "HP LaserJet P2055dn",
            "HP LaserJet Pro 400 M401",
            "HP LaserJet Pro 400 M401dn",
            "HP LaserJet Pro 400 M401dne",
            "HP LaserJet Pro M402dn",
            "HP LaserJet Pro M402dne",
            "HP LaserJet 5200",
            "HP LaserJet 5200dtn",
            "HP LaserJet Enterprise 700 M712",
            "HP LaserJet Enterprise 700 M712dn",

            // HP — Цветные и мобильные
            "HP Color LaserJet CP2025",
            "HP Color LaserJet CP2025dn",
            "HP Color LaserJet 4700dtn",
            "HP Color LaserJet 5550dn",
            "HP Color LaserJet Pro M454dn",
            "HP LaserJet Pro 400 Color M451dn",
            "HP DeskJet 7000",
            "HP OfficeJet 252 Mobile",

            // Kyocera
            "Kyocera FS-1040",
            "Kyocera ECOSYS P2035d",
            "Kyocera ECOSYS P2035dn",
            "Kyocera ECOSYS P2135dn",
            "Kyocera ECOSYS P3145dn",
            "Kyocera ECOSYS P4040dn",

            // Xerox
            "Xerox Phaser 3100",
            "Xerox Phaser 3330",
            "Xerox Phaser 5335",
            "Xerox Phaser 6125",
            "Xerox Phaser 6125N",
            "Xerox VersaLink C7000",

            // Zebra (термопринтеры этикеток и штрихкодов)
            "Zebra GK420t",
            "Zebra TLP 2844",

            // Epson, Canon, OKI, Lexmark
            "Epson Stylus B-510DN",
            "Epson WorkForce Pro WP-4535DWF",
            "Canon PIXMA MX850",
            "Canon PIXMA MX894",
            "OKI C911",
            "Lexmark MS415dn"
        };
        SaveCatalog();
    }

    private void SaveCatalog()
    {
        var json = JsonSerializer.Serialize(_models.ToList(), new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_catalogPath, json);
    }
}