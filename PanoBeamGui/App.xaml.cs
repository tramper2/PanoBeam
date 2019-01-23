using System;
using System.Windows;

namespace PanoBeam
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    // ReSharper disable once RedundantExtendsListEntry
    public partial class App : Application
    {

        public App()
        {
            DispatcherUnhandledException += App_DispatcherUnhandledException;
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show(e.Exception.ToString(), "Fehler");
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            var splashScreen = new Startup.SplashScreen();
            splashScreen.InitializingCompleted += SplashScreen_InitializingCompleted;
            splashScreen.Show();
        }

        private void SplashScreen_InitializingCompleted(object splashScreen, EventArgs e)
        {
            ShutdownMode = ShutdownMode.OnLastWindowClose;
            var mainWindow = new MainWindow();
            mainWindow.Loaded += (sender, args) => ((Startup.SplashScreen)splashScreen).Close();
            mainWindow.Show();
        }
    }
}
