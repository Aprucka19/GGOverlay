using System.Configuration;
using System.Data;
using System.Windows;

namespace GGOverlay
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            // Additional startup logic if needed
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            // Additional cleanup logic if needed
            // For example, ensure that the GameInterface's Stop method is called
        }
    }
}

