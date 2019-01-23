using System.Runtime.InteropServices;

namespace PanoBeamLib
{
    class NvApi
    {
        [DllImport(@"NvAPIWrapper", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Initialize();

        [DllImport(@"NvAPIWrapper", CallingConvention = CallingConvention.Cdecl)]
        public static extern int GetMosaicInfo(out MosaicInfo mosaicInfo);

        [DllImport(@"NvAPIWrapper", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Warp(uint displayId, float[] vertices, int numVertices);

        [DllImport(@"NvAPIWrapper", CallingConvention = CallingConvention.Cdecl)]
        public static extern int WarpMultiple(uint[] displayIds, int count, float[] vertices, int numVertices);

        [DllImport(@"NvAPIWrapper", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Blend(uint displayId, float[] blend, float[] offset, int width, int height);

        [DllImport(@"NvAPIWrapper", CallingConvention = CallingConvention.Cdecl)]
        public static extern int UnWarp(uint[] displayIds, int count);

        [DllImport(@"NvAPIWrapper", CallingConvention = CallingConvention.Cdecl)]
        public static extern int UnBlend(uint[] displayIds, int count, int width, int height);

        [DllImport(@"NvAPIWrapper", CallingConvention = CallingConvention.Cdecl)]
        public static extern int ShowImage(uint displayId, float[] image, int width, int height);

        [DllImport(@"NvAPIWrapper", CallingConvention = CallingConvention.Cdecl)]
        public static extern void GetError(int errorcode, out Error error);
                
        public struct Error
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
            public string Message;
        }

        // ReSharper disable once InconsistentNaming
        public const int NVAPI_OK = 0;
    }
}