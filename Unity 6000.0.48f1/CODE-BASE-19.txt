 
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\OpenCL\kernels\atrousFilter.cl---------------


/* Edge-Avoiding A-Trous Wavelet Filter
   see: https://jo.dreggn.org/home/2010_atrous.pdf
*/

#include "commonCL.h"

#if !defined(BLOCK_SIZE)
#define BLOCK_SIZE 8
#endif

// ----------------------------------------------------------------------------------------------------

#define REFLECT(x, max) \
   if (x < 0) x = - x - 1; \
   if (x >= max) x =  2 * max - x - 1;

// ----------------------------------------------------------------------------------------------------

#define FILL_LOCAL_BUFFER(source, srcBufferSize, coord, localBuff, halfWindow, localStart) \
do \
{ \
   int localX = get_local_id(0); \
   int localY = get_local_id(1); \
   int beg = 0, end = 0; \
   int windowSize = (BLOCK_SIZE + halfWindow*2)*(BLOCK_SIZE + halfWindow*2); \
   int block = ceil((float)(windowSize) / (BLOCK_SIZE * BLOCK_SIZE)); \
   if (localX + localY*BLOCK_SIZE <= ceil((float)(windowSize) / block)) \
   { \
      beg = (localX + localY*BLOCK_SIZE)*block; \
      end = beg + block; \
      end = clamp(end, 0, windowSize); \
   } \
   localStart = (int2)((coord.x & (~(BLOCK_SIZE - 1))) - halfWindow, (coord.y & (~(BLOCK_SIZE - 1))) - halfWindow); \
   localStart = clamp(localStart, (int2)0, srcBufferSize - (int2)1); \
   for (int i = beg; i < end; ++i) \
   { \
      int2 xy = (int2)(localStart.x + (i % (BLOCK_SIZE + halfWindow*2)), localStart.y + (i / (BLOCK_SIZE + halfWindow*2))); \
      xy = clamp(xy, 0, srcBufferSize - (int2)1); \
      localBuff[i] = read_imagef(source, kSamplerClampNearestUnormCoords, xy); \
   } \
   barrier(CLK_LOCAL_MEM_FENCE); \
} \
while(0);

// ----------------------------------------------------------------------------------------------------

#define DERIVATE(buffer, bufferSize, coord, halfWindow, localStart, dFdX, dFdY) \
do \
{ \
   int left = clamp(coord.x - 1, 0, bufferSize.x - 1); \
   int right = clamp(coord.x + 1, 0, bufferSize.x - 1); \
   int top = clamp(coord.y - 1, 0, bufferSize.y - 1); \
   int bottom = clamp(coord.y + 1, 0, bufferSize.y - 1); \
   dFdX = (buffer[(left - localStart.x) + (BLOCK_SIZE + halfWindow*2)*(top - localStart.y)]  \
        - buffer[(right - localStart.x) + (BLOCK_SIZE + halfWindow*2)*(top - localStart.y)] \
        + 2 * (buffer[(left - localStart.x) + (BLOCK_SIZE + halfWindow*2)*(coord.y - localStart.y)]  \
               - buffer[(right - localStart.x) + (BLOCK_SIZE + halfWindow*2)*(coord.y - localStart.y)]) \
        + buffer[(left - localStart.x) + (BLOCK_SIZE + halfWindow*2)*(bottom - localStart.y)] \
        - buffer[(right - localStart.x) + (BLOCK_SIZE + halfWindow*2)*(bottom - localStart.y)]); \
   dFdY = (buffer[(left - localStart.x) + (BLOCK_SIZE + halfWindow*2)*(top - localStart.y)]  \
        - buffer[(left - localStart.x) + (BLOCK_SIZE + halfWindow*2)*(bottom - localStart.y)] \
        + 2 * (buffer[(coord.x - localStart.x) + (BLOCK_SIZE + halfWindow*2)*(top - localStart.y)]  \
               - buffer[(coord.x - localStart.x) + (BLOCK_SIZE + halfWindow*2)*(bottom - localStart.y)]) \
        + buffer[(right - localStart.x) + (BLOCK_SIZE + halfWindow*2)*(top - localStart.y)] \
        - buffer[(right - localStart.x) + (BLOCK_SIZE + halfWindow*2)*(bottom - localStart.y)]); \
} \
while(0);


// ----------------------------------------------------------------------------------------------------
// Similarity function
static inline float C(float3 x1, float3 x2, float sigma)
{
   float3 distance = x1 - x2;
   float a = fast_length(convert_float3(distance)) / sigma;
   return native_exp(-a);
}

// ----------------------------------------------------------------------------------------------------
// Depth similarity function
static inline float dW(float x1, float x2, float sigma)
{
   float a = fabs(x1 - x2) / sigma;
   return native_exp(-a);
}

// ----------------------------------------------------------------------------------------------------
// Normals similarity function
static inline float nW(float3 x1, float3 x2, float sigma)
{
   x1 = normalize(x1 + make_float3(0.01f, 0.01f, 0.01f));
   x2 = normalize(x2 + make_float3(0.01f, 0.01f, 0.01f));
   float a = fmax((float)0.0f, dot(x1, x2));

   return pow(a, (float)1.0f / sigma);
}

// ----------------------------------------------------------------------------------------------------
static inline float4 SampleGauss3x3F(__read_only image2d_t buffer, int2 buffer_size, int2 coord, __local float4* window)
{
   int2 tl = clamp(coord - (int2)1, (int2)0, buffer_size - (int2)1);
   int2 br = clamp(coord + (int2)1, (int2)0, buffer_size - (int2)1);
   int2 localStart;

   FILL_LOCAL_BUFFER(buffer, buffer_size, coord, window, 1, localStart);

   float4 bluredVal = 0.077847f * (
      window[(tl.x - localStart.x) + (BLOCK_SIZE + 2)*(tl.y - localStart.y)]
      + window[(br.x - localStart.x) + (BLOCK_SIZE + 2)*(tl.y - localStart.y)]
      + window[(tl.x - localStart.x) + (BLOCK_SIZE + 2)*(br.y - localStart.y)]
      + window[(br.x - localStart.x) + (BLOCK_SIZE + 2)*(br.y - localStart.y)] );

   bluredVal += 0.123317f * (
      window[(coord.x - localStart.x) + (BLOCK_SIZE + 2)*(tl.y - localStart.y)]
      + window[(br.x - localStart.x) + (BLOCK_SIZE + 2)*(coord.y - localStart.y)]
      + window[(tl.x - localStart.x) + (BLOCK_SIZE + 2)*(coord.y - localStart.y)]
      + window[(coord.x - localStart.x) + (BLOCK_SIZE + 2)*(br.y - localStart.y)] );

   bluredVal += 0.195346f * window[(coord.x - localStart.x) + (BLOCK_SIZE + 2)*(coord.y - localStart.y)];
   return bluredVal;
}

// ----------------------------------------------------------------------------------------------------
__kernel void ATrousKernel(
    __read_only image2d_t dynarg_srcTile0, // Source buffer
    __read_only image2d_t dynarg_srcTile1, // Normals/ChartId buffer
    __read_only image2d_t dynarg_srcTile2, // Prev variance buffer
   __write_only image2d_t dynarg_dstTile , // Dest buffer
   __write_only image2d_t dynarg_dstTile1, // Dest variance buffer
    INPUT_VALUE( 5, int2  ,   imageSize),
    INPUT_VALUE( 6, float4,   sigma ),
    INPUT_VALUE( 7, int   ,   coordOffset )
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
   int2 coord = (int2)(get_global_id(0), get_global_id(1));

   float2 gradDpt = 0.0f;
   __local float4 window[(BLOCK_SIZE + 2)*(BLOCK_SIZE + 2)];
   float4 dFdX, dFdY;
   int2 localStart;
   FILL_LOCAL_BUFFER(dynarg_srcTile1, imageSize, coord, window, 1, localStart);
   DERIVATE(window, imageSize, coord, 1, localStart, dFdX, dFdY);
   barrier(CLK_LOCAL_MEM_FENCE);

   gradDpt.x = dFdX.w;
   gradDpt.y = dFdY.w;

   // color variance value
#ifdef FIRST_PASS
   float colVar = fast_length(convert_float3(SampleGauss3x3F(dynarg_srcTile0, imageSize, coord, window).xyz));
#else
   float colVar = READ_IMAGEF_SAFE(dynarg_srcTile2, kSamplerClampNearestUnormCoords, coord).x;
#endif

   //B3 spline
   const float kernl[] =
   {
      1.0f / 256, 1.0f / 64, 3.0f / 128, 1.0f / 64, 1.0f / 256,
      1.0f / 64, 1.0f / 16, 3.0f / 32, 1.0f / 16, 1.0f / 64,
      3.0f / 128, 3.0f / 32, 9.0f / 64, 3.0f / 32, 3.0f / 128,
      1.0f / 64, 1.0f / 16, 3.0f / 32, 1.0f / 16, 1.0f / 64,
      1.0f / 256, 1.0f / 64, 3.0f / 128, 1.0f / 64, 1.0f / 256
   };

   // color value at the center of the window
   float4 temp = READ_IMAGEF_SAFE(dynarg_srcTile0, kSamplerClampNearestUnormCoords, coord);
   float3 qcol = temp.xyz;
   const float srcAlpha = temp.w;

   // normal/depth value at the center of the window
   temp = READ_IMAGEF_SAFE(dynarg_srcTile1, kSamplerClampNearestUnormCoords, coord);
   float3 qnorm = temp.xyz;
   float qdpt   = temp.w;

   float4 out = 0.0f;
   float sum  = 0.0f;
   float vsum = 0.0f;

   const float colSigma   = sigma.x;
   const float normSigma  = sigma.y;
   const float depthSigma = sigma.z;

   for (int i = -2; i <= 2; ++i)
      for (int j = -2; j <= 2; ++j)
      {
         int2 offsetUV;
         offsetUV.x = coord.x + i * coordOffset;
         offsetUV.y = coord.y + j * coordOffset;

         REFLECT(offsetUV.x, imageSize.x)
         REFLECT(offsetUV.y, imageSize.y)
         offsetUV.x = clamp(offsetUV.x, (int)0, (int)imageSize.x-1);
         offsetUV.y = clamp(offsetUV.y, (int)0, (int)imageSize.y-1);

         float coeff = kernl[i + 2 + (j + 2) * 5];

         float4 temp;
         float3 c;
         float multiplier;

         temp = READ_IMAGEF_SAFE(dynarg_srcTile0, kSamplerClampNearestUnormCoords, offsetUV);
         c = temp.xyz;

         multiplier = C(c, qcol, colSigma * sqrt(colVar) + 1.0e-5f);
         coeff *= multiplier > 0.0f ? multiplier : 0.0f;

         // Normal edge stopping
         temp = READ_IMAGEF_SAFE(dynarg_srcTile1, kSamplerClampNearestUnormCoords, offsetUV);
         multiplier = nW(temp.xyz, qnorm, normSigma);
         coeff *= multiplier > 0.0f ? multiplier : 0.0f;

         // Depth edge stopping
         multiplier = dW(temp.w, qdpt, depthSigma * fabs(dot(gradDpt, make_float2(i * coordOffset, j * coordOffset))) + 1.0e-3f);
         coeff *= multiplier > 0.0f ? multiplier : 0.0f;

         //temp = ReadPixelTyped(transBuff, cx, cy);
         //multiplier = C(temp.xyz / temp.w, qtrans, transSigma);
         //coeff *= multiplier > 0.0f ? multiplier : 0.0f;

         out.xyz += c * coeff;

#ifdef FIRST_PASS
         vsum += fast_length(c) * coeff * coeff;
#else
         vsum += READ_IMAGEF_SAFE(dynarg_srcTile2, kSamplerClampNearestUnormCoords, offsetUV).x * coeff * coeff;
#endif
         sum += coeff;
      }

   out.w   = srcAlpha;
   out.xyz = sum > make_float3(0.0f, 0.0f, 0.0f) ? out.xyz / sum : make_float3(0.0f, 0.0f, 0.0f);

#if !defined(LAST_PASS)
   vsum /= sum * sum;

   //Back prop variance
   WRITE_IMAGEF_SAFE(dynarg_dstTile1, coord, make_float4(vsum,vsum,vsum,vsum));
#endif

   WRITE_IMAGEF_SAFE(dynarg_dstTile, coord, out);
}


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\OpenCL\kernels\atrousFilter.cl---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\OpenCL\kernels\compositeLightmaps.cl---------------


#include "commonCL.h"
#include "colorSpace.h"
#include "rgbmEncoding.h"

__constant float4 kZero = (float4)(0.0f, 0.0f, 0.0f, 0.0f);
__constant float4 kOne = (float4)(1.0f, 1.0f, 1.0f, 1.0f);
__constant float4 kHalf = (float4)(0.5, 0.5f, 0.5f, 0.5f);

// This function works in both non-tiled baking mode or tiled baking mode
//
// In case of non-tiled baking contains the lightmap size
// In case of tiled baking contains a tile size, both TilingHelper and RRBakeLightmapTechnique use the same tile size
//
// In case of non-tiled baking tileOffset contains a threadId to lightmapSpace coords offset
// In case of tiled baking, tileOffset contains an offset from TilingHelper (compositing tiles) to RRBakeLightmapTechnique tiles (baked tiles)
//   Indeed, compositing may have move the tile from TilingHelper to stay inside the lightmap buffers,
//   resulting in an offset
static uint ConvertThreadCoordsToBakingTileCoords(int2 tileThreadId, int2 tileOffset, int lightmapOrBakingTileSize)
{
    // First apply offset
    int2 coordInBakingTile = clamp(tileThreadId, (int2)(0, 0), (int2)(lightmapOrBakingTileSize-1, lightmapOrBakingTileSize-1)) + tileOffset;
    // Then clamp inside the baking tile or lightmap
    coordInBakingTile = clamp(coordInBakingTile, (int2)(0, 0), (int2)(lightmapOrBakingTileSize-1, lightmapOrBakingTileSize-1));

    // Finally, return a 1D index in baking tile or lightmap
    return coordInBakingTile.y * lightmapOrBakingTileSize + coordInBakingTile.x;
}

// ----------------------------------------------------------------------------------------------------
__kernel void compositingBlit(
    __write_only image2d_t   dynarg_dstImage,
    __read_only image2d_t    dynarg_srcImage
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    // Image coordinates
    int2 coords = (int2)(get_global_id(0), get_global_id(1));

    float4 srcColor = READ_IMAGEF_SAFE(dynarg_srcImage, kSamplerClampNearestUnormCoords, coords);
    WRITE_IMAGEF_SAFE(dynarg_dstImage, coords, srcColor);
}

// ----------------------------------------------------------------------------------------------------
__kernel void compositingMarkupInvalidTexels(
    __write_only image2d_t      dynarg_dstTile,
    __read_only image2d_t       dynarg_srcTile,
    INPUT_BUFFER(2,  int,        indirectSampleCountBuffer),
    INPUT_BUFFER(3,  float,      outputValidityBuffer),
    INPUT_BUFFER(4,  unsigned char, occupancyBuffer),
    INPUT_VALUE( 5,  float,      backfaceTolerance),
    INPUT_VALUE( 6,  int2,    compositingTileToBakingTileOffset),
    INPUT_VALUE( 7,  int,     tileSize),
    INPUT_VALUE( 8,  int2,    compositingTileOffset),
    INPUT_VALUE( 9,  int,     lightmapSize),
    INPUT_VALUE( 10, int,     inputIsTiled)
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    // Coordinates in tile space
    int2 tileThreadId = (int2)(get_global_id(0), get_global_id(1));

    const uint index = ConvertThreadCoordsToBakingTileCoords(tileThreadId, compositingTileToBakingTileOffset, tileSize);

    const int sampleCount = INDEX_SAFE(indirectSampleCountBuffer, index);

    const float validityValue    = INDEX_SAFE(outputValidityBuffer, index);
    float4 value                 = READ_IMAGEF_SAFE(dynarg_srcTile, kSamplerClampNearestUnormCoords, tileThreadId);

    const bool backfaceInvalid = sampleCount <= 0 ? false : ((validityValue / sampleCount) > (1.f - backfaceTolerance));
    if (backfaceInvalid)
    {
        value.w = 0.0f;
    }
    else
    {
        const int occupiedSamplesWithinTexel = INDEX_SAFE(occupancyBuffer, index);
        value.w = ((occupiedSamplesWithinTexel > 0) ? 1.f : 0.f);
    }
    WRITE_IMAGEF_SAFE(dynarg_dstTile, tileThreadId, value);
}

// ----------------------------------------------------------------------------------------------------
__kernel void compositingDirect(
    __write_only image2d_t  dynarg_dstTile,
    INPUT_BUFFER(1, float4,  dynarg_directLighting),
    INPUT_BUFFER(2, int,     directSampleCountBuffer),
    INPUT_VALUE( 3, int2,    compositingTileToBakingTileOffset),
    INPUT_VALUE( 4, int,     tileSize),
    INPUT_VALUE( 5, int2,    compositingTileOffset),
    INPUT_VALUE( 6, int,     lightmapSize),
    INPUT_VALUE( 7, int,     inputIsTiled)
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    // Coordinates in tile space
    int2 tileThreadId = (int2)(get_global_id(0), get_global_id(1));

    const uint indexSPP = ConvertThreadCoordsToBakingTileCoords(tileThreadId, compositingTileToBakingTileOffset, tileSize);

    const int currentDirectSampleCount = INDEX_SAFE(directSampleCountBuffer, indexSPP);
    if (currentDirectSampleCount <= 0)
        return;

    uint indexLighting = 0;
    if(inputIsTiled)
        indexLighting = ConvertThreadCoordsToBakingTileCoords(tileThreadId, compositingTileToBakingTileOffset, tileSize);
    else
        indexLighting = ConvertThreadCoordsToBakingTileCoords(tileThreadId, compositingTileOffset, lightmapSize);

    const float3 lightingValue = INDEX_SAFE(dynarg_directLighting, indexLighting).xyz;

    float4 result;
    result.xyz = lightingValue / currentDirectSampleCount;
    result.w = 1.f;

    WRITE_IMAGEF_SAFE(dynarg_dstTile, tileThreadId, result);
}

// ----------------------------------------------------------------------------------------------------
__kernel void compositingShadowMask(
    __write_only image2d_t      dynarg_dstTile,
    INPUT_BUFFER(1, int,        directSampleCountBuffer),
    INPUT_BUFFER(2, float4,     dynarg_shadowmask),
    INPUT_VALUE( 3, int2,    compositingTileToBakingTileOffset),
    INPUT_VALUE( 4, int,     tileSize),
    INPUT_VALUE( 5, int2,    compositingTileOffset),
    INPUT_VALUE( 6, int,     lightmapSize),
    INPUT_VALUE( 7, int,    inputIsTiled)
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    // Coordinates in tile space
    int2 tileThreadId = (int2)(get_global_id(0), get_global_id(1));

    const uint indexSPP = ConvertThreadCoordsToBakingTileCoords(tileThreadId, compositingTileToBakingTileOffset, tileSize);

    const int currentDirectSampleCount = INDEX_SAFE(directSampleCountBuffer, indexSPP);
    if (currentDirectSampleCount <= 0)
        return;

    uint indexLighting = 0;
    if(inputIsTiled)
        indexLighting = ConvertThreadCoordsToBakingTileCoords(tileThreadId, compositingTileToBakingTileOffset, tileSize);
    else
        indexLighting = ConvertThreadCoordsToBakingTileCoords(tileThreadId, compositingTileOffset, lightmapSize);

    KERNEL_ASSERT(currentDirectSampleCount > 0);
    const float4 shadowMaskValue = INDEX_SAFE(dynarg_shadowmask, indexLighting);
    float4 result = shadowMaskValue / currentDirectSampleCount;
    result = saturate4(result);

    WRITE_IMAGEF_SAFE(dynarg_dstTile, tileThreadId, result);
}

// ----------------------------------------------------------------------------------------------------
__kernel void compositingIndirect(
    __write_only image2d_t  dynarg_dstTile,
    INPUT_BUFFER(1, float4, dynarg_indirectLighting),
    INPUT_BUFFER(2, int,    indirectSampleCountBuffer),
    INPUT_BUFFER(3, float4, dynarg_environmentLighting),
    INPUT_BUFFER(4, int,    environmentSampleCountBuffer),
    INPUT_VALUE( 5, float,  indirectIntensity),
    INPUT_VALUE( 6, int2,   compositingTileToBakingTileOffset),
    INPUT_VALUE( 7, int,    tileSize),
    INPUT_VALUE( 8, int2,   compositingTileOffset),
    INPUT_VALUE( 9, int,    lightmapSize),
    INPUT_VALUE( 10, int,   indirectLightingIsTiled),
    INPUT_VALUE( 11, int,   environmentLightingIsTiled)
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    // Coordinates in tile space
    int2 tileThreadId = (int2)(get_global_id(0), get_global_id(1));

    uint indexIndLighting;
    uint indexEnvLighting;

    const uint indexSPP = ConvertThreadCoordsToBakingTileCoords(tileThreadId, compositingTileToBakingTileOffset, tileSize);

    const int sampleCount = INDEX_SAFE(indirectSampleCountBuffer, indexSPP);
    if (sampleCount == 0)
        return;

    const int envSampleCount = INDEX_SAFE(environmentSampleCountBuffer, indexSPP);
    if (envSampleCount == 0)
        return;

    if(indirectLightingIsTiled)
        indexIndLighting = ConvertThreadCoordsToBakingTileCoords(tileThreadId, compositingTileToBakingTileOffset, tileSize);
    else
        indexIndLighting = ConvertThreadCoordsToBakingTileCoords(tileThreadId, compositingTileOffset, lightmapSize);

    if(environmentLightingIsTiled)
        indexEnvLighting = ConvertThreadCoordsToBakingTileCoords(tileThreadId, compositingTileToBakingTileOffset, tileSize);
    else
        indexEnvLighting = ConvertThreadCoordsToBakingTileCoords(tileThreadId, compositingTileOffset, lightmapSize);

    KERNEL_ASSERT(sampleCount > 0);
    KERNEL_ASSERT(envSampleCount > 0);
    float4 indirectLightValue = INDEX_SAFE(dynarg_indirectLighting, indexIndLighting);
    float4 environmentValue   = INDEX_SAFE(dynarg_environmentLighting, indexEnvLighting);

    float4 result = indirectIntensity * (indirectLightValue / sampleCount + environmentValue / envSampleCount);
    result.w = 1.0f;

    WRITE_IMAGEF_SAFE(dynarg_dstTile, tileThreadId, result);
}

// ----------------------------------------------------------------------------------------------------
__kernel void compositingAO(
    __write_only image2d_t      dynarg_dstTile,
    INPUT_BUFFER(1, float,      dynarg_ao),
    INPUT_BUFFER(2, int,        indirectSampleCountBuffer),
    INPUT_VALUE( 3, float,      aoExponent),
    INPUT_VALUE( 4, int2,    compositingTileToBakingTileOffset),
    INPUT_VALUE( 5, int,     tileSize),
    INPUT_VALUE( 6, int2,    compositingTileOffset),
    INPUT_VALUE( 7, int,     lightmapSize),
    INPUT_VALUE( 8, int,    inputIsTiled)
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    // Coordinates in tile space
    int2 tileThreadId = (int2)(get_global_id(0), get_global_id(1));

    const uint indexSPP = ConvertThreadCoordsToBakingTileCoords(tileThreadId, compositingTileToBakingTileOffset, tileSize);
    const int sampleCount = INDEX_SAFE(indirectSampleCountBuffer, indexSPP);

    if (sampleCount == 0)
        return;

    uint indexLighting = 0;
    if(inputIsTiled)
        indexLighting = ConvertThreadCoordsToBakingTileCoords(tileThreadId, compositingTileToBakingTileOffset, tileSize);
    else
        indexLighting = ConvertThreadCoordsToBakingTileCoords(tileThreadId, compositingTileOffset, lightmapSize);

    float aoValue = INDEX_SAFE(dynarg_ao, indexLighting);
    KERNEL_ASSERT(sampleCount > 0);
    aoValue = aoValue / (float)sampleCount;

    aoValue = pow(aoValue, aoExponent);

    float4 result = (float4)(aoValue, aoValue, aoValue, 1.0f);

    WRITE_IMAGEF_SAFE(dynarg_dstTile, tileThreadId, result);
}

// ----------------------------------------------------------------------------------------------------
__kernel void compositingAddLighting(
    __write_only image2d_t      dynarg_dstTile,
    __read_only image2d_t       dynarg_srcTile0,    // directLightingImage
    __read_only image2d_t       dynarg_srcTile1     // indirectLightingImage
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    // Coordinates in tile space
    int2 tileThreadId = (int2)(get_global_id(0), get_global_id(1));

    float4 directLightingValue = READ_IMAGEF_SAFE(dynarg_srcTile0, kSamplerClampNearestUnormCoords, tileThreadId);
    float4 indirectLightingValue = READ_IMAGEF_SAFE(dynarg_srcTile1, kSamplerClampNearestUnormCoords, tileThreadId);

    float4 result = directLightingValue + indirectLightingValue;
    result.w = saturate1(directLightingValue.w * indirectLightingValue.w);

    WRITE_IMAGEF_SAFE(dynarg_dstTile, tileThreadId, result);
}

// ----------------------------------------------------------------------------------------------------
__kernel void compositingAddLightingIndirectOnly(
    __write_only image2d_t      dynarg_dstTile,
    __read_only image2d_t       dynarg_srcTile0     // indirectLightingImage
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    // Coordinates in tile space
    int2 tileThreadId = (int2)(get_global_id(0), get_global_id(1));

    float4 result = READ_IMAGEF_SAFE(dynarg_srcTile0, kSamplerClampNearestUnormCoords, tileThreadId);
    result.w = saturate1(result.w);

    WRITE_IMAGEF_SAFE(dynarg_dstTile, tileThreadId, result);
}

// ----------------------------------------------------------------------------------------------------
__kernel void compositingDilate(
    __write_only image2d_t          dynarg_dstTile,
    __read_only image2d_t           dynarg_srcTile,
    INPUT_BUFFER(2, unsigned char,  occupancyBuffer),
    INPUT_VALUE( 3, int,            useOccupancy),
    INPUT_VALUE( 4, int2,           compositingTileToBakingTileOffset),
    INPUT_VALUE( 5, int,            tileSize),
    INPUT_VALUE( 6, int2,           compositingTileOffset),
    INPUT_VALUE( 7, int,            lightmapSize),
    INPUT_VALUE( 8, int,            inputIsTiled)
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    // Coordinates in tile space
    int2 tileThreadId = (int2)(get_global_id(0), get_global_id(1));

    float4 inputValue = READ_IMAGEF_SAFE(dynarg_srcTile, kSamplerClampNearestUnormCoords, tileThreadId);

    // The texel is valid -> just write it to the output
    if (inputValue.w > 0)
    {
        WRITE_IMAGEF_SAFE(dynarg_dstTile, tileThreadId, inputValue);
        return;
    }

    if (useOccupancy) // Internal dilation
    {
        uint index;
        if(inputIsTiled)
            index = ConvertThreadCoordsToBakingTileCoords(tileThreadId, compositingTileToBakingTileOffset, tileSize);
        else
            index = ConvertThreadCoordsToBakingTileCoords(tileThreadId, compositingTileOffset, lightmapSize);

        const int occupiedSamplesWithinTexel = INDEX_SAFE(occupancyBuffer, index);

        // A non-occupied texel, just copy when doing internal dilation.
        if (occupiedSamplesWithinTexel == 0)
        {
            WRITE_IMAGEF_SAFE(dynarg_dstTile, tileThreadId, inputValue);
            return;
        }
    }

    float4 dilated = kZero;
    float weightCount = 0.0f;

    // Note: not using READ_IMAGEF_SAFE below as those samples are expected to read just outside of the tile boundary, they will get safely clamped though.

    // Upper row
    float4 value0 = read_imagef(dynarg_srcTile, kSamplerClampNearestUnormCoords, tileThreadId + (int2)(-1, -1));
    float4 value1 = read_imagef(dynarg_srcTile, kSamplerClampNearestUnormCoords, tileThreadId + (int2)(0, -1));
    float4 value2 = read_imagef(dynarg_srcTile, kSamplerClampNearestUnormCoords, tileThreadId + (int2)(1, -1));

    // Side values
    float4 value3 = read_imagef(dynarg_srcTile, kSamplerClampNearestUnormCoords, tileThreadId + (int2)(-1, 0));
    float4 value4 = read_imagef(dynarg_srcTile, kSamplerClampNearestUnormCoords, tileThreadId + (int2)(1, 0));

    // Bottom row
    float4 value5 = read_imagef(dynarg_srcTile, kSamplerClampNearestUnormCoords, tileThreadId + (int2)(-1, 1));
    float4 value6 = read_imagef(dynarg_srcTile, kSamplerClampNearestUnormCoords, tileThreadId + (int2)(0, 1));
    float4 value7 = read_imagef(dynarg_srcTile, kSamplerClampNearestUnormCoords, tileThreadId + (int2)(1, 1));

    dilated = value0.w * value0;
    dilated += value1.w * value1;
    dilated += value2.w * value2;
    dilated += value3.w * value3;
    dilated += value4.w * value4;
    dilated += value5.w * value5;
    dilated += value6.w * value6;
    dilated += value7.w * value7;

    weightCount = value0.w;
    weightCount += value1.w;
    weightCount += value2.w;
    weightCount += value3.w;
    weightCount += value4.w;
    weightCount += value5.w;
    weightCount += value6.w;
    weightCount += value7.w;

    dilated *= 1.0f / max(1.0f, weightCount);

    dilated.w = saturate1(weightCount);

    WRITE_IMAGEF_SAFE(dynarg_dstTile, tileThreadId, dilated);
}

// ----------------------------------------------------------------------------------------------------
__kernel void compositingNormalizeDirectionality(
    __write_only image2d_t               dynarg_dstTile,
    INPUT_BUFFER(1, float4,              dynarg_directionalityBuffer),
    INPUT_BUFFER(2, PackedNormalOctQuad, interpNormalsWSBuffer),
    INPUT_VALUE( 3,  int2,    compositingTileToBakingTileOffset),
    INPUT_VALUE( 4,  int,     tileSize),
    INPUT_VALUE( 5,  int2,    compositingTileOffset),
    INPUT_VALUE( 6,  int,     lightmapSize),
    INPUT_VALUE( 7, int,      inputIsTiled),
    INPUT_VALUE( 8, int,      superSamplingMultiplier)
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    int2 tileThreadId = (int2)(get_global_id(0), get_global_id(1));

    int index = ConvertThreadCoordsToBakingTileCoords(tileThreadId, compositingTileToBakingTileOffset, tileSize);

    float4 dir = INDEX_SAFE(dynarg_directionalityBuffer, index);
    dir = dir / max(0.001f, dir.w);

    float3 normalWS = CalculateSuperSampledInterpolatedNormal(index, superSamplingMultiplier, interpNormalsWSBuffer KERNEL_VALIDATOR_BUFFERS);

    // Compute rebalancing coefficients
    dir.w = dot(normalWS.xyz, dir.xyz);

    dir = dir * kHalf + kHalf;

    WRITE_IMAGEF_SAFE(dynarg_dstTile, tileThreadId, dir);
}

// ----------------------------------------------------------------------------------------------------
__kernel void compositingDecodeNormalsWS(
    __write_only image2d_t               dynarg_dstTile,
    INPUT_BUFFER(1, PackedNormalOctQuad, interpNormalsWSBuffer),
    INPUT_BUFFER(2, int,                 chartIndexBuffer),
    INPUT_VALUE( 3, int2,           compositingTileToBakingTileOffset),
    INPUT_VALUE( 4, int,            tileSize),
    INPUT_VALUE( 5, int2,           compositingTileOffset),
    INPUT_VALUE( 6, int,            lightmapSize),
    INPUT_VALUE( 7, int,            inputIsTiled),
    INPUT_VALUE( 8, int,            superSamplingMultiplier)
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    int2 tileThreadId = (int2)(get_global_id(0), get_global_id(1));

    int index = ConvertThreadCoordsToBakingTileCoords(tileThreadId, compositingTileToBakingTileOffset, tileSize);

    int centerChartId = INDEX_SAFE(chartIndexBuffer, index);

    float4 dir;

    dir.xyz = CalculateSuperSampledInterpolatedNormal(index, superSamplingMultiplier, interpNormalsWSBuffer KERNEL_VALIDATOR_BUFFERS);
    dir.w   = (float)centerChartId;

    WRITE_IMAGEF_SAFE(dynarg_dstTile, tileThreadId, dir);
}

// ----------------------------------------------------------------------------------------------------
__kernel void compositingCombineDirectionality(
    __write_only image2d_t      dynarg_dstTile,
    __read_only image2d_t       dynarg_srcTile0, // directLightingImage
    __read_only image2d_t       dynarg_srcTile1, // indirectLightingImage
    __read_only image2d_t       dynarg_srcTile2, // directionalityFromDirectImage
    __read_only image2d_t       dynarg_srcTile3, // directionalityFromIndirectImage
    INPUT_VALUE(5, float,       indirectScale)
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    int2 tileThreadId = (int2)(get_global_id(0), get_global_id(1));

    float4 directLighting               = READ_IMAGEF_SAFE(dynarg_srcTile0, kSamplerClampNearestUnormCoords, tileThreadId);
    float4 indirectLighting             = READ_IMAGEF_SAFE(dynarg_srcTile1, kSamplerClampNearestUnormCoords, tileThreadId);
    float4 directionalityFromDirect     = READ_IMAGEF_SAFE(dynarg_srcTile2, kSamplerClampNearestUnormCoords, tileThreadId);
    float4 directionalityFromIndirect   = READ_IMAGEF_SAFE(dynarg_srcTile3, kSamplerClampNearestUnormCoords, tileThreadId);

    float directWeight      = Luminance(directLighting.xyz) * length(directionalityFromDirect.xyz);
    float indirectWeight    = Luminance(indirectLighting.xyz) * length(directionalityFromIndirect.xyz) * indirectScale;

    float normalizationWeight = directWeight + indirectWeight;

    directWeight = directWeight / max(0.0001f, normalizationWeight);

    float4 output = select(directionalityFromDirect, lerp4(directionalityFromIndirect, directionalityFromDirect, (float4)directWeight), (int4)(-(indirectLighting.w > 0.0f)));

    WRITE_IMAGEF_SAFE(dynarg_dstTile, tileThreadId, output);
}

// ----------------------------------------------------------------------------------------------------
__kernel void compositingCombineDirectionalityIndirectOnly(
    __write_only image2d_t      dynarg_dstTile,
    __read_only image2d_t       dynarg_srcTile0, // indirectLightingImage
    __read_only image2d_t       dynarg_srcTile1  // directionalityFromIndirectImage
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    int2 tileThreadId = (int2)(get_global_id(0), get_global_id(1));

    float4 indirectLighting             = READ_IMAGEF_SAFE(dynarg_srcTile0, kSamplerClampNearestUnormCoords, tileThreadId);
    float4 directionalityFromIndirect   = READ_IMAGEF_SAFE(dynarg_srcTile1, kSamplerClampNearestUnormCoords, tileThreadId);
    float4 output = select(kZero, directionalityFromIndirect, (int4)(-(indirectLighting.w > 0.0f)));

    WRITE_IMAGEF_SAFE(dynarg_dstTile, tileThreadId, output);
}

// ----------------------------------------------------------------------------------------------------
__kernel void compositingSplitRGBA(
    __write_only image2d_t      dynarg_dstTile0,    // outRGBImage
    __write_only image2d_t      dynarg_dstTile1,    // outAlphaImage
    __read_only image2d_t       dynarg_srcTile0,    // directionalLightmap
    __read_only image2d_t       dynarg_srcTile1     // lightmap
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    int2 tileCoordinates = (int2)(get_global_id(0), get_global_id(1));

    float4 directionalValue = READ_IMAGEF_SAFE(dynarg_srcTile0, kSamplerClampNearestUnormCoords, tileCoordinates);
    float4 lightmapValue    = READ_IMAGEF_SAFE(dynarg_srcTile1, kSamplerClampNearestUnormCoords, tileCoordinates);

    float4 rgbValue         = (float4)(directionalValue.xyz, lightmapValue.w);
    float4 alphaValue       = (float4)(directionalValue.www, lightmapValue.w);

    WRITE_IMAGEF_SAFE(dynarg_dstTile0, tileCoordinates, rgbValue);
    WRITE_IMAGEF_SAFE(dynarg_dstTile1, tileCoordinates, alphaValue);
}

// ----------------------------------------------------------------------------------------------------
__kernel void compositingMergeRGBA(
    __write_only image2d_t      dynarg_dstTile,
    __read_only image2d_t       dynarg_srcTile0,    // dirRGBDilatedImage
    __read_only image2d_t       dynarg_srcTile1,    // dirAlphaDilatedImage
    INPUT_VALUE(3, uint,        tileBorderWidth)
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    // Coordinates in tile space
    int2 tileCoords = (int2)(get_global_id(0), get_global_id(1));

    // Discard tile border(output is smaller than the input)
    int2 sampleCoords = (int2)(tileCoords.x + tileBorderWidth, tileCoords.y + tileBorderWidth);

    float4 dirRGBValue      = READ_IMAGEF_SAFE(dynarg_srcTile0, kSamplerClampNearestUnormCoords, sampleCoords);
    float4 dirAlphaValue    = READ_IMAGEF_SAFE(dynarg_srcTile1, kSamplerClampNearestUnormCoords, sampleCoords);

    float4 result = (float4)(dirRGBValue.xyz, dirAlphaValue.x);

    WRITE_IMAGEF_SAFE(dynarg_dstTile, tileCoords, result);
}

// ----------------------------------------------------------------------------------------------------
__kernel void compositingNormalizeWithSampleCount(
    OUTPUT_BUFFER(0, float4, outputDirectLightingBuffer),   // dest buffer
    INPUT_BUFFER( 1, float4, outputIndirectLightingBuffer), // source buffer (named like this because of kernel asserts to work, but can be used not only for indirect)
    INPUT_BUFFER( 2, int,    indirectSampleCountBuffer),    // buffer with sample count
    INPUT_VALUE(  3, int2,   imageSize)
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    uint index = get_global_id(0) + get_global_id(1) * imageSize.y;

    const int sampleCount = INDEX_SAFE(indirectSampleCountBuffer, index);
    const float norm = sampleCount > 0 ? 1.0 / (float)sampleCount : 1.0;

    float4 color = INDEX_SAFE(outputIndirectLightingBuffer,index);

    INDEX_SAFE(outputDirectLightingBuffer, index) = color * norm;
}

// ----------------------------------------------------------------------------------------------------
__kernel void compositingNormalizeWithSampleCountAO(
    OUTPUT_BUFFER(0, float,  outputDirectLightingBuffer),
    INPUT_BUFFER( 1, float,  outputAoBuffer),
    INPUT_BUFFER( 2, int,    indirectSampleCountBuffer),
    INPUT_VALUE(  3, int2,   imageSize)
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    uint index = get_global_id(0) + get_global_id(1) * imageSize.y;

    const int sampleCount = INDEX_SAFE(indirectSampleCountBuffer, index);
    const float norm = sampleCount > 0 ? 1.0 / (float)sampleCount : 1.0;

    float color = INDEX_SAFE(outputAoBuffer,index);

    INDEX_SAFE(outputDirectLightingBuffer, index) = color * norm;
}

// ----------------------------------------------------------------------------------------------------
__kernel void compositingMultiplyWithSampleCount(
    OUTPUT_BUFFER(0, float4, outputDirectLightingBuffer),
    INPUT_BUFFER( 1, float4, outputIndirectLightingBuffer),
    INPUT_BUFFER( 2, int,    indirectSampleCountBuffer),
    INPUT_VALUE(  3, int2,   imageSize)
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    uint index = get_global_id(0) + get_global_id(1) * imageSize.y;

    const int sampleCount = INDEX_SAFE(indirectSampleCountBuffer, index);
    float4 color = INDEX_SAFE(outputIndirectLightingBuffer,index);

    INDEX_SAFE(outputDirectLightingBuffer, index) = color * (float)sampleCount;
}

// ----------------------------------------------------------------------------------------------------
__kernel void compositingMultiplyWithSampleCountAO(
    OUTPUT_BUFFER(0, float,  outputIndirectLightingBuffer),
    INPUT_BUFFER( 1, float,  outputAoBuffer),
    INPUT_BUFFER( 2, int,    indirectSampleCountBuffer),
    INPUT_VALUE(  3, int2,   imageSize)
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    uint index = get_global_id(0) + get_global_id(1) * imageSize.y;

    const int sampleCount = INDEX_SAFE(indirectSampleCountBuffer, index);
    float color = INDEX_SAFE(outputAoBuffer,index);

    INDEX_SAFE(outputIndirectLightingBuffer, index) = color * (float)sampleCount;
}

// ----------------------------------------------------------------------------------------------------
__kernel void compositingCopyAlphaFromRGBA(
    OUTPUT_BUFFER(0, float,   outputAoBuffer),
    INPUT_BUFFER( 1, float4,  outputIndirectLightingBuffer),
    INPUT_VALUE(  2, int2,    imageSize)
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    uint index = get_global_id(0) + get_global_id(1) * imageSize.y;

    float4 color = INDEX_SAFE(outputIndirectLightingBuffer,index);

    INDEX_SAFE(outputAoBuffer, index) = color.w;
}

// ----------------------------------------------------------------------------------------------------
__kernel void compositingCopyAlphaToRGBA(
    OUTPUT_BUFFER(0, float4,  outputIndirectLightingBuffer),
    INPUT_BUFFER( 1, float,  outputAoBuffer),
    INPUT_VALUE(  2, int2,    imageSize)
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    uint index = get_global_id(0) + get_global_id(1) * imageSize.y;

    float color = INDEX_SAFE(outputAoBuffer,index);

    INDEX_SAFE(outputIndirectLightingBuffer, index).w = color;
}

// ----------------------------------------------------------------------------------------------------
__kernel void compositingRGBMEncode(
    __write_only image2d_t      dynarg_dstTile,
    __read_only image2d_t       dynarg_srcTile,
    INPUT_VALUE(2, float,       rgbmRange),
    INPUT_VALUE(3, float,       lowerThreshold)
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    int2 tileThreadId = (int2)(get_global_id(0), get_global_id(1));

    float4 linearSpaceColor = READ_IMAGEF_SAFE(dynarg_srcTile, kSamplerClampNearestUnormCoords, tileThreadId);

    float4 rgbmValue = RGBMEncode(linearSpaceColor, rgbmRange, lowerThreshold);

    float4 gammaSpaceColor = LinearToGammaSpace01(rgbmValue);

    WRITE_IMAGEF_SAFE(dynarg_dstTile, tileThreadId, gammaSpaceColor);
}

// ----------------------------------------------------------------------------------------------------
__kernel void compositingDLDREncode(
    __write_only image2d_t  dynarg_dstTile,
    __read_only image2d_t   dynarg_srcTile
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    int2 tileThreadId = (int2)(get_global_id(0), get_global_id(1));

    float4 linearSpaceColor = READ_IMAGEF_SAFE(dynarg_srcTile, kSamplerClampNearestUnormCoords, tileThreadId);

    float4 gammaSpaceColor = (float4)(LinearToGammaSpace(linearSpaceColor.x), LinearToGammaSpace(linearSpaceColor.y), LinearToGammaSpace(linearSpaceColor.z), linearSpaceColor.w);

    gammaSpaceColor = min(gammaSpaceColor * 0.5f, kOne);

    WRITE_IMAGEF_SAFE(dynarg_dstTile, tileThreadId, gammaSpaceColor);
}

// ----------------------------------------------------------------------------------------------------
__kernel void compositingLinearToGamma(
    __write_only image2d_t      dynarg_dstTile,
    __read_only image2d_t       dynarg_srcTile
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    int2 tileThreadId = (int2)(get_global_id(0), get_global_id(1));

    float4 linearSpaceColor = READ_IMAGEF_SAFE(dynarg_srcTile, kSamplerClampNearestUnormCoords, tileThreadId);

    float4 gammaSpaceColor = (float4)(LinearToGammaSpace(linearSpaceColor.x), LinearToGammaSpace(linearSpaceColor.y), LinearToGammaSpace(linearSpaceColor.z), linearSpaceColor.w);

    WRITE_IMAGEF_SAFE(dynarg_dstTile, tileThreadId, gammaSpaceColor);
}

// ----------------------------------------------------------------------------------------------------
__kernel void compositingClampValues(
    __write_only image2d_t      dynarg_dstTile,
    __read_only image2d_t       dynarg_srcTile,
    INPUT_VALUE(2, float,       min),
    INPUT_VALUE(3, float,       max)
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    const float4 vMin = (float4)(min, min, min, min);
    const float4 vMax = (float4)(max, max, max, max);

    int2 tileThreadId = (int2)(get_global_id(0), get_global_id(1));

    float4 value = READ_IMAGEF_SAFE(dynarg_srcTile, kSamplerClampNearestUnormCoords, tileThreadId);

    value = clamp(value, vMin, vMax);

    WRITE_IMAGEF_SAFE(dynarg_dstTile, tileThreadId, value);
}

// ----------------------------------------------------------------------------------------------------
__kernel void compositingMultiplyImages(
    __write_only image2d_t      dynarg_dstTile,
    __read_only image2d_t       dynarg_srcTile0,
    __read_only image2d_t       dynarg_srcTile1
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    int2 tileThreadId = (int2)(get_global_id(0), get_global_id(1));

    float4 value1 = READ_IMAGEF_SAFE(dynarg_srcTile0, kSamplerClampNearestUnormCoords, tileThreadId);
    float4 value2 = READ_IMAGEF_SAFE(dynarg_srcTile1, kSamplerClampNearestUnormCoords, tileThreadId);

    float4 result = value1 * value2;

    WRITE_IMAGEF_SAFE(dynarg_dstTile, tileThreadId, result);
}

// ----------------------------------------------------------------------------------------------------
__kernel void compositingBlitTile(
    __write_only image2d_t      dynarg_dstTile,
    __read_only image2d_t       dynarg_srcTile,
    INPUT_VALUE(2, int2,        tileCoordinates)
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    int2 tileThreadId = (int2)(get_global_id(0), get_global_id(1));

    float4 value = READ_IMAGEF_SAFE(dynarg_srcTile, kSamplerClampNearestUnormCoords, tileThreadId);

    int2 lightmapCoords = tileThreadId + tileCoordinates;

    // write_imagef does appropriate data format conversion to the target image format
    WRITE_IMAGEF_SAFE(dynarg_dstTile, lightmapCoords, value);
}

// ----------------------------------------------------------------------------------------------------
// Filters horizontally or vertically depending on filterDirection - (1, 0) or (0, 1)
__kernel void compositingGaussFilter(
    __write_only image2d_t      dynarg_dstTile,
    __read_only image2d_t       dynarg_srcTile,
    INPUT_BUFFER(2, float,      dynarg_filterWeights),
    INPUT_BUFFER(3, int,        chartIndexBuffer),
    INPUT_VALUE( 4, int,        kernelWidth),
    INPUT_VALUE( 5, int2,       filterDirection),
    INPUT_VALUE( 6, int2,       halfKernelWidth),
    INPUT_VALUE( 7,  int2,    compositingTileToBakingTileOffset),
    INPUT_VALUE( 8,  int,     tileSize),
    INPUT_VALUE( 9,  int2,    compositingTileOffset),
    INPUT_VALUE( 10, int,     lightmapSize),
    INPUT_VALUE( 11, int,     inputIsTiled)
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    // Coordinates in tile space
    int2 tileThreadId = (int2)(get_global_id(0), get_global_id(1));

    int index = ConvertThreadCoordsToBakingTileCoords(tileThreadId, compositingTileToBakingTileOffset, tileSize);
    int centerChartId = INDEX_SAFE(chartIndexBuffer, index);

    float4 centerValue = READ_IMAGEF_SAFE(dynarg_srcTile, kSamplerClampNearestUnormCoords, tileThreadId);

    if (centerChartId == -1 || centerValue.w == 0.0f)
        return;

    float4 filtered     = kZero;
    float  weightSum    = 0.0f;
    float  weightCount  = 0.0f;

    int2 startOffset = tileThreadId - halfKernelWidth * filterDirection;
    for (int s = 0; s < kernelWidth; s++)
    {
        int2    sampleCoords    = startOffset + s * filterDirection;

        // Note: not using READ_IMAGEF_SAFE below as those samples are expected to read just outside of the tile boundary, they will get safely clamped though.
        // The srcTile and dstTile are of the same size, so iterating over dstTile texels means trying to sample by halfKernelWidth outside of srcTile at the edges.
        // We are using a separable Gaussian blur, first the vertical one and then the horizontal. The second pass depends on being able to read the results
        // stored in the border area from the first pass. Since we simply swap srcTile and dstTile, it's the easiest to keep them of the same size instead of doing
        // the tileSize vs expanded tileSize logic.

        float4  sampleValue     = read_imagef(dynarg_srcTile, kSamplerClampNearestUnormCoords, sampleCoords);
        index = ConvertThreadCoordsToBakingTileCoords(sampleCoords, compositingTileToBakingTileOffset, tileSize);
        int     sampleChartId   = INDEX_SAFE(chartIndexBuffer, index);

        float weight = sampleValue.w * INDEX_SAFE(dynarg_filterWeights, s);

        weight *= sampleChartId == centerChartId ? 1.0f : 0.0f;

        weightSum   += weight;
        weightCount += sampleValue.w;
        filtered    += weight * sampleValue;
    }

    filtered *= 1.0f / lerp1(1.0f, weightSum, clamp(weightCount, 0.0f, 1.0f));
    filtered.w = 1.0f;

    WRITE_IMAGEF_SAFE(dynarg_dstTile, tileThreadId, filtered);
}


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\OpenCL\kernels\compositeLightmaps.cl---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\OpenCL\kernels\convergence.cl---------------


#include "commonCL.h"

__constant ConvergenceOutputData g_clearedConvergenceOutputData = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, INT_MAX, INT_MAX, INT_MAX, INT_MIN, INT_MIN, INT_MIN};

__kernel void clearConvergenceData(
    OUTPUT_BUFFER(00, ConvergenceOutputData, convergenceOutputDataBuffer)
    KERNEL_VALIDATOR_BUFFERS_DEF
)
{
    INDEX_SAFE(convergenceOutputDataBuffer, 0) = g_clearedConvergenceOutputData;
}

__kernel void calculateConvergenceMap(
    INPUT_BUFFER( 00, unsigned char,         cullingMapBuffer),
    INPUT_BUFFER( 01, int,                   directSampleCountBuffer),
    INPUT_BUFFER( 02, int,                   indirectSampleCountBuffer),
    INPUT_BUFFER( 03, int,                   environmentSampleCountBuffer),
    INPUT_VALUE(  04, int,                   maxDirectSamplesPerPixel),
    INPUT_VALUE(  05, int,                   maxGISamplesPerPixel),
    INPUT_VALUE(  06, int,                   maxEnvSamplesPerPixel),
    INPUT_BUFFER( 07, unsigned char,         occupancyBuffer),
    INPUT_VALUE(  08, int,                   occupiedTexelCount),
    OUTPUT_BUFFER(09, ConvergenceOutputData, convergenceOutputDataBuffer) //Should be cleared properly before kernel is running
    KERNEL_VALIDATOR_BUFFERS_DEF
)
{
    __local ConvergenceOutputData dataShared;
    __local unsigned int totalDirectSamples;
    __local unsigned int totalGISamples;
    __local unsigned int totalEnvSamples;

    int idx = get_global_id(0);

    if (get_local_id(0) == 0)
    {
        dataShared = g_clearedConvergenceOutputData;
        totalDirectSamples = 0;
        totalGISamples = 0;
        totalEnvSamples = 0;
    }

    barrier(CLK_LOCAL_MEM_FENCE);

    const int occupiedSamplesWithinTexel = INDEX_SAFE(occupancyBuffer, idx);

    if (occupiedSamplesWithinTexel != 0)
    {
        const bool isTexelVisible = !IsCulled(INDEX_SAFE(cullingMapBuffer, idx));
        if (isTexelVisible)
            atomic_inc(&(dataShared.visibleTexelCount));

        int directSampleCount = 0;
        if(maxDirectSamplesPerPixel > 0)
        {
            directSampleCount = INDEX_SAFE(directSampleCountBuffer, idx);
            atomic_add(&totalDirectSamples, directSampleCount);
        }
        atomic_min(&(dataShared.minDirectSamples), directSampleCount);
        atomic_max(&(dataShared.maxDirectSamples), directSampleCount);

        const int giSampleCount = INDEX_SAFE(indirectSampleCountBuffer, idx);
        atomic_min(&(dataShared.minGISamples), giSampleCount);
        atomic_max(&(dataShared.maxGISamples), giSampleCount);
        atomic_add(&totalGISamples, giSampleCount);

        const int envSampleCount = INDEX_SAFE(environmentSampleCountBuffer, idx);
        atomic_min(&(dataShared.minEnvSamples), envSampleCount);
        atomic_max(&(dataShared.maxEnvSamples), envSampleCount);
        atomic_add(&totalEnvSamples, envSampleCount);

        if (IsGIConverged(giSampleCount, maxGISamplesPerPixel))
        {
            atomic_inc(&(dataShared.convergedGITexelCount));

            if (isTexelVisible)
                atomic_inc(&(dataShared.visibleConvergedGITexelCount));
        }

        if (IsDirectConverged(directSampleCount, maxDirectSamplesPerPixel))
        {
            atomic_inc(&(dataShared.convergedDirectTexelCount));

            if (isTexelVisible)
                atomic_inc(&(dataShared.visibleConvergedDirectTexelCount));
        }

        if (IsEnvironmentConverged(envSampleCount, maxEnvSamplesPerPixel))
        {
            atomic_inc(&(dataShared.convergedEnvTexelCount));

            if (isTexelVisible)
                atomic_inc(&(dataShared.visibleConvergedEnvTexelCount));
        }

    }

    barrier(CLK_LOCAL_MEM_FENCE);
    if (get_local_id(0) == 0)
    {
        const float maxTotalDirectSamples = (float)occupiedTexelCount * (float)maxDirectSamplesPerPixel;
        const float maxTotalGISamples     = (float)occupiedTexelCount * (float)maxGISamplesPerPixel;
        const float maxTotalEnvSamples    = (float)occupiedTexelCount * (float)maxEnvSamplesPerPixel;
        const unsigned int averageDirectSamplesRatio = (int)ceil(CONVERGENCE_AVERAGES_PRECISION * (float)totalDirectSamples / maxTotalDirectSamples);
        const unsigned int averageGISamplesRatio     = (int)ceil(CONVERGENCE_AVERAGES_PRECISION * (float)totalGISamples     / maxTotalGISamples);
        const unsigned int averageEnvSamplesRatio    = (int)ceil(CONVERGENCE_AVERAGES_PRECISION * (float)totalEnvSamples    / maxTotalEnvSamples);

        atomic_add(&(INDEX_SAFE(convergenceOutputDataBuffer, 0).visibleTexelCount), dataShared.visibleTexelCount);
        atomic_min(&(INDEX_SAFE(convergenceOutputDataBuffer, 0).minDirectSamples), dataShared.minDirectSamples);
        atomic_max(&(INDEX_SAFE(convergenceOutputDataBuffer, 0).maxDirectSamples), dataShared.maxDirectSamples);
        atomic_add(&(INDEX_SAFE(convergenceOutputDataBuffer, 0).averageDirectSamplesPercent), averageDirectSamplesRatio);
        atomic_min(&(INDEX_SAFE(convergenceOutputDataBuffer, 0).minGISamples), dataShared.minGISamples);
        atomic_max(&(INDEX_SAFE(convergenceOutputDataBuffer, 0).maxGISamples), dataShared.maxGISamples);
        atomic_add(&(INDEX_SAFE(convergenceOutputDataBuffer, 0).averageGISamplesPercent), averageGISamplesRatio);
        atomic_min(&(INDEX_SAFE(convergenceOutputDataBuffer, 0).minEnvSamples), dataShared.minEnvSamples);
        atomic_max(&(INDEX_SAFE(convergenceOutputDataBuffer, 0).maxEnvSamples), dataShared.maxEnvSamples);
        atomic_add(&(INDEX_SAFE(convergenceOutputDataBuffer, 0).averageEnvSamplesPercent), averageEnvSamplesRatio);
        atomic_add(&(INDEX_SAFE(convergenceOutputDataBuffer, 0).convergedGITexelCount), dataShared.convergedGITexelCount);
        atomic_add(&(INDEX_SAFE(convergenceOutputDataBuffer, 0).visibleConvergedGITexelCount), dataShared.visibleConvergedGITexelCount);
        atomic_add(&(INDEX_SAFE(convergenceOutputDataBuffer, 0).convergedDirectTexelCount), dataShared.convergedDirectTexelCount);
        atomic_add(&(INDEX_SAFE(convergenceOutputDataBuffer, 0).visibleConvergedDirectTexelCount), dataShared.visibleConvergedDirectTexelCount);
        atomic_add(&(INDEX_SAFE(convergenceOutputDataBuffer, 0).convergedEnvTexelCount), dataShared.convergedEnvTexelCount);
        atomic_add(&(INDEX_SAFE(convergenceOutputDataBuffer, 0).visibleConvergedEnvTexelCount), dataShared.visibleConvergedEnvTexelCount);
    }
}

__kernel void countUnconverged(
    INPUT_BUFFER(00, int, directSampleCountBuffer),
    INPUT_BUFFER(01, int, indirectSampleCountBuffer),
    INPUT_BUFFER(02, int, environmentSampleCountBuffer),
    INPUT_VALUE( 03, int, targetDirectSamplesPerProbe),
    INPUT_VALUE( 04, int, targetGISamplesPerProbe),
    INPUT_VALUE( 05, int, targetEnvSamplesPerProbe),
    INPUT_VALUE( 06, int, probeCount),
    OUTPUT_BUFFER(07, int, unconvergedCountBuffer)
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    int idx = get_global_id(0);
    if (idx >= probeCount)
        return;

    __local int threadGroupProbeCount;
    if (get_local_id(0) == 0)
    {
        threadGroupProbeCount = 0;
    }

    barrier(CLK_LOCAL_MEM_FENCE);

    int directSampleCount = INDEX_SAFE(directSampleCountBuffer, idx);
    int giSampleCount = INDEX_SAFE(indirectSampleCountBuffer, idx);
    int envSampleCount = INDEX_SAFE(environmentSampleCountBuffer, idx);

    if (directSampleCount < targetDirectSamplesPerProbe
        || giSampleCount < targetGISamplesPerProbe
        || envSampleCount < targetEnvSamplesPerProbe)
    {
        atomic_inc(&threadGroupProbeCount);
    }

    barrier(CLK_LOCAL_MEM_FENCE);

    if (get_local_id(0) == 0)
    {
        atomic_add(&INDEX_SAFE(unconvergedCountBuffer, 0), threadGroupProbeCount);
    }
}


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\OpenCL\kernels\convergence.cl---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\OpenCL\kernels\culling.cl---------------


#include "commonCL.h"

__kernel void clearLightmapCulling(
    OUTPUT_BUFFER(00, unsigned char, cullingMapBuffer)
    KERNEL_VALIDATOR_BUFFERS_DEF
)
{
    int idx = get_global_id(0);
    INDEX_SAFE(cullingMapBuffer, idx) = 255;
}

__kernel void prepareLightmapCulling(
    INPUT_BUFFER( 00, unsigned char,       occupancyBuffer),
    INPUT_BUFFER( 01, float4,              positionsWSBuffer),
    INPUT_BUFFER( 02, PackedNormalOctQuad, interpNormalsWSBuffer),
    INPUT_VALUE(  03, Matrix4x4,           worldToClip),
    INPUT_VALUE(  04, float4,              cameraPosition),
    INPUT_VALUE(  05, int,                 superSamplingMultiplier),
    OUTPUT_BUFFER(06, ray,                 lightRaysCompactedBuffer),
    OUTPUT_BUFFER(07, uint,                lightRaysCountBuffer),
    INPUT_BUFFER( 08, uint,                instanceIdToLodInfoBuffer)
    KERNEL_VALIDATOR_BUFFERS_DEF
)
{
    ray r;  // prepare ray in private memory
    int idx = get_global_id(0);

    __local int numRayPreparedSharedMem;
    if (get_local_id(0) == 0)
        numRayPreparedSharedMem = 0;
    barrier(CLK_LOCAL_MEM_FENCE);
    atomic_inc(&numRayPreparedSharedMem);

    //TODO(RadeonRays) on spot compaction (guillaume v1 style)

    const int occupiedSamplesWithinTexel = INDEX_SAFE(occupancyBuffer, idx);
    if (occupiedSamplesWithinTexel == 0) // Reject texels that are invalid.
    {
        Ray_SetInactive(&r);
    }
    else
    {
        // Just fetch one sample, we know the position is within an occupied texel.
        int ssIdx = idx * superSamplingMultiplier * superSamplingMultiplier;
        float4 position = INDEX_SAFE(positionsWSBuffer, ssIdx);

        //Clip space position
        float4 clipPos = transform_point(position.xyz, worldToClip);
        clipPos.xyz /= clipPos.w;

        //Camera to texel
        //float3 camToPos = (position.xyz - cameraPosition.xyz);

        //Normal
        float3 normal = CalculateSuperSampledInterpolatedNormal(idx, superSamplingMultiplier, interpNormalsWSBuffer KERNEL_VALIDATOR_BUFFERS);
        //float normalDotCamToPos = dot(normal, camToPos);

        //Is the texel visible?
        if (clipPos.x >= -1.0f && clipPos.x <= 1.0f &&
            clipPos.y >= -1.0f && clipPos.y <= 1.0f &&
            clipPos.z >= 0.0f && clipPos.z <= 1.0f)
            //TODO(RadeonRays) understand why this does not work.
            //&& normalDotCamToPos < 0.0f)
        {
            const float kMinPushOffDistance = 0.001f;
            float3 targetPos = position.xyz + normal * kMinPushOffDistance;
            float3 camToTarget = (targetPos - cameraPosition.xyz);
            float camToTargetDist = length(camToTarget);
            if (camToTargetDist > 0)
            {
                const int instanceId = (int)(floor(position.w));
                const int instanceLodInfo = INDEX_SAFE(instanceIdToLodInfoBuffer, instanceId);
                Ray_Init(&r, cameraPosition.xyz, camToTarget/ camToTargetDist, camToTargetDist, 0.f, instanceLodInfo);
            }
            else
            {
                Ray_SetInactive(&r);
            }
        }
        else
        {
            Ray_SetInactive(&r);
        }
    }

    barrier(CLK_LOCAL_MEM_FENCE);
    if (get_local_id(0) == 0)
    {
        atomic_add(GET_PTR_SAFE(lightRaysCountBuffer, 0), numRayPreparedSharedMem);
    }
    INDEX_SAFE(lightRaysCompactedBuffer, idx) = r;
}

__kernel void processLightmapCulling(
    INPUT_BUFFER( 00, ray,           lightRaysCompactedBuffer),
    INPUT_BUFFER( 01, float4,        lightOcclusionCompactedBuffer),
    OUTPUT_BUFFER(02, unsigned char, cullingMapBuffer),
    OUTPUT_BUFFER(03, unsigned int,  visibleTexelCountBuffer) //Need to have been cleared to 0 before the kernel is called.
    KERNEL_VALIDATOR_BUFFERS_DEF
)
{
    __local int visibleTexelCountSharedMem;
    int idx = get_global_id(0);
    if (get_local_id(0) == 0)
        visibleTexelCountSharedMem = 0;
    barrier(CLK_LOCAL_MEM_FENCE);

    const bool rayActive = !Ray_IsInactive(GET_PTR_SAFE(lightRaysCompactedBuffer, idx));//TODO(RadeonRays) on spot compaction (guillaume v1 style) see same comment above
    const bool hit = rayActive && INDEX_SAFE(lightOcclusionCompactedBuffer, idx).w < TRANSMISSION_THRESHOLD;
    const bool texelVisible = rayActive && !hit;

    if (texelVisible)
    {
        INDEX_SAFE(cullingMapBuffer, idx) = 255;
    }
    else
    {
        INDEX_SAFE(cullingMapBuffer, idx) = 0;
    }

    // nvidia+macOS hack (atomic operation in the if above break the write to cullingMapBuffer!).
    int intTexelVisible = texelVisible?1:0;
    atomic_add(&visibleTexelCountSharedMem,intTexelVisible);

    barrier(CLK_LOCAL_MEM_FENCE);
    if (get_local_id(0) == 0)
        atomic_add(GET_PTR_SAFE(visibleTexelCountBuffer, 0), visibleTexelCountSharedMem);
}


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\OpenCL\kernels\culling.cl---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\OpenCL\kernels\customBake.cl---------------


#include "commonCL.h"

__kernel void prepareCustomBake(
    OUTPUT_BUFFER(00, ray, pathRaysCompactedBuffer_0),
    OUTPUT_BUFFER(01, uint, activePathCountBuffer_0),
    OUTPUT_BUFFER(02, uint, totalRaysCastCountBuffer),
    OUTPUT_BUFFER(03, float4, originalRaysExpandedBuffer),
    INPUT_BUFFER( 04, float4, positionsWSBuffer),
    INPUT_BUFFER( 05, uint, sobol_buffer),
    INPUT_BUFFER( 06, float, goldenSample_buffer),
    INPUT_VALUE(  07, int, numGoldenSample),
    INPUT_VALUE(  08, int, fakedLightmapResolution), // position count
    INPUT_BUFFER( 09, SampleDescription, sampleDescriptionsExpandedBuffer),
    INPUT_BUFFER( 10, uint, sampleDescriptionsExpandedCountBuffer)
    KERNEL_VALIDATOR_BUFFERS_DEF
)
{
    __local uint numRayPreparedSharedMem;
    if (get_local_id(0) == 0)
        numRayPreparedSharedMem = 0;
    barrier(CLK_LOCAL_MEM_FENCE);

    // Prepare ray in private memory
    ray r;
    Ray_SetInactive(&r);

    int expandedPathRayIdx = get_global_id(0), local_idx;
    const uint sampleDescriptionsExpandedCount = INDEX_SAFE(sampleDescriptionsExpandedCountBuffer, 0);
    if (expandedPathRayIdx < sampleDescriptionsExpandedCount)
    {
        const SampleDescription sampleDescription = INDEX_SAFE(sampleDescriptionsExpandedBuffer, expandedPathRayIdx);
#if DISALLOW_RAY_EXPANSION
        if (sampleDescription.texelIndex >= 0)
        {
#endif
            const int ssIdx = sampleDescription.texelIndex;
            const float4 position = INDEX_SAFE(positionsWSBuffer, ssIdx);
            const float rayOffset = position.w;

            // Skip unused texels.
            if (rayOffset >= 0.0)
            {
                AssertPositionIsOccupied(position KERNEL_VALIDATOR_BUFFERS);

                // Get random numbers
                float3 sample3D;
                sample3D.x = SobolSample(sampleDescription.currentSampleCount, 0, sobol_buffer KERNEL_VALIDATOR_BUFFERS);
                sample3D.y = SobolSample(sampleDescription.currentSampleCount, 1, sobol_buffer KERNEL_VALIDATOR_BUFFERS);
                sample3D.z = SobolSample(sampleDescription.currentSampleCount, 2, sobol_buffer KERNEL_VALIDATOR_BUFFERS);

                int texel_x = sampleDescription.texelIndex % fakedLightmapResolution;
                int texel_y = sampleDescription.texelIndex / fakedLightmapResolution;
                sample3D = ApplyCranleyPattersonRotation3D(sample3D, texel_x, texel_y, fakedLightmapResolution, 0, goldenSample_buffer KERNEL_VALIDATOR_BUFFERS);

                // We don't want the full sphere, we only want the upper hemisphere.
                float3 direction = SphereSample(sample3D.xy);
                if (direction.y < 0.0f)
                    direction = make_float3(direction.x, -direction.y, direction.z);

                const float randOffset = 0.1f * rayOffset + 0.9f * rayOffset * sample3D.z;
                const float3 origin = position.xyz + direction * randOffset;
                const float kMaxt = 1000000.0f;
                const int instanceLodInfo = PackLODInfo(NO_LOD_MASK, NO_LOD_GROUP);
                Ray_Init(&r, origin, direction, kMaxt, 0.f, instanceLodInfo);

                // Set the index so we can map to the originating texel/probe
                Ray_SetSourceIndex(&r, sampleDescription.texelIndex);
            }
#if DISALLOW_RAY_EXPANSION
        }
#endif
    }

    // Threads synchronization for compaction
    if (Ray_IsActive_Private(&r))
    {
        local_idx = atomic_inc(&numRayPreparedSharedMem);
    }
    barrier(CLK_LOCAL_MEM_FENCE);
    if (get_local_id(0) == 0)
    {
        atomic_add(GET_PTR_SAFE(totalRaysCastCountBuffer, 0), numRayPreparedSharedMem);
        int numRayToAdd = numRayPreparedSharedMem;
#if DISALLOW_PATH_RAYS_COMPACTION
        numRayToAdd = get_local_size(0);
#endif
        numRayPreparedSharedMem = atomic_add(GET_PTR_SAFE(activePathCountBuffer_0, 0), numRayToAdd);
    }
    barrier(CLK_LOCAL_MEM_FENCE);

    int compactedPathRayIndex = numRayPreparedSharedMem + local_idx;
#if DISALLOW_PATH_RAYS_COMPACTION
    compactedPathRayIndex = expandedPathRayIdx;
#else
    // Write the ray out to memory
    if (Ray_IsActive_Private(&r))
#endif
    {
        INDEX_SAFE(originalRaysExpandedBuffer, expandedPathRayIdx) = (float4)(r.d.x, r.d.y, r.d.z, 0);
        Ray_SetSampleDescriptionIndex(&r, expandedPathRayIdx);
        INDEX_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIndex) = r;
    }
}

__kernel void processCustomBake(
    //*** input ***
    INPUT_BUFFER( 00, ray,    pathRaysCompactedBuffer_0),
    INPUT_BUFFER( 01, uint,   activePathCountBuffer_0),
    INPUT_BUFFER( 02, float4, pathThroughputExpandedBuffer),
    INPUT_VALUE(  03, int,    totalSampleCount),
    INPUT_BUFFER( 04, float4, shadowmaskAoValidityExpandedBuffer), //Used to store validity in .y
    INPUT_BUFFER( 05, Intersection, pathIntersectionsCompactedBuffer),
    //*** output ***
    OUTPUT_BUFFER(06, float4, probeOcclusionExpandedBuffer)
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    const uint compactedRayIdx = get_global_id(0);
    if (compactedRayIdx >= INDEX_SAFE(activePathCountBuffer_0, 0))
        return;

#if DISALLOW_LIGHT_RAYS_COMPACTION
    if (Ray_IsInactive(GET_PTR_SAFE(pathRaysCompactedBuffer_0, compactedRayIdx)))
        return;
#endif

    KERNEL_ASSERT(Ray_IsActive(GET_PTR_SAFE(pathRaysCompactedBuffer_0, compactedRayIdx)));
    const int sampleDescriptionIdx = Ray_GetSampleDescriptionIndex(GET_PTR_SAFE(pathRaysCompactedBuffer_0, compactedRayIdx));

    const bool pathRayHitSomething = INDEX_SAFE(pathIntersectionsCompactedBuffer, compactedRayIdx).shapeid != MISS_MARKER;
    const float4 throughput = INDEX_SAFE(pathThroughputExpandedBuffer, sampleDescriptionIdx);
    float3 color = throughput.xyz;

    if (pathRayHitSomething)
        color = make_float3(0,0,0);

    // accumulate sky occlusion.
    const float backfacing = INDEX_SAFE(shadowmaskAoValidityExpandedBuffer, sampleDescriptionIdx).y;
    INDEX_SAFE(probeOcclusionExpandedBuffer, sampleDescriptionIdx) += make_float4(color.x, color.y, color.z, backfacing);
    KERNEL_ASSERT(totalSampleCount > 0);
}


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\OpenCL\kernels\customBake.cl---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\OpenCL\kernels\directLightingTests.cl---------------


#include "directLighting.h"

// From PBRTv3
float nextFloatUp(float v)
{
    if (isinf(v) && v > 0.0f)
        return v;
    if (v == -0.0f)
        v = 0.0f;

    uint ui = as_uint(v);
    if (v >= 0)
        ++ui;
    else
        --ui;
    return as_float(ui);
}

float nextFloatDown(float v)
{
    if (isinf(v) && v < 0.)
        return v;
    if (v == 0.f)
        v = -0.f;

    uint ui = as_uint(v);
    if (v > 0)
        --ui;
    else
        ++ui;
    return as_float(ui);
}

#define SEARCH_SQUARE_SIDE 10

void test(float3 surfacePosition, __global int* directionHasNaN, __global int* rayIsActive, int outputOffset)
{
    LightBuffer light;
    light.pos = (float4)(0.1f, 0.1f, 0.1f, 0.0f);
    light.col = (float4)(1.0f, 1.0f, 1.0f, 1.0f);
    light.dir = (float4)(0.0f, 0.0f, 1.0f, 1000.0f); // w is lightRange
    light.lightType = kPVRLightRectangle;
    light.directBakeMode = kDirectBakeMode_None;
    light.probeOcclusionLightIndex = 0;
    light.castShadow = 1;
    light.dataUnion.areaLightData.areaHeight = 0.5f;
    light.dataUnion.areaLightData.areaWidth = 0.5f;
    light.dataUnion.areaLightData.cookieIndex = -1;
    light.dataUnion.areaLightData.Normal = (float4)(0.0f, 0.0f, 1.0f, 0.0f);
    light.dataUnion.areaLightData.Tangent = (float4)(1.0f, 0.0f, 0.0, 0.0f);
    light.dataUnion.areaLightData.Bitangent = (float4)(0.0f, 1.0f, 0.0f, 0.0f);

    float2 sample2D = (float2)(0.5f, 0.3f);
    float3 surfaceNormal = normalize((float3)(0.1f, -0.5f, -0.3f));
    float pushOff = 0.0f;

    for (int i = 0; i < SEARCH_SQUARE_SIDE/2; ++i)
    {
        surfacePosition.x = nextFloatDown(surfacePosition.x);
        surfacePosition.y = nextFloatDown(surfacePosition.y);
    }
    float3 startSurfacePosition = surfacePosition;

    for (int i = 0; i < SEARCH_SQUARE_SIDE*SEARCH_SQUARE_SIDE; ++i)
    {
        ray outputRay;
        PrepareShadowRay(light, sample2D, surfacePosition, surfaceNormal, pushOff, false, &outputRay, 0);

        directionHasNaN[i+outputOffset] = (int)(any(isnan(outputRay.d)));
        rayIsActive[i+outputOffset] = (int)(!Ray_IsInactive_Private(&outputRay));

        surfacePosition.x = nextFloatUp(surfacePosition.x);

        // end of line, goto beginning of next line
        if (i % SEARCH_SQUARE_SIDE == 0)
        {
            surfacePosition.y = nextFloatUp(surfacePosition.y);
            surfacePosition.x = startSurfacePosition.x;
        }
    }
}


__kernel void testShadowRay_Case1358519(
    OUTPUT_BUFFER(0, int, directionHasNaN),
    OUTPUT_BUFFER(0, int, rayIsActive)
)
{
    // The outcome of floating point computations will vary from machine to machine (different hardware, compiler version...)
    // In order to make the test more reliable, we execute PrepareShadowRay for various surfacePositions located in a small neighbourhood around the problematic value
    float3 surfacePosition =(float3)(0.126095816,0.0242773965,0.100000001);// Value that was causing an issue on an RTX 3090
    test(surfacePosition, directionHasNaN, rayIsActive, 0);

    // surfacePosition that is not near the light surface and should always produce valid rays
    surfacePosition = (float3)(0.126095816, 0.0242773965, 0.5);
    test(surfacePosition, directionHasNaN, rayIsActive, SEARCH_SQUARE_SIDE*SEARCH_SQUARE_SIDE);
}


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\OpenCL\kernels\directLightingTests.cl---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\OpenCL\kernels\expansionAndGathering.cl---------------


#include "commonCL.h"

__kernel void prepareExpandedRayIndices(
    //output
    OUTPUT_BUFFER(00, SampleDescription, sampleDescriptionsExpandedBuffer),
    OUTPUT_BUFFER(01, uint,              sampleDescriptionsExpandedCountBuffer),
    OUTPUT_BUFFER(02, ExpandedTexelInfo, expandedTexelsBuffer),
    OUTPUT_BUFFER(03, uint,              expandedTexelsCountBuffer),
    //input and output
    OUTPUT_BUFFER(04, int,               directSampleCountBuffer),
    OUTPUT_BUFFER(05, int,               environmentSampleCountBuffer),
    OUTPUT_BUFFER(06, int,               indirectSampleCountBuffer),
    //input
    INPUT_VALUE(  07, int,               radeonRaysExpansionPass),
    INPUT_VALUE(  08, int,               numRaysToShootPerTexel),
    INPUT_VALUE(  09, int,               maxSampleCount),
    INPUT_VALUE(  10, int,               maxOutputRayCount)
#ifndef PROBES
    ,
    INPUT_BUFFER( 11, unsigned char,     cullingMapBuffer),
    INPUT_BUFFER( 12, unsigned char,     occupancyBuffer),
    INPUT_VALUE(  13, int,               shouldUseCullingMap),
    INPUT_VALUE(  14, int,               lightmapSize),
    INPUT_VALUE(  15, uint,              currentTileIdx),
    INPUT_VALUE(  16, uint,              sqrtNumTiles)
#endif
    KERNEL_VALIDATOR_BUFFERS_DEF
)
{
    // Initialize local memory
    __local int numExpandedTexelsForThreadGroup;
    __local int threadGroupExpandedTexelOffsetInGlobalMemory;
    __local int numRaysForThreadGroup;
    __local int threadGroupRaysOffsetInGlobalMemory;
    if (get_local_id(0) == 0)
    {
        numExpandedTexelsForThreadGroup = 0;
        threadGroupExpandedTexelOffsetInGlobalMemory = 0;
        numRaysForThreadGroup = 0;
        threadGroupRaysOffsetInGlobalMemory = 0;
    }
    barrier(CLK_LOCAL_MEM_FENCE);

    // When enqueuing this kernel, we launch as many threads as nbTexels.
    // Therefore this idx is in [0,tileWidth*tileHeight] if tiling is activated,
    // or [0,lightmapWidth*lightmapHeight] if tiling is deactivated
    const uint idx = get_global_id(0);

#ifndef PROBES
    uint globalIdx = GetGlobalIndex(idx, lightmapSize, currentTileIdx, sqrtNumTiles);
#else
    uint globalIdx = idx;
#endif

    int numRaysToPrepare = numRaysToShootPerTexel;
#if DISALLOW_RAY_EXPANSION
    numRaysToPrepare = 1;
#endif

    // STEP 1 : Determine if the texel is active (i.e. occupied && visible).
#ifndef PROBES
    const int occupiedSamplesWithinTexel = INDEX_SAFE(occupancyBuffer, idx);
    if (occupiedSamplesWithinTexel == 0)
        numRaysToPrepare = 0;
    if (shouldUseCullingMap && numRaysToPrepare && IsCulled(INDEX_SAFE(cullingMapBuffer, idx)))
        numRaysToPrepare = 0;
#endif

    // STEP 2 : Compute how many rays we want to shoot for the active texels.
    int currentSampleCount;
    if (numRaysToPrepare)
    {
        if (radeonRaysExpansionPass == kRRExpansionPass_direct)
        {
            currentSampleCount = INDEX_SAFE(directSampleCountBuffer, idx);
        }
        else if (radeonRaysExpansionPass == kRRExpansionPass_environment)
        {
            currentSampleCount = INDEX_SAFE(environmentSampleCountBuffer, idx);
        }
        else if (radeonRaysExpansionPass == kRRExpansionPass_indirect)
        {
            currentSampleCount = INDEX_SAFE(indirectSampleCountBuffer, idx);
        }

        KERNEL_ASSERT(maxSampleCount >= currentSampleCount);
        int samplesLeftBeforeConvergence = max(maxSampleCount - currentSampleCount, 0);
        numRaysToPrepare = min(samplesLeftBeforeConvergence, numRaysToPrepare);
    }

    // STEP 3 : Compute rays write offsets and init the rays indices.
    int rayOffsetInThreadGroup = 0;
    if (numRaysToPrepare)
        rayOffsetInThreadGroup = atomic_add(&numRaysForThreadGroup, numRaysToPrepare);
    barrier(CLK_LOCAL_MEM_FENCE);
    if (get_local_id(0) == 0)
    {
#if DISALLOW_RAY_EXPANSION
        numRaysForThreadGroup = get_local_size(0);
#endif
        //Note: SampleDescriptionsExpandedCountBuffer will be potentially bigger than the size of SampleDescriptionsExpandedBuffer. However this is fine
        //as we will only dispatch the following kernel with numthread = ray buffer size.
        threadGroupRaysOffsetInGlobalMemory = atomic_add(GET_PTR_SAFE(sampleDescriptionsExpandedCountBuffer, 0), numRaysForThreadGroup);
    }
    barrier(CLK_LOCAL_MEM_FENCE);

    // STEP 4 : Write the rays texel index out (avoiding writing more rays than the buffer can hold).
    int threadGlobalRayOffset = threadGroupRaysOffsetInGlobalMemory + rayOffsetInThreadGroup;
    int maxNumRaysThisThreadCanPrepare = max(maxOutputRayCount - threadGlobalRayOffset, 0);
    numRaysToPrepare = min(maxNumRaysThisThreadCanPrepare, numRaysToPrepare);
#if DISALLOW_RAY_EXPANSION
    SampleDescription sampleDescription;

    // -1 marks a texel we should not cast a ray from (invalid or culled)
    // texelIndex is always a global index in lightmap, not in the tile.
    sampleDescription.texelIndex = numRaysToPrepare ? globalIdx : -1;
    sampleDescription.currentSampleCount = currentSampleCount;
    INDEX_SAFE(sampleDescriptionsExpandedBuffer, idx) = sampleDescription;
#else
    for (int i = 0; i < numRaysToPrepare; ++i)
    {
        SampleDescription sampleDescription;
        sampleDescription.texelIndex = globalIdx;
        sampleDescription.currentSampleCount = currentSampleCount + i;
        INDEX_SAFE(sampleDescriptionsExpandedBuffer, threadGlobalRayOffset + i) = sampleDescription;
    }
#endif

    // STEP 5 : Register expanded texel info for the gather step.
    int expandedTexelOffsetInThreadGroup = 0;
    if (numRaysToPrepare)
        expandedTexelOffsetInThreadGroup = atomic_inc(&numExpandedTexelsForThreadGroup);
    barrier(CLK_LOCAL_MEM_FENCE);
    if (get_local_id(0) == 0)
    {
        KERNEL_ASSERT(numExpandedTexelsForThreadGroup <= get_local_size(0));
        KERNEL_ASSERT(numRaysForThreadGroup <= (numRaysToShootPerTexel * get_local_size(0)));
        KERNEL_ASSERT(numRaysForThreadGroup >= numExpandedTexelsForThreadGroup);
        KERNEL_ASSERT(numExpandedTexelsForThreadGroup <= get_local_size(0));
        threadGroupExpandedTexelOffsetInGlobalMemory = atomic_add(GET_PTR_SAFE(expandedTexelsCountBuffer, 0), numExpandedTexelsForThreadGroup);
    }
    barrier(CLK_LOCAL_MEM_FENCE);
    if (numRaysToPrepare)
    {
        ExpandedTexelInfo expandedTexelInfo;
#if DISALLOW_RAY_EXPANSION
        expandedTexelInfo.firstRaysOffset = idx;
#else
        expandedTexelInfo.firstRaysOffset = threadGlobalRayOffset;
#endif
        expandedTexelInfo.numRays = numRaysToPrepare;
        expandedTexelInfo.originalTexelIndex = idx;
        KERNEL_ASSERT(threadGroupExpandedTexelOffsetInGlobalMemory < get_global_size(0));
        KERNEL_ASSERT((threadGroupExpandedTexelOffsetInGlobalMemory + expandedTexelOffsetInThreadGroup)< get_global_size(0));
        KERNEL_ASSERT(expandedTexelOffsetInThreadGroup < get_local_size(0));
        INDEX_SAFE(expandedTexelsBuffer, threadGroupExpandedTexelOffsetInGlobalMemory + expandedTexelOffsetInThreadGroup) = expandedTexelInfo;

        //increment sample count
        if (radeonRaysExpansionPass == kRRExpansionPass_direct)
        {
            INDEX_SAFE(directSampleCountBuffer, idx) += numRaysToPrepare;
        }
        else if (radeonRaysExpansionPass == kRRExpansionPass_environment)
        {
            INDEX_SAFE(environmentSampleCountBuffer, idx) += numRaysToPrepare;
        }
        else if (radeonRaysExpansionPass == kRRExpansionPass_indirect)
        {
            INDEX_SAFE(indirectSampleCountBuffer, idx) += numRaysToPrepare;
        }
    }
}

__kernel void gatherProcessedExpandedRays(
    INPUT_BUFFER( 00, ExpandedTexelInfo, expandedTexelsBuffer),
    INPUT_BUFFER( 01, uint,              expandedTexelsCountBuffer),
    INPUT_VALUE(  02, int,               radeonRaysExpansionPass),
#ifdef PROBES
    INPUT_VALUE(  03, int,               numProbes),
    INPUT_VALUE(  04, int,               lightmappingSourceType),
    INPUT_BUFFER( 05, float4,            probeSHExpandedBuffer),
    INPUT_BUFFER( 06, float,             probeDepthOctahedronExpandedBuffer),
    INPUT_BUFFER( 07, float4,            probeOcclusionExpandedBuffer),
    INPUT_BUFFER( 08, float4,            shadowmaskAoValidityExpandedBuffer), //when gathering indirect .x will contain AO and .y will contain Validity
    OUTPUT_BUFFER(09, float4,            outputProbeDirectSHDataBuffer),
    OUTPUT_BUFFER(10, float4,            outputProbeOcclusionBuffer),
    OUTPUT_BUFFER(11, float4,            outputProbeIndirectSHDataBuffer),
    OUTPUT_BUFFER(12, float,             outputProbeValidityBuffer),
    OUTPUT_BUFFER(13, float,             outputProbeDepthOctahedronBuffer)
#else
    INPUT_VALUE(  03, int,               lightmapMode),
    INPUT_VALUE(  04, int,               useAo),
    INPUT_VALUE(  05, int,               useShadowmask),
    INPUT_BUFFER( 06, float3,            lightingExpandedBuffer),
    INPUT_BUFFER( 07, float4,            shadowmaskAoValidityExpandedBuffer),//when gathering indirect .x will contain AO and .y will contain Validity
    INPUT_BUFFER( 08, float4,            directionalExpandedBuffer),
    OUTPUT_BUFFER(09, float4,            outputDirectLightingBuffer),
    OUTPUT_BUFFER(10, float4,            outputShadowmaskFromDirectBuffer),
    OUTPUT_BUFFER(11, float4,            outputDirectionalFromDirectBuffer),
    OUTPUT_BUFFER(12, float4,            outputIndirectLightingBuffer),
    OUTPUT_BUFFER(13, float4,            outputEnvironmentLightingBuffer),
    OUTPUT_BUFFER(14, float4,            outputDirectionalFromGiBuffer),
    OUTPUT_BUFFER(15, float,             outputAoBuffer),
    OUTPUT_BUFFER(16, float,             outputValidityBuffer)
#endif
    KERNEL_VALIDATOR_BUFFERS_DEF
)
{
    const uint expandedTexelInfoIdx = get_global_id(0);
    const uint numExpandedTexels = INDEX_SAFE(expandedTexelsCountBuffer, 0);
    if (expandedTexelInfoIdx < numExpandedTexels)
    {
        const ExpandedTexelInfo expandedTexelInfo = INDEX_SAFE(expandedTexelsBuffer, expandedTexelInfoIdx);
        const int numRays = expandedTexelInfo.numRays;
        const int raysOffset = expandedTexelInfo.firstRaysOffset;
        const uint originalTexelIndex = expandedTexelInfo.originalTexelIndex;

#ifdef PROBES
        float4 probeOcclusion = (float4)(0.0f, 0.0f, 0.0f, 0.0f);
        for (int i = 0; i < numRays; ++i)
            probeOcclusion += INDEX_SAFE(probeOcclusionExpandedBuffer, raysOffset + i);

        if (lightmappingSourceType == kLightmappingSourceType_Probe)
        {
            float4 outSH[SH_COEFF_COUNT];

            for (int coeff = 0; coeff < SH_COEFF_COUNT; ++coeff)
                outSH[coeff] = (float4)(0.0f, 0.0f, 0.0f, 0.0f);

            float outDepthOctahedron[OCTAHEDRON_TEXEL_COUNT];
            if (outputProbeDepthOctahedronBuffer)
            {
                for (int texel = 0; texel < OCTAHEDRON_TEXEL_COUNT; ++texel)
                    outDepthOctahedron[texel] = 0;
            }

            float4 shadowMask = (float4)(0.0f, 0.0f, 0.0f, 0.0f);
            for (int i = 0; i < numRays; ++i)
            {
                int dataPositionCoeff = (raysOffset + i) * SH_COEFF_COUNT;
                for (int coeff = 0; coeff < SH_COEFF_COUNT; ++coeff)
                {
                    outSH[coeff] += INDEX_SAFE(probeSHExpandedBuffer, dataPositionCoeff + coeff);
                }

                shadowMask += INDEX_SAFE(shadowmaskAoValidityExpandedBuffer, raysOffset + i);

                if (outputProbeDepthOctahedronBuffer)
                {
                    int dataPositionOctahedron = (raysOffset + i) * OCTAHEDRON_TEXEL_COUNT;
                    for (int texel = 0; texel < OCTAHEDRON_TEXEL_COUNT; ++texel)
                    {
                        outDepthOctahedron[texel] += INDEX_SAFE(probeDepthOctahedronExpandedBuffer, dataPositionOctahedron + texel);
                    }
                }
            }

            // TODO(RadeonRays): memory access is all over the place, make a struct ala SphericalHarmonicsL2 instead of loading/storing with a stride.
            if (radeonRaysExpansionPass == kRRExpansionPass_direct)
            {
                for (int coeff = 0; coeff < SH_COEFF_COUNT; ++coeff)
                {
                    INDEX_SAFE(outputProbeDirectSHDataBuffer, numProbes * coeff + originalTexelIndex) += outSH[coeff];
                }
                INDEX_SAFE(outputProbeOcclusionBuffer, originalTexelIndex) += probeOcclusion;
            }
            else
            {
                for (int coeff = 0; coeff < SH_COEFF_COUNT; ++coeff)
                {
                    INDEX_SAFE(outputProbeIndirectSHDataBuffer, numProbes * coeff + originalTexelIndex) += outSH[coeff];
                }
            }

            if (radeonRaysExpansionPass == kRRExpansionPass_indirect)
            {
                INDEX_SAFE(outputProbeValidityBuffer, originalTexelIndex) += shadowMask.y;
                if (outputProbeDepthOctahedronBuffer)
                {
                    for (int texel = 0; texel < OCTAHEDRON_TEXEL_COUNT; ++texel)
                        INDEX_SAFE(outputProbeDepthOctahedronBuffer, originalTexelIndex * OCTAHEDRON_TEXEL_COUNT + texel) += outDepthOctahedron[texel];
                }
            }
        }
        else //Custom Bake
        {
            if (radeonRaysExpansionPass == kRRExpansionPass_direct)
            {
                INDEX_SAFE(outputProbeOcclusionBuffer, originalTexelIndex) += probeOcclusion;
            }
        }
#else
        float4 shadowMask = (float4)(0.0f, 0.0f, 0.0f, 0.0f);
        float3 lighting = (float3)(0.0f, 0.0f, 0.0f);
        bool accumulateShadowmask = ((radeonRaysExpansionPass == kRRExpansionPass_direct && useShadowmask) || radeonRaysExpansionPass == kRRExpansionPass_indirect);
        for (int i = 0; i < numRays; ++i)
        {
            if(accumulateShadowmask)
                shadowMask += shadowmaskAoValidityExpandedBuffer[raysOffset + i];
            lighting += lightingExpandedBuffer[raysOffset + i];
        }

        if (radeonRaysExpansionPass == kRRExpansionPass_direct)
        {
            if(useShadowmask)
                INDEX_SAFE(outputShadowmaskFromDirectBuffer, originalTexelIndex) += shadowMask;
            INDEX_SAFE(outputDirectLightingBuffer, originalTexelIndex).xyz += lighting;
        }
        else if (radeonRaysExpansionPass == kRRExpansionPass_environment)
        {
            INDEX_SAFE(outputEnvironmentLightingBuffer, originalTexelIndex).xyz += lighting;
        }
        else
        {
            KERNEL_ASSERT(radeonRaysExpansionPass == kRRExpansionPass_indirect);
            INDEX_SAFE(outputIndirectLightingBuffer, originalTexelIndex).xyz += lighting;
            if(useAo)
                INDEX_SAFE(outputAoBuffer, originalTexelIndex) += shadowMask.x;
            INDEX_SAFE(outputValidityBuffer, originalTexelIndex) += shadowMask.y;
        }

        if (lightmapMode == LIGHTMAPMODE_DIRECTIONAL)
        {
            float4 directionality = (float4)(0.0f, 0.0f, 0.0f, 0.0f);
            for (int i = 0; i < numRays; ++i)
            {
                directionality += directionalExpandedBuffer[raysOffset + i];
            }

            if (radeonRaysExpansionPass == kRRExpansionPass_direct)
            {
                INDEX_SAFE(outputDirectionalFromDirectBuffer, originalTexelIndex) += directionality;
            }
            else
            {
                INDEX_SAFE(outputDirectionalFromGiBuffer, originalTexelIndex) += directionality;
            }
        }
#endif
    }
}


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\OpenCL\kernels\expansionAndGathering.cl---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\OpenCL\kernels\fillBuffer.cl---------------


#include "commonCL.h"

#define FILLBUFFER(TYPE_) \
__kernel void fillBuffer_##TYPE_( \
    __global TYPE_* buffer,       \
    TYPE_ value,                  \
    int bufferSize                \
)                                 \
{                                 \
    int idx = get_global_id(0);   \
                                  \
    if (idx < bufferSize)         \
        buffer[idx] = value;      \
}

FILLBUFFER(float)
FILLBUFFER(float2)
FILLBUFFER(float4)
FILLBUFFER(Vector3f_storage)
FILLBUFFER(int)
FILLBUFFER(uint)
FILLBUFFER(uchar)
FILLBUFFER(uchar4)
FILLBUFFER(LightSample)
FILLBUFFER(LightBuffer)
FILLBUFFER(MeshDataOffsets)
FILLBUFFER(MaterialTextureProperties)
FILLBUFFER(ray)
FILLBUFFER(Matrix4x4)
FILLBUFFER(Intersection)
FILLBUFFER(OpenCLKernelAssert)
FILLBUFFER(ConvergenceOutputData)
FILLBUFFER(PackedNormalOctQuad)
FILLBUFFER(ExpandedTexelInfo)
FILLBUFFER(SampleDescription)
FILLBUFFER(PowerSamplingStat)


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\OpenCL\kernels\fillBuffer.cl---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\OpenCL\kernels\intersectBvh.cl---------------


/**********************************************************************
Copyright (c) 2016 Advanced Micro Devices, Inc. All rights reserved.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.  IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
********************************************************************/

//UNITY++
//Source: https://github.com/GPUOpen-LibrariesAndSDKs/RadeonRays_SDK/blob/master/RadeonRays/src/kernels/CL/intersect_bvh2_lds.cl

/*************************************************************************
EXTENSIONS
**************************************************************************/
#ifdef AMD_MEDIA_OPS
#pragma OPENCL EXTENSION cl_amd_media_ops2 : enable
#endif //! AMD_MEDIA_OPS
//UNITY--
/*************************************************************************
INCLUDES
**************************************************************************/
//UNITY++
#include "commonCL.h"
#include "textureFetch.h"
//UNITY--

/*************************************************************************
TYPE DEFINITIONS
**************************************************************************/

#define INVALID_ADDR 0xffffffffu
#define INTERNAL_NODE(node) (GetAddrLeft(node) != INVALID_ADDR)

//UNITY++
#define GROUP_SIZE INTERSECT_BVH_WORKGROUPSIZE
//UNITY--
#define STACK_SIZE 32
#define LDS_STACK_SIZE 16

// BVH node
typedef struct
{
    float4 aabb_left_min_or_v0_and_addr_left;
    float4 aabb_left_max_or_v1_and_mesh_id;
    float4 aabb_right_min_or_v2_and_addr_right;
    float4 aabb_right_max_and_prim_id;

} bvh_node;

//UNITY++
/*************************************************************************
HELPER FUNCTIONS
**************************************************************************/
//UNITY--

#define GetAddrLeft(node)   as_uint((node).aabb_left_min_or_v0_and_addr_left.w)
#define GetAddrRight(node)  as_uint((node).aabb_right_min_or_v2_and_addr_right.w)
#define GetMeshId(node)     as_uint((node).aabb_left_max_or_v1_and_mesh_id.w)
#define GetPrimId(node)     as_uint((node).aabb_right_max_and_prim_id.w)

//UNITY++
inline float min3(float a, float b, float c)
{
#ifdef AMD_MEDIA_OPS
    return amd_min3(a, b, c);
#else //! AMD_MEDIA_OPS
    return min(min(a, b), c);
#endif //! AMD_MEDIA_OPS
}

inline float max3(float a, float b, float c)
{
#ifdef AMD_MEDIA_OPS
    return amd_max3(a, b, c);
#else //! AMD_MEDIA_OPS
    return max(max(a, b), c);
#endif //! AMD_MEDIA_OPS
}
//UNITY--

inline float2 fast_intersect_bbox2(float3 pmin, float3 pmax, float3 invdir, float3 oxinvdir, float t_max)
{
    const float3 f = mad(pmax.xyz, invdir, oxinvdir);
    const float3 n = mad(pmin.xyz, invdir, oxinvdir);
    const float3 tmax = max(f, n);
    const float3 tmin = min(f, n);
    const float t1 = min(min3(tmax.x, tmax.y, tmax.z), t_max);
    const float t0 = max(max3(tmin.x, tmin.y, tmin.z), 0.f);
    return (float2)(t0, t1);
}

//UNITY++
// Intersect ray against a triangle and return intersection interval value if it is in
// (0, t_max], return t_max otherwise.
inline float fast_intersect_triangle(ray r, float3 v1, float3 v2, float3 v3, float t_max)
{
    float3 const e1 = v2 - v1;
    float3 const e2 = v3 - v1;
    float3 const s1 = cross(r.d.xyz, e2);

#ifdef USE_SAFE_MATH
    float const invd = 1.f / dot(s1, e1);
#else //! USE_SAFE_MATH
    float const invd = native_recip(dot(s1, e1));
#endif //! USE_SAFE_MATH

    float3 const d = r.o.xyz - v1;
    float const b1 = dot(d, s1) * invd;
    float3 const s2 = cross(d, e1);
    float const b2 = dot(r.d.xyz, s2) * invd;
    float const temp = dot(e2, s2) * invd;

    if (b1 < 0.f || b1 > 1.f || b2 < 0.f || b1 + b2 > 1.f || temp < 0.f || temp > t_max)
    {
        return t_max;
    }
    else
    {
        return temp;
    }
}

inline int ray_is_active(ray const* r)
{
    return r->extra.y;
}

inline float3 safe_invdir(ray r)
{
    float const dirx = r.d.x;
    float const diry = r.d.y;
    float const dirz = r.d.z;
    float const ooeps = 1e-8;
    float3 invdir;
    invdir.x = 1.0f / (fabs(dirx) > ooeps ? dirx : copysign(ooeps, dirx));
    invdir.y = 1.0f / (fabs(diry) > ooeps ? diry : copysign(ooeps, diry));
    invdir.z = 1.0f / (fabs(dirz) > ooeps ? dirz : copysign(ooeps, dirz));
    return invdir;
}

// Given a point in triangle plane, calculate its barycentrics
inline float2 triangle_calculate_barycentrics(float3 p, float3 v1, float3 v2, float3 v3)
{
    float3 const e1 = v2 - v1;
    float3 const e2 = v3 - v1;
    float3 const e = p - v1;
    float const d00 = dot(e1, e1);
    float const d01 = dot(e1, e2);
    float const d11 = dot(e2, e2);
    float const d20 = dot(e, e1);
    float const d21 = dot(e, e2);

#ifdef USE_SAFE_MATH
    float const invdenom = 1.0f / (d00 * d11 - d01 * d01);
#else //! USE_SAFE_MATH
    float const invdenom = native_recip(d00 * d11 - d01 * d01);
#endif //! USE_SAFE_MATH

    float const b1 = (d11 * d20 - d01 * d21) * invdenom;
    float const b2 = (d00 * d21 - d01 * d20) * invdenom;

    return (float2)(b1, b2);
}

/*************************************************************************
KERNELS
**************************************************************************/

__kernel void clearIntersectionBuffer(
    OUTPUT_BUFFER(00, Intersection, pathIntersectionsCompactedBuffer)
    KERNEL_VALIDATOR_BUFFERS_DEF
)
{
    int idx = get_global_id(0);
    INDEX_SAFE(pathIntersectionsCompactedBuffer, idx).primid = MISS_MARKER;
    INDEX_SAFE(pathIntersectionsCompactedBuffer, idx).shapeid = MISS_MARKER;
    INDEX_SAFE(pathIntersectionsCompactedBuffer, idx).uvwt = (float4)(0.0f, 0.0f, 0.0f, 0.0f);
}

__kernel void clearOcclusionBuffer(
    OUTPUT_BUFFER(00,float4, lightOcclusionCompactedBuffer)
    KERNEL_VALIDATOR_BUFFERS_DEF
)
{
    int idx = get_global_id(0);
    INDEX_SAFE(lightOcclusionCompactedBuffer, idx) = (float4)(1.0f, 1.0f, 1.0f, 1.0f);
}
//UNITY--

__attribute__((reqd_work_group_size(GROUP_SIZE, 1, 1)))
__kernel void intersectWithTransmission(
    INPUT_BUFFER( 00, bvh_node,                  nodes),
    INPUT_BUFFER( 01, ray,                       pathRaysCompactedBuffer_0),
    INPUT_BUFFER( 02, uint,                      activePathCountBuffer_0),
    OUTPUT_BUFFER(03, uint,                      bvhStackBuffer),
    OUTPUT_BUFFER(04, Intersection,              pathIntersectionsCompactedBuffer),
//UNITY++
    OUTPUT_BUFFER(05, uint,                      transparentPathRayIndicesCompactedBuffer),
    OUTPUT_BUFFER(06, uint,                      transparentPathRayIndicesCompactedCountBuffer),
    OUTPUT_BUFFER(07, uint,                      totalRaysCastCountBuffer),
    INPUT_BUFFER( 08, MaterialTextureProperties, instanceIdToTransmissionTextureProperties),
    INPUT_BUFFER( 09, float4,                    instanceIdToTransmissionTextureSTs),
    INPUT_BUFFER( 10, MeshDataOffsets,           instanceIdToMeshDataOffsets),
    INPUT_BUFFER( 11, uchar4,                    dynarg_texture_buffer),
    INPUT_BUFFER( 12, uint,                      geometryIndicesBuffer),
    INPUT_BUFFER( 13, float2,                    geometryUV0sBuffer),
    INPUT_VALUE(  14, int,                       lightmapSize),
    INPUT_VALUE(  15, int,                       bounce),
    INPUT_VALUE(  16, int,                       superSamplingMultiplier),
    INPUT_BUFFER( 17, float,                     goldenSample_buffer),
    INPUT_BUFFER( 18, uint,                      sobol_buffer),
    INPUT_BUFFER( 19, SampleDescription,         sampleDescriptionsExpandedBuffer)
    KERNEL_VALIDATOR_BUFFERS_DEF
)
{
    __local uint numTransparentRaySharedMem;
    if (get_local_id(0) == 0)
        numTransparentRaySharedMem = 0;
    barrier(CLK_LOCAL_MEM_FENCE);

    bool atLeastATransparentMaterialWasHit = false;
    __local uint lds_stack[GROUP_SIZE * LDS_STACK_SIZE];
//UNITY--

    uint index = get_global_id(0);
    uint local_index = get_local_id(0);

    // Handle only working subset
    if (index < INDEX_SAFE(activePathCountBuffer_0, 0))
    {
        const ray my_ray = INDEX_SAFE(pathRaysCompactedBuffer_0, index);

        if (ray_is_active(&my_ray))
        {
            const float3 invDir = safe_invdir(my_ray);
            const float3 oxInvDir = -my_ray.o.xyz * invDir;

            // Intersection parametric distance
            float closest_t = my_ray.o.w;

            // Current node address
            uint addr = 0;
            // Current closest address
            uint closest_addr = INVALID_ADDR;

            uint stack_bottom = STACK_SIZE * index;
            uint sptr = stack_bottom;
            uint lds_stack_bottom = local_index * LDS_STACK_SIZE;
            uint lds_sptr = lds_stack_bottom;

            KERNEL_ASSERT(lds_sptr < GROUP_SIZE * LDS_STACK_SIZE);
            lds_stack[lds_sptr++] = INVALID_ADDR;

            //UNITY++
            int  sampleDimension = bounce * PLM_MAX_NUM_SOBOL_DIMENSIONS_PER_BOUNCE;
            const int rayLodLevel = UnpackLODMask(my_ray.extra.x);
            const int rayLodGroup = UnpackLODGroup(my_ray.extra.x);
            int hitLodParam = my_ray.extra.x;
            //UNITY--

            while (addr != INVALID_ADDR)
            {
                const bvh_node node = nodes[addr];

                if (INTERNAL_NODE(node))
                {
                    float2 s0 = fast_intersect_bbox2(
                        node.aabb_left_min_or_v0_and_addr_left.xyz,
                        node.aabb_left_max_or_v1_and_mesh_id.xyz,
                        invDir, oxInvDir, closest_t);
                    float2 s1 = fast_intersect_bbox2(
                        node.aabb_right_min_or_v2_and_addr_right.xyz,
                        node.aabb_right_max_and_prim_id.xyz,
                        invDir, oxInvDir, closest_t);

                    bool traverse_c0 = (s0.x <= s0.y);
                    bool traverse_c1 = (s1.x <= s1.y);
                    bool c1first = traverse_c1 && (s0.x > s1.x);

                    if (traverse_c0 || traverse_c1)
                    {
                        uint deferred = INVALID_ADDR;

                        if (c1first || !traverse_c0)
                        {
                            addr = GetAddrRight(node);
                            deferred = GetAddrLeft(node);
                        }
                        else
                        {
                            addr = GetAddrLeft(node);
                            deferred = GetAddrRight(node);
                        }

                        if (traverse_c0 && traverse_c1)
                        {
                            if (lds_sptr - lds_stack_bottom >= LDS_STACK_SIZE)
                            {
                                for (int i = 1; i < LDS_STACK_SIZE; ++i)
                                {
                                    KERNEL_ASSERT(lds_stack_bottom + i < GROUP_SIZE * LDS_STACK_SIZE);
                                    INDEX_SAFE(bvhStackBuffer, sptr + i) = lds_stack[lds_stack_bottom + i];
                                }

                                sptr += LDS_STACK_SIZE;
                                lds_sptr = lds_stack_bottom + 1;
                            }

                            KERNEL_ASSERT(lds_sptr < GROUP_SIZE * LDS_STACK_SIZE);
                            lds_stack[lds_sptr++] = deferred;
                        }

                        continue;
                    }
                }
                else
                {
                    float t = fast_intersect_triangle(
                        my_ray,
                        node.aabb_left_min_or_v0_and_addr_left.xyz,
                        node.aabb_left_max_or_v1_and_mesh_id.xyz,
                        node.aabb_right_min_or_v2_and_addr_right.xyz,
                        closest_t);

                    if (t < closest_t)
                    {
//UNITY++
                        const int instanceId = GetMeshId(node) - 1;
                        const MaterialTextureProperties matProperty = INDEX_SAFE(instanceIdToTransmissionTextureProperties, instanceId);
                        const int instanceLODMask = UnpackLODMask(matProperty.lodInfo);
                        const int instanceLODGroup = UnpackLODGroup(matProperty.lodInfo);
                        const bool isInstanceHit = IsInstanceHit(instanceLODMask, instanceLODGroup, rayLodLevel, rayLodGroup);

                        if (isInstanceHit)
                        {
                            hitLodParam = (instanceLODMask & 1) ? ((1 << 24) | (NO_LOD_GROUP & ((1<<24)-1))) : hitLodParam;
                            // Evaluate whether we've hit a transparent material
                            bool useTransmission = GetMaterialProperty(matProperty, kMaterialInstanceProperties_UseTransmission);
                            if (useTransmission)
                            {
                                const float3 p = my_ray.o.xyz + t * my_ray.d.xyz;
                                const float2 barycentricCoord = triangle_calculate_barycentrics(
                                    p,
                                    node.aabb_left_min_or_v0_and_addr_left.xyz,
                                    node.aabb_left_max_or_v1_and_mesh_id.xyz,
                                    node.aabb_right_min_or_v2_and_addr_right.xyz);

                                const int primIndex = GetPrimId(node);
                                const float2 geometryUVs = GetUVsAtPrimitiveIntersection(instanceId, primIndex, barycentricCoord, instanceIdToMeshDataOffsets, geometryUV0sBuffer, geometryIndicesBuffer KERNEL_VALIDATOR_BUFFERS);
                                const float2 textureUVs = geometryUVs * INDEX_SAFE(instanceIdToTransmissionTextureSTs, instanceId).xy + INDEX_SAFE(instanceIdToTransmissionTextureSTs, instanceId).zw;
                                const float4 transmission = FetchUChar4TextureFromMaterialAndUVs(dynarg_texture_buffer, textureUVs, matProperty, false, false KERNEL_VALIDATOR_BUFFERS);
                                const float averageTransmission = dot(transmission.xyz, kAverageFactors);
                                const int expandedRayIndex = Ray_GetSampleDescriptionIndex(GET_PTR_SAFE(pathRaysCompactedBuffer_0, index));
                                const SampleDescription sampleDescription = INDEX_SAFE(sampleDescriptionsExpandedBuffer, expandedRayIndex);

                                float rnd = SobolSample(sampleDescription.currentSampleCount, sampleDimension, sobol_buffer KERNEL_VALIDATOR_BUFFERS);

                                int texel_x = sampleDescription.texelIndex % lightmapSize;
                                int texel_y = sampleDescription.texelIndex / lightmapSize;
                                rnd = ApplyCranleyPattersonRotation1D(rnd, texel_x, texel_y, lightmapSize, sampleDimension, goldenSample_buffer KERNEL_VALIDATOR_BUFFERS);

                                // NOTE: This is wrong! The probability of either reflecting or refracting a ray
                                // should depend on the Fresnel of the material. However, since we do not support
                                // any specularity in PVR there is currently no way to query this value, so for now
                                // we use the transmission (texture) albedo.
                                if (rnd >= averageTransmission)
                                {
                                    //Bounce of the transparent material, the material is considered opaque.
                                    closest_t = t;
                                    closest_addr = addr;
                                }
                                else
                                {
                                    //Thought the transparent material, attenuation will need to be collected in an additional pass (specialized occlusion pass)
                                    atLeastATransparentMaterialWasHit = true;
                                    ++sampleDimension;
                                    sampleDimension %= SOBOL_MATRICES_COUNT;
                                }
                            }
                            else
                            {
    //UNITY--
                                closest_t = t;
                                closest_addr = addr;
                            }
                        }
                    }
                }

                KERNEL_ASSERT(lds_sptr - 1 < GROUP_SIZE * LDS_STACK_SIZE);
                addr = lds_stack[--lds_sptr];

                if (addr == INVALID_ADDR && sptr > stack_bottom)
                {
                    sptr -= LDS_STACK_SIZE;
                    for (int i = 1; i < LDS_STACK_SIZE; ++i)
                    {
                        KERNEL_ASSERT(lds_stack_bottom + i < GROUP_SIZE * LDS_STACK_SIZE);
                        lds_stack[lds_stack_bottom + i] = INDEX_SAFE(bvhStackBuffer, sptr + i);
                    }

                    lds_sptr = lds_stack_bottom + LDS_STACK_SIZE - 1;
                    KERNEL_ASSERT(lds_sptr < GROUP_SIZE * LDS_STACK_SIZE);
                    addr = lds_stack[lds_sptr];
                }
            }

            // Check if we have found an intersection
            if (closest_addr != INVALID_ADDR)
            {
                // Calculate hit position
                const bvh_node node = nodes[closest_addr];
                const float3 p = my_ray.o.xyz + closest_t * my_ray.d.xyz;

                // Calculate barycentric coordinates
                const float2 uv = triangle_calculate_barycentrics(
                    p,
                    node.aabb_left_min_or_v0_and_addr_left.xyz,
                    node.aabb_left_max_or_v1_and_mesh_id.xyz,
                    node.aabb_right_min_or_v2_and_addr_right.xyz);

                // Update hit information
                INDEX_SAFE(pathIntersectionsCompactedBuffer, index).primid = GetPrimId(node);
                INDEX_SAFE(pathIntersectionsCompactedBuffer, index).shapeid = GetMeshId(node);
                INDEX_SAFE(pathIntersectionsCompactedBuffer, index).uvwt = (float4)(uv.x, uv.y, 0.0f, closest_t);
                INDEX_SAFE(pathIntersectionsCompactedBuffer, index).padding0 = hitLodParam;
            }
            else
            {
                // Miss here
                INDEX_SAFE(pathIntersectionsCompactedBuffer, index).primid = MISS_MARKER;
                INDEX_SAFE(pathIntersectionsCompactedBuffer, index).shapeid = MISS_MARKER;
                INDEX_SAFE(pathIntersectionsCompactedBuffer, index).padding0 = hitLodParam;
            }
        }
    }

//UNITY++
    //Compact transparent ray that will be process further via the adjustPathThroughputFromIntersection kernel (see below)
    int compactedTransparentRayIndex = -1;
    if (atLeastATransparentMaterialWasHit)
        compactedTransparentRayIndex = atomic_inc(&numTransparentRaySharedMem);

    barrier(CLK_LOCAL_MEM_FENCE);
    if (get_local_id(0) == 0)
    {
        if (numTransparentRaySharedMem)
        {
            atomic_add(GET_PTR_SAFE(totalRaysCastCountBuffer, 0), numTransparentRaySharedMem);
        }
        numTransparentRaySharedMem = atomic_add(GET_PTR_SAFE(transparentPathRayIndicesCompactedCountBuffer, 0), numTransparentRaySharedMem);
    }
    barrier(CLK_LOCAL_MEM_FENCE);
    if (atLeastATransparentMaterialWasHit)
    {
        KERNEL_ASSERT(compactedTransparentRayIndex >= 0);
        INDEX_SAFE(transparentPathRayIndicesCompactedBuffer, numTransparentRaySharedMem + compactedTransparentRayIndex) = index;
    }
//UNITY--
}

//UNITY++
// This kernel is a copy of the occlusion one, but specialized to collect transmission in the ray path.
__attribute__((reqd_work_group_size(GROUP_SIZE, 1, 1)))
__kernel void adjustPathThroughputFromIntersection(
//UNITY--
    INPUT_BUFFER( 00, bvh_node,                  nodes),
    INPUT_BUFFER( 01, ray,                       pathRaysCompactedBuffer_0),
    INPUT_BUFFER( 02, uint,                      activePathCountBuffer_0),
    OUTPUT_BUFFER(03, uint,                      bvhStackBuffer),
    //UNITY++
    OUTPUT_BUFFER(04, float4,                    pathThroughputExpandedBuffer),
    INPUT_BUFFER( 05, Intersection,              pathIntersectionsCompactedBuffer),
    INPUT_BUFFER( 06, uint,                      transparentPathRayIndicesCompactedBuffer),
    INPUT_BUFFER( 07, uint,                      transparentPathRayIndicesCompactedCountBuffer),
    INPUT_BUFFER( 08, MaterialTextureProperties, instanceIdToTransmissionTextureProperties),
    INPUT_BUFFER( 09, float4,                    instanceIdToTransmissionTextureSTs),
    INPUT_BUFFER( 10, MeshDataOffsets,           instanceIdToMeshDataOffsets),
    INPUT_BUFFER( 11, uchar4,                    dynarg_texture_buffer),
    INPUT_BUFFER( 12, uint,                      geometryIndicesBuffer),
    INPUT_BUFFER( 13, float2,                    geometryUV0sBuffer)
    KERNEL_VALIDATOR_BUFFERS_DEF
    //UNITY--
)
{
    uint index = get_global_id(0);
    uint local_index = get_local_id(0);

    //UNITY++
    __local uint lds_stack[GROUP_SIZE * LDS_STACK_SIZE];
    //UNITY--

    // Handle only working subset
    if (index < INDEX_SAFE(transparentPathRayIndicesCompactedCountBuffer, 0))
    {
        const int compactedRayIndex = INDEX_SAFE(transparentPathRayIndicesCompactedBuffer, index);
        const ray my_ray = INDEX_SAFE(pathRaysCompactedBuffer_0, compactedRayIndex);
        KERNEL_ASSERT(ray_is_active(&my_ray));
        //UNITY++
        const Intersection my_intersection = INDEX_SAFE(pathIntersectionsCompactedBuffer, compactedRayIndex);

        const int rayLodLevel = UnpackLODMask(my_ray.extra.x);
        const int rayLodGroup = UnpackLODGroup(my_ray.extra.x);
        //UNITY--

        const float3 invDir = safe_invdir(my_ray);
        const float3 oxInvDir = -my_ray.o.xyz * invDir;

        // Current node address
        uint addr = 0;
        //UNITY++
        // Intersection distance or ray distance if the ray did not stop on a geometry.
        const float closest_t = (my_intersection.primid == MISS_MARKER)? my_ray.o.w : my_intersection.uvwt.w;
        //UNITY--

        uint stack_bottom = STACK_SIZE * index;
        uint sptr = stack_bottom;
        uint lds_stack_bottom = local_index * LDS_STACK_SIZE;
        uint lds_sptr = lds_stack_bottom;

        KERNEL_ASSERT(lds_sptr < GROUP_SIZE * LDS_STACK_SIZE);
        lds_stack[lds_sptr++] = INVALID_ADDR;

        while (addr != INVALID_ADDR)
        {
            const bvh_node node = nodes[addr];

            if (INTERNAL_NODE(node))
            {
                float2 s0 = fast_intersect_bbox2(
                    node.aabb_left_min_or_v0_and_addr_left.xyz,
                    node.aabb_left_max_or_v1_and_mesh_id.xyz,
                    invDir, oxInvDir, closest_t);
                float2 s1 = fast_intersect_bbox2(
                    node.aabb_right_min_or_v2_and_addr_right.xyz,
                    node.aabb_right_max_and_prim_id.xyz,
                    invDir, oxInvDir, closest_t);

                bool traverse_c0 = (s0.x <= s0.y);
                bool traverse_c1 = (s1.x <= s1.y);
                bool c1first = traverse_c1 && (s0.x > s1.x);

                if (traverse_c0 || traverse_c1)
                {
                    uint deferred = INVALID_ADDR;

                    if (c1first || !traverse_c0)
                    {
                        addr = GetAddrRight(node);
                        deferred = GetAddrLeft(node);
                    }
                    else
                    {
                        addr = GetAddrLeft(node);
                        deferred = GetAddrRight(node);
                    }

                    if (traverse_c0 && traverse_c1)
                    {
                        if (lds_sptr - lds_stack_bottom >= LDS_STACK_SIZE)
                        {
                            for (int i = 1; i < LDS_STACK_SIZE; ++i)
                            {
                                KERNEL_ASSERT(lds_stack_bottom + i < GROUP_SIZE * LDS_STACK_SIZE);
                                INDEX_SAFE(bvhStackBuffer, sptr + i) = lds_stack[lds_stack_bottom + i];
                            }

                            sptr += LDS_STACK_SIZE;
                            lds_sptr = lds_stack_bottom + 1;
                        }

                        KERNEL_ASSERT(lds_sptr < GROUP_SIZE * LDS_STACK_SIZE);
                        lds_stack[lds_sptr++] = deferred;
                    }

                    continue;
                }
            }
            else
            {
                float t = fast_intersect_triangle(
                    my_ray,
                    node.aabb_left_min_or_v0_and_addr_left.xyz,
                    node.aabb_left_max_or_v1_and_mesh_id.xyz,
                    node.aabb_right_min_or_v2_and_addr_right.xyz,
                    closest_t);

                if (t < closest_t)
                {
                    //UNITY++
                    const int instanceId = GetMeshId(node) - 1;
                    const MaterialTextureProperties matProperty = INDEX_SAFE(instanceIdToTransmissionTextureProperties, instanceId);
                    const int instanceLODMask = UnpackLODMask(matProperty.lodInfo);
                    const int instanceLODGroup = UnpackLODGroup(matProperty.lodInfo);
                    const bool isInstanceHit = IsInstanceHit(instanceLODMask, instanceLODGroup, rayLodLevel, rayLodGroup);
                    if (isInstanceHit)
                    {
                        // Evaluate transparent material attenuation
                        bool useTransmission = GetMaterialProperty(matProperty, kMaterialInstanceProperties_UseTransmission);
                        if (useTransmission)
                        {
                            const float3 p = my_ray.o.xyz + t * my_ray.d.xyz;
                            const float2 barycentricCoord = triangle_calculate_barycentrics(
                                p,
                                node.aabb_left_min_or_v0_and_addr_left.xyz,
                                node.aabb_left_max_or_v1_and_mesh_id.xyz,
                                node.aabb_right_min_or_v2_and_addr_right.xyz);

                            const int primIndex = GetPrimId(node);
                            const float2 geometryUVs = GetUVsAtPrimitiveIntersection(instanceId, primIndex, barycentricCoord, instanceIdToMeshDataOffsets, geometryUV0sBuffer, geometryIndicesBuffer KERNEL_VALIDATOR_BUFFERS);
                            const float2 textureUVs = geometryUVs * INDEX_SAFE(instanceIdToTransmissionTextureSTs, instanceId).xy + INDEX_SAFE(instanceIdToTransmissionTextureSTs, instanceId).zw;
                            const float4 transmission = FetchUChar4TextureFromMaterialAndUVs(dynarg_texture_buffer, textureUVs, matProperty, false, false KERNEL_VALIDATOR_BUFFERS);
                            const float averageTransmission = dot(transmission.xyz, kAverageFactors);
                            const int expandedRayIndex = Ray_GetSampleDescriptionIndex(GET_PTR_SAFE(pathRaysCompactedBuffer_0, compactedRayIndex));
                            INDEX_SAFE(pathThroughputExpandedBuffer, expandedRayIndex) *= (float4)(transmission.x, transmission.y, transmission.z, averageTransmission);
                        }
                    }
                    //UNITY--
                }
            }
            KERNEL_ASSERT(lds_sptr - 1 < GROUP_SIZE * LDS_STACK_SIZE);
            addr = lds_stack[--lds_sptr];

            if (addr == INVALID_ADDR && sptr > stack_bottom)
            {
                sptr -= LDS_STACK_SIZE;
                for (int i = 1; i < LDS_STACK_SIZE; ++i)
                {
                    KERNEL_ASSERT(lds_stack_bottom + i < GROUP_SIZE * LDS_STACK_SIZE);
                    lds_stack[lds_stack_bottom + i] = INDEX_SAFE(bvhStackBuffer, sptr + i);
                }

                lds_sptr = lds_stack_bottom + LDS_STACK_SIZE - 1;
                KERNEL_ASSERT(lds_sptr < GROUP_SIZE * LDS_STACK_SIZE);
                addr = lds_stack[lds_sptr];
            }
//UNITY++
        }
//UNITY--
    }
}

__attribute__((reqd_work_group_size(GROUP_SIZE, 1, 1)))
__kernel void occludedWithTransmission(
    INPUT_BUFFER( 00, bvh_node,                  nodes),
    INPUT_BUFFER( 01, ray,                       lightRaysCompactedBuffer),
    INPUT_BUFFER( 02, uint,                      lightRaysCountBuffer),
    OUTPUT_BUFFER(03, uint,                      bvhStackBuffer),
//UNITY++
    OUTPUT_BUFFER(04, float4,                    lightOcclusionCompactedBuffer),
    INPUT_BUFFER( 05, MaterialTextureProperties, instanceIdToTransmissionTextureProperties),
    INPUT_BUFFER( 06, float4,                    instanceIdToTransmissionTextureSTs),
    INPUT_BUFFER( 07, MeshDataOffsets,           instanceIdToMeshDataOffsets),
    INPUT_BUFFER( 08, uchar4,                    dynarg_texture_buffer),
    INPUT_BUFFER( 09, uint,                      geometryIndicesBuffer),
    INPUT_BUFFER( 10, float2,                    geometryUV0sBuffer),
    INPUT_VALUE(  11, int,                       useCastShadowsFlag)
    KERNEL_VALIDATOR_BUFFERS_DEF
//UNITY--
)
{
    uint index = get_global_id(0);
    uint local_index = get_local_id(0);
//UNITY++
    __local uint lds_stack[GROUP_SIZE * LDS_STACK_SIZE];
//UNITY--

    // Handle only working subset
    if (index < INDEX_SAFE(lightRaysCountBuffer, 0))
    {
//UNITY++
        // Initialize memory
        INDEX_SAFE(lightOcclusionCompactedBuffer, index) = (float4)(1.0f, 1.0f, 1.0f, 1.0f);
//UNITY--

        const ray my_ray = INDEX_SAFE(lightRaysCompactedBuffer, index);

        if (ray_is_active(&my_ray))
        {
            //UNITY++
            const int rayLodLevel = UnpackLODMask(my_ray.extra.x);
            const int rayLodGroup = UnpackLODGroup(my_ray.extra.x);
            //UNITY--
            const float3 invDir = safe_invdir(my_ray);
            const float3 oxInvDir = -my_ray.o.xyz * invDir;

            // Current node address
            uint addr = 0;
            // Intersection parametric distance
            const float closest_t = my_ray.o.w;

            uint stack_bottom = STACK_SIZE * index;
            uint sptr = stack_bottom;
            uint lds_stack_bottom = local_index * LDS_STACK_SIZE;
            uint lds_sptr = lds_stack_bottom;

            KERNEL_ASSERT(lds_sptr < GROUP_SIZE * LDS_STACK_SIZE);
            lds_stack[lds_sptr++] = INVALID_ADDR;

            while (addr != INVALID_ADDR)
            {
                const bvh_node node = nodes[addr];

                if (INTERNAL_NODE(node))
                {
                    float2 s0 = fast_intersect_bbox2(
                        node.aabb_left_min_or_v0_and_addr_left.xyz,
                        node.aabb_left_max_or_v1_and_mesh_id.xyz,
                        invDir, oxInvDir, closest_t);
                    float2 s1 = fast_intersect_bbox2(
                        node.aabb_right_min_or_v2_and_addr_right.xyz,
                        node.aabb_right_max_and_prim_id.xyz,
                        invDir, oxInvDir, closest_t);

                    bool traverse_c0 = (s0.x <= s0.y);
                    bool traverse_c1 = (s1.x <= s1.y);
                    bool c1first = traverse_c1 && (s0.x > s1.x);

                    if (traverse_c0 || traverse_c1)
                    {
                        uint deferred = INVALID_ADDR;

                        if (c1first || !traverse_c0)
                        {
                            addr = GetAddrRight(node);
                            deferred = GetAddrLeft(node);
                        }
                        else
                        {
                            addr = GetAddrLeft(node);
                            deferred = GetAddrRight(node);
                        }

                        if (traverse_c0 && traverse_c1)
                        {
                            if (lds_sptr - lds_stack_bottom >= LDS_STACK_SIZE)
                            {
                                for (int i = 1; i < LDS_STACK_SIZE; ++i)
                                {
                                    KERNEL_ASSERT(lds_stack_bottom + i < GROUP_SIZE * LDS_STACK_SIZE);
                                    INDEX_SAFE(bvhStackBuffer, sptr + i) = lds_stack[lds_stack_bottom + i];
                                }

                                sptr += LDS_STACK_SIZE;
                                lds_sptr = lds_stack_bottom + 1;
                            }

                            KERNEL_ASSERT(lds_sptr < GROUP_SIZE * LDS_STACK_SIZE);
                            lds_stack[lds_sptr++] = deferred;
                        }

                        continue;
                    }
                }
                else
                {
                    float t = fast_intersect_triangle(
                        my_ray,
                        node.aabb_left_min_or_v0_and_addr_left.xyz,
                        node.aabb_left_max_or_v1_and_mesh_id.xyz,
                        node.aabb_right_min_or_v2_and_addr_right.xyz,
                        closest_t);

                    if (t < closest_t)
                    {
//UNITY++
                        const int instanceId = GetMeshId(node) - 1;
                        const MaterialTextureProperties matProperty = INDEX_SAFE(instanceIdToTransmissionTextureProperties, instanceId);
                        const int instanceLODMask = UnpackLODMask(matProperty.lodInfo);
                        const int instanceLODGroup = UnpackLODGroup(matProperty.lodInfo);
                        const bool isInstanceHit = IsInstanceHit(instanceLODMask, instanceLODGroup, rayLodLevel, rayLodGroup);

                        if (isInstanceHit)
                        {
                            bool castShadows = (useCastShadowsFlag ? GetMaterialProperty(matProperty, kMaterialInstanceProperties_CastShadows) : true);
                            if (castShadows)
                            {
                                // Evaluate whether we've hit a transparent material
                                bool useTransmission = GetMaterialProperty(matProperty, kMaterialInstanceProperties_UseTransmission);
                                if (useTransmission)
                                {
                                    const float3 p = my_ray.o.xyz + t * my_ray.d.xyz;
                                    const float2 barycentricCoord = triangle_calculate_barycentrics(
                                        p,
                                        node.aabb_left_min_or_v0_and_addr_left.xyz,
                                        node.aabb_left_max_or_v1_and_mesh_id.xyz,
                                        node.aabb_right_min_or_v2_and_addr_right.xyz);

                                    const int primIndex = GetPrimId(node);
                                    const float2 geometryUVs = GetUVsAtPrimitiveIntersection(instanceId, primIndex, barycentricCoord, instanceIdToMeshDataOffsets, geometryUV0sBuffer, geometryIndicesBuffer KERNEL_VALIDATOR_BUFFERS);
                                    const float2 textureUVs = geometryUVs * INDEX_SAFE(instanceIdToTransmissionTextureSTs, instanceId).xy + INDEX_SAFE(instanceIdToTransmissionTextureSTs, instanceId).zw;
                                    const float4 transmission = FetchUChar4TextureFromMaterialAndUVs(dynarg_texture_buffer, textureUVs, matProperty, false, false KERNEL_VALIDATOR_BUFFERS);
                                    const float averageTransmission = dot(transmission.xyz, kAverageFactors);
                                    INDEX_SAFE(lightOcclusionCompactedBuffer, index) *= (float4)(transmission.x, transmission.y, transmission.z, averageTransmission);
                                    if (INDEX_SAFE(lightOcclusionCompactedBuffer, index).w < TRANSMISSION_THRESHOLD)
                                        return;// fully occluded
                                }
                                else
                                {
                                    INDEX_SAFE(lightOcclusionCompactedBuffer, index) = (float4)(0.0f, 0.0f, 0.0f, 0.0f);
                                    return;// fully occluded
                                }
                            }
                        }
//UNITY--
                    }
                }
                KERNEL_ASSERT(lds_sptr - 1 < GROUP_SIZE * LDS_STACK_SIZE);
                addr = lds_stack[--lds_sptr];

                if (addr == INVALID_ADDR && sptr > stack_bottom)
                {
                    sptr -= LDS_STACK_SIZE;
                    for (int i = 1; i < LDS_STACK_SIZE; ++i)
                    {
                        KERNEL_ASSERT(lds_stack_bottom + i < GROUP_SIZE * LDS_STACK_SIZE);
                        lds_stack[lds_stack_bottom + i] = INDEX_SAFE(bvhStackBuffer, sptr + i);
                    }

                    lds_sptr = lds_stack_bottom + LDS_STACK_SIZE - 1;
                    KERNEL_ASSERT(lds_sptr < GROUP_SIZE * LDS_STACK_SIZE);
                    addr = lds_stack[lds_sptr];
                }
            }
        }
    }
}


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\OpenCL\kernels\intersectBvh.cl---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\OpenCL\kernels\postProcessProbes.cl---------------


#include "commonCL.h"

#define kSphericalHarmonicsL2_CoeffCount 9
#define kSphericalHarmonicsL2_ColorChannelCount 3
#define kSphericalHarmonicsL2_FloatCount (kSphericalHarmonicsL2_CoeffCount * kSphericalHarmonicsL2_ColorChannelCount)

typedef struct _SphericalHarmonicsL2
{
    // Notation:
    // http://graphics.stanford.edu/papers/envmap/envmap.pdf
    //
    //                       [L00:  DC]
    //            [L1-1:  y] [L10:   z] [L11:   x]
    // [L2-2: xy] [L2-1: yz] [L20:  zz] [L21:  xz]  [L22:  xx - yy]
    //
    // 9 coefficients for R, G and B ordered:
    // {  L00, L1-1,  L10,  L11, L2-2, L2-1,  L20,  L21,  L22,  // red   channel
    //    L00, L1-1,  L10,  L11, L2-2, L2-1,  L20,  L21,  L22,  // blue  channel
    //    L00, L1-1,  L10,  L11, L2-2, L2-1,  L20,  L21,  L22 } // green channel
    float sh[kSphericalHarmonicsL2_FloatCount];
} SphericalHarmonicsL2;

#define L00 0
#define L1_1 1
#define L10 2
#define L11 3
#define L2_2 4
#define L2_1 5
#define L20 6
#define L21 7
#define L22 8

// aHat is from https://cseweb.ucsd.edu/~ravir/papers/envmap/envmap.pdf and is used to convert spherical radiance to irradiance.
#define aHat0 3.1415926535897932384626433832795028841971693993751058209749445923f // π
#define aHat1 2.0943951023931954923084289221863352561314462662500705473166297282f // 2π/3
#define aHat2 0.785398f // π/4 (see equation 8).

__kernel void ConvolveRadianceToIrradiance(
    //*** input ***
    INPUT_BUFFER(00, SphericalHarmonicsL2, radianceIn),
    INPUT_VALUE(01, int, probeCount),
    //*** output ***
    OUTPUT_BUFFER(02, SphericalHarmonicsL2, irradianceOut)
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    const uint probeIdx = get_global_id(0);
    KERNEL_ASSERT(probeIdx < probeCount);
    if (probeIdx >= probeCount)
        return;

    SphericalHarmonicsL2 radiance = INDEX_SAFE(radianceIn, probeIdx);
    SphericalHarmonicsL2 irradiance;
    for (int rgb = 0; rgb < 3; ++rgb)
    {
        const int rgbOffset = rgb * kSphericalHarmonicsL2_CoeffCount;

        irradiance.sh[rgbOffset + L00] =  radiance.sh[rgbOffset + L00] * aHat0;
        irradiance.sh[rgbOffset + L1_1] = radiance.sh[rgbOffset + L1_1] * aHat1;
        irradiance.sh[rgbOffset + L10] =  radiance.sh[rgbOffset + L10] * aHat1;
        irradiance.sh[rgbOffset + L11] =  radiance.sh[rgbOffset + L11] * aHat1;
        irradiance.sh[rgbOffset + L2_2] = radiance.sh[rgbOffset + L2_2] * aHat2;
        irradiance.sh[rgbOffset + L2_1] = radiance.sh[rgbOffset + L2_1] * aHat2;
        irradiance.sh[rgbOffset + L20] =  radiance.sh[rgbOffset + L20] * aHat2;
        irradiance.sh[rgbOffset + L21] =  radiance.sh[rgbOffset + L21] * aHat2;
        irradiance.sh[rgbOffset + L22] =  radiance.sh[rgbOffset + L22] * aHat2;
    }
    INDEX_SAFE(irradianceOut, probeIdx) = irradiance;
}

__kernel void ConvertToUnityFormat(
    //*** input ***
    INPUT_BUFFER(00, SphericalHarmonicsL2, irradianceIn),
    INPUT_VALUE(01, int, probeCount),
    //*** output ***
    OUTPUT_BUFFER(02, SphericalHarmonicsL2, irradianceOut)
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    const uint probeIdx = get_global_id(0);
    KERNEL_ASSERT(probeIdx < probeCount);
    if (probeIdx >= probeCount)
        return;

    float shY0Normalization = sqrt(1.0f / FLT_PI) / 2.0f;

    float shY1Normalization = sqrt(3.0f / FLT_PI) / 2.0f;

    float shY2_2Normalization = sqrt(15.0f / FLT_PI) / 2.0f;
    float shY2_1Normalization = shY2_2Normalization;
    float shY20Normalization = sqrt(5.0f / FLT_PI) / 4.0f;
    float shY21Normalization = shY2_2Normalization;
    float shY22Normalization = sqrt(15.0f / FLT_PI) / 4.0f;

    SphericalHarmonicsL2 irradiance = INDEX_SAFE(irradianceIn, probeIdx);
    SphericalHarmonicsL2 output;
    for (int rgb = 0; rgb < 3; ++rgb)
    {
        const int rgbOffset = rgb * kSphericalHarmonicsL2_CoeffCount;

        // See documentation IProbePostProcessor.ConvertToUnityFormat for an explanation of the steps below.

        // L0
        output.sh[rgbOffset + L00] = irradiance.sh[rgbOffset + L00];
        output.sh[rgbOffset + L00] *= shY0Normalization; // 1)
        output.sh[rgbOffset + L00] /= FLT_PI; // 2)

        // L1
        output.sh[rgbOffset + L1_1] = irradiance.sh[rgbOffset + L10]; // 3)
        output.sh[rgbOffset + L1_1] *= shY1Normalization; // 1)
        output.sh[rgbOffset + L1_1] /= FLT_PI; // 3 )

        output.sh[rgbOffset + L10] = irradiance.sh[rgbOffset + L11]; // 3)
        output.sh[rgbOffset + L10] *= shY1Normalization; // 1)
        output.sh[rgbOffset + L10] /= FLT_PI; // 2)

        output.sh[rgbOffset + L11] = irradiance.sh[rgbOffset + L1_1]; // 3)
        output.sh[rgbOffset + L11] *= shY1Normalization; // 1)
        output.sh[rgbOffset + L11] /= FLT_PI; // 2)

        // L2
        output.sh[rgbOffset + L2_2] = irradiance.sh[rgbOffset + L2_2];
        output.sh[rgbOffset + L2_2] *= shY2_2Normalization; // 1)
        output.sh[rgbOffset + L2_2] /= FLT_PI; // 2)

        output.sh[rgbOffset + L2_1] = irradiance.sh[rgbOffset + L2_1];
        output.sh[rgbOffset + L2_1] *= shY2_1Normalization; // 1)
        output.sh[rgbOffset + L2_1] /= FLT_PI; // 2)

        output.sh[rgbOffset + L20] = irradiance.sh[rgbOffset + L20];
        output.sh[rgbOffset + L20] *= shY20Normalization; // 1)
        output.sh[rgbOffset + L20] /= FLT_PI; // 2)

        output.sh[rgbOffset + L21] = irradiance.sh[rgbOffset + L21];
        output.sh[rgbOffset + L21] *= shY21Normalization; // 1)
        output.sh[rgbOffset + L21] /= FLT_PI; // 2)

        output.sh[rgbOffset + L22] = irradiance.sh[rgbOffset + L22];
        output.sh[rgbOffset + L22] *= shY22Normalization; // 1)
        output.sh[rgbOffset + L22] /= FLT_PI; // 2)
    }
    INDEX_SAFE(irradianceOut, probeIdx) = output;
}

__kernel void AddSphericalHarmonicsL2(
    //*** input ***
    INPUT_BUFFER( 00, SphericalHarmonicsL2, A),
    INPUT_BUFFER( 01, SphericalHarmonicsL2, B),
    INPUT_VALUE(  02, int, probeCount),
    //*** output ***
    OUTPUT_BUFFER(03, SphericalHarmonicsL2, Sum)
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    const uint probeIdx = get_global_id(0);
    KERNEL_ASSERT(probeIdx < probeCount);
    if (probeIdx >= probeCount)
        return;

    SphericalHarmonicsL2 a = INDEX_SAFE(A, probeIdx);
    SphericalHarmonicsL2 b = INDEX_SAFE(B, probeIdx);
    SphericalHarmonicsL2 sum;
    for (int i = 0; i < kSphericalHarmonicsL2_FloatCount; i++)
    {
        sum.sh[i] = a.sh[i] + b.sh[i];
    }
    INDEX_SAFE(Sum, probeIdx) = sum;
}

__kernel void ScaleSphericalHarmonicsL2(
    //*** input ***
    INPUT_BUFFER(00, SphericalHarmonicsL2, shIn),
    INPUT_VALUE(01, int, probeCount),
    INPUT_VALUE(02, float, scale),
    //*** output ***
    OUTPUT_BUFFER(03, SphericalHarmonicsL2, shOut)
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    const uint probeIdx = get_global_id(0);
    KERNEL_ASSERT(probeIdx < probeCount);
    if (probeIdx >= probeCount)
        return;

    SphericalHarmonicsL2 a = INDEX_SAFE(shIn, probeIdx);
    SphericalHarmonicsL2 scaled;
    for (int i = 0; i < kSphericalHarmonicsL2_FloatCount; i++)
    {
        scaled.sh[i] = a.sh[i] * scale;
    }
    INDEX_SAFE(shOut, probeIdx) = scaled;
}

__kernel void WindowSphericalHarmonicsL2(
    //*** input ***
    INPUT_BUFFER(00, SphericalHarmonicsL2, shIn),
    INPUT_VALUE(01, int, probeCount),
    //*** output ***
    OUTPUT_BUFFER(02, SphericalHarmonicsL2, shOut)
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    const uint probeIdx = get_global_id(0);
    KERNEL_ASSERT(probeIdx < probeCount);
    if (probeIdx >= probeCount)
        return;

    // Windowing constants from WindowDirectSH in SHDering.cpp
    float extraWindow[3] = { 1.0f, 0.922066f, 0.731864f };

    // Apply windowing: Essentially SHConv3 times the window constants
    SphericalHarmonicsL2 sh = INDEX_SAFE(shIn, probeIdx);
    for (int coefficientIndex = 0; coefficientIndex < kSphericalHarmonicsL2_CoeffCount; ++coefficientIndex)
    {
        float window;
        if (coefficientIndex == 0)
            window = extraWindow[0];
        else if (coefficientIndex < 4)
            window = extraWindow[1];
        else
            window = extraWindow[2];
        sh.sh[0 * kSphericalHarmonicsL2_CoeffCount + coefficientIndex] *= window;
        sh.sh[1 * kSphericalHarmonicsL2_CoeffCount + coefficientIndex] *= window;
        sh.sh[2 * kSphericalHarmonicsL2_CoeffCount + coefficientIndex] *= window;
    }
    INDEX_SAFE(shOut, probeIdx) = sh;
}


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\OpenCL\kernels\postProcessProbes.cl---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\OpenCL\kernels\prepareEnvironmentRays.cl---------------


#include "environmentLighting.h"


__kernel void prepareDirectEnvironmentRays(
    // *** output *** //
    OUTPUT_BUFFER( 0, ray,                 lightRaysCompactedBuffer),
    OUTPUT_BUFFER( 1, uint,                lightRaysCountBuffer),
    OUTPUT_BUFFER( 2, uint,                totalRaysCastCountBuffer),
    // *** input *** //
    INPUT_VALUE(   3, int,                 lightmapSize),
    INPUT_VALUE(   4, int,                 envFlags),
    INPUT_VALUE(   5, int,                 numEnvironmentSamples),
    INPUT_BUFFER(  6, PackedNormalOctQuad, envDirectionsBuffer),
    INPUT_BUFFER(  7, float4,              positionsWSBuffer),
    INPUT_BUFFER(  8, float,               goldenSample_buffer),
    INPUT_BUFFER( 9, uint,                 sobol_buffer),
    INPUT_BUFFER( 10, SampleDescription,   sampleDescriptionsExpandedBuffer),
    INPUT_BUFFER( 11, uint,                sampleDescriptionsExpandedCountBuffer),
    INPUT_BUFFER( 12, uint,                instanceIdToLodInfoBuffer)
#   ifndef PROBES
    ,
    INPUT_VALUE(  13, uint,                currentTileIdx),
    INPUT_VALUE(  14, uint,                sqrtNumTiles),
    INPUT_BUFFER( 15, unsigned char,                 blueNoiseSampling_buffer),
    INPUT_BUFFER( 16, unsigned char,                 blueNoiseScrambling_buffer),
    INPUT_BUFFER( 17, unsigned char,                 blueNoiseRanking_buffer),
    INPUT_VALUE(  18, int,                 blueNoiseBufferOffset),
    INPUT_BUFFER( 19, PackedNormalOctQuad, interpNormalsWSBuffer),
    INPUT_BUFFER( 20, PackedNormalOctQuad, planeNormalsWSBuffer),
    INPUT_VALUE(  21, float,               pushOff),
    INPUT_VALUE(  22, int,                 superSamplingMultiplier)
#   endif
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    __local uint numRayPreparedSharedMem;
    if (get_local_id(0) == 0)
        numRayPreparedSharedMem = 0;
    barrier(CLK_LOCAL_MEM_FENCE);

    // Prepare ray in private memory
    ray r;
    Ray_SetInactive(&r);

    int expandedRayIdx = get_global_id(0), local_idx;
    const uint sampleDescriptionsExpandedCount = INDEX_SAFE(sampleDescriptionsExpandedCountBuffer, 0);
    if (expandedRayIdx < sampleDescriptionsExpandedCount)
    {
        const SampleDescription sampleDescription = INDEX_SAFE(sampleDescriptionsExpandedBuffer, expandedRayIdx);
#if DISALLOW_RAY_EXPANSION
        if (sampleDescription.texelIndex >= 0)
        {
#endif

#ifndef PROBES
        // Remap the global lightmap index to the local, super-sampled space of the tile
        const int localIdx = GetSuperSampledLocalIndex(sampleDescription.texelIndex, lightmapSize, sampleDescription.currentSampleCount, superSamplingMultiplier, currentTileIdx, sqrtNumTiles, sobol_buffer, goldenSample_buffer KERNEL_VALIDATOR_BUFFERS);
#else
        const int localIdx = sampleDescription.texelIndex;
#endif
        const float4 position = INDEX_SAFE(positionsWSBuffer, localIdx);

        AssertPositionIsOccupied(position KERNEL_VALIDATOR_BUFFERS);

        //Random numbers
        float3 rand;
        int texel_x = sampleDescription.texelIndex % lightmapSize;
        int texel_y = sampleDescription.texelIndex / lightmapSize;
#ifndef PROBES
#if PLM_USE_BLUE_NOISE_SAMPLING
        if (sampleDescription.currentSampleCount < PLM_BLUE_NOISE_MAX_SAMPLES)
        {
            rand.x = BlueNoiseSobolSample(sampleDescription.currentSampleCount, 0, texel_x, texel_y, blueNoiseBufferOffset, blueNoiseSampling_buffer, blueNoiseScrambling_buffer, blueNoiseRanking_buffer KERNEL_VALIDATOR_BUFFERS);
            rand.y = BlueNoiseSobolSample(sampleDescription.currentSampleCount, 1, texel_x, texel_y, blueNoiseBufferOffset, blueNoiseSampling_buffer, blueNoiseScrambling_buffer, blueNoiseRanking_buffer KERNEL_VALIDATOR_BUFFERS);
            rand.z = BlueNoiseSobolSample(sampleDescription.currentSampleCount, 2, texel_x, texel_y, blueNoiseBufferOffset, blueNoiseSampling_buffer, blueNoiseScrambling_buffer, blueNoiseRanking_buffer KERNEL_VALIDATOR_BUFFERS);
        }
        else
#endif
#endif
        {
            rand.x = SobolSample(sampleDescription.currentSampleCount, 0, sobol_buffer KERNEL_VALIDATOR_BUFFERS);
            rand.y = SobolSample(sampleDescription.currentSampleCount, 1, sobol_buffer KERNEL_VALIDATOR_BUFFERS);
            rand.z = SobolSample(sampleDescription.currentSampleCount, 2, sobol_buffer KERNEL_VALIDATOR_BUFFERS);
            rand = ApplyCranleyPattersonRotation3D(rand, texel_x, texel_y, lightmapSize, 0, goldenSample_buffer KERNEL_VALIDATOR_BUFFERS);
        }

#ifdef PROBES
        float3 P = position.xyz;
        float4 D;
        if (UseEnvironmentImportanceSampling(envFlags))
            D = GenerateVolumeEnvironmentRayIS(numEnvironmentSamples, rand, envDirectionsBuffer KERNEL_VALIDATOR_BUFFERS);
        else
            D = GenerateVolumeEnvironmentRay(rand.xy);
        const int packedLODInfo = PackLODInfo(NO_LOD_MASK, NO_LOD_GROUP);
#else
        float3 interpNormal = DecodeNormal(INDEX_SAFE(interpNormalsWSBuffer, localIdx));
        float3 planeNormal = DecodeNormal(INDEX_SAFE(planeNormalsWSBuffer, localIdx));

        float4 D;
        if (UseEnvironmentImportanceSampling(envFlags))
            D = GenerateSurfaceEnvironmentRayIS(numEnvironmentSamples, interpNormal, planeNormal, rand, envDirectionsBuffer KERNEL_VALIDATOR_BUFFERS);
        else
            D = GenerateSurfaceEnvironmentRay(interpNormal, planeNormal, rand.xy);

        float3 P = position.xyz + planeNormal * pushOff;
        const int instanceId = (int)(floor(position.w));
        const int packedLODInfo = INDEX_SAFE(instanceIdToLodInfoBuffer, instanceId);
#endif
        if (D.w != 0.0f)
        {
            Ray_Init(&r, P, D.xyz, DEFAULT_RAY_LENGTH, D.w, packedLODInfo);

            // Set the index so we can map to the originating texel/probe
            Ray_SetSourceIndex(&r, sampleDescription.texelIndex);
        }
#if DISALLOW_RAY_EXPANSION
        }
#endif
    }

    // Threads synchronization for compaction
    if (Ray_IsActive_Private(&r))
    {
        local_idx = atomic_inc(&numRayPreparedSharedMem);
    }
    barrier(CLK_LOCAL_MEM_FENCE);
    if (get_local_id(0) == 0)
    {
        atomic_add(GET_PTR_SAFE(totalRaysCastCountBuffer, 0), numRayPreparedSharedMem);
        int numRayToAdd = numRayPreparedSharedMem;
#if DISALLOW_LIGHT_RAYS_COMPACTION
        numRayToAdd = get_local_size(0);
#endif
        numRayPreparedSharedMem = atomic_add(GET_PTR_SAFE(lightRaysCountBuffer, 0), numRayToAdd);
    }
    barrier(CLK_LOCAL_MEM_FENCE);

    int compactedIndex = numRayPreparedSharedMem + local_idx;
#if DISALLOW_LIGHT_RAYS_COMPACTION
    compactedIndex = expandedRayIdx;
#else
    // Write the ray out to memory
    if (Ray_IsActive_Private(&r))
#endif
    {
        Ray_SetSampleDescriptionIndex(&r, expandedRayIdx);
        INDEX_SAFE(lightRaysCompactedBuffer, compactedIndex) = r;
    }
}

__kernel void prepareIndirectEnvironmentRays(
    //*** output ***
    OUTPUT_BUFFER( 0, ray,                 lightRaysCompactedBuffer),
    OUTPUT_BUFFER( 1, uint,                lightRaysCountBuffer),
    OUTPUT_BUFFER( 2, uint,                totalRaysCastCountBuffer),
    OUTPUT_BUFFER( 3, uint,                lightRayIndexToPathRayIndexCompactedBuffer),
    //*** input ***
    INPUT_BUFFER(  4, ray,                 pathRaysCompactedBuffer_0),
    INPUT_BUFFER(  5, uint,                activePathCountBuffer_0),
    INPUT_BUFFER(  6, Intersection,        pathIntersectionsCompactedBuffer),
    INPUT_BUFFER(  7, PackedNormalOctQuad, pathLastPlaneNormalCompactedBuffer),
    INPUT_BUFFER(  8, unsigned char,       pathLastNormalFacingTheRayCompactedBuffer),
    INPUT_BUFFER(  9, PackedNormalOctQuad, pathLastInterpNormalCompactedBuffer),
    INPUT_BUFFER( 10, float,               goldenSample_buffer),
    INPUT_BUFFER( 11, uint,                sobol_buffer),
    INPUT_BUFFER( 12, PackedNormalOctQuad, envDirectionsBuffer),
    INPUT_BUFFER( 13, SampleDescription,   sampleDescriptionsExpandedBuffer),
    INPUT_VALUE(  14, int,                 envFlags),
    INPUT_VALUE(  15, int,                 numEnvironmentSamples),
    INPUT_VALUE(  16, int,                 lightmapSize),
    INPUT_VALUE(  17, int,                 bounce),
    INPUT_VALUE(  18, float,               pushOff),
    INPUT_VALUE(  19, int,                 superSamplingMultiplier),
    INPUT_BUFFER( 20, unsigned char,                 blueNoiseSampling_buffer),
    INPUT_BUFFER( 21, unsigned char,                 blueNoiseScrambling_buffer),
    INPUT_BUFFER( 22, unsigned char,                 blueNoiseRanking_buffer),
    INPUT_VALUE(  23, int,                 blueNoiseBufferOffset)
    KERNEL_VALIDATOR_BUFFERS_DEF
)
{
    // Initialize local memory
    __local uint numRayPreparedSharedMem;
    if (get_local_id(0) == 0)
        numRayPreparedSharedMem = 0;
    barrier(CLK_LOCAL_MEM_FENCE);

    // Prepare ray in private memory
    ray r;
    Ray_SetInactive(&r);

    // Should we prepare a light ray?
    int compactedPathRayIdx = get_global_id(0), local_idx;
    bool shouldPrepareNewRay = compactedPathRayIdx < INDEX_SAFE(activePathCountBuffer_0, 0);

#if DISALLOW_PATH_RAYS_COMPACTION
    if (Ray_IsInactive(GET_PTR_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIdx)))
    {
        shouldPrepareNewRay = false;
    }
#endif

    if (shouldPrepareNewRay)
    {
        KERNEL_ASSERT(Ray_IsActive(GET_PTR_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIdx)));

        // We did not hit anything, no light ray.
        const bool pathRayHitSomething = INDEX_SAFE(pathIntersectionsCompactedBuffer, compactedPathRayIdx).shapeid != MISS_MARKER;
        shouldPrepareNewRay &= pathRayHitSomething;

        // We hit an invalid triangle (from the back, no double sided GI), no light ray.
        const bool isNormalFacingTheRay = INDEX_SAFE(pathLastNormalFacingTheRayCompactedBuffer, compactedPathRayIdx);
        shouldPrepareNewRay &= isNormalFacingTheRay;
    }

    // Prepare the shadow ray
    if (shouldPrepareNewRay)
    {
        int sampleDescriptionIdx = Ray_GetSampleDescriptionIndex(GET_PTR_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIdx));
        const SampleDescription sampleDescription = INDEX_SAFE(sampleDescriptionsExpandedBuffer, sampleDescriptionIdx);

        // Get random numbers
        int baseDimension = bounce * PLM_MAX_NUM_SOBOL_DIMENSIONS_PER_BOUNCE;
        float3 sample3D;
        int texel_x = sampleDescription.texelIndex % lightmapSize;
        int texel_y = sampleDescription.texelIndex / lightmapSize;

#if PLM_USE_BLUE_NOISE_SAMPLING
        if(sampleDescription.currentSampleCount < PLM_BLUE_NOISE_MAX_SAMPLES)
        {
            sample3D.x = BlueNoiseSobolSample(sampleDescription.currentSampleCount, baseDimension + 0, texel_x, texel_y, blueNoiseBufferOffset, blueNoiseSampling_buffer, blueNoiseScrambling_buffer, blueNoiseRanking_buffer KERNEL_VALIDATOR_BUFFERS);
            sample3D.y = BlueNoiseSobolSample(sampleDescription.currentSampleCount, baseDimension + 1, texel_x, texel_y, blueNoiseBufferOffset, blueNoiseSampling_buffer, blueNoiseScrambling_buffer, blueNoiseRanking_buffer KERNEL_VALIDATOR_BUFFERS);
            sample3D.z = BlueNoiseSobolSample(sampleDescription.currentSampleCount, baseDimension + 2, texel_x, texel_y, blueNoiseBufferOffset, blueNoiseSampling_buffer, blueNoiseScrambling_buffer, blueNoiseRanking_buffer KERNEL_VALIDATOR_BUFFERS);
        }
        else
#endif
        {
            sample3D.x = SobolSample(sampleDescription.currentSampleCount, baseDimension + 0, sobol_buffer KERNEL_VALIDATOR_BUFFERS);
            sample3D.y = SobolSample(sampleDescription.currentSampleCount, baseDimension + 1, sobol_buffer KERNEL_VALIDATOR_BUFFERS);
            sample3D.z = SobolSample(sampleDescription.currentSampleCount, baseDimension + 2, sobol_buffer KERNEL_VALIDATOR_BUFFERS);
            sample3D = ApplyCranleyPattersonRotation3D(sample3D, texel_x, texel_y, lightmapSize, baseDimension, goldenSample_buffer KERNEL_VALIDATOR_BUFFERS);
        }

        float3 planeNormal  = DecodeNormal(INDEX_SAFE(pathLastPlaneNormalCompactedBuffer, compactedPathRayIdx));
        float3 interpNormal = DecodeNormal(INDEX_SAFE(pathLastInterpNormalCompactedBuffer, compactedPathRayIdx));

        float4 D;
        if (UseEnvironmentImportanceSampling(envFlags))
            D = GenerateSurfaceEnvironmentRayIS(numEnvironmentSamples, interpNormal, planeNormal, sample3D, envDirectionsBuffer KERNEL_VALIDATOR_BUFFERS);
        else
            D = GenerateSurfaceEnvironmentRay(interpNormal, planeNormal, sample3D.xy);

        // TODO(RadeonRays) gboisse: we're generating some NaN directions somehow, fix it!!
        if (D.w != 0.0f && !any(isnan(D)))
        {
            float  t  = INDEX_SAFE(pathIntersectionsCompactedBuffer, compactedPathRayIdx).uvwt.w;
            float3 P  = INDEX_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIdx).o.xyz + INDEX_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIdx).d.xyz * t;
                   P += planeNormal * pushOff;

            // propagate potentially fixed up lod param from the intersection
            const int instanceLodInfo = INDEX_SAFE(pathIntersectionsCompactedBuffer, compactedPathRayIdx).padding0;

            Ray_Init(&r, P, D.xyz, DEFAULT_RAY_LENGTH, D.w, instanceLodInfo);

            // Set the index so we can map to the originating texel/probe
            Ray_SetSourceIndex(&r, sampleDescription.texelIndex);
            Ray_SetSampleDescriptionIndex(&r, sampleDescriptionIdx);
        }
    }

    // Threads synchronization for compaction
    if (Ray_IsActive_Private(&r))
    {
        local_idx = atomic_inc(&numRayPreparedSharedMem);
    }
    barrier(CLK_LOCAL_MEM_FENCE);
    if (get_local_id(0) == 0)
    {
        atomic_add(GET_PTR_SAFE(totalRaysCastCountBuffer, 0), numRayPreparedSharedMem);
        int numRayToAdd = numRayPreparedSharedMem;
#if DISALLOW_LIGHT_RAYS_COMPACTION
        numRayToAdd = get_local_size(0);
#endif
        numRayPreparedSharedMem = atomic_add(GET_PTR_SAFE(lightRaysCountBuffer, 0), numRayToAdd);
    }
    barrier(CLK_LOCAL_MEM_FENCE);

    int compactedLightRayIndex = numRayPreparedSharedMem + local_idx;
#if DISALLOW_LIGHT_RAYS_COMPACTION
    compactedLightRayIndex = compactedPathRayIdx;
#else
    // Write the ray out to memory
    if (Ray_IsActive_Private(&r))
#endif
    {
        INDEX_SAFE(lightRaysCompactedBuffer, compactedLightRayIndex) = r;
        INDEX_SAFE(lightRayIndexToPathRayIndexCompactedBuffer, compactedLightRayIndex) = compactedPathRayIdx;
    }
}


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\OpenCL\kernels\prepareEnvironmentRays.cl---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\OpenCL\kernels\prepareLightRays.cl---------------


#include "commonCL.h"
#include "directLighting.h"

static int GetCellIndex(float3 position, float3 gridBias, float3 gridScale, int3 gridDims)
{
    const int3 cellPos = clamp(convert_int3(position * gridScale + gridBias), (int3)0, gridDims - 1);
    return cellPos.x + cellPos.y * gridDims.x + cellPos.z * gridDims.x * gridDims.y;
}

//Preparing shadowRays for direct lighting.
__kernel void prepareLightRays(
    //outputs
    OUTPUT_BUFFER(00, ray,                 lightRaysCompactedBuffer),
    OUTPUT_BUFFER(01, LightSample,         lightSamplesCompactedBuffer),
    OUTPUT_BUFFER(02, uint,                totalRaysCastCountBuffer),
    OUTPUT_BUFFER(03, uint,                lightRaysCountBuffer),
    //inputs
    INPUT_BUFFER( 04, float4,              positionsWSBuffer),
    INPUT_BUFFER( 05, int,                 directLightsIndexBuffer),
    INPUT_BUFFER( 06, LightBuffer,         directLightsBuffer),
    INPUT_BUFFER( 07, int,                 directLightsOffsetBuffer),
    INPUT_BUFFER( 08, int,                 directLightsCountPerCellBuffer),
    INPUT_VALUE(  09, float3,              lightGridBias),
    INPUT_VALUE(  10, float3,              lightGridScale),
    INPUT_VALUE(  11, int3,                lightGridDims),
    INPUT_VALUE(  12, int,                 lightmapSize),
    INPUT_BUFFER( 13, float,               goldenSample_buffer),
    INPUT_BUFFER( 14, uint,                sobol_buffer),
    INPUT_BUFFER( 15, uint,                sampleDescriptionsExpandedCountBuffer),
    INPUT_VALUE(  16, int,                 lightIndexInCell),
    INPUT_BUFFER( 17, SampleDescription,   sampleDescriptionsExpandedBuffer),
    INPUT_BUFFER( 18, uint,                instanceIdToLodInfoBuffer)
#ifndef PROBES
    ,
    INPUT_VALUE(  19, uint,                currentTileIdx),
    INPUT_VALUE(  20, uint,                sqrtNumTiles),
    INPUT_BUFFER( 21, unsigned char,                 blueNoiseSampling_buffer),
    INPUT_BUFFER( 22, unsigned char,                 blueNoiseScrambling_buffer),
    INPUT_BUFFER( 23, unsigned char,                 blueNoiseRanking_buffer),
    INPUT_VALUE(  24, int,                 blueNoiseBufferOffset),
    INPUT_BUFFER( 25, PackedNormalOctQuad, interpNormalsWSBuffer),
    INPUT_VALUE(  26, float,               pushOff),
    INPUT_VALUE(  27, int,                 superSamplingMultiplier)
#endif
    KERNEL_VALIDATOR_BUFFERS_DEF
)
{
    __local uint numRayPreparedSharedMem;
    if (get_local_id(0) == 0)
        numRayPreparedSharedMem = 0;
    barrier(CLK_LOCAL_MEM_FENCE);

    // Prepare ray in private memory
    ray r;
    Ray_SetInactive(&r);
    LightSample lightSample;
    lightSample.lightIdx = -1;

    int expandedRayIdx = get_global_id(0), local_idx;
    const uint sampleDescriptionsExpandedCount = INDEX_SAFE(sampleDescriptionsExpandedCountBuffer, 0);
    if (expandedRayIdx < sampleDescriptionsExpandedCount)
    {
        const SampleDescription sampleDescription = INDEX_SAFE(sampleDescriptionsExpandedBuffer, expandedRayIdx);
#if DISALLOW_RAY_EXPANSION
        if (sampleDescription.texelIndex >= 0)
        {
#endif

#ifndef PROBES
        // Remap the global lightmap index to the local, super-sampled space of the tile
        const int localIdx = GetSuperSampledLocalIndex(sampleDescription.texelIndex, lightmapSize, sampleDescription.currentSampleCount, superSamplingMultiplier, currentTileIdx, sqrtNumTiles, sobol_buffer, goldenSample_buffer KERNEL_VALIDATOR_BUFFERS);
#else
        const int localIdx = sampleDescription.texelIndex;
#endif

        float4 position = INDEX_SAFE(positionsWSBuffer, localIdx);
        AssertPositionIsOccupied(position KERNEL_VALIDATOR_BUFFERS);

        // Directional light pass
        // The direction light index is encoded in the lightIndexInCell in negative numbers offseted by 1
        // The offset of 1 allows us to always have a negative lightIndexInCell for the dir light pass, especially with dir_light_id == 0 which becomes -1
        // Before calling this kernel we then do:  lightIndexInCell = -directionalLightIndex - 1;
        if (lightIndexInCell < 0)
        {
            int directionalLightIndex = -lightIndexInCell - 1;

            lightSample.lightPdf = 1.0f;
            lightSample.lightIdx = directionalLightIndex;
        }
        else
        {
            const int cellIdx = GetCellIndex(position.xyz, lightGridBias, lightGridScale, lightGridDims);
            const int lightCountInCell = INDEX_SAFE(directLightsCountPerCellBuffer, cellIdx);

            // Lights in light grid pass
            // If we already did all the lights in the cell bail out
            if (lightIndexInCell < lightCountInCell)
            {
                // Select a light in a round robin fashion (no need for pdf)
                const int lightCellOffset = INDEX_SAFE(directLightsOffsetBuffer, cellIdx) + lightIndexInCell;
                lightSample.lightPdf = 1.0f;
                lightSample.lightIdx = INDEX_SAFE(directLightsIndexBuffer, lightCellOffset);
            }
        }

        if(lightSample.lightIdx >=0)
        {
            const LightBuffer light = INDEX_SAFE(directLightsBuffer, lightSample.lightIdx);

            // Get random numbers
            float2 sample2D;
            int texel_x = sampleDescription.texelIndex % lightmapSize;
            int texel_y = sampleDescription.texelIndex / lightmapSize;

#ifndef PROBES
#if PLM_USE_BLUE_NOISE_SAMPLING
            if(sampleDescription.currentSampleCount < PLM_BLUE_NOISE_MAX_SAMPLES)
            {
                sample2D.x = BlueNoiseSobolSample(sampleDescription.currentSampleCount, 0, texel_x, texel_y, blueNoiseBufferOffset, blueNoiseSampling_buffer, blueNoiseScrambling_buffer, blueNoiseRanking_buffer KERNEL_VALIDATOR_BUFFERS);
                sample2D.y = BlueNoiseSobolSample(sampleDescription.currentSampleCount, 1, texel_x, texel_y, blueNoiseBufferOffset, blueNoiseSampling_buffer, blueNoiseScrambling_buffer, blueNoiseRanking_buffer KERNEL_VALIDATOR_BUFFERS);
            }
            else
#endif
#endif
            {
                sample2D.x = SobolSample(sampleDescription.currentSampleCount, 0, sobol_buffer KERNEL_VALIDATOR_BUFFERS);
                sample2D.y = SobolSample(sampleDescription.currentSampleCount, 1, sobol_buffer KERNEL_VALIDATOR_BUFFERS);
                sample2D = ApplyCranleyPattersonRotation2D(sample2D, texel_x, texel_y, lightmapSize, 0, goldenSample_buffer KERNEL_VALIDATOR_BUFFERS);
            }

            // Generate the shadow ray. This might be an inactive ray (in case of back facing surfaces or out of cone angle for spots).
#ifdef PROBES
            float3 notUsed3 = (float3)(0, 0, 0);
            const int packedNoLODInfo = PackLODInfo(NO_LOD_MASK, NO_LOD_GROUP);
            PrepareShadowRay(light, sample2D, position.xyz, notUsed3, 0, false, &r, packedNoLODInfo);
#else
            const int instanceId = (int)(floor(position.w));
            const int instanceLodInfo = INDEX_SAFE(instanceIdToLodInfoBuffer, instanceId);
            float3 normal = DecodeNormal(INDEX_SAFE(interpNormalsWSBuffer, localIdx));
            PrepareShadowRay(light, sample2D, position.xyz, normal, pushOff, false, &r, instanceLodInfo);
#endif
            // Set the index so we can map to the originating texel/probe
            Ray_SetSourceIndex(&r, sampleDescription.texelIndex);
        }
#if DISALLOW_RAY_EXPANSION
        }
#endif
    }

    // Threads synchronization for compaction
    if (Ray_IsActive_Private(&r))
    {
        local_idx = atomic_inc(&numRayPreparedSharedMem);
    }
    barrier(CLK_LOCAL_MEM_FENCE);
    if (get_local_id(0) == 0)
    {
        atomic_add(GET_PTR_SAFE(totalRaysCastCountBuffer, 0), numRayPreparedSharedMem);
        int numRayToAdd = numRayPreparedSharedMem;
#if DISALLOW_LIGHT_RAYS_COMPACTION
        numRayToAdd = get_local_size(0);
#endif
        numRayPreparedSharedMem = atomic_add(GET_PTR_SAFE(lightRaysCountBuffer, 0), numRayToAdd);
    }
    barrier(CLK_LOCAL_MEM_FENCE);

    int compactedIndex = numRayPreparedSharedMem + local_idx;
#if DISALLOW_LIGHT_RAYS_COMPACTION
    compactedIndex = expandedRayIdx;
#else
    // Write the ray out to memory
    if (Ray_IsActive_Private(&r))
#endif
    {
        INDEX_SAFE(lightSamplesCompactedBuffer, compactedIndex) = lightSample;
        Ray_SetSampleDescriptionIndex(&r, expandedRayIdx);
        INDEX_SAFE(lightRaysCompactedBuffer, compactedIndex) = r;
    }
}

//Preparing shadowRays for indirect lighting.
__kernel void prepareLightRaysFromBounce(
    INPUT_BUFFER(00, LightBuffer, indirectLightsBuffer),
    INPUT_BUFFER(01, int, indirectLightsOffsetBuffer),
    INPUT_BUFFER(02, int, indirectLightsIndexBuffer),
    INPUT_BUFFER(03, int, indirectLightsDistributionBuffer),
    INPUT_BUFFER(04, int, indirectLightDistributionOffsetBuffer),
    INPUT_BUFFER(05, PowerSamplingStat, usePowerSamplingBuffer),
    INPUT_VALUE(06, float3, lightGridBias),
    INPUT_VALUE(07, float3, lightGridScale),
    INPUT_VALUE(08, int3, lightGridDims),
    INPUT_BUFFER(09, ray, pathRaysCompactedBuffer_0),
    INPUT_BUFFER(10, Intersection, pathIntersectionsCompactedBuffer),
    INPUT_BUFFER(11, PackedNormalOctQuad, pathLastInterpNormalCompactedBuffer),
    INPUT_BUFFER(12, unsigned char, pathLastNormalFacingTheRayCompactedBuffer),
    INPUT_VALUE(13, int, lightmapSize),
    INPUT_VALUE(14, int, bounce),
    INPUT_BUFFER(15, float, goldenSample_buffer),
    INPUT_BUFFER(16, uint, sobol_buffer),
    INPUT_VALUE(17, float, pushOff),
    INPUT_BUFFER(18, uint, activePathCountBuffer_0),
    INPUT_BUFFER(19, SampleDescription, sampleDescriptionsExpandedBuffer),
    OUTPUT_BUFFER(20, ray, lightRaysCompactedBuffer),
    OUTPUT_BUFFER(21, LightSample, lightSamplesCompactedBuffer),
    OUTPUT_BUFFER(22, uint, lightRayIndexToPathRayIndexCompactedBuffer),
    OUTPUT_BUFFER(23, uint, totalRaysCastCountBuffer),
    OUTPUT_BUFFER(24, uint, lightRaysCountBuffer),
    INPUT_BUFFER( 25, unsigned char, blueNoiseSampling_buffer),
    INPUT_BUFFER( 26, unsigned char, blueNoiseScrambling_buffer),
    INPUT_BUFFER( 27, unsigned char, blueNoiseRanking_buffer),
    INPUT_VALUE(  28, int, blueNoiseBufferOffset),
    INPUT_VALUE(  29, int, directionalLightIndex),
    INPUT_VALUE(  30, uint, currentTileIdx),
    INPUT_VALUE(  31, uint, sqrtNumTiles)
    KERNEL_VALIDATOR_BUFFERS_DEF
)
{
    // Initialize local memory
    __local uint numRayPreparedSharedMem;
    if (get_local_id(0) == 0)
        numRayPreparedSharedMem = 0;
    barrier(CLK_LOCAL_MEM_FENCE);

    // Prepare ray in private memory
    ray r;
    Ray_SetInactive(&r);
    LightSample lightSample;

    // Should we prepare a light ray?
    int compactedPathRayIdx = get_global_id(0), local_idx;
    bool shouldPrepareNewRay = compactedPathRayIdx < INDEX_SAFE(activePathCountBuffer_0, 0);

#if DISALLOW_PATH_RAYS_COMPACTION
    if (Ray_IsInactive(GET_PTR_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIdx)))
    {
        shouldPrepareNewRay = false;
    }
#endif

    if (shouldPrepareNewRay)
    {
        KERNEL_ASSERT(Ray_IsActive(GET_PTR_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIdx)));

        // We did not hit anything, no light ray.
        const bool pathRayHitSomething = INDEX_SAFE(pathIntersectionsCompactedBuffer, compactedPathRayIdx).shapeid != MISS_MARKER;
        shouldPrepareNewRay &= pathRayHitSomething;

        // We hit an invalid triangle (from the back, no double sided GI), no light ray.
        const bool isNormalFacingTheRay = INDEX_SAFE(pathLastNormalFacingTheRayCompactedBuffer, compactedPathRayIdx);
        shouldPrepareNewRay &= isNormalFacingTheRay;
    }

    // Prepare the shadow ray
    if (shouldPrepareNewRay)
    {
        const float3 surfaceNormal = DecodeNormal(INDEX_SAFE(pathLastInterpNormalCompactedBuffer, compactedPathRayIdx));
        const float t = INDEX_SAFE(pathIntersectionsCompactedBuffer, compactedPathRayIdx).uvwt.w;
        const float3 surfacePosition = INDEX_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIdx).o.xyz + t * INDEX_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIdx).d.xyz;

        // Retrieve the light distribution at the shading site
        const int cellIdx = GetCellIndex(surfacePosition, lightGridBias, lightGridScale, lightGridDims);
        __global const int *const restrict lightDistributionPtr = GET_PTR_SAFE(indirectLightsDistributionBuffer, INDEX_SAFE(indirectLightDistributionOffsetBuffer, cellIdx));
        const int lightDistribution = *lightDistributionPtr; // safe to dereference, as GET_PTR_SAFE above does the validation

        // If there is no light in the cell, or not doing the directional light pass bail out
        if (lightDistribution || directionalLightIndex>=0)
        {
            // propagate potentially fixed up lod param from the intersection
            const int instanceLodInfo = INDEX_SAFE(pathIntersectionsCompactedBuffer, compactedPathRayIdx).padding0;

            int expandedRayIdx = Ray_GetSampleDescriptionIndex(GET_PTR_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIdx));
            const SampleDescription sampleDescription = INDEX_SAFE(sampleDescriptionsExpandedBuffer, expandedRayIdx);

            // Get random numbers
            int baseDimension = bounce * PLM_MAX_NUM_SOBOL_DIMENSIONS_PER_BOUNCE;
            float3 sample3D;
            int texel_x = sampleDescription.texelIndex % lightmapSize;
            int texel_y = sampleDescription.texelIndex / lightmapSize;

#if PLM_USE_BLUE_NOISE_SAMPLING
            if(sampleDescription.currentSampleCount < PLM_BLUE_NOISE_MAX_SAMPLES)
            {
                sample3D.x = BlueNoiseSobolSample(sampleDescription.currentSampleCount, baseDimension + 0, texel_x, texel_y, blueNoiseBufferOffset, blueNoiseSampling_buffer, blueNoiseScrambling_buffer, blueNoiseRanking_buffer KERNEL_VALIDATOR_BUFFERS);
                sample3D.y = BlueNoiseSobolSample(sampleDescription.currentSampleCount, baseDimension + 1, texel_x, texel_y, blueNoiseBufferOffset, blueNoiseSampling_buffer, blueNoiseScrambling_buffer, blueNoiseRanking_buffer KERNEL_VALIDATOR_BUFFERS);
                sample3D.z = BlueNoiseSobolSample(sampleDescription.currentSampleCount, baseDimension + 2, texel_x, texel_y, blueNoiseBufferOffset, blueNoiseSampling_buffer, blueNoiseScrambling_buffer, blueNoiseRanking_buffer KERNEL_VALIDATOR_BUFFERS);
            }
            else
#endif
            {
                sample3D.x = SobolSample(sampleDescription.currentSampleCount, baseDimension + 0, sobol_buffer KERNEL_VALIDATOR_BUFFERS);
                sample3D.y = SobolSample(sampleDescription.currentSampleCount, baseDimension + 1, sobol_buffer KERNEL_VALIDATOR_BUFFERS);
                sample3D.z = SobolSample(sampleDescription.currentSampleCount, baseDimension + 2, sobol_buffer KERNEL_VALIDATOR_BUFFERS);
                sample3D = ApplyCranleyPattersonRotation3D(sample3D, texel_x, texel_y, lightmapSize, baseDimension, goldenSample_buffer KERNEL_VALIDATOR_BUFFERS);
            }


            // Directional light pass
            if (directionalLightIndex >= 0)
            {
                lightSample.lightIdx = directionalLightIndex;
                lightSample.lightPdf = 1.0f;
            }
            else
            {
                // Select a light
                const int powerSamplingIndex = GetLocalIndex(sampleDescription.texelIndex, lightmapSize, currentTileIdx, sqrtNumTiles);
                if (usePowerSampling(powerSamplingIndex, usePowerSamplingBuffer KERNEL_VALIDATOR_BUFFERS))
                {
                    float selectionPdf;
                    const int lightCellOffset = INDEX_SAFE(indirectLightsOffsetBuffer, cellIdx) + Distribution1D_SampleDiscrete(sample3D.z, lightDistributionPtr, &selectionPdf);
                    lightSample.lightIdx = INDEX_SAFE(indirectLightsIndexBuffer, lightCellOffset);
                    lightSample.lightPdf = selectionPdf;
                }
                else
                {
                    const int offset = min(lightDistribution - 1, (int)(sample3D.z * (float)lightDistribution));
                    const int lightCellOffset = INDEX_SAFE(indirectLightsOffsetBuffer, cellIdx) + offset;
                    lightSample.lightIdx = INDEX_SAFE(indirectLightsIndexBuffer, lightCellOffset);
                    lightSample.lightPdf = 1.0f / lightDistribution;
                }
            }

            // Generate the shadow ray
            const LightBuffer light = INDEX_SAFE(indirectLightsBuffer, lightSample.lightIdx);
            PrepareShadowRay(light, sample3D.xy, surfacePosition, surfaceNormal, pushOff, false, &r, instanceLodInfo);

            // Set the index so we can map to the originating texel/probe
            Ray_SetSourceIndex(&r, sampleDescription.texelIndex);
            Ray_SetSampleDescriptionIndex(&r, expandedRayIdx);
        }
    }

    // Threads synchronization for compaction
    if (Ray_IsActive_Private(&r))
    {
        local_idx = atomic_inc(&numRayPreparedSharedMem);
    }
    barrier(CLK_LOCAL_MEM_FENCE);
    if (get_local_id(0) == 0)
    {
        atomic_add(GET_PTR_SAFE(totalRaysCastCountBuffer, 0), numRayPreparedSharedMem);
        int numRayToAdd = numRayPreparedSharedMem;
#if DISALLOW_LIGHT_RAYS_COMPACTION
        numRayToAdd = get_local_size(0);
#endif
        numRayPreparedSharedMem = atomic_add(GET_PTR_SAFE(lightRaysCountBuffer, 0), numRayToAdd);
    }
    barrier(CLK_LOCAL_MEM_FENCE);

    int compactedLightRayIndex = numRayPreparedSharedMem + local_idx;
#if DISALLOW_LIGHT_RAYS_COMPACTION
    compactedLightRayIndex = compactedPathRayIdx;
#else
    // Write the ray out to memory
    if (Ray_IsActive_Private(&r))
#endif
    {
        INDEX_SAFE(lightSamplesCompactedBuffer, compactedLightRayIndex) = lightSample;
        INDEX_SAFE(lightRaysCompactedBuffer, compactedLightRayIndex) = r;
        INDEX_SAFE(lightRayIndexToPathRayIndexCompactedBuffer, compactedLightRayIndex) = compactedPathRayIdx;
    }
}


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\OpenCL\kernels\prepareLightRays.cl---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\OpenCL\kernels\preparePathRays.cl---------------


#include "commonCL.h"

__kernel void preparePathRays(
    OUTPUT_BUFFER(00, ray,               pathRaysCompactedBuffer_0),
    OUTPUT_BUFFER(01, uint,              activePathCountBuffer_0),
    OUTPUT_BUFFER(02, uint,              totalRaysCastCountBuffer),
    OUTPUT_BUFFER(03, float4,            originalRaysExpandedBuffer),
    INPUT_BUFFER( 04, float4,            positionsWSBuffer),
    INPUT_VALUE(  05, int,               lightmapSize),
    INPUT_VALUE(  06, int,               bounce),
    INPUT_BUFFER( 07, uint,              sobol_buffer),
    INPUT_BUFFER( 08, float,             goldenSample_buffer),
    INPUT_BUFFER( 09, SampleDescription, sampleDescriptionsExpandedBuffer),
    INPUT_BUFFER( 10, uint,              sampleDescriptionsExpandedCountBuffer),
    INPUT_BUFFER( 11, uint,              instanceIdToLodInfoBuffer)
#ifndef PROBES
    ,
    INPUT_VALUE(  12, uint,              currentTileIdx),
    INPUT_VALUE(  13, uint,              sqrtNumTiles),
    INPUT_BUFFER( 14, PackedNormalOctQuad, interpNormalsWSBuffer),
    INPUT_BUFFER( 15, PackedNormalOctQuad, planeNormalsWSBuffer),
    INPUT_VALUE(  16, float,               pushOff),
    INPUT_VALUE(  17, int,                 superSamplingMultiplier),
    INPUT_BUFFER( 18, unsigned char,                 blueNoiseSampling_buffer),
    INPUT_BUFFER( 19, unsigned char,                 blueNoiseScrambling_buffer),
    INPUT_BUFFER( 20, unsigned char,                 blueNoiseRanking_buffer),
    INPUT_VALUE(  21, int,                 blueNoiseBufferOffset)
#endif
    KERNEL_VALIDATOR_BUFFERS_DEF
)
{
    __local uint numRayPreparedSharedMem;
    if (get_local_id(0) == 0)
        numRayPreparedSharedMem = 0;
    barrier(CLK_LOCAL_MEM_FENCE);

    // Prepare ray in private memory
    ray r;
    Ray_SetInactive(&r);

    int expandedPathRayIdx = get_global_id(0), local_idx;
    const uint sampleDescriptionsExpandedCount = INDEX_SAFE(sampleDescriptionsExpandedCountBuffer, 0);
    if (expandedPathRayIdx < sampleDescriptionsExpandedCount)
    {
        const SampleDescription sampleDescription = INDEX_SAFE(sampleDescriptionsExpandedBuffer, expandedPathRayIdx);
#if DISALLOW_RAY_EXPANSION
        if (sampleDescription.texelIndex >= 0)
        {
#endif

#ifndef PROBES
        // Remap the global lightmap index to the local, super-sampled space of the tile
        const int localIdx = GetSuperSampledLocalIndex(sampleDescription.texelIndex, lightmapSize, sampleDescription.currentSampleCount, superSamplingMultiplier, currentTileIdx, sqrtNumTiles, sobol_buffer, goldenSample_buffer KERNEL_VALIDATOR_BUFFERS);
#else
        const int localIdx = sampleDescription.texelIndex;
#endif
        // Get random numbers
        float2 sample2D;

        //first bounce uses dimension 0 and 1
        int texel_x = sampleDescription.texelIndex % lightmapSize;
        int texel_y = sampleDescription.texelIndex / lightmapSize;

#ifndef PROBES
#if PLM_USE_BLUE_NOISE_SAMPLING
        if(sampleDescription.currentSampleCount < PLM_BLUE_NOISE_MAX_SAMPLES)
        {
            sample2D.x = BlueNoiseSobolSample(sampleDescription.currentSampleCount, 0, texel_x, texel_y, blueNoiseBufferOffset, blueNoiseSampling_buffer, blueNoiseScrambling_buffer, blueNoiseRanking_buffer KERNEL_VALIDATOR_BUFFERS);
            sample2D.y = BlueNoiseSobolSample(sampleDescription.currentSampleCount, 1, texel_x, texel_y, blueNoiseBufferOffset, blueNoiseSampling_buffer, blueNoiseScrambling_buffer, blueNoiseRanking_buffer KERNEL_VALIDATOR_BUFFERS);
        }
        else
#endif
#endif
        {
            sample2D.x = SobolSample(sampleDescription.currentSampleCount, 0, sobol_buffer KERNEL_VALIDATOR_BUFFERS);
            sample2D.y = SobolSample(sampleDescription.currentSampleCount, 1, sobol_buffer KERNEL_VALIDATOR_BUFFERS);
            sample2D = ApplyCranleyPattersonRotation2D(sample2D, texel_x, texel_y, lightmapSize, 0, goldenSample_buffer KERNEL_VALIDATOR_BUFFERS);
        }

        float4 position = INDEX_SAFE(positionsWSBuffer, localIdx);
        AssertPositionIsOccupied(position KERNEL_VALIDATOR_BUFFERS);

#ifdef PROBES
        float3 D = SphereSample(sample2D);
        const float3 P = position.xyz;
        const int instanceLodInfo = PackLODInfo(NO_LOD_MASK, NO_LOD_GROUP);
#else
        const float3 interpNormal = DecodeNormal(INDEX_SAFE(interpNormalsWSBuffer, localIdx));
        //Map to cosine weighted hemisphere directed toward normal
        float3 b1;
        float3 b2;
        CreateOrthoNormalBasis(interpNormal, &b1, &b2);
        float3 hamDir = HemisphereCosineSample(sample2D);
        float3 D = hamDir.x*b1 + hamDir.y*b2 + hamDir.z*interpNormal;

        const float3 planeNormal = DecodeNormal(INDEX_SAFE(planeNormalsWSBuffer, localIdx));
        const float3 P = position.xyz + planeNormal * pushOff;

        // if plane normal is too different from interpolated normal, the hemisphere orientation will be wrong and the sample could be under the surface.
        float dotVal = dot(D, planeNormal);
        const int instanceId = (int)(floor(position.w));
        const int instanceLodInfo = INDEX_SAFE(instanceIdToLodInfoBuffer, instanceId);
        if (dotVal > 0.0f && !isnan(dotVal))
#endif
        {
            Ray_Init(&r, P, D, DEFAULT_RAY_LENGTH, 0.f, instanceLodInfo);

            // Set the index so we can map to the originating texel/probe
            Ray_SetSourceIndex(&r, sampleDescription.texelIndex);
        }
#if DISALLOW_RAY_EXPANSION
        }
#endif
    }

    // Threads synchronization for compaction
    if (Ray_IsActive_Private(&r))
    {
        local_idx = atomic_inc(&numRayPreparedSharedMem);
    }
    barrier(CLK_LOCAL_MEM_FENCE);
    if (get_local_id(0) == 0)
    {
        atomic_add(GET_PTR_SAFE(totalRaysCastCountBuffer, 0), numRayPreparedSharedMem);
        int numRayToAdd = numRayPreparedSharedMem;
#if DISALLOW_PATH_RAYS_COMPACTION
        numRayToAdd = get_local_size(0);
#endif
        numRayPreparedSharedMem = atomic_add(GET_PTR_SAFE(activePathCountBuffer_0, 0), numRayToAdd);
    }
    barrier(CLK_LOCAL_MEM_FENCE);

    int compactedPathRayIndex = numRayPreparedSharedMem + local_idx;
#if DISALLOW_PATH_RAYS_COMPACTION
    compactedPathRayIndex = expandedPathRayIdx;
#else
    // Write the ray out to memory
    if (Ray_IsActive_Private(&r))
#endif
    {
        INDEX_SAFE(originalRaysExpandedBuffer, expandedPathRayIdx) = (float4)(r.d.x, r.d.y, r.d.z, 0);
        Ray_SetSampleDescriptionIndex(&r, expandedPathRayIdx);
        INDEX_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIndex) = r;
    }
}

__kernel void preparePathRaysFromBounce(
    //*** input ***
    INPUT_BUFFER( 00, ray,                 pathRaysCompactedBuffer_0),
    INPUT_BUFFER( 01, Intersection,        pathIntersectionsCompactedBuffer),
    INPUT_BUFFER( 02, uint,                activePathCountBuffer_0),
    INPUT_BUFFER( 03, PackedNormalOctQuad, pathLastPlaneNormalCompactedBuffer),
    INPUT_BUFFER( 04, unsigned char,       pathLastNormalFacingTheRayCompactedBuffer),
    INPUT_BUFFER( 05, SampleDescription,   sampleDescriptionsExpandedBuffer),
    INPUT_VALUE(  06, int,                 lightmapSize),
    INPUT_VALUE(  07, int,                 bounce),
    INPUT_BUFFER( 08, uint,                sobol_buffer),
    INPUT_BUFFER( 09, float,               goldenSample_buffer),
    INPUT_VALUE(  10, float,               pushOff),
    INPUT_VALUE(  11, int,                 minBounces),
    //*** output ***
    OUTPUT_BUFFER(12, ray,                 pathRaysCompactedBuffer_1),
    OUTPUT_BUFFER(13, uint,                totalRaysCastCountBuffer),
    OUTPUT_BUFFER(14, uint,                activePathCountBuffer_1),
    //*** in/output ***
    OUTPUT_BUFFER(15, float4,              pathThroughputExpandedBuffer),
    INPUT_BUFFER( 16, unsigned char,       blueNoiseSampling_buffer),
    INPUT_BUFFER( 17, unsigned char,       blueNoiseScrambling_buffer),
    INPUT_BUFFER( 18, unsigned char,       blueNoiseRanking_buffer),
    INPUT_VALUE(  19, int,                 blueNoiseBufferOffset)
    KERNEL_VALIDATOR_BUFFERS_DEF
)
{
    __local uint numRayPreparedSharedMem;
    if (get_local_id(0) == 0)
        numRayPreparedSharedMem = 0;
    barrier(CLK_LOCAL_MEM_FENCE);

    ray r;// Prepare ray in private memory
    Ray_SetInactive(&r);

    uint compactedPathRayIdx = get_global_id(0), local_idx;
    bool shouldPrepareNewRay = compactedPathRayIdx < INDEX_SAFE(activePathCountBuffer_0, 0);

#if DISALLOW_PATH_RAYS_COMPACTION
    if (Ray_IsInactive(GET_PTR_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIdx)))
    {
        shouldPrepareNewRay = false;
    }
#endif

    SampleDescription sampleDescription;
    int expandedPathRayIdx;
    if (shouldPrepareNewRay)
    {
        KERNEL_ASSERT(Ray_IsActive(GET_PTR_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIdx)));

        expandedPathRayIdx = Ray_GetSampleDescriptionIndex(GET_PTR_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIdx));
        sampleDescription = INDEX_SAFE(sampleDescriptionsExpandedBuffer, expandedPathRayIdx);

        // We did not hit anything, no bounce path ray.
        const bool pathRayHitSomething = INDEX_SAFE(pathIntersectionsCompactedBuffer, compactedPathRayIdx).shapeid != MISS_MARKER;
        shouldPrepareNewRay &= pathRayHitSomething;

        // We hit an invalid triangle (from the back, no double sided GI), stop the path.
        const unsigned char isNormalFacingTheRay = INDEX_SAFE(pathLastNormalFacingTheRayCompactedBuffer, compactedPathRayIdx);
        shouldPrepareNewRay = (shouldPrepareNewRay && isNormalFacingTheRay);
    }

    // Russian roulette step can terminate the path
    float3 sample3D;
    if (shouldPrepareNewRay)
    {
        // Get random numbers
        int baseDimension = bounce * PLM_MAX_NUM_SOBOL_DIMENSIONS_PER_BOUNCE;
        int texel_x = sampleDescription.texelIndex % lightmapSize;
        int texel_y = sampleDescription.texelIndex / lightmapSize;

#if PLM_USE_BLUE_NOISE_SAMPLING
        if(sampleDescription.currentSampleCount < PLM_BLUE_NOISE_MAX_SAMPLES)
        {
            sample3D.x = BlueNoiseSobolSample(sampleDescription.currentSampleCount, baseDimension + 0, texel_x, texel_y, blueNoiseBufferOffset, blueNoiseSampling_buffer, blueNoiseScrambling_buffer, blueNoiseRanking_buffer KERNEL_VALIDATOR_BUFFERS);
            sample3D.y = BlueNoiseSobolSample(sampleDescription.currentSampleCount, baseDimension + 1, texel_x, texel_y, blueNoiseBufferOffset, blueNoiseSampling_buffer, blueNoiseScrambling_buffer, blueNoiseRanking_buffer KERNEL_VALIDATOR_BUFFERS);
            sample3D.z = BlueNoiseSobolSample(sampleDescription.currentSampleCount, baseDimension + 2, texel_x, texel_y, blueNoiseBufferOffset, blueNoiseSampling_buffer, blueNoiseScrambling_buffer, blueNoiseRanking_buffer KERNEL_VALIDATOR_BUFFERS);
        }
        else
#endif
        {
            sample3D.x = SobolSample(sampleDescription.currentSampleCount, baseDimension + 0, sobol_buffer KERNEL_VALIDATOR_BUFFERS);
            sample3D.y = SobolSample(sampleDescription.currentSampleCount, baseDimension + 1, sobol_buffer KERNEL_VALIDATOR_BUFFERS);
            sample3D.z = SobolSample(sampleDescription.currentSampleCount, baseDimension + 2, sobol_buffer KERNEL_VALIDATOR_BUFFERS);
            sample3D = ApplyCranleyPattersonRotation3D(sample3D, texel_x, texel_y, lightmapSize, baseDimension, goldenSample_buffer KERNEL_VALIDATOR_BUFFERS);
        }

        const bool doRussianRoulette = (bounce >= minBounces && shouldPrepareNewRay);
        if (doRussianRoulette)
        {
            const float4 pathThroughput = INDEX_SAFE(pathThroughputExpandedBuffer, expandedPathRayIdx);
            float p = max(max(pathThroughput.x, pathThroughput.y), pathThroughput.z);
            if (p < sample3D.z)
                shouldPrepareNewRay = false;
            else
                INDEX_SAFE(pathThroughputExpandedBuffer, expandedPathRayIdx).xyz *= (1 / p);
        }
    }

    if (shouldPrepareNewRay)
    {
        const float3 planeNormal = DecodeNormal(INDEX_SAFE(pathLastPlaneNormalCompactedBuffer, compactedPathRayIdx));
        const float t = INDEX_SAFE(pathIntersectionsCompactedBuffer, compactedPathRayIdx).uvwt.w;
        const float3 position = INDEX_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIdx).o.xyz + INDEX_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIdx).d.xyz * t;

        // Map to cosine weighted hemisphere directed toward plane normal
        float3 b1;
        float3 b2;
        CreateOrthoNormalBasis(planeNormal, &b1, &b2);
        float3 hamDir = HemisphereCosineSample(sample3D.xy);
        float3 D = hamDir.x*b1 + hamDir.y*b2 + hamDir.z*planeNormal;

        // TODO(RadeonRays) gboisse: we're generating some NaN directions somehow, fix it!!
        if (!any(isnan(D)))
        {
            const float3 P = position.xyz + planeNormal * pushOff;

            // propagate potentially fixed up lod param from the intersection
            const int instanceLodInfo = INDEX_SAFE(pathIntersectionsCompactedBuffer, compactedPathRayIdx).padding0;

            Ray_Init(&r, P, D, DEFAULT_RAY_LENGTH, 0.f, instanceLodInfo);
            Ray_SetSourceIndex(&r, sampleDescription.texelIndex);
            Ray_SetSampleDescriptionIndex(&r, expandedPathRayIdx);
        }
    }

    // Threads synchronization for compaction
    if (Ray_IsActive_Private(&r))
    {
        local_idx = atomic_inc(&numRayPreparedSharedMem);
    }
    barrier(CLK_LOCAL_MEM_FENCE);
    if (get_local_id(0) == 0)
    {
        atomic_add(GET_PTR_SAFE(totalRaysCastCountBuffer, 0), numRayPreparedSharedMem);
        int numRayToAdd = numRayPreparedSharedMem;
#if DISALLOW_PATH_RAYS_COMPACTION
        numRayToAdd = get_local_size(0);
#endif
        numRayPreparedSharedMem = atomic_add(GET_PTR_SAFE(activePathCountBuffer_1, 0), numRayToAdd);
    }
    barrier(CLK_LOCAL_MEM_FENCE);

    int compactedBouncedPathRayIndex = numRayPreparedSharedMem + local_idx;
#if DISALLOW_PATH_RAYS_COMPACTION
    compactedBouncedPathRayIndex = compactedPathRayIdx;
#else
    // Write the ray out to memory
    if (Ray_IsActive_Private(&r))
#endif
    {
        INDEX_SAFE(pathRaysCompactedBuffer_1, compactedBouncedPathRayIndex) = r;
    }
}


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\OpenCL\kernels\preparePathRays.cl---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\OpenCL\kernels\processBounce.cl---------------


#include "commonCL.h"
#include "colorSpace.h"
#include "directLighting.h"
#include "emissiveLighting.h"

__constant sampler_t linear2DSampler = CLK_NORMALIZED_COORDS_TRUE | CLK_ADDRESS_CLAMP_TO_EDGE | CLK_FILTER_LINEAR;

static void AccumulateLightFromBounce(float3 albedo, float3 directLightingAtHit, int expandedRayIdx, __global float3* lightingExpandedBuffer, int lightmapMode,
    __global float4* directionalExpandedBuffer, float3 direction KERNEL_VALIDATOR_BUFFERS_DEF)
{
    //Purely diffuse surface reflect the unabsorbed light evenly on the hemisphere.
    float3 energyFromHit = albedo * directLightingAtHit;
    INDEX_SAFE(lightingExpandedBuffer, expandedRayIdx).xyz += energyFromHit;

    //compute directionality from indirect
    if (lightmapMode == LIGHTMAPMODE_DIRECTIONAL)
    {
        float lum = Luminance(energyFromHit);

        INDEX_SAFE(directionalExpandedBuffer, expandedRayIdx).xyz += direction * lum;
        INDEX_SAFE(directionalExpandedBuffer, expandedRayIdx).w += lum;
    }
}

__kernel void processLightRaysFromBounce(
    INPUT_BUFFER( 00, LightBuffer,       indirectLightsBuffer),
    INPUT_BUFFER( 01, LightSample,       lightSamplesCompactedBuffer),
    OUTPUT_BUFFER(02, PowerSamplingStat, usePowerSamplingBuffer),
    INPUT_BUFFER( 03, float,             angularFalloffLUT_buffer),
    INPUT_BUFFER( 04, float,             distanceFalloffs_buffer),
    INPUT_BUFFER( 05, int,               cookiesBuffer),
    INPUT_BUFFER( 06, ray,               pathRaysCompactedBuffer_0),
    INPUT_BUFFER( 07, Intersection,      pathIntersectionsCompactedBuffer),
    INPUT_BUFFER( 08, ray,               lightRaysCompactedBuffer),
    INPUT_BUFFER( 09, float4,            lightOcclusionCompactedBuffer),
    INPUT_BUFFER( 10, float4,            pathThroughputExpandedBuffer),
    INPUT_BUFFER( 11, uint,              lightRayIndexToPathRayIndexCompactedBuffer),
    INPUT_BUFFER( 12, uint,              lightRaysCountBuffer),
    INPUT_BUFFER( 13, float4,            originalRaysExpandedBuffer),
    INPUT_VALUE(  14, int,               updatePowerSamplingBuffer),
#ifdef PROBES
    INPUT_VALUE(  15, int,               totalSampleCount),
    OUTPUT_BUFFER(16, float4,            probeSHExpandedBuffer)
#else
    INPUT_VALUE(  15, int,               lightmapMode),
    OUTPUT_BUFFER(16, float3,            lightingExpandedBuffer),
    OUTPUT_BUFFER(17, float4,            directionalExpandedBuffer),
    INPUT_VALUE(  18, int,               lightmapSize),
    INPUT_VALUE(  19, uint,              currentTileIdx),
    INPUT_VALUE(  20, uint,              sqrtNumTiles)
#endif
    KERNEL_VALIDATOR_BUFFERS_DEF
    )
{
    uint compactedLightRayIdx = get_global_id(0);
    int numLightHitCount = 0;
    int numLightRayCount = 0;

    bool shouldProcessRay = true;
#if DISALLOW_LIGHT_RAYS_COMPACTION
    if (Ray_IsInactive(GET_PTR_SAFE(lightRaysCompactedBuffer, compactedLightRayIdx)))
    {
        shouldProcessRay = false;
    }
#endif

    if (shouldProcessRay && compactedLightRayIdx < INDEX_SAFE(lightRaysCountBuffer, 0))
    {
        KERNEL_ASSERT(Ray_IsActive(GET_PTR_SAFE(lightRaysCompactedBuffer, compactedLightRayIdx)));
        const int compactedPathRayIdx = INDEX_SAFE(lightRayIndexToPathRayIndexCompactedBuffer, compactedLightRayIdx);
        KERNEL_ASSERT(Ray_IsActive(GET_PTR_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIdx)));
        const bool pathRayHitSomething = INDEX_SAFE(pathIntersectionsCompactedBuffer, compactedPathRayIdx).shapeid != MISS_MARKER;
        KERNEL_ASSERT(pathRayHitSomething);

        const int expandedRayIdx = Ray_GetSampleDescriptionIndex(GET_PTR_SAFE(lightRaysCompactedBuffer, compactedLightRayIdx));
        LightSample lightSample = INDEX_SAFE(lightSamplesCompactedBuffer, compactedLightRayIdx);
        LightBuffer light = INDEX_SAFE(indirectLightsBuffer, lightSample.lightIdx);

        bool useShadows = light.castShadow;
        const float4 occlusions4 = useShadows ? INDEX_SAFE(lightOcclusionCompactedBuffer, compactedLightRayIdx) : make_float4(1.0f, 1.0f, 1.0f, 1.0f);
        const bool  isLightOccludedFromBounce = occlusions4.w < TRANSMISSION_THRESHOLD;

        if (!isLightOccludedFromBounce)
        {
            const float t = INDEX_SAFE(pathIntersectionsCompactedBuffer, compactedPathRayIdx).uvwt.w;
            //We need to compute direct lighting on the fly
            float3 surfacePosition = INDEX_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIdx).o.xyz + INDEX_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIdx).d.xyz * t;
            float3 albedoAttenuation = INDEX_SAFE(pathThroughputExpandedBuffer, expandedRayIdx).xyz;

            float3 directLightingAtHit = occlusions4.xyz * ShadeLight(light, INDEX_SAFE(lightRaysCompactedBuffer, compactedLightRayIdx), surfacePosition, angularFalloffLUT_buffer, distanceFalloffs_buffer, cookiesBuffer KERNEL_VALIDATOR_BUFFERS) / lightSample.lightPdf;

            // The original direction from which the rays was shot from the probe position
            float4 originalRayDirection = INDEX_SAFE(originalRaysExpandedBuffer, expandedRayIdx);
#ifdef PROBES
            float3 L = albedoAttenuation * directLightingAtHit;
            float weight = 4.0 / totalSampleCount;
            accumulateSHExpanded(L, originalRayDirection, weight, probeSHExpandedBuffer, expandedRayIdx KERNEL_VALIDATOR_BUFFERS);
#else
            AccumulateLightFromBounce(albedoAttenuation, directLightingAtHit, expandedRayIdx, lightingExpandedBuffer, lightmapMode, directionalExpandedBuffer, originalRayDirection.xyz KERNEL_VALIDATOR_BUFFERS);
#endif
            numLightHitCount++;
        }
        numLightRayCount++;

        if (updatePowerSamplingBuffer)
        {
            const int globalTexelIndex = Ray_GetSourceIndex(GET_PTR_SAFE(lightRaysCompactedBuffer, compactedLightRayIdx));
#ifndef PROBES
            const int localTexelIndex = GetLocalIndex(globalTexelIndex, lightmapSize, currentTileIdx, sqrtNumTiles);
#else
            const int localTexelIndex = globalTexelIndex;
#endif
            if (usePowerSampling(localTexelIndex, usePowerSamplingBuffer KERNEL_VALIDATOR_BUFFERS))
            {
                atomic_add(&INDEX_SAFE(usePowerSamplingBuffer, localTexelIndex).LightHitCount, numLightHitCount);
                atomic_add(&INDEX_SAFE(usePowerSamplingBuffer, localTexelIndex).LightRayCount, numLightRayCount);
            }
        }
    }
}

__kernel void processEmissiveAndAOFromBounce(
    INPUT_BUFFER( 00, ray,                       pathRaysCompactedBuffer_0),
    INPUT_BUFFER( 01, Intersection,              pathIntersectionsCompactedBuffer),
    INPUT_BUFFER( 02, MaterialTextureProperties, instanceIdToEmissiveTextureProperties),
    INPUT_BUFFER( 03, float2,                    geometryUV1sBuffer),
    INPUT_BUFFER( 04, float4,                    dynarg_texture_buffer),
    INPUT_BUFFER( 05, MeshDataOffsets,           instanceIdToMeshDataOffsets),
    INPUT_BUFFER( 06, uint,                      geometryIndicesBuffer),
    INPUT_BUFFER( 07, float4,                    pathThroughputExpandedBuffer),
    INPUT_BUFFER( 08, uint,                      activePathCountBuffer_0),
    INPUT_BUFFER( 09, unsigned char,             pathLastNormalFacingTheRayCompactedBuffer),
    INPUT_BUFFER( 10, float4,                    originalRaysExpandedBuffer),
#ifdef PROBES
    INPUT_VALUE(  11, int,                       totalSampleCount),
    OUTPUT_BUFFER(12, float4,                    probeSHExpandedBuffer)
#else
    INPUT_VALUE(  11, int,                       lightmapMode),
    INPUT_VALUE(  12, float,                     aoMaxDistance),
    INPUT_VALUE(  13, int,                       bounce),
    OUTPUT_BUFFER(14, float3,                    lightingExpandedBuffer),
    OUTPUT_BUFFER(15, float4,                    directionalExpandedBuffer),
    OUTPUT_BUFFER(16, float4,                    shadowmaskAoValidityExpandedBuffer) //when gathering indirect .x will contain AO and .y will contain Validity
#endif
    KERNEL_VALIDATOR_BUFFERS_DEF
)
{
    uint compactedPathRayIdx = get_global_id(0);

    if (compactedPathRayIdx >= INDEX_SAFE(activePathCountBuffer_0, 0))
        return;

#if DISALLOW_PATH_RAYS_COMPACTION
    if (Ray_IsInactive(GET_PTR_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIdx)))
        return;
#endif

    KERNEL_ASSERT(Ray_IsActive(GET_PTR_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIdx)));
    const int expandedPathRayIdx = Ray_GetSampleDescriptionIndex(GET_PTR_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIdx));
    const bool  hit = INDEX_SAFE(pathIntersectionsCompactedBuffer, compactedPathRayIdx).shapeid != MISS_MARKER;

#ifndef PROBES
    const bool shouldAddOneToAOCount = (bounce == 0 && (!hit || INDEX_SAFE(pathIntersectionsCompactedBuffer, compactedPathRayIdx).uvwt.w > aoMaxDistance));
    if (shouldAddOneToAOCount)
    {
        INDEX_SAFE(shadowmaskAoValidityExpandedBuffer, expandedPathRayIdx).x += 1.0f;
    }
#endif

    if (hit)
    {
        AtlasInfo emissiveContribution = FetchEmissionFromRayIntersection(compactedPathRayIdx,
            pathIntersectionsCompactedBuffer,
            instanceIdToEmissiveTextureProperties,
            instanceIdToMeshDataOffsets,
            geometryUV1sBuffer,
            geometryIndicesBuffer,
            dynarg_texture_buffer
            KERNEL_VALIDATOR_BUFFERS
        );

        // If hit an invalid triangle (from the back, no double sided GI) we do not apply emissive.
        const unsigned char isNormalFacingTheRay = INDEX_SAFE(pathLastNormalFacingTheRayCompactedBuffer, compactedPathRayIdx);

        // The original direction from which the rays was shot
        float4 originalRayDirection = INDEX_SAFE(originalRaysExpandedBuffer, expandedPathRayIdx);

#ifdef PROBES
        float3 L = isNormalFacingTheRay * emissiveContribution.color.xyz * INDEX_SAFE(pathThroughputExpandedBuffer, expandedPathRayIdx).xyz;
        float weight = 4.0 / totalSampleCount;
        accumulateSHExpanded(L, originalRayDirection, weight, probeSHExpandedBuffer, expandedPathRayIdx KERNEL_VALIDATOR_BUFFERS);
#else
        float3 output = isNormalFacingTheRay * emissiveContribution.color.xyz * INDEX_SAFE(pathThroughputExpandedBuffer, expandedPathRayIdx).xyz;

        // Compute directionality from indirect
        if (lightmapMode == LIGHTMAPMODE_DIRECTIONAL)
        {
            float lum = Luminance(output);
            float4 directionality;
            directionality.xyz = originalRayDirection.xyz * lum;
            directionality.w = lum;
            INDEX_SAFE(directionalExpandedBuffer, expandedPathRayIdx) += directionality;
        }


        // Write Result
        INDEX_SAFE(lightingExpandedBuffer, expandedPathRayIdx).xyz += output.xyz;
#endif
    }
}

__kernel void advanceInPathAndAdjustPathProperties(
    INPUT_BUFFER( 00, ray,                       pathRaysCompactedBuffer_0),
    INPUT_BUFFER( 01, Intersection,              pathIntersectionsCompactedBuffer),
    INPUT_BUFFER( 02, MaterialTextureProperties, instanceIdToAlbedoTextureProperties),
    INPUT_BUFFER( 03, MeshDataOffsets,           instanceIdToMeshDataOffsets),
    INPUT_BUFFER( 04, float2,                    geometryUV1sBuffer),
    INPUT_BUFFER( 05, uint,                      geometryIndicesBuffer),
    INPUT_BUFFER( 06, uchar4,                    albedoTextures_buffer),
    INPUT_BUFFER( 07, uint,                      activePathCountBuffer_0),
    OUTPUT_BUFFER(08, float4,                    pathThroughputExpandedBuffer) //in & output
    KERNEL_VALIDATOR_BUFFERS_DEF
)
{
    uint compactedPathRayIdx = get_global_id(0);

    if (compactedPathRayIdx >= INDEX_SAFE(activePathCountBuffer_0, 0))
        return;

#if DISALLOW_PATH_RAYS_COMPACTION
    if (Ray_IsInactive(GET_PTR_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIdx)))
        return;
#endif

    KERNEL_ASSERT(Ray_IsActive(GET_PTR_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIdx)));
    const int expandedPathRayIdx = Ray_GetSampleDescriptionIndex(GET_PTR_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIdx));
    const bool  hit = INDEX_SAFE(pathIntersectionsCompactedBuffer, compactedPathRayIdx).shapeid != MISS_MARKER;
    if (!hit)
        return;

    AtlasInfo albedoAtHit = FetchAlbedoFromRayIntersection(compactedPathRayIdx,
        pathIntersectionsCompactedBuffer,
        instanceIdToAlbedoTextureProperties,
        instanceIdToMeshDataOffsets,
        geometryUV1sBuffer,
        geometryIndicesBuffer,
        albedoTextures_buffer
        KERNEL_VALIDATOR_BUFFERS);

    const float throughputAttenuation = dot(albedoAtHit.color.xyz, kAverageFactors);
    INDEX_SAFE(pathThroughputExpandedBuffer, expandedPathRayIdx) *= (float4)(albedoAtHit.color.x, albedoAtHit.color.y, albedoAtHit.color.z, throughputAttenuation);
}

__kernel void getNormalsFromLastBounceAndDoValidity(
    INPUT_BUFFER( 00, ray,                       pathRaysCompactedBuffer_0),              // rays from last to current hit
    INPUT_BUFFER( 01, Intersection,              pathIntersectionsCompactedBuffer),       // intersections from last to current hit
    INPUT_BUFFER( 02, MeshDataOffsets,           instanceIdToMeshDataOffsets),
    INPUT_BUFFER( 03, Matrix4x4,                 instanceIdToInvTransposedMatrices),
    INPUT_BUFFER( 04, Vector3f_storage,          geometryPositionsBuffer),
    INPUT_BUFFER( 05, PackedNormalOctQuad,       geometryNormalsBuffer),
    INPUT_BUFFER( 06, uint,                      geometryIndicesBuffer),
    INPUT_BUFFER( 07, uint,                      activePathCountBuffer_0),
    INPUT_BUFFER( 08, MaterialTextureProperties, instanceIdToTransmissionTextureProperties),
    INPUT_BUFFER( 09, float4,                    instanceIdToTransmissionTextureSTs),
    INPUT_BUFFER( 10, float2,                    geometryUV0sBuffer),
    INPUT_BUFFER( 11, uchar4,                    dynarg_texture_buffer),
    INPUT_VALUE(  12, int,                       primaryBufferMode),
    //output
    OUTPUT_BUFFER(13, PackedNormalOctQuad,       pathLastPlaneNormalCompactedBuffer),
    OUTPUT_BUFFER(14, PackedNormalOctQuad,       pathLastInterpNormalCompactedBuffer),
    OUTPUT_BUFFER(15, unsigned char,             pathLastNormalFacingTheRayCompactedBuffer),
    OUTPUT_BUFFER(16, float4,                    shadowmaskAoValidityExpandedBuffer) // Used to store validity in .y
#ifdef PROBES
    ,
    INPUT_VALUE(  17, int,                       lightmappingSourceType),
    OUTPUT_BUFFER(18, float,                     probeDepthOctahedronExpandedBuffer)
#endif
    KERNEL_VALIDATOR_BUFFERS_DEF
)
{
    uint compactedPathRayIdx = get_global_id(0);

    if (compactedPathRayIdx >= INDEX_SAFE(activePathCountBuffer_0, 0))
        return;

#if DISALLOW_PATH_RAYS_COMPACTION
    if (Ray_IsInactive(GET_PTR_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIdx)))
        return;
#endif

    KERNEL_ASSERT(Ray_IsActive(GET_PTR_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIdx)));
    if (INDEX_SAFE(pathIntersectionsCompactedBuffer, compactedPathRayIdx).shapeid == MISS_MARKER)
    {
        PackedNormalOctQuad zero;
        zero.x = 0xffffffff; // Will yield a decoded value of float3(0, 0, -1)
        INDEX_SAFE(pathLastPlaneNormalCompactedBuffer, compactedPathRayIdx) = zero;
        INDEX_SAFE(pathLastInterpNormalCompactedBuffer, compactedPathRayIdx) = zero;
        INDEX_SAFE(pathLastNormalFacingTheRayCompactedBuffer, compactedPathRayIdx) = 0;
        return;
    }

    const int instanceId = GetInstanceIdFromIntersection(GET_PTR_SAFE(pathIntersectionsCompactedBuffer, compactedPathRayIdx));
    float3 planeNormalWS;
    float3 interpVertexNormalWS;
    GetNormalsAtRayIntersection(compactedPathRayIdx,
        instanceId,
        pathIntersectionsCompactedBuffer,
        instanceIdToMeshDataOffsets,
        instanceIdToInvTransposedMatrices,
        geometryPositionsBuffer,
        geometryNormalsBuffer,
        geometryIndicesBuffer,
        &planeNormalWS,
        &interpVertexNormalWS
        KERNEL_VALIDATOR_BUFFERS);

    unsigned char isNormalFacingTheRay = 1;
    float3 rayDirection = INDEX_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIdx).d.xyz;
    const bool frontFacing = dot(planeNormalWS, rayDirection) <= 0.0f;
    const int expandedPathRayIdx = Ray_GetSampleDescriptionIndex(GET_PTR_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIdx));

    bool isRayValid = true;

    if (!frontFacing)
    {
        const MaterialTextureProperties matProperty = INDEX_SAFE(instanceIdToTransmissionTextureProperties, instanceId);
        const bool isDoubleSidedGI = GetMaterialProperty(matProperty, kMaterialInstanceProperties_DoubleSidedGI);
        planeNormalWS =        isDoubleSidedGI ? -planeNormalWS        : planeNormalWS;
        interpVertexNormalWS = isDoubleSidedGI ? -interpVertexNormalWS : interpVertexNormalWS;
        isNormalFacingTheRay = isDoubleSidedGI? 1 : 0;
        if (primaryBufferMode == PrimaryBufferMode_Generate && !isDoubleSidedGI)
        {
            const bool isTransparent = GetMaterialProperty(matProperty, kMaterialInstanceProperties_UseTransmission);
            if (!isTransparent)
            {
                // We use the shadowmaskAoValidityExpandedBuffer.y to store validity to avoid having an additional expanded buffer.
                INDEX_SAFE(shadowmaskAoValidityExpandedBuffer, expandedPathRayIdx).y = 1.0f;
                isRayValid = false;
            }
            else
            {
                // sample transmission texture
                const float2 barycentricCoord = INDEX_SAFE(pathIntersectionsCompactedBuffer, compactedPathRayIdx).uvwt.xy;
                const int primIndex = INDEX_SAFE(pathIntersectionsCompactedBuffer, compactedPathRayIdx).primid;
                const float2 geometryUVs = GetUVsAtPrimitiveIntersection(instanceId, primIndex, barycentricCoord, instanceIdToMeshDataOffsets, geometryUV0sBuffer, geometryIndicesBuffer KERNEL_VALIDATOR_BUFFERS);
                const float2 textureUVs = geometryUVs * INDEX_SAFE(instanceIdToTransmissionTextureSTs, instanceId).xy + INDEX_SAFE(instanceIdToTransmissionTextureSTs, instanceId).zw;
                const float4 transmission = FetchUChar4TextureFromMaterialAndUVs(dynarg_texture_buffer, textureUVs, matProperty, false, false KERNEL_VALIDATOR_BUFFERS);
                const float averageTransmission = dot(transmission.xyz, kAverageFactors);

                // We use the shadowmaskAoValidityExpandedBuffer.y to store validity to avoid having an additional expanded buffer.
                INDEX_SAFE(shadowmaskAoValidityExpandedBuffer, expandedPathRayIdx).y = 1.0f - averageTransmission;
                isRayValid = (averageTransmission > 0.99f);
            }
        }
    }

#ifdef PROBES
    if (probeDepthOctahedronExpandedBuffer)
    {
        if (primaryBufferMode == PrimaryBufferMode_Generate && isRayValid && lightmappingSourceType == kLightmappingSourceType_Probe)
        {
            float t = INDEX_SAFE(pathIntersectionsCompactedBuffer, compactedPathRayIdx).uvwt.w;
            float2 normalizedOctCoord = PackNormalOctQuadEncoded(normalize(rayDirection));
            int texel = GetOctQuadEncodedTexelFromPackedNormal(normalizedOctCoord, OCTAHEDRON_SIZE);
            float3 texelDirection = UnpackNormalOctQuadEncoded(normalizedOctCoord);
            float weight = max(0.0f, dot(texelDirection, rayDirection));
            INDEX_SAFE(probeDepthOctahedronExpandedBuffer, expandedPathRayIdx * OCTAHEDRON_TEXEL_COUNT + texel) += t * weight;
        }
    }
#endif

    // Store normals for various kernels to use later
    INDEX_SAFE(pathLastPlaneNormalCompactedBuffer, compactedPathRayIdx) = EncodeNormalToUint(planeNormalWS);
    INDEX_SAFE(pathLastInterpNormalCompactedBuffer, compactedPathRayIdx) = EncodeNormalToUint(interpVertexNormalWS);
    INDEX_SAFE(pathLastNormalFacingTheRayCompactedBuffer, compactedPathRayIdx) = isNormalFacingTheRay;
}


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\OpenCL\kernels\processBounce.cl---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\OpenCL\kernels\processEnvironment.cl---------------


#include "environmentLighting.h"


__kernel void processDirectEnvironment(
    //*** input ***
    INPUT_BUFFER( 00, ray,                 lightRaysCompactedBuffer),
    INPUT_BUFFER( 01, uint,                lightRaysCountBuffer),
    INPUT_BUFFER( 02, float4,              lightOcclusionCompactedBuffer),
    INPUT_BUFFER( 03, float4,              env_mipped_cube_texels_buffer),
    INPUT_BUFFER( 04, int,                 env_mip_offsets_buffer),
    INPUT_VALUE(  05, Environment,         envData),
    INPUT_VALUE(  06, uint,                currentTileIdx),
    INPUT_VALUE(  07, uint,                sqrtNumTiles),
#ifdef PROBES
    INPUT_VALUE(  08, int,                 totalSampleCount),
    //*** output ***
    OUTPUT_BUFFER(09, float4,              probeSHExpandedBuffer)
#else
    INPUT_BUFFER( 08, PackedNormalOctQuad, interpNormalsWSBuffer),
    INPUT_VALUE(  09, int,                 lightmapMode),
    INPUT_VALUE(  10, int,                 superSamplingMultiplier),
    INPUT_VALUE(  11, int,                 lightmapSize),
    INPUT_BUFFER( 12, SampleDescription,   sampleDescriptionsExpandedBuffer),
    INPUT_BUFFER( 13, uint,                sobol_buffer),
    INPUT_BUFFER( 14, float,               goldenSample_buffer),
    //*** output ***
    OUTPUT_BUFFER(15, float4,              directionalExpandedBuffer),
    OUTPUT_BUFFER(16, float3,              lightingExpandedBuffer)

#endif
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    uint compactedRayIdx = get_global_id(0);
    if (compactedRayIdx >= INDEX_SAFE(lightRaysCountBuffer, 0))
        return;

#if DISALLOW_LIGHT_RAYS_COMPACTION
    if (Ray_IsInactive(GET_PTR_SAFE(lightRaysCompactedBuffer, compactedRayIdx)))
        return;
#endif

    KERNEL_ASSERT(Ray_IsActive(GET_PTR_SAFE(lightRaysCompactedBuffer, compactedRayIdx)));
    int sampleDescriptionIdx = Ray_GetSampleDescriptionIndex(GET_PTR_SAFE(lightRaysCompactedBuffer, compactedRayIdx));

    float4 occlusion = INDEX_SAFE(lightOcclusionCompactedBuffer, compactedRayIdx);
    bool   occluded  = occlusion.w < TRANSMISSION_THRESHOLD;

    if (!occluded)
    {
        // Environment intersection
        float4 dir          = INDEX_SAFE(lightRaysCompactedBuffer, compactedRayIdx).d;
        float3 color        = make_float3(0.0f, 0.0f, 0.0f);
#ifdef PROBES
        if (UseEnvironmentImportanceSampling(envData.flags))
            color = ProcessVolumeEnvironmentRayIS(dir, envData.envDim, envData.numMips, envData.envmapIntegral, env_mipped_cube_texels_buffer, env_mip_offsets_buffer KERNEL_VALIDATOR_BUFFERS);
        else
            color = ProcessVolumeEnvironmentRay(dir, envData.envDim, envData.numMips, env_mipped_cube_texels_buffer, env_mip_offsets_buffer KERNEL_VALIDATOR_BUFFERS);

        // accumulate environment lighting
        color *= occlusion.xyz;
        KERNEL_ASSERT(totalSampleCount > 0);
        float weight = 1.0f / totalSampleCount;
        accumulateSHExpanded(color.xyz, dir, weight, probeSHExpandedBuffer, sampleDescriptionIdx KERNEL_VALIDATOR_BUFFERS);
#else
        const SampleDescription sampleDescription = INDEX_SAFE(sampleDescriptionsExpandedBuffer, sampleDescriptionIdx);
        // Remap the global lightmap index to the local, super-sampled space of the tile
        const int localIdx = GetSuperSampledLocalIndex(sampleDescription.texelIndex, lightmapSize, sampleDescription.currentSampleCount, superSamplingMultiplier, currentTileIdx, sqrtNumTiles, sobol_buffer, goldenSample_buffer KERNEL_VALIDATOR_BUFFERS);

        float3 interpNormal = DecodeNormal(INDEX_SAFE(interpNormalsWSBuffer, localIdx));
        if (UseEnvironmentImportanceSampling(envData.flags))
            color = ProcessEnvironmentRayIS(dir, interpNormal, envData.envDim, envData.numMips, envData.envmapIntegral, env_mipped_cube_texels_buffer, env_mip_offsets_buffer KERNEL_VALIDATOR_BUFFERS);
        else
            color = ProcessEnvironmentRay(dir, envData.envDim, envData.numMips, env_mipped_cube_texels_buffer, env_mip_offsets_buffer KERNEL_VALIDATOR_BUFFERS);

        // accumulate environment lighting
        INDEX_SAFE(lightingExpandedBuffer, sampleDescriptionIdx) += occlusion.xyz * color;

        //compute directionality from indirect
        if (lightmapMode == LIGHTMAPMODE_DIRECTIONAL)
        {
            float  luminance   = Luminance(color);
            float3 scaledDir   = dir.xyz * luminance;
            float4 directional = make_float4(scaledDir.x, scaledDir.y, scaledDir.z, luminance);
            INDEX_SAFE(directionalExpandedBuffer, sampleDescriptionIdx) += directional;
        }
#endif
    }
}

__kernel void processIndirectEnvironment(
    INPUT_BUFFER(  0, ray,                 lightRaysCompactedBuffer),
    INPUT_BUFFER(  1, uint,                lightRaysCountBuffer),
    INPUT_BUFFER(  2, ray,                 pathRaysCompactedBuffer_0),//Only for kernel assert purpose.
    INPUT_BUFFER(  3, uint,                activePathCountBuffer_0),//Only for kernel assert purpose.
    INPUT_BUFFER(  4, uint,                lightRayIndexToPathRayIndexCompactedBuffer),
    INPUT_BUFFER(  5, float4,              originalRaysExpandedBuffer),
    INPUT_BUFFER(  6, float4,              lightOcclusionCompactedBuffer),
    INPUT_BUFFER(  7, float4,              pathThroughputExpandedBuffer),
    INPUT_BUFFER(  8, PackedNormalOctQuad, pathLastInterpNormalCompactedBuffer),
    INPUT_BUFFER(  9, float4,              env_mipped_cube_texels_buffer),
    INPUT_BUFFER( 10, int,                 env_mip_offsets_buffer),
    INPUT_VALUE(  11, Environment,         envData),
#ifdef PROBES
    INPUT_VALUE(  12, int,                 totalSampleCount),
    OUTPUT_BUFFER(13, float4,              probeSHExpandedBuffer)
#else
    INPUT_VALUE(  12, int,                 lightmapMode),
    OUTPUT_BUFFER(13, float3,              lightingExpandedBuffer),
    OUTPUT_BUFFER(14, float4,              directionalExpandedBuffer)
#endif
    KERNEL_VALIDATOR_BUFFERS_DEF
)
{
    uint compactedLightRayIdx = get_global_id(0);
    bool shouldProcessRay = true;

#if DISALLOW_LIGHT_RAYS_COMPACTION
    if (Ray_IsInactive(GET_PTR_SAFE(lightRaysCompactedBuffer, compactedLightRayIdx)))
    {
        shouldProcessRay = false;
    }
#endif

    if (shouldProcessRay && compactedLightRayIdx < INDEX_SAFE(lightRaysCountBuffer, 0))
    {
        KERNEL_ASSERT(Ray_IsActive(GET_PTR_SAFE(lightRaysCompactedBuffer, compactedLightRayIdx)));
        const int compactedPathRayIdx = INDEX_SAFE(lightRayIndexToPathRayIndexCompactedBuffer, compactedLightRayIdx);
        KERNEL_ASSERT(compactedPathRayIdx < INDEX_SAFE(activePathCountBuffer_0, 0));
        KERNEL_ASSERT(Ray_IsActive(GET_PTR_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIdx)));
        const int expandedRayIdx = Ray_GetSampleDescriptionIndex(GET_PTR_SAFE(lightRaysCompactedBuffer, compactedLightRayIdx));

        float4 occlusion = INDEX_SAFE(lightOcclusionCompactedBuffer, compactedLightRayIdx);
        bool   occluded = occlusion.w < TRANSMISSION_THRESHOLD;
        if (!occluded)
        {
            // Environment intersection
            float3 interpNormal = DecodeNormal(INDEX_SAFE(pathLastInterpNormalCompactedBuffer, compactedPathRayIdx));
            float4 dir = INDEX_SAFE(lightRaysCompactedBuffer, compactedLightRayIdx).d;
            float3 color;

            if (UseEnvironmentImportanceSampling(envData.flags))
                color = ProcessEnvironmentRayIS(dir, interpNormal, envData.envDim, envData.numMips, envData.envmapIntegral, env_mipped_cube_texels_buffer, env_mip_offsets_buffer KERNEL_VALIDATOR_BUFFERS);
            else
                color = ProcessEnvironmentRay(dir, envData.envDim, envData.numMips, env_mipped_cube_texels_buffer, env_mip_offsets_buffer KERNEL_VALIDATOR_BUFFERS);

            color *= occlusion.xyz * INDEX_SAFE(pathThroughputExpandedBuffer, expandedRayIdx).xyz;
            float4 originalRayDirection = INDEX_SAFE(originalRaysExpandedBuffer, expandedRayIdx);
#ifdef PROBES
            float weight = 4.0f / totalSampleCount;
            accumulateSHExpanded(color.xyz, originalRayDirection, weight, probeSHExpandedBuffer, expandedRayIdx KERNEL_VALIDATOR_BUFFERS);
#else
            //compute directionality from indirect
            if (lightmapMode == LIGHTMAPMODE_DIRECTIONAL)
            {
                float luminance = Luminance(color);
                originalRayDirection.xyz *= luminance;
                originalRayDirection.w = luminance;
                INDEX_SAFE(directionalExpandedBuffer, expandedRayIdx) += originalRayDirection;
            }
            // accumulate environment lighting
            INDEX_SAFE(lightingExpandedBuffer, expandedRayIdx) += occlusion.xyz * color;
#endif
        }
    }
}


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\OpenCL\kernels\processEnvironment.cl---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\OpenCL\kernels\processLightRays.cl---------------


#include "commonCL.h"
#include "directLighting.h"

__kernel void processLightRays(
    // Inputs
    INPUT_BUFFER( 00, ray,                 lightRaysCompactedBuffer),
    INPUT_BUFFER( 01, float4,              positionsWSBuffer),
    INPUT_BUFFER( 02, LightBuffer,         directLightsBuffer),
    INPUT_BUFFER( 03, LightSample,         lightSamplesCompactedBuffer),
    INPUT_BUFFER( 04, float4,              lightOcclusionCompactedBuffer),
    INPUT_BUFFER( 05, float,               angularFalloffLUT_buffer),
    INPUT_BUFFER( 06, float,               distanceFalloffs_buffer),
    INPUT_BUFFER( 07, int,                 cookiesBuffer),
    INPUT_BUFFER( 08, uint,                lightRaysCountBuffer),
    INPUT_VALUE(  09, uint,                currentTileIdx),
    INPUT_VALUE(  10, uint,                sqrtNumTiles),
#ifdef PROBES
    INPUT_VALUE(  11, int,                 totalSampleCount),
    INPUT_BUFFER( 12, float4,              inputLightIndicesBuffer),
    // Outputs
    OUTPUT_BUFFER(13, float4,              probeSHExpandedBuffer),
    OUTPUT_BUFFER(14, float4,              probeOcclusionExpandedBuffer)
#else
    INPUT_BUFFER( 11, SampleDescription,   sampleDescriptionsExpandedBuffer),
    INPUT_BUFFER( 12, PackedNormalOctQuad, planeNormalsWSBuffer),
    INPUT_VALUE(  13, float,               pushOff),
    INPUT_VALUE(  14, int,                 lightmapMode),
    INPUT_VALUE(  15, int,                 superSamplingMultiplier),
    INPUT_VALUE(  16, int,                 lightmapSize),
    INPUT_BUFFER( 17, unsigned char,       gbufferInstanceIdToReceiveShadowsBuffer),
    INPUT_BUFFER( 18, uint,                sobol_buffer),
    INPUT_BUFFER( 19, float,               goldenSample_buffer),
    // Outputs
    OUTPUT_BUFFER(20, float4,              shadowmaskAoValidityExpandedBuffer),
    OUTPUT_BUFFER(21, float4,              directionalExpandedBuffer),
    OUTPUT_BUFFER(22, float3,              lightingExpandedBuffer)
#endif
    KERNEL_VALIDATOR_BUFFERS_DEF
)
{
    uint compactedRayIdx = get_global_id(0);
    if (compactedRayIdx >= INDEX_SAFE(lightRaysCountBuffer, 0))
        return;

#if DISALLOW_LIGHT_RAYS_COMPACTION
    if (Ray_IsInactive(GET_PTR_SAFE(lightRaysCompactedBuffer, compactedRayIdx)))
        return;
#endif

    KERNEL_ASSERT(Ray_IsActive(GET_PTR_SAFE(lightRaysCompactedBuffer, compactedRayIdx)));
    int sampleDescriptionIdx = Ray_GetSampleDescriptionIndex(GET_PTR_SAFE(lightRaysCompactedBuffer, compactedRayIdx));
    LightSample lightSample = INDEX_SAFE(lightSamplesCompactedBuffer, compactedRayIdx);
    LightBuffer light = INDEX_SAFE(directLightsBuffer, lightSample.lightIdx);

#ifndef PROBES
    const SampleDescription sampleDescription = INDEX_SAFE(sampleDescriptionsExpandedBuffer, sampleDescriptionIdx);
    const int texelIndex = sampleDescription.texelIndex;

    // Remap the global lightmap index to the local, super-sampled space of the tile
    const int localIdx = GetSuperSampledLocalIndex(sampleDescription.texelIndex, lightmapSize, sampleDescription.currentSampleCount, superSamplingMultiplier, currentTileIdx, sqrtNumTiles, sobol_buffer, goldenSample_buffer KERNEL_VALIDATOR_BUFFERS);

    const float4 positionAndGbufferInstanceId = INDEX_SAFE(positionsWSBuffer, localIdx);
    const int gBufferInstanceId = (int)(floor(positionAndGbufferInstanceId.w));
    const float3 P = positionAndGbufferInstanceId.xyz;
    const float3 planeNormal = DecodeNormal(INDEX_SAFE(planeNormalsWSBuffer, localIdx));
    const float3 position = P + planeNormal * pushOff;
#else
    const int texelIndex = Ray_GetSourceIndex(GET_PTR_SAFE(lightRaysCompactedBuffer, compactedRayIdx));
    const float3 position = INDEX_SAFE(positionsWSBuffer, texelIndex).xyz;
#endif

    bool useShadows = light.castShadow;
#ifndef PROBES
    useShadows &= INDEX_SAFE(gbufferInstanceIdToReceiveShadowsBuffer, gBufferInstanceId);
#endif
    float4 occlusions4 = useShadows ? INDEX_SAFE(lightOcclusionCompactedBuffer, compactedRayIdx) : make_float4(1.0f, 1.0f, 1.0f, 1.0f);
    const bool hit = occlusions4.w < TRANSMISSION_THRESHOLD;
    if (!hit)
    {
#ifdef PROBES
        const float weight = 1.0 / totalSampleCount;
        if (light.directBakeMode >= kDirectBakeMode_Subtractive)
        {
            int lightIdx = light.probeOcclusionLightIndex;
            const float4 lightIndicesFloat = INDEX_SAFE(inputLightIndicesBuffer, texelIndex);
            int4 lightIndices = (int4)((int)(lightIndicesFloat.x), (int)(lightIndicesFloat.y), (int)(lightIndicesFloat.z), (int)(lightIndicesFloat.w));
            float4 channelSelector = (float4)((lightIndices.x == lightIdx) ? 1.0f : 0.0f, (lightIndices.y == lightIdx) ? 1.0f : 0.0f, (lightIndices.z == lightIdx) ? 1.0f : 0.0f, (lightIndices.w == lightIdx) ? 1.0f : 0.0f);
            INDEX_SAFE(probeOcclusionExpandedBuffer, sampleDescriptionIdx) += channelSelector * weight;
        }
        else if (light.directBakeMode != kDirectBakeMode_None)
        {
            float4 D = (float4)(INDEX_SAFE(lightRaysCompactedBuffer, compactedRayIdx).d.x, INDEX_SAFE(lightRaysCompactedBuffer, compactedRayIdx).d.y, INDEX_SAFE(lightRaysCompactedBuffer, compactedRayIdx).d.z, 0);
            float3 L = ShadeLight(light, INDEX_SAFE(lightRaysCompactedBuffer, compactedRayIdx), position, angularFalloffLUT_buffer, distanceFalloffs_buffer, cookiesBuffer KERNEL_VALIDATOR_BUFFERS);
            accumulateSHExpanded(L, D, weight, probeSHExpandedBuffer, sampleDescriptionIdx KERNEL_VALIDATOR_BUFFERS);
        }
#else
        if (light.directBakeMode >= kDirectBakeMode_OcclusionChannel0)
        {
            float4 channelSelector = (float4)(light.directBakeMode == kDirectBakeMode_OcclusionChannel0 ? 1.0f : 0.0f, light.directBakeMode == kDirectBakeMode_OcclusionChannel1 ? 1.0f : 0.0f, light.directBakeMode == kDirectBakeMode_OcclusionChannel2 ? 1.0f : 0.0f, light.directBakeMode == kDirectBakeMode_OcclusionChannel3 ? 1.0f : 0.0f);
            INDEX_SAFE(shadowmaskAoValidityExpandedBuffer, sampleDescriptionIdx) += occlusions4.w * channelSelector;
        }
        else if (light.directBakeMode != kDirectBakeMode_None)
        {
            const float3 lighting = occlusions4.xyz * ShadeLight(light, INDEX_SAFE(lightRaysCompactedBuffer, compactedRayIdx), position, angularFalloffLUT_buffer, distanceFalloffs_buffer, cookiesBuffer KERNEL_VALIDATOR_BUFFERS);
            INDEX_SAFE(lightingExpandedBuffer, sampleDescriptionIdx).xyz += lighting;

            //compute directionality from direct lighting
            if (lightmapMode == LIGHTMAPMODE_DIRECTIONAL)
            {
                float lum = Luminance(lighting);
                float4 directionality;
                directionality.xyz = INDEX_SAFE(lightRaysCompactedBuffer, compactedRayIdx).d.xyz * lum;
                directionality.w = lum;
                INDEX_SAFE(directionalExpandedBuffer, sampleDescriptionIdx) += directionality;
            }
        }
#endif
    }
}


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\OpenCL\kernels\processLightRays.cl---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\OpenCL\kernels\atrousFilter.cl---------------
.
.
/* Edge-Avoiding A-Trous Wavelet Filter
   see: https://jo.dreggn.org/home/2010_atrous.pdf
*/

#include "commonCL.h"

#if !defined(BLOCK_SIZE)
#define BLOCK_SIZE 8
#endif

// ----------------------------------------------------------------------------------------------------

#define REFLECT(x, max) \
   if (x < 0) x = - x - 1; \
   if (x >= max) x =  2 * max - x - 1;

// ----------------------------------------------------------------------------------------------------

#define FILL_LOCAL_BUFFER(source, srcBufferSize, coord, localBuff, halfWindow, localStart) \
do \
{ \
   int localX = get_local_id(0); \
   int localY = get_local_id(1); \
   int beg = 0, end = 0; \
   int windowSize = (BLOCK_SIZE + halfWindow*2)*(BLOCK_SIZE + halfWindow*2); \
   int block = ceil((float)(windowSize) / (BLOCK_SIZE * BLOCK_SIZE)); \
   if (localX + localY*BLOCK_SIZE <= ceil((float)(windowSize) / block)) \
   { \
      beg = (localX + localY*BLOCK_SIZE)*block; \
      end = beg + block; \
      end = clamp(end, 0, windowSize); \
   } \
   localStart = (int2)((coord.x & (~(BLOCK_SIZE - 1))) - halfWindow, (coord.y & (~(BLOCK_SIZE - 1))) - halfWindow); \
   localStart = clamp(localStart, (int2)0, srcBufferSize - (int2)1); \
   for (int i = beg; i < end; ++i) \
   { \
      int2 xy = (int2)(localStart.x + (i % (BLOCK_SIZE + halfWindow*2)), localStart.y + (i / (BLOCK_SIZE + halfWindow*2))); \
      xy = clamp(xy, 0, srcBufferSize - (int2)1); \
      localBuff[i] = read_imagef(source, kSamplerClampNearestUnormCoords, xy); \
   } \
   barrier(CLK_LOCAL_MEM_FENCE); \
} \
while(0);

// ----------------------------------------------------------------------------------------------------

#define DERIVATE(buffer, bufferSize, coord, halfWindow, localStart, dFdX, dFdY) \
do \
{ \
   int left = clamp(coord.x - 1, 0, bufferSize.x - 1); \
   int right = clamp(coord.x + 1, 0, bufferSize.x - 1); \
   int top = clamp(coord.y - 1, 0, bufferSize.y - 1); \
   int bottom = clamp(coord.y + 1, 0, bufferSize.y - 1); \
   dFdX = (buffer[(left - localStart.x) + (BLOCK_SIZE + halfWindow*2)*(top - localStart.y)]  \
        - buffer[(right - localStart.x) + (BLOCK_SIZE + halfWindow*2)*(top - localStart.y)] \
        + 2 * (buffer[(left - localStart.x) + (BLOCK_SIZE + halfWindow*2)*(coord.y - localStart.y)]  \
               - buffer[(right - localStart.x) + (BLOCK_SIZE + halfWindow*2)*(coord.y - localStart.y)]) \
        + buffer[(left - localStart.x) + (BLOCK_SIZE + halfWindow*2)*(bottom - localStart.y)] \
        - buffer[(right - localStart.x) + (BLOCK_SIZE + halfWindow*2)*(bottom - localStart.y)]); \
   dFdY = (buffer[(left - localStart.x) + (BLOCK_SIZE + halfWindow*2)*(top - localStart.y)]  \
        - buffer[(left - localStart.x) + (BLOCK_SIZE + halfWindow*2)*(bottom - localStart.y)] \
        + 2 * (buffer[(coord.x - localStart.x) + (BLOCK_SIZE + halfWindow*2)*(top - localStart.y)]  \
               - buffer[(coord.x - localStart.x) + (BLOCK_SIZE + halfWindow*2)*(bottom - localStart.y)]) \
        + buffer[(right - localStart.x) + (BLOCK_SIZE + halfWindow*2)*(top - localStart.y)] \
        - buffer[(right - localStart.x) + (BLOCK_SIZE + halfWindow*2)*(bottom - localStart.y)]); \
} \
while(0);


// ----------------------------------------------------------------------------------------------------
// Similarity function
static inline float C(float3 x1, float3 x2, float sigma)
{
   float3 distance = x1 - x2;
   float a = fast_length(convert_float3(distance)) / sigma;
   return native_exp(-a);
}

// ----------------------------------------------------------------------------------------------------
// Depth similarity function
static inline float dW(float x1, float x2, float sigma)
{
   float a = fabs(x1 - x2) / sigma;
   return native_exp(-a);
}

// ----------------------------------------------------------------------------------------------------
// Normals similarity function
static inline float nW(float3 x1, float3 x2, float sigma)
{
   x1 = normalize(x1 + make_float3(0.01f, 0.01f, 0.01f));
   x2 = normalize(x2 + make_float3(0.01f, 0.01f, 0.01f));
   float a = fmax((float)0.0f, dot(x1, x2));

   return pow(a, (float)1.0f / sigma);
}

// ----------------------------------------------------------------------------------------------------
static inline float4 SampleGauss3x3F(__read_only image2d_t buffer, int2 buffer_size, int2 coord, __local float4* window)
{
   int2 tl = clamp(coord - (int2)1, (int2)0, buffer_size - (int2)1);
   int2 br = clamp(coord + (int2)1, (int2)0, buffer_size - (int2)1);
   int2 localStart;

   FILL_LOCAL_BUFFER(buffer, buffer_size, coord, window, 1, localStart);

   float4 bluredVal = 0.077847f * (
      window[(tl.x - localStart.x) + (BLOCK_SIZE + 2)*(tl.y - localStart.y)]
      + window[(br.x - localStart.x) + (BLOCK_SIZE + 2)*(tl.y - localStart.y)]
      + window[(tl.x - localStart.x) + (BLOCK_SIZE + 2)*(br.y - localStart.y)]
      + window[(br.x - localStart.x) + (BLOCK_SIZE + 2)*(br.y - localStart.y)] );

   bluredVal += 0.123317f * (
      window[(coord.x - localStart.x) + (BLOCK_SIZE + 2)*(tl.y - localStart.y)]
      + window[(br.x - localStart.x) + (BLOCK_SIZE + 2)*(coord.y - localStart.y)]
      + window[(tl.x - localStart.x) + (BLOCK_SIZE + 2)*(coord.y - localStart.y)]
      + window[(coord.x - localStart.x) + (BLOCK_SIZE + 2)*(br.y - localStart.y)] );

   bluredVal += 0.195346f * window[(coord.x - localStart.x) + (BLOCK_SIZE + 2)*(coord.y - localStart.y)];
   return bluredVal;
}

// ----------------------------------------------------------------------------------------------------
__kernel void ATrousKernel(
    __read_only image2d_t dynarg_srcTile0, // Source buffer
    __read_only image2d_t dynarg_srcTile1, // Normals/ChartId buffer
    __read_only image2d_t dynarg_srcTile2, // Prev variance buffer
   __write_only image2d_t dynarg_dstTile , // Dest buffer
   __write_only image2d_t dynarg_dstTile1, // Dest variance buffer
    INPUT_VALUE( 5, int2  ,   imageSize),
    INPUT_VALUE( 6, float4,   sigma ),
    INPUT_VALUE( 7, int   ,   coordOffset )
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
   int2 coord = (int2)(get_global_id(0), get_global_id(1));

   float2 gradDpt = 0.0f;
   __local float4 window[(BLOCK_SIZE + 2)*(BLOCK_SIZE + 2)];
   float4 dFdX, dFdY;
   int2 localStart;
   FILL_LOCAL_BUFFER(dynarg_srcTile1, imageSize, coord, window, 1, localStart);
   DERIVATE(window, imageSize, coord, 1, localStart, dFdX, dFdY);
   barrier(CLK_LOCAL_MEM_FENCE);

   gradDpt.x = dFdX.w;
   gradDpt.y = dFdY.w;

   // color variance value
#ifdef FIRST_PASS
   float colVar = fast_length(convert_float3(SampleGauss3x3F(dynarg_srcTile0, imageSize, coord, window).xyz));
#else
   float colVar = READ_IMAGEF_SAFE(dynarg_srcTile2, kSamplerClampNearestUnormCoords, coord).x;
#endif

   //B3 spline
   const float kernl[] =
   {
      1.0f / 256, 1.0f / 64, 3.0f / 128, 1.0f / 64, 1.0f / 256,
      1.0f / 64, 1.0f / 16, 3.0f / 32, 1.0f / 16, 1.0f / 64,
      3.0f / 128, 3.0f / 32, 9.0f / 64, 3.0f / 32, 3.0f / 128,
      1.0f / 64, 1.0f / 16, 3.0f / 32, 1.0f / 16, 1.0f / 64,
      1.0f / 256, 1.0f / 64, 3.0f / 128, 1.0f / 64, 1.0f / 256
   };

   // color value at the center of the window
   float4 temp = READ_IMAGEF_SAFE(dynarg_srcTile0, kSamplerClampNearestUnormCoords, coord);
   float3 qcol = temp.xyz;
   const float srcAlpha = temp.w;

   // normal/depth value at the center of the window
   temp = READ_IMAGEF_SAFE(dynarg_srcTile1, kSamplerClampNearestUnormCoords, coord);
   float3 qnorm = temp.xyz;
   float qdpt   = temp.w;

   float4 out = 0.0f;
   float sum  = 0.0f;
   float vsum = 0.0f;

   const float colSigma   = sigma.x;
   const float normSigma  = sigma.y;
   const float depthSigma = sigma.z;

   for (int i = -2; i <= 2; ++i)
      for (int j = -2; j <= 2; ++j)
      {
         int2 offsetUV;
         offsetUV.x = coord.x + i * coordOffset;
         offsetUV.y = coord.y + j * coordOffset;

         REFLECT(offsetUV.x, imageSize.x)
         REFLECT(offsetUV.y, imageSize.y)
         offsetUV.x = clamp(offsetUV.x, (int)0, (int)imageSize.x-1);
         offsetUV.y = clamp(offsetUV.y, (int)0, (int)imageSize.y-1);

         float coeff = kernl[i + 2 + (j + 2) * 5];

         float4 temp;
         float3 c;
         float multiplier;

         temp = READ_IMAGEF_SAFE(dynarg_srcTile0, kSamplerClampNearestUnormCoords, offsetUV);
         c = temp.xyz;

         multiplier = C(c, qcol, colSigma * sqrt(colVar) + 1.0e-5f);
         coeff *= multiplier > 0.0f ? multiplier : 0.0f;

         // Normal edge stopping
         temp = READ_IMAGEF_SAFE(dynarg_srcTile1, kSamplerClampNearestUnormCoords, offsetUV);
         multiplier = nW(temp.xyz, qnorm, normSigma);
         coeff *= multiplier > 0.0f ? multiplier : 0.0f;

         // Depth edge stopping
         multiplier = dW(temp.w, qdpt, depthSigma * fabs(dot(gradDpt, make_float2(i * coordOffset, j * coordOffset))) + 1.0e-3f);
         coeff *= multiplier > 0.0f ? multiplier : 0.0f;

         //temp = ReadPixelTyped(transBuff, cx, cy);
         //multiplier = C(temp.xyz / temp.w, qtrans, transSigma);
         //coeff *= multiplier > 0.0f ? multiplier : 0.0f;

         out.xyz += c * coeff;

#ifdef FIRST_PASS
         vsum += fast_length(c) * coeff * coeff;
#else
         vsum += READ_IMAGEF_SAFE(dynarg_srcTile2, kSamplerClampNearestUnormCoords, offsetUV).x * coeff * coeff;
#endif
         sum += coeff;
      }

   out.w   = srcAlpha;
   out.xyz = sum > make_float3(0.0f, 0.0f, 0.0f) ? out.xyz / sum : make_float3(0.0f, 0.0f, 0.0f);

#if !defined(LAST_PASS)
   vsum /= sum * sum;

   //Back prop variance
   WRITE_IMAGEF_SAFE(dynarg_dstTile1, coord, make_float4(vsum,vsum,vsum,vsum));
#endif

   WRITE_IMAGEF_SAFE(dynarg_dstTile, coord, out);
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\OpenCL\kernels\atrousFilter.cl---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\OpenCL\kernels\compositeLightmaps.cl---------------
.
.
#include "commonCL.h"
#include "colorSpace.h"
#include "rgbmEncoding.h"

__constant float4 kZero = (float4)(0.0f, 0.0f, 0.0f, 0.0f);
__constant float4 kOne = (float4)(1.0f, 1.0f, 1.0f, 1.0f);
__constant float4 kHalf = (float4)(0.5, 0.5f, 0.5f, 0.5f);

// This function works in both non-tiled baking mode or tiled baking mode
//
// In case of non-tiled baking contains the lightmap size
// In case of tiled baking contains a tile size, both TilingHelper and RRBakeLightmapTechnique use the same tile size
//
// In case of non-tiled baking tileOffset contains a threadId to lightmapSpace coords offset
// In case of tiled baking, tileOffset contains an offset from TilingHelper (compositing tiles) to RRBakeLightmapTechnique tiles (baked tiles)
//   Indeed, compositing may have move the tile from TilingHelper to stay inside the lightmap buffers,
//   resulting in an offset
static uint ConvertThreadCoordsToBakingTileCoords(int2 tileThreadId, int2 tileOffset, int lightmapOrBakingTileSize)
{
    // First apply offset
    int2 coordInBakingTile = clamp(tileThreadId, (int2)(0, 0), (int2)(lightmapOrBakingTileSize-1, lightmapOrBakingTileSize-1)) + tileOffset;
    // Then clamp inside the baking tile or lightmap
    coordInBakingTile = clamp(coordInBakingTile, (int2)(0, 0), (int2)(lightmapOrBakingTileSize-1, lightmapOrBakingTileSize-1));

    // Finally, return a 1D index in baking tile or lightmap
    return coordInBakingTile.y * lightmapOrBakingTileSize + coordInBakingTile.x;
}

// ----------------------------------------------------------------------------------------------------
__kernel void compositingBlit(
    __write_only image2d_t   dynarg_dstImage,
    __read_only image2d_t    dynarg_srcImage
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    // Image coordinates
    int2 coords = (int2)(get_global_id(0), get_global_id(1));

    float4 srcColor = READ_IMAGEF_SAFE(dynarg_srcImage, kSamplerClampNearestUnormCoords, coords);
    WRITE_IMAGEF_SAFE(dynarg_dstImage, coords, srcColor);
}

// ----------------------------------------------------------------------------------------------------
__kernel void compositingMarkupInvalidTexels(
    __write_only image2d_t      dynarg_dstTile,
    __read_only image2d_t       dynarg_srcTile,
    INPUT_BUFFER(2,  int,        indirectSampleCountBuffer),
    INPUT_BUFFER(3,  float,      outputValidityBuffer),
    INPUT_BUFFER(4,  unsigned char, occupancyBuffer),
    INPUT_VALUE( 5,  float,      backfaceTolerance),
    INPUT_VALUE( 6,  int2,    compositingTileToBakingTileOffset),
    INPUT_VALUE( 7,  int,     tileSize),
    INPUT_VALUE( 8,  int2,    compositingTileOffset),
    INPUT_VALUE( 9,  int,     lightmapSize),
    INPUT_VALUE( 10, int,     inputIsTiled)
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    // Coordinates in tile space
    int2 tileThreadId = (int2)(get_global_id(0), get_global_id(1));

    const uint index = ConvertThreadCoordsToBakingTileCoords(tileThreadId, compositingTileToBakingTileOffset, tileSize);

    const int sampleCount = INDEX_SAFE(indirectSampleCountBuffer, index);

    const float validityValue    = INDEX_SAFE(outputValidityBuffer, index);
    float4 value                 = READ_IMAGEF_SAFE(dynarg_srcTile, kSamplerClampNearestUnormCoords, tileThreadId);

    const bool backfaceInvalid = sampleCount <= 0 ? false : ((validityValue / sampleCount) > (1.f - backfaceTolerance));
    if (backfaceInvalid)
    {
        value.w = 0.0f;
    }
    else
    {
        const int occupiedSamplesWithinTexel = INDEX_SAFE(occupancyBuffer, index);
        value.w = ((occupiedSamplesWithinTexel > 0) ? 1.f : 0.f);
    }
    WRITE_IMAGEF_SAFE(dynarg_dstTile, tileThreadId, value);
}

// ----------------------------------------------------------------------------------------------------
__kernel void compositingDirect(
    __write_only image2d_t  dynarg_dstTile,
    INPUT_BUFFER(1, float4,  dynarg_directLighting),
    INPUT_BUFFER(2, int,     directSampleCountBuffer),
    INPUT_VALUE( 3, int2,    compositingTileToBakingTileOffset),
    INPUT_VALUE( 4, int,     tileSize),
    INPUT_VALUE( 5, int2,    compositingTileOffset),
    INPUT_VALUE( 6, int,     lightmapSize),
    INPUT_VALUE( 7, int,     inputIsTiled)
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    // Coordinates in tile space
    int2 tileThreadId = (int2)(get_global_id(0), get_global_id(1));

    const uint indexSPP = ConvertThreadCoordsToBakingTileCoords(tileThreadId, compositingTileToBakingTileOffset, tileSize);

    const int currentDirectSampleCount = INDEX_SAFE(directSampleCountBuffer, indexSPP);
    if (currentDirectSampleCount <= 0)
        return;

    uint indexLighting = 0;
    if(inputIsTiled)
        indexLighting = ConvertThreadCoordsToBakingTileCoords(tileThreadId, compositingTileToBakingTileOffset, tileSize);
    else
        indexLighting = ConvertThreadCoordsToBakingTileCoords(tileThreadId, compositingTileOffset, lightmapSize);

    const float3 lightingValue = INDEX_SAFE(dynarg_directLighting, indexLighting).xyz;

    float4 result;
    result.xyz = lightingValue / currentDirectSampleCount;
    result.w = 1.f;

    WRITE_IMAGEF_SAFE(dynarg_dstTile, tileThreadId, result);
}

// ----------------------------------------------------------------------------------------------------
__kernel void compositingShadowMask(
    __write_only image2d_t      dynarg_dstTile,
    INPUT_BUFFER(1, int,        directSampleCountBuffer),
    INPUT_BUFFER(2, float4,     dynarg_shadowmask),
    INPUT_VALUE( 3, int2,    compositingTileToBakingTileOffset),
    INPUT_VALUE( 4, int,     tileSize),
    INPUT_VALUE( 5, int2,    compositingTileOffset),
    INPUT_VALUE( 6, int,     lightmapSize),
    INPUT_VALUE( 7, int,    inputIsTiled)
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    // Coordinates in tile space
    int2 tileThreadId = (int2)(get_global_id(0), get_global_id(1));

    const uint indexSPP = ConvertThreadCoordsToBakingTileCoords(tileThreadId, compositingTileToBakingTileOffset, tileSize);

    const int currentDirectSampleCount = INDEX_SAFE(directSampleCountBuffer, indexSPP);
    if (currentDirectSampleCount <= 0)
        return;

    uint indexLighting = 0;
    if(inputIsTiled)
        indexLighting = ConvertThreadCoordsToBakingTileCoords(tileThreadId, compositingTileToBakingTileOffset, tileSize);
    else
        indexLighting = ConvertThreadCoordsToBakingTileCoords(tileThreadId, compositingTileOffset, lightmapSize);

    KERNEL_ASSERT(currentDirectSampleCount > 0);
    const float4 shadowMaskValue = INDEX_SAFE(dynarg_shadowmask, indexLighting);
    float4 result = shadowMaskValue / currentDirectSampleCount;
    result = saturate4(result);

    WRITE_IMAGEF_SAFE(dynarg_dstTile, tileThreadId, result);
}

// ----------------------------------------------------------------------------------------------------
__kernel void compositingIndirect(
    __write_only image2d_t  dynarg_dstTile,
    INPUT_BUFFER(1, float4, dynarg_indirectLighting),
    INPUT_BUFFER(2, int,    indirectSampleCountBuffer),
    INPUT_BUFFER(3, float4, dynarg_environmentLighting),
    INPUT_BUFFER(4, int,    environmentSampleCountBuffer),
    INPUT_VALUE( 5, float,  indirectIntensity),
    INPUT_VALUE( 6, int2,   compositingTileToBakingTileOffset),
    INPUT_VALUE( 7, int,    tileSize),
    INPUT_VALUE( 8, int2,   compositingTileOffset),
    INPUT_VALUE( 9, int,    lightmapSize),
    INPUT_VALUE( 10, int,   indirectLightingIsTiled),
    INPUT_VALUE( 11, int,   environmentLightingIsTiled)
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    // Coordinates in tile space
    int2 tileThreadId = (int2)(get_global_id(0), get_global_id(1));

    uint indexIndLighting;
    uint indexEnvLighting;

    const uint indexSPP = ConvertThreadCoordsToBakingTileCoords(tileThreadId, compositingTileToBakingTileOffset, tileSize);

    const int sampleCount = INDEX_SAFE(indirectSampleCountBuffer, indexSPP);
    if (sampleCount == 0)
        return;

    const int envSampleCount = INDEX_SAFE(environmentSampleCountBuffer, indexSPP);
    if (envSampleCount == 0)
        return;

    if(indirectLightingIsTiled)
        indexIndLighting = ConvertThreadCoordsToBakingTileCoords(tileThreadId, compositingTileToBakingTileOffset, tileSize);
    else
        indexIndLighting = ConvertThreadCoordsToBakingTileCoords(tileThreadId, compositingTileOffset, lightmapSize);

    if(environmentLightingIsTiled)
        indexEnvLighting = ConvertThreadCoordsToBakingTileCoords(tileThreadId, compositingTileToBakingTileOffset, tileSize);
    else
        indexEnvLighting = ConvertThreadCoordsToBakingTileCoords(tileThreadId, compositingTileOffset, lightmapSize);

    KERNEL_ASSERT(sampleCount > 0);
    KERNEL_ASSERT(envSampleCount > 0);
    float4 indirectLightValue = INDEX_SAFE(dynarg_indirectLighting, indexIndLighting);
    float4 environmentValue   = INDEX_SAFE(dynarg_environmentLighting, indexEnvLighting);

    float4 result = indirectIntensity * (indirectLightValue / sampleCount + environmentValue / envSampleCount);
    result.w = 1.0f;

    WRITE_IMAGEF_SAFE(dynarg_dstTile, tileThreadId, result);
}

// ----------------------------------------------------------------------------------------------------
__kernel void compositingAO(
    __write_only image2d_t      dynarg_dstTile,
    INPUT_BUFFER(1, float,      dynarg_ao),
    INPUT_BUFFER(2, int,        indirectSampleCountBuffer),
    INPUT_VALUE( 3, float,      aoExponent),
    INPUT_VALUE( 4, int2,    compositingTileToBakingTileOffset),
    INPUT_VALUE( 5, int,     tileSize),
    INPUT_VALUE( 6, int2,    compositingTileOffset),
    INPUT_VALUE( 7, int,     lightmapSize),
    INPUT_VALUE( 8, int,    inputIsTiled)
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    // Coordinates in tile space
    int2 tileThreadId = (int2)(get_global_id(0), get_global_id(1));

    const uint indexSPP = ConvertThreadCoordsToBakingTileCoords(tileThreadId, compositingTileToBakingTileOffset, tileSize);
    const int sampleCount = INDEX_SAFE(indirectSampleCountBuffer, indexSPP);

    if (sampleCount == 0)
        return;

    uint indexLighting = 0;
    if(inputIsTiled)
        indexLighting = ConvertThreadCoordsToBakingTileCoords(tileThreadId, compositingTileToBakingTileOffset, tileSize);
    else
        indexLighting = ConvertThreadCoordsToBakingTileCoords(tileThreadId, compositingTileOffset, lightmapSize);

    float aoValue = INDEX_SAFE(dynarg_ao, indexLighting);
    KERNEL_ASSERT(sampleCount > 0);
    aoValue = aoValue / (float)sampleCount;

    aoValue = pow(aoValue, aoExponent);

    float4 result = (float4)(aoValue, aoValue, aoValue, 1.0f);

    WRITE_IMAGEF_SAFE(dynarg_dstTile, tileThreadId, result);
}

// ----------------------------------------------------------------------------------------------------
__kernel void compositingAddLighting(
    __write_only image2d_t      dynarg_dstTile,
    __read_only image2d_t       dynarg_srcTile0,    // directLightingImage
    __read_only image2d_t       dynarg_srcTile1     // indirectLightingImage
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    // Coordinates in tile space
    int2 tileThreadId = (int2)(get_global_id(0), get_global_id(1));

    float4 directLightingValue = READ_IMAGEF_SAFE(dynarg_srcTile0, kSamplerClampNearestUnormCoords, tileThreadId);
    float4 indirectLightingValue = READ_IMAGEF_SAFE(dynarg_srcTile1, kSamplerClampNearestUnormCoords, tileThreadId);

    float4 result = directLightingValue + indirectLightingValue;
    result.w = saturate1(directLightingValue.w * indirectLightingValue.w);

    WRITE_IMAGEF_SAFE(dynarg_dstTile, tileThreadId, result);
}

// ----------------------------------------------------------------------------------------------------
__kernel void compositingAddLightingIndirectOnly(
    __write_only image2d_t      dynarg_dstTile,
    __read_only image2d_t       dynarg_srcTile0     // indirectLightingImage
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    // Coordinates in tile space
    int2 tileThreadId = (int2)(get_global_id(0), get_global_id(1));

    float4 result = READ_IMAGEF_SAFE(dynarg_srcTile0, kSamplerClampNearestUnormCoords, tileThreadId);
    result.w = saturate1(result.w);

    WRITE_IMAGEF_SAFE(dynarg_dstTile, tileThreadId, result);
}

// ----------------------------------------------------------------------------------------------------
__kernel void compositingDilate(
    __write_only image2d_t          dynarg_dstTile,
    __read_only image2d_t           dynarg_srcTile,
    INPUT_BUFFER(2, unsigned char,  occupancyBuffer),
    INPUT_VALUE( 3, int,            useOccupancy),
    INPUT_VALUE( 4, int2,           compositingTileToBakingTileOffset),
    INPUT_VALUE( 5, int,            tileSize),
    INPUT_VALUE( 6, int2,           compositingTileOffset),
    INPUT_VALUE( 7, int,            lightmapSize),
    INPUT_VALUE( 8, int,            inputIsTiled)
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    // Coordinates in tile space
    int2 tileThreadId = (int2)(get_global_id(0), get_global_id(1));

    float4 inputValue = READ_IMAGEF_SAFE(dynarg_srcTile, kSamplerClampNearestUnormCoords, tileThreadId);

    // The texel is valid -> just write it to the output
    if (inputValue.w > 0)
    {
        WRITE_IMAGEF_SAFE(dynarg_dstTile, tileThreadId, inputValue);
        return;
    }

    if (useOccupancy) // Internal dilation
    {
        uint index;
        if(inputIsTiled)
            index = ConvertThreadCoordsToBakingTileCoords(tileThreadId, compositingTileToBakingTileOffset, tileSize);
        else
            index = ConvertThreadCoordsToBakingTileCoords(tileThreadId, compositingTileOffset, lightmapSize);

        const int occupiedSamplesWithinTexel = INDEX_SAFE(occupancyBuffer, index);

        // A non-occupied texel, just copy when doing internal dilation.
        if (occupiedSamplesWithinTexel == 0)
        {
            WRITE_IMAGEF_SAFE(dynarg_dstTile, tileThreadId, inputValue);
            return;
        }
    }

    float4 dilated = kZero;
    float weightCount = 0.0f;

    // Note: not using READ_IMAGEF_SAFE below as those samples are expected to read just outside of the tile boundary, they will get safely clamped though.

    // Upper row
    float4 value0 = read_imagef(dynarg_srcTile, kSamplerClampNearestUnormCoords, tileThreadId + (int2)(-1, -1));
    float4 value1 = read_imagef(dynarg_srcTile, kSamplerClampNearestUnormCoords, tileThreadId + (int2)(0, -1));
    float4 value2 = read_imagef(dynarg_srcTile, kSamplerClampNearestUnormCoords, tileThreadId + (int2)(1, -1));

    // Side values
    float4 value3 = read_imagef(dynarg_srcTile, kSamplerClampNearestUnormCoords, tileThreadId + (int2)(-1, 0));
    float4 value4 = read_imagef(dynarg_srcTile, kSamplerClampNearestUnormCoords, tileThreadId + (int2)(1, 0));

    // Bottom row
    float4 value5 = read_imagef(dynarg_srcTile, kSamplerClampNearestUnormCoords, tileThreadId + (int2)(-1, 1));
    float4 value6 = read_imagef(dynarg_srcTile, kSamplerClampNearestUnormCoords, tileThreadId + (int2)(0, 1));
    float4 value7 = read_imagef(dynarg_srcTile, kSamplerClampNearestUnormCoords, tileThreadId + (int2)(1, 1));

    dilated = value0.w * value0;
    dilated += value1.w * value1;
    dilated += value2.w * value2;
    dilated += value3.w * value3;
    dilated += value4.w * value4;
    dilated += value5.w * value5;
    dilated += value6.w * value6;
    dilated += value7.w * value7;

    weightCount = value0.w;
    weightCount += value1.w;
    weightCount += value2.w;
    weightCount += value3.w;
    weightCount += value4.w;
    weightCount += value5.w;
    weightCount += value6.w;
    weightCount += value7.w;

    dilated *= 1.0f / max(1.0f, weightCount);

    dilated.w = saturate1(weightCount);

    WRITE_IMAGEF_SAFE(dynarg_dstTile, tileThreadId, dilated);
}

// ----------------------------------------------------------------------------------------------------
__kernel void compositingNormalizeDirectionality(
    __write_only image2d_t               dynarg_dstTile,
    INPUT_BUFFER(1, float4,              dynarg_directionalityBuffer),
    INPUT_BUFFER(2, PackedNormalOctQuad, interpNormalsWSBuffer),
    INPUT_VALUE( 3,  int2,    compositingTileToBakingTileOffset),
    INPUT_VALUE( 4,  int,     tileSize),
    INPUT_VALUE( 5,  int2,    compositingTileOffset),
    INPUT_VALUE( 6,  int,     lightmapSize),
    INPUT_VALUE( 7, int,      inputIsTiled),
    INPUT_VALUE( 8, int,      superSamplingMultiplier)
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    int2 tileThreadId = (int2)(get_global_id(0), get_global_id(1));

    int index = ConvertThreadCoordsToBakingTileCoords(tileThreadId, compositingTileToBakingTileOffset, tileSize);

    float4 dir = INDEX_SAFE(dynarg_directionalityBuffer, index);
    dir = dir / max(0.001f, dir.w);

    float3 normalWS = CalculateSuperSampledInterpolatedNormal(index, superSamplingMultiplier, interpNormalsWSBuffer KERNEL_VALIDATOR_BUFFERS);

    // Compute rebalancing coefficients
    dir.w = dot(normalWS.xyz, dir.xyz);

    dir = dir * kHalf + kHalf;

    WRITE_IMAGEF_SAFE(dynarg_dstTile, tileThreadId, dir);
}

// ----------------------------------------------------------------------------------------------------
__kernel void compositingDecodeNormalsWS(
    __write_only image2d_t               dynarg_dstTile,
    INPUT_BUFFER(1, PackedNormalOctQuad, interpNormalsWSBuffer),
    INPUT_BUFFER(2, int,                 chartIndexBuffer),
    INPUT_VALUE( 3, int2,           compositingTileToBakingTileOffset),
    INPUT_VALUE( 4, int,            tileSize),
    INPUT_VALUE( 5, int2,           compositingTileOffset),
    INPUT_VALUE( 6, int,            lightmapSize),
    INPUT_VALUE( 7, int,            inputIsTiled),
    INPUT_VALUE( 8, int,            superSamplingMultiplier)
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    int2 tileThreadId = (int2)(get_global_id(0), get_global_id(1));

    int index = ConvertThreadCoordsToBakingTileCoords(tileThreadId, compositingTileToBakingTileOffset, tileSize);

    int centerChartId = INDEX_SAFE(chartIndexBuffer, index);

    float4 dir;

    dir.xyz = CalculateSuperSampledInterpolatedNormal(index, superSamplingMultiplier, interpNormalsWSBuffer KERNEL_VALIDATOR_BUFFERS);
    dir.w   = (float)centerChartId;

    WRITE_IMAGEF_SAFE(dynarg_dstTile, tileThreadId, dir);
}

// ----------------------------------------------------------------------------------------------------
__kernel void compositingCombineDirectionality(
    __write_only image2d_t      dynarg_dstTile,
    __read_only image2d_t       dynarg_srcTile0, // directLightingImage
    __read_only image2d_t       dynarg_srcTile1, // indirectLightingImage
    __read_only image2d_t       dynarg_srcTile2, // directionalityFromDirectImage
    __read_only image2d_t       dynarg_srcTile3, // directionalityFromIndirectImage
    INPUT_VALUE(5, float,       indirectScale)
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    int2 tileThreadId = (int2)(get_global_id(0), get_global_id(1));

    float4 directLighting               = READ_IMAGEF_SAFE(dynarg_srcTile0, kSamplerClampNearestUnormCoords, tileThreadId);
    float4 indirectLighting             = READ_IMAGEF_SAFE(dynarg_srcTile1, kSamplerClampNearestUnormCoords, tileThreadId);
    float4 directionalityFromDirect     = READ_IMAGEF_SAFE(dynarg_srcTile2, kSamplerClampNearestUnormCoords, tileThreadId);
    float4 directionalityFromIndirect   = READ_IMAGEF_SAFE(dynarg_srcTile3, kSamplerClampNearestUnormCoords, tileThreadId);

    float directWeight      = Luminance(directLighting.xyz) * length(directionalityFromDirect.xyz);
    float indirectWeight    = Luminance(indirectLighting.xyz) * length(directionalityFromIndirect.xyz) * indirectScale;

    float normalizationWeight = directWeight + indirectWeight;

    directWeight = directWeight / max(0.0001f, normalizationWeight);

    float4 output = select(directionalityFromDirect, lerp4(directionalityFromIndirect, directionalityFromDirect, (float4)directWeight), (int4)(-(indirectLighting.w > 0.0f)));

    WRITE_IMAGEF_SAFE(dynarg_dstTile, tileThreadId, output);
}

// ----------------------------------------------------------------------------------------------------
__kernel void compositingCombineDirectionalityIndirectOnly(
    __write_only image2d_t      dynarg_dstTile,
    __read_only image2d_t       dynarg_srcTile0, // indirectLightingImage
    __read_only image2d_t       dynarg_srcTile1  // directionalityFromIndirectImage
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    int2 tileThreadId = (int2)(get_global_id(0), get_global_id(1));

    float4 indirectLighting             = READ_IMAGEF_SAFE(dynarg_srcTile0, kSamplerClampNearestUnormCoords, tileThreadId);
    float4 directionalityFromIndirect   = READ_IMAGEF_SAFE(dynarg_srcTile1, kSamplerClampNearestUnormCoords, tileThreadId);
    float4 output = select(kZero, directionalityFromIndirect, (int4)(-(indirectLighting.w > 0.0f)));

    WRITE_IMAGEF_SAFE(dynarg_dstTile, tileThreadId, output);
}

// ----------------------------------------------------------------------------------------------------
__kernel void compositingSplitRGBA(
    __write_only image2d_t      dynarg_dstTile0,    // outRGBImage
    __write_only image2d_t      dynarg_dstTile1,    // outAlphaImage
    __read_only image2d_t       dynarg_srcTile0,    // directionalLightmap
    __read_only image2d_t       dynarg_srcTile1     // lightmap
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    int2 tileCoordinates = (int2)(get_global_id(0), get_global_id(1));

    float4 directionalValue = READ_IMAGEF_SAFE(dynarg_srcTile0, kSamplerClampNearestUnormCoords, tileCoordinates);
    float4 lightmapValue    = READ_IMAGEF_SAFE(dynarg_srcTile1, kSamplerClampNearestUnormCoords, tileCoordinates);

    float4 rgbValue         = (float4)(directionalValue.xyz, lightmapValue.w);
    float4 alphaValue       = (float4)(directionalValue.www, lightmapValue.w);

    WRITE_IMAGEF_SAFE(dynarg_dstTile0, tileCoordinates, rgbValue);
    WRITE_IMAGEF_SAFE(dynarg_dstTile1, tileCoordinates, alphaValue);
}

// ----------------------------------------------------------------------------------------------------
__kernel void compositingMergeRGBA(
    __write_only image2d_t      dynarg_dstTile,
    __read_only image2d_t       dynarg_srcTile0,    // dirRGBDilatedImage
    __read_only image2d_t       dynarg_srcTile1,    // dirAlphaDilatedImage
    INPUT_VALUE(3, uint,        tileBorderWidth)
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    // Coordinates in tile space
    int2 tileCoords = (int2)(get_global_id(0), get_global_id(1));

    // Discard tile border(output is smaller than the input)
    int2 sampleCoords = (int2)(tileCoords.x + tileBorderWidth, tileCoords.y + tileBorderWidth);

    float4 dirRGBValue      = READ_IMAGEF_SAFE(dynarg_srcTile0, kSamplerClampNearestUnormCoords, sampleCoords);
    float4 dirAlphaValue    = READ_IMAGEF_SAFE(dynarg_srcTile1, kSamplerClampNearestUnormCoords, sampleCoords);

    float4 result = (float4)(dirRGBValue.xyz, dirAlphaValue.x);

    WRITE_IMAGEF_SAFE(dynarg_dstTile, tileCoords, result);
}

// ----------------------------------------------------------------------------------------------------
__kernel void compositingNormalizeWithSampleCount(
    OUTPUT_BUFFER(0, float4, outputDirectLightingBuffer),   // dest buffer
    INPUT_BUFFER( 1, float4, outputIndirectLightingBuffer), // source buffer (named like this because of kernel asserts to work, but can be used not only for indirect)
    INPUT_BUFFER( 2, int,    indirectSampleCountBuffer),    // buffer with sample count
    INPUT_VALUE(  3, int2,   imageSize)
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    uint index = get_global_id(0) + get_global_id(1) * imageSize.y;

    const int sampleCount = INDEX_SAFE(indirectSampleCountBuffer, index);
    const float norm = sampleCount > 0 ? 1.0 / (float)sampleCount : 1.0;

    float4 color = INDEX_SAFE(outputIndirectLightingBuffer,index);

    INDEX_SAFE(outputDirectLightingBuffer, index) = color * norm;
}

// ----------------------------------------------------------------------------------------------------
__kernel void compositingNormalizeWithSampleCountAO(
    OUTPUT_BUFFER(0, float,  outputDirectLightingBuffer),
    INPUT_BUFFER( 1, float,  outputAoBuffer),
    INPUT_BUFFER( 2, int,    indirectSampleCountBuffer),
    INPUT_VALUE(  3, int2,   imageSize)
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    uint index = get_global_id(0) + get_global_id(1) * imageSize.y;

    const int sampleCount = INDEX_SAFE(indirectSampleCountBuffer, index);
    const float norm = sampleCount > 0 ? 1.0 / (float)sampleCount : 1.0;

    float color = INDEX_SAFE(outputAoBuffer,index);

    INDEX_SAFE(outputDirectLightingBuffer, index) = color * norm;
}

// ----------------------------------------------------------------------------------------------------
__kernel void compositingMultiplyWithSampleCount(
    OUTPUT_BUFFER(0, float4, outputDirectLightingBuffer),
    INPUT_BUFFER( 1, float4, outputIndirectLightingBuffer),
    INPUT_BUFFER( 2, int,    indirectSampleCountBuffer),
    INPUT_VALUE(  3, int2,   imageSize)
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    uint index = get_global_id(0) + get_global_id(1) * imageSize.y;

    const int sampleCount = INDEX_SAFE(indirectSampleCountBuffer, index);
    float4 color = INDEX_SAFE(outputIndirectLightingBuffer,index);

    INDEX_SAFE(outputDirectLightingBuffer, index) = color * (float)sampleCount;
}

// ----------------------------------------------------------------------------------------------------
__kernel void compositingMultiplyWithSampleCountAO(
    OUTPUT_BUFFER(0, float,  outputIndirectLightingBuffer),
    INPUT_BUFFER( 1, float,  outputAoBuffer),
    INPUT_BUFFER( 2, int,    indirectSampleCountBuffer),
    INPUT_VALUE(  3, int2,   imageSize)
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    uint index = get_global_id(0) + get_global_id(1) * imageSize.y;

    const int sampleCount = INDEX_SAFE(indirectSampleCountBuffer, index);
    float color = INDEX_SAFE(outputAoBuffer,index);

    INDEX_SAFE(outputIndirectLightingBuffer, index) = color * (float)sampleCount;
}

// ----------------------------------------------------------------------------------------------------
__kernel void compositingCopyAlphaFromRGBA(
    OUTPUT_BUFFER(0, float,   outputAoBuffer),
    INPUT_BUFFER( 1, float4,  outputIndirectLightingBuffer),
    INPUT_VALUE(  2, int2,    imageSize)
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    uint index = get_global_id(0) + get_global_id(1) * imageSize.y;

    float4 color = INDEX_SAFE(outputIndirectLightingBuffer,index);

    INDEX_SAFE(outputAoBuffer, index) = color.w;
}

// ----------------------------------------------------------------------------------------------------
__kernel void compositingCopyAlphaToRGBA(
    OUTPUT_BUFFER(0, float4,  outputIndirectLightingBuffer),
    INPUT_BUFFER( 1, float,  outputAoBuffer),
    INPUT_VALUE(  2, int2,    imageSize)
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    uint index = get_global_id(0) + get_global_id(1) * imageSize.y;

    float color = INDEX_SAFE(outputAoBuffer,index);

    INDEX_SAFE(outputIndirectLightingBuffer, index).w = color;
}

// ----------------------------------------------------------------------------------------------------
__kernel void compositingRGBMEncode(
    __write_only image2d_t      dynarg_dstTile,
    __read_only image2d_t       dynarg_srcTile,
    INPUT_VALUE(2, float,       rgbmRange),
    INPUT_VALUE(3, float,       lowerThreshold)
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    int2 tileThreadId = (int2)(get_global_id(0), get_global_id(1));

    float4 linearSpaceColor = READ_IMAGEF_SAFE(dynarg_srcTile, kSamplerClampNearestUnormCoords, tileThreadId);

    float4 rgbmValue = RGBMEncode(linearSpaceColor, rgbmRange, lowerThreshold);

    float4 gammaSpaceColor = LinearToGammaSpace01(rgbmValue);

    WRITE_IMAGEF_SAFE(dynarg_dstTile, tileThreadId, gammaSpaceColor);
}

// ----------------------------------------------------------------------------------------------------
__kernel void compositingDLDREncode(
    __write_only image2d_t  dynarg_dstTile,
    __read_only image2d_t   dynarg_srcTile
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    int2 tileThreadId = (int2)(get_global_id(0), get_global_id(1));

    float4 linearSpaceColor = READ_IMAGEF_SAFE(dynarg_srcTile, kSamplerClampNearestUnormCoords, tileThreadId);

    float4 gammaSpaceColor = (float4)(LinearToGammaSpace(linearSpaceColor.x), LinearToGammaSpace(linearSpaceColor.y), LinearToGammaSpace(linearSpaceColor.z), linearSpaceColor.w);

    gammaSpaceColor = min(gammaSpaceColor * 0.5f, kOne);

    WRITE_IMAGEF_SAFE(dynarg_dstTile, tileThreadId, gammaSpaceColor);
}

// ----------------------------------------------------------------------------------------------------
__kernel void compositingLinearToGamma(
    __write_only image2d_t      dynarg_dstTile,
    __read_only image2d_t       dynarg_srcTile
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    int2 tileThreadId = (int2)(get_global_id(0), get_global_id(1));

    float4 linearSpaceColor = READ_IMAGEF_SAFE(dynarg_srcTile, kSamplerClampNearestUnormCoords, tileThreadId);

    float4 gammaSpaceColor = (float4)(LinearToGammaSpace(linearSpaceColor.x), LinearToGammaSpace(linearSpaceColor.y), LinearToGammaSpace(linearSpaceColor.z), linearSpaceColor.w);

    WRITE_IMAGEF_SAFE(dynarg_dstTile, tileThreadId, gammaSpaceColor);
}

// ----------------------------------------------------------------------------------------------------
__kernel void compositingClampValues(
    __write_only image2d_t      dynarg_dstTile,
    __read_only image2d_t       dynarg_srcTile,
    INPUT_VALUE(2, float,       min),
    INPUT_VALUE(3, float,       max)
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    const float4 vMin = (float4)(min, min, min, min);
    const float4 vMax = (float4)(max, max, max, max);

    int2 tileThreadId = (int2)(get_global_id(0), get_global_id(1));

    float4 value = READ_IMAGEF_SAFE(dynarg_srcTile, kSamplerClampNearestUnormCoords, tileThreadId);

    value = clamp(value, vMin, vMax);

    WRITE_IMAGEF_SAFE(dynarg_dstTile, tileThreadId, value);
}

// ----------------------------------------------------------------------------------------------------
__kernel void compositingMultiplyImages(
    __write_only image2d_t      dynarg_dstTile,
    __read_only image2d_t       dynarg_srcTile0,
    __read_only image2d_t       dynarg_srcTile1
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    int2 tileThreadId = (int2)(get_global_id(0), get_global_id(1));

    float4 value1 = READ_IMAGEF_SAFE(dynarg_srcTile0, kSamplerClampNearestUnormCoords, tileThreadId);
    float4 value2 = READ_IMAGEF_SAFE(dynarg_srcTile1, kSamplerClampNearestUnormCoords, tileThreadId);

    float4 result = value1 * value2;

    WRITE_IMAGEF_SAFE(dynarg_dstTile, tileThreadId, result);
}

// ----------------------------------------------------------------------------------------------------
__kernel void compositingBlitTile(
    __write_only image2d_t      dynarg_dstTile,
    __read_only image2d_t       dynarg_srcTile,
    INPUT_VALUE(2, int2,        tileCoordinates)
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    int2 tileThreadId = (int2)(get_global_id(0), get_global_id(1));

    float4 value = READ_IMAGEF_SAFE(dynarg_srcTile, kSamplerClampNearestUnormCoords, tileThreadId);

    int2 lightmapCoords = tileThreadId + tileCoordinates;

    // write_imagef does appropriate data format conversion to the target image format
    WRITE_IMAGEF_SAFE(dynarg_dstTile, lightmapCoords, value);
}

// ----------------------------------------------------------------------------------------------------
// Filters horizontally or vertically depending on filterDirection - (1, 0) or (0, 1)
__kernel void compositingGaussFilter(
    __write_only image2d_t      dynarg_dstTile,
    __read_only image2d_t       dynarg_srcTile,
    INPUT_BUFFER(2, float,      dynarg_filterWeights),
    INPUT_BUFFER(3, int,        chartIndexBuffer),
    INPUT_VALUE( 4, int,        kernelWidth),
    INPUT_VALUE( 5, int2,       filterDirection),
    INPUT_VALUE( 6, int2,       halfKernelWidth),
    INPUT_VALUE( 7,  int2,    compositingTileToBakingTileOffset),
    INPUT_VALUE( 8,  int,     tileSize),
    INPUT_VALUE( 9,  int2,    compositingTileOffset),
    INPUT_VALUE( 10, int,     lightmapSize),
    INPUT_VALUE( 11, int,     inputIsTiled)
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    // Coordinates in tile space
    int2 tileThreadId = (int2)(get_global_id(0), get_global_id(1));

    int index = ConvertThreadCoordsToBakingTileCoords(tileThreadId, compositingTileToBakingTileOffset, tileSize);
    int centerChartId = INDEX_SAFE(chartIndexBuffer, index);

    float4 centerValue = READ_IMAGEF_SAFE(dynarg_srcTile, kSamplerClampNearestUnormCoords, tileThreadId);

    if (centerChartId == -1 || centerValue.w == 0.0f)
        return;

    float4 filtered     = kZero;
    float  weightSum    = 0.0f;
    float  weightCount  = 0.0f;

    int2 startOffset = tileThreadId - halfKernelWidth * filterDirection;
    for (int s = 0; s < kernelWidth; s++)
    {
        int2    sampleCoords    = startOffset + s * filterDirection;

        // Note: not using READ_IMAGEF_SAFE below as those samples are expected to read just outside of the tile boundary, they will get safely clamped though.
        // The srcTile and dstTile are of the same size, so iterating over dstTile texels means trying to sample by halfKernelWidth outside of srcTile at the edges.
        // We are using a separable Gaussian blur, first the vertical one and then the horizontal. The second pass depends on being able to read the results
        // stored in the border area from the first pass. Since we simply swap srcTile and dstTile, it's the easiest to keep them of the same size instead of doing
        // the tileSize vs expanded tileSize logic.

        float4  sampleValue     = read_imagef(dynarg_srcTile, kSamplerClampNearestUnormCoords, sampleCoords);
        index = ConvertThreadCoordsToBakingTileCoords(sampleCoords, compositingTileToBakingTileOffset, tileSize);
        int     sampleChartId   = INDEX_SAFE(chartIndexBuffer, index);

        float weight = sampleValue.w * INDEX_SAFE(dynarg_filterWeights, s);

        weight *= sampleChartId == centerChartId ? 1.0f : 0.0f;

        weightSum   += weight;
        weightCount += sampleValue.w;
        filtered    += weight * sampleValue;
    }

    filtered *= 1.0f / lerp1(1.0f, weightSum, clamp(weightCount, 0.0f, 1.0f));
    filtered.w = 1.0f;

    WRITE_IMAGEF_SAFE(dynarg_dstTile, tileThreadId, filtered);
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\OpenCL\kernels\compositeLightmaps.cl---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\OpenCL\kernels\convergence.cl---------------
.
.
#include "commonCL.h"

__constant ConvergenceOutputData g_clearedConvergenceOutputData = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, INT_MAX, INT_MAX, INT_MAX, INT_MIN, INT_MIN, INT_MIN};

__kernel void clearConvergenceData(
    OUTPUT_BUFFER(00, ConvergenceOutputData, convergenceOutputDataBuffer)
    KERNEL_VALIDATOR_BUFFERS_DEF
)
{
    INDEX_SAFE(convergenceOutputDataBuffer, 0) = g_clearedConvergenceOutputData;
}

__kernel void calculateConvergenceMap(
    INPUT_BUFFER( 00, unsigned char,         cullingMapBuffer),
    INPUT_BUFFER( 01, int,                   directSampleCountBuffer),
    INPUT_BUFFER( 02, int,                   indirectSampleCountBuffer),
    INPUT_BUFFER( 03, int,                   environmentSampleCountBuffer),
    INPUT_VALUE(  04, int,                   maxDirectSamplesPerPixel),
    INPUT_VALUE(  05, int,                   maxGISamplesPerPixel),
    INPUT_VALUE(  06, int,                   maxEnvSamplesPerPixel),
    INPUT_BUFFER( 07, unsigned char,         occupancyBuffer),
    INPUT_VALUE(  08, int,                   occupiedTexelCount),
    OUTPUT_BUFFER(09, ConvergenceOutputData, convergenceOutputDataBuffer) //Should be cleared properly before kernel is running
    KERNEL_VALIDATOR_BUFFERS_DEF
)
{
    __local ConvergenceOutputData dataShared;
    __local unsigned int totalDirectSamples;
    __local unsigned int totalGISamples;
    __local unsigned int totalEnvSamples;

    int idx = get_global_id(0);

    if (get_local_id(0) == 0)
    {
        dataShared = g_clearedConvergenceOutputData;
        totalDirectSamples = 0;
        totalGISamples = 0;
        totalEnvSamples = 0;
    }

    barrier(CLK_LOCAL_MEM_FENCE);

    const int occupiedSamplesWithinTexel = INDEX_SAFE(occupancyBuffer, idx);

    if (occupiedSamplesWithinTexel != 0)
    {
        const bool isTexelVisible = !IsCulled(INDEX_SAFE(cullingMapBuffer, idx));
        if (isTexelVisible)
            atomic_inc(&(dataShared.visibleTexelCount));

        int directSampleCount = 0;
        if(maxDirectSamplesPerPixel > 0)
        {
            directSampleCount = INDEX_SAFE(directSampleCountBuffer, idx);
            atomic_add(&totalDirectSamples, directSampleCount);
        }
        atomic_min(&(dataShared.minDirectSamples), directSampleCount);
        atomic_max(&(dataShared.maxDirectSamples), directSampleCount);

        const int giSampleCount = INDEX_SAFE(indirectSampleCountBuffer, idx);
        atomic_min(&(dataShared.minGISamples), giSampleCount);
        atomic_max(&(dataShared.maxGISamples), giSampleCount);
        atomic_add(&totalGISamples, giSampleCount);

        const int envSampleCount = INDEX_SAFE(environmentSampleCountBuffer, idx);
        atomic_min(&(dataShared.minEnvSamples), envSampleCount);
        atomic_max(&(dataShared.maxEnvSamples), envSampleCount);
        atomic_add(&totalEnvSamples, envSampleCount);

        if (IsGIConverged(giSampleCount, maxGISamplesPerPixel))
        {
            atomic_inc(&(dataShared.convergedGITexelCount));

            if (isTexelVisible)
                atomic_inc(&(dataShared.visibleConvergedGITexelCount));
        }

        if (IsDirectConverged(directSampleCount, maxDirectSamplesPerPixel))
        {
            atomic_inc(&(dataShared.convergedDirectTexelCount));

            if (isTexelVisible)
                atomic_inc(&(dataShared.visibleConvergedDirectTexelCount));
        }

        if (IsEnvironmentConverged(envSampleCount, maxEnvSamplesPerPixel))
        {
            atomic_inc(&(dataShared.convergedEnvTexelCount));

            if (isTexelVisible)
                atomic_inc(&(dataShared.visibleConvergedEnvTexelCount));
        }

    }

    barrier(CLK_LOCAL_MEM_FENCE);
    if (get_local_id(0) == 0)
    {
        const float maxTotalDirectSamples = (float)occupiedTexelCount * (float)maxDirectSamplesPerPixel;
        const float maxTotalGISamples     = (float)occupiedTexelCount * (float)maxGISamplesPerPixel;
        const float maxTotalEnvSamples    = (float)occupiedTexelCount * (float)maxEnvSamplesPerPixel;
        const unsigned int averageDirectSamplesRatio = (int)ceil(CONVERGENCE_AVERAGES_PRECISION * (float)totalDirectSamples / maxTotalDirectSamples);
        const unsigned int averageGISamplesRatio     = (int)ceil(CONVERGENCE_AVERAGES_PRECISION * (float)totalGISamples     / maxTotalGISamples);
        const unsigned int averageEnvSamplesRatio    = (int)ceil(CONVERGENCE_AVERAGES_PRECISION * (float)totalEnvSamples    / maxTotalEnvSamples);

        atomic_add(&(INDEX_SAFE(convergenceOutputDataBuffer, 0).visibleTexelCount), dataShared.visibleTexelCount);
        atomic_min(&(INDEX_SAFE(convergenceOutputDataBuffer, 0).minDirectSamples), dataShared.minDirectSamples);
        atomic_max(&(INDEX_SAFE(convergenceOutputDataBuffer, 0).maxDirectSamples), dataShared.maxDirectSamples);
        atomic_add(&(INDEX_SAFE(convergenceOutputDataBuffer, 0).averageDirectSamplesPercent), averageDirectSamplesRatio);
        atomic_min(&(INDEX_SAFE(convergenceOutputDataBuffer, 0).minGISamples), dataShared.minGISamples);
        atomic_max(&(INDEX_SAFE(convergenceOutputDataBuffer, 0).maxGISamples), dataShared.maxGISamples);
        atomic_add(&(INDEX_SAFE(convergenceOutputDataBuffer, 0).averageGISamplesPercent), averageGISamplesRatio);
        atomic_min(&(INDEX_SAFE(convergenceOutputDataBuffer, 0).minEnvSamples), dataShared.minEnvSamples);
        atomic_max(&(INDEX_SAFE(convergenceOutputDataBuffer, 0).maxEnvSamples), dataShared.maxEnvSamples);
        atomic_add(&(INDEX_SAFE(convergenceOutputDataBuffer, 0).averageEnvSamplesPercent), averageEnvSamplesRatio);
        atomic_add(&(INDEX_SAFE(convergenceOutputDataBuffer, 0).convergedGITexelCount), dataShared.convergedGITexelCount);
        atomic_add(&(INDEX_SAFE(convergenceOutputDataBuffer, 0).visibleConvergedGITexelCount), dataShared.visibleConvergedGITexelCount);
        atomic_add(&(INDEX_SAFE(convergenceOutputDataBuffer, 0).convergedDirectTexelCount), dataShared.convergedDirectTexelCount);
        atomic_add(&(INDEX_SAFE(convergenceOutputDataBuffer, 0).visibleConvergedDirectTexelCount), dataShared.visibleConvergedDirectTexelCount);
        atomic_add(&(INDEX_SAFE(convergenceOutputDataBuffer, 0).convergedEnvTexelCount), dataShared.convergedEnvTexelCount);
        atomic_add(&(INDEX_SAFE(convergenceOutputDataBuffer, 0).visibleConvergedEnvTexelCount), dataShared.visibleConvergedEnvTexelCount);
    }
}

__kernel void countUnconverged(
    INPUT_BUFFER(00, int, directSampleCountBuffer),
    INPUT_BUFFER(01, int, indirectSampleCountBuffer),
    INPUT_BUFFER(02, int, environmentSampleCountBuffer),
    INPUT_VALUE( 03, int, targetDirectSamplesPerProbe),
    INPUT_VALUE( 04, int, targetGISamplesPerProbe),
    INPUT_VALUE( 05, int, targetEnvSamplesPerProbe),
    INPUT_VALUE( 06, int, probeCount),
    OUTPUT_BUFFER(07, int, unconvergedCountBuffer)
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    int idx = get_global_id(0);
    if (idx >= probeCount)
        return;

    __local int threadGroupProbeCount;
    if (get_local_id(0) == 0)
    {
        threadGroupProbeCount = 0;
    }

    barrier(CLK_LOCAL_MEM_FENCE);

    int directSampleCount = INDEX_SAFE(directSampleCountBuffer, idx);
    int giSampleCount = INDEX_SAFE(indirectSampleCountBuffer, idx);
    int envSampleCount = INDEX_SAFE(environmentSampleCountBuffer, idx);

    if (directSampleCount < targetDirectSamplesPerProbe
        || giSampleCount < targetGISamplesPerProbe
        || envSampleCount < targetEnvSamplesPerProbe)
    {
        atomic_inc(&threadGroupProbeCount);
    }

    barrier(CLK_LOCAL_MEM_FENCE);

    if (get_local_id(0) == 0)
    {
        atomic_add(&INDEX_SAFE(unconvergedCountBuffer, 0), threadGroupProbeCount);
    }
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\OpenCL\kernels\convergence.cl---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\OpenCL\kernels\culling.cl---------------
.
.
#include "commonCL.h"

__kernel void clearLightmapCulling(
    OUTPUT_BUFFER(00, unsigned char, cullingMapBuffer)
    KERNEL_VALIDATOR_BUFFERS_DEF
)
{
    int idx = get_global_id(0);
    INDEX_SAFE(cullingMapBuffer, idx) = 255;
}

__kernel void prepareLightmapCulling(
    INPUT_BUFFER( 00, unsigned char,       occupancyBuffer),
    INPUT_BUFFER( 01, float4,              positionsWSBuffer),
    INPUT_BUFFER( 02, PackedNormalOctQuad, interpNormalsWSBuffer),
    INPUT_VALUE(  03, Matrix4x4,           worldToClip),
    INPUT_VALUE(  04, float4,              cameraPosition),
    INPUT_VALUE(  05, int,                 superSamplingMultiplier),
    OUTPUT_BUFFER(06, ray,                 lightRaysCompactedBuffer),
    OUTPUT_BUFFER(07, uint,                lightRaysCountBuffer),
    INPUT_BUFFER( 08, uint,                instanceIdToLodInfoBuffer)
    KERNEL_VALIDATOR_BUFFERS_DEF
)
{
    ray r;  // prepare ray in private memory
    int idx = get_global_id(0);

    __local int numRayPreparedSharedMem;
    if (get_local_id(0) == 0)
        numRayPreparedSharedMem = 0;
    barrier(CLK_LOCAL_MEM_FENCE);
    atomic_inc(&numRayPreparedSharedMem);

    //TODO(RadeonRays) on spot compaction (guillaume v1 style)

    const int occupiedSamplesWithinTexel = INDEX_SAFE(occupancyBuffer, idx);
    if (occupiedSamplesWithinTexel == 0) // Reject texels that are invalid.
    {
        Ray_SetInactive(&r);
    }
    else
    {
        // Just fetch one sample, we know the position is within an occupied texel.
        int ssIdx = idx * superSamplingMultiplier * superSamplingMultiplier;
        float4 position = INDEX_SAFE(positionsWSBuffer, ssIdx);

        //Clip space position
        float4 clipPos = transform_point(position.xyz, worldToClip);
        clipPos.xyz /= clipPos.w;

        //Camera to texel
        //float3 camToPos = (position.xyz - cameraPosition.xyz);

        //Normal
        float3 normal = CalculateSuperSampledInterpolatedNormal(idx, superSamplingMultiplier, interpNormalsWSBuffer KERNEL_VALIDATOR_BUFFERS);
        //float normalDotCamToPos = dot(normal, camToPos);

        //Is the texel visible?
        if (clipPos.x >= -1.0f && clipPos.x <= 1.0f &&
            clipPos.y >= -1.0f && clipPos.y <= 1.0f &&
            clipPos.z >= 0.0f && clipPos.z <= 1.0f)
            //TODO(RadeonRays) understand why this does not work.
            //&& normalDotCamToPos < 0.0f)
        {
            const float kMinPushOffDistance = 0.001f;
            float3 targetPos = position.xyz + normal * kMinPushOffDistance;
            float3 camToTarget = (targetPos - cameraPosition.xyz);
            float camToTargetDist = length(camToTarget);
            if (camToTargetDist > 0)
            {
                const int instanceId = (int)(floor(position.w));
                const int instanceLodInfo = INDEX_SAFE(instanceIdToLodInfoBuffer, instanceId);
                Ray_Init(&r, cameraPosition.xyz, camToTarget/ camToTargetDist, camToTargetDist, 0.f, instanceLodInfo);
            }
            else
            {
                Ray_SetInactive(&r);
            }
        }
        else
        {
            Ray_SetInactive(&r);
        }
    }

    barrier(CLK_LOCAL_MEM_FENCE);
    if (get_local_id(0) == 0)
    {
        atomic_add(GET_PTR_SAFE(lightRaysCountBuffer, 0), numRayPreparedSharedMem);
    }
    INDEX_SAFE(lightRaysCompactedBuffer, idx) = r;
}

__kernel void processLightmapCulling(
    INPUT_BUFFER( 00, ray,           lightRaysCompactedBuffer),
    INPUT_BUFFER( 01, float4,        lightOcclusionCompactedBuffer),
    OUTPUT_BUFFER(02, unsigned char, cullingMapBuffer),
    OUTPUT_BUFFER(03, unsigned int,  visibleTexelCountBuffer) //Need to have been cleared to 0 before the kernel is called.
    KERNEL_VALIDATOR_BUFFERS_DEF
)
{
    __local int visibleTexelCountSharedMem;
    int idx = get_global_id(0);
    if (get_local_id(0) == 0)
        visibleTexelCountSharedMem = 0;
    barrier(CLK_LOCAL_MEM_FENCE);

    const bool rayActive = !Ray_IsInactive(GET_PTR_SAFE(lightRaysCompactedBuffer, idx));//TODO(RadeonRays) on spot compaction (guillaume v1 style) see same comment above
    const bool hit = rayActive && INDEX_SAFE(lightOcclusionCompactedBuffer, idx).w < TRANSMISSION_THRESHOLD;
    const bool texelVisible = rayActive && !hit;

    if (texelVisible)
    {
        INDEX_SAFE(cullingMapBuffer, idx) = 255;
    }
    else
    {
        INDEX_SAFE(cullingMapBuffer, idx) = 0;
    }

    // nvidia+macOS hack (atomic operation in the if above break the write to cullingMapBuffer!).
    int intTexelVisible = texelVisible?1:0;
    atomic_add(&visibleTexelCountSharedMem,intTexelVisible);

    barrier(CLK_LOCAL_MEM_FENCE);
    if (get_local_id(0) == 0)
        atomic_add(GET_PTR_SAFE(visibleTexelCountBuffer, 0), visibleTexelCountSharedMem);
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\OpenCL\kernels\culling.cl---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\OpenCL\kernels\customBake.cl---------------
.
.
#include "commonCL.h"

__kernel void prepareCustomBake(
    OUTPUT_BUFFER(00, ray, pathRaysCompactedBuffer_0),
    OUTPUT_BUFFER(01, uint, activePathCountBuffer_0),
    OUTPUT_BUFFER(02, uint, totalRaysCastCountBuffer),
    OUTPUT_BUFFER(03, float4, originalRaysExpandedBuffer),
    INPUT_BUFFER( 04, float4, positionsWSBuffer),
    INPUT_BUFFER( 05, uint, sobol_buffer),
    INPUT_BUFFER( 06, float, goldenSample_buffer),
    INPUT_VALUE(  07, int, numGoldenSample),
    INPUT_VALUE(  08, int, fakedLightmapResolution), // position count
    INPUT_BUFFER( 09, SampleDescription, sampleDescriptionsExpandedBuffer),
    INPUT_BUFFER( 10, uint, sampleDescriptionsExpandedCountBuffer)
    KERNEL_VALIDATOR_BUFFERS_DEF
)
{
    __local uint numRayPreparedSharedMem;
    if (get_local_id(0) == 0)
        numRayPreparedSharedMem = 0;
    barrier(CLK_LOCAL_MEM_FENCE);

    // Prepare ray in private memory
    ray r;
    Ray_SetInactive(&r);

    int expandedPathRayIdx = get_global_id(0), local_idx;
    const uint sampleDescriptionsExpandedCount = INDEX_SAFE(sampleDescriptionsExpandedCountBuffer, 0);
    if (expandedPathRayIdx < sampleDescriptionsExpandedCount)
    {
        const SampleDescription sampleDescription = INDEX_SAFE(sampleDescriptionsExpandedBuffer, expandedPathRayIdx);
#if DISALLOW_RAY_EXPANSION
        if (sampleDescription.texelIndex >= 0)
        {
#endif
            const int ssIdx = sampleDescription.texelIndex;
            const float4 position = INDEX_SAFE(positionsWSBuffer, ssIdx);
            const float rayOffset = position.w;

            // Skip unused texels.
            if (rayOffset >= 0.0)
            {
                AssertPositionIsOccupied(position KERNEL_VALIDATOR_BUFFERS);

                // Get random numbers
                float3 sample3D;
                sample3D.x = SobolSample(sampleDescription.currentSampleCount, 0, sobol_buffer KERNEL_VALIDATOR_BUFFERS);
                sample3D.y = SobolSample(sampleDescription.currentSampleCount, 1, sobol_buffer KERNEL_VALIDATOR_BUFFERS);
                sample3D.z = SobolSample(sampleDescription.currentSampleCount, 2, sobol_buffer KERNEL_VALIDATOR_BUFFERS);

                int texel_x = sampleDescription.texelIndex % fakedLightmapResolution;
                int texel_y = sampleDescription.texelIndex / fakedLightmapResolution;
                sample3D = ApplyCranleyPattersonRotation3D(sample3D, texel_x, texel_y, fakedLightmapResolution, 0, goldenSample_buffer KERNEL_VALIDATOR_BUFFERS);

                // We don't want the full sphere, we only want the upper hemisphere.
                float3 direction = SphereSample(sample3D.xy);
                if (direction.y < 0.0f)
                    direction = make_float3(direction.x, -direction.y, direction.z);

                const float randOffset = 0.1f * rayOffset + 0.9f * rayOffset * sample3D.z;
                const float3 origin = position.xyz + direction * randOffset;
                const float kMaxt = 1000000.0f;
                const int instanceLodInfo = PackLODInfo(NO_LOD_MASK, NO_LOD_GROUP);
                Ray_Init(&r, origin, direction, kMaxt, 0.f, instanceLodInfo);

                // Set the index so we can map to the originating texel/probe
                Ray_SetSourceIndex(&r, sampleDescription.texelIndex);
            }
#if DISALLOW_RAY_EXPANSION
        }
#endif
    }

    // Threads synchronization for compaction
    if (Ray_IsActive_Private(&r))
    {
        local_idx = atomic_inc(&numRayPreparedSharedMem);
    }
    barrier(CLK_LOCAL_MEM_FENCE);
    if (get_local_id(0) == 0)
    {
        atomic_add(GET_PTR_SAFE(totalRaysCastCountBuffer, 0), numRayPreparedSharedMem);
        int numRayToAdd = numRayPreparedSharedMem;
#if DISALLOW_PATH_RAYS_COMPACTION
        numRayToAdd = get_local_size(0);
#endif
        numRayPreparedSharedMem = atomic_add(GET_PTR_SAFE(activePathCountBuffer_0, 0), numRayToAdd);
    }
    barrier(CLK_LOCAL_MEM_FENCE);

    int compactedPathRayIndex = numRayPreparedSharedMem + local_idx;
#if DISALLOW_PATH_RAYS_COMPACTION
    compactedPathRayIndex = expandedPathRayIdx;
#else
    // Write the ray out to memory
    if (Ray_IsActive_Private(&r))
#endif
    {
        INDEX_SAFE(originalRaysExpandedBuffer, expandedPathRayIdx) = (float4)(r.d.x, r.d.y, r.d.z, 0);
        Ray_SetSampleDescriptionIndex(&r, expandedPathRayIdx);
        INDEX_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIndex) = r;
    }
}

__kernel void processCustomBake(
    //*** input ***
    INPUT_BUFFER( 00, ray,    pathRaysCompactedBuffer_0),
    INPUT_BUFFER( 01, uint,   activePathCountBuffer_0),
    INPUT_BUFFER( 02, float4, pathThroughputExpandedBuffer),
    INPUT_VALUE(  03, int,    totalSampleCount),
    INPUT_BUFFER( 04, float4, shadowmaskAoValidityExpandedBuffer), //Used to store validity in .y
    INPUT_BUFFER( 05, Intersection, pathIntersectionsCompactedBuffer),
    //*** output ***
    OUTPUT_BUFFER(06, float4, probeOcclusionExpandedBuffer)
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    const uint compactedRayIdx = get_global_id(0);
    if (compactedRayIdx >= INDEX_SAFE(activePathCountBuffer_0, 0))
        return;

#if DISALLOW_LIGHT_RAYS_COMPACTION
    if (Ray_IsInactive(GET_PTR_SAFE(pathRaysCompactedBuffer_0, compactedRayIdx)))
        return;
#endif

    KERNEL_ASSERT(Ray_IsActive(GET_PTR_SAFE(pathRaysCompactedBuffer_0, compactedRayIdx)));
    const int sampleDescriptionIdx = Ray_GetSampleDescriptionIndex(GET_PTR_SAFE(pathRaysCompactedBuffer_0, compactedRayIdx));

    const bool pathRayHitSomething = INDEX_SAFE(pathIntersectionsCompactedBuffer, compactedRayIdx).shapeid != MISS_MARKER;
    const float4 throughput = INDEX_SAFE(pathThroughputExpandedBuffer, sampleDescriptionIdx);
    float3 color = throughput.xyz;

    if (pathRayHitSomething)
        color = make_float3(0,0,0);

    // accumulate sky occlusion.
    const float backfacing = INDEX_SAFE(shadowmaskAoValidityExpandedBuffer, sampleDescriptionIdx).y;
    INDEX_SAFE(probeOcclusionExpandedBuffer, sampleDescriptionIdx) += make_float4(color.x, color.y, color.z, backfacing);
    KERNEL_ASSERT(totalSampleCount > 0);
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\OpenCL\kernels\customBake.cl---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\OpenCL\kernels\directLightingTests.cl---------------
.
.
#include "directLighting.h"

// From PBRTv3
float nextFloatUp(float v)
{
    if (isinf(v) && v > 0.0f)
        return v;
    if (v == -0.0f)
        v = 0.0f;

    uint ui = as_uint(v);
    if (v >= 0)
        ++ui;
    else
        --ui;
    return as_float(ui);
}

float nextFloatDown(float v)
{
    if (isinf(v) && v < 0.)
        return v;
    if (v == 0.f)
        v = -0.f;

    uint ui = as_uint(v);
    if (v > 0)
        --ui;
    else
        ++ui;
    return as_float(ui);
}

#define SEARCH_SQUARE_SIDE 10

void test(float3 surfacePosition, __global int* directionHasNaN, __global int* rayIsActive, int outputOffset)
{
    LightBuffer light;
    light.pos = (float4)(0.1f, 0.1f, 0.1f, 0.0f);
    light.col = (float4)(1.0f, 1.0f, 1.0f, 1.0f);
    light.dir = (float4)(0.0f, 0.0f, 1.0f, 1000.0f); // w is lightRange
    light.lightType = kPVRLightRectangle;
    light.directBakeMode = kDirectBakeMode_None;
    light.probeOcclusionLightIndex = 0;
    light.castShadow = 1;
    light.dataUnion.areaLightData.areaHeight = 0.5f;
    light.dataUnion.areaLightData.areaWidth = 0.5f;
    light.dataUnion.areaLightData.cookieIndex = -1;
    light.dataUnion.areaLightData.Normal = (float4)(0.0f, 0.0f, 1.0f, 0.0f);
    light.dataUnion.areaLightData.Tangent = (float4)(1.0f, 0.0f, 0.0, 0.0f);
    light.dataUnion.areaLightData.Bitangent = (float4)(0.0f, 1.0f, 0.0f, 0.0f);

    float2 sample2D = (float2)(0.5f, 0.3f);
    float3 surfaceNormal = normalize((float3)(0.1f, -0.5f, -0.3f));
    float pushOff = 0.0f;

    for (int i = 0; i < SEARCH_SQUARE_SIDE/2; ++i)
    {
        surfacePosition.x = nextFloatDown(surfacePosition.x);
        surfacePosition.y = nextFloatDown(surfacePosition.y);
    }
    float3 startSurfacePosition = surfacePosition;

    for (int i = 0; i < SEARCH_SQUARE_SIDE*SEARCH_SQUARE_SIDE; ++i)
    {
        ray outputRay;
        PrepareShadowRay(light, sample2D, surfacePosition, surfaceNormal, pushOff, false, &outputRay, 0);

        directionHasNaN[i+outputOffset] = (int)(any(isnan(outputRay.d)));
        rayIsActive[i+outputOffset] = (int)(!Ray_IsInactive_Private(&outputRay));

        surfacePosition.x = nextFloatUp(surfacePosition.x);

        // end of line, goto beginning of next line
        if (i % SEARCH_SQUARE_SIDE == 0)
        {
            surfacePosition.y = nextFloatUp(surfacePosition.y);
            surfacePosition.x = startSurfacePosition.x;
        }
    }
}


__kernel void testShadowRay_Case1358519(
    OUTPUT_BUFFER(0, int, directionHasNaN),
    OUTPUT_BUFFER(0, int, rayIsActive)
)
{
    // The outcome of floating point computations will vary from machine to machine (different hardware, compiler version...)
    // In order to make the test more reliable, we execute PrepareShadowRay for various surfacePositions located in a small neighbourhood around the problematic value
    float3 surfacePosition =(float3)(0.126095816,0.0242773965,0.100000001);// Value that was causing an issue on an RTX 3090
    test(surfacePosition, directionHasNaN, rayIsActive, 0);

    // surfacePosition that is not near the light surface and should always produce valid rays
    surfacePosition = (float3)(0.126095816, 0.0242773965, 0.5);
    test(surfacePosition, directionHasNaN, rayIsActive, SEARCH_SQUARE_SIDE*SEARCH_SQUARE_SIDE);
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\OpenCL\kernels\directLightingTests.cl---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\OpenCL\kernels\expansionAndGathering.cl---------------
.
.
#include "commonCL.h"

__kernel void prepareExpandedRayIndices(
    //output
    OUTPUT_BUFFER(00, SampleDescription, sampleDescriptionsExpandedBuffer),
    OUTPUT_BUFFER(01, uint,              sampleDescriptionsExpandedCountBuffer),
    OUTPUT_BUFFER(02, ExpandedTexelInfo, expandedTexelsBuffer),
    OUTPUT_BUFFER(03, uint,              expandedTexelsCountBuffer),
    //input and output
    OUTPUT_BUFFER(04, int,               directSampleCountBuffer),
    OUTPUT_BUFFER(05, int,               environmentSampleCountBuffer),
    OUTPUT_BUFFER(06, int,               indirectSampleCountBuffer),
    //input
    INPUT_VALUE(  07, int,               radeonRaysExpansionPass),
    INPUT_VALUE(  08, int,               numRaysToShootPerTexel),
    INPUT_VALUE(  09, int,               maxSampleCount),
    INPUT_VALUE(  10, int,               maxOutputRayCount)
#ifndef PROBES
    ,
    INPUT_BUFFER( 11, unsigned char,     cullingMapBuffer),
    INPUT_BUFFER( 12, unsigned char,     occupancyBuffer),
    INPUT_VALUE(  13, int,               shouldUseCullingMap),
    INPUT_VALUE(  14, int,               lightmapSize),
    INPUT_VALUE(  15, uint,              currentTileIdx),
    INPUT_VALUE(  16, uint,              sqrtNumTiles)
#endif
    KERNEL_VALIDATOR_BUFFERS_DEF
)
{
    // Initialize local memory
    __local int numExpandedTexelsForThreadGroup;
    __local int threadGroupExpandedTexelOffsetInGlobalMemory;
    __local int numRaysForThreadGroup;
    __local int threadGroupRaysOffsetInGlobalMemory;
    if (get_local_id(0) == 0)
    {
        numExpandedTexelsForThreadGroup = 0;
        threadGroupExpandedTexelOffsetInGlobalMemory = 0;
        numRaysForThreadGroup = 0;
        threadGroupRaysOffsetInGlobalMemory = 0;
    }
    barrier(CLK_LOCAL_MEM_FENCE);

    // When enqueuing this kernel, we launch as many threads as nbTexels.
    // Therefore this idx is in [0,tileWidth*tileHeight] if tiling is activated,
    // or [0,lightmapWidth*lightmapHeight] if tiling is deactivated
    const uint idx = get_global_id(0);

#ifndef PROBES
    uint globalIdx = GetGlobalIndex(idx, lightmapSize, currentTileIdx, sqrtNumTiles);
#else
    uint globalIdx = idx;
#endif

    int numRaysToPrepare = numRaysToShootPerTexel;
#if DISALLOW_RAY_EXPANSION
    numRaysToPrepare = 1;
#endif

    // STEP 1 : Determine if the texel is active (i.e. occupied && visible).
#ifndef PROBES
    const int occupiedSamplesWithinTexel = INDEX_SAFE(occupancyBuffer, idx);
    if (occupiedSamplesWithinTexel == 0)
        numRaysToPrepare = 0;
    if (shouldUseCullingMap && numRaysToPrepare && IsCulled(INDEX_SAFE(cullingMapBuffer, idx)))
        numRaysToPrepare = 0;
#endif

    // STEP 2 : Compute how many rays we want to shoot for the active texels.
    int currentSampleCount;
    if (numRaysToPrepare)
    {
        if (radeonRaysExpansionPass == kRRExpansionPass_direct)
        {
            currentSampleCount = INDEX_SAFE(directSampleCountBuffer, idx);
        }
        else if (radeonRaysExpansionPass == kRRExpansionPass_environment)
        {
            currentSampleCount = INDEX_SAFE(environmentSampleCountBuffer, idx);
        }
        else if (radeonRaysExpansionPass == kRRExpansionPass_indirect)
        {
            currentSampleCount = INDEX_SAFE(indirectSampleCountBuffer, idx);
        }

        KERNEL_ASSERT(maxSampleCount >= currentSampleCount);
        int samplesLeftBeforeConvergence = max(maxSampleCount - currentSampleCount, 0);
        numRaysToPrepare = min(samplesLeftBeforeConvergence, numRaysToPrepare);
    }

    // STEP 3 : Compute rays write offsets and init the rays indices.
    int rayOffsetInThreadGroup = 0;
    if (numRaysToPrepare)
        rayOffsetInThreadGroup = atomic_add(&numRaysForThreadGroup, numRaysToPrepare);
    barrier(CLK_LOCAL_MEM_FENCE);
    if (get_local_id(0) == 0)
    {
#if DISALLOW_RAY_EXPANSION
        numRaysForThreadGroup = get_local_size(0);
#endif
        //Note: SampleDescriptionsExpandedCountBuffer will be potentially bigger than the size of SampleDescriptionsExpandedBuffer. However this is fine
        //as we will only dispatch the following kernel with numthread = ray buffer size.
        threadGroupRaysOffsetInGlobalMemory = atomic_add(GET_PTR_SAFE(sampleDescriptionsExpandedCountBuffer, 0), numRaysForThreadGroup);
    }
    barrier(CLK_LOCAL_MEM_FENCE);

    // STEP 4 : Write the rays texel index out (avoiding writing more rays than the buffer can hold).
    int threadGlobalRayOffset = threadGroupRaysOffsetInGlobalMemory + rayOffsetInThreadGroup;
    int maxNumRaysThisThreadCanPrepare = max(maxOutputRayCount - threadGlobalRayOffset, 0);
    numRaysToPrepare = min(maxNumRaysThisThreadCanPrepare, numRaysToPrepare);
#if DISALLOW_RAY_EXPANSION
    SampleDescription sampleDescription;

    // -1 marks a texel we should not cast a ray from (invalid or culled)
    // texelIndex is always a global index in lightmap, not in the tile.
    sampleDescription.texelIndex = numRaysToPrepare ? globalIdx : -1;
    sampleDescription.currentSampleCount = currentSampleCount;
    INDEX_SAFE(sampleDescriptionsExpandedBuffer, idx) = sampleDescription;
#else
    for (int i = 0; i < numRaysToPrepare; ++i)
    {
        SampleDescription sampleDescription;
        sampleDescription.texelIndex = globalIdx;
        sampleDescription.currentSampleCount = currentSampleCount + i;
        INDEX_SAFE(sampleDescriptionsExpandedBuffer, threadGlobalRayOffset + i) = sampleDescription;
    }
#endif

    // STEP 5 : Register expanded texel info for the gather step.
    int expandedTexelOffsetInThreadGroup = 0;
    if (numRaysToPrepare)
        expandedTexelOffsetInThreadGroup = atomic_inc(&numExpandedTexelsForThreadGroup);
    barrier(CLK_LOCAL_MEM_FENCE);
    if (get_local_id(0) == 0)
    {
        KERNEL_ASSERT(numExpandedTexelsForThreadGroup <= get_local_size(0));
        KERNEL_ASSERT(numRaysForThreadGroup <= (numRaysToShootPerTexel * get_local_size(0)));
        KERNEL_ASSERT(numRaysForThreadGroup >= numExpandedTexelsForThreadGroup);
        KERNEL_ASSERT(numExpandedTexelsForThreadGroup <= get_local_size(0));
        threadGroupExpandedTexelOffsetInGlobalMemory = atomic_add(GET_PTR_SAFE(expandedTexelsCountBuffer, 0), numExpandedTexelsForThreadGroup);
    }
    barrier(CLK_LOCAL_MEM_FENCE);
    if (numRaysToPrepare)
    {
        ExpandedTexelInfo expandedTexelInfo;
#if DISALLOW_RAY_EXPANSION
        expandedTexelInfo.firstRaysOffset = idx;
#else
        expandedTexelInfo.firstRaysOffset = threadGlobalRayOffset;
#endif
        expandedTexelInfo.numRays = numRaysToPrepare;
        expandedTexelInfo.originalTexelIndex = idx;
        KERNEL_ASSERT(threadGroupExpandedTexelOffsetInGlobalMemory < get_global_size(0));
        KERNEL_ASSERT((threadGroupExpandedTexelOffsetInGlobalMemory + expandedTexelOffsetInThreadGroup)< get_global_size(0));
        KERNEL_ASSERT(expandedTexelOffsetInThreadGroup < get_local_size(0));
        INDEX_SAFE(expandedTexelsBuffer, threadGroupExpandedTexelOffsetInGlobalMemory + expandedTexelOffsetInThreadGroup) = expandedTexelInfo;

        //increment sample count
        if (radeonRaysExpansionPass == kRRExpansionPass_direct)
        {
            INDEX_SAFE(directSampleCountBuffer, idx) += numRaysToPrepare;
        }
        else if (radeonRaysExpansionPass == kRRExpansionPass_environment)
        {
            INDEX_SAFE(environmentSampleCountBuffer, idx) += numRaysToPrepare;
        }
        else if (radeonRaysExpansionPass == kRRExpansionPass_indirect)
        {
            INDEX_SAFE(indirectSampleCountBuffer, idx) += numRaysToPrepare;
        }
    }
}

__kernel void gatherProcessedExpandedRays(
    INPUT_BUFFER( 00, ExpandedTexelInfo, expandedTexelsBuffer),
    INPUT_BUFFER( 01, uint,              expandedTexelsCountBuffer),
    INPUT_VALUE(  02, int,               radeonRaysExpansionPass),
#ifdef PROBES
    INPUT_VALUE(  03, int,               numProbes),
    INPUT_VALUE(  04, int,               lightmappingSourceType),
    INPUT_BUFFER( 05, float4,            probeSHExpandedBuffer),
    INPUT_BUFFER( 06, float,             probeDepthOctahedronExpandedBuffer),
    INPUT_BUFFER( 07, float4,            probeOcclusionExpandedBuffer),
    INPUT_BUFFER( 08, float4,            shadowmaskAoValidityExpandedBuffer), //when gathering indirect .x will contain AO and .y will contain Validity
    OUTPUT_BUFFER(09, float4,            outputProbeDirectSHDataBuffer),
    OUTPUT_BUFFER(10, float4,            outputProbeOcclusionBuffer),
    OUTPUT_BUFFER(11, float4,            outputProbeIndirectSHDataBuffer),
    OUTPUT_BUFFER(12, float,             outputProbeValidityBuffer),
    OUTPUT_BUFFER(13, float,             outputProbeDepthOctahedronBuffer)
#else
    INPUT_VALUE(  03, int,               lightmapMode),
    INPUT_VALUE(  04, int,               useAo),
    INPUT_VALUE(  05, int,               useShadowmask),
    INPUT_BUFFER( 06, float3,            lightingExpandedBuffer),
    INPUT_BUFFER( 07, float4,            shadowmaskAoValidityExpandedBuffer),//when gathering indirect .x will contain AO and .y will contain Validity
    INPUT_BUFFER( 08, float4,            directionalExpandedBuffer),
    OUTPUT_BUFFER(09, float4,            outputDirectLightingBuffer),
    OUTPUT_BUFFER(10, float4,            outputShadowmaskFromDirectBuffer),
    OUTPUT_BUFFER(11, float4,            outputDirectionalFromDirectBuffer),
    OUTPUT_BUFFER(12, float4,            outputIndirectLightingBuffer),
    OUTPUT_BUFFER(13, float4,            outputEnvironmentLightingBuffer),
    OUTPUT_BUFFER(14, float4,            outputDirectionalFromGiBuffer),
    OUTPUT_BUFFER(15, float,             outputAoBuffer),
    OUTPUT_BUFFER(16, float,             outputValidityBuffer)
#endif
    KERNEL_VALIDATOR_BUFFERS_DEF
)
{
    const uint expandedTexelInfoIdx = get_global_id(0);
    const uint numExpandedTexels = INDEX_SAFE(expandedTexelsCountBuffer, 0);
    if (expandedTexelInfoIdx < numExpandedTexels)
    {
        const ExpandedTexelInfo expandedTexelInfo = INDEX_SAFE(expandedTexelsBuffer, expandedTexelInfoIdx);
        const int numRays = expandedTexelInfo.numRays;
        const int raysOffset = expandedTexelInfo.firstRaysOffset;
        const uint originalTexelIndex = expandedTexelInfo.originalTexelIndex;

#ifdef PROBES
        float4 probeOcclusion = (float4)(0.0f, 0.0f, 0.0f, 0.0f);
        for (int i = 0; i < numRays; ++i)
            probeOcclusion += INDEX_SAFE(probeOcclusionExpandedBuffer, raysOffset + i);

        if (lightmappingSourceType == kLightmappingSourceType_Probe)
        {
            float4 outSH[SH_COEFF_COUNT];

            for (int coeff = 0; coeff < SH_COEFF_COUNT; ++coeff)
                outSH[coeff] = (float4)(0.0f, 0.0f, 0.0f, 0.0f);

            float outDepthOctahedron[OCTAHEDRON_TEXEL_COUNT];
            if (outputProbeDepthOctahedronBuffer)
            {
                for (int texel = 0; texel < OCTAHEDRON_TEXEL_COUNT; ++texel)
                    outDepthOctahedron[texel] = 0;
            }

            float4 shadowMask = (float4)(0.0f, 0.0f, 0.0f, 0.0f);
            for (int i = 0; i < numRays; ++i)
            {
                int dataPositionCoeff = (raysOffset + i) * SH_COEFF_COUNT;
                for (int coeff = 0; coeff < SH_COEFF_COUNT; ++coeff)
                {
                    outSH[coeff] += INDEX_SAFE(probeSHExpandedBuffer, dataPositionCoeff + coeff);
                }

                shadowMask += INDEX_SAFE(shadowmaskAoValidityExpandedBuffer, raysOffset + i);

                if (outputProbeDepthOctahedronBuffer)
                {
                    int dataPositionOctahedron = (raysOffset + i) * OCTAHEDRON_TEXEL_COUNT;
                    for (int texel = 0; texel < OCTAHEDRON_TEXEL_COUNT; ++texel)
                    {
                        outDepthOctahedron[texel] += INDEX_SAFE(probeDepthOctahedronExpandedBuffer, dataPositionOctahedron + texel);
                    }
                }
            }

            // TODO(RadeonRays): memory access is all over the place, make a struct ala SphericalHarmonicsL2 instead of loading/storing with a stride.
            if (radeonRaysExpansionPass == kRRExpansionPass_direct)
            {
                for (int coeff = 0; coeff < SH_COEFF_COUNT; ++coeff)
                {
                    INDEX_SAFE(outputProbeDirectSHDataBuffer, numProbes * coeff + originalTexelIndex) += outSH[coeff];
                }
                INDEX_SAFE(outputProbeOcclusionBuffer, originalTexelIndex) += probeOcclusion;
            }
            else
            {
                for (int coeff = 0; coeff < SH_COEFF_COUNT; ++coeff)
                {
                    INDEX_SAFE(outputProbeIndirectSHDataBuffer, numProbes * coeff + originalTexelIndex) += outSH[coeff];
                }
            }

            if (radeonRaysExpansionPass == kRRExpansionPass_indirect)
            {
                INDEX_SAFE(outputProbeValidityBuffer, originalTexelIndex) += shadowMask.y;
                if (outputProbeDepthOctahedronBuffer)
                {
                    for (int texel = 0; texel < OCTAHEDRON_TEXEL_COUNT; ++texel)
                        INDEX_SAFE(outputProbeDepthOctahedronBuffer, originalTexelIndex * OCTAHEDRON_TEXEL_COUNT + texel) += outDepthOctahedron[texel];
                }
            }
        }
        else //Custom Bake
        {
            if (radeonRaysExpansionPass == kRRExpansionPass_direct)
            {
                INDEX_SAFE(outputProbeOcclusionBuffer, originalTexelIndex) += probeOcclusion;
            }
        }
#else
        float4 shadowMask = (float4)(0.0f, 0.0f, 0.0f, 0.0f);
        float3 lighting = (float3)(0.0f, 0.0f, 0.0f);
        bool accumulateShadowmask = ((radeonRaysExpansionPass == kRRExpansionPass_direct && useShadowmask) || radeonRaysExpansionPass == kRRExpansionPass_indirect);
        for (int i = 0; i < numRays; ++i)
        {
            if(accumulateShadowmask)
                shadowMask += shadowmaskAoValidityExpandedBuffer[raysOffset + i];
            lighting += lightingExpandedBuffer[raysOffset + i];
        }

        if (radeonRaysExpansionPass == kRRExpansionPass_direct)
        {
            if(useShadowmask)
                INDEX_SAFE(outputShadowmaskFromDirectBuffer, originalTexelIndex) += shadowMask;
            INDEX_SAFE(outputDirectLightingBuffer, originalTexelIndex).xyz += lighting;
        }
        else if (radeonRaysExpansionPass == kRRExpansionPass_environment)
        {
            INDEX_SAFE(outputEnvironmentLightingBuffer, originalTexelIndex).xyz += lighting;
        }
        else
        {
            KERNEL_ASSERT(radeonRaysExpansionPass == kRRExpansionPass_indirect);
            INDEX_SAFE(outputIndirectLightingBuffer, originalTexelIndex).xyz += lighting;
            if(useAo)
                INDEX_SAFE(outputAoBuffer, originalTexelIndex) += shadowMask.x;
            INDEX_SAFE(outputValidityBuffer, originalTexelIndex) += shadowMask.y;
        }

        if (lightmapMode == LIGHTMAPMODE_DIRECTIONAL)
        {
            float4 directionality = (float4)(0.0f, 0.0f, 0.0f, 0.0f);
            for (int i = 0; i < numRays; ++i)
            {
                directionality += directionalExpandedBuffer[raysOffset + i];
            }

            if (radeonRaysExpansionPass == kRRExpansionPass_direct)
            {
                INDEX_SAFE(outputDirectionalFromDirectBuffer, originalTexelIndex) += directionality;
            }
            else
            {
                INDEX_SAFE(outputDirectionalFromGiBuffer, originalTexelIndex) += directionality;
            }
        }
#endif
    }
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\OpenCL\kernels\expansionAndGathering.cl---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\OpenCL\kernels\fillBuffer.cl---------------
.
.
#include "commonCL.h"

#define FILLBUFFER(TYPE_) \
__kernel void fillBuffer_##TYPE_( \
    __global TYPE_* buffer,       \
    TYPE_ value,                  \
    int bufferSize                \
)                                 \
{                                 \
    int idx = get_global_id(0);   \
                                  \
    if (idx < bufferSize)         \
        buffer[idx] = value;      \
}

FILLBUFFER(float)
FILLBUFFER(float2)
FILLBUFFER(float4)
FILLBUFFER(Vector3f_storage)
FILLBUFFER(int)
FILLBUFFER(uint)
FILLBUFFER(uchar)
FILLBUFFER(uchar4)
FILLBUFFER(LightSample)
FILLBUFFER(LightBuffer)
FILLBUFFER(MeshDataOffsets)
FILLBUFFER(MaterialTextureProperties)
FILLBUFFER(ray)
FILLBUFFER(Matrix4x4)
FILLBUFFER(Intersection)
FILLBUFFER(OpenCLKernelAssert)
FILLBUFFER(ConvergenceOutputData)
FILLBUFFER(PackedNormalOctQuad)
FILLBUFFER(ExpandedTexelInfo)
FILLBUFFER(SampleDescription)
FILLBUFFER(PowerSamplingStat)
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\OpenCL\kernels\fillBuffer.cl---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\OpenCL\kernels\intersectBvh.cl---------------
.
.
/**********************************************************************
Copyright (c) 2016 Advanced Micro Devices, Inc. All rights reserved.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.  IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
********************************************************************/

//UNITY++
//Source: https://github.com/GPUOpen-LibrariesAndSDKs/RadeonRays_SDK/blob/master/RadeonRays/src/kernels/CL/intersect_bvh2_lds.cl

/*************************************************************************
EXTENSIONS
**************************************************************************/
#ifdef AMD_MEDIA_OPS
#pragma OPENCL EXTENSION cl_amd_media_ops2 : enable
#endif //! AMD_MEDIA_OPS
//UNITY--
/*************************************************************************
INCLUDES
**************************************************************************/
//UNITY++
#include "commonCL.h"
#include "textureFetch.h"
//UNITY--

/*************************************************************************
TYPE DEFINITIONS
**************************************************************************/

#define INVALID_ADDR 0xffffffffu
#define INTERNAL_NODE(node) (GetAddrLeft(node) != INVALID_ADDR)

//UNITY++
#define GROUP_SIZE INTERSECT_BVH_WORKGROUPSIZE
//UNITY--
#define STACK_SIZE 32
#define LDS_STACK_SIZE 16

// BVH node
typedef struct
{
    float4 aabb_left_min_or_v0_and_addr_left;
    float4 aabb_left_max_or_v1_and_mesh_id;
    float4 aabb_right_min_or_v2_and_addr_right;
    float4 aabb_right_max_and_prim_id;

} bvh_node;

//UNITY++
/*************************************************************************
HELPER FUNCTIONS
**************************************************************************/
//UNITY--

#define GetAddrLeft(node)   as_uint((node).aabb_left_min_or_v0_and_addr_left.w)
#define GetAddrRight(node)  as_uint((node).aabb_right_min_or_v2_and_addr_right.w)
#define GetMeshId(node)     as_uint((node).aabb_left_max_or_v1_and_mesh_id.w)
#define GetPrimId(node)     as_uint((node).aabb_right_max_and_prim_id.w)

//UNITY++
inline float min3(float a, float b, float c)
{
#ifdef AMD_MEDIA_OPS
    return amd_min3(a, b, c);
#else //! AMD_MEDIA_OPS
    return min(min(a, b), c);
#endif //! AMD_MEDIA_OPS
}

inline float max3(float a, float b, float c)
{
#ifdef AMD_MEDIA_OPS
    return amd_max3(a, b, c);
#else //! AMD_MEDIA_OPS
    return max(max(a, b), c);
#endif //! AMD_MEDIA_OPS
}
//UNITY--

inline float2 fast_intersect_bbox2(float3 pmin, float3 pmax, float3 invdir, float3 oxinvdir, float t_max)
{
    const float3 f = mad(pmax.xyz, invdir, oxinvdir);
    const float3 n = mad(pmin.xyz, invdir, oxinvdir);
    const float3 tmax = max(f, n);
    const float3 tmin = min(f, n);
    const float t1 = min(min3(tmax.x, tmax.y, tmax.z), t_max);
    const float t0 = max(max3(tmin.x, tmin.y, tmin.z), 0.f);
    return (float2)(t0, t1);
}

//UNITY++
// Intersect ray against a triangle and return intersection interval value if it is in
// (0, t_max], return t_max otherwise.
inline float fast_intersect_triangle(ray r, float3 v1, float3 v2, float3 v3, float t_max)
{
    float3 const e1 = v2 - v1;
    float3 const e2 = v3 - v1;
    float3 const s1 = cross(r.d.xyz, e2);

#ifdef USE_SAFE_MATH
    float const invd = 1.f / dot(s1, e1);
#else //! USE_SAFE_MATH
    float const invd = native_recip(dot(s1, e1));
#endif //! USE_SAFE_MATH

    float3 const d = r.o.xyz - v1;
    float const b1 = dot(d, s1) * invd;
    float3 const s2 = cross(d, e1);
    float const b2 = dot(r.d.xyz, s2) * invd;
    float const temp = dot(e2, s2) * invd;

    if (b1 < 0.f || b1 > 1.f || b2 < 0.f || b1 + b2 > 1.f || temp < 0.f || temp > t_max)
    {
        return t_max;
    }
    else
    {
        return temp;
    }
}

inline int ray_is_active(ray const* r)
{
    return r->extra.y;
}

inline float3 safe_invdir(ray r)
{
    float const dirx = r.d.x;
    float const diry = r.d.y;
    float const dirz = r.d.z;
    float const ooeps = 1e-8;
    float3 invdir;
    invdir.x = 1.0f / (fabs(dirx) > ooeps ? dirx : copysign(ooeps, dirx));
    invdir.y = 1.0f / (fabs(diry) > ooeps ? diry : copysign(ooeps, diry));
    invdir.z = 1.0f / (fabs(dirz) > ooeps ? dirz : copysign(ooeps, dirz));
    return invdir;
}

// Given a point in triangle plane, calculate its barycentrics
inline float2 triangle_calculate_barycentrics(float3 p, float3 v1, float3 v2, float3 v3)
{
    float3 const e1 = v2 - v1;
    float3 const e2 = v3 - v1;
    float3 const e = p - v1;
    float const d00 = dot(e1, e1);
    float const d01 = dot(e1, e2);
    float const d11 = dot(e2, e2);
    float const d20 = dot(e, e1);
    float const d21 = dot(e, e2);

#ifdef USE_SAFE_MATH
    float const invdenom = 1.0f / (d00 * d11 - d01 * d01);
#else //! USE_SAFE_MATH
    float const invdenom = native_recip(d00 * d11 - d01 * d01);
#endif //! USE_SAFE_MATH

    float const b1 = (d11 * d20 - d01 * d21) * invdenom;
    float const b2 = (d00 * d21 - d01 * d20) * invdenom;

    return (float2)(b1, b2);
}

/*************************************************************************
KERNELS
**************************************************************************/

__kernel void clearIntersectionBuffer(
    OUTPUT_BUFFER(00, Intersection, pathIntersectionsCompactedBuffer)
    KERNEL_VALIDATOR_BUFFERS_DEF
)
{
    int idx = get_global_id(0);
    INDEX_SAFE(pathIntersectionsCompactedBuffer, idx).primid = MISS_MARKER;
    INDEX_SAFE(pathIntersectionsCompactedBuffer, idx).shapeid = MISS_MARKER;
    INDEX_SAFE(pathIntersectionsCompactedBuffer, idx).uvwt = (float4)(0.0f, 0.0f, 0.0f, 0.0f);
}

__kernel void clearOcclusionBuffer(
    OUTPUT_BUFFER(00,float4, lightOcclusionCompactedBuffer)
    KERNEL_VALIDATOR_BUFFERS_DEF
)
{
    int idx = get_global_id(0);
    INDEX_SAFE(lightOcclusionCompactedBuffer, idx) = (float4)(1.0f, 1.0f, 1.0f, 1.0f);
}
//UNITY--

__attribute__((reqd_work_group_size(GROUP_SIZE, 1, 1)))
__kernel void intersectWithTransmission(
    INPUT_BUFFER( 00, bvh_node,                  nodes),
    INPUT_BUFFER( 01, ray,                       pathRaysCompactedBuffer_0),
    INPUT_BUFFER( 02, uint,                      activePathCountBuffer_0),
    OUTPUT_BUFFER(03, uint,                      bvhStackBuffer),
    OUTPUT_BUFFER(04, Intersection,              pathIntersectionsCompactedBuffer),
//UNITY++
    OUTPUT_BUFFER(05, uint,                      transparentPathRayIndicesCompactedBuffer),
    OUTPUT_BUFFER(06, uint,                      transparentPathRayIndicesCompactedCountBuffer),
    OUTPUT_BUFFER(07, uint,                      totalRaysCastCountBuffer),
    INPUT_BUFFER( 08, MaterialTextureProperties, instanceIdToTransmissionTextureProperties),
    INPUT_BUFFER( 09, float4,                    instanceIdToTransmissionTextureSTs),
    INPUT_BUFFER( 10, MeshDataOffsets,           instanceIdToMeshDataOffsets),
    INPUT_BUFFER( 11, uchar4,                    dynarg_texture_buffer),
    INPUT_BUFFER( 12, uint,                      geometryIndicesBuffer),
    INPUT_BUFFER( 13, float2,                    geometryUV0sBuffer),
    INPUT_VALUE(  14, int,                       lightmapSize),
    INPUT_VALUE(  15, int,                       bounce),
    INPUT_VALUE(  16, int,                       superSamplingMultiplier),
    INPUT_BUFFER( 17, float,                     goldenSample_buffer),
    INPUT_BUFFER( 18, uint,                      sobol_buffer),
    INPUT_BUFFER( 19, SampleDescription,         sampleDescriptionsExpandedBuffer)
    KERNEL_VALIDATOR_BUFFERS_DEF
)
{
    __local uint numTransparentRaySharedMem;
    if (get_local_id(0) == 0)
        numTransparentRaySharedMem = 0;
    barrier(CLK_LOCAL_MEM_FENCE);

    bool atLeastATransparentMaterialWasHit = false;
    __local uint lds_stack[GROUP_SIZE * LDS_STACK_SIZE];
//UNITY--

    uint index = get_global_id(0);
    uint local_index = get_local_id(0);

    // Handle only working subset
    if (index < INDEX_SAFE(activePathCountBuffer_0, 0))
    {
        const ray my_ray = INDEX_SAFE(pathRaysCompactedBuffer_0, index);

        if (ray_is_active(&my_ray))
        {
            const float3 invDir = safe_invdir(my_ray);
            const float3 oxInvDir = -my_ray.o.xyz * invDir;

            // Intersection parametric distance
            float closest_t = my_ray.o.w;

            // Current node address
            uint addr = 0;
            // Current closest address
            uint closest_addr = INVALID_ADDR;

            uint stack_bottom = STACK_SIZE * index;
            uint sptr = stack_bottom;
            uint lds_stack_bottom = local_index * LDS_STACK_SIZE;
            uint lds_sptr = lds_stack_bottom;

            KERNEL_ASSERT(lds_sptr < GROUP_SIZE * LDS_STACK_SIZE);
            lds_stack[lds_sptr++] = INVALID_ADDR;

            //UNITY++
            int  sampleDimension = bounce * PLM_MAX_NUM_SOBOL_DIMENSIONS_PER_BOUNCE;
            const int rayLodLevel = UnpackLODMask(my_ray.extra.x);
            const int rayLodGroup = UnpackLODGroup(my_ray.extra.x);
            int hitLodParam = my_ray.extra.x;
            //UNITY--

            while (addr != INVALID_ADDR)
            {
                const bvh_node node = nodes[addr];

                if (INTERNAL_NODE(node))
                {
                    float2 s0 = fast_intersect_bbox2(
                        node.aabb_left_min_or_v0_and_addr_left.xyz,
                        node.aabb_left_max_or_v1_and_mesh_id.xyz,
                        invDir, oxInvDir, closest_t);
                    float2 s1 = fast_intersect_bbox2(
                        node.aabb_right_min_or_v2_and_addr_right.xyz,
                        node.aabb_right_max_and_prim_id.xyz,
                        invDir, oxInvDir, closest_t);

                    bool traverse_c0 = (s0.x <= s0.y);
                    bool traverse_c1 = (s1.x <= s1.y);
                    bool c1first = traverse_c1 && (s0.x > s1.x);

                    if (traverse_c0 || traverse_c1)
                    {
                        uint deferred = INVALID_ADDR;

                        if (c1first || !traverse_c0)
                        {
                            addr = GetAddrRight(node);
                            deferred = GetAddrLeft(node);
                        }
                        else
                        {
                            addr = GetAddrLeft(node);
                            deferred = GetAddrRight(node);
                        }

                        if (traverse_c0 && traverse_c1)
                        {
                            if (lds_sptr - lds_stack_bottom >= LDS_STACK_SIZE)
                            {
                                for (int i = 1; i < LDS_STACK_SIZE; ++i)
                                {
                                    KERNEL_ASSERT(lds_stack_bottom + i < GROUP_SIZE * LDS_STACK_SIZE);
                                    INDEX_SAFE(bvhStackBuffer, sptr + i) = lds_stack[lds_stack_bottom + i];
                                }

                                sptr += LDS_STACK_SIZE;
                                lds_sptr = lds_stack_bottom + 1;
                            }

                            KERNEL_ASSERT(lds_sptr < GROUP_SIZE * LDS_STACK_SIZE);
                            lds_stack[lds_sptr++] = deferred;
                        }

                        continue;
                    }
                }
                else
                {
                    float t = fast_intersect_triangle(
                        my_ray,
                        node.aabb_left_min_or_v0_and_addr_left.xyz,
                        node.aabb_left_max_or_v1_and_mesh_id.xyz,
                        node.aabb_right_min_or_v2_and_addr_right.xyz,
                        closest_t);

                    if (t < closest_t)
                    {
//UNITY++
                        const int instanceId = GetMeshId(node) - 1;
                        const MaterialTextureProperties matProperty = INDEX_SAFE(instanceIdToTransmissionTextureProperties, instanceId);
                        const int instanceLODMask = UnpackLODMask(matProperty.lodInfo);
                        const int instanceLODGroup = UnpackLODGroup(matProperty.lodInfo);
                        const bool isInstanceHit = IsInstanceHit(instanceLODMask, instanceLODGroup, rayLodLevel, rayLodGroup);

                        if (isInstanceHit)
                        {
                            hitLodParam = (instanceLODMask & 1) ? ((1 << 24) | (NO_LOD_GROUP & ((1<<24)-1))) : hitLodParam;
                            // Evaluate whether we've hit a transparent material
                            bool useTransmission = GetMaterialProperty(matProperty, kMaterialInstanceProperties_UseTransmission);
                            if (useTransmission)
                            {
                                const float3 p = my_ray.o.xyz + t * my_ray.d.xyz;
                                const float2 barycentricCoord = triangle_calculate_barycentrics(
                                    p,
                                    node.aabb_left_min_or_v0_and_addr_left.xyz,
                                    node.aabb_left_max_or_v1_and_mesh_id.xyz,
                                    node.aabb_right_min_or_v2_and_addr_right.xyz);

                                const int primIndex = GetPrimId(node);
                                const float2 geometryUVs = GetUVsAtPrimitiveIntersection(instanceId, primIndex, barycentricCoord, instanceIdToMeshDataOffsets, geometryUV0sBuffer, geometryIndicesBuffer KERNEL_VALIDATOR_BUFFERS);
                                const float2 textureUVs = geometryUVs * INDEX_SAFE(instanceIdToTransmissionTextureSTs, instanceId).xy + INDEX_SAFE(instanceIdToTransmissionTextureSTs, instanceId).zw;
                                const float4 transmission = FetchUChar4TextureFromMaterialAndUVs(dynarg_texture_buffer, textureUVs, matProperty, false, false KERNEL_VALIDATOR_BUFFERS);
                                const float averageTransmission = dot(transmission.xyz, kAverageFactors);
                                const int expandedRayIndex = Ray_GetSampleDescriptionIndex(GET_PTR_SAFE(pathRaysCompactedBuffer_0, index));
                                const SampleDescription sampleDescription = INDEX_SAFE(sampleDescriptionsExpandedBuffer, expandedRayIndex);

                                float rnd = SobolSample(sampleDescription.currentSampleCount, sampleDimension, sobol_buffer KERNEL_VALIDATOR_BUFFERS);

                                int texel_x = sampleDescription.texelIndex % lightmapSize;
                                int texel_y = sampleDescription.texelIndex / lightmapSize;
                                rnd = ApplyCranleyPattersonRotation1D(rnd, texel_x, texel_y, lightmapSize, sampleDimension, goldenSample_buffer KERNEL_VALIDATOR_BUFFERS);

                                // NOTE: This is wrong! The probability of either reflecting or refracting a ray
                                // should depend on the Fresnel of the material. However, since we do not support
                                // any specularity in PVR there is currently no way to query this value, so for now
                                // we use the transmission (texture) albedo.
                                if (rnd >= averageTransmission)
                                {
                                    //Bounce of the transparent material, the material is considered opaque.
                                    closest_t = t;
                                    closest_addr = addr;
                                }
                                else
                                {
                                    //Thought the transparent material, attenuation will need to be collected in an additional pass (specialized occlusion pass)
                                    atLeastATransparentMaterialWasHit = true;
                                    ++sampleDimension;
                                    sampleDimension %= SOBOL_MATRICES_COUNT;
                                }
                            }
                            else
                            {
    //UNITY--
                                closest_t = t;
                                closest_addr = addr;
                            }
                        }
                    }
                }

                KERNEL_ASSERT(lds_sptr - 1 < GROUP_SIZE * LDS_STACK_SIZE);
                addr = lds_stack[--lds_sptr];

                if (addr == INVALID_ADDR && sptr > stack_bottom)
                {
                    sptr -= LDS_STACK_SIZE;
                    for (int i = 1; i < LDS_STACK_SIZE; ++i)
                    {
                        KERNEL_ASSERT(lds_stack_bottom + i < GROUP_SIZE * LDS_STACK_SIZE);
                        lds_stack[lds_stack_bottom + i] = INDEX_SAFE(bvhStackBuffer, sptr + i);
                    }

                    lds_sptr = lds_stack_bottom + LDS_STACK_SIZE - 1;
                    KERNEL_ASSERT(lds_sptr < GROUP_SIZE * LDS_STACK_SIZE);
                    addr = lds_stack[lds_sptr];
                }
            }

            // Check if we have found an intersection
            if (closest_addr != INVALID_ADDR)
            {
                // Calculate hit position
                const bvh_node node = nodes[closest_addr];
                const float3 p = my_ray.o.xyz + closest_t * my_ray.d.xyz;

                // Calculate barycentric coordinates
                const float2 uv = triangle_calculate_barycentrics(
                    p,
                    node.aabb_left_min_or_v0_and_addr_left.xyz,
                    node.aabb_left_max_or_v1_and_mesh_id.xyz,
                    node.aabb_right_min_or_v2_and_addr_right.xyz);

                // Update hit information
                INDEX_SAFE(pathIntersectionsCompactedBuffer, index).primid = GetPrimId(node);
                INDEX_SAFE(pathIntersectionsCompactedBuffer, index).shapeid = GetMeshId(node);
                INDEX_SAFE(pathIntersectionsCompactedBuffer, index).uvwt = (float4)(uv.x, uv.y, 0.0f, closest_t);
                INDEX_SAFE(pathIntersectionsCompactedBuffer, index).padding0 = hitLodParam;
            }
            else
            {
                // Miss here
                INDEX_SAFE(pathIntersectionsCompactedBuffer, index).primid = MISS_MARKER;
                INDEX_SAFE(pathIntersectionsCompactedBuffer, index).shapeid = MISS_MARKER;
                INDEX_SAFE(pathIntersectionsCompactedBuffer, index).padding0 = hitLodParam;
            }
        }
    }

//UNITY++
    //Compact transparent ray that will be process further via the adjustPathThroughputFromIntersection kernel (see below)
    int compactedTransparentRayIndex = -1;
    if (atLeastATransparentMaterialWasHit)
        compactedTransparentRayIndex = atomic_inc(&numTransparentRaySharedMem);

    barrier(CLK_LOCAL_MEM_FENCE);
    if (get_local_id(0) == 0)
    {
        if (numTransparentRaySharedMem)
        {
            atomic_add(GET_PTR_SAFE(totalRaysCastCountBuffer, 0), numTransparentRaySharedMem);
        }
        numTransparentRaySharedMem = atomic_add(GET_PTR_SAFE(transparentPathRayIndicesCompactedCountBuffer, 0), numTransparentRaySharedMem);
    }
    barrier(CLK_LOCAL_MEM_FENCE);
    if (atLeastATransparentMaterialWasHit)
    {
        KERNEL_ASSERT(compactedTransparentRayIndex >= 0);
        INDEX_SAFE(transparentPathRayIndicesCompactedBuffer, numTransparentRaySharedMem + compactedTransparentRayIndex) = index;
    }
//UNITY--
}

//UNITY++
// This kernel is a copy of the occlusion one, but specialized to collect transmission in the ray path.
__attribute__((reqd_work_group_size(GROUP_SIZE, 1, 1)))
__kernel void adjustPathThroughputFromIntersection(
//UNITY--
    INPUT_BUFFER( 00, bvh_node,                  nodes),
    INPUT_BUFFER( 01, ray,                       pathRaysCompactedBuffer_0),
    INPUT_BUFFER( 02, uint,                      activePathCountBuffer_0),
    OUTPUT_BUFFER(03, uint,                      bvhStackBuffer),
    //UNITY++
    OUTPUT_BUFFER(04, float4,                    pathThroughputExpandedBuffer),
    INPUT_BUFFER( 05, Intersection,              pathIntersectionsCompactedBuffer),
    INPUT_BUFFER( 06, uint,                      transparentPathRayIndicesCompactedBuffer),
    INPUT_BUFFER( 07, uint,                      transparentPathRayIndicesCompactedCountBuffer),
    INPUT_BUFFER( 08, MaterialTextureProperties, instanceIdToTransmissionTextureProperties),
    INPUT_BUFFER( 09, float4,                    instanceIdToTransmissionTextureSTs),
    INPUT_BUFFER( 10, MeshDataOffsets,           instanceIdToMeshDataOffsets),
    INPUT_BUFFER( 11, uchar4,                    dynarg_texture_buffer),
    INPUT_BUFFER( 12, uint,                      geometryIndicesBuffer),
    INPUT_BUFFER( 13, float2,                    geometryUV0sBuffer)
    KERNEL_VALIDATOR_BUFFERS_DEF
    //UNITY--
)
{
    uint index = get_global_id(0);
    uint local_index = get_local_id(0);

    //UNITY++
    __local uint lds_stack[GROUP_SIZE * LDS_STACK_SIZE];
    //UNITY--

    // Handle only working subset
    if (index < INDEX_SAFE(transparentPathRayIndicesCompactedCountBuffer, 0))
    {
        const int compactedRayIndex = INDEX_SAFE(transparentPathRayIndicesCompactedBuffer, index);
        const ray my_ray = INDEX_SAFE(pathRaysCompactedBuffer_0, compactedRayIndex);
        KERNEL_ASSERT(ray_is_active(&my_ray));
        //UNITY++
        const Intersection my_intersection = INDEX_SAFE(pathIntersectionsCompactedBuffer, compactedRayIndex);

        const int rayLodLevel = UnpackLODMask(my_ray.extra.x);
        const int rayLodGroup = UnpackLODGroup(my_ray.extra.x);
        //UNITY--

        const float3 invDir = safe_invdir(my_ray);
        const float3 oxInvDir = -my_ray.o.xyz * invDir;

        // Current node address
        uint addr = 0;
        //UNITY++
        // Intersection distance or ray distance if the ray did not stop on a geometry.
        const float closest_t = (my_intersection.primid == MISS_MARKER)? my_ray.o.w : my_intersection.uvwt.w;
        //UNITY--

        uint stack_bottom = STACK_SIZE * index;
        uint sptr = stack_bottom;
        uint lds_stack_bottom = local_index * LDS_STACK_SIZE;
        uint lds_sptr = lds_stack_bottom;

        KERNEL_ASSERT(lds_sptr < GROUP_SIZE * LDS_STACK_SIZE);
        lds_stack[lds_sptr++] = INVALID_ADDR;

        while (addr != INVALID_ADDR)
        {
            const bvh_node node = nodes[addr];

            if (INTERNAL_NODE(node))
            {
                float2 s0 = fast_intersect_bbox2(
                    node.aabb_left_min_or_v0_and_addr_left.xyz,
                    node.aabb_left_max_or_v1_and_mesh_id.xyz,
                    invDir, oxInvDir, closest_t);
                float2 s1 = fast_intersect_bbox2(
                    node.aabb_right_min_or_v2_and_addr_right.xyz,
                    node.aabb_right_max_and_prim_id.xyz,
                    invDir, oxInvDir, closest_t);

                bool traverse_c0 = (s0.x <= s0.y);
                bool traverse_c1 = (s1.x <= s1.y);
                bool c1first = traverse_c1 && (s0.x > s1.x);

                if (traverse_c0 || traverse_c1)
                {
                    uint deferred = INVALID_ADDR;

                    if (c1first || !traverse_c0)
                    {
                        addr = GetAddrRight(node);
                        deferred = GetAddrLeft(node);
                    }
                    else
                    {
                        addr = GetAddrLeft(node);
                        deferred = GetAddrRight(node);
                    }

                    if (traverse_c0 && traverse_c1)
                    {
                        if (lds_sptr - lds_stack_bottom >= LDS_STACK_SIZE)
                        {
                            for (int i = 1; i < LDS_STACK_SIZE; ++i)
                            {
                                KERNEL_ASSERT(lds_stack_bottom + i < GROUP_SIZE * LDS_STACK_SIZE);
                                INDEX_SAFE(bvhStackBuffer, sptr + i) = lds_stack[lds_stack_bottom + i];
                            }

                            sptr += LDS_STACK_SIZE;
                            lds_sptr = lds_stack_bottom + 1;
                        }

                        KERNEL_ASSERT(lds_sptr < GROUP_SIZE * LDS_STACK_SIZE);
                        lds_stack[lds_sptr++] = deferred;
                    }

                    continue;
                }
            }
            else
            {
                float t = fast_intersect_triangle(
                    my_ray,
                    node.aabb_left_min_or_v0_and_addr_left.xyz,
                    node.aabb_left_max_or_v1_and_mesh_id.xyz,
                    node.aabb_right_min_or_v2_and_addr_right.xyz,
                    closest_t);

                if (t < closest_t)
                {
                    //UNITY++
                    const int instanceId = GetMeshId(node) - 1;
                    const MaterialTextureProperties matProperty = INDEX_SAFE(instanceIdToTransmissionTextureProperties, instanceId);
                    const int instanceLODMask = UnpackLODMask(matProperty.lodInfo);
                    const int instanceLODGroup = UnpackLODGroup(matProperty.lodInfo);
                    const bool isInstanceHit = IsInstanceHit(instanceLODMask, instanceLODGroup, rayLodLevel, rayLodGroup);
                    if (isInstanceHit)
                    {
                        // Evaluate transparent material attenuation
                        bool useTransmission = GetMaterialProperty(matProperty, kMaterialInstanceProperties_UseTransmission);
                        if (useTransmission)
                        {
                            const float3 p = my_ray.o.xyz + t * my_ray.d.xyz;
                            const float2 barycentricCoord = triangle_calculate_barycentrics(
                                p,
                                node.aabb_left_min_or_v0_and_addr_left.xyz,
                                node.aabb_left_max_or_v1_and_mesh_id.xyz,
                                node.aabb_right_min_or_v2_and_addr_right.xyz);

                            const int primIndex = GetPrimId(node);
                            const float2 geometryUVs = GetUVsAtPrimitiveIntersection(instanceId, primIndex, barycentricCoord, instanceIdToMeshDataOffsets, geometryUV0sBuffer, geometryIndicesBuffer KERNEL_VALIDATOR_BUFFERS);
                            const float2 textureUVs = geometryUVs * INDEX_SAFE(instanceIdToTransmissionTextureSTs, instanceId).xy + INDEX_SAFE(instanceIdToTransmissionTextureSTs, instanceId).zw;
                            const float4 transmission = FetchUChar4TextureFromMaterialAndUVs(dynarg_texture_buffer, textureUVs, matProperty, false, false KERNEL_VALIDATOR_BUFFERS);
                            const float averageTransmission = dot(transmission.xyz, kAverageFactors);
                            const int expandedRayIndex = Ray_GetSampleDescriptionIndex(GET_PTR_SAFE(pathRaysCompactedBuffer_0, compactedRayIndex));
                            INDEX_SAFE(pathThroughputExpandedBuffer, expandedRayIndex) *= (float4)(transmission.x, transmission.y, transmission.z, averageTransmission);
                        }
                    }
                    //UNITY--
                }
            }
            KERNEL_ASSERT(lds_sptr - 1 < GROUP_SIZE * LDS_STACK_SIZE);
            addr = lds_stack[--lds_sptr];

            if (addr == INVALID_ADDR && sptr > stack_bottom)
            {
                sptr -= LDS_STACK_SIZE;
                for (int i = 1; i < LDS_STACK_SIZE; ++i)
                {
                    KERNEL_ASSERT(lds_stack_bottom + i < GROUP_SIZE * LDS_STACK_SIZE);
                    lds_stack[lds_stack_bottom + i] = INDEX_SAFE(bvhStackBuffer, sptr + i);
                }

                lds_sptr = lds_stack_bottom + LDS_STACK_SIZE - 1;
                KERNEL_ASSERT(lds_sptr < GROUP_SIZE * LDS_STACK_SIZE);
                addr = lds_stack[lds_sptr];
            }
//UNITY++
        }
//UNITY--
    }
}

__attribute__((reqd_work_group_size(GROUP_SIZE, 1, 1)))
__kernel void occludedWithTransmission(
    INPUT_BUFFER( 00, bvh_node,                  nodes),
    INPUT_BUFFER( 01, ray,                       lightRaysCompactedBuffer),
    INPUT_BUFFER( 02, uint,                      lightRaysCountBuffer),
    OUTPUT_BUFFER(03, uint,                      bvhStackBuffer),
//UNITY++
    OUTPUT_BUFFER(04, float4,                    lightOcclusionCompactedBuffer),
    INPUT_BUFFER( 05, MaterialTextureProperties, instanceIdToTransmissionTextureProperties),
    INPUT_BUFFER( 06, float4,                    instanceIdToTransmissionTextureSTs),
    INPUT_BUFFER( 07, MeshDataOffsets,           instanceIdToMeshDataOffsets),
    INPUT_BUFFER( 08, uchar4,                    dynarg_texture_buffer),
    INPUT_BUFFER( 09, uint,                      geometryIndicesBuffer),
    INPUT_BUFFER( 10, float2,                    geometryUV0sBuffer),
    INPUT_VALUE(  11, int,                       useCastShadowsFlag)
    KERNEL_VALIDATOR_BUFFERS_DEF
//UNITY--
)
{
    uint index = get_global_id(0);
    uint local_index = get_local_id(0);
//UNITY++
    __local uint lds_stack[GROUP_SIZE * LDS_STACK_SIZE];
//UNITY--

    // Handle only working subset
    if (index < INDEX_SAFE(lightRaysCountBuffer, 0))
    {
//UNITY++
        // Initialize memory
        INDEX_SAFE(lightOcclusionCompactedBuffer, index) = (float4)(1.0f, 1.0f, 1.0f, 1.0f);
//UNITY--

        const ray my_ray = INDEX_SAFE(lightRaysCompactedBuffer, index);

        if (ray_is_active(&my_ray))
        {
            //UNITY++
            const int rayLodLevel = UnpackLODMask(my_ray.extra.x);
            const int rayLodGroup = UnpackLODGroup(my_ray.extra.x);
            //UNITY--
            const float3 invDir = safe_invdir(my_ray);
            const float3 oxInvDir = -my_ray.o.xyz * invDir;

            // Current node address
            uint addr = 0;
            // Intersection parametric distance
            const float closest_t = my_ray.o.w;

            uint stack_bottom = STACK_SIZE * index;
            uint sptr = stack_bottom;
            uint lds_stack_bottom = local_index * LDS_STACK_SIZE;
            uint lds_sptr = lds_stack_bottom;

            KERNEL_ASSERT(lds_sptr < GROUP_SIZE * LDS_STACK_SIZE);
            lds_stack[lds_sptr++] = INVALID_ADDR;

            while (addr != INVALID_ADDR)
            {
                const bvh_node node = nodes[addr];

                if (INTERNAL_NODE(node))
                {
                    float2 s0 = fast_intersect_bbox2(
                        node.aabb_left_min_or_v0_and_addr_left.xyz,
                        node.aabb_left_max_or_v1_and_mesh_id.xyz,
                        invDir, oxInvDir, closest_t);
                    float2 s1 = fast_intersect_bbox2(
                        node.aabb_right_min_or_v2_and_addr_right.xyz,
                        node.aabb_right_max_and_prim_id.xyz,
                        invDir, oxInvDir, closest_t);

                    bool traverse_c0 = (s0.x <= s0.y);
                    bool traverse_c1 = (s1.x <= s1.y);
                    bool c1first = traverse_c1 && (s0.x > s1.x);

                    if (traverse_c0 || traverse_c1)
                    {
                        uint deferred = INVALID_ADDR;

                        if (c1first || !traverse_c0)
                        {
                            addr = GetAddrRight(node);
                            deferred = GetAddrLeft(node);
                        }
                        else
                        {
                            addr = GetAddrLeft(node);
                            deferred = GetAddrRight(node);
                        }

                        if (traverse_c0 && traverse_c1)
                        {
                            if (lds_sptr - lds_stack_bottom >= LDS_STACK_SIZE)
                            {
                                for (int i = 1; i < LDS_STACK_SIZE; ++i)
                                {
                                    KERNEL_ASSERT(lds_stack_bottom + i < GROUP_SIZE * LDS_STACK_SIZE);
                                    INDEX_SAFE(bvhStackBuffer, sptr + i) = lds_stack[lds_stack_bottom + i];
                                }

                                sptr += LDS_STACK_SIZE;
                                lds_sptr = lds_stack_bottom + 1;
                            }

                            KERNEL_ASSERT(lds_sptr < GROUP_SIZE * LDS_STACK_SIZE);
                            lds_stack[lds_sptr++] = deferred;
                        }

                        continue;
                    }
                }
                else
                {
                    float t = fast_intersect_triangle(
                        my_ray,
                        node.aabb_left_min_or_v0_and_addr_left.xyz,
                        node.aabb_left_max_or_v1_and_mesh_id.xyz,
                        node.aabb_right_min_or_v2_and_addr_right.xyz,
                        closest_t);

                    if (t < closest_t)
                    {
//UNITY++
                        const int instanceId = GetMeshId(node) - 1;
                        const MaterialTextureProperties matProperty = INDEX_SAFE(instanceIdToTransmissionTextureProperties, instanceId);
                        const int instanceLODMask = UnpackLODMask(matProperty.lodInfo);
                        const int instanceLODGroup = UnpackLODGroup(matProperty.lodInfo);
                        const bool isInstanceHit = IsInstanceHit(instanceLODMask, instanceLODGroup, rayLodLevel, rayLodGroup);

                        if (isInstanceHit)
                        {
                            bool castShadows = (useCastShadowsFlag ? GetMaterialProperty(matProperty, kMaterialInstanceProperties_CastShadows) : true);
                            if (castShadows)
                            {
                                // Evaluate whether we've hit a transparent material
                                bool useTransmission = GetMaterialProperty(matProperty, kMaterialInstanceProperties_UseTransmission);
                                if (useTransmission)
                                {
                                    const float3 p = my_ray.o.xyz + t * my_ray.d.xyz;
                                    const float2 barycentricCoord = triangle_calculate_barycentrics(
                                        p,
                                        node.aabb_left_min_or_v0_and_addr_left.xyz,
                                        node.aabb_left_max_or_v1_and_mesh_id.xyz,
                                        node.aabb_right_min_or_v2_and_addr_right.xyz);

                                    const int primIndex = GetPrimId(node);
                                    const float2 geometryUVs = GetUVsAtPrimitiveIntersection(instanceId, primIndex, barycentricCoord, instanceIdToMeshDataOffsets, geometryUV0sBuffer, geometryIndicesBuffer KERNEL_VALIDATOR_BUFFERS);
                                    const float2 textureUVs = geometryUVs * INDEX_SAFE(instanceIdToTransmissionTextureSTs, instanceId).xy + INDEX_SAFE(instanceIdToTransmissionTextureSTs, instanceId).zw;
                                    const float4 transmission = FetchUChar4TextureFromMaterialAndUVs(dynarg_texture_buffer, textureUVs, matProperty, false, false KERNEL_VALIDATOR_BUFFERS);
                                    const float averageTransmission = dot(transmission.xyz, kAverageFactors);
                                    INDEX_SAFE(lightOcclusionCompactedBuffer, index) *= (float4)(transmission.x, transmission.y, transmission.z, averageTransmission);
                                    if (INDEX_SAFE(lightOcclusionCompactedBuffer, index).w < TRANSMISSION_THRESHOLD)
                                        return;// fully occluded
                                }
                                else
                                {
                                    INDEX_SAFE(lightOcclusionCompactedBuffer, index) = (float4)(0.0f, 0.0f, 0.0f, 0.0f);
                                    return;// fully occluded
                                }
                            }
                        }
//UNITY--
                    }
                }
                KERNEL_ASSERT(lds_sptr - 1 < GROUP_SIZE * LDS_STACK_SIZE);
                addr = lds_stack[--lds_sptr];

                if (addr == INVALID_ADDR && sptr > stack_bottom)
                {
                    sptr -= LDS_STACK_SIZE;
                    for (int i = 1; i < LDS_STACK_SIZE; ++i)
                    {
                        KERNEL_ASSERT(lds_stack_bottom + i < GROUP_SIZE * LDS_STACK_SIZE);
                        lds_stack[lds_stack_bottom + i] = INDEX_SAFE(bvhStackBuffer, sptr + i);
                    }

                    lds_sptr = lds_stack_bottom + LDS_STACK_SIZE - 1;
                    KERNEL_ASSERT(lds_sptr < GROUP_SIZE * LDS_STACK_SIZE);
                    addr = lds_stack[lds_sptr];
                }
            }
        }
    }
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\OpenCL\kernels\intersectBvh.cl---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\OpenCL\kernels\postProcessProbes.cl---------------
.
.
#include "commonCL.h"

#define kSphericalHarmonicsL2_CoeffCount 9
#define kSphericalHarmonicsL2_ColorChannelCount 3
#define kSphericalHarmonicsL2_FloatCount (kSphericalHarmonicsL2_CoeffCount * kSphericalHarmonicsL2_ColorChannelCount)

typedef struct _SphericalHarmonicsL2
{
    // Notation:
    // http://graphics.stanford.edu/papers/envmap/envmap.pdf
    //
    //                       [L00:  DC]
    //            [L1-1:  y] [L10:   z] [L11:   x]
    // [L2-2: xy] [L2-1: yz] [L20:  zz] [L21:  xz]  [L22:  xx - yy]
    //
    // 9 coefficients for R, G and B ordered:
    // {  L00, L1-1,  L10,  L11, L2-2, L2-1,  L20,  L21,  L22,  // red   channel
    //    L00, L1-1,  L10,  L11, L2-2, L2-1,  L20,  L21,  L22,  // blue  channel
    //    L00, L1-1,  L10,  L11, L2-2, L2-1,  L20,  L21,  L22 } // green channel
    float sh[kSphericalHarmonicsL2_FloatCount];
} SphericalHarmonicsL2;

#define L00 0
#define L1_1 1
#define L10 2
#define L11 3
#define L2_2 4
#define L2_1 5
#define L20 6
#define L21 7
#define L22 8

// aHat is from https://cseweb.ucsd.edu/~ravir/papers/envmap/envmap.pdf and is used to convert spherical radiance to irradiance.
#define aHat0 3.1415926535897932384626433832795028841971693993751058209749445923f // π
#define aHat1 2.0943951023931954923084289221863352561314462662500705473166297282f // 2π/3
#define aHat2 0.785398f // π/4 (see equation 8).

__kernel void ConvolveRadianceToIrradiance(
    //*** input ***
    INPUT_BUFFER(00, SphericalHarmonicsL2, radianceIn),
    INPUT_VALUE(01, int, probeCount),
    //*** output ***
    OUTPUT_BUFFER(02, SphericalHarmonicsL2, irradianceOut)
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    const uint probeIdx = get_global_id(0);
    KERNEL_ASSERT(probeIdx < probeCount);
    if (probeIdx >= probeCount)
        return;

    SphericalHarmonicsL2 radiance = INDEX_SAFE(radianceIn, probeIdx);
    SphericalHarmonicsL2 irradiance;
    for (int rgb = 0; rgb < 3; ++rgb)
    {
        const int rgbOffset = rgb * kSphericalHarmonicsL2_CoeffCount;

        irradiance.sh[rgbOffset + L00] =  radiance.sh[rgbOffset + L00] * aHat0;
        irradiance.sh[rgbOffset + L1_1] = radiance.sh[rgbOffset + L1_1] * aHat1;
        irradiance.sh[rgbOffset + L10] =  radiance.sh[rgbOffset + L10] * aHat1;
        irradiance.sh[rgbOffset + L11] =  radiance.sh[rgbOffset + L11] * aHat1;
        irradiance.sh[rgbOffset + L2_2] = radiance.sh[rgbOffset + L2_2] * aHat2;
        irradiance.sh[rgbOffset + L2_1] = radiance.sh[rgbOffset + L2_1] * aHat2;
        irradiance.sh[rgbOffset + L20] =  radiance.sh[rgbOffset + L20] * aHat2;
        irradiance.sh[rgbOffset + L21] =  radiance.sh[rgbOffset + L21] * aHat2;
        irradiance.sh[rgbOffset + L22] =  radiance.sh[rgbOffset + L22] * aHat2;
    }
    INDEX_SAFE(irradianceOut, probeIdx) = irradiance;
}

__kernel void ConvertToUnityFormat(
    //*** input ***
    INPUT_BUFFER(00, SphericalHarmonicsL2, irradianceIn),
    INPUT_VALUE(01, int, probeCount),
    //*** output ***
    OUTPUT_BUFFER(02, SphericalHarmonicsL2, irradianceOut)
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    const uint probeIdx = get_global_id(0);
    KERNEL_ASSERT(probeIdx < probeCount);
    if (probeIdx >= probeCount)
        return;

    float shY0Normalization = sqrt(1.0f / FLT_PI) / 2.0f;

    float shY1Normalization = sqrt(3.0f / FLT_PI) / 2.0f;

    float shY2_2Normalization = sqrt(15.0f / FLT_PI) / 2.0f;
    float shY2_1Normalization = shY2_2Normalization;
    float shY20Normalization = sqrt(5.0f / FLT_PI) / 4.0f;
    float shY21Normalization = shY2_2Normalization;
    float shY22Normalization = sqrt(15.0f / FLT_PI) / 4.0f;

    SphericalHarmonicsL2 irradiance = INDEX_SAFE(irradianceIn, probeIdx);
    SphericalHarmonicsL2 output;
    for (int rgb = 0; rgb < 3; ++rgb)
    {
        const int rgbOffset = rgb * kSphericalHarmonicsL2_CoeffCount;

        // See documentation IProbePostProcessor.ConvertToUnityFormat for an explanation of the steps below.

        // L0
        output.sh[rgbOffset + L00] = irradiance.sh[rgbOffset + L00];
        output.sh[rgbOffset + L00] *= shY0Normalization; // 1)
        output.sh[rgbOffset + L00] /= FLT_PI; // 2)

        // L1
        output.sh[rgbOffset + L1_1] = irradiance.sh[rgbOffset + L10]; // 3)
        output.sh[rgbOffset + L1_1] *= shY1Normalization; // 1)
        output.sh[rgbOffset + L1_1] /= FLT_PI; // 3 )

        output.sh[rgbOffset + L10] = irradiance.sh[rgbOffset + L11]; // 3)
        output.sh[rgbOffset + L10] *= shY1Normalization; // 1)
        output.sh[rgbOffset + L10] /= FLT_PI; // 2)

        output.sh[rgbOffset + L11] = irradiance.sh[rgbOffset + L1_1]; // 3)
        output.sh[rgbOffset + L11] *= shY1Normalization; // 1)
        output.sh[rgbOffset + L11] /= FLT_PI; // 2)

        // L2
        output.sh[rgbOffset + L2_2] = irradiance.sh[rgbOffset + L2_2];
        output.sh[rgbOffset + L2_2] *= shY2_2Normalization; // 1)
        output.sh[rgbOffset + L2_2] /= FLT_PI; // 2)

        output.sh[rgbOffset + L2_1] = irradiance.sh[rgbOffset + L2_1];
        output.sh[rgbOffset + L2_1] *= shY2_1Normalization; // 1)
        output.sh[rgbOffset + L2_1] /= FLT_PI; // 2)

        output.sh[rgbOffset + L20] = irradiance.sh[rgbOffset + L20];
        output.sh[rgbOffset + L20] *= shY20Normalization; // 1)
        output.sh[rgbOffset + L20] /= FLT_PI; // 2)

        output.sh[rgbOffset + L21] = irradiance.sh[rgbOffset + L21];
        output.sh[rgbOffset + L21] *= shY21Normalization; // 1)
        output.sh[rgbOffset + L21] /= FLT_PI; // 2)

        output.sh[rgbOffset + L22] = irradiance.sh[rgbOffset + L22];
        output.sh[rgbOffset + L22] *= shY22Normalization; // 1)
        output.sh[rgbOffset + L22] /= FLT_PI; // 2)
    }
    INDEX_SAFE(irradianceOut, probeIdx) = output;
}

__kernel void AddSphericalHarmonicsL2(
    //*** input ***
    INPUT_BUFFER( 00, SphericalHarmonicsL2, A),
    INPUT_BUFFER( 01, SphericalHarmonicsL2, B),
    INPUT_VALUE(  02, int, probeCount),
    //*** output ***
    OUTPUT_BUFFER(03, SphericalHarmonicsL2, Sum)
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    const uint probeIdx = get_global_id(0);
    KERNEL_ASSERT(probeIdx < probeCount);
    if (probeIdx >= probeCount)
        return;

    SphericalHarmonicsL2 a = INDEX_SAFE(A, probeIdx);
    SphericalHarmonicsL2 b = INDEX_SAFE(B, probeIdx);
    SphericalHarmonicsL2 sum;
    for (int i = 0; i < kSphericalHarmonicsL2_FloatCount; i++)
    {
        sum.sh[i] = a.sh[i] + b.sh[i];
    }
    INDEX_SAFE(Sum, probeIdx) = sum;
}

__kernel void ScaleSphericalHarmonicsL2(
    //*** input ***
    INPUT_BUFFER(00, SphericalHarmonicsL2, shIn),
    INPUT_VALUE(01, int, probeCount),
    INPUT_VALUE(02, float, scale),
    //*** output ***
    OUTPUT_BUFFER(03, SphericalHarmonicsL2, shOut)
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    const uint probeIdx = get_global_id(0);
    KERNEL_ASSERT(probeIdx < probeCount);
    if (probeIdx >= probeCount)
        return;

    SphericalHarmonicsL2 a = INDEX_SAFE(shIn, probeIdx);
    SphericalHarmonicsL2 scaled;
    for (int i = 0; i < kSphericalHarmonicsL2_FloatCount; i++)
    {
        scaled.sh[i] = a.sh[i] * scale;
    }
    INDEX_SAFE(shOut, probeIdx) = scaled;
}

__kernel void WindowSphericalHarmonicsL2(
    //*** input ***
    INPUT_BUFFER(00, SphericalHarmonicsL2, shIn),
    INPUT_VALUE(01, int, probeCount),
    //*** output ***
    OUTPUT_BUFFER(02, SphericalHarmonicsL2, shOut)
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    const uint probeIdx = get_global_id(0);
    KERNEL_ASSERT(probeIdx < probeCount);
    if (probeIdx >= probeCount)
        return;

    // Windowing constants from WindowDirectSH in SHDering.cpp
    float extraWindow[3] = { 1.0f, 0.922066f, 0.731864f };

    // Apply windowing: Essentially SHConv3 times the window constants
    SphericalHarmonicsL2 sh = INDEX_SAFE(shIn, probeIdx);
    for (int coefficientIndex = 0; coefficientIndex < kSphericalHarmonicsL2_CoeffCount; ++coefficientIndex)
    {
        float window;
        if (coefficientIndex == 0)
            window = extraWindow[0];
        else if (coefficientIndex < 4)
            window = extraWindow[1];
        else
            window = extraWindow[2];
        sh.sh[0 * kSphericalHarmonicsL2_CoeffCount + coefficientIndex] *= window;
        sh.sh[1 * kSphericalHarmonicsL2_CoeffCount + coefficientIndex] *= window;
        sh.sh[2 * kSphericalHarmonicsL2_CoeffCount + coefficientIndex] *= window;
    }
    INDEX_SAFE(shOut, probeIdx) = sh;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\OpenCL\kernels\postProcessProbes.cl---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\OpenCL\kernels\prepareEnvironmentRays.cl---------------
.
.
#include "environmentLighting.h"


__kernel void prepareDirectEnvironmentRays(
    // *** output *** //
    OUTPUT_BUFFER( 0, ray,                 lightRaysCompactedBuffer),
    OUTPUT_BUFFER( 1, uint,                lightRaysCountBuffer),
    OUTPUT_BUFFER( 2, uint,                totalRaysCastCountBuffer),
    // *** input *** //
    INPUT_VALUE(   3, int,                 lightmapSize),
    INPUT_VALUE(   4, int,                 envFlags),
    INPUT_VALUE(   5, int,                 numEnvironmentSamples),
    INPUT_BUFFER(  6, PackedNormalOctQuad, envDirectionsBuffer),
    INPUT_BUFFER(  7, float4,              positionsWSBuffer),
    INPUT_BUFFER(  8, float,               goldenSample_buffer),
    INPUT_BUFFER( 9, uint,                 sobol_buffer),
    INPUT_BUFFER( 10, SampleDescription,   sampleDescriptionsExpandedBuffer),
    INPUT_BUFFER( 11, uint,                sampleDescriptionsExpandedCountBuffer),
    INPUT_BUFFER( 12, uint,                instanceIdToLodInfoBuffer)
#   ifndef PROBES
    ,
    INPUT_VALUE(  13, uint,                currentTileIdx),
    INPUT_VALUE(  14, uint,                sqrtNumTiles),
    INPUT_BUFFER( 15, unsigned char,                 blueNoiseSampling_buffer),
    INPUT_BUFFER( 16, unsigned char,                 blueNoiseScrambling_buffer),
    INPUT_BUFFER( 17, unsigned char,                 blueNoiseRanking_buffer),
    INPUT_VALUE(  18, int,                 blueNoiseBufferOffset),
    INPUT_BUFFER( 19, PackedNormalOctQuad, interpNormalsWSBuffer),
    INPUT_BUFFER( 20, PackedNormalOctQuad, planeNormalsWSBuffer),
    INPUT_VALUE(  21, float,               pushOff),
    INPUT_VALUE(  22, int,                 superSamplingMultiplier)
#   endif
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    __local uint numRayPreparedSharedMem;
    if (get_local_id(0) == 0)
        numRayPreparedSharedMem = 0;
    barrier(CLK_LOCAL_MEM_FENCE);

    // Prepare ray in private memory
    ray r;
    Ray_SetInactive(&r);

    int expandedRayIdx = get_global_id(0), local_idx;
    const uint sampleDescriptionsExpandedCount = INDEX_SAFE(sampleDescriptionsExpandedCountBuffer, 0);
    if (expandedRayIdx < sampleDescriptionsExpandedCount)
    {
        const SampleDescription sampleDescription = INDEX_SAFE(sampleDescriptionsExpandedBuffer, expandedRayIdx);
#if DISALLOW_RAY_EXPANSION
        if (sampleDescription.texelIndex >= 0)
        {
#endif

#ifndef PROBES
        // Remap the global lightmap index to the local, super-sampled space of the tile
        const int localIdx = GetSuperSampledLocalIndex(sampleDescription.texelIndex, lightmapSize, sampleDescription.currentSampleCount, superSamplingMultiplier, currentTileIdx, sqrtNumTiles, sobol_buffer, goldenSample_buffer KERNEL_VALIDATOR_BUFFERS);
#else
        const int localIdx = sampleDescription.texelIndex;
#endif
        const float4 position = INDEX_SAFE(positionsWSBuffer, localIdx);

        AssertPositionIsOccupied(position KERNEL_VALIDATOR_BUFFERS);

        //Random numbers
        float3 rand;
        int texel_x = sampleDescription.texelIndex % lightmapSize;
        int texel_y = sampleDescription.texelIndex / lightmapSize;
#ifndef PROBES
#if PLM_USE_BLUE_NOISE_SAMPLING
        if (sampleDescription.currentSampleCount < PLM_BLUE_NOISE_MAX_SAMPLES)
        {
            rand.x = BlueNoiseSobolSample(sampleDescription.currentSampleCount, 0, texel_x, texel_y, blueNoiseBufferOffset, blueNoiseSampling_buffer, blueNoiseScrambling_buffer, blueNoiseRanking_buffer KERNEL_VALIDATOR_BUFFERS);
            rand.y = BlueNoiseSobolSample(sampleDescription.currentSampleCount, 1, texel_x, texel_y, blueNoiseBufferOffset, blueNoiseSampling_buffer, blueNoiseScrambling_buffer, blueNoiseRanking_buffer KERNEL_VALIDATOR_BUFFERS);
            rand.z = BlueNoiseSobolSample(sampleDescription.currentSampleCount, 2, texel_x, texel_y, blueNoiseBufferOffset, blueNoiseSampling_buffer, blueNoiseScrambling_buffer, blueNoiseRanking_buffer KERNEL_VALIDATOR_BUFFERS);
        }
        else
#endif
#endif
        {
            rand.x = SobolSample(sampleDescription.currentSampleCount, 0, sobol_buffer KERNEL_VALIDATOR_BUFFERS);
            rand.y = SobolSample(sampleDescription.currentSampleCount, 1, sobol_buffer KERNEL_VALIDATOR_BUFFERS);
            rand.z = SobolSample(sampleDescription.currentSampleCount, 2, sobol_buffer KERNEL_VALIDATOR_BUFFERS);
            rand = ApplyCranleyPattersonRotation3D(rand, texel_x, texel_y, lightmapSize, 0, goldenSample_buffer KERNEL_VALIDATOR_BUFFERS);
        }

#ifdef PROBES
        float3 P = position.xyz;
        float4 D;
        if (UseEnvironmentImportanceSampling(envFlags))
            D = GenerateVolumeEnvironmentRayIS(numEnvironmentSamples, rand, envDirectionsBuffer KERNEL_VALIDATOR_BUFFERS);
        else
            D = GenerateVolumeEnvironmentRay(rand.xy);
        const int packedLODInfo = PackLODInfo(NO_LOD_MASK, NO_LOD_GROUP);
#else
        float3 interpNormal = DecodeNormal(INDEX_SAFE(interpNormalsWSBuffer, localIdx));
        float3 planeNormal = DecodeNormal(INDEX_SAFE(planeNormalsWSBuffer, localIdx));

        float4 D;
        if (UseEnvironmentImportanceSampling(envFlags))
            D = GenerateSurfaceEnvironmentRayIS(numEnvironmentSamples, interpNormal, planeNormal, rand, envDirectionsBuffer KERNEL_VALIDATOR_BUFFERS);
        else
            D = GenerateSurfaceEnvironmentRay(interpNormal, planeNormal, rand.xy);

        float3 P = position.xyz + planeNormal * pushOff;
        const int instanceId = (int)(floor(position.w));
        const int packedLODInfo = INDEX_SAFE(instanceIdToLodInfoBuffer, instanceId);
#endif
        if (D.w != 0.0f)
        {
            Ray_Init(&r, P, D.xyz, DEFAULT_RAY_LENGTH, D.w, packedLODInfo);

            // Set the index so we can map to the originating texel/probe
            Ray_SetSourceIndex(&r, sampleDescription.texelIndex);
        }
#if DISALLOW_RAY_EXPANSION
        }
#endif
    }

    // Threads synchronization for compaction
    if (Ray_IsActive_Private(&r))
    {
        local_idx = atomic_inc(&numRayPreparedSharedMem);
    }
    barrier(CLK_LOCAL_MEM_FENCE);
    if (get_local_id(0) == 0)
    {
        atomic_add(GET_PTR_SAFE(totalRaysCastCountBuffer, 0), numRayPreparedSharedMem);
        int numRayToAdd = numRayPreparedSharedMem;
#if DISALLOW_LIGHT_RAYS_COMPACTION
        numRayToAdd = get_local_size(0);
#endif
        numRayPreparedSharedMem = atomic_add(GET_PTR_SAFE(lightRaysCountBuffer, 0), numRayToAdd);
    }
    barrier(CLK_LOCAL_MEM_FENCE);

    int compactedIndex = numRayPreparedSharedMem + local_idx;
#if DISALLOW_LIGHT_RAYS_COMPACTION
    compactedIndex = expandedRayIdx;
#else
    // Write the ray out to memory
    if (Ray_IsActive_Private(&r))
#endif
    {
        Ray_SetSampleDescriptionIndex(&r, expandedRayIdx);
        INDEX_SAFE(lightRaysCompactedBuffer, compactedIndex) = r;
    }
}

__kernel void prepareIndirectEnvironmentRays(
    //*** output ***
    OUTPUT_BUFFER( 0, ray,                 lightRaysCompactedBuffer),
    OUTPUT_BUFFER( 1, uint,                lightRaysCountBuffer),
    OUTPUT_BUFFER( 2, uint,                totalRaysCastCountBuffer),
    OUTPUT_BUFFER( 3, uint,                lightRayIndexToPathRayIndexCompactedBuffer),
    //*** input ***
    INPUT_BUFFER(  4, ray,                 pathRaysCompactedBuffer_0),
    INPUT_BUFFER(  5, uint,                activePathCountBuffer_0),
    INPUT_BUFFER(  6, Intersection,        pathIntersectionsCompactedBuffer),
    INPUT_BUFFER(  7, PackedNormalOctQuad, pathLastPlaneNormalCompactedBuffer),
    INPUT_BUFFER(  8, unsigned char,       pathLastNormalFacingTheRayCompactedBuffer),
    INPUT_BUFFER(  9, PackedNormalOctQuad, pathLastInterpNormalCompactedBuffer),
    INPUT_BUFFER( 10, float,               goldenSample_buffer),
    INPUT_BUFFER( 11, uint,                sobol_buffer),
    INPUT_BUFFER( 12, PackedNormalOctQuad, envDirectionsBuffer),
    INPUT_BUFFER( 13, SampleDescription,   sampleDescriptionsExpandedBuffer),
    INPUT_VALUE(  14, int,                 envFlags),
    INPUT_VALUE(  15, int,                 numEnvironmentSamples),
    INPUT_VALUE(  16, int,                 lightmapSize),
    INPUT_VALUE(  17, int,                 bounce),
    INPUT_VALUE(  18, float,               pushOff),
    INPUT_VALUE(  19, int,                 superSamplingMultiplier),
    INPUT_BUFFER( 20, unsigned char,                 blueNoiseSampling_buffer),
    INPUT_BUFFER( 21, unsigned char,                 blueNoiseScrambling_buffer),
    INPUT_BUFFER( 22, unsigned char,                 blueNoiseRanking_buffer),
    INPUT_VALUE(  23, int,                 blueNoiseBufferOffset)
    KERNEL_VALIDATOR_BUFFERS_DEF
)
{
    // Initialize local memory
    __local uint numRayPreparedSharedMem;
    if (get_local_id(0) == 0)
        numRayPreparedSharedMem = 0;
    barrier(CLK_LOCAL_MEM_FENCE);

    // Prepare ray in private memory
    ray r;
    Ray_SetInactive(&r);

    // Should we prepare a light ray?
    int compactedPathRayIdx = get_global_id(0), local_idx;
    bool shouldPrepareNewRay = compactedPathRayIdx < INDEX_SAFE(activePathCountBuffer_0, 0);

#if DISALLOW_PATH_RAYS_COMPACTION
    if (Ray_IsInactive(GET_PTR_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIdx)))
    {
        shouldPrepareNewRay = false;
    }
#endif

    if (shouldPrepareNewRay)
    {
        KERNEL_ASSERT(Ray_IsActive(GET_PTR_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIdx)));

        // We did not hit anything, no light ray.
        const bool pathRayHitSomething = INDEX_SAFE(pathIntersectionsCompactedBuffer, compactedPathRayIdx).shapeid != MISS_MARKER;
        shouldPrepareNewRay &= pathRayHitSomething;

        // We hit an invalid triangle (from the back, no double sided GI), no light ray.
        const bool isNormalFacingTheRay = INDEX_SAFE(pathLastNormalFacingTheRayCompactedBuffer, compactedPathRayIdx);
        shouldPrepareNewRay &= isNormalFacingTheRay;
    }

    // Prepare the shadow ray
    if (shouldPrepareNewRay)
    {
        int sampleDescriptionIdx = Ray_GetSampleDescriptionIndex(GET_PTR_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIdx));
        const SampleDescription sampleDescription = INDEX_SAFE(sampleDescriptionsExpandedBuffer, sampleDescriptionIdx);

        // Get random numbers
        int baseDimension = bounce * PLM_MAX_NUM_SOBOL_DIMENSIONS_PER_BOUNCE;
        float3 sample3D;
        int texel_x = sampleDescription.texelIndex % lightmapSize;
        int texel_y = sampleDescription.texelIndex / lightmapSize;

#if PLM_USE_BLUE_NOISE_SAMPLING
        if(sampleDescription.currentSampleCount < PLM_BLUE_NOISE_MAX_SAMPLES)
        {
            sample3D.x = BlueNoiseSobolSample(sampleDescription.currentSampleCount, baseDimension + 0, texel_x, texel_y, blueNoiseBufferOffset, blueNoiseSampling_buffer, blueNoiseScrambling_buffer, blueNoiseRanking_buffer KERNEL_VALIDATOR_BUFFERS);
            sample3D.y = BlueNoiseSobolSample(sampleDescription.currentSampleCount, baseDimension + 1, texel_x, texel_y, blueNoiseBufferOffset, blueNoiseSampling_buffer, blueNoiseScrambling_buffer, blueNoiseRanking_buffer KERNEL_VALIDATOR_BUFFERS);
            sample3D.z = BlueNoiseSobolSample(sampleDescription.currentSampleCount, baseDimension + 2, texel_x, texel_y, blueNoiseBufferOffset, blueNoiseSampling_buffer, blueNoiseScrambling_buffer, blueNoiseRanking_buffer KERNEL_VALIDATOR_BUFFERS);
        }
        else
#endif
        {
            sample3D.x = SobolSample(sampleDescription.currentSampleCount, baseDimension + 0, sobol_buffer KERNEL_VALIDATOR_BUFFERS);
            sample3D.y = SobolSample(sampleDescription.currentSampleCount, baseDimension + 1, sobol_buffer KERNEL_VALIDATOR_BUFFERS);
            sample3D.z = SobolSample(sampleDescription.currentSampleCount, baseDimension + 2, sobol_buffer KERNEL_VALIDATOR_BUFFERS);
            sample3D = ApplyCranleyPattersonRotation3D(sample3D, texel_x, texel_y, lightmapSize, baseDimension, goldenSample_buffer KERNEL_VALIDATOR_BUFFERS);
        }

        float3 planeNormal  = DecodeNormal(INDEX_SAFE(pathLastPlaneNormalCompactedBuffer, compactedPathRayIdx));
        float3 interpNormal = DecodeNormal(INDEX_SAFE(pathLastInterpNormalCompactedBuffer, compactedPathRayIdx));

        float4 D;
        if (UseEnvironmentImportanceSampling(envFlags))
            D = GenerateSurfaceEnvironmentRayIS(numEnvironmentSamples, interpNormal, planeNormal, sample3D, envDirectionsBuffer KERNEL_VALIDATOR_BUFFERS);
        else
            D = GenerateSurfaceEnvironmentRay(interpNormal, planeNormal, sample3D.xy);

        // TODO(RadeonRays) gboisse: we're generating some NaN directions somehow, fix it!!
        if (D.w != 0.0f && !any(isnan(D)))
        {
            float  t  = INDEX_SAFE(pathIntersectionsCompactedBuffer, compactedPathRayIdx).uvwt.w;
            float3 P  = INDEX_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIdx).o.xyz + INDEX_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIdx).d.xyz * t;
                   P += planeNormal * pushOff;

            // propagate potentially fixed up lod param from the intersection
            const int instanceLodInfo = INDEX_SAFE(pathIntersectionsCompactedBuffer, compactedPathRayIdx).padding0;

            Ray_Init(&r, P, D.xyz, DEFAULT_RAY_LENGTH, D.w, instanceLodInfo);

            // Set the index so we can map to the originating texel/probe
            Ray_SetSourceIndex(&r, sampleDescription.texelIndex);
            Ray_SetSampleDescriptionIndex(&r, sampleDescriptionIdx);
        }
    }

    // Threads synchronization for compaction
    if (Ray_IsActive_Private(&r))
    {
        local_idx = atomic_inc(&numRayPreparedSharedMem);
    }
    barrier(CLK_LOCAL_MEM_FENCE);
    if (get_local_id(0) == 0)
    {
        atomic_add(GET_PTR_SAFE(totalRaysCastCountBuffer, 0), numRayPreparedSharedMem);
        int numRayToAdd = numRayPreparedSharedMem;
#if DISALLOW_LIGHT_RAYS_COMPACTION
        numRayToAdd = get_local_size(0);
#endif
        numRayPreparedSharedMem = atomic_add(GET_PTR_SAFE(lightRaysCountBuffer, 0), numRayToAdd);
    }
    barrier(CLK_LOCAL_MEM_FENCE);

    int compactedLightRayIndex = numRayPreparedSharedMem + local_idx;
#if DISALLOW_LIGHT_RAYS_COMPACTION
    compactedLightRayIndex = compactedPathRayIdx;
#else
    // Write the ray out to memory
    if (Ray_IsActive_Private(&r))
#endif
    {
        INDEX_SAFE(lightRaysCompactedBuffer, compactedLightRayIndex) = r;
        INDEX_SAFE(lightRayIndexToPathRayIndexCompactedBuffer, compactedLightRayIndex) = compactedPathRayIdx;
    }
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\OpenCL\kernels\prepareEnvironmentRays.cl---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\OpenCL\kernels\prepareLightRays.cl---------------
.
.
#include "commonCL.h"
#include "directLighting.h"

static int GetCellIndex(float3 position, float3 gridBias, float3 gridScale, int3 gridDims)
{
    const int3 cellPos = clamp(convert_int3(position * gridScale + gridBias), (int3)0, gridDims - 1);
    return cellPos.x + cellPos.y * gridDims.x + cellPos.z * gridDims.x * gridDims.y;
}

//Preparing shadowRays for direct lighting.
__kernel void prepareLightRays(
    //outputs
    OUTPUT_BUFFER(00, ray,                 lightRaysCompactedBuffer),
    OUTPUT_BUFFER(01, LightSample,         lightSamplesCompactedBuffer),
    OUTPUT_BUFFER(02, uint,                totalRaysCastCountBuffer),
    OUTPUT_BUFFER(03, uint,                lightRaysCountBuffer),
    //inputs
    INPUT_BUFFER( 04, float4,              positionsWSBuffer),
    INPUT_BUFFER( 05, int,                 directLightsIndexBuffer),
    INPUT_BUFFER( 06, LightBuffer,         directLightsBuffer),
    INPUT_BUFFER( 07, int,                 directLightsOffsetBuffer),
    INPUT_BUFFER( 08, int,                 directLightsCountPerCellBuffer),
    INPUT_VALUE(  09, float3,              lightGridBias),
    INPUT_VALUE(  10, float3,              lightGridScale),
    INPUT_VALUE(  11, int3,                lightGridDims),
    INPUT_VALUE(  12, int,                 lightmapSize),
    INPUT_BUFFER( 13, float,               goldenSample_buffer),
    INPUT_BUFFER( 14, uint,                sobol_buffer),
    INPUT_BUFFER( 15, uint,                sampleDescriptionsExpandedCountBuffer),
    INPUT_VALUE(  16, int,                 lightIndexInCell),
    INPUT_BUFFER( 17, SampleDescription,   sampleDescriptionsExpandedBuffer),
    INPUT_BUFFER( 18, uint,                instanceIdToLodInfoBuffer)
#ifndef PROBES
    ,
    INPUT_VALUE(  19, uint,                currentTileIdx),
    INPUT_VALUE(  20, uint,                sqrtNumTiles),
    INPUT_BUFFER( 21, unsigned char,                 blueNoiseSampling_buffer),
    INPUT_BUFFER( 22, unsigned char,                 blueNoiseScrambling_buffer),
    INPUT_BUFFER( 23, unsigned char,                 blueNoiseRanking_buffer),
    INPUT_VALUE(  24, int,                 blueNoiseBufferOffset),
    INPUT_BUFFER( 25, PackedNormalOctQuad, interpNormalsWSBuffer),
    INPUT_VALUE(  26, float,               pushOff),
    INPUT_VALUE(  27, int,                 superSamplingMultiplier)
#endif
    KERNEL_VALIDATOR_BUFFERS_DEF
)
{
    __local uint numRayPreparedSharedMem;
    if (get_local_id(0) == 0)
        numRayPreparedSharedMem = 0;
    barrier(CLK_LOCAL_MEM_FENCE);

    // Prepare ray in private memory
    ray r;
    Ray_SetInactive(&r);
    LightSample lightSample;
    lightSample.lightIdx = -1;

    int expandedRayIdx = get_global_id(0), local_idx;
    const uint sampleDescriptionsExpandedCount = INDEX_SAFE(sampleDescriptionsExpandedCountBuffer, 0);
    if (expandedRayIdx < sampleDescriptionsExpandedCount)
    {
        const SampleDescription sampleDescription = INDEX_SAFE(sampleDescriptionsExpandedBuffer, expandedRayIdx);
#if DISALLOW_RAY_EXPANSION
        if (sampleDescription.texelIndex >= 0)
        {
#endif

#ifndef PROBES
        // Remap the global lightmap index to the local, super-sampled space of the tile
        const int localIdx = GetSuperSampledLocalIndex(sampleDescription.texelIndex, lightmapSize, sampleDescription.currentSampleCount, superSamplingMultiplier, currentTileIdx, sqrtNumTiles, sobol_buffer, goldenSample_buffer KERNEL_VALIDATOR_BUFFERS);
#else
        const int localIdx = sampleDescription.texelIndex;
#endif

        float4 position = INDEX_SAFE(positionsWSBuffer, localIdx);
        AssertPositionIsOccupied(position KERNEL_VALIDATOR_BUFFERS);

        // Directional light pass
        // The direction light index is encoded in the lightIndexInCell in negative numbers offseted by 1
        // The offset of 1 allows us to always have a negative lightIndexInCell for the dir light pass, especially with dir_light_id == 0 which becomes -1
        // Before calling this kernel we then do:  lightIndexInCell = -directionalLightIndex - 1;
        if (lightIndexInCell < 0)
        {
            int directionalLightIndex = -lightIndexInCell - 1;

            lightSample.lightPdf = 1.0f;
            lightSample.lightIdx = directionalLightIndex;
        }
        else
        {
            const int cellIdx = GetCellIndex(position.xyz, lightGridBias, lightGridScale, lightGridDims);
            const int lightCountInCell = INDEX_SAFE(directLightsCountPerCellBuffer, cellIdx);

            // Lights in light grid pass
            // If we already did all the lights in the cell bail out
            if (lightIndexInCell < lightCountInCell)
            {
                // Select a light in a round robin fashion (no need for pdf)
                const int lightCellOffset = INDEX_SAFE(directLightsOffsetBuffer, cellIdx) + lightIndexInCell;
                lightSample.lightPdf = 1.0f;
                lightSample.lightIdx = INDEX_SAFE(directLightsIndexBuffer, lightCellOffset);
            }
        }

        if(lightSample.lightIdx >=0)
        {
            const LightBuffer light = INDEX_SAFE(directLightsBuffer, lightSample.lightIdx);

            // Get random numbers
            float2 sample2D;
            int texel_x = sampleDescription.texelIndex % lightmapSize;
            int texel_y = sampleDescription.texelIndex / lightmapSize;

#ifndef PROBES
#if PLM_USE_BLUE_NOISE_SAMPLING
            if(sampleDescription.currentSampleCount < PLM_BLUE_NOISE_MAX_SAMPLES)
            {
                sample2D.x = BlueNoiseSobolSample(sampleDescription.currentSampleCount, 0, texel_x, texel_y, blueNoiseBufferOffset, blueNoiseSampling_buffer, blueNoiseScrambling_buffer, blueNoiseRanking_buffer KERNEL_VALIDATOR_BUFFERS);
                sample2D.y = BlueNoiseSobolSample(sampleDescription.currentSampleCount, 1, texel_x, texel_y, blueNoiseBufferOffset, blueNoiseSampling_buffer, blueNoiseScrambling_buffer, blueNoiseRanking_buffer KERNEL_VALIDATOR_BUFFERS);
            }
            else
#endif
#endif
            {
                sample2D.x = SobolSample(sampleDescription.currentSampleCount, 0, sobol_buffer KERNEL_VALIDATOR_BUFFERS);
                sample2D.y = SobolSample(sampleDescription.currentSampleCount, 1, sobol_buffer KERNEL_VALIDATOR_BUFFERS);
                sample2D = ApplyCranleyPattersonRotation2D(sample2D, texel_x, texel_y, lightmapSize, 0, goldenSample_buffer KERNEL_VALIDATOR_BUFFERS);
            }

            // Generate the shadow ray. This might be an inactive ray (in case of back facing surfaces or out of cone angle for spots).
#ifdef PROBES
            float3 notUsed3 = (float3)(0, 0, 0);
            const int packedNoLODInfo = PackLODInfo(NO_LOD_MASK, NO_LOD_GROUP);
            PrepareShadowRay(light, sample2D, position.xyz, notUsed3, 0, false, &r, packedNoLODInfo);
#else
            const int instanceId = (int)(floor(position.w));
            const int instanceLodInfo = INDEX_SAFE(instanceIdToLodInfoBuffer, instanceId);
            float3 normal = DecodeNormal(INDEX_SAFE(interpNormalsWSBuffer, localIdx));
            PrepareShadowRay(light, sample2D, position.xyz, normal, pushOff, false, &r, instanceLodInfo);
#endif
            // Set the index so we can map to the originating texel/probe
            Ray_SetSourceIndex(&r, sampleDescription.texelIndex);
        }
#if DISALLOW_RAY_EXPANSION
        }
#endif
    }

    // Threads synchronization for compaction
    if (Ray_IsActive_Private(&r))
    {
        local_idx = atomic_inc(&numRayPreparedSharedMem);
    }
    barrier(CLK_LOCAL_MEM_FENCE);
    if (get_local_id(0) == 0)
    {
        atomic_add(GET_PTR_SAFE(totalRaysCastCountBuffer, 0), numRayPreparedSharedMem);
        int numRayToAdd = numRayPreparedSharedMem;
#if DISALLOW_LIGHT_RAYS_COMPACTION
        numRayToAdd = get_local_size(0);
#endif
        numRayPreparedSharedMem = atomic_add(GET_PTR_SAFE(lightRaysCountBuffer, 0), numRayToAdd);
    }
    barrier(CLK_LOCAL_MEM_FENCE);

    int compactedIndex = numRayPreparedSharedMem + local_idx;
#if DISALLOW_LIGHT_RAYS_COMPACTION
    compactedIndex = expandedRayIdx;
#else
    // Write the ray out to memory
    if (Ray_IsActive_Private(&r))
#endif
    {
        INDEX_SAFE(lightSamplesCompactedBuffer, compactedIndex) = lightSample;
        Ray_SetSampleDescriptionIndex(&r, expandedRayIdx);
        INDEX_SAFE(lightRaysCompactedBuffer, compactedIndex) = r;
    }
}

//Preparing shadowRays for indirect lighting.
__kernel void prepareLightRaysFromBounce(
    INPUT_BUFFER(00, LightBuffer, indirectLightsBuffer),
    INPUT_BUFFER(01, int, indirectLightsOffsetBuffer),
    INPUT_BUFFER(02, int, indirectLightsIndexBuffer),
    INPUT_BUFFER(03, int, indirectLightsDistributionBuffer),
    INPUT_BUFFER(04, int, indirectLightDistributionOffsetBuffer),
    INPUT_BUFFER(05, PowerSamplingStat, usePowerSamplingBuffer),
    INPUT_VALUE(06, float3, lightGridBias),
    INPUT_VALUE(07, float3, lightGridScale),
    INPUT_VALUE(08, int3, lightGridDims),
    INPUT_BUFFER(09, ray, pathRaysCompactedBuffer_0),
    INPUT_BUFFER(10, Intersection, pathIntersectionsCompactedBuffer),
    INPUT_BUFFER(11, PackedNormalOctQuad, pathLastInterpNormalCompactedBuffer),
    INPUT_BUFFER(12, unsigned char, pathLastNormalFacingTheRayCompactedBuffer),
    INPUT_VALUE(13, int, lightmapSize),
    INPUT_VALUE(14, int, bounce),
    INPUT_BUFFER(15, float, goldenSample_buffer),
    INPUT_BUFFER(16, uint, sobol_buffer),
    INPUT_VALUE(17, float, pushOff),
    INPUT_BUFFER(18, uint, activePathCountBuffer_0),
    INPUT_BUFFER(19, SampleDescription, sampleDescriptionsExpandedBuffer),
    OUTPUT_BUFFER(20, ray, lightRaysCompactedBuffer),
    OUTPUT_BUFFER(21, LightSample, lightSamplesCompactedBuffer),
    OUTPUT_BUFFER(22, uint, lightRayIndexToPathRayIndexCompactedBuffer),
    OUTPUT_BUFFER(23, uint, totalRaysCastCountBuffer),
    OUTPUT_BUFFER(24, uint, lightRaysCountBuffer),
    INPUT_BUFFER( 25, unsigned char, blueNoiseSampling_buffer),
    INPUT_BUFFER( 26, unsigned char, blueNoiseScrambling_buffer),
    INPUT_BUFFER( 27, unsigned char, blueNoiseRanking_buffer),
    INPUT_VALUE(  28, int, blueNoiseBufferOffset),
    INPUT_VALUE(  29, int, directionalLightIndex),
    INPUT_VALUE(  30, uint, currentTileIdx),
    INPUT_VALUE(  31, uint, sqrtNumTiles)
    KERNEL_VALIDATOR_BUFFERS_DEF
)
{
    // Initialize local memory
    __local uint numRayPreparedSharedMem;
    if (get_local_id(0) == 0)
        numRayPreparedSharedMem = 0;
    barrier(CLK_LOCAL_MEM_FENCE);

    // Prepare ray in private memory
    ray r;
    Ray_SetInactive(&r);
    LightSample lightSample;

    // Should we prepare a light ray?
    int compactedPathRayIdx = get_global_id(0), local_idx;
    bool shouldPrepareNewRay = compactedPathRayIdx < INDEX_SAFE(activePathCountBuffer_0, 0);

#if DISALLOW_PATH_RAYS_COMPACTION
    if (Ray_IsInactive(GET_PTR_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIdx)))
    {
        shouldPrepareNewRay = false;
    }
#endif

    if (shouldPrepareNewRay)
    {
        KERNEL_ASSERT(Ray_IsActive(GET_PTR_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIdx)));

        // We did not hit anything, no light ray.
        const bool pathRayHitSomething = INDEX_SAFE(pathIntersectionsCompactedBuffer, compactedPathRayIdx).shapeid != MISS_MARKER;
        shouldPrepareNewRay &= pathRayHitSomething;

        // We hit an invalid triangle (from the back, no double sided GI), no light ray.
        const bool isNormalFacingTheRay = INDEX_SAFE(pathLastNormalFacingTheRayCompactedBuffer, compactedPathRayIdx);
        shouldPrepareNewRay &= isNormalFacingTheRay;
    }

    // Prepare the shadow ray
    if (shouldPrepareNewRay)
    {
        const float3 surfaceNormal = DecodeNormal(INDEX_SAFE(pathLastInterpNormalCompactedBuffer, compactedPathRayIdx));
        const float t = INDEX_SAFE(pathIntersectionsCompactedBuffer, compactedPathRayIdx).uvwt.w;
        const float3 surfacePosition = INDEX_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIdx).o.xyz + t * INDEX_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIdx).d.xyz;

        // Retrieve the light distribution at the shading site
        const int cellIdx = GetCellIndex(surfacePosition, lightGridBias, lightGridScale, lightGridDims);
        __global const int *const restrict lightDistributionPtr = GET_PTR_SAFE(indirectLightsDistributionBuffer, INDEX_SAFE(indirectLightDistributionOffsetBuffer, cellIdx));
        const int lightDistribution = *lightDistributionPtr; // safe to dereference, as GET_PTR_SAFE above does the validation

        // If there is no light in the cell, or not doing the directional light pass bail out
        if (lightDistribution || directionalLightIndex>=0)
        {
            // propagate potentially fixed up lod param from the intersection
            const int instanceLodInfo = INDEX_SAFE(pathIntersectionsCompactedBuffer, compactedPathRayIdx).padding0;

            int expandedRayIdx = Ray_GetSampleDescriptionIndex(GET_PTR_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIdx));
            const SampleDescription sampleDescription = INDEX_SAFE(sampleDescriptionsExpandedBuffer, expandedRayIdx);

            // Get random numbers
            int baseDimension = bounce * PLM_MAX_NUM_SOBOL_DIMENSIONS_PER_BOUNCE;
            float3 sample3D;
            int texel_x = sampleDescription.texelIndex % lightmapSize;
            int texel_y = sampleDescription.texelIndex / lightmapSize;

#if PLM_USE_BLUE_NOISE_SAMPLING
            if(sampleDescription.currentSampleCount < PLM_BLUE_NOISE_MAX_SAMPLES)
            {
                sample3D.x = BlueNoiseSobolSample(sampleDescription.currentSampleCount, baseDimension + 0, texel_x, texel_y, blueNoiseBufferOffset, blueNoiseSampling_buffer, blueNoiseScrambling_buffer, blueNoiseRanking_buffer KERNEL_VALIDATOR_BUFFERS);
                sample3D.y = BlueNoiseSobolSample(sampleDescription.currentSampleCount, baseDimension + 1, texel_x, texel_y, blueNoiseBufferOffset, blueNoiseSampling_buffer, blueNoiseScrambling_buffer, blueNoiseRanking_buffer KERNEL_VALIDATOR_BUFFERS);
                sample3D.z = BlueNoiseSobolSample(sampleDescription.currentSampleCount, baseDimension + 2, texel_x, texel_y, blueNoiseBufferOffset, blueNoiseSampling_buffer, blueNoiseScrambling_buffer, blueNoiseRanking_buffer KERNEL_VALIDATOR_BUFFERS);
            }
            else
#endif
            {
                sample3D.x = SobolSample(sampleDescription.currentSampleCount, baseDimension + 0, sobol_buffer KERNEL_VALIDATOR_BUFFERS);
                sample3D.y = SobolSample(sampleDescription.currentSampleCount, baseDimension + 1, sobol_buffer KERNEL_VALIDATOR_BUFFERS);
                sample3D.z = SobolSample(sampleDescription.currentSampleCount, baseDimension + 2, sobol_buffer KERNEL_VALIDATOR_BUFFERS);
                sample3D = ApplyCranleyPattersonRotation3D(sample3D, texel_x, texel_y, lightmapSize, baseDimension, goldenSample_buffer KERNEL_VALIDATOR_BUFFERS);
            }


            // Directional light pass
            if (directionalLightIndex >= 0)
            {
                lightSample.lightIdx = directionalLightIndex;
                lightSample.lightPdf = 1.0f;
            }
            else
            {
                // Select a light
                const int powerSamplingIndex = GetLocalIndex(sampleDescription.texelIndex, lightmapSize, currentTileIdx, sqrtNumTiles);
                if (usePowerSampling(powerSamplingIndex, usePowerSamplingBuffer KERNEL_VALIDATOR_BUFFERS))
                {
                    float selectionPdf;
                    const int lightCellOffset = INDEX_SAFE(indirectLightsOffsetBuffer, cellIdx) + Distribution1D_SampleDiscrete(sample3D.z, lightDistributionPtr, &selectionPdf);
                    lightSample.lightIdx = INDEX_SAFE(indirectLightsIndexBuffer, lightCellOffset);
                    lightSample.lightPdf = selectionPdf;
                }
                else
                {
                    const int offset = min(lightDistribution - 1, (int)(sample3D.z * (float)lightDistribution));
                    const int lightCellOffset = INDEX_SAFE(indirectLightsOffsetBuffer, cellIdx) + offset;
                    lightSample.lightIdx = INDEX_SAFE(indirectLightsIndexBuffer, lightCellOffset);
                    lightSample.lightPdf = 1.0f / lightDistribution;
                }
            }

            // Generate the shadow ray
            const LightBuffer light = INDEX_SAFE(indirectLightsBuffer, lightSample.lightIdx);
            PrepareShadowRay(light, sample3D.xy, surfacePosition, surfaceNormal, pushOff, false, &r, instanceLodInfo);

            // Set the index so we can map to the originating texel/probe
            Ray_SetSourceIndex(&r, sampleDescription.texelIndex);
            Ray_SetSampleDescriptionIndex(&r, expandedRayIdx);
        }
    }

    // Threads synchronization for compaction
    if (Ray_IsActive_Private(&r))
    {
        local_idx = atomic_inc(&numRayPreparedSharedMem);
    }
    barrier(CLK_LOCAL_MEM_FENCE);
    if (get_local_id(0) == 0)
    {
        atomic_add(GET_PTR_SAFE(totalRaysCastCountBuffer, 0), numRayPreparedSharedMem);
        int numRayToAdd = numRayPreparedSharedMem;
#if DISALLOW_LIGHT_RAYS_COMPACTION
        numRayToAdd = get_local_size(0);
#endif
        numRayPreparedSharedMem = atomic_add(GET_PTR_SAFE(lightRaysCountBuffer, 0), numRayToAdd);
    }
    barrier(CLK_LOCAL_MEM_FENCE);

    int compactedLightRayIndex = numRayPreparedSharedMem + local_idx;
#if DISALLOW_LIGHT_RAYS_COMPACTION
    compactedLightRayIndex = compactedPathRayIdx;
#else
    // Write the ray out to memory
    if (Ray_IsActive_Private(&r))
#endif
    {
        INDEX_SAFE(lightSamplesCompactedBuffer, compactedLightRayIndex) = lightSample;
        INDEX_SAFE(lightRaysCompactedBuffer, compactedLightRayIndex) = r;
        INDEX_SAFE(lightRayIndexToPathRayIndexCompactedBuffer, compactedLightRayIndex) = compactedPathRayIdx;
    }
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\OpenCL\kernels\prepareLightRays.cl---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\OpenCL\kernels\preparePathRays.cl---------------
.
.
#include "commonCL.h"

__kernel void preparePathRays(
    OUTPUT_BUFFER(00, ray,               pathRaysCompactedBuffer_0),
    OUTPUT_BUFFER(01, uint,              activePathCountBuffer_0),
    OUTPUT_BUFFER(02, uint,              totalRaysCastCountBuffer),
    OUTPUT_BUFFER(03, float4,            originalRaysExpandedBuffer),
    INPUT_BUFFER( 04, float4,            positionsWSBuffer),
    INPUT_VALUE(  05, int,               lightmapSize),
    INPUT_VALUE(  06, int,               bounce),
    INPUT_BUFFER( 07, uint,              sobol_buffer),
    INPUT_BUFFER( 08, float,             goldenSample_buffer),
    INPUT_BUFFER( 09, SampleDescription, sampleDescriptionsExpandedBuffer),
    INPUT_BUFFER( 10, uint,              sampleDescriptionsExpandedCountBuffer),
    INPUT_BUFFER( 11, uint,              instanceIdToLodInfoBuffer)
#ifndef PROBES
    ,
    INPUT_VALUE(  12, uint,              currentTileIdx),
    INPUT_VALUE(  13, uint,              sqrtNumTiles),
    INPUT_BUFFER( 14, PackedNormalOctQuad, interpNormalsWSBuffer),
    INPUT_BUFFER( 15, PackedNormalOctQuad, planeNormalsWSBuffer),
    INPUT_VALUE(  16, float,               pushOff),
    INPUT_VALUE(  17, int,                 superSamplingMultiplier),
    INPUT_BUFFER( 18, unsigned char,                 blueNoiseSampling_buffer),
    INPUT_BUFFER( 19, unsigned char,                 blueNoiseScrambling_buffer),
    INPUT_BUFFER( 20, unsigned char,                 blueNoiseRanking_buffer),
    INPUT_VALUE(  21, int,                 blueNoiseBufferOffset)
#endif
    KERNEL_VALIDATOR_BUFFERS_DEF
)
{
    __local uint numRayPreparedSharedMem;
    if (get_local_id(0) == 0)
        numRayPreparedSharedMem = 0;
    barrier(CLK_LOCAL_MEM_FENCE);

    // Prepare ray in private memory
    ray r;
    Ray_SetInactive(&r);

    int expandedPathRayIdx = get_global_id(0), local_idx;
    const uint sampleDescriptionsExpandedCount = INDEX_SAFE(sampleDescriptionsExpandedCountBuffer, 0);
    if (expandedPathRayIdx < sampleDescriptionsExpandedCount)
    {
        const SampleDescription sampleDescription = INDEX_SAFE(sampleDescriptionsExpandedBuffer, expandedPathRayIdx);
#if DISALLOW_RAY_EXPANSION
        if (sampleDescription.texelIndex >= 0)
        {
#endif

#ifndef PROBES
        // Remap the global lightmap index to the local, super-sampled space of the tile
        const int localIdx = GetSuperSampledLocalIndex(sampleDescription.texelIndex, lightmapSize, sampleDescription.currentSampleCount, superSamplingMultiplier, currentTileIdx, sqrtNumTiles, sobol_buffer, goldenSample_buffer KERNEL_VALIDATOR_BUFFERS);
#else
        const int localIdx = sampleDescription.texelIndex;
#endif
        // Get random numbers
        float2 sample2D;

        //first bounce uses dimension 0 and 1
        int texel_x = sampleDescription.texelIndex % lightmapSize;
        int texel_y = sampleDescription.texelIndex / lightmapSize;

#ifndef PROBES
#if PLM_USE_BLUE_NOISE_SAMPLING
        if(sampleDescription.currentSampleCount < PLM_BLUE_NOISE_MAX_SAMPLES)
        {
            sample2D.x = BlueNoiseSobolSample(sampleDescription.currentSampleCount, 0, texel_x, texel_y, blueNoiseBufferOffset, blueNoiseSampling_buffer, blueNoiseScrambling_buffer, blueNoiseRanking_buffer KERNEL_VALIDATOR_BUFFERS);
            sample2D.y = BlueNoiseSobolSample(sampleDescription.currentSampleCount, 1, texel_x, texel_y, blueNoiseBufferOffset, blueNoiseSampling_buffer, blueNoiseScrambling_buffer, blueNoiseRanking_buffer KERNEL_VALIDATOR_BUFFERS);
        }
        else
#endif
#endif
        {
            sample2D.x = SobolSample(sampleDescription.currentSampleCount, 0, sobol_buffer KERNEL_VALIDATOR_BUFFERS);
            sample2D.y = SobolSample(sampleDescription.currentSampleCount, 1, sobol_buffer KERNEL_VALIDATOR_BUFFERS);
            sample2D = ApplyCranleyPattersonRotation2D(sample2D, texel_x, texel_y, lightmapSize, 0, goldenSample_buffer KERNEL_VALIDATOR_BUFFERS);
        }

        float4 position = INDEX_SAFE(positionsWSBuffer, localIdx);
        AssertPositionIsOccupied(position KERNEL_VALIDATOR_BUFFERS);

#ifdef PROBES
        float3 D = SphereSample(sample2D);
        const float3 P = position.xyz;
        const int instanceLodInfo = PackLODInfo(NO_LOD_MASK, NO_LOD_GROUP);
#else
        const float3 interpNormal = DecodeNormal(INDEX_SAFE(interpNormalsWSBuffer, localIdx));
        //Map to cosine weighted hemisphere directed toward normal
        float3 b1;
        float3 b2;
        CreateOrthoNormalBasis(interpNormal, &b1, &b2);
        float3 hamDir = HemisphereCosineSample(sample2D);
        float3 D = hamDir.x*b1 + hamDir.y*b2 + hamDir.z*interpNormal;

        const float3 planeNormal = DecodeNormal(INDEX_SAFE(planeNormalsWSBuffer, localIdx));
        const float3 P = position.xyz + planeNormal * pushOff;

        // if plane normal is too different from interpolated normal, the hemisphere orientation will be wrong and the sample could be under the surface.
        float dotVal = dot(D, planeNormal);
        const int instanceId = (int)(floor(position.w));
        const int instanceLodInfo = INDEX_SAFE(instanceIdToLodInfoBuffer, instanceId);
        if (dotVal > 0.0f && !isnan(dotVal))
#endif
        {
            Ray_Init(&r, P, D, DEFAULT_RAY_LENGTH, 0.f, instanceLodInfo);

            // Set the index so we can map to the originating texel/probe
            Ray_SetSourceIndex(&r, sampleDescription.texelIndex);
        }
#if DISALLOW_RAY_EXPANSION
        }
#endif
    }

    // Threads synchronization for compaction
    if (Ray_IsActive_Private(&r))
    {
        local_idx = atomic_inc(&numRayPreparedSharedMem);
    }
    barrier(CLK_LOCAL_MEM_FENCE);
    if (get_local_id(0) == 0)
    {
        atomic_add(GET_PTR_SAFE(totalRaysCastCountBuffer, 0), numRayPreparedSharedMem);
        int numRayToAdd = numRayPreparedSharedMem;
#if DISALLOW_PATH_RAYS_COMPACTION
        numRayToAdd = get_local_size(0);
#endif
        numRayPreparedSharedMem = atomic_add(GET_PTR_SAFE(activePathCountBuffer_0, 0), numRayToAdd);
    }
    barrier(CLK_LOCAL_MEM_FENCE);

    int compactedPathRayIndex = numRayPreparedSharedMem + local_idx;
#if DISALLOW_PATH_RAYS_COMPACTION
    compactedPathRayIndex = expandedPathRayIdx;
#else
    // Write the ray out to memory
    if (Ray_IsActive_Private(&r))
#endif
    {
        INDEX_SAFE(originalRaysExpandedBuffer, expandedPathRayIdx) = (float4)(r.d.x, r.d.y, r.d.z, 0);
        Ray_SetSampleDescriptionIndex(&r, expandedPathRayIdx);
        INDEX_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIndex) = r;
    }
}

__kernel void preparePathRaysFromBounce(
    //*** input ***
    INPUT_BUFFER( 00, ray,                 pathRaysCompactedBuffer_0),
    INPUT_BUFFER( 01, Intersection,        pathIntersectionsCompactedBuffer),
    INPUT_BUFFER( 02, uint,                activePathCountBuffer_0),
    INPUT_BUFFER( 03, PackedNormalOctQuad, pathLastPlaneNormalCompactedBuffer),
    INPUT_BUFFER( 04, unsigned char,       pathLastNormalFacingTheRayCompactedBuffer),
    INPUT_BUFFER( 05, SampleDescription,   sampleDescriptionsExpandedBuffer),
    INPUT_VALUE(  06, int,                 lightmapSize),
    INPUT_VALUE(  07, int,                 bounce),
    INPUT_BUFFER( 08, uint,                sobol_buffer),
    INPUT_BUFFER( 09, float,               goldenSample_buffer),
    INPUT_VALUE(  10, float,               pushOff),
    INPUT_VALUE(  11, int,                 minBounces),
    //*** output ***
    OUTPUT_BUFFER(12, ray,                 pathRaysCompactedBuffer_1),
    OUTPUT_BUFFER(13, uint,                totalRaysCastCountBuffer),
    OUTPUT_BUFFER(14, uint,                activePathCountBuffer_1),
    //*** in/output ***
    OUTPUT_BUFFER(15, float4,              pathThroughputExpandedBuffer),
    INPUT_BUFFER( 16, unsigned char,       blueNoiseSampling_buffer),
    INPUT_BUFFER( 17, unsigned char,       blueNoiseScrambling_buffer),
    INPUT_BUFFER( 18, unsigned char,       blueNoiseRanking_buffer),
    INPUT_VALUE(  19, int,                 blueNoiseBufferOffset)
    KERNEL_VALIDATOR_BUFFERS_DEF
)
{
    __local uint numRayPreparedSharedMem;
    if (get_local_id(0) == 0)
        numRayPreparedSharedMem = 0;
    barrier(CLK_LOCAL_MEM_FENCE);

    ray r;// Prepare ray in private memory
    Ray_SetInactive(&r);

    uint compactedPathRayIdx = get_global_id(0), local_idx;
    bool shouldPrepareNewRay = compactedPathRayIdx < INDEX_SAFE(activePathCountBuffer_0, 0);

#if DISALLOW_PATH_RAYS_COMPACTION
    if (Ray_IsInactive(GET_PTR_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIdx)))
    {
        shouldPrepareNewRay = false;
    }
#endif

    SampleDescription sampleDescription;
    int expandedPathRayIdx;
    if (shouldPrepareNewRay)
    {
        KERNEL_ASSERT(Ray_IsActive(GET_PTR_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIdx)));

        expandedPathRayIdx = Ray_GetSampleDescriptionIndex(GET_PTR_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIdx));
        sampleDescription = INDEX_SAFE(sampleDescriptionsExpandedBuffer, expandedPathRayIdx);

        // We did not hit anything, no bounce path ray.
        const bool pathRayHitSomething = INDEX_SAFE(pathIntersectionsCompactedBuffer, compactedPathRayIdx).shapeid != MISS_MARKER;
        shouldPrepareNewRay &= pathRayHitSomething;

        // We hit an invalid triangle (from the back, no double sided GI), stop the path.
        const unsigned char isNormalFacingTheRay = INDEX_SAFE(pathLastNormalFacingTheRayCompactedBuffer, compactedPathRayIdx);
        shouldPrepareNewRay = (shouldPrepareNewRay && isNormalFacingTheRay);
    }

    // Russian roulette step can terminate the path
    float3 sample3D;
    if (shouldPrepareNewRay)
    {
        // Get random numbers
        int baseDimension = bounce * PLM_MAX_NUM_SOBOL_DIMENSIONS_PER_BOUNCE;
        int texel_x = sampleDescription.texelIndex % lightmapSize;
        int texel_y = sampleDescription.texelIndex / lightmapSize;

#if PLM_USE_BLUE_NOISE_SAMPLING
        if(sampleDescription.currentSampleCount < PLM_BLUE_NOISE_MAX_SAMPLES)
        {
            sample3D.x = BlueNoiseSobolSample(sampleDescription.currentSampleCount, baseDimension + 0, texel_x, texel_y, blueNoiseBufferOffset, blueNoiseSampling_buffer, blueNoiseScrambling_buffer, blueNoiseRanking_buffer KERNEL_VALIDATOR_BUFFERS);
            sample3D.y = BlueNoiseSobolSample(sampleDescription.currentSampleCount, baseDimension + 1, texel_x, texel_y, blueNoiseBufferOffset, blueNoiseSampling_buffer, blueNoiseScrambling_buffer, blueNoiseRanking_buffer KERNEL_VALIDATOR_BUFFERS);
            sample3D.z = BlueNoiseSobolSample(sampleDescription.currentSampleCount, baseDimension + 2, texel_x, texel_y, blueNoiseBufferOffset, blueNoiseSampling_buffer, blueNoiseScrambling_buffer, blueNoiseRanking_buffer KERNEL_VALIDATOR_BUFFERS);
        }
        else
#endif
        {
            sample3D.x = SobolSample(sampleDescription.currentSampleCount, baseDimension + 0, sobol_buffer KERNEL_VALIDATOR_BUFFERS);
            sample3D.y = SobolSample(sampleDescription.currentSampleCount, baseDimension + 1, sobol_buffer KERNEL_VALIDATOR_BUFFERS);
            sample3D.z = SobolSample(sampleDescription.currentSampleCount, baseDimension + 2, sobol_buffer KERNEL_VALIDATOR_BUFFERS);
            sample3D = ApplyCranleyPattersonRotation3D(sample3D, texel_x, texel_y, lightmapSize, baseDimension, goldenSample_buffer KERNEL_VALIDATOR_BUFFERS);
        }

        const bool doRussianRoulette = (bounce >= minBounces && shouldPrepareNewRay);
        if (doRussianRoulette)
        {
            const float4 pathThroughput = INDEX_SAFE(pathThroughputExpandedBuffer, expandedPathRayIdx);
            float p = max(max(pathThroughput.x, pathThroughput.y), pathThroughput.z);
            if (p < sample3D.z)
                shouldPrepareNewRay = false;
            else
                INDEX_SAFE(pathThroughputExpandedBuffer, expandedPathRayIdx).xyz *= (1 / p);
        }
    }

    if (shouldPrepareNewRay)
    {
        const float3 planeNormal = DecodeNormal(INDEX_SAFE(pathLastPlaneNormalCompactedBuffer, compactedPathRayIdx));
        const float t = INDEX_SAFE(pathIntersectionsCompactedBuffer, compactedPathRayIdx).uvwt.w;
        const float3 position = INDEX_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIdx).o.xyz + INDEX_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIdx).d.xyz * t;

        // Map to cosine weighted hemisphere directed toward plane normal
        float3 b1;
        float3 b2;
        CreateOrthoNormalBasis(planeNormal, &b1, &b2);
        float3 hamDir = HemisphereCosineSample(sample3D.xy);
        float3 D = hamDir.x*b1 + hamDir.y*b2 + hamDir.z*planeNormal;

        // TODO(RadeonRays) gboisse: we're generating some NaN directions somehow, fix it!!
        if (!any(isnan(D)))
        {
            const float3 P = position.xyz + planeNormal * pushOff;

            // propagate potentially fixed up lod param from the intersection
            const int instanceLodInfo = INDEX_SAFE(pathIntersectionsCompactedBuffer, compactedPathRayIdx).padding0;

            Ray_Init(&r, P, D, DEFAULT_RAY_LENGTH, 0.f, instanceLodInfo);
            Ray_SetSourceIndex(&r, sampleDescription.texelIndex);
            Ray_SetSampleDescriptionIndex(&r, expandedPathRayIdx);
        }
    }

    // Threads synchronization for compaction
    if (Ray_IsActive_Private(&r))
    {
        local_idx = atomic_inc(&numRayPreparedSharedMem);
    }
    barrier(CLK_LOCAL_MEM_FENCE);
    if (get_local_id(0) == 0)
    {
        atomic_add(GET_PTR_SAFE(totalRaysCastCountBuffer, 0), numRayPreparedSharedMem);
        int numRayToAdd = numRayPreparedSharedMem;
#if DISALLOW_PATH_RAYS_COMPACTION
        numRayToAdd = get_local_size(0);
#endif
        numRayPreparedSharedMem = atomic_add(GET_PTR_SAFE(activePathCountBuffer_1, 0), numRayToAdd);
    }
    barrier(CLK_LOCAL_MEM_FENCE);

    int compactedBouncedPathRayIndex = numRayPreparedSharedMem + local_idx;
#if DISALLOW_PATH_RAYS_COMPACTION
    compactedBouncedPathRayIndex = compactedPathRayIdx;
#else
    // Write the ray out to memory
    if (Ray_IsActive_Private(&r))
#endif
    {
        INDEX_SAFE(pathRaysCompactedBuffer_1, compactedBouncedPathRayIndex) = r;
    }
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\OpenCL\kernels\preparePathRays.cl---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\OpenCL\kernels\processBounce.cl---------------
.
.
#include "commonCL.h"
#include "colorSpace.h"
#include "directLighting.h"
#include "emissiveLighting.h"

__constant sampler_t linear2DSampler = CLK_NORMALIZED_COORDS_TRUE | CLK_ADDRESS_CLAMP_TO_EDGE | CLK_FILTER_LINEAR;

static void AccumulateLightFromBounce(float3 albedo, float3 directLightingAtHit, int expandedRayIdx, __global float3* lightingExpandedBuffer, int lightmapMode,
    __global float4* directionalExpandedBuffer, float3 direction KERNEL_VALIDATOR_BUFFERS_DEF)
{
    //Purely diffuse surface reflect the unabsorbed light evenly on the hemisphere.
    float3 energyFromHit = albedo * directLightingAtHit;
    INDEX_SAFE(lightingExpandedBuffer, expandedRayIdx).xyz += energyFromHit;

    //compute directionality from indirect
    if (lightmapMode == LIGHTMAPMODE_DIRECTIONAL)
    {
        float lum = Luminance(energyFromHit);

        INDEX_SAFE(directionalExpandedBuffer, expandedRayIdx).xyz += direction * lum;
        INDEX_SAFE(directionalExpandedBuffer, expandedRayIdx).w += lum;
    }
}

__kernel void processLightRaysFromBounce(
    INPUT_BUFFER( 00, LightBuffer,       indirectLightsBuffer),
    INPUT_BUFFER( 01, LightSample,       lightSamplesCompactedBuffer),
    OUTPUT_BUFFER(02, PowerSamplingStat, usePowerSamplingBuffer),
    INPUT_BUFFER( 03, float,             angularFalloffLUT_buffer),
    INPUT_BUFFER( 04, float,             distanceFalloffs_buffer),
    INPUT_BUFFER( 05, int,               cookiesBuffer),
    INPUT_BUFFER( 06, ray,               pathRaysCompactedBuffer_0),
    INPUT_BUFFER( 07, Intersection,      pathIntersectionsCompactedBuffer),
    INPUT_BUFFER( 08, ray,               lightRaysCompactedBuffer),
    INPUT_BUFFER( 09, float4,            lightOcclusionCompactedBuffer),
    INPUT_BUFFER( 10, float4,            pathThroughputExpandedBuffer),
    INPUT_BUFFER( 11, uint,              lightRayIndexToPathRayIndexCompactedBuffer),
    INPUT_BUFFER( 12, uint,              lightRaysCountBuffer),
    INPUT_BUFFER( 13, float4,            originalRaysExpandedBuffer),
    INPUT_VALUE(  14, int,               updatePowerSamplingBuffer),
#ifdef PROBES
    INPUT_VALUE(  15, int,               totalSampleCount),
    OUTPUT_BUFFER(16, float4,            probeSHExpandedBuffer)
#else
    INPUT_VALUE(  15, int,               lightmapMode),
    OUTPUT_BUFFER(16, float3,            lightingExpandedBuffer),
    OUTPUT_BUFFER(17, float4,            directionalExpandedBuffer),
    INPUT_VALUE(  18, int,               lightmapSize),
    INPUT_VALUE(  19, uint,              currentTileIdx),
    INPUT_VALUE(  20, uint,              sqrtNumTiles)
#endif
    KERNEL_VALIDATOR_BUFFERS_DEF
    )
{
    uint compactedLightRayIdx = get_global_id(0);
    int numLightHitCount = 0;
    int numLightRayCount = 0;

    bool shouldProcessRay = true;
#if DISALLOW_LIGHT_RAYS_COMPACTION
    if (Ray_IsInactive(GET_PTR_SAFE(lightRaysCompactedBuffer, compactedLightRayIdx)))
    {
        shouldProcessRay = false;
    }
#endif

    if (shouldProcessRay && compactedLightRayIdx < INDEX_SAFE(lightRaysCountBuffer, 0))
    {
        KERNEL_ASSERT(Ray_IsActive(GET_PTR_SAFE(lightRaysCompactedBuffer, compactedLightRayIdx)));
        const int compactedPathRayIdx = INDEX_SAFE(lightRayIndexToPathRayIndexCompactedBuffer, compactedLightRayIdx);
        KERNEL_ASSERT(Ray_IsActive(GET_PTR_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIdx)));
        const bool pathRayHitSomething = INDEX_SAFE(pathIntersectionsCompactedBuffer, compactedPathRayIdx).shapeid != MISS_MARKER;
        KERNEL_ASSERT(pathRayHitSomething);

        const int expandedRayIdx = Ray_GetSampleDescriptionIndex(GET_PTR_SAFE(lightRaysCompactedBuffer, compactedLightRayIdx));
        LightSample lightSample = INDEX_SAFE(lightSamplesCompactedBuffer, compactedLightRayIdx);
        LightBuffer light = INDEX_SAFE(indirectLightsBuffer, lightSample.lightIdx);

        bool useShadows = light.castShadow;
        const float4 occlusions4 = useShadows ? INDEX_SAFE(lightOcclusionCompactedBuffer, compactedLightRayIdx) : make_float4(1.0f, 1.0f, 1.0f, 1.0f);
        const bool  isLightOccludedFromBounce = occlusions4.w < TRANSMISSION_THRESHOLD;

        if (!isLightOccludedFromBounce)
        {
            const float t = INDEX_SAFE(pathIntersectionsCompactedBuffer, compactedPathRayIdx).uvwt.w;
            //We need to compute direct lighting on the fly
            float3 surfacePosition = INDEX_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIdx).o.xyz + INDEX_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIdx).d.xyz * t;
            float3 albedoAttenuation = INDEX_SAFE(pathThroughputExpandedBuffer, expandedRayIdx).xyz;

            float3 directLightingAtHit = occlusions4.xyz * ShadeLight(light, INDEX_SAFE(lightRaysCompactedBuffer, compactedLightRayIdx), surfacePosition, angularFalloffLUT_buffer, distanceFalloffs_buffer, cookiesBuffer KERNEL_VALIDATOR_BUFFERS) / lightSample.lightPdf;

            // The original direction from which the rays was shot from the probe position
            float4 originalRayDirection = INDEX_SAFE(originalRaysExpandedBuffer, expandedRayIdx);
#ifdef PROBES
            float3 L = albedoAttenuation * directLightingAtHit;
            float weight = 4.0 / totalSampleCount;
            accumulateSHExpanded(L, originalRayDirection, weight, probeSHExpandedBuffer, expandedRayIdx KERNEL_VALIDATOR_BUFFERS);
#else
            AccumulateLightFromBounce(albedoAttenuation, directLightingAtHit, expandedRayIdx, lightingExpandedBuffer, lightmapMode, directionalExpandedBuffer, originalRayDirection.xyz KERNEL_VALIDATOR_BUFFERS);
#endif
            numLightHitCount++;
        }
        numLightRayCount++;

        if (updatePowerSamplingBuffer)
        {
            const int globalTexelIndex = Ray_GetSourceIndex(GET_PTR_SAFE(lightRaysCompactedBuffer, compactedLightRayIdx));
#ifndef PROBES
            const int localTexelIndex = GetLocalIndex(globalTexelIndex, lightmapSize, currentTileIdx, sqrtNumTiles);
#else
            const int localTexelIndex = globalTexelIndex;
#endif
            if (usePowerSampling(localTexelIndex, usePowerSamplingBuffer KERNEL_VALIDATOR_BUFFERS))
            {
                atomic_add(&INDEX_SAFE(usePowerSamplingBuffer, localTexelIndex).LightHitCount, numLightHitCount);
                atomic_add(&INDEX_SAFE(usePowerSamplingBuffer, localTexelIndex).LightRayCount, numLightRayCount);
            }
        }
    }
}

__kernel void processEmissiveAndAOFromBounce(
    INPUT_BUFFER( 00, ray,                       pathRaysCompactedBuffer_0),
    INPUT_BUFFER( 01, Intersection,              pathIntersectionsCompactedBuffer),
    INPUT_BUFFER( 02, MaterialTextureProperties, instanceIdToEmissiveTextureProperties),
    INPUT_BUFFER( 03, float2,                    geometryUV1sBuffer),
    INPUT_BUFFER( 04, float4,                    dynarg_texture_buffer),
    INPUT_BUFFER( 05, MeshDataOffsets,           instanceIdToMeshDataOffsets),
    INPUT_BUFFER( 06, uint,                      geometryIndicesBuffer),
    INPUT_BUFFER( 07, float4,                    pathThroughputExpandedBuffer),
    INPUT_BUFFER( 08, uint,                      activePathCountBuffer_0),
    INPUT_BUFFER( 09, unsigned char,             pathLastNormalFacingTheRayCompactedBuffer),
    INPUT_BUFFER( 10, float4,                    originalRaysExpandedBuffer),
#ifdef PROBES
    INPUT_VALUE(  11, int,                       totalSampleCount),
    OUTPUT_BUFFER(12, float4,                    probeSHExpandedBuffer)
#else
    INPUT_VALUE(  11, int,                       lightmapMode),
    INPUT_VALUE(  12, float,                     aoMaxDistance),
    INPUT_VALUE(  13, int,                       bounce),
    OUTPUT_BUFFER(14, float3,                    lightingExpandedBuffer),
    OUTPUT_BUFFER(15, float4,                    directionalExpandedBuffer),
    OUTPUT_BUFFER(16, float4,                    shadowmaskAoValidityExpandedBuffer) //when gathering indirect .x will contain AO and .y will contain Validity
#endif
    KERNEL_VALIDATOR_BUFFERS_DEF
)
{
    uint compactedPathRayIdx = get_global_id(0);

    if (compactedPathRayIdx >= INDEX_SAFE(activePathCountBuffer_0, 0))
        return;

#if DISALLOW_PATH_RAYS_COMPACTION
    if (Ray_IsInactive(GET_PTR_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIdx)))
        return;
#endif

    KERNEL_ASSERT(Ray_IsActive(GET_PTR_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIdx)));
    const int expandedPathRayIdx = Ray_GetSampleDescriptionIndex(GET_PTR_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIdx));
    const bool  hit = INDEX_SAFE(pathIntersectionsCompactedBuffer, compactedPathRayIdx).shapeid != MISS_MARKER;

#ifndef PROBES
    const bool shouldAddOneToAOCount = (bounce == 0 && (!hit || INDEX_SAFE(pathIntersectionsCompactedBuffer, compactedPathRayIdx).uvwt.w > aoMaxDistance));
    if (shouldAddOneToAOCount)
    {
        INDEX_SAFE(shadowmaskAoValidityExpandedBuffer, expandedPathRayIdx).x += 1.0f;
    }
#endif

    if (hit)
    {
        AtlasInfo emissiveContribution = FetchEmissionFromRayIntersection(compactedPathRayIdx,
            pathIntersectionsCompactedBuffer,
            instanceIdToEmissiveTextureProperties,
            instanceIdToMeshDataOffsets,
            geometryUV1sBuffer,
            geometryIndicesBuffer,
            dynarg_texture_buffer
            KERNEL_VALIDATOR_BUFFERS
        );

        // If hit an invalid triangle (from the back, no double sided GI) we do not apply emissive.
        const unsigned char isNormalFacingTheRay = INDEX_SAFE(pathLastNormalFacingTheRayCompactedBuffer, compactedPathRayIdx);

        // The original direction from which the rays was shot
        float4 originalRayDirection = INDEX_SAFE(originalRaysExpandedBuffer, expandedPathRayIdx);

#ifdef PROBES
        float3 L = isNormalFacingTheRay * emissiveContribution.color.xyz * INDEX_SAFE(pathThroughputExpandedBuffer, expandedPathRayIdx).xyz;
        float weight = 4.0 / totalSampleCount;
        accumulateSHExpanded(L, originalRayDirection, weight, probeSHExpandedBuffer, expandedPathRayIdx KERNEL_VALIDATOR_BUFFERS);
#else
        float3 output = isNormalFacingTheRay * emissiveContribution.color.xyz * INDEX_SAFE(pathThroughputExpandedBuffer, expandedPathRayIdx).xyz;

        // Compute directionality from indirect
        if (lightmapMode == LIGHTMAPMODE_DIRECTIONAL)
        {
            float lum = Luminance(output);
            float4 directionality;
            directionality.xyz = originalRayDirection.xyz * lum;
            directionality.w = lum;
            INDEX_SAFE(directionalExpandedBuffer, expandedPathRayIdx) += directionality;
        }


        // Write Result
        INDEX_SAFE(lightingExpandedBuffer, expandedPathRayIdx).xyz += output.xyz;
#endif
    }
}

__kernel void advanceInPathAndAdjustPathProperties(
    INPUT_BUFFER( 00, ray,                       pathRaysCompactedBuffer_0),
    INPUT_BUFFER( 01, Intersection,              pathIntersectionsCompactedBuffer),
    INPUT_BUFFER( 02, MaterialTextureProperties, instanceIdToAlbedoTextureProperties),
    INPUT_BUFFER( 03, MeshDataOffsets,           instanceIdToMeshDataOffsets),
    INPUT_BUFFER( 04, float2,                    geometryUV1sBuffer),
    INPUT_BUFFER( 05, uint,                      geometryIndicesBuffer),
    INPUT_BUFFER( 06, uchar4,                    albedoTextures_buffer),
    INPUT_BUFFER( 07, uint,                      activePathCountBuffer_0),
    OUTPUT_BUFFER(08, float4,                    pathThroughputExpandedBuffer) //in & output
    KERNEL_VALIDATOR_BUFFERS_DEF
)
{
    uint compactedPathRayIdx = get_global_id(0);

    if (compactedPathRayIdx >= INDEX_SAFE(activePathCountBuffer_0, 0))
        return;

#if DISALLOW_PATH_RAYS_COMPACTION
    if (Ray_IsInactive(GET_PTR_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIdx)))
        return;
#endif

    KERNEL_ASSERT(Ray_IsActive(GET_PTR_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIdx)));
    const int expandedPathRayIdx = Ray_GetSampleDescriptionIndex(GET_PTR_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIdx));
    const bool  hit = INDEX_SAFE(pathIntersectionsCompactedBuffer, compactedPathRayIdx).shapeid != MISS_MARKER;
    if (!hit)
        return;

    AtlasInfo albedoAtHit = FetchAlbedoFromRayIntersection(compactedPathRayIdx,
        pathIntersectionsCompactedBuffer,
        instanceIdToAlbedoTextureProperties,
        instanceIdToMeshDataOffsets,
        geometryUV1sBuffer,
        geometryIndicesBuffer,
        albedoTextures_buffer
        KERNEL_VALIDATOR_BUFFERS);

    const float throughputAttenuation = dot(albedoAtHit.color.xyz, kAverageFactors);
    INDEX_SAFE(pathThroughputExpandedBuffer, expandedPathRayIdx) *= (float4)(albedoAtHit.color.x, albedoAtHit.color.y, albedoAtHit.color.z, throughputAttenuation);
}

__kernel void getNormalsFromLastBounceAndDoValidity(
    INPUT_BUFFER( 00, ray,                       pathRaysCompactedBuffer_0),              // rays from last to current hit
    INPUT_BUFFER( 01, Intersection,              pathIntersectionsCompactedBuffer),       // intersections from last to current hit
    INPUT_BUFFER( 02, MeshDataOffsets,           instanceIdToMeshDataOffsets),
    INPUT_BUFFER( 03, Matrix4x4,                 instanceIdToInvTransposedMatrices),
    INPUT_BUFFER( 04, Vector3f_storage,          geometryPositionsBuffer),
    INPUT_BUFFER( 05, PackedNormalOctQuad,       geometryNormalsBuffer),
    INPUT_BUFFER( 06, uint,                      geometryIndicesBuffer),
    INPUT_BUFFER( 07, uint,                      activePathCountBuffer_0),
    INPUT_BUFFER( 08, MaterialTextureProperties, instanceIdToTransmissionTextureProperties),
    INPUT_BUFFER( 09, float4,                    instanceIdToTransmissionTextureSTs),
    INPUT_BUFFER( 10, float2,                    geometryUV0sBuffer),
    INPUT_BUFFER( 11, uchar4,                    dynarg_texture_buffer),
    INPUT_VALUE(  12, int,                       primaryBufferMode),
    //output
    OUTPUT_BUFFER(13, PackedNormalOctQuad,       pathLastPlaneNormalCompactedBuffer),
    OUTPUT_BUFFER(14, PackedNormalOctQuad,       pathLastInterpNormalCompactedBuffer),
    OUTPUT_BUFFER(15, unsigned char,             pathLastNormalFacingTheRayCompactedBuffer),
    OUTPUT_BUFFER(16, float4,                    shadowmaskAoValidityExpandedBuffer) // Used to store validity in .y
#ifdef PROBES
    ,
    INPUT_VALUE(  17, int,                       lightmappingSourceType),
    OUTPUT_BUFFER(18, float,                     probeDepthOctahedronExpandedBuffer)
#endif
    KERNEL_VALIDATOR_BUFFERS_DEF
)
{
    uint compactedPathRayIdx = get_global_id(0);

    if (compactedPathRayIdx >= INDEX_SAFE(activePathCountBuffer_0, 0))
        return;

#if DISALLOW_PATH_RAYS_COMPACTION
    if (Ray_IsInactive(GET_PTR_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIdx)))
        return;
#endif

    KERNEL_ASSERT(Ray_IsActive(GET_PTR_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIdx)));
    if (INDEX_SAFE(pathIntersectionsCompactedBuffer, compactedPathRayIdx).shapeid == MISS_MARKER)
    {
        PackedNormalOctQuad zero;
        zero.x = 0xffffffff; // Will yield a decoded value of float3(0, 0, -1)
        INDEX_SAFE(pathLastPlaneNormalCompactedBuffer, compactedPathRayIdx) = zero;
        INDEX_SAFE(pathLastInterpNormalCompactedBuffer, compactedPathRayIdx) = zero;
        INDEX_SAFE(pathLastNormalFacingTheRayCompactedBuffer, compactedPathRayIdx) = 0;
        return;
    }

    const int instanceId = GetInstanceIdFromIntersection(GET_PTR_SAFE(pathIntersectionsCompactedBuffer, compactedPathRayIdx));
    float3 planeNormalWS;
    float3 interpVertexNormalWS;
    GetNormalsAtRayIntersection(compactedPathRayIdx,
        instanceId,
        pathIntersectionsCompactedBuffer,
        instanceIdToMeshDataOffsets,
        instanceIdToInvTransposedMatrices,
        geometryPositionsBuffer,
        geometryNormalsBuffer,
        geometryIndicesBuffer,
        &planeNormalWS,
        &interpVertexNormalWS
        KERNEL_VALIDATOR_BUFFERS);

    unsigned char isNormalFacingTheRay = 1;
    float3 rayDirection = INDEX_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIdx).d.xyz;
    const bool frontFacing = dot(planeNormalWS, rayDirection) <= 0.0f;
    const int expandedPathRayIdx = Ray_GetSampleDescriptionIndex(GET_PTR_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIdx));

    bool isRayValid = true;

    if (!frontFacing)
    {
        const MaterialTextureProperties matProperty = INDEX_SAFE(instanceIdToTransmissionTextureProperties, instanceId);
        const bool isDoubleSidedGI = GetMaterialProperty(matProperty, kMaterialInstanceProperties_DoubleSidedGI);
        planeNormalWS =        isDoubleSidedGI ? -planeNormalWS        : planeNormalWS;
        interpVertexNormalWS = isDoubleSidedGI ? -interpVertexNormalWS : interpVertexNormalWS;
        isNormalFacingTheRay = isDoubleSidedGI? 1 : 0;
        if (primaryBufferMode == PrimaryBufferMode_Generate && !isDoubleSidedGI)
        {
            const bool isTransparent = GetMaterialProperty(matProperty, kMaterialInstanceProperties_UseTransmission);
            if (!isTransparent)
            {
                // We use the shadowmaskAoValidityExpandedBuffer.y to store validity to avoid having an additional expanded buffer.
                INDEX_SAFE(shadowmaskAoValidityExpandedBuffer, expandedPathRayIdx).y = 1.0f;
                isRayValid = false;
            }
            else
            {
                // sample transmission texture
                const float2 barycentricCoord = INDEX_SAFE(pathIntersectionsCompactedBuffer, compactedPathRayIdx).uvwt.xy;
                const int primIndex = INDEX_SAFE(pathIntersectionsCompactedBuffer, compactedPathRayIdx).primid;
                const float2 geometryUVs = GetUVsAtPrimitiveIntersection(instanceId, primIndex, barycentricCoord, instanceIdToMeshDataOffsets, geometryUV0sBuffer, geometryIndicesBuffer KERNEL_VALIDATOR_BUFFERS);
                const float2 textureUVs = geometryUVs * INDEX_SAFE(instanceIdToTransmissionTextureSTs, instanceId).xy + INDEX_SAFE(instanceIdToTransmissionTextureSTs, instanceId).zw;
                const float4 transmission = FetchUChar4TextureFromMaterialAndUVs(dynarg_texture_buffer, textureUVs, matProperty, false, false KERNEL_VALIDATOR_BUFFERS);
                const float averageTransmission = dot(transmission.xyz, kAverageFactors);

                // We use the shadowmaskAoValidityExpandedBuffer.y to store validity to avoid having an additional expanded buffer.
                INDEX_SAFE(shadowmaskAoValidityExpandedBuffer, expandedPathRayIdx).y = 1.0f - averageTransmission;
                isRayValid = (averageTransmission > 0.99f);
            }
        }
    }

#ifdef PROBES
    if (probeDepthOctahedronExpandedBuffer)
    {
        if (primaryBufferMode == PrimaryBufferMode_Generate && isRayValid && lightmappingSourceType == kLightmappingSourceType_Probe)
        {
            float t = INDEX_SAFE(pathIntersectionsCompactedBuffer, compactedPathRayIdx).uvwt.w;
            float2 normalizedOctCoord = PackNormalOctQuadEncoded(normalize(rayDirection));
            int texel = GetOctQuadEncodedTexelFromPackedNormal(normalizedOctCoord, OCTAHEDRON_SIZE);
            float3 texelDirection = UnpackNormalOctQuadEncoded(normalizedOctCoord);
            float weight = max(0.0f, dot(texelDirection, rayDirection));
            INDEX_SAFE(probeDepthOctahedronExpandedBuffer, expandedPathRayIdx * OCTAHEDRON_TEXEL_COUNT + texel) += t * weight;
        }
    }
#endif

    // Store normals for various kernels to use later
    INDEX_SAFE(pathLastPlaneNormalCompactedBuffer, compactedPathRayIdx) = EncodeNormalToUint(planeNormalWS);
    INDEX_SAFE(pathLastInterpNormalCompactedBuffer, compactedPathRayIdx) = EncodeNormalToUint(interpVertexNormalWS);
    INDEX_SAFE(pathLastNormalFacingTheRayCompactedBuffer, compactedPathRayIdx) = isNormalFacingTheRay;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\OpenCL\kernels\processBounce.cl---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\OpenCL\kernels\processEnvironment.cl---------------
.
.
#include "environmentLighting.h"


__kernel void processDirectEnvironment(
    //*** input ***
    INPUT_BUFFER( 00, ray,                 lightRaysCompactedBuffer),
    INPUT_BUFFER( 01, uint,                lightRaysCountBuffer),
    INPUT_BUFFER( 02, float4,              lightOcclusionCompactedBuffer),
    INPUT_BUFFER( 03, float4,              env_mipped_cube_texels_buffer),
    INPUT_BUFFER( 04, int,                 env_mip_offsets_buffer),
    INPUT_VALUE(  05, Environment,         envData),
    INPUT_VALUE(  06, uint,                currentTileIdx),
    INPUT_VALUE(  07, uint,                sqrtNumTiles),
#ifdef PROBES
    INPUT_VALUE(  08, int,                 totalSampleCount),
    //*** output ***
    OUTPUT_BUFFER(09, float4,              probeSHExpandedBuffer)
#else
    INPUT_BUFFER( 08, PackedNormalOctQuad, interpNormalsWSBuffer),
    INPUT_VALUE(  09, int,                 lightmapMode),
    INPUT_VALUE(  10, int,                 superSamplingMultiplier),
    INPUT_VALUE(  11, int,                 lightmapSize),
    INPUT_BUFFER( 12, SampleDescription,   sampleDescriptionsExpandedBuffer),
    INPUT_BUFFER( 13, uint,                sobol_buffer),
    INPUT_BUFFER( 14, float,               goldenSample_buffer),
    //*** output ***
    OUTPUT_BUFFER(15, float4,              directionalExpandedBuffer),
    OUTPUT_BUFFER(16, float3,              lightingExpandedBuffer)

#endif
    KERNEL_VALIDATOR_BUFFERS_DEF)
{
    uint compactedRayIdx = get_global_id(0);
    if (compactedRayIdx >= INDEX_SAFE(lightRaysCountBuffer, 0))
        return;

#if DISALLOW_LIGHT_RAYS_COMPACTION
    if (Ray_IsInactive(GET_PTR_SAFE(lightRaysCompactedBuffer, compactedRayIdx)))
        return;
#endif

    KERNEL_ASSERT(Ray_IsActive(GET_PTR_SAFE(lightRaysCompactedBuffer, compactedRayIdx)));
    int sampleDescriptionIdx = Ray_GetSampleDescriptionIndex(GET_PTR_SAFE(lightRaysCompactedBuffer, compactedRayIdx));

    float4 occlusion = INDEX_SAFE(lightOcclusionCompactedBuffer, compactedRayIdx);
    bool   occluded  = occlusion.w < TRANSMISSION_THRESHOLD;

    if (!occluded)
    {
        // Environment intersection
        float4 dir          = INDEX_SAFE(lightRaysCompactedBuffer, compactedRayIdx).d;
        float3 color        = make_float3(0.0f, 0.0f, 0.0f);
#ifdef PROBES
        if (UseEnvironmentImportanceSampling(envData.flags))
            color = ProcessVolumeEnvironmentRayIS(dir, envData.envDim, envData.numMips, envData.envmapIntegral, env_mipped_cube_texels_buffer, env_mip_offsets_buffer KERNEL_VALIDATOR_BUFFERS);
        else
            color = ProcessVolumeEnvironmentRay(dir, envData.envDim, envData.numMips, env_mipped_cube_texels_buffer, env_mip_offsets_buffer KERNEL_VALIDATOR_BUFFERS);

        // accumulate environment lighting
        color *= occlusion.xyz;
        KERNEL_ASSERT(totalSampleCount > 0);
        float weight = 1.0f / totalSampleCount;
        accumulateSHExpanded(color.xyz, dir, weight, probeSHExpandedBuffer, sampleDescriptionIdx KERNEL_VALIDATOR_BUFFERS);
#else
        const SampleDescription sampleDescription = INDEX_SAFE(sampleDescriptionsExpandedBuffer, sampleDescriptionIdx);
        // Remap the global lightmap index to the local, super-sampled space of the tile
        const int localIdx = GetSuperSampledLocalIndex(sampleDescription.texelIndex, lightmapSize, sampleDescription.currentSampleCount, superSamplingMultiplier, currentTileIdx, sqrtNumTiles, sobol_buffer, goldenSample_buffer KERNEL_VALIDATOR_BUFFERS);

        float3 interpNormal = DecodeNormal(INDEX_SAFE(interpNormalsWSBuffer, localIdx));
        if (UseEnvironmentImportanceSampling(envData.flags))
            color = ProcessEnvironmentRayIS(dir, interpNormal, envData.envDim, envData.numMips, envData.envmapIntegral, env_mipped_cube_texels_buffer, env_mip_offsets_buffer KERNEL_VALIDATOR_BUFFERS);
        else
            color = ProcessEnvironmentRay(dir, envData.envDim, envData.numMips, env_mipped_cube_texels_buffer, env_mip_offsets_buffer KERNEL_VALIDATOR_BUFFERS);

        // accumulate environment lighting
        INDEX_SAFE(lightingExpandedBuffer, sampleDescriptionIdx) += occlusion.xyz * color;

        //compute directionality from indirect
        if (lightmapMode == LIGHTMAPMODE_DIRECTIONAL)
        {
            float  luminance   = Luminance(color);
            float3 scaledDir   = dir.xyz * luminance;
            float4 directional = make_float4(scaledDir.x, scaledDir.y, scaledDir.z, luminance);
            INDEX_SAFE(directionalExpandedBuffer, sampleDescriptionIdx) += directional;
        }
#endif
    }
}

__kernel void processIndirectEnvironment(
    INPUT_BUFFER(  0, ray,                 lightRaysCompactedBuffer),
    INPUT_BUFFER(  1, uint,                lightRaysCountBuffer),
    INPUT_BUFFER(  2, ray,                 pathRaysCompactedBuffer_0),//Only for kernel assert purpose.
    INPUT_BUFFER(  3, uint,                activePathCountBuffer_0),//Only for kernel assert purpose.
    INPUT_BUFFER(  4, uint,                lightRayIndexToPathRayIndexCompactedBuffer),
    INPUT_BUFFER(  5, float4,              originalRaysExpandedBuffer),
    INPUT_BUFFER(  6, float4,              lightOcclusionCompactedBuffer),
    INPUT_BUFFER(  7, float4,              pathThroughputExpandedBuffer),
    INPUT_BUFFER(  8, PackedNormalOctQuad, pathLastInterpNormalCompactedBuffer),
    INPUT_BUFFER(  9, float4,              env_mipped_cube_texels_buffer),
    INPUT_BUFFER( 10, int,                 env_mip_offsets_buffer),
    INPUT_VALUE(  11, Environment,         envData),
#ifdef PROBES
    INPUT_VALUE(  12, int,                 totalSampleCount),
    OUTPUT_BUFFER(13, float4,              probeSHExpandedBuffer)
#else
    INPUT_VALUE(  12, int,                 lightmapMode),
    OUTPUT_BUFFER(13, float3,              lightingExpandedBuffer),
    OUTPUT_BUFFER(14, float4,              directionalExpandedBuffer)
#endif
    KERNEL_VALIDATOR_BUFFERS_DEF
)
{
    uint compactedLightRayIdx = get_global_id(0);
    bool shouldProcessRay = true;

#if DISALLOW_LIGHT_RAYS_COMPACTION
    if (Ray_IsInactive(GET_PTR_SAFE(lightRaysCompactedBuffer, compactedLightRayIdx)))
    {
        shouldProcessRay = false;
    }
#endif

    if (shouldProcessRay && compactedLightRayIdx < INDEX_SAFE(lightRaysCountBuffer, 0))
    {
        KERNEL_ASSERT(Ray_IsActive(GET_PTR_SAFE(lightRaysCompactedBuffer, compactedLightRayIdx)));
        const int compactedPathRayIdx = INDEX_SAFE(lightRayIndexToPathRayIndexCompactedBuffer, compactedLightRayIdx);
        KERNEL_ASSERT(compactedPathRayIdx < INDEX_SAFE(activePathCountBuffer_0, 0));
        KERNEL_ASSERT(Ray_IsActive(GET_PTR_SAFE(pathRaysCompactedBuffer_0, compactedPathRayIdx)));
        const int expandedRayIdx = Ray_GetSampleDescriptionIndex(GET_PTR_SAFE(lightRaysCompactedBuffer, compactedLightRayIdx));

        float4 occlusion = INDEX_SAFE(lightOcclusionCompactedBuffer, compactedLightRayIdx);
        bool   occluded = occlusion.w < TRANSMISSION_THRESHOLD;
        if (!occluded)
        {
            // Environment intersection
            float3 interpNormal = DecodeNormal(INDEX_SAFE(pathLastInterpNormalCompactedBuffer, compactedPathRayIdx));
            float4 dir = INDEX_SAFE(lightRaysCompactedBuffer, compactedLightRayIdx).d;
            float3 color;

            if (UseEnvironmentImportanceSampling(envData.flags))
                color = ProcessEnvironmentRayIS(dir, interpNormal, envData.envDim, envData.numMips, envData.envmapIntegral, env_mipped_cube_texels_buffer, env_mip_offsets_buffer KERNEL_VALIDATOR_BUFFERS);
            else
                color = ProcessEnvironmentRay(dir, envData.envDim, envData.numMips, env_mipped_cube_texels_buffer, env_mip_offsets_buffer KERNEL_VALIDATOR_BUFFERS);

            color *= occlusion.xyz * INDEX_SAFE(pathThroughputExpandedBuffer, expandedRayIdx).xyz;
            float4 originalRayDirection = INDEX_SAFE(originalRaysExpandedBuffer, expandedRayIdx);
#ifdef PROBES
            float weight = 4.0f / totalSampleCount;
            accumulateSHExpanded(color.xyz, originalRayDirection, weight, probeSHExpandedBuffer, expandedRayIdx KERNEL_VALIDATOR_BUFFERS);
#else
            //compute directionality from indirect
            if (lightmapMode == LIGHTMAPMODE_DIRECTIONAL)
            {
                float luminance = Luminance(color);
                originalRayDirection.xyz *= luminance;
                originalRayDirection.w = luminance;
                INDEX_SAFE(directionalExpandedBuffer, expandedRayIdx) += originalRayDirection;
            }
            // accumulate environment lighting
            INDEX_SAFE(lightingExpandedBuffer, expandedRayIdx) += occlusion.xyz * color;
#endif
        }
    }
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\OpenCL\kernels\processEnvironment.cl---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\OpenCL\kernels\processLightRays.cl---------------
.
.
#include "commonCL.h"
#include "directLighting.h"

__kernel void processLightRays(
    // Inputs
    INPUT_BUFFER( 00, ray,                 lightRaysCompactedBuffer),
    INPUT_BUFFER( 01, float4,              positionsWSBuffer),
    INPUT_BUFFER( 02, LightBuffer,         directLightsBuffer),
    INPUT_BUFFER( 03, LightSample,         lightSamplesCompactedBuffer),
    INPUT_BUFFER( 04, float4,              lightOcclusionCompactedBuffer),
    INPUT_BUFFER( 05, float,               angularFalloffLUT_buffer),
    INPUT_BUFFER( 06, float,               distanceFalloffs_buffer),
    INPUT_BUFFER( 07, int,                 cookiesBuffer),
    INPUT_BUFFER( 08, uint,                lightRaysCountBuffer),
    INPUT_VALUE(  09, uint,                currentTileIdx),
    INPUT_VALUE(  10, uint,                sqrtNumTiles),
#ifdef PROBES
    INPUT_VALUE(  11, int,                 totalSampleCount),
    INPUT_BUFFER( 12, float4,              inputLightIndicesBuffer),
    // Outputs
    OUTPUT_BUFFER(13, float4,              probeSHExpandedBuffer),
    OUTPUT_BUFFER(14, float4,              probeOcclusionExpandedBuffer)
#else
    INPUT_BUFFER( 11, SampleDescription,   sampleDescriptionsExpandedBuffer),
    INPUT_BUFFER( 12, PackedNormalOctQuad, planeNormalsWSBuffer),
    INPUT_VALUE(  13, float,               pushOff),
    INPUT_VALUE(  14, int,                 lightmapMode),
    INPUT_VALUE(  15, int,                 superSamplingMultiplier),
    INPUT_VALUE(  16, int,                 lightmapSize),
    INPUT_BUFFER( 17, unsigned char,       gbufferInstanceIdToReceiveShadowsBuffer),
    INPUT_BUFFER( 18, uint,                sobol_buffer),
    INPUT_BUFFER( 19, float,               goldenSample_buffer),
    // Outputs
    OUTPUT_BUFFER(20, float4,              shadowmaskAoValidityExpandedBuffer),
    OUTPUT_BUFFER(21, float4,              directionalExpandedBuffer),
    OUTPUT_BUFFER(22, float3,              lightingExpandedBuffer)
#endif
    KERNEL_VALIDATOR_BUFFERS_DEF
)
{
    uint compactedRayIdx = get_global_id(0);
    if (compactedRayIdx >= INDEX_SAFE(lightRaysCountBuffer, 0))
        return;

#if DISALLOW_LIGHT_RAYS_COMPACTION
    if (Ray_IsInactive(GET_PTR_SAFE(lightRaysCompactedBuffer, compactedRayIdx)))
        return;
#endif

    KERNEL_ASSERT(Ray_IsActive(GET_PTR_SAFE(lightRaysCompactedBuffer, compactedRayIdx)));
    int sampleDescriptionIdx = Ray_GetSampleDescriptionIndex(GET_PTR_SAFE(lightRaysCompactedBuffer, compactedRayIdx));
    LightSample lightSample = INDEX_SAFE(lightSamplesCompactedBuffer, compactedRayIdx);
    LightBuffer light = INDEX_SAFE(directLightsBuffer, lightSample.lightIdx);

#ifndef PROBES
    const SampleDescription sampleDescription = INDEX_SAFE(sampleDescriptionsExpandedBuffer, sampleDescriptionIdx);
    const int texelIndex = sampleDescription.texelIndex;

    // Remap the global lightmap index to the local, super-sampled space of the tile
    const int localIdx = GetSuperSampledLocalIndex(sampleDescription.texelIndex, lightmapSize, sampleDescription.currentSampleCount, superSamplingMultiplier, currentTileIdx, sqrtNumTiles, sobol_buffer, goldenSample_buffer KERNEL_VALIDATOR_BUFFERS);

    const float4 positionAndGbufferInstanceId = INDEX_SAFE(positionsWSBuffer, localIdx);
    const int gBufferInstanceId = (int)(floor(positionAndGbufferInstanceId.w));
    const float3 P = positionAndGbufferInstanceId.xyz;
    const float3 planeNormal = DecodeNormal(INDEX_SAFE(planeNormalsWSBuffer, localIdx));
    const float3 position = P + planeNormal * pushOff;
#else
    const int texelIndex = Ray_GetSourceIndex(GET_PTR_SAFE(lightRaysCompactedBuffer, compactedRayIdx));
    const float3 position = INDEX_SAFE(positionsWSBuffer, texelIndex).xyz;
#endif

    bool useShadows = light.castShadow;
#ifndef PROBES
    useShadows &= INDEX_SAFE(gbufferInstanceIdToReceiveShadowsBuffer, gBufferInstanceId);
#endif
    float4 occlusions4 = useShadows ? INDEX_SAFE(lightOcclusionCompactedBuffer, compactedRayIdx) : make_float4(1.0f, 1.0f, 1.0f, 1.0f);
    const bool hit = occlusions4.w < TRANSMISSION_THRESHOLD;
    if (!hit)
    {
#ifdef PROBES
        const float weight = 1.0 / totalSampleCount;
        if (light.directBakeMode >= kDirectBakeMode_Subtractive)
        {
            int lightIdx = light.probeOcclusionLightIndex;
            const float4 lightIndicesFloat = INDEX_SAFE(inputLightIndicesBuffer, texelIndex);
            int4 lightIndices = (int4)((int)(lightIndicesFloat.x), (int)(lightIndicesFloat.y), (int)(lightIndicesFloat.z), (int)(lightIndicesFloat.w));
            float4 channelSelector = (float4)((lightIndices.x == lightIdx) ? 1.0f : 0.0f, (lightIndices.y == lightIdx) ? 1.0f : 0.0f, (lightIndices.z == lightIdx) ? 1.0f : 0.0f, (lightIndices.w == lightIdx) ? 1.0f : 0.0f);
            INDEX_SAFE(probeOcclusionExpandedBuffer, sampleDescriptionIdx) += channelSelector * weight;
        }
        else if (light.directBakeMode != kDirectBakeMode_None)
        {
            float4 D = (float4)(INDEX_SAFE(lightRaysCompactedBuffer, compactedRayIdx).d.x, INDEX_SAFE(lightRaysCompactedBuffer, compactedRayIdx).d.y, INDEX_SAFE(lightRaysCompactedBuffer, compactedRayIdx).d.z, 0);
            float3 L = ShadeLight(light, INDEX_SAFE(lightRaysCompactedBuffer, compactedRayIdx), position, angularFalloffLUT_buffer, distanceFalloffs_buffer, cookiesBuffer KERNEL_VALIDATOR_BUFFERS);
            accumulateSHExpanded(L, D, weight, probeSHExpandedBuffer, sampleDescriptionIdx KERNEL_VALIDATOR_BUFFERS);
        }
#else
        if (light.directBakeMode >= kDirectBakeMode_OcclusionChannel0)
        {
            float4 channelSelector = (float4)(light.directBakeMode == kDirectBakeMode_OcclusionChannel0 ? 1.0f : 0.0f, light.directBakeMode == kDirectBakeMode_OcclusionChannel1 ? 1.0f : 0.0f, light.directBakeMode == kDirectBakeMode_OcclusionChannel2 ? 1.0f : 0.0f, light.directBakeMode == kDirectBakeMode_OcclusionChannel3 ? 1.0f : 0.0f);
            INDEX_SAFE(shadowmaskAoValidityExpandedBuffer, sampleDescriptionIdx) += occlusions4.w * channelSelector;
        }
        else if (light.directBakeMode != kDirectBakeMode_None)
        {
            const float3 lighting = occlusions4.xyz * ShadeLight(light, INDEX_SAFE(lightRaysCompactedBuffer, compactedRayIdx), position, angularFalloffLUT_buffer, distanceFalloffs_buffer, cookiesBuffer KERNEL_VALIDATOR_BUFFERS);
            INDEX_SAFE(lightingExpandedBuffer, sampleDescriptionIdx).xyz += lighting;

            //compute directionality from direct lighting
            if (lightmapMode == LIGHTMAPMODE_DIRECTIONAL)
            {
                float lum = Luminance(lighting);
                float4 directionality;
                directionality.xyz = INDEX_SAFE(lightRaysCompactedBuffer, compactedRayIdx).d.xyz * lum;
                directionality.w = lum;
                INDEX_SAFE(directionalExpandedBuffer, sampleDescriptionIdx) += directionality;
            }
        }
#endif
    }
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Tools\Data\Resources\OpenCL\kernels\processLightRays.cl---------------
.
.
