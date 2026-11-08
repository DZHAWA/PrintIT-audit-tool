using Android.App;
using Android.Content.PM;
using Android.Content.Res;
using Android.OS;
using AndroidX.Core.View;

namespace PrintIT
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            ApplySystemBars();
        }

        public override void OnConfigurationChanged(Configuration newConfig)
        {
            base.OnConfigurationChanged(newConfig);
            ApplySystemBars();
        }

        // Системные панели в цвет фона приложения, значки — контрастные текущей теме
        private void ApplySystemBars()
        {
            if (Window == null || Resources?.Configuration == null) return;

            bool isDark = (Resources.Configuration.UiMode & UiMode.NightMask) == UiMode.NightYes;
            var barColor = Android.Graphics.Color.ParseColor(isDark ? "#131415" : "#F5F4F1");

#pragma warning disable CA1422 // На Android 15+ панели прозрачные, цвет игнорируется — это нормально
            Window.SetStatusBarColor(barColor);
            Window.SetNavigationBarColor(barColor);
#pragma warning restore CA1422

            var controller = WindowCompat.GetInsetsController(Window, Window.DecorView);
            controller.AppearanceLightStatusBars = !isDark;
            controller.AppearanceLightNavigationBars = !isDark;
        }
    }
}
