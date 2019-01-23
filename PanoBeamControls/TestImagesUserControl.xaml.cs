using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace PanoBeam.Controls
{
    public delegate void ShowImageDelegate(BitmapImage image);

    /// <summary>
    /// Interaction logic for TestImagesUserControl.xaml
    /// </summary>
    public partial class TestImagesUserControl
    {
        public event ShowImageDelegate ShowImage;
        private readonly string[] _images =
        {
            "Pattern3240x1080.png",
            "Pattern.png",
            "weiss.png",
            "02_Helligkeit.jpg",
            "03_Kontrast.jpg",
            "04_Farbe.jpg",
            "RoterRahmen.png",
            "schwarz.png",
            "grau.png",
            "orange.png",
            "blau.png",
            "farbstreifen.png",
            "rgbw.png",
            "Mond.jpg",
            "DSC02451.jpg",
            "DSC08285.JPG",
            "DSC08659.JPG",
            "DSC09124.JPG",
            "DSC09822.JPG",
            "DJI_0992-Pano.jpg",
            "DSC00289.jpg",
            "D75_0008.jpg"
        };

        public TestImagesUserControl()
        {
            var thread = new Thread(CreateThumbnails)
            {
                IsBackground = true,
                Priority = ThreadPriority.Lowest
            };

            DataContext = this;
            InitializeComponent();

            thread.Start();
        }

        private void CreateThumbnails()
        {
            var first = true;
            Thread.Sleep(100);
            foreach (var img in _images)
            {
                Thread.Sleep(50);
                Dispatcher.Invoke(() =>
                {
                    var image = new TestImage(img);
                    Images.Add(image);
                    if (first)
                    {
                        DisplayImage(image.UriSource);
                        first = false;
                    }
                });
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var uri = (string) ((Button) sender).Tag;
            DisplayImage(uri);
        }

        private void DisplayImage(string uri)
        {
            if (ShowImage != null)
            {
                var image = new BitmapImage(new Uri(uri));
                ShowImage(image);
            }
        }

        public ObservableCollection<TestImage> Images { get; } = new ObservableCollection<TestImage>();
    }
}
