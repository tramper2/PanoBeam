using System.Windows.Media.Imaging;

// ReSharper disable once CheckNamespace
namespace PanoBeamLib.Delegates
{
    public delegate void ShowImageDelegate(BitmapImage image);

    public delegate void ProgressDelegate(float progress);
}