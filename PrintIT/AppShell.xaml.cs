using PrintIT.Views;

namespace PrintIT;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Регистрация маршрутов для перехода между экранами
        Routing.RegisterRoute(nameof(InventoryPage), typeof(InventoryPage));
        Routing.RegisterRoute(nameof(ExportPage), typeof(ExportPage));
    }
}