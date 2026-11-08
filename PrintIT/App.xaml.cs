using Microsoft.Extensions.DependencyInjection;

namespace PrintIT
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new AppShell())
            {
                Title = "PrintIT",
                MinimumWidth = 400,
                MinimumHeight = 560
            };

#if WINDOWS
            window.Width = 1040;
            window.Height = 760;
#endif
            return window;
        }
    }
}