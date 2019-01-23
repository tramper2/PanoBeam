#include "nvapi.h"
//#define TRIAL

struct MosaicInfo {
	NvU32 ProjectorWidth;
	NvU32 ProjectorHeight;
	NvS32 Overlap;
	NvU32 DisplayId0;
	NvU32 DisplayId1;
};

struct Error {
	NvAPI_ShortString Message;
};

extern "C" __declspec(dllexport) int Initialize();

extern "C" __declspec(dllexport) int GetMosaicInfo(MosaicInfo* mosaicInfo);

extern "C" __declspec(dllexport) int Warp(NvU32 displayId, float vertices[], int numVertices);

extern "C" __declspec(dllexport) int WarpMultiple(NvU32 displayIds[], int count, float vertices[], int numVertices);

extern "C" __declspec(dllexport) int Blend(NvU32 displayId, float blend[], float offset[], int width, int height);

extern "C" __declspec(dllexport) int UnWarp(NvU32 displayIds[], int count);

extern "C" __declspec(dllexport) int UnBlend(NvU32 displayIds[], int count, int width, int height);

extern "C" __declspec(dllexport) int ShowImage(NvU32 displayId, float image[], int width, int height);

extern "C" __declspec(dllexport) void GetError(NvAPI_Status errorcode, Error* errod);