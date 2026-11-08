using Microsoft.Extensions.Logging;
using PrintIT.Services;
using PrintIT.ViewModels;
using PrintIT.Views;
namespace PrintIT
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });

#if DEBUG
    		builder.Logging.AddDebug();
#endif

            // Приложение русскоязычное: даты и числа всегда в русском формате ("3 ноября 2026")
            var ru = new System.Globalization.CultureInfo("ru-RU");
            System.Globalization.CultureInfo.DefaultThreadCurrentCulture = ru;
            System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = ru;

            // Поля ввода без собственной рамки/подчёркивания: оформление задаёт подложка Border в XAML
            Microsoft.Maui.Handlers.EntryHandler.Mapper.AppendToMapping("Borderless", (handler, view) =>
            {
#if ANDROID
                handler.PlatformView.BackgroundTintList =
                    Android.Content.Res.ColorStateList.ValueOf(Android.Graphics.Color.Transparent);
#elif WINDOWS
                var textBox = handler.PlatformView;
                var transparent = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Transparent);
                textBox.BorderThickness = new Microsoft.UI.Xaml.Thickness(0);
                foreach (var key in new[]
                {
                    "TextControlBackground", "TextControlBackgroundPointerOver", "TextControlBackgroundFocused",
                    "TextControlBorderBrush", "TextControlBorderBrushPointerOver", "TextControlBorderBrushFocused"
                })
                {
                    textBox.Resources[key] = transparent;
                }
#endif
            });

            builder.Services.AddSingleton<PrinterCatalogService>();
            builder.Services.AddSingleton<InventoryStorageService>();
            builder.Services.AddSingleton<ExportService>();
            // Регистрация ViewModels
            builder.Services.AddTransient<MainViewModel>();
            builder.Services.AddTransient<InventoryViewModel>();
            builder.Services.AddTransient<ExportViewModel>();
            // Регистрация Views
            // Регистрация Views
            builder.Services.AddTransient<SessionsPage>();
            builder.Services.AddTransient<InventoryPage>();
            builder.Services.AddTransient<ExportPage>();
            return builder.Build();
        }
    }
}
