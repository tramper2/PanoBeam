#include "stdafx.h"
#include "NvApiLib.h"
#include "nvapi.h"
//#include "log.h"

using namespace std;

void GetError(NvAPI_Status errorcode, Error* error) {
	NvAPI_GetErrorMessage(errorcode, error->Message);
}

int Initialize() {
	//FILELog::ReportingLevel() = logDEBUG;
	//FILE* log_fd;
	//fopen_s(&log_fd, "C:\\Temp\\nvapi.log", "w");
	//Output2FILE::Stream() = log_fd;
	//FILE_LOG(logINFO) << "Initialize";
	NvAPI_Status error;
	error = NvAPI_Initialize();
	if (error != NVAPI_OK)
	{
		return error;
	}
	return NVAPI_OK;
}

int GetMosaicInfo(MosaicInfo* mosaicInfo) {
	NvAPI_Status error;
	NV_MOSAIC_TOPO_BRIEF  topo;
	topo.version = NVAPI_MOSAIC_TOPO_BRIEF_VER;
	NV_MOSAIC_DISPLAY_SETTING dispSetting;
	dispSetting.version = NVAPI_MOSAIC_DISPLAY_SETTING_VER;
	NvS32 overlapX, overlapY;
	error = NvAPI_Mosaic_GetCurrentTopo(&topo, &dispSetting, &overlapX, &overlapY);
	if (error != NVAPI_OK)
	{
		return error;
	}
	mosaicInfo->Overlap = overlapX;

	NvU32 gridCount = 0;
	error = NvAPI_Mosaic_EnumDisplayGrids(NULL, &gridCount);
	if (error != NVAPI_OK)
	{
		return error;
	}
	NV_MOSAIC_GRID_TOPO* grids = NULL;
	grids = new NV_MOSAIC_GRID_TOPO[gridCount];
	grids->version = NV_MOSAIC_GRID_TOPO_VER;
	error = NvAPI_Mosaic_EnumDisplayGrids(grids, &gridCount);
	if (error != NVAPI_OK) {
		return error;
	}
	for (NvU32 i = 0; i < gridCount; i++) {
		if (grids[i].displayCount != 2) {
			continue;
		}
		mosaicInfo->ProjectorWidth = grids[i].displaySettings.width;
		mosaicInfo->ProjectorHeight = grids[i].displaySettings.height;
		mosaicInfo->DisplayId0 = grids[i].displays[0].displayId;
		mosaicInfo->DisplayId1 = grids[i].displays[1].displayId;
		break;
	}
	return error;
}

int Warp(NvU32 displayId, float vertices[], int numVertices) {
	NvAPI_Status error;
	NV_SCANOUT_WARPING_DATA warpingData;
	int maxNumVertices = 0;
	int sticky = 0;

	NV_SCANOUT_INFORMATION scanInfo;

	ZeroMemory(&scanInfo, sizeof(NV_SCANOUT_INFORMATION));
	scanInfo.version = NV_SCANOUT_INFORMATION_VER;

	error = NvAPI_GPU_GetScanoutConfigurationEx(displayId, &scanInfo);
	if (error != NVAPI_OK)
	{
		return error;
	}

	NV_MOSAIC_TOPO_BRIEF  topo;
	topo.version = NVAPI_MOSAIC_TOPO_BRIEF_VER;

	NV_MOSAIC_DISPLAY_SETTING dispSetting;
	dispSetting.version = NVAPI_MOSAIC_DISPLAY_SETTING_VER;

	NvS32 overlapX, overlapY;
	float srcLeft, srcTop, srcWidth, srcHeight;


	error = NvAPI_Mosaic_GetCurrentTopo(&topo, &dispSetting, &overlapX, &overlapY);
	if (error != NVAPI_OK)
	{
		return error;
	}

	if (topo.enabled == false)
	{
		srcLeft = (float)scanInfo.sourceDesktopRect.sX;
		srcTop = (float)scanInfo.sourceDesktopRect.sY;
		srcWidth = (float)scanInfo.sourceDesktopRect.sWidth;
		srcHeight = (float)scanInfo.sourceDesktopRect.sHeight;
	}
	else
	{
		srcLeft = (float)scanInfo.sourceViewportRect.sX;
		srcTop = (float)scanInfo.sourceViewportRect.sY;
		srcWidth = (float)scanInfo.sourceViewportRect.sWidth;
		srcHeight = (float)scanInfo.sourceViewportRect.sHeight;
	}

	warpingData.version = NV_SCANOUT_WARPING_VER;
	warpingData.numVertices = numVertices;
	warpingData.vertexFormat = NV_GPU_WARPING_VERTICE_FORMAT_TRIANGLESTRIP_XYUVRQ;
	warpingData.textureRect = &scanInfo.sourceDesktopRect;
	warpingData.vertices = &vertices[0];

	error = NvAPI_GPU_SetScanoutWarping(displayId, &warpingData, &maxNumVertices, &sticky);
	if (error != NVAPI_OK)
	{
		return error;
	}
#ifdef TRIAL
	int count = (int)srcWidth*(int)srcHeight * 3;
	float* image = new float[count];
	for (int i = 0; i < count; i++) {
		image[i] = 1;
	}

	ShowImage(displayId, image, (int)srcWidth, (int)srcHeight);

	delete[] image;
#endif

	return error;
}

int WarpMultiple(NvU32 displayIds[], int count, float vertices[], int numVertices) {
	NvAPI_Status status;
	int sticky;
	int maxNumVertices;
	NV_SCANOUT_INFORMATION scanInfo;
	NV_SCANOUT_WARPING_DATA warpingData;
	NV_MOSAIC_TOPO_BRIEF topo;
	NV_MOSAIC_DISPLAY_SETTING dispSetting;
	NvS32 overlapX, overlapY;
	float srcLeft, srcTop, srcWidth, srcHeight;

	// Mit dem Warping des 2. Beamers gibt es irgend ein Problem. Entweder muss 2 Mal gewarpt werden,
	// oder es könnte auch zuerst der zweite Beamer gewarpt werden.
	//for(int i = 1;i>=0;i--) {
	for (int i = 0; i < count; i++) {

		maxNumVertices = 0;
		sticky = 0;

		ZeroMemory(&scanInfo, sizeof(NV_SCANOUT_INFORMATION));
		scanInfo.version = NV_SCANOUT_INFORMATION_VER;

		ZeroMemory(&warpingData, sizeof(NV_SCANOUT_WARPING_DATA));
		ZeroMemory(&topo, sizeof(NV_MOSAIC_TOPO_BRIEF));
		ZeroMemory(&dispSetting, sizeof(NV_MOSAIC_DISPLAY_SETTING));

		status = NvAPI_GPU_GetScanoutConfigurationEx(displayIds[i], &scanInfo);
		if (status != NVAPI_OK)
		{
			return status;
		}

		topo.version = NVAPI_MOSAIC_TOPO_BRIEF_VER;

		dispSetting.version = NVAPI_MOSAIC_DISPLAY_SETTING_VER;

		status = NvAPI_Mosaic_GetCurrentTopo(&topo, &dispSetting, &overlapX, &overlapY);
		if (status != NVAPI_OK)
		{
			return status;
		}

		if (topo.enabled == false)
		{
			srcLeft = (float)scanInfo.sourceDesktopRect.sX;
			srcTop = (float)scanInfo.sourceDesktopRect.sY;
			srcWidth = (float)scanInfo.sourceDesktopRect.sWidth;
			srcHeight = (float)scanInfo.sourceDesktopRect.sHeight;
		}
		else
		{
			srcLeft = (float)scanInfo.sourceViewportRect.sX;
			srcTop = (float)scanInfo.sourceViewportRect.sY;
			srcWidth = (float)scanInfo.sourceViewportRect.sWidth;
			srcHeight = (float)scanInfo.sourceViewportRect.sHeight;
		}		

		warpingData.version = NV_SCANOUT_WARPING_VER;
		warpingData.numVertices = numVertices;
		warpingData.vertexFormat = NV_GPU_WARPING_VERTICE_FORMAT_TRIANGLESTRIP_XYUVRQ;
		warpingData.textureRect = &scanInfo.sourceDesktopRect;

		warpingData.vertices = &vertices[i*numVertices*6];

		// 2 Mal warpen, siehe Kommentar oben.
		for (int j = 0; j <= 1; j++) {
			status = NvAPI_GPU_SetScanoutWarping(displayIds[i], &warpingData, &maxNumVertices, &sticky);
			if (status != NVAPI_OK)
			{
				return status;
			}
		}
#ifdef TRIAL
	int count = (int)srcWidth*(int)srcHeight * 3;
	float* image = new float[count];
	for (int i = 0; i < count; i++) {
		image[i] = 1;
	}

	ShowImage(displayId, image, (int)srcWidth, (int)srcHeight);

	delete[] image;
#endif

	}
	return status;
}

int Blend(NvU32 displayId, float blend[], float offset[], int width, int height) {
#ifdef TRIAL
	int x0 = 0, x1 = width - 1;
	int y0 = 0, y1 = height - 1;
	int dx = x1 - x0, sx = x0<x1 ? 1 : -1;
	int dy = -y1, sy = y0 < y1 ? 1 : -1;
	int err = dx + dy, e2;

	while (1) {		
		blend[(x0 + y0 * width) * 3 + 0] = 1;
		blend[(x0 + y0 * width) * 3 + 1] = 0;
		blend[(x0 + y0 * width) * 3 + 2] = 0;

		if (x0 == x1 && y0 == y1) break;
		e2 = 2 * err;
		if (e2 > dy) { err += dy; x0 += sx; }
		if (e2 < dx) { err += dx; y0 += sy; }
	}

	x0 = 0, x1 = width - 1;
	y0 = height - 1, y1 = 0;
	dx = x1 - x0, sx = x0<x1 ? 1 : -1;
	dy = -y0, sy = y0 < y1 ? 1 : -1;
	err = dx + dy, e2;

	while (1) {
		blend[(x0 + y0 * width) * 3 + 0] = 1;
		blend[(x0 + y0 * width) * 3 + 1] = 0;
		blend[(x0 + y0 * width) * 3 + 2] = 0;

		if (x0 == x1 && y0 == y1) break;
		e2 = 2 * err;
		if (e2 > dy) { err += dy; x0 += sx; }
		if (e2 < dx) { err += dx; y0 += sy; }
	}
#endif
	NV_SCANOUT_INTENSITY_DATA intensityData;
	intensityData.version = NV_SCANOUT_INTENSITY_DATA_VER;
	intensityData.width = width;
	intensityData.height = height;
	intensityData.blendingTexture = blend;
	intensityData.offsetTexture = offset;
	intensityData.offsetTexChannels = 1;
	int sticky = 0;
	return NvAPI_GPU_SetScanoutIntensity(displayId, &intensityData, &sticky);
}


int UnWarp(NvU32 displayIds[], int count) {
	NV_SCANOUT_WARPING_DATA warpingData;
	NV_SCANOUT_INFORMATION scanInfo;
	int maxNumVertices = 0;
	int sticky = 0;
	NvAPI_Status status;

	ZeroMemory(&scanInfo, sizeof(NV_SCANOUT_INFORMATION));
	scanInfo.version = NV_SCANOUT_INFORMATION_VER;

	warpingData.version = NV_SCANOUT_WARPING_VER;
	warpingData.vertexFormat = NV_GPU_WARPING_VERTICE_FORMAT_TRIANGLESTRIP_XYUVRQ;
	warpingData.textureRect = &scanInfo.sourceDesktopRect;
	warpingData.vertices = NULL;
	warpingData.numVertices = 0;

	for (int i = 0; i < count; i++)
	{
		status = NvAPI_GPU_SetScanoutWarping(displayIds[i], &warpingData, &maxNumVertices, &sticky);
		if (status != NVAPI_OK)
		{
			return status;
		}
	}
	return NVAPI_OK;
}

int UnBlend(NvU32 displayIds[], int count, int width, int height) {
	int sticky = 0;
	NV_SCANOUT_INTENSITY_DATA intensityData;
	intensityData.version = NV_SCANOUT_INTENSITY_DATA_VER;
	intensityData.blendingTexture = NULL;
	intensityData.width = width;
	intensityData.height = height;
	NvAPI_Status status;

	intensityData.offsetTexture = NULL;
	intensityData.offsetTexChannels = 1;

	for (int i = 0; i < count; i++)
	{
		status = NvAPI_GPU_SetScanoutIntensity(displayIds[i], &intensityData, &sticky);
		if (status != NVAPI_OK)
		{
			return status;
		}
	}
	return NVAPI_OK;
}

int ShowImage(NvU32 displayId, float image[], int width, int height) {
#ifdef TRIAL
	int x0 = 0, x1 = width - 1;
	int y0 = 0, y1 = height - 1;
	int dx = x1 - x0, sx = x0<x1 ? 1 : -1;
	int dy = -y1, sy = y0 < y1 ? 1 : -1;
	int err = dx + dy, e2;

	while (1) {
		image[(x0 + y0 * width) * 3 + 0] = 1;
		image[(x0 + y0 * width) * 3 + 1] = 0;
		image[(x0 + y0 * width) * 3 + 2] = 0;

		if (x0 == x1 && y0 == y1) break;
		e2 = 2 * err;
		if (e2 > dy) { err += dy; x0 += sx; }
		if (e2 < dx) { err += dx; y0 += sy; }
	}

	x0 = 0, x1 = width - 1;
	y0 = height - 1, y1 = 0;
	dx = x1 - x0, sx = x0<x1 ? 1 : -1;
	dy = -y0, sy = y0 < y1 ? 1 : -1;
	err = dx + dy, e2;

	while (1) {
		image[(x0 + y0 * width) * 3 + 0] = 1;
		image[(x0 + y0 * width) * 3 + 1] = 0;
		image[(x0 + y0 * width) * 3 + 2] = 0;

		if (x0 == x1 && y0 == y1) break;
		e2 = 2 * err;
		if (e2 > dy) { err += dy; x0 += sx; }
		if (e2 < dx) { err += dx; y0 += sy; }
	}
#endif
	NV_SCANOUT_INTENSITY_DATA intensityData;
	intensityData.version = NV_SCANOUT_INTENSITY_DATA_VER;
	intensityData.width = width;
	intensityData.height = height;
	intensityData.blendingTexture = image;
	intensityData.offsetTexture = NULL;
	intensityData.offsetTexChannels = 1;
	int sticky = 0;
	return NvAPI_GPU_SetScanoutIntensity(displayId, &intensityData, &sticky);
}
