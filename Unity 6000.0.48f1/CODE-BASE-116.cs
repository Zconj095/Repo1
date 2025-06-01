 
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.path-tracing\Runtime\Shaders\LightmapAOIntegration.urtshader---------------


#define UNIFIED_RT_GROUP_SIZE_X 64
#define UNIFIED_RT_GROUP_SIZE_Y 1
#define UNIFIED_RT_RAYGEN_FUNC_NAME AccumulateInternal

#include "PathTracing.hlsl"
#include "LightmapIntegrationHelpers.hlsl"

int g_SampleOffset;
float g_PushOff;
RWStructuredBuffer<float4> g_ExpandedOutput;
float g_AOMaxDistance;

void AccumulateInternal(UnifiedRT::DispatchInfo dispatchInfo)
{
    float3 worldPosition = 0.f;
    float3 worldNormal = 0.f;
    float3 worldFaceNormal = 0.f;
    uint localSampleOffset = 0;
    uint2 instanceTexelPos = 0;
    const bool gotSample = GetExpandedSample(dispatchInfo.dispatchThreadID.x, localSampleOffset, instanceTexelPos, worldPosition, worldNormal, worldFaceNormal);
    if (!gotSample)
        return;

    UnifiedRT::RayTracingAccelStruct accelStruct = UNIFIED_RT_GET_ACCEL_STRUCT(g_SceneAccelStruct);

    // setup rng
    const uint sampleOffset = g_SampleOffset + localSampleOffset;
    PathTracingSampler rngState;
    rngState.Init(instanceTexelPos, sampleOffset);

    // now sample occlusion with the gBuffer data
    UnifiedRT::Ray ray;
    float3 origin = OffsetRayOrigin(worldPosition, worldFaceNormal, g_PushOff);
    float3 direction = CosineSample(float2(rngState.GetFloatSample(RAND_DIM_SURF_SCATTER_X), rngState.GetFloatSample(RAND_DIM_SURF_SCATTER_Y)), worldNormal);
    ray.origin = origin;
    ray.direction = direction;
    ray.tMin = 0;
    ray.tMax = g_AOMaxDistance;
    rngState.NextBounce();

    float3 occlusion = 0.f;
    // trace through potentially several layers of transmissive materials, determining at each hit whether or not to kill the ray
    for (uint i = 0; i < MAX_TRANSMISSION_BOUNCES; i++)
    {
        UnifiedRT::Hit hitResult = UnifiedRT::TraceRayClosestHit(dispatchInfo, accelStruct, RayMask(true), ray, 0);
        if (hitResult.IsValid())
        {
            UnifiedRT::InstanceData instance = UnifiedRT::GetInstance(hitResult.instanceID);
            PTHitGeom geometry = GetHitGeomInfo(instance, hitResult);
            geometry.FixNormals(direction);
            MaterialProperties material = LoadMaterialProperties(instance, false, geometry);

            // Transmissive material, continue with a probability
            bool treatAsBackFace = ShouldTreatAsBackface(hitResult, material);
            if (ShouldTransmitRay(rngState, material))
            {
                ray.origin = geometry.NextTransmissionRayOrigin();
                ray.tMax = g_AOMaxDistance - distance(origin, ray.origin);
                rngState.NextBounce();
            }
            // No transmission, so the ray is occluded
            else
            {
                occlusion = 1.f;
                break;
            }
        }
        // Hit nothing, so the ray is not occluded
        else
            break;
    }
    g_ExpandedOutput[dispatchInfo.dispatchThreadID.x] += float4(occlusion, 1.0f);
}


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.path-tracing\Runtime\Shaders\LightmapAOIntegration.urtshader---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.path-tracing\Runtime\Shaders\LightmapDirectIntegration.urtshader---------------


#define UNIFIED_RT_GROUP_SIZE_X 64
#define UNIFIED_RT_GROUP_SIZE_Y 1
#define UNIFIED_RT_RAYGEN_FUNC_NAME AccumulateInternal

#include "PathTracing.hlsl"
#include "LightmapIntegrationHelpers.hlsl"

int g_AccumulateDirectional;
int g_SampleOffset;
uint g_ExcludeMeshAndEnvironment;
uint g_ReceiveShadows;
float g_PushOff;
RWStructuredBuffer<float4> g_ExpandedOutput;
RWStructuredBuffer<float4> g_ExpandedDirectional;

void AccumulateInternal(UnifiedRT::DispatchInfo dispatchInfo)
{
    float3 worldPosition = 0.f;
    float3 worldNormal = 0.f;
    float3 worldFaceNormal = 0.f;
    uint localSampleOffset = 0;
    uint2 instanceTexelPos = 0;
    const bool gotSample = GetExpandedSample(dispatchInfo.dispatchThreadID.x, localSampleOffset, instanceTexelPos, worldPosition, worldNormal, worldFaceNormal);
    if (!gotSample)
        return;

    UnifiedRT::RayTracingAccelStruct accelStruct = UNIFIED_RT_GET_ACCEL_STRUCT(g_SceneAccelStruct);

    // now sample direct lighting with the gBuffer data
    const uint sampleOffset = g_SampleOffset + localSampleOffset;
    PathTracingSampler rngState;
    rngState.Init(instanceTexelPos, sampleOffset);

    float4 radiance = 0.f;
    float4 directional = 0.f;
    float3 lightRadiance = 0.f;
    float3 lightDirection = 0.f;
    uint numLights = g_NumLights;
    uint dimsOffset = 2; // first two dimensions are used for the stochastic gBuffer sampling
    for (uint lightIndex = 0; lightIndex < numLights; ++lightIndex)
    {
        PTLight light = FetchLight(lightIndex);
        if (g_ExcludeMeshAndEnvironment && (light.type == EMISSIVE_MESH || light.type == ENVIRONMENT_LIGHT))
            continue;
        if (!light.contributesToDirectLighting)
            continue;
        uint dimsUsed = 0;
        float3 origin = OffsetRayOrigin(worldPosition, worldFaceNormal, g_PushOff);
        SampleDirectRadiance(dispatchInfo, accelStruct, ShadowRayMask(), origin, rngState, dimsOffset, dimsUsed, light, g_ReceiveShadows, lightRadiance, lightDirection);
        // always start at an even dimension
        if (dimsUsed % 2 == 1)
            dimsUsed++;
        dimsOffset += dimsUsed;
        float nDotL = dot(worldNormal, lightDirection);
        if (nDotL <= 0.0f)
            continue;
        lightRadiance = lightRadiance * nDotL;
        radiance.rgb += lightRadiance;
        directional += float4(lightDirection, 1.f) * Luminance(lightRadiance);
    }

    // store new accumulated radiance
    g_ExpandedOutput[dispatchInfo.dispatchThreadID.x] += float4(radiance.rgb, 1.0f);
    if (g_AccumulateDirectional > 0)
        g_ExpandedDirectional[dispatchInfo.dispatchThreadID.x] += directional;
}



#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.path-tracing\Runtime\Shaders\LightmapDirectIntegration.urtshader---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.path-tracing\Runtime\Shaders\LightmapGBufferIntegration.urtshader---------------


#define UNIFIED_RT_GROUP_SIZE_X 64
#define UNIFIED_RT_GROUP_SIZE_Y 1
#define UNIFIED_RT_RAYGEN_FUNC_NAME AccumulateInternal

#include "Packages/com.unity.rendering.light-transport/Runtime/UnifiedRayTracing/FetchGeometry.hlsl"
#include "Packages/com.unity.rendering.light-transport/Runtime/UnifiedRayTracing/TraceRay.hlsl"
#include "PathTracingRandom.hlsl"
#include "StochasticLightmapSampling.hlsl"

UNIFIED_RT_DECLARE_ACCEL_STRUCT(g_UVAccelStruct);
int g_StochasticAntialiasing;
float g_ExplicitSampleOffsetX;
float g_ExplicitSampleOffsetY;
int g_AASampleOffset; // index of the anti aliasing sample [0 - N], N is the total number of antialiasing samples to take
int g_InstanceOffsetX;
int g_InstanceOffsetY;
int g_InstanceWidth;
int g_InstanceHeight;
float g_InstanceWidthScale;
float g_InstanceHeightScale;
int g_ChunkOffsetX;
int g_ChunkOffsetY;
int g_ChunkSize;
Texture2D<half2> g_UvFallback;
RWStructuredBuffer<HitEntry> g_GBuffer;

float2 JitterSample(float2 sample, float2 random01, float jitterAmount)
{
    const float2 jitteredSample = sample + jitterAmount * (random01 - 0.5f);
    return saturate(jitteredSample);
}

void AccumulateInternal(UnifiedRT::DispatchInfo dispatchInfo)
{
    // The dispatch domain is [0; g_ChunkSize-1] in X
    const uint linearChunkOffset = g_ChunkOffsetX + g_ChunkOffsetY * g_InstanceWidth;
    const uint linearDispatch = linearChunkOffset + dispatchInfo.dispatchThreadID.x;
    if (linearDispatch >= (uint)g_InstanceWidth * (uint)g_InstanceHeight)
        return;
    const uint gbufferIndex = dispatchInfo.dispatchThreadID.x;// linearDispatch - linearChunkOffset; // 0-based index relative to the chunk
    const uint2 instanceTexelPos = uint2(linearDispatch % g_InstanceWidth, linearDispatch / g_InstanceWidth);
    const uint2 lightmapTexelPos = uint2(g_InstanceOffsetX, g_InstanceOffsetY) + instanceTexelPos;
    const float2 instanceSize = float2(g_InstanceWidth, g_InstanceHeight);

    if (g_UvFallback[instanceTexelPos].x < 0)
    {
        g_GBuffer[gbufferIndex].instanceID = -1; // record no intersection
        return;
    }

    UnifiedRT::RayTracingAccelStruct uvAccelStruct = UNIFIED_RT_GET_ACCEL_STRUCT(g_UVAccelStruct);

    PathTracingSampler rngState;
    rngState.Init(lightmapTexelPos, g_AASampleOffset);

     // select a random point in the texel
    const float2 random01 = float2(rngState.GetFloatSample(RAND_DIM_AA_X), rngState.GetFloatSample(RAND_DIM_AA_Y));
    const float2 texelSample = g_StochasticAntialiasing == 1 ? random01 : JitterSample(float2(g_ExplicitSampleOffsetX, g_ExplicitSampleOffsetY), random01, 0.00001f); // the jitter is to avoid issues with raytracing watertightness
    const float2 instanceScale = float2(g_InstanceWidthScale, g_InstanceHeightScale);
    const UnifiedRT::Hit hit = LightmapSampleTexelOffset(instanceTexelPos, texelSample, instanceSize, instanceScale, dispatchInfo, uvAccelStruct, g_UvFallback[instanceTexelPos]);

    if (!hit.IsValid())
    {
        // no intersection found
        g_GBuffer[gbufferIndex].instanceID = -1;
        return;
    }
    g_GBuffer[gbufferIndex].instanceID = hit.instanceID;
    g_GBuffer[gbufferIndex].primitiveIndex = hit.primitiveIndex;
    g_GBuffer[gbufferIndex].barycentrics = hit.uvBarycentrics;
}



#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.path-tracing\Runtime\Shaders\LightmapGBufferIntegration.urtshader---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.path-tracing\Runtime\Shaders\LightmapIndirectIntegration.urtshader---------------


#define UNIFIED_RT_GROUP_SIZE_X 64
#define UNIFIED_RT_GROUP_SIZE_Y 1
#define UNIFIED_RT_RAYGEN_FUNC_NAME AccumulateInternal

#include "PathTracing.hlsl"
#include "LightmapIntegrationHelpers.hlsl"

int g_AccumulateDirectional;
int g_SampleOffset;
float g_PushOff;
RWStructuredBuffer<float4> g_ExpandedOutput;
RWStructuredBuffer<float4> g_ExpandedDirectional;

// additional instance flags to deal with lightmap lods
#define CURRENT_LOD_FOR_LIGHTMAP_INSTANCE 8u
#define LOD_ZERO_FOR_LIGHTMAP_INSTANCE 16u
#define CURRENT_LOD_FOR_LIGHTMAP_INSTANCE_SHADOW 32u
#define LOD_ZERO_FOR_LIGHTMAP_INSTANCE_SHADOW 64u

float3 EstimateLightmapRadiance(UnifiedRT::DispatchInfo dispatchInfo, UnifiedRT::Ray ray, inout PathTracingSampler rngState)
{
    UnifiedRT::RayTracingAccelStruct accelStruct = UNIFIED_RT_GET_ACCEL_STRUCT(g_SceneAccelStruct);

    PathIterator pathIter;
    InitPathIterator(pathIter, ray);

    // LOD handling: Rays starting from the lightmapped object use an accel struct built with that same LOD level
    uint rayLightmapLodMask = CURRENT_LOD_FOR_LIGHTMAP_INSTANCE;
    uint shadowRayLightmapLodMask = CURRENT_LOD_FOR_LIGHTMAP_INSTANCE_SHADOW;

    int transparencyBounce = 0;
    // We start at bounce index 1, as bounce index is defined relative to the camera for this path tracer.
    // Since this function is used for baking, we already implicitly have the first "hit", and are about
    // to process the second path segment.
    for (int bounceIndex = 1; bounceIndex <= g_BounceCount && transparencyBounce < MAX_TRANSMISSION_BOUNCES; bounceIndex++)
    {
        uint pathRayMask = RayMask(bounceIndex == 0) | rayLightmapLodMask;
        uint traceResult = TraceBounceRay(pathIter, bounceIndex, pathRayMask, dispatchInfo, accelStruct, rngState);

        if (traceResult == TRACE_HIT)
        {
            uint hitInstanceMask = UnifiedRT::GetInstance(pathIter.hitResult.instanceID).instanceMask;

            // LOD handling: If the ray hits a surface that is not the current lightmap instance,
            // then, for the rest of the path, we can replace it in the scene accel struct by one that using lod 0.
            if (!(hitInstanceMask & CURRENT_LOD_FOR_LIGHTMAP_INSTANCE))
            {
                rayLightmapLodMask = LOD_ZERO_FOR_LIGHTMAP_INSTANCE;
                shadowRayLightmapLodMask = LOD_ZERO_FOR_LIGHTMAP_INSTANCE_SHADOW;
            }

            uint shadowRayMask = ShadowRayMask() | shadowRayLightmapLodMask;
            EvalDirectIllumination(pathIter, bounceIndex, shadowRayMask, dispatchInfo, accelStruct, rngState);
        }

        if (traceResult == TRACE_MISS)
        {
            break;
        }

        if (traceResult == TRACE_TRANSMISSION)
        {
            bounceIndex--;
            transparencyBounce++;
            pathIter.ray.origin = pathIter.hitGeo.NextTransmissionRayOrigin();
            pathIter.throughput *= pathIter.material.transmission;
            rngState.NextBounce();
            continue;
        }

        if (!Scatter(pathIter, rngState))
            break;

        if (bounceIndex >= RUSSIAN_ROULETTE_MIN_BOUNCES)
        {
            float p = max(pathIter.throughput.x, max(pathIter.throughput.y, pathIter.throughput.z));
            if (rngState.GetFloatSample(RAND_DIM_RUSSIAN_ROULETTE) > p)
                break;
            else
                pathIter.throughput /= p;
        }

        rngState.NextBounce();
    }

    return pathIter.radianceSample;
}


void AccumulateInternal(UnifiedRT::DispatchInfo dispatchInfo)
{
    float3 worldPosition = 0.f;
    float3 worldNormal = 0.f;
    float3 worldFaceNormal = 0.f;
    uint localSampleOffset = 0;
    uint2 instanceTexelPos = 0;
    const bool gotSample = GetExpandedSample(dispatchInfo.dispatchThreadID.x, localSampleOffset, instanceTexelPos, worldPosition, worldNormal, worldFaceNormal);
    if (!gotSample)
        return;

    UnifiedRT::RayTracingAccelStruct accelStruct = UNIFIED_RT_GET_ACCEL_STRUCT(g_SceneAccelStruct);

    // now sample irradiance with the gBuffer data
    const uint sampleOffset = g_SampleOffset + localSampleOffset;
    PathTracingSampler rngState;
    rngState.Init(instanceTexelPos, sampleOffset);
    UnifiedRT::Ray ray;
    ray.origin = OffsetRayOrigin(worldPosition, worldFaceNormal, g_PushOff);
    ray.direction = CosineSample(float2(rngState.GetFloatSample(RAND_DIM_SURF_SCATTER_X), rngState.GetFloatSample(RAND_DIM_SURF_SCATTER_Y)), worldNormal);
    ray.tMin = 0;
    ray.tMax = Max_float();
    rngState.NextBounce();

    float3 sampleRadiance = EstimateLightmapRadiance(dispatchInfo, ray, rngState);

    const float sampleLuminance = Luminance(sampleRadiance.xyz);
    const float probabilityDensityDividedByCosine = PI;

    // store new accumulated radiance
    g_ExpandedOutput[dispatchInfo.dispatchThreadID.x] += float4(sampleRadiance * probabilityDensityDividedByCosine, 1.0f);
    // the cosine term from the PDF cancels with the cosine term for the integrand.
    if (g_AccumulateDirectional > 0)
        g_ExpandedDirectional[dispatchInfo.dispatchThreadID.x] += float4(normalize(ray.direction), 1.f) * sampleLuminance;
}


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.path-tracing\Runtime\Shaders\LightmapIndirectIntegration.urtshader---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.path-tracing\Runtime\Shaders\LightmapNormalIntegration.urtshader---------------


#define UNIFIED_RT_GROUP_SIZE_X 64
#define UNIFIED_RT_GROUP_SIZE_Y 1
#define UNIFIED_RT_RAYGEN_FUNC_NAME AccumulateInternal

#include "PathTracing.hlsl"
#include "LightmapIntegrationHelpers.hlsl"

int g_SampleOffset;
float g_PushOff;
RWStructuredBuffer<float4> g_ExpandedOutput;
float g_AOMaxDistance;

void AccumulateInternal(UnifiedRT::DispatchInfo dispatchInfo)
{
    float3 worldPosition = 0.f;
    float3 worldNormal = 0.f;
    float3 worldFaceNormal = 0.f;
    uint localSampleOffset = 0;
    uint2 instanceTexelPos = 0;
    const bool gotSample = GetExpandedSample(dispatchInfo.dispatchThreadID.x, localSampleOffset, instanceTexelPos, worldPosition, worldNormal, worldFaceNormal);
    if (!gotSample)
        return;
    g_ExpandedOutput[dispatchInfo.dispatchThreadID.x] = float4(worldNormal, 1.0f);
}


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.path-tracing\Runtime\Shaders\LightmapNormalIntegration.urtshader---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.path-tracing\Runtime\Shaders\LightmapShadowMaskIntegration.urtshader---------------


#define UNIFIED_RT_GROUP_SIZE_X 64
#define UNIFIED_RT_GROUP_SIZE_Y 1
#define UNIFIED_RT_RAYGEN_FUNC_NAME AccumulateInternal

#include "PathTracing.hlsl"
#include "LightmapIntegrationHelpers.hlsl"

int g_SampleOffset;
uint g_ExcludeMeshAndEnvironment;
uint g_ReceiveShadows;
float g_PushOff;
RWStructuredBuffer<float4> g_ExpandedOutput;
RWStructuredBuffer<float4> g_ExpandedSampleCountInW;

void AccumulateInternal(UnifiedRT::DispatchInfo dispatchInfo)
{
    float3 worldPosition = 0.f;
    float3 worldNormal = 0.f;
    float3 worldFaceNormal = 0.f;
    uint localSampleOffset = 0;
    uint2 instanceTexelPos = 0;
    const bool gotSample = GetExpandedSample(dispatchInfo.dispatchThreadID.x, localSampleOffset, instanceTexelPos, worldPosition, worldNormal, worldFaceNormal);
    if (!gotSample)
        return;

    UnifiedRT::RayTracingAccelStruct accelStruct = UNIFIED_RT_GET_ACCEL_STRUCT(g_SceneAccelStruct);

    // now sample direct lighting with the gBuffer data
    const uint sampleOffset = g_SampleOffset + localSampleOffset;
    PathTracingSampler rngState;
    rngState.Init(instanceTexelPos, sampleOffset);

    // now sample direct lighting with the gBuffer data
    uint numLights = g_NumLights;
    uint dimsOffset = 2; // first two dimensions are used for the stochastic gBuffer sampling
    float visibility[4] = { 0.f, 0.f, 0.f, 0.f };
    for (uint lightIndex = 0; lightIndex < numLights; ++lightIndex)
    {
        PTLight light = FetchLight(lightIndex);
        if (light.shadowMaskChannel == -1)
            continue;
        if (g_ExcludeMeshAndEnvironment && (light.type == EMISSIVE_MESH || light.type == ENVIRONMENT_LIGHT))
            continue;
        uint dimsUsed = 0;

        float3 origin = OffsetRayOrigin(worldPosition, worldFaceNormal, g_PushOff);

        float3 attenuation = 1.0f;
        float isVisible = IsLightVisibleFromPoint(dispatchInfo, accelStruct, ShadowRayMask(), origin, rngState, dimsOffset, dimsUsed, light, g_ReceiveShadows, attenuation) ? dot(float3(1.0f, 1.0f, 1.0f), attenuation)/3.0f : 0.0f;
        // always start at an even dimension
        if (dimsUsed % 2 == 1)
            dimsUsed++;
        dimsOffset += dimsUsed;
        if (light.shadowMaskChannel < 0 || light.shadowMaskChannel >= 4)
            continue;
        visibility[light.shadowMaskChannel] += isVisible;
    }

    // store new accumulated radiance
    g_ExpandedOutput[dispatchInfo.dispatchThreadID.x] += float4(visibility[0], visibility[1], visibility[2], visibility[3]);
    g_ExpandedSampleCountInW[dispatchInfo.dispatchThreadID.x] += float4(0.f, 0.f, 0.f, 1.f);
}



#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.path-tracing\Runtime\Shaders\LightmapShadowMaskIntegration.urtshader---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.path-tracing\Runtime\Shaders\LightmapValidityIntegration.urtshader---------------


#define UNIFIED_RT_GROUP_SIZE_X 64
#define UNIFIED_RT_GROUP_SIZE_Y 1
#define UNIFIED_RT_RAYGEN_FUNC_NAME AccumulateInternal

#include "PathTracing.hlsl"
#include "LightmapIntegrationHelpers.hlsl"

int g_SampleOffset;
float g_PushOff;
RWStructuredBuffer<float4> g_ExpandedOutput;

void AccumulateInternal(UnifiedRT::DispatchInfo dispatchInfo)
{
    float3 worldPosition = 0.f;
    float3 worldNormal = 0.f;
    float3 worldFaceNormal = 0.f;
    uint localSampleOffset = 0;
    uint2 instanceTexelPos = 0;
    const bool gotSample = GetExpandedSample(dispatchInfo.dispatchThreadID.x, localSampleOffset, instanceTexelPos, worldPosition, worldNormal, worldFaceNormal);
    if (!gotSample)
        return;

    UnifiedRT::RayTracingAccelStruct accelStruct = UNIFIED_RT_GET_ACCEL_STRUCT(g_SceneAccelStruct);

    // setup rng
    const uint sampleOffset = g_SampleOffset + localSampleOffset;
    PathTracingSampler rngState;
    rngState.Init(instanceTexelPos, sampleOffset);

    // now sample validity with the gBuffer data
    float invalidity = 0.f;
    UnifiedRT::Ray ray;
    ray.origin = OffsetRayOrigin(worldPosition, worldFaceNormal, g_PushOff);
    const float3 direction = CosineSample(float2(rngState.GetFloatSample(RAND_DIM_SURF_SCATTER_X), rngState.GetFloatSample(RAND_DIM_SURF_SCATTER_Y)), worldNormal);
    ray.direction = direction;
    ray.tMin = 0;
    ray.tMax = Max_float();
    rngState.NextBounce();

    // trace through potentially several layers of transmissive materials, determining validity at each hit
    for (uint i = 0; i < MAX_TRANSMISSION_BOUNCES; i++)
    {
        UnifiedRT::Hit hitResult = UnifiedRT::TraceRayClosestHit(dispatchInfo, accelStruct, RayMask(true), ray, 0);
        if (!hitResult.IsValid())
            break; // Hit nothing, so the ray is valid

        // Something was hit
        UnifiedRT::InstanceData instance = UnifiedRT::GetInstance(hitResult.instanceID);
        PTHitGeom geometry = GetHitGeomInfo(instance, hitResult);
        MaterialProperties material = LoadMaterialProperties(instance, false, geometry);

        // Check for transmission
        if (ShouldTransmitRay(rngState, material))
        {
            geometry.FixNormals(direction);
            ray.origin = geometry.NextTransmissionRayOrigin();
            rngState.NextBounce();
            continue;
        }

        // Check for validity
        bool treatAsBackFace = ShouldTreatAsBackface(hitResult, material);
        if (!treatAsBackFace)
            break; // we have a valid hit

        // Hit was invalid and there is no transmission
        invalidity += 1.f;
        break;
    }
    g_ExpandedOutput[dispatchInfo.dispatchThreadID.x] += float4(invalidity, invalidity, invalidity, 1.0f);
}


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.path-tracing\Runtime\Shaders\LightmapValidityIntegration.urtshader---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.path-tracing\Runtime\Shaders\ProbeIntegrationDirect.urtshader---------------


#define UNIFIED_RT_GROUP_SIZE_X 128
#define UNIFIED_RT_GROUP_SIZE_Y 1
#define UNIFIED_RT_RAYGEN_FUNC_NAME IntegrateDirectRadiance

#include "Packages/com.unity.rendering.light-transport/Runtime/UnifiedRayTracing/FetchGeometry.hlsl"
#include "Packages/com.unity.rendering.light-transport/Runtime/UnifiedRayTracing/TraceRay.hlsl"

#include "PathTracing.hlsl"
#include "SphericalHarmonicsUtils.hlsl"

RWStructuredBuffer<float3> g_Positions;
RWStructuredBuffer<float> g_RadianceShl2;
uint g_PositionsOffset;
uint g_SampleOffset;
uint g_SampleCount;
uint g_ExcludeMeshAndEnvironment;

void IntegrateDirectRadiance(UnifiedRT::DispatchInfo dispatchInfo)
{
    const uint threadIdx = dispatchInfo.dispatchThreadID.x;
    const uint inProbeIdx = threadIdx / g_SampleCount + g_PositionsOffset;
    const uint inProbeSampleIdx = threadIdx % g_SampleCount;
    const uint outProbeIdx = threadIdx;

    PathTracingSampler rngState;
    rngState.Init(uint2(inProbeIdx, 0), g_SampleOffset + inProbeSampleIdx); // TODO(pema.malling): Make 1D version of scrambling. https://jira.unity3d.com/browse/LIGHT-1686

    // Local array to accumulate radiance into, using SoA layout.
    float3 accumulatedRadianceSH[SH_COEFFICIENTS_PER_CHANNEL];
    for (int i = 0; i < SH_COEFFICIENTS_PER_CHANNEL; ++i)
    {
        accumulatedRadianceSH[i] = 0.0f;
    }

    // Set up some stuff we need to sample lights.
    UnifiedRT::RayTracingAccelStruct accelStruct = UNIFIED_RT_GET_ACCEL_STRUCT(g_SceneAccelStruct);
    float3 worldPosition = g_Positions[inProbeIdx];

    // Sample all lights.
    uint dimsOffset = 0;
    for (uint lightIndex = 0; lightIndex < g_NumLights; lightIndex++)
    {
        float3 lightRadiance = 0;
        float3 lightDirection = 0;

        PTLight light = FetchLight(lightIndex);
        if (g_ExcludeMeshAndEnvironment && (light.type == EMISSIVE_MESH || light.type == ENVIRONMENT_LIGHT))
            continue;
        if (!light.contributesToDirectLighting)
            continue;
        uint dimsUsed = 0;
        SampleDirectRadiance(dispatchInfo, accelStruct, ShadowRayMask(), worldPosition, rngState, dimsOffset, dimsUsed, light, true, lightRadiance, lightDirection);
        // always start at an even dimension
        if (dimsUsed % 2 == 1)
            dimsUsed++;
        dimsOffset += dimsUsed;

        // Project into SH.
        accumulatedRadianceSH[0] += lightRadiance * SHL0();
        accumulatedRadianceSH[1] += lightRadiance * SHL1_1(lightDirection);
        accumulatedRadianceSH[2] += lightRadiance * SHL10(lightDirection);
        accumulatedRadianceSH[3] += lightRadiance * SHL11(lightDirection);

        accumulatedRadianceSH[4] += lightRadiance * SHL2_2(lightDirection);
        accumulatedRadianceSH[5] += lightRadiance * SHL2_1(lightDirection);
        accumulatedRadianceSH[6] += lightRadiance * SHL20(lightDirection);
        accumulatedRadianceSH[7] += lightRadiance * SHL21(lightDirection);
        accumulatedRadianceSH[8] += lightRadiance * SHL22(lightDirection);
    }

    const float monteCarloNormalization = 1.0f / (float)g_SampleCount;
    for (uint channel = 0; channel < SH_COLOR_CHANNELS; ++channel)
    {
        for (uint i = 0; i < SH_COEFFICIENTS_PER_CHANNEL; ++i)
        {
            g_RadianceShl2[SHIndex(outProbeIdx, channel, i)] = accumulatedRadianceSH[i][channel] * monteCarloNormalization;
        }
    }
}


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.path-tracing\Runtime\Shaders\ProbeIntegrationDirect.urtshader---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.path-tracing\Runtime\Shaders\ProbeIntegrationIndirect.urtshader---------------


#define UNIFIED_RT_GROUP_SIZE_X 128
#define UNIFIED_RT_GROUP_SIZE_Y 1
#define UNIFIED_RT_RAYGEN_FUNC_NAME IntegrateIndirectRadiance

#include "Packages/com.unity.rendering.light-transport/Runtime/UnifiedRayTracing/FetchGeometry.hlsl"
#include "Packages/com.unity.rendering.light-transport/Runtime/UnifiedRayTracing/TraceRay.hlsl"

#include "PathTracing.hlsl"
#include "SphericalHarmonicsUtils.hlsl"

RWStructuredBuffer<float3> g_Positions;
RWStructuredBuffer<float> g_RadianceShl2;
uint g_PositionsOffset;
uint g_SampleOffset;
uint g_SampleCount;

void IntegrateIndirectRadiance(UnifiedRT::DispatchInfo dispatchInfo)
{
    const uint threadIdx = dispatchInfo.dispatchThreadID.x;
    const uint inProbeIdx = threadIdx / g_SampleCount + g_PositionsOffset;
    const uint inProbeSampleIdx = threadIdx % g_SampleCount;
    const uint outProbeIdx = threadIdx;

    PathTracingSampler rngState;
    rngState.Init(uint2(inProbeIdx, 0), g_SampleOffset + inProbeSampleIdx); // TODO(pema.malling): Make 1D version of scrambling. https://jira.unity3d.com/browse/LIGHT-1686

    // TODO(pema.malling): This works but that is sort of by accident. Avoid coupling to AA (which is unrelated to probe integration). https://jira.unity3d.com/browse/LIGHT-1687
    const float3 uniformSphereDir = MapSquareToSphere(float2(rngState.GetFloatSample(RAND_DIM_AA_X), rngState.GetFloatSample(RAND_DIM_AA_Y)));

    UnifiedRT::Ray ray;
    ray.origin = g_Positions[inProbeIdx];
    ray.direction = uniformSphereDir;
    ray.tMin = 0;
    ray.tMax = K_T_MAX;

    float3 radiance = EstimateRadiance(dispatchInfo, ray, rngState);

    // Local array to accumulate radiance into, using SoA layout.
    float3 accumulatedRadianceSH[SH_COEFFICIENTS_PER_CHANNEL];
    accumulatedRadianceSH[0] = radiance * SHL0();
    accumulatedRadianceSH[1] = radiance * SHL1_1(uniformSphereDir);
    accumulatedRadianceSH[2] = radiance * SHL10(uniformSphereDir);
    accumulatedRadianceSH[3] = radiance * SHL11(uniformSphereDir);

    accumulatedRadianceSH[4] = radiance * SHL2_2(uniformSphereDir);
    accumulatedRadianceSH[5] = radiance * SHL2_1(uniformSphereDir);
    accumulatedRadianceSH[6] = radiance * SHL20(uniformSphereDir);
    accumulatedRadianceSH[7] = radiance * SHL21(uniformSphereDir);
    accumulatedRadianceSH[8] = radiance * SHL22(uniformSphereDir);

    const float reciprocalSampleCount = 1.0f / (float) g_SampleCount;
    const float reciprocalUniformSphereDensity = 4.0f * PI;
    const float monteCarloNormalization = reciprocalSampleCount * reciprocalUniformSphereDensity;
    for (uint channel = 0; channel < SH_COLOR_CHANNELS; ++channel)
    {
        for (uint i = 0; i < SH_COEFFICIENTS_PER_CHANNEL; ++i)
        {
            g_RadianceShl2[SHIndex(outProbeIdx, channel, i)] = accumulatedRadianceSH[i][channel] * monteCarloNormalization;
        }
    }
}


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.path-tracing\Runtime\Shaders\ProbeIntegrationIndirect.urtshader---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.path-tracing\Runtime\Shaders\ProbeIntegrationOcclusion.urtshader---------------


#define UNIFIED_RT_GROUP_SIZE_X 128
#define UNIFIED_RT_GROUP_SIZE_Y 1
#define UNIFIED_RT_RAYGEN_FUNC_NAME IntegrateOcclusion

#include "Packages/com.unity.rendering.light-transport/Runtime/UnifiedRayTracing/FetchGeometry.hlsl"
#include "Packages/com.unity.rendering.light-transport/Runtime/UnifiedRayTracing/TraceRay.hlsl"

#include "PathTracingRandom.hlsl"
#include "LightSampling.hlsl"

UNIFIED_RT_DECLARE_ACCEL_STRUCT(g_SceneAccelStruct);

RWStructuredBuffer<float3> g_Positions;
RWStructuredBuffer<int> g_PerProbeLightIndices;
RWStructuredBuffer<float> g_Occlusion;
uint g_PositionsOffset;
uint g_PerProbeLightIndicesOffset;
uint g_MaxLightsPerProbe;
uint g_SampleCount;
uint g_SampleOffset;

void IntegrateOcclusion(UnifiedRT::DispatchInfo dispatchInfo)
{
    UnifiedRT::RayTracingAccelStruct accelStruct = UNIFIED_RT_GET_ACCEL_STRUCT(g_SceneAccelStruct);

    const uint threadIdx = dispatchInfo.dispatchThreadID.x;
    const uint inProbeIdx = threadIdx / g_SampleCount;
    const uint inProbeIdxWithOffset = inProbeIdx + g_PositionsOffset;
    const uint inProbeSampleIdx = threadIdx % g_SampleCount;

    float3 worldPosition = g_Positions[inProbeIdxWithOffset];

    // Which probe in the expanded g_Occlusion buffer are we in?
    const uint outProbeIdx = inProbeIdx * g_SampleCount + inProbeSampleIdx;

    for (uint indirectLightIndex = 0; indirectLightIndex < g_MaxLightsPerProbe; indirectLightIndex++)
    {
        // Which light in the expanded g_Occlusion buffer are we in?
        const uint outOcclusionValueIdx = outProbeIdx * g_MaxLightsPerProbe + indirectLightIndex;
        // Which light in the non-expanded g_PerProbeLightIndices buffer are we in?
        const uint perProbeLightIndicesIdx = inProbeIdx * g_MaxLightsPerProbe + indirectLightIndex;

        PathTracingSampler rngState;
        rngState.Init(uint2(inProbeIdxWithOffset, 0), g_SampleOffset + inProbeSampleIdx); // TODO(pema.malling): Make 1D version of scrambling. https://jira.unity3d.com/browse/LIGHT-1686

        uint lightIndex = g_PerProbeLightIndices[perProbeLightIndicesIdx];
        if (lightIndex == -1)
        {
            g_Occlusion[outOcclusionValueIdx] = 0.0f;
            continue;
        }

        PTLight light = FetchLight(lightIndex);
        if (light.type != SPOT_LIGHT && light.type != POINT_LIGHT && light.type != DIRECTIONAL_LIGHT)
        {
            g_Occlusion[outOcclusionValueIdx] = 0.0f;
            continue;
        }

        uint dimsOffset = 0;
        uint dimsUsed = 0;
        float3 attenuation = 1.0f;
        bool isVisible = IsLightVisibleFromPoint(dispatchInfo, accelStruct, SHADOW_RAY_VIS_MASK, worldPosition, rngState, dimsOffset, dimsUsed, light, true, attenuation);
        if (isVisible)
        {
            g_Occlusion[outOcclusionValueIdx] = 1.0f / g_SampleCount;
        }
    }
}


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.path-tracing\Runtime\Shaders\ProbeIntegrationOcclusion.urtshader---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.path-tracing\Runtime\Shaders\ProbeIntegrationValidity.urtshader---------------


#define UNIFIED_RT_GROUP_SIZE_X 128
#define UNIFIED_RT_GROUP_SIZE_Y 1
#define UNIFIED_RT_RAYGEN_FUNC_NAME IntegrateValidity

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.rendering.light-transport/Runtime/UnifiedRayTracing/FetchGeometry.hlsl"
#include "Packages/com.unity.rendering.light-transport/Runtime/UnifiedRayTracing/TraceRay.hlsl"

#include "PathTracingCommon.hlsl"
#include "PathTracingMaterials.hlsl"
#include "PathTracingRandom.hlsl"

UNIFIED_RT_DECLARE_ACCEL_STRUCT(g_SceneAccelStruct);

RWStructuredBuffer<float3> g_Positions;
RWStructuredBuffer<float> g_Validity;
uint g_PositionsOffset;
uint g_SampleCount;
uint g_SampleOffset;

void IntegrateValidity(UnifiedRT::DispatchInfo dispatchInfo)
{
    const uint threadIdx = dispatchInfo.dispatchThreadID.x;
    const uint inProbeIdx = threadIdx / g_SampleCount + g_PositionsOffset;
    const uint inProbeSampleIdx = threadIdx % g_SampleCount;
    const uint outProbeIdx = threadIdx;

    PathTracingSampler rngState;
    rngState.Init(uint2(inProbeIdx, 0), g_SampleOffset + inProbeSampleIdx); // TODO(pema.malling): Make 1D version of scrambling. https://jira.unity3d.com/browse/LIGHT-1686

    // TODO(pema.malling): This works but that is sort of by accident. Avoid coupling to AA (which is unrelated to probe integration). https://jira.unity3d.com/browse/LIGHT-1687
    const float3 uniformSphereDir = MapSquareToSphere(float2(rngState.GetFloatSample(RAND_DIM_AA_X), rngState.GetFloatSample(RAND_DIM_AA_Y)));

    UnifiedRT::Ray ray;
    ray.origin = g_Positions[inProbeIdx];
    ray.direction = uniformSphereDir;
    ray.tMin = 0;
    ray.tMax = K_T_MAX;

    int rayFlags = UnifiedRT::kRayFlagNone;
    UnifiedRT::RayTracingAccelStruct accelStruct = UNIFIED_RT_GET_ACCEL_STRUCT(g_SceneAccelStruct);
    UnifiedRT::Hit hitResult = UnifiedRT::TraceRayClosestHit(dispatchInfo, accelStruct, UINT_MAX, ray, rayFlags);

    bool invalidHit = false;
    if (hitResult.IsValid())
    {
        UnifiedRT::InstanceData instance = UnifiedRT::GetInstance(hitResult.instanceID);
        PTHitGeom geometry = GetHitGeomInfo(instance, hitResult);
        MaterialProperties material = LoadMaterialProperties(instance, false, geometry);

        if (!hitResult.isFrontFace && !material.doubleSidedGI && !material.isTransmissive)
            invalidHit = true;
    }

    g_Validity[outProbeIdx] = invalidHit ? 1.0f / g_SampleCount : 0.0f;
}


#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.path-tracing\Runtime\Shaders\ProbeIntegrationValidity.urtshader---------------


#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.path-tracing\Tests\Runtime\PathIteratorTest.urtshader---------------
.
.
#define UNIFIED_RT_GROUP_SIZE_X 16
#define UNIFIED_RT_GROUP_SIZE_Y 8
#define UNIFIED_RT_RAYGEN_FUNC_NAME TestRadianceEstimation

#include "Packages/com.unity.rendering.light-transport/Runtime/UnifiedRayTracing/FetchGeometry.hlsl"
#include "Packages/com.unity.rendering.light-transport/Runtime/UnifiedRayTracing/TraceRay.hlsl"
#include "Packages/com.unity.path-tracing/Runtime/Shaders/PathTracing.hlsl"

uint g_SampleCount;

struct TestRay
{
    float3 origin;
    float3 direction;
};

StructuredBuffer<TestRay> _InputRay;
RWStructuredBuffer<float3> _Output;

float3 EstimateIncomingRadiance(UnifiedRT::DispatchInfo dispatchInfo, float3 rayOrigin, float3 rayDirection, uint sampleCount, uint bounceCount, UnifiedRT::RayTracingAccelStruct accelStruct)
{
    PathTracingSampler rngState;
    rngState.Init(uint2(0, 0), 0);

    PathIterator pathIter;
    float3 sampleSum = 0;
    for (uint sampleIdx = 0; sampleIdx < sampleCount; ++sampleIdx)
    {
        UnifiedRT::Ray ray;
        ray.origin = rayOrigin;
        ray.direction = rayDirection;
        ray.tMin = 0;
        ray.tMax = FLT_INF;
        InitPathIterator(pathIter, ray);

        int transparencyBounce = 0;
        const int maxTransparencyBounces = 6;
        for (uint bounceIndex = 0; bounceIndex <= bounceCount && transparencyBounce < maxTransparencyBounces; bounceIndex++)
        {
            uint traceResult = TraceBounceRayAndEvalDirectIllumination(pathIter, bounceIndex, RayMask(bounceIndex == 0), ShadowRayMask(), dispatchInfo, accelStruct, rngState);

            if (traceResult == TRACE_MISS)
            {
                break;
            }
            if (traceResult == TRACE_TRANSMISSION)
            {
                bounceIndex--;
                transparencyBounce++;
                continue;
            }

            if (!Scatter(pathIter, rngState))
                break;

            rngState.NextBounce();
        }

        sampleSum += pathIter.radianceSample;
    }

    return sampleSum / float(sampleCount);
}

void TestRadianceEstimation(UnifiedRT::DispatchInfo dispatchInfo)
{
    UnifiedRT::RayTracingAccelStruct accelStruct = UNIFIED_RT_GET_ACCEL_STRUCT(g_SceneAccelStruct);
    TestRay ray = _InputRay[0];
    _Output[0] = EstimateIncomingRadiance(dispatchInfo, ray.origin, ray.direction, g_SampleCount, g_BounceCount, accelStruct);
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.path-tracing\Tests\Runtime\PathIteratorTest.urtshader---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.core\Editor\Lighting\ProbeVolume\DynamicGI\DynamicGISkyOcclusion.urtshader---------------
.
.
#pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal webgpu
#define UNIFIED_RT_GROUP_SIZE_X 64
#define UNIFIED_RT_GROUP_SIZE_Y 1

#include "Packages/com.unity.rendering.light-transport/Runtime/UnifiedRayTracing/FetchGeometry.hlsl"
#include "Packages/com.unity.rendering.light-transport/Runtime/UnifiedRayTracing/TraceRay.hlsl"
#include "Packages/com.unity.rendering.light-transport/Runtime/UnifiedRayTracing/Common.hlsl"

#define QRNG_METHOD_SOBOL
#include "Packages/com.unity.rendering.light-transport/Runtime/Sampling/QuasiRandom.hlsl"
#include "Packages/com.unity.rendering.light-transport/Runtime/Sampling/Common.hlsl"

UNIFIED_RT_DECLARE_ACCEL_STRUCT(_AccelStruct);


int _SampleCount;
int _SampleId;
int _MaxBounces;
float _OffsetRay;
float _AverageAlbedo;
int _BackFaceCulling;
int _BakeSkyShadingDirection;

StructuredBuffer<float3> _ProbePositions;
RWStructuredBuffer<float4> _SkyOcclusionOut;
RWStructuredBuffer<float3> _SkyShadingOut;

void RayGenExecute(UnifiedRT::DispatchInfo dispatchInfo)
{
    const float kSHBasis0 = 0.28209479177387814347f;
    const float kSHBasis1 = 0.48860251190291992159f;

    int probeId = dispatchInfo.globalThreadIndex;

    QuasiRandomGenerator rngState;
    rngState.Init(uint2((uint)probeId, 0), _SampleId);

    if (_SampleId==0)
    {
        _SkyOcclusionOut[probeId] = float4(0,0,0,0);
        if (_BakeSkyShadingDirection > 0)
            _SkyShadingOut[probeId] = float3(0,0,0);
    }

    UnifiedRT::RayTracingAccelStruct accelStruct = UNIFIED_RT_GET_ACCEL_STRUCT(_AccelStruct);

    float2 u = float2(rngState.GetFloat(0), rngState.GetFloat(1));
    float3 rayFirstDirection = MapSquareToSphere(u);

    float pathWeight = 4.0f * PI; // 1 / SphereSamplePDF
    float3 normalWS = float3(0,1,0);
    float3 hitPointWS = float3(0,0,0);
    uint rayFlags = 0x0;
    if(_BackFaceCulling != 0)
        rayFlags = UnifiedRT::kRayFlagCullBackFacingTriangles;

    for (int bounceIndex=0; bounceIndex < _MaxBounces+1; bounceIndex++)
    {
        UnifiedRT::Ray ray;
        ray.tMin = 0;
        ray.tMax = FLT_MAX;
        ray.origin = float3(0, 0, 0);
        ray.direction = float3(0, 0, 0);
        UnifiedRT::Hit hitResult;

        if (bounceIndex==0)
        {
            ray.direction = rayFirstDirection;
            ray.origin = _ProbePositions[probeId].xyz;
        }
        else
        {
            u = float2(rngState.GetFloat(2*bounceIndex), rngState.GetFloat(2*bounceIndex+1));

            SampleDiffuseBrdf(u, normalWS, ray.direction);
            ray.direction = normalize(ray.direction);

            ray.origin = hitPointWS + _OffsetRay * ray.direction;
            float cosTheta = clamp(dot(normalWS, ray.direction),0.f,1.0f);

            if(cosTheta < 0.001f)
                break;

            pathWeight = pathWeight * _AverageAlbedo; // cosTheta * avgAlbedo / PI * PI/(cosTheta) == avgAlbedo
        }

        bool hasHit = false;

        hitResult = UnifiedRT::TraceRayClosestHit(dispatchInfo, accelStruct, 0xFFFFFFFF, ray, rayFlags);
        hasHit = hitResult.IsValid();

        if (hasHit)
        {
            UnifiedRT::InstanceData instanceInfo = UnifiedRT::GetInstance(hitResult.instanceID);
            UnifiedRT::HitGeomAttributes hitGeom = UnifiedRT::FetchHitGeomAttributesInWorldSpace(instanceInfo, hitResult);
            hitPointWS = hitGeom.position;
            normalWS = hitGeom.normal;
            if (dot(normalWS, ray.direction) > 0.0f) // flip normal if hitting backface
                normalWS *= -1.0f;
        }
        else
        {
            float norm = pathWeight / (float)_SampleCount;
            // Layout is DC, x, y, z
            float4 tempSH = float4(
                norm * kSHBasis0,
                rayFirstDirection.x * norm * kSHBasis1,
                rayFirstDirection.y * norm * kSHBasis1,
                rayFirstDirection.z * norm * kSHBasis1);

            _SkyOcclusionOut[probeId] += tempSH;
            if(_BakeSkyShadingDirection > 0)
                _SkyShadingOut[probeId] += ray.direction / _SampleCount;

            // break the loop;
            bounceIndex = _MaxBounces + 2;
        }
    }

    // Last sample
    if (_SampleId == _SampleCount - 1)
    {
        // Window L1 coefficients to make sure no value is negative when sampling SH, layout is DC, x, y, z
        float4 SHData = _SkyOcclusionOut[probeId];
        // find main direction for light
        float3 mainDir;
        mainDir.x = SHData.y;
        mainDir.y = SHData.z;
        mainDir.z = SHData.w;
        mainDir = normalize(mainDir);

        // find the value in the opposite direction, which is the lowest value in the SH
        float4 temp2 = float4(kSHBasis0, kSHBasis1 * -mainDir.x, kSHBasis1 * -mainDir.y, kSHBasis1 * -mainDir.z);
        float value = dot(temp2, SHData);
        float windowL1 = 1.0f;

        if (value < 0.0f)
        {
            // find the L1 factor for this value to be null instead of negative
            windowL1 = -(temp2.x * SHData.x) / dot(temp2.yzw, SHData.yzw);
            windowL1 = saturate(windowL1);
        }

        _SkyOcclusionOut[probeId].yzw *= windowL1;

        float radianceToIrradianceFactor = 2.0f / 3.0f;
        // This is a hacky solution for mitigating the radianceToIrradianceFactor based on the previous windowing operation.
        // The 1.125f exponent comes from experimental testing. It's the value that works the best when trying to match a bake and deringing done with the lightmapper, but it has no theoretical explanation.
        // In the future, we should replace these custom windowing and deringing operations with the ones used in the lightmapper to implement a more academical solution.
        _SkyOcclusionOut[probeId].yzw *= lerp(1.0f, radianceToIrradianceFactor, pow(windowL1, 1.125f));
    }
}

#ifdef UNIFIED_RT_BACKEND_COMPUTE

#pragma kernel EncodeShadingDirection

StructuredBuffer<float3> _SkyShadingPrecomputedDirection;
StructuredBuffer<float3> _SkyShadingDirections;
RWStructuredBuffer<uint> _SkyShadingIndices;

uint _ProbeCount;

uint LinearSearchClosestDirection(float3 direction)
{
    int indexMax = 255;
    float bestDot = -10.0f;
    int bestIndex = 0;

    for (int index=0; index< indexMax; index++)
    {
        float currentDot = dot(direction, _SkyShadingPrecomputedDirection[index]);
        if (currentDot > bestDot)
        {
            bestDot = currentDot;
            bestIndex = index;
        }
    }
    return bestIndex;
}

[numthreads(64, 1, 1)]
void EncodeShadingDirection(uint probeId : SV_DispatchThreadID)
{
    if (probeId >= _ProbeCount)
        return;

    uint bestDirectionIndex = 255;
    float norm = length(_SkyShadingDirections[probeId]);
    if (norm > 0.0001f)
        bestDirectionIndex = LinearSearchClosestDirection(_SkyShadingDirections[probeId] / norm);

    _SkyShadingIndices[probeId] = bestDirectionIndex;
}
#endif
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.core\Editor\Lighting\ProbeVolume\DynamicGI\DynamicGISkyOcclusion.urtshader---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.core\Editor\Lighting\ProbeVolume\RenderingLayerMask\TraceRenderingLayerMask.urtshader---------------
.
.
#define UNIFIED_RT_GROUP_SIZE_X 64
#define UNIFIED_RT_GROUP_SIZE_Y 1

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Lighting/ProbeVolume/ShaderVariablesProbeVolumes.cs.hlsl"

#include "Packages/com.unity.rendering.light-transport/Runtime/UnifiedRayTracing/FetchGeometry.hlsl"
#include "Packages/com.unity.rendering.light-transport/Runtime/UnifiedRayTracing/TraceRay.hlsl"

#define QRNG_METHOD_SOBOL
#define SAMPLE_COUNT 32
#define RAND_SAMPLES_PER_BOUNCE 2
#include "Packages/com.unity.rendering.light-transport/Runtime/Sampling/QuasiRandom.hlsl"
#include "Packages/com.unity.rendering.light-transport/Runtime/Sampling/Common.hlsl"

UNIFIED_RT_DECLARE_ACCEL_STRUCT(_AccelStruct);

StructuredBuffer<float3> _ProbePositions;
RWStructuredBuffer<uint> _LayerMasks;

float4 _RenderingLayerMasks;

void RayGenExecute(UnifiedRT::DispatchInfo dispatchInfo)
{
    UnifiedRT::Ray ray;
    ray.origin = _ProbePositions[dispatchInfo.globalThreadIndex].xyz;
    ray.tMax = FLT_MAX;
    ray.tMin = 0.0f;

    QuasiRandomGenerator rngState;
    rngState.Init(0, SAMPLE_COUNT);

    int4 hitCount = 0;

    UnifiedRT::RayTracingAccelStruct accelStruct = UNIFIED_RT_GET_ACCEL_STRUCT(_AccelStruct);

    for (uint i = 0; i < SAMPLE_COUNT; ++i)
    {
        float2 u = float2(rngState.GetFloat(2*i), rngState.GetFloat(2*i+1));
        ray.direction = MapSquareToSphere(u);

        uint hitMask = 0;
        UnifiedRT::Hit hit = UnifiedRT::TraceRayClosestHit(dispatchInfo, accelStruct, 0xFFFFFFFF, ray, 0);

        if (hit.IsValid() & hit.isFrontFace)
        {
            // we use material id to store layer mask
            uint objMask = g_AccelStructInstanceList[hit.instanceID].userMaterialID;

            [unroll]
            for (int l = 0; l < PROBE_MAX_REGION_COUNT; l++)
            {
                if ((asuint(_RenderingLayerMasks[l]) & objMask) != 0)
                    hitCount[l]++;
            }
        }
    }

    uint layerMask = 0;

    if (true)
    {
        // Find the layer with the most hits
        uint index = 0;
        layerMask = 0xF;

        [unroll]
        for (uint l = 1; l < PROBE_MAX_REGION_COUNT; l++)
        {
            if (hitCount[l] > hitCount[index])
                index = l;
        }
        if (hitCount[index] != 0)
            layerMask = 1u << index;
    }
    else
    {
        // Find any layer that was hit
        [unroll]
        for (uint l = 1; l < PROBE_MAX_REGION_COUNT; l++)
        {
            if (hitCount[l] != 0)
                layerMask |= 1u << l;
        }
    }

    _LayerMasks[dispatchInfo.globalThreadIndex] = layerMask;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.core\Editor\Lighting\ProbeVolume\RenderingLayerMask\TraceRenderingLayerMask.urtshader---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.core\Editor\Lighting\ProbeVolume\VirtualOffset\TraceVirtualOffset.urtshader---------------
.
.
#pragma only_renderers d3d11 playstation xboxone xboxseries vulkan metal webgpu switch
#define UNIFIED_RT_GROUP_SIZE_X 64
#define UNIFIED_RT_GROUP_SIZE_Y 1

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Sampling/Sampling.hlsl"

#include "Packages/com.unity.rendering.light-transport/Runtime/UnifiedRayTracing/FetchGeometry.hlsl"
#include "Packages/com.unity.rendering.light-transport/Runtime/UnifiedRayTracing/TraceRay.hlsl"


#define DISTANCE_THRESHOLD 5e-5f
#define DOT_THRESHOLD 1e-2f

#define SAMPLE_COUNT (3*3*3 - 1)

static const float k0 = 0, k1 = 1, k2 = 0.70710678118654752440084436210485f, k3 = 0.57735026918962576450914878050196f;
static const float3 k_RayDirections[SAMPLE_COUNT] = {
    float3(-k3, +k3, -k3), // -1  1 -1
    float3( k0, +k2, -k2), //  0  1 -1
    float3(+k3, +k3, -k3), //  1  1 -1
    float3(-k2, +k2,  k0), // -1  1  0
    float3( k0, +k1,  k0), //  0  1  0
    float3(+k2, +k2,  k0), //  1  1  0
    float3(-k3, +k3, +k3), // -1  1  1
    float3( k0, +k2, +k2), //  0  1  1
    float3(+k3, +k3, +k3), //  1  1  1

    float3(-k2,  k0, -k2), // -1  0 -1
    float3( k0,  k0, -k1), //  0  0 -1
    float3(+k2,  k0, -k2), //  1  0 -1
    float3(-k1,  k0,  k0), // -1  0  0
    // k0, k0, k0 - skip center position (which would be a zero-length ray)
    float3(+k1,  k0,  k0), //  1  0  0
    float3(-k2,  k0, +k2), // -1  0  1
    float3( k0,  k0, +k1), //  0  0  1
    float3(+k2,  k0, +k2), //  1  0  1

    float3(-k3, -k3, -k3), // -1 -1 -1
    float3( k0, -k2, -k2), //  0 -1 -1
    float3(+k3, -k3, -k3), //  1 -1 -1
    float3(-k2, -k2,  k0), // -1 -1  0
    float3( k0, -k1,  k0), //  0 -1  0
    float3(+k2, -k2,  k0), //  1 -1  0
    float3(-k3, -k3, +k3), // -1 -1  1
    float3( k0, -k2, +k2), //  0 -1  1
    float3(+k3, -k3, +k3), //  1 -1  1;
};

UNIFIED_RT_DECLARE_ACCEL_STRUCT(_AccelStruct);

struct ProbeData
{
    float3 position;
    float originBias;
    float tMax;
    float geometryBias;
    int probeIndex;
    float validityThreshold;
};

StructuredBuffer<ProbeData> _Probes;
RWStructuredBuffer<float3> _Offsets;

void RayGenExecute(UnifiedRT::DispatchInfo dispatchInfo)
{
    ProbeData probe = _Probes[dispatchInfo.globalThreadIndex];
    float3 outDirection = 0.0f;
    float maxDotSurface = -1;
    float minDist = FLT_MAX;

    UnifiedRT::Ray ray;
    ray.tMax = probe.tMax;
    ray.tMin = 0.0f;

    UnifiedRT::RayTracingAccelStruct accelStruct = UNIFIED_RT_GET_ACCEL_STRUCT(_AccelStruct);

    uint validHits = 0;
    for (uint i = 0; i < SAMPLE_COUNT; ++i)
    {
        ray.direction = k_RayDirections[i];
        ray.origin = probe.position + probe.originBias * ray.direction;

        UnifiedRT::Hit hit = UnifiedRT::TraceRayClosestHit(dispatchInfo, accelStruct, 0xFFFFFFFF, ray, 0);

        // If any of the closest hit is the sky or a front face, skip it
        if (!hit.IsValid() || hit.isFrontFace)
        {
            validHits++;
            continue;
        }

        float distanceDiff = hit.hitDistance - minDist;
        if (distanceDiff < DISTANCE_THRESHOLD)
        {
            UnifiedRT::HitGeomAttributes attributes = UnifiedRT::FetchHitGeomAttributes(hit, UnifiedRT::kGeomAttribFaceNormal);
            float dotSurface = dot(ray.direction, attributes.faceNormal);

            // If new distance is smaller by at least kDistanceThreshold, or if ray is at least DOT_THRESHOLD more colinear with normal
            if (distanceDiff < -DISTANCE_THRESHOLD || dotSurface - maxDotSurface > DOT_THRESHOLD)
            {
                outDirection = ray.direction;
                maxDotSurface = dotSurface;
                minDist = hit.hitDistance;
            }
        }
    }

    // Disable VO for probes that don't see enough backface
    // validity = percentage of backfaces seen
    float validity = 1.0f - validHits / (float)(SAMPLE_COUNT - 1.0f);
    if (validity <= probe.validityThreshold)
        outDirection = 0.0f;

    if (minDist == FLT_MAX)
        minDist = 0.0f;

    _Offsets[dispatchInfo.globalThreadIndex] = (minDist * 1.05f + probe.geometryBias) * outDirection;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.render-pipelines.core\Editor\Lighting\ProbeVolume\VirtualOffset\TraceVirtualOffset.urtshader---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.rendering.light-transport\Tests\Editor\UnifiedRayTracing\TraceRays.urtshader---------------
.
.
#define UNIFIED_RT_GROUP_SIZE_X 16
#define UNIFIED_RT_GROUP_SIZE_Y 8
#include "Packages/com.unity.rendering.light-transport/Runtime/UnifiedRayTracing/TraceRay.hlsl"

UNIFIED_RT_DECLARE_ACCEL_STRUCT(_AccelStruct);

struct RayWithFlags
{
    float3 origin;
    float tMin;
    float3 direction;
    float tMax;
    uint culling;
    uint instanceMask;
    uint padding;
    uint padding2;
};

StructuredBuffer<RayWithFlags> _Rays;
RWStructuredBuffer<UnifiedRT::Hit> _Hits;


void RayGenExecute(UnifiedRT::DispatchInfo dispatchInfo)
{
    RayWithFlags rayWithFlags = _Rays[dispatchInfo.globalThreadIndex];
    UnifiedRT::Ray ray;
    ray.origin = rayWithFlags.origin;
    ray.direction = rayWithFlags.direction;
    ray.tMin = rayWithFlags.tMin;
    ray.tMax = rayWithFlags.tMax;

    UnifiedRT::RayTracingAccelStruct accelStruct = UNIFIED_RT_GET_ACCEL_STRUCT(_AccelStruct);
    UnifiedRT::Hit hitResult = UnifiedRT::TraceRayClosestHit(dispatchInfo, accelStruct, rayWithFlags.instanceMask, ray, rayWithFlags.culling);

    _Hits[dispatchInfo.globalThreadIndex] = hitResult;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.rendering.light-transport\Tests\Editor\UnifiedRayTracing\TraceRays.urtshader---------------
.
.
#---------------BEGIN FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.rendering.light-transport\Tests\Editor\UnifiedRayTracing\TraceRaysAndFetchAttributes.urtshader---------------
.
.
#define UNIFIED_RT_GROUP_SIZE_X 16
#define UNIFIED_RT_GROUP_SIZE_Y 8
#include "Packages/com.unity.rendering.light-transport/Runtime/UnifiedRayTracing/FetchGeometry.hlsl"
#include "Packages/com.unity.rendering.light-transport/Runtime/UnifiedRayTracing/TraceRay.hlsl"

UNIFIED_RT_DECLARE_ACCEL_STRUCT(_AccelStruct);

struct RayWithFlags
{
    float3 origin;
    float tMin;
    float3 direction;
    float tMax;
    uint culling;
    uint instanceMask;
    uint padding;
    uint padding2;
};

StructuredBuffer<RayWithFlags> _Rays;
RWStructuredBuffer<UnifiedRT::Hit> _Hits;
RWStructuredBuffer<UnifiedRT::HitGeomAttributes> _HitAttributes;

void RayGenExecute(UnifiedRT::DispatchInfo dispatchInfo)
{

    RayWithFlags rayWithFlags = _Rays[dispatchInfo.globalThreadIndex];
    UnifiedRT::Ray ray;
    ray.origin = rayWithFlags.origin;
    ray.direction = rayWithFlags.direction;
    ray.tMin = rayWithFlags.tMin;
    ray.tMax = rayWithFlags.tMax;

    UnifiedRT::RayTracingAccelStruct accelStruct = UNIFIED_RT_GET_ACCEL_STRUCT(_AccelStruct);
    UnifiedRT::Hit hitResult = UnifiedRT::TraceRayClosestHit(dispatchInfo, accelStruct, rayWithFlags.instanceMask, ray, rayWithFlags.culling);
    if (hitResult.IsValid())
    {
        UnifiedRT::HitGeomAttributes hitAttribs = UnifiedRT::FetchHitGeomAttributes(hitResult);
        _HitAttributes[dispatchInfo.globalThreadIndex] = hitAttribs;
    }
    else
    {
        _HitAttributes[dispatchInfo.globalThreadIndex] = (UnifiedRT::HitGeomAttributes)0;
    }


    _Hits[dispatchInfo.globalThreadIndex] = hitResult;
}
.
.
#---------------END FILE: C:\Users\Zack\Unity Versions\6000.0.48f1\6000.0.48f1\Editor\Data\Resources\PackageManager\BuiltInPackages\com.unity.rendering.light-transport\Tests\Editor\UnifiedRayTracing\TraceRaysAndFetchAttributes.urtshader---------------
.
.
