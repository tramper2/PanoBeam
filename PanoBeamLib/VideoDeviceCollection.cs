using System.Collections;
using AForge.Video.DirectShow;

namespace PanoBeamLib
{
    public class VideoDeviceCollection : CollectionBase
    {
        public VideoDeviceCollection()
        {
            // ReSharper disable once CollectionNeverUpdated.Local
            var videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            foreach (FilterInfo videoDevice in videoDevices)
            {
                InnerList.Add(new VideoDevice
                {
                    Name = videoDevice.Name,
                    MonikerString = videoDevice.MonikerString
                });
            }
        }

        public VideoDevice this[int index] => ((VideoDevice)InnerList[index]);
    }
}