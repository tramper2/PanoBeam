using System;
using System.Windows.Media.Imaging;

namespace PanoBeam.Controls
{
    public class TestImage : ViewModelBase
    {
        private BitmapSource _thumbnail;
        public BitmapSource Thumbnail
        {
            get => _thumbnail;
            set
            {
                _thumbnail = value;
                OnPropertyChanged();
            }
        }

        public string Name { get; }

        public string UriSource { get; }

        public TestImage(string img)
        {
            Name = img;
            var image = new BitmapImage {DecodePixelHeight = 50};
            image.BeginInit();
            UriSource = @"pack://application:,,,/PanoBeam.Controls;component/Images/" + Name;
            image.UriSource = new Uri(UriSource, UriKind.Absolute);
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.CreateOptions = BitmapCreateOptions.None;
            image.EndInit();
            image.Freeze();

            _thumbnail = image;
        }
    }
}