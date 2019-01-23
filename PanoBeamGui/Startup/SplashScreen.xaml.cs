using System;
using System.Reflection;

namespace PanoBeam.Startup
{
    /// <summary>
    /// Interaction logic for SplashScreen.xaml
    /// </summary>
    public partial class SplashScreen
    {
        public SplashScreen()
        {
            InitializeComponent();

            var version = Assembly.GetExecutingAssembly().GetName().Version;
            LabelVersion.Content = $"Version {version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }

        public event EventHandler InitializingCompleted;

        private void Storyboard_Completed(object sender, EventArgs e)
        {
            InitializingCompleted?.Invoke(this, null);
        }
    }
}
